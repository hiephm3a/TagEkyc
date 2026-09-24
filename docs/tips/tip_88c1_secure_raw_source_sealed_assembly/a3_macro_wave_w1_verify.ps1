param(
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$server = (Resolve-Path -LiteralPath $ServerRoot).Path
$agent = (Resolve-Path -LiteralPath $AgentRoot).Path
$doc = Join-Path $server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$manifest = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w1_candidate_manifest.tsv'))
$predecessor = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_r2r6_remaining_cluster_candidate_manifest_v4.tsv'))
$inventory = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w1_r2r6_trx_inventory.tsv'))
$failed = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w1_r2r6_failed_run_census.tsv'))
$scratch = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_scratch_evidence_v1.tsv'))
$allowed = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w1_allowed_delta.tsv'))
$mutants = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_preflight_recorded_mutants.tsv'))

function Index($rows) {
    $result = @{}
    foreach ($row in $rows) {
        $key = "$($row.repo)/$($row.repo_relative_path)"
        if ($result.ContainsKey($key)) { throw "DUPLICATE_MANIFEST_PATH $key" }
        $result[$key] = $row
    }
    $result
}
function Assert-Equal($expected, $actual, [string]$name) {
    if ($expected -ne $actual) { throw "$name expected=$expected actual=$actual" }
}

$current = Index $manifest
$prior = Index $predecessor
$delta = @{}
foreach ($key in $current.Keys) {
    if (-not $prior.ContainsKey($key) -or $prior[$key].sha256 -ne $current[$key].sha256) { $delta[$key] = $true }
}
foreach ($key in $prior.Keys) {
    if (-not $current.ContainsKey($key)) { throw "UNEXPECTED_DELETION $key" }
}
$allowedPaths = @{}
foreach ($row in $allowed) {
    if ($allowedPaths.ContainsKey($row.path)) { throw "DUPLICATE_ALLOWED_PATH $($row.path)" }
    $allowedPaths[$row.path] = $true
}
foreach ($key in $delta.Keys) { if (-not $allowedPaths.ContainsKey($key)) { throw "DELTA_OUTSIDE_ALLOWLIST $key" } }
foreach ($key in $allowedPaths.Keys) { if (-not $delta.ContainsKey($key)) { throw "ALLOWLIST_WITHOUT_DELTA $key" } }

$inventoryPaths = @{}
foreach ($row in $inventory) {
    if ($inventoryPaths.ContainsKey($row.path)) { throw "DUPLICATE_TRX_PATH $($row.path)" }
    $inventoryPaths[$row.path] = $row
    if (-not $current.ContainsKey($row.path)) { throw "TRX_NOT_IN_MANIFEST $($row.path)" }
    $root = if ($row.path.StartsWith('server/')) { $server } else { $agent }
    $relative = $row.path.Substring($row.path.IndexOf('/') + 1).Replace('/', '\')
    $path = Join-Path $root $relative
    [xml]$trx = Get-Content -Raw -LiteralPath $path
    $c = $trx.TestRun.ResultSummary.Counters
    Assert-Equal $row.sha256 (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash "TRX_SHA $($row.path)"
    Assert-Equal $row.result_summary $trx.TestRun.ResultSummary.outcome "TRX_OUTCOME $($row.path)"
    foreach ($name in @('total','passed','failed')) { Assert-Equal ([int]$row.$name) ([int]$c.$name) "TRX_$name $($row.path)" }
    Assert-Equal ([int]$row.skipped) ([int]$c.notExecuted) "TRX_skipped $($row.path)"
}

$failedPaths = @{}
foreach ($row in $failed) {
    if ($failedPaths.ContainsKey($row.path)) { throw "DUPLICATE_FAILED_PATH $($row.path)" }
    $failedPaths[$row.path] = $true
    if (-not $inventoryPaths.ContainsKey($row.path)) { throw "FAILED_NOT_IN_INVENTORY $($row.path)" }
    if ($inventoryPaths[$row.path].result_summary -ne 'Failed') { throw "FAILED_ROW_NOT_FAILED $($row.path)" }
    if ($row.classification -notin @('EVIDENCE_RED','SUPERSEDED_RED','EXCLUDED_DIAGNOSTIC')) { throw "BAD_FAILED_CLASS $($row.path)" }
    if ([string]::IsNullOrWhiteSpace($row.locator)) { throw "FAILED_LOCATOR_MISSING $($row.path)" }
}
$actualFailed = @($inventory | Where-Object result_summary -eq 'Failed')
Assert-Equal $actualFailed.Count $failed.Count 'FAILED_RUN_CENSUS_COUNT'

$excludedDiagnostics = @($scratch | Where-Object classification -eq 'EXCLUDED_DIAGNOSTIC')
foreach ($row in $excludedDiagnostics) {
    $scratchPath = "server/$($row.TrxPath)"
    if (-not $inventoryPaths.ContainsKey($scratchPath)) { throw "SCRATCH_DIAGNOSTIC_NOT_IN_INVENTORY $scratchPath" }
    if ($inventoryPaths[$scratchPath].result_summary -ne 'Completed') { throw "SCRATCH_DIAGNOSTIC_NOT_COMPLETED $scratchPath" }
}

$newTrx = @($delta.Keys | Where-Object { $_.EndsWith('.trx', [StringComparison]::OrdinalIgnoreCase) })
$outside = @($newTrx | Where-Object { -not $inventoryPaths.ContainsKey($_) })
if ($outside.Count -ne 0) { throw "OUTSIDE_NAME_FAMILY_TRX $($outside -join ',')" }

$mutantHashes = @{}
foreach ($row in $mutants) {
    if ($row.sha256 -notmatch '^[0-9A-F]{64}$') { throw "BAD_MUTANT_SHA $($row.sha256)" }
    if ($mutantHashes.ContainsKey($row.sha256)) { throw "DUPLICATE_MUTANT_SHA $($row.sha256)" }
    $mutantHashes[$row.sha256] = $true
}
$live = @($manifest | Where-Object { $mutantHashes.ContainsKey($_.sha256) })

$partitionOutput = @(& (Join-Path $doc 'a3_macro_wave_partition_verify.ps1'))
if ($partitionOutput -notcontains 'assigned_once=51' -or $partitionOutput -notcontains 'unassigned=0') {
    throw 'PARTITION_VERIFY_FAILED'
}

$stagedServer = @(& git -C $server diff --cached --name-only).Count
$conflictedServer = @(& git -C $server ls-files -u).Count
$stagedAgent = @(& git -C $agent diff --cached --name-only).Count
$conflictedAgent = @(& git -C $agent ls-files -u).Count

"manifest_files=$($manifest.Count)"
"manifest_sha=$((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $doc 'a3_macro_wave_w1_candidate_manifest.tsv')).Hash)"
"delta_added=$(@($delta.Keys | Where-Object { -not $prior.ContainsKey($_) }).Count)"
"delta_modified=$(@($delta.Keys | Where-Object { $prior.ContainsKey($_) }).Count)"
"delta_deleted=0"
"trx_total=$($inventory.Count)"
"failed_runs_total=$($failed.Count)"
"evidence_red=$(@($failed | Where-Object classification -eq 'EVIDENCE_RED').Count)"
"superseded_red=$(@($failed | Where-Object classification -eq 'SUPERSEDED_RED').Count)"
"excluded_diagnostic=$($excludedDiagnostics.Count)"
"unclassified_failed_runs=0"
"outside_name_family_failed=$($outside.Count)"
"mutants=$($mutantHashes.Count)"
"live_mutant_matches=$($live.Count)"
"staged_server=$stagedServer conflicted_server=$conflictedServer"
"staged_agent=$stagedAgent conflicted_agent=$conflictedAgent"
