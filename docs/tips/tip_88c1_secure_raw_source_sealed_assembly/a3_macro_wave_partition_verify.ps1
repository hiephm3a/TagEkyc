param(
    [string]$DocDir = 'D:\Task\Remote Signing\TagEkyc\docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
)

$ErrorActionPreference = 'Stop'
$partitionPath = Join-Path $DocDir 'a3_macro_wave_partition_v1.tsv'
$scratchPath = Join-Path $DocDir 'a3_macro_wave_scratch_evidence_v1.tsv'
$ledgerPath = Join-Path $DocDir 'tip_88c1_c6b_a3_proof_reconciliation_v0_2.md'

$partition = @(Import-Csv -LiteralPath $partitionPath -Delimiter "`t")
$expectedHeader = 'RunId', 'MacroWave', 'ScenarioFamily', 'GuardId', 'CommandScope', 'TrxPath', 'TrxSha256', 'SourcePath', 'SourceSha256', 'Result', 'Classification', 'RestoreStatus', 'Notes'
$scratchHeader = (Get-Content -LiteralPath $scratchPath -TotalCount 1) -split "`t"
if ([string]::Join("`t", $scratchHeader) -cne [string]::Join("`t", $expectedHeader)) {
    throw 'SCRATCH_LEDGER_HEADER_INVALID'
}

function Normalize-RowId([string]$value) {
    return (($value -replace '`', '') -replace '\s+', ' ').Trim()
}

$openRows = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($line in Get-Content -LiteralPath $ledgerPath) {
    if ($line -notmatch '^\|\s*(?<row>.+?)\s*\|.*\|\s*(?<status>NOT IMPLEMENTED|CANDIDATE_PENDING_RATIFICATION)') { continue }
    $row = Normalize-RowId $Matches.row
    $status = if ($Matches.status -ceq 'NOT IMPLEMENTED') { 'NOT_IMPLEMENTED' } else { $Matches.status }
    if ($openRows.ContainsKey($row)) { throw "DUPLICATE_CANONICAL_OPEN_ROW row=$row" }
    $openRows[$row] = $status
}

$partitionSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$duplicate = [Collections.Generic.List[string]]::new()
$allowedScratchPairs = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $partition) {
    foreach ($field in @('RowId', 'CurrentStatus', 'MacroWave', 'ScenarioFamily', 'SharedGuard', 'ExitCondition')) {
        if ([string]::IsNullOrWhiteSpace($row.$field)) { throw "PARTITION_REQUIRED_FIELD_EMPTY row=$($row.RowId) field=$field" }
    }
    $normalized = Normalize-RowId $row.RowId
    if (-not $partitionSet.Add($normalized)) { $duplicate.Add($normalized) }
    [void]$allowedScratchPairs.Add("$($row.MacroWave)`t$($row.ScenarioFamily)")
    if ($openRows.ContainsKey($normalized) -and $row.CurrentStatus -cne $openRows[$normalized]) {
        throw "PARTITION_STATUS_MISMATCH row=$normalized expected=$($openRows[$normalized]) actual=$($row.CurrentStatus)"
    }
}
# Historical scratch evidence remains valid after all rows in a family are
# ratified and therefore removed from the open partition.
[void]$allowedScratchPairs.Add("W2_AUTHORIZED_IMPLEMENTATION`tPHASE_REACHABILITY")
[void]$allowedScratchPairs.Add("W3_ADMISSION_OUTCOMES`tCAPACITY")

$unassigned = @($openRows.Keys | Where-Object { -not $partitionSet.Contains($_) } | Sort-Object)
$extra = @($partitionSet | Where-Object { -not $openRows.ContainsKey($_) } | Sort-Object)
if ($openRows.Count -ne 46) { throw "OFFICIAL_OPEN_ROWS_INVALID expected=46 actual=$($openRows.Count)" }
if ($partition.Count -ne 46) { throw "PARTITION_ROWS_INVALID expected=46 actual=$($partition.Count)" }
if ($duplicate.Count -ne 0) { throw "PARTITION_DUPLICATE count=$($duplicate.Count) rows=$([string]::Join(',', $duplicate))" }
if ($unassigned.Count -ne 0) { throw "PARTITION_UNASSIGNED count=$($unassigned.Count) rows=$([string]::Join(',', $unassigned))" }
if ($extra.Count -ne 0) { throw "PARTITION_EXTRA count=$($extra.Count) rows=$([string]::Join(',', $extra))" }

$expectedWaveCounts = [ordered]@{
    W1_R2_R6_EXECUTION = 8
    W2_AUTHORIZED_IMPLEMENTATION = 11
    W3_ADMISSION_OUTCOMES = 5
    W4_INTERNAL_NO_EGRESS = 5
    W5_RETENTION_CUSTODY = 12
    W6_TRANSPORT_SEAMS = 5
}
foreach ($wave in $expectedWaveCounts.Keys) {
    $actual = @($partition | Where-Object MacroWave -CEQ $wave).Count
    if ($actual -ne $expectedWaveCounts[$wave]) { throw "WAVE_COUNT_INVALID wave=$wave expected=$($expectedWaveCounts[$wave]) actual=$actual" }
}

$scratch = @(Import-Csv -LiteralPath $scratchPath -Delimiter "`t")
$runIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($run in $scratch) {
    if ([string]::IsNullOrWhiteSpace($run.RunId)) { throw 'SCRATCH_RUN_ID_EMPTY' }
    if (-not $runIds.Add($run.RunId)) { throw "SCRATCH_RUN_ID_DUPLICATE id=$($run.RunId)" }
    foreach ($field in @('MacroWave', 'ScenarioFamily', 'CommandScope', 'Result', 'Classification', 'RestoreStatus')) {
        if ([string]::IsNullOrWhiteSpace($run.$field)) { throw "SCRATCH_REQUIRED_FIELD_EMPTY run=$($run.RunId) field=$field" }
    }
    $pair = "$($run.MacroWave)`t$($run.ScenarioFamily)"
    if (-not $allowedScratchPairs.Contains($pair)) {
        throw "SCRATCH_PARTITION_PAIR_INVALID run=$($run.RunId) wave=$($run.MacroWave) family=$($run.ScenarioFamily)"
    }
    if (-not [string]::IsNullOrWhiteSpace($run.TrxSha256) -and $run.TrxSha256 -notmatch '^[0-9A-F]{64}$') {
        throw "SCRATCH_TRX_SHA_INVALID run=$($run.RunId)"
    }
    if (-not [string]::IsNullOrWhiteSpace($run.SourceSha256) -and $run.SourceSha256 -notmatch '^[0-9A-F]{64}$') {
        throw "SCRATCH_SOURCE_SHA_INVALID run=$($run.RunId)"
    }
}

"official_open_rows=$($openRows.Count)"
"partition_rows=$($partition.Count)"
"assigned_once=$($partitionSet.Count)"
"duplicate=$($duplicate.Count)"
"unassigned=$($unassigned.Count)"
"extra=$($extra.Count)"
"macro_waves=$($expectedWaveCounts.Count)"
foreach ($wave in $expectedWaveCounts.Keys) {
    "wave_$wave=$($expectedWaveCounts[$wave])"
}
"scratch_runs=$($scratch.Count)"
