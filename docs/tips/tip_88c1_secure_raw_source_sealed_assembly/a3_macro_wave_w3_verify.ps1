param(
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$server = (Resolve-Path -LiteralPath $ServerRoot).Path
$agent = (Resolve-Path -LiteralPath $AgentRoot).Path
$doc = Join-Path $server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$manifestPath = Join-Path $doc 'a3_macro_wave_w3_candidate_manifest_v1.tsv'
$manifest = @(Import-Csv -Delimiter "`t" -LiteralPath $manifestPath)
$prior = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_candidate_manifest_v2.tsv'))
$inventory = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w3_trx_inventory_v1.tsv'))
$failed = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w3_failed_run_census_v1.tsv'))
$scratch = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_scratch_evidence_v1.tsv'))
$allowed = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w3_allowed_delta_v1.tsv'))
$mutants = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_preflight_recorded_mutants.tsv'))

function Index($rows) {
    $result = @{}
    foreach ($row in $rows) {
        $key = "$($row.repo)/$($row.repo_relative_path)"
        if ($result.ContainsKey($key)) { throw "DUPLICATE_MANIFEST_PATH $key" }
        $result[$key] = $row
    }
    return $result
}
function Assert-Equal($expected, $actual, [string]$name) {
    if ($expected -ne $actual) { throw "$name expected=$expected actual=$actual" }
}

$current = Index $manifest
$predecessor = Index $prior
foreach ($key in $current.Keys) {
    $slash = $key.IndexOf('/')
    $repo = $key.Substring(0, $slash)
    $relative = $key.Substring($slash + 1).Replace('/', '\')
    $root = if ($repo -eq 'server') { $server } else { $agent }
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "MANIFEST_FILE_MISSING $key" }
    Assert-Equal $current[$key].sha256 (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash "MANIFEST_SHA $key"
}

$delta = @{}
foreach ($key in $current.Keys) {
    if (-not $predecessor.ContainsKey($key) -or $predecessor[$key].sha256 -cne $current[$key].sha256) {
        $delta[$key] = $true
    }
}
foreach ($key in $predecessor.Keys) {
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

$actualW3 = @{}
foreach ($pair in @(@('server',$server),@('agent',$agent))) {
    foreach ($file in @(Get-ChildItem -LiteralPath $pair[1] -Recurse -File -Filter 'a3-w3*.trx')) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($pair[1], $file.FullName).Replace('\','/')
        $actualW3["$($pair[0])/$relative"] = $true
    }
}
$unlisted = @($actualW3.Keys | Where-Object { -not $inventoryPaths.ContainsKey($_) })
$missing = @($inventoryPaths.Keys | Where-Object { -not $actualW3.ContainsKey($_) })
if ($unlisted.Count) { throw "W3_TRX_UNLISTED $($unlisted -join ',')" }
if ($missing.Count) { throw "W3_TRX_MISSING $($missing -join ',')" }

$failedPaths = @{}
foreach ($row in $failed) {
    if ($failedPaths.ContainsKey($row.path)) { throw "DUPLICATE_FAILED_PATH $($row.path)" }
    $failedPaths[$row.path] = $true
    if (-not $inventoryPaths.ContainsKey($row.path)) { throw "FAILED_NOT_IN_INVENTORY $($row.path)" }
    if ($inventoryPaths[$row.path].result_summary -ne 'Failed') { throw "FAILED_ROW_NOT_FAILED $($row.path)" }
    if ($row.classification -notin @('EVIDENCE_RED','SUPERSEDED_RED','EXCLUDED_DIAGNOSTIC')) { throw "BAD_FAILED_CLASS $($row.path)" }
    if ([string]::IsNullOrWhiteSpace($row.locator)) { throw "FAILED_LOCATOR_MISSING $($row.path)" }
    if ($row.classification -eq 'EVIDENCE_RED' -and [string]::IsNullOrWhiteSpace($row.guard_or_row)) { throw "EVIDENCE_GUARD_MISSING $($row.path)" }
    if ($row.classification -eq 'SUPERSEDED_RED' -and [string]::IsNullOrWhiteSpace($row.successor_trx)) { throw "SUPERSEDED_SUCCESSOR_MISSING $($row.path)" }
}
$actualFailed = @($inventory | Where-Object result_summary -eq 'Failed')
Assert-Equal $actualFailed.Count $failed.Count 'FAILED_RUN_CENSUS_COUNT'

$mutantHashes = @{}
foreach ($row in $mutants) {
    if ($row.sha256 -notmatch '^[0-9A-F]{64}$') { throw "BAD_MUTANT_SHA $($row.sha256)" }
    if ($mutantHashes.ContainsKey($row.sha256)) { throw "DUPLICATE_MUTANT_SHA $($row.sha256)" }
    $mutantHashes[$row.sha256] = $true
}
$live = @($manifest | Where-Object { $mutantHashes.ContainsKey($_.sha256) })
if ($live.Count) { throw "LIVE_MUTANT_MATCH count=$($live.Count)" }

$expected = [ordered]@{
    'server/src/TagEkyc.Application/RawExport/CaptureRuntimeRawIngressAdmissionService.cs' = '54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C'
    'server/src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerTransactionFacade.cs' = '050D627D63B13621C442CDF0645B910A07225065F940E4EDDE8A93E737D120ED'
}
foreach ($key in $expected.Keys) {
    if (-not $current.ContainsKey($key)) { throw "RESTORED_SOURCE_NOT_IN_MANIFEST $key" }
    Assert-Equal $expected[$key] $current[$key].sha256 "RESTORED_SOURCE_SHA $key"
}

$partition = @(& (Join-Path $doc 'a3_macro_wave_partition_verify.ps1'))
if ($partition -notcontains 'assigned_once=48' -or $partition -notcontains 'wave_W3_ADMISSION_OUTCOMES=7') {
    throw 'PARTITION_VERIFY_FAILED'
}
$stagedServer = @(& git -C $server diff --cached --name-only).Count
$conflictedServer = @(& git -C $server ls-files -u).Count
$stagedAgent = @(& git -C $agent diff --cached --name-only).Count
$conflictedAgent = @(& git -C $agent ls-files -u).Count

"manifest_files=$($manifest.Count)"
"manifest_sha=$((Get-FileHash -Algorithm SHA256 -LiteralPath $manifestPath).Hash)"
"delta_added=$(@($delta.Keys | Where-Object { -not $predecessor.ContainsKey($_) }).Count)"
"delta_modified=$(@($delta.Keys | Where-Object { $predecessor.ContainsKey($_) }).Count)"
"delta_deleted=0"
"trx_total=$($inventory.Count)"
"whole_repo_w3_trx=$($actualW3.Count)"
"failed_runs_total=$($failed.Count)"
"evidence_red=$(@($failed | Where-Object classification -eq 'EVIDENCE_RED').Count)"
"superseded_red=$(@($failed | Where-Object classification -eq 'SUPERSEDED_RED').Count)"
"excluded_diagnostic_failed=$(@($failed | Where-Object classification -eq 'EXCLUDED_DIAGNOSTIC').Count)"
"excluded_diagnostic_all=$(@($scratch | Where-Object { $_.RunId -like 'W3-*' -and $_.Classification -eq 'EXCLUDED_DIAGNOSTIC' }).Count)"
"unclassified_failed_runs=0"
"outside_name_family_failed=$($unlisted.Count)"
"mutants=$($mutantHashes.Count)"
"live_mutant_matches=$($live.Count)"
"staged_server=$stagedServer conflicted_server=$conflictedServer"
"staged_agent=$stagedAgent conflicted_agent=$conflictedAgent"
