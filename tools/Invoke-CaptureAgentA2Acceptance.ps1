[CmdletBinding()]
param([switch]$GraphOnly)
$ErrorActionPreference = 'Stop'
$serverRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$agentRoot = [IO.Path]::GetFullPath((Join-Path $serverRoot '../TagEkyc.CaptureAgent'))
$dispatch = Join-Path $serverRoot 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a2_windows_agent_profile_dispatch_v0_4.md'
if ((Get-FileHash -LiteralPath $dispatch -Algorithm SHA256).Hash -cne '93D06034E8A69F2F13B9CA8BFB4F407E4C7AE6E71FA8AB8B6A435E23E5861C8D') { throw 'A2 dispatch source freeze drift.' }
if (-not (Test-Path -LiteralPath (Join-Path $agentRoot 'src/TagEkyc.CaptureAgent.Client/TagEkyc.CaptureAgent.Client.csproj'))) { throw 'A2 exact sibling project missing.' }
if ((git -C $serverRoot rev-parse HEAD) -cne '4fafa17a43ad38f46c2e0c69d570f65b8d01abfe') { throw 'A2 server baseline drift.' }
if ((git -C $agentRoot rev-parse HEAD) -cne 'b193f6316e13a82fb2f9f985f68500feae194cfa') { throw 'A2 Agent baseline drift.' }
$readonlyServer = @('src/TagEkyc.Contracts/CaptureRuntime', 'src/TagEkyc.Api/CaptureRuntimeEndpoints.cs',
    'src/TagEkyc.Api/CaptureRuntimeExecutionEndpoints.cs', 'src/TagEkyc.Api/CaptureRuntimeRotationEndpoints.cs',
    'src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeEnrollmentApplicationService.cs',
    'src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeRotationApplicationService.cs',
    'src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs')
$serverDrift = @(git -C $serverRoot diff --name-only HEAD -- @readonlyServer)
if ($LASTEXITCODE -ne 0 -or $serverDrift.Count -ne 0) { throw 'A2 readonly server product dependency drift.' }
$authorizedAgentMutations = @(
    'src/TagEkyc.CaptureAgent.Client/TagEkycHttpClient.cs', 'src/TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs',
    'src/TagEkyc.CaptureAgent.Core/CaptureAgentProductionConfigGate.cs', 'src/TagEkyc.CaptureAgent.Core/RawExportIngressContracts.cs',
    'src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs', 'src/TagEkyc.CaptureAgent.Host/Program.cs',
    'src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs', 'tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BCaptureAgentTests.cs',
    'tests/TagEkyc.CaptureAgent.Tests/Tip74HttpClientTests.cs', 'tests/TagEkyc.CaptureAgent.Tests/Tip74CaptureAgentHostTests.cs',
    'tests/TagEkyc.CaptureAgent.Tests/Tip75WpfCaptureAppTests.cs',
    'src/TagEkyc.CaptureAgent.Crypto/packages.lock.json' # Homeowner bounded empty-RID-group correction
)
$rows = [regex]::Matches([IO.File]::ReadAllText($dispatch), '(?m)^\| `([^`]+)` \| `([A-F0-9]{64})` \|')
if ($rows.Count -ne 24) { throw 'A2 consumed-source census drift.' }
# Homeowner landing-only rebind: these five unchanged HEAD/index project blobs
# are LF. Pin only their exact Git bytes; do not normalize or accept old-or-new
# hashes. Dispatch v0.4 and all other readonly expectations remain unchanged.
$landingProjectHashes = @{
    'src/TagEkyc.CaptureAgent.Client/TagEkyc.CaptureAgent.Client.csproj' = '2B4F9BCAC0CECB103EE243648845E38494B1DBDC7DFF7E7EC0B19DE94B0DC7D5'
    'src/TagEkyc.CaptureAgent.Core/TagEkyc.CaptureAgent.Core.csproj' = '2976B76D71DC7FCCDDD5A7889F4766CCE2154066E2F02C5D5C17F6A76D4F3594'
    'src/TagEkyc.CaptureAgent.Host/TagEkyc.CaptureAgent.Host.csproj' = 'E146BEE7628AA3C931CD39DAF0589F97DAE9FEBB7EF3AB1250FB0EC8E986E03C'
    'src/TagEkyc.CaptureAgent.Crypto/TagEkyc.CaptureAgent.Crypto.csproj' = 'CE43C3005BD719CD734F455470D12DEE0C748557ED9C9A5F91A360AD1E098305'
    'tests/TagEkyc.CaptureAgent.Tests/TagEkyc.CaptureAgent.Tests.csproj' = '91C8C8AA3E8D46A0F1832CF0B48E608975292346B486384462BA1B16D288761B'
}
foreach ($row in $rows) {
    $relative = $row.Groups[1].Value
    $actual = (Get-FileHash -LiteralPath (Join-Path $agentRoot $relative) -Algorithm SHA256).Hash
    $expected = if ($landingProjectHashes.ContainsKey($relative)) { $landingProjectHashes[$relative] } else { $row.Groups[2].Value }
    if ($relative -notin $authorizedAgentMutations -and $actual -cne $expected) { throw "A2 readonly dependency drift: $relative" }
    if ($landingProjectHashes.ContainsKey($relative)) { Write-Output "A2 exact landing project: $relative $actual" }
    Write-Output "A2 candidate source: $relative $actual"
}
if ((Get-FileHash -LiteralPath (Join-Path $agentRoot 'src/TagEkyc.CaptureAgent.Ui/MainWindow.xaml.cs')).Hash -cne 'D40898C259D4B80089C6E9D45FE4A3D7A88A6F13BF87F45EBFE7EBAE066929BD') { throw 'A2 actual WPF caller drift.' }

