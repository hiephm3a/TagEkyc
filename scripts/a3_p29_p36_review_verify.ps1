param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$AtCommit = 'c7b7eb9b1738b4134edcc4e34538aab025e43f0a'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Get-GitBlobEvidence([string]$Path) {
    $gitPath = $Path.Replace('\', '/')
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.WorkingDirectory = $RepoRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = 'cat-file blob "' + $AtCommit + ':' + $gitPath.Replace('"', '\"') + '"'

    $process = [Diagnostics.Process]::Start($start)
    $errorRead = $process.StandardError.ReadToEndAsync()
    $hasher = [Security.Cryptography.IncrementalHash]::CreateHash(
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    $buffer = New-Object byte[] 81920
    [long]$bytes = 0
    try {
        while (($read = $process.StandardOutput.BaseStream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $hasher.AppendData($buffer, 0, $read)
            $bytes += $read
        }
        $process.WaitForExit()
        $errorText = $errorRead.GetAwaiter().GetResult()
        Assert-True ($process.ExitCode -eq 0) "git blob is unavailable for $gitPath`: $errorText"
        return [pscustomobject]@{
            Sha256 = ([BitConverter]::ToString($hasher.GetHashAndReset())).Replace('-', '')
            Bytes = $bytes
        }
    }
    finally {
        $hasher.Dispose()
        $process.Dispose()
    }
}

function Get-GitBlobText([string]$Path) {
    $gitPath = $Path.Replace('\', '/')
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.WorkingDirectory = $RepoRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = 'cat-file blob "' + $AtCommit + ':' + $gitPath.Replace('"', '\"') + '"'

    $process = [Diagnostics.Process]::Start($start)
    try {
        $text = $process.StandardOutput.ReadToEnd()
        $errorText = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        Assert-True ($process.ExitCode -eq 0) "git blob is unavailable for $gitPath at $AtCommit`: $errorText"
        return $text
    }
    finally {
        $process.Dispose()
    }
}

$evidenceRoot = 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly'
$partitionPath = "$evidenceRoot/a3_activation_scope_partition_v1.tsv"
$mapPath = "$evidenceRoot/a3_p29_p36_row_evidence_map_v1.tsv"
$censusPath = "$evidenceRoot/a3_p29_p36_failed_run_census_v1.tsv"
$manifestPath = "$evidenceRoot/a3_p29_p36_review_manifest_v1.tsv"
$packetPath = "$evidenceRoot/a3_p29_p36_durable_worker_review_packet_v1.md"
$testSourcePath = 'tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs'

$partition = @((Get-GitBlobText $partitionPath) | ConvertFrom-Csv -Delimiter "`t")
$map = @((Get-GitBlobText $mapPath) | ConvertFrom-Csv -Delimiter "`t")
Assert-True ($partition.Count -eq 8) "partition row count must be 8, found $($partition.Count)"
Assert-True ($map.Count -eq 8) "evidence-map row count must be 8, found $($map.Count)"

$partitionIds = @($partition.RowId | Sort-Object)
$mapIds = @($map.RowId | Sort-Object)
Assert-True (($partitionIds -join "`n") -ceq ($mapIds -join "`n")) 'partition and evidence-map RowId sets differ'
Assert-True ((@($map.PublicOutcome | Sort-Object -Unique)).Count -eq 8) 'public outcomes are not one-to-one'
Assert-True ((@($map.ExecutionOutcome | Sort-Object -Unique)).Count -eq 8) 'execution outcomes are not one-to-one'
Assert-True ((@($map.NamedAssertion | Sort-Object -Unique)).Count -eq 8) 'named row assertions are not one-to-one'

$testSource = Get-GitBlobText $testSourcePath
foreach ($row in $map) {
    foreach ($assertion in ($row.NamedAssertion -split ' \+ ')) {
        Assert-True ($testSource.Contains($assertion)) "named assertion is absent from test source: $assertion"
    }
}

$manifest = @((Get-GitBlobText $manifestPath) | ConvertFrom-Csv -Delimiter "`t")
Assert-True ((@($manifest | Where-Object { $_.HashBasis -cne 'GIT_OBJECT_CONTENT' })).Count -eq 0) `
    'manifest contains a non-Git-object hash basis'
$mismatch = 0
foreach ($entry in $manifest) {
    $evidence = Get-GitBlobEvidence $entry.Path
    if ($evidence.Sha256 -cne $entry.Sha256 -or "$($evidence.Bytes)" -cne $entry.Bytes) {
        $mismatch++
    }
}
Assert-True ($mismatch -eq 0) "manifest mismatches: $mismatch"

$manifestRelativePath = 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_p29_p36_review_manifest_v1.tsv'
$manifestHash = (Get-GitBlobEvidence $manifestRelativePath).Sha256
$packet = Get-GitBlobText $packetPath
Assert-True ($packet.Contains($manifestHash)) 'review packet does not pin the current manifest SHA-256'

$census = @((Get-GitBlobText $censusPath) | ConvertFrom-Csv -Delimiter "`t")
$failedArtifacts = @()
foreach ($entry in $manifest | Where-Object { $_.Path.EndsWith('.trx', [StringComparison]::OrdinalIgnoreCase) }) {
    [xml]$trx = Get-GitBlobText $entry.Path
    if ([int]$trx.TestRun.ResultSummary.Counters.failed -gt 0) {
        $failedArtifacts += [IO.Path]::GetFileName($entry.Path)
    }
}
$censusArtifacts = @($census.Artifact | Sort-Object)
$failedArtifacts = @($failedArtifacts | Sort-Object)
Assert-True (($censusArtifacts -join "`n") -ceq ($failedArtifacts -join "`n")) 'failed-run census does not exactly cover failed TRX artifacts'
Assert-True ((@($census | Where-Object { [string]::IsNullOrWhiteSpace($_.Classification) })).Count -eq 0) 'unclassified failed run exists'

Write-Output "partition_rows=$($partition.Count)"
Write-Output "evidence_commit=$AtCommit"
Write-Output "evidence_map_rows=$($map.Count)"
Write-Output "manifest_entries=$($manifest.Count)"
Write-Output "manifest_mismatch=$mismatch"
Write-Output "failed_runs=$($census.Count)"
Write-Output "unclassified_failed_runs=0"
Write-Output "manifest_sha256=$manifestHash"
Write-Output 'A3_P29_P36_REVIEW_VERIFY=PASS'
