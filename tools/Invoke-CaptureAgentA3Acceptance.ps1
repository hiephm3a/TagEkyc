[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$serverRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$agentRoot = [IO.Path]::GetFullPath((Join-Path $serverRoot '../TagEkyc.CaptureAgent'))
$testProject = Join-Path $serverRoot 'tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj'
$a2Acceptance = Join-Path $serverRoot 'tests/TagEkyc.IntegrationTests/Tip88C1C6BA2ClientServerAcceptanceTests.cs'
$a3Acceptance = Join-Path $serverRoot 'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ClientServerAcceptanceTests.cs'
$agentClient = Join-Path $agentRoot 'src/TagEkyc.CaptureAgent.Client/TagEkyc.CaptureAgent.Client.csproj'
$acceptanceClasses = @(
    'TagEkyc.IntegrationTests.Tip88C1C6BA2ClientServerAcceptanceTests'
    'TagEkyc.IntegrationTests.Tip88C1C6BA3ClientServerAcceptanceTests'
    'TagEkyc.IntegrationTests.Tip88C1C6BA3AgentRawCrt1ParserTests'
)
$testFilter = ($acceptanceClasses | ForEach-Object { 'FullyQualifiedName~' + $_ }) -join '|'

if (-not [Environment]::Is64BitProcess -or -not $IsWindows) {
    throw 'A3 two-repo acceptance requires Windows x64; unsupported execution is not a pass.'
}
foreach ($path in @($testProject, $a2Acceptance, $a3Acceptance, $agentClient)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "A3 exact two-repo acceptance dependency missing: $path"
    }
}

function Invoke-A3Child(
    [string]$File,
    [string[]]$Arguments,
    [hashtable]$Environment = @{},
    [int]$TimeoutMilliseconds = 600000
) {
    $start = [Diagnostics.ProcessStartInfo]::new($File)
    $start.WorkingDirectory = $serverRoot
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    foreach ($entry in $Environment.GetEnumerator()) { $start.Environment[$entry.Key] = $entry.Value }
    $child = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $child.StandardOutput.ReadToEndAsync()
        $stderr = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit($TimeoutMilliseconds)) {
            $child.Kill($true)
            throw "A3 child exceeded its bounded deadline: $File"
        }
        $output = $stdout.GetAwaiter().GetResult()
        $errorOutput = $stderr.GetAwaiter().GetResult()
        foreach ($secretName in @('POSTGRES_PASSWORD', 'TAGEKYC_A2_TEST_CONNECTION_STRING')) {
            if ($Environment.ContainsKey($secretName)) {
                $output = $output.Replace($Environment[$secretName], '[REDACTED]')
                $errorOutput = $errorOutput.Replace($Environment[$secretName], '[REDACTED]')
            }
        }
        return @{ ExitCode = $child.ExitCode; Output = $output; Error = $errorOutput }
    }
    finally {
        $start.Environment.Clear()
        $child.Dispose()
    }
}

$msbuildProperties = @(
    '-p:EnableCaptureAgentA2Acceptance=true'
    '-p:EnableCaptureAgentA3Acceptance=true'
)
$mutex = [Threading.Mutex]::new($false, 'Global\TagEkycA3Acceptance')
$ownsMutex = $false
$runId = [Guid]::NewGuid().ToString('N')
$containerName = 'tagekyc-a2-' + $runId
$database = 'tagekyc_a2_' + $runId
$dbUser = 'tagekyc_a2_' + $runId.Substring(0, 12)
$password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$resultDirectory = Join-Path $serverRoot ('TestResults/a3-client-server-acceptance/' + $runId)
$trx = Join-Path $resultDirectory 'a3-client-server-acceptance.trx'
$childEnvironment = @{}
$created = $false