function Invoke-A2Child([string]$File, [string[]]$Arguments, [hashtable]$Environment = @{}, [int]$TimeoutMilliseconds = 600000, [string]$WorkingDirectory = $serverRoot) {
    $start = [Diagnostics.ProcessStartInfo]::new($File)
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($arg in $Arguments) { $start.ArgumentList.Add($arg) }
    foreach ($entry in $Environment.GetEnumerator()) { $start.Environment[$entry.Key] = $entry.Value }
    $child = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $child.StandardOutput.ReadToEndAsync()
        $stderr = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit($TimeoutMilliseconds)) { $child.Kill($true); throw 'A2 child exceeded its bounded execution deadline.' }
        $out = $stdout.GetAwaiter().GetResult()
        $err = $stderr.GetAwaiter().GetResult()
        foreach ($secretName in @('POSTGRES_PASSWORD', 'TAGEKYC_A2_TEST_CONNECTION_STRING')) {
            if ($Environment.ContainsKey($secretName)) { $out = $out.Replace($Environment[$secretName], '[REDACTED]'); $err = $err.Replace($Environment[$secretName], '[REDACTED]') }
        }
        return @{ ExitCode = $child.ExitCode; Output = $out; Error = $err }
    } finally { $child.Dispose(); $start.Environment.Clear() }
}

function Test-A2StandaloneGraph {
    $testRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('tagekyc-a2-graph-' + [Guid]::NewGuid().ToString('N'))))
    $checkout = Join-Path $testRoot 'TagEkyc'
    New-Item -ItemType Directory -Path $checkout | Out-Null
    try {
        $archive = Join-Path $testRoot 'source.zip'
        $export = Invoke-A2Child 'git' @('archive', '--format=zip', "--output=$archive", '4fafa17a43ad38f46c2e0c69d570f65b8d01abfe')
        if ($export.ExitCode -ne 0) { throw 'A2 graph baseline archive failed.' }
        Expand-Archive -LiteralPath $archive -DestinationPath $checkout
        foreach ($relative in @('tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj', 'tests/TagEkyc.IntegrationTests/Tip88C1C6BA2ClientServerAcceptanceTests.cs')) {
            Copy-Item -LiteralPath (Join-Path $serverRoot $relative) -Destination (Join-Path $checkout $relative)
            if ((Get-FileHash -LiteralPath (Join-Path $serverRoot $relative)).Hash -cne (Get-FileHash -LiteralPath (Join-Path $checkout $relative)).Hash) { throw 'A2 graph candidate copy mismatch.' }
        }
        if (Test-Path -LiteralPath (Join-Path $testRoot 'TagEkyc.CaptureAgent')) { throw 'A2 negative graph unexpectedly contains Agent.' }
        $plain = Invoke-A2Child 'dotnet' @('build', 'TagEkyc.sln', '-p:RestoreLockedMode=true', '--verbosity', 'quiet') @{} 600000 $checkout
        if ($plain.ExitCode -ne 0) { Write-Output $plain.Output; Write-Output $plain.Error; throw 'A2 default standalone solution build failed.' }
        $optIn = Invoke-A2Child 'dotnet' @('restore', 'tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj', '--locked-mode', '-p:EnableCaptureAgentA2Acceptance=true') @{} 600000 $checkout
        if ($optIn.ExitCode -eq 0 -or (($optIn.Output + $optIn.Error) -notmatch 'A2 acceptance explicitly requires the exact sibling')) {
            throw 'A2 missing-sibling negative control did not fail explicitly.'
        }
        Write-Output 'P13 AcceptanceGraph_DefaultStandalone_OptInRequired: PASS; isolated default solution build succeeded, exact opt-in missing-sibling error observed; no tests silently skipped.'
    } finally {
        $resolved = [IO.Path]::GetFullPath($testRoot)
        $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
        if (-not $resolved.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
            [IO.Path]::GetFileName($resolved) -notmatch '^tagekyc-a2-graph-[0-9a-f]{32}$') { throw 'A2 graph cleanup target validation failed.' }
        if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
    }
}

