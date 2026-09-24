param(
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$server = (Resolve-Path -LiteralPath $ServerRoot).Path
$agent = (Resolve-Path -LiteralPath $AgentRoot).Path
$doc = Join-Path $server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$manifest = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_candidate_manifest_v2.tsv'))
$predecessor = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_candidate_manifest.tsv'))
$inventory = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_trx_inventory_v2.tsv'))
$failed = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_failed_run_census_v2.tsv'))
$scratch = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_scratch_evidence_v1.tsv'))
$allowed = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_allowed_delta_v2.tsv'))
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
    if (-not $prior.ContainsKey($key) -or $prior[$key].sha256 -cne $current[$key].sha256) { $delta[$key] = $true }
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
    if ($row.classification -eq 'EVIDENCE_RED' -and [string]::IsNullOrWhiteSpace($row.guard_or_row)) { throw "EVIDENCE_GUARD_MISSING $($row.path)" }
    if ($row.classification -eq 'SUPERSEDED_RED' -and [string]::IsNullOrWhiteSpace($row.successor_trx)) { throw "SUPERSEDED_SUCCESSOR_MISSING $($row.path)" }
}
$actualFailed = @($inventory | Where-Object result_summary -eq 'Failed')
Assert-Equal $actualFailed.Count $failed.Count 'FAILED_RUN_CENSUS_COUNT'

$actualW2Trx = @{}
foreach ($repo in @('server','agent')) {
    $root = if ($repo -eq 'server') { $server } else { $agent }
    foreach ($file in @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter 'a3-w2-*.trx')) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\','/')
        $actualW2Trx["$repo/$relative"] = $true
    }
}
$unlistedW2 = @($actualW2Trx.Keys | Where-Object { -not $inventoryPaths.ContainsKey($_) })
$missingW2 = @($inventoryPaths.Keys | Where-Object { -not $actualW2Trx.ContainsKey($_) })
if ($unlistedW2.Count -ne 0) { throw "W2_TRX_UNLISTED $($unlistedW2 -join ',')" }
if ($missingW2.Count -ne 0) { throw "W2_TRX_MISSING $($missingW2 -join ',')" }

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
if ($live.Count -ne 0) { throw "LIVE_MUTANT_MATCH count=$($live.Count)" }

$expectedRestored = [ordered]@{
    'server/src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerTransactionFacade.cs' = '050D627D63B13621C442CDF0645B910A07225065F940E4EDDE8A93E737D120ED'
    'server/src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs' = '86B67AD6751E6270D7EA2BE1053E7C4338A74F94C8FAD65E10133DDFA5C8187F'
    'server/src/TagEkyc.Application/RawExport/CaptureRuntimeRawIngressAdmissionService.cs' = '54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C'
    'server/src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs' = 'F9CE348596EBCC4C79F64675A427AFB179BE5C31D9567C44580AB709ECB313F9'
    'server/src/TagEkyc.Infrastructure/Persistence/Migrations/20260815120000_Tip88C1C1ResolverAssembly.cs' = 'E9353063E47F6CA37444EF5A8740FAFB113C5189BBC845FA7B97D9CCFCFC1D3A'
    'server/src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOrchestrator.cs' = '5EDC9B2A79EA7CB98567B779A7A454BC62C478AC927C734489AE966716C235A0'
    'server/src/TagEkyc.Infrastructure/RawExport/RawExportAssemblySourceResolver.cs' = 'FA907C07D8239582ACAF984713D2607A6D14F5163157EF61DDDC4732FB8D7D87'
    'server/src/TagEkyc.Api/RawExportAssemblyHostedService.cs' = 'EAEC409E6B20702F143EC0D0A45DDAB0432B2E5B9F0A23EE7D29A09431217CAA'
}
foreach ($key in $expectedRestored.Keys) {
    if (-not $current.ContainsKey($key)) { throw "RESTORED_SOURCE_NOT_IN_MANIFEST $key" }
    Assert-Equal $expectedRestored[$key] $current[$key].sha256 "RESTORED_SOURCE_SHA $key"
}

$partitionOutput = @(& (Join-Path $doc 'a3_macro_wave_partition_verify.ps1'))
if ($partitionOutput -notcontains 'assigned_once=48' -or $partitionOutput -notcontains 'unassigned=0') { throw 'PARTITION_VERIFY_FAILED' }

$stagedServer = @(& git -C $server diff --cached --name-only).Count
$conflictedServer = @(& git -C $server ls-files -u).Count
$stagedAgent = @(& git -C $agent diff --cached --name-only).Count
$conflictedAgent = @(& git -C $agent ls-files -u).Count

"manifest_files=$($manifest.Count)"
"manifest_sha=$((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_candidate_manifest_v2.tsv')).Hash)"
"delta_added=$(@($delta.Keys | Where-Object { -not $prior.ContainsKey($_) }).Count)"
"delta_modified=$(@($delta.Keys | Where-Object { $prior.ContainsKey($_) }).Count)"
"delta_deleted=0"
"trx_total=$($inventory.Count)"
"whole_repo_w2_trx=$($actualW2Trx.Count)"
"failed_runs_total=$($failed.Count)"
"evidence_red=$(@($failed | Where-Object classification -eq 'EVIDENCE_RED').Count)"
"superseded_red=$(@($failed | Where-Object classification -eq 'SUPERSEDED_RED').Count)"
"excluded_diagnostic=$(@($failed | Where-Object classification -eq 'EXCLUDED_DIAGNOSTIC').Count)"
"unclassified_failed_runs=0"
"outside_name_family_failed=$($outside.Count)"
"mutants=$($mutantHashes.Count)"
"live_mutant_matches=$($live.Count)"
"staged_server=$stagedServer conflicted_server=$conflictedServer"
"staged_agent=$stagedAgent conflicted_agent=$conflictedAgent"