try {
    try { $ownsMutex = $mutex.WaitOne(0) }
    catch [Threading.AbandonedMutexException] { $ownsMutex = $true }
    if (-not $ownsMutex) { throw 'Another A3 acceptance run owns the mutex.' }
    if (Test-Path -LiteralPath $resultDirectory) { throw 'A3 fresh result directory unexpectedly exists.' }

    # Discovery builds the exact candidate graph once. Execution below uses
    # --no-build so discovered and executed identities come from one assembly.
    $discoveryArguments = @('test', $testProject, '--no-restore') + $msbuildProperties + @(
        '--filter', $testFilter,
        '--list-tests',
        '--verbosity', 'quiet'
    )
    $discovery = Invoke-A3Child 'dotnet' $discoveryArguments
    if ($discovery.ExitCode -ne 0) {
        Write-Output $discovery.Output
        Write-Output $discovery.Error
        throw 'A3 combined acceptance discovery/build failed.'
    }
    $discovered = @($discovery.Output -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object {
        $candidate = $_
        @($acceptanceClasses | Where-Object { $candidate.StartsWith($_ + '.', [StringComparison]::Ordinal) }).Count -eq 1
    } | Sort-Object -Unique)
    if ($discovered.Count -eq 0) { throw 'A3 combined acceptance discovery returned no named tests.' }
    foreach ($className in $acceptanceClasses) {
        if (@($discovered | Where-Object { $_.StartsWith($className + '.', [StringComparison]::Ordinal) }).Count -eq 0) {
            throw "A3 combined acceptance class was compiled but not discovered: $className"
        }
    }

    $create = Invoke-A3Child 'docker' @(
        'run', '-d', '--name', $containerName,
        '--label', "tagekyc.a2.run=$runId",
        '-p', '127.0.0.1::5432',
        '-e', 'POSTGRES_PASSWORD', '-e', 'POSTGRES_USER', '-e', 'POSTGRES_DB',
        'postgres:16'
    ) @{
        POSTGRES_PASSWORD = $password
        POSTGRES_USER = $dbUser
        POSTGRES_DB = $database
    }
    if ($create.ExitCode -ne 0) { throw 'A3 combined acceptance PostgreSQL container could not start.' }
    $created = $true

    $inspect = Invoke-A3Child 'docker' @('inspect', '--format', '{{json .NetworkSettings.Ports}}', $containerName)
    if ($inspect.ExitCode -ne 0) { throw 'A3 combined acceptance PostgreSQL port inspection failed.' }
    $ports = $inspect.Output | ConvertFrom-Json
    $binding = $ports.'5432/tcp'[0]
    if ($binding.HostIp -ne '127.0.0.1') { throw 'A3 combined acceptance PostgreSQL is not loopback-only.' }
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        $probe = Invoke-A3Child 'docker' @('exec', $containerName, 'pg_isready', '-U', $dbUser, '-d', $database) @{} 10000
        if ($probe.ExitCode -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'A3 combined acceptance PostgreSQL did not become ready.' }

    $childEnvironment = @{
        TAGEKYC_A2_TEST_RUN_ID = $runId
        TAGEKYC_A2_TEST_CONNECTION_STRING = "Host=127.0.0.1;Port=$($binding.HostPort);Database=$database;Username=$dbUser;Password=$password;Pooling=false"
    }
    $testArguments = @('test', $testProject, '--no-build', '--no-restore') + $msbuildProperties + @(
        '--filter', $testFilter,
        '--logger', 'trx;LogFileName=a3-client-server-acceptance.trx',
        '--results-directory', $resultDirectory,
        '--verbosity', 'minimal'
    )
    $test = Invoke-A3Child 'dotnet' $testArguments $childEnvironment
    Write-Output $test.Output
    if ($test.ExitCode -ne 0) {
        Write-Output $test.Error
        throw 'A3 combined A2/A3 acceptance failed.'
    }
    if (-not (Test-Path -LiteralPath $trx -PathType Leaf)) { throw 'A3 combined acceptance TRX is missing.' }

    [xml]$document = Get-Content -LiteralPath $trx -Raw
    $results = @($document.SelectNodes("//*[local-name()='UnitTestResult']"))
    $definitions = @($document.SelectNodes("//*[local-name()='UnitTest']"))
    $classByTestId = @{}
    foreach ($definition in $definitions) {
        $method = $definition.SelectSingleNode(".//*[local-name()='TestMethod']")
        $classByTestId[$definition.id] = $method.className
    }
    $actual = @($results | ForEach-Object { $_.testName } | Sort-Object -Unique)
    if ($results.Count -ne $actual.Count) { throw 'A3 combined acceptance contains duplicate test identities.' }
    foreach ($result in $results) {
        if ($result.outcome -cne 'Passed') { throw "A3 combined acceptance non-pass: $($result.testName) $($result.outcome)" }
        $className = $classByTestId[$result.testId]
        if ($className -notin $acceptanceClasses) { throw "A3 combined acceptance executed an unexpected class: $className" }
    }
    if ($discovered.Count -ne $actual.Count -or
        @($discovered | Where-Object { $_ -notin $actual }).Count -ne 0 -or
        @($actual | Where-Object { $_ -notin $discovered }).Count -ne 0) {
        throw 'A3 combined acceptance discovery/execution identity closure failed.'
    }
    $counters = $document.TestRun.ResultSummary.Counters
    if ([int]$counters.total -ne $discovered.Count -or [int]$counters.executed -ne $discovered.Count -or
        [int]$counters.passed -ne $discovered.Count -or [int]$counters.failed -ne 0 -or
        [int]$counters.notExecuted -ne 0) {
        throw 'A3 combined acceptance counter closure failed.'
    }
    $trxSha = (Get-FileHash -LiteralPath $trx -Algorithm SHA256).Hash
    Write-Output "A3_TWO_REPO_ACCEPTANCE: $($discovered.Count)/$($discovered.Count) PASS (A2 + A3 + CRT1 parser; synthetic/non-patient)"
    Write-Output "A3_TWO_REPO_ACCEPTANCE_TRX_SHA256: $trxSha"
}
finally {
    if ($created) {
        $identity = Invoke-A3Child 'docker' @('inspect', '--format', '{{.Name}}|{{index .Config.Labels "tagekyc.a2.run"}}', $containerName)
        if ($identity.ExitCode -eq 0 -and $identity.Output.Trim() -ceq "/$containerName|$runId") {
            $remove = Invoke-A3Child 'docker' @('rm', '-f', $containerName)
            if ($remove.ExitCode -ne 0) { Write-Warning "A3 acceptance container cleanup failed: $containerName" }
        }
        else { Write-Warning "A3 acceptance container identity mismatch: refusing cleanup of $containerName" }
    }
    $childEnvironment.Clear()
    $password = $null
    if ($ownsMutex) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