if ($GraphOnly) { Test-A2StandaloneGraph; return }

$mutex = [Threading.Mutex]::new($false, 'Global\TagEkycA2Acceptance')
$ownsMutex = $false
$runId = [Guid]::NewGuid().ToString('N')
$containerName = 'tagekyc-a2-' + $runId
$database = 'tagekyc_a2_' + $runId
$dbUser = 'tagekyc_a2_' + $runId.Substring(0, 12)
$password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$childEnvironment = @{}
$created = $false
try {
    try { $ownsMutex = $mutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $ownsMutex = $true }
    if (-not $ownsMutex) { throw 'Another A2 acceptance run owns the mutex.' }
    $restore = Invoke-A2Child 'dotnet' @('restore', 'tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj', '-p:EnableCaptureAgentA2Acceptance=true', '--locked-mode')
    Write-Output $restore.Output
    if ($restore.ExitCode -ne 0) { throw 'A2 opt-in locked restore failed.' }
    $create = Invoke-A2Child 'docker' @('run', '-d', '--name', $containerName, '--label', "tagekyc.a2.run=$runId", '-p', '127.0.0.1::5432',
        '-e', 'POSTGRES_PASSWORD', '-e', 'POSTGRES_USER', '-e', 'POSTGRES_DB', 'postgres:16') @{
        POSTGRES_PASSWORD = $password; POSTGRES_USER = $dbUser; POSTGRES_DB = $database
    }
    if ($create.ExitCode -ne 0) { throw 'A2 dedicated PostgreSQL container could not start.' }
    $created = $true
    $inspect = Invoke-A2Child 'docker' @('inspect', '--format', '{{json .NetworkSettings.Ports}}', $containerName)
    if ($inspect.ExitCode -ne 0) { throw 'A2 PostgreSQL port inspection failed.' }
    $ports = $inspect.Output | ConvertFrom-Json
    $binding = $ports.'5432/tcp'[0]
    if ($binding.HostIp -ne '127.0.0.1') { throw 'A2 PostgreSQL is not loopback-only.' }
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        $probe = Invoke-A2Child 'docker' @('exec', $containerName, 'pg_isready', '-U', $dbUser, '-d', $database) @{} 10000
        if ($probe.ExitCode -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'A2 PostgreSQL did not become ready.' }
    $childEnvironment = @{
        TAGEKYC_A2_TEST_RUN_ID = $runId
        TAGEKYC_A2_TEST_CONNECTION_STRING = "Host=127.0.0.1;Port=$($binding.HostPort);Database=$database;Username=$dbUser;Password=$password;Pooling=false"
    }
    Write-Output "A2 isolated run: $runId (synthetic only)"
    $test = Invoke-A2Child 'dotnet' @('test', 'tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj', '--no-restore',
        '-p:EnableCaptureAgentA2Acceptance=true', '--filter', 'FullyQualifiedName~Tip88C1C6BA2ClientServerAcceptanceTests',
        '--logger', 'trx', '--results-directory', 'TestResults/a2-acceptance') $childEnvironment
    Write-Output $test.Output
    if ($test.ExitCode -ne 0) { Write-Output $test.Error; throw 'A2 real-server acceptance failed; inspect the bounded TRX.' }
    Test-A2StandaloneGraph
} finally {
    if ($created) {
        $identity = Invoke-A2Child 'docker' @('inspect', '--format', '{{.Name}}|{{index .Config.Labels "tagekyc.a2.run"}}', $containerName)
        if ($identity.ExitCode -eq 0 -and $identity.Output.Trim() -ceq "/$containerName|$runId") {
            $remove = Invoke-A2Child 'docker' @('rm', '-f', $containerName)
            if ($remove.ExitCode -ne 0) { Write-Warning "A2 isolated container cleanup failed: $containerName" }
        } else { Write-Warning "A2 container identity mismatch: refusing cleanup of $containerName" }
    }
    $childEnvironment.Clear(); $password = $null
    if ($ownsMutex) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
}
