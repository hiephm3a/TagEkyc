param(
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$roots = @{
    server = (Resolve-Path -LiteralPath $ServerRoot).Path
    agent = (Resolve-Path -LiteralPath $AgentRoot).Path
}
$docDir = Join-Path $roots.server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$inventory = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_r2r6_trx_inventory.tsv') -Delimiter "`t")
$failedCensus = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_r2r6_failed_run_census.tsv') -Delimiter "`t")
$manifest = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_r2r6_cluster_final_candidate_manifest.tsv') -Delimiter "`t")
$references = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_preflight_trx_reference_resolution.tsv') -Delimiter "`t")
$mutants = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_preflight_recorded_mutants.tsv') -Delimiter "`t")

function Assert-Equal($expected, $actual, [string]$name) {
    if ($expected -ne $actual) { throw "$name expected=$expected actual=$actual" }
}

$found = @{}
foreach ($repo in @('server', 'agent')) {
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx' -ErrorAction Stop)) {
        if ($file.FullName -match '[\/](?:\.git|bin|obj|cache)[\/]') { continue }
        if ($file.Name -notmatch '(?i)r2.?r6') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\', '/')
        $key = "$repo/$relative"
        if ($found.ContainsKey($key)) { throw "DUPLICATE_CLUSTER_TRX $key" }
        [xml]$trx = Get-Content -LiteralPath $file.FullName -Raw
        $found[$key] = [pscustomobject]@{ file = $file; trx = $trx }
    }
}

$inventoryByPath = @{}
foreach ($row in $inventory) {
    if ($inventoryByPath.ContainsKey($row.path)) { throw "DUPLICATE_INVENTORY_PATH $($row.path)" }
    if (-not $found.ContainsKey($row.path)) { throw "INVENTORY_TRX_MISSING $($row.path)" }
    $inventoryByPath[$row.path] = $row
    $item = $found[$row.path]
    $counters = $item.trx.TestRun.ResultSummary.Counters
    Assert-Equal $item.trx.TestRun.ResultSummary.outcome $row.result_summary "INVENTORY_OUTCOME $($row.path)"
    Assert-Equal (Get-FileHash -LiteralPath $item.file.FullName -Algorithm SHA256).Hash $row.sha256 "INVENTORY_SHA $($row.path)"
    foreach ($name in @('total', 'passed', 'failed')) {
        Assert-Equal ([int]$counters.$name) ([int]$row.$name) "INVENTORY_$name $($row.path)"
    }
    Assert-Equal ([int]$counters.notExecuted) ([int]$row.skipped) "INVENTORY_skipped $($row.path)"
}
Assert-Equal $found.Count $inventory.Count 'CLUSTER_TRX_INVENTORY_COUNT'

$censusByPath = @{}
$ids = @{}
$classes = @('EVIDENCE_RED', 'SUPERSEDED_RED', 'EXCLUDED_DIAGNOSTIC')
foreach ($row in $failedCensus) {
    if ($censusByPath.ContainsKey($row.path)) { throw "DUPLICATE_FAILED_PATH $($row.path)" }
    if ($ids.ContainsKey($row.id)) { throw "DUPLICATE_FAILED_ID $($row.id)" }
    if (-not $found.ContainsKey($row.path)) { throw "FAILED_TRX_MISSING $($row.path)" }
    if ($row.classification -notin $classes) { throw "UNCLASSIFIED_FAILED_RUN $($row.path)" }
    $censusByPath[$row.path] = $row
    $ids[$row.id] = $true
    $item = $found[$row.path]
    $trx = $item.trx
    $counters = $trx.TestRun.ResultSummary.Counters
    Assert-Equal 'Failed' $trx.TestRun.ResultSummary.outcome "FAILED_OUTCOME $($row.path)"
    Assert-Equal (Get-FileHash -LiteralPath $item.file.FullName -Algorithm SHA256).Hash $row.sha256 "FAILED_SHA $($row.path)"
    foreach ($name in @('total', 'passed', 'failed')) {
        Assert-Equal ([int]$counters.$name) ([int]$row.$name) "FAILED_$name $($row.path)"
    }
    Assert-Equal ([int]$counters.notExecuted) ([int]$row.skipped) "FAILED_skipped $($row.path)"
    $bad = @($trx.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed')
    $names = [string]::Join('; ', @($bad | ForEach-Object testName))
    Assert-Equal $names $row.failing_tests "FAILED_TEST_IDENTITY $($row.path)"
    $message = [regex]::Replace([string]$bad[0].Output.ErrorInfo.Message, '\s+', ' ').Trim()
    $prefix = $message.Substring(0, [Math]::Min(500, $message.Length))
    Assert-Equal $prefix $row.first_assertion_or_exception "FAILED_FIRST_ERROR $($row.path)"
    Assert-Equal $row.id $inventoryByPath[$row.path].failed_census_id "FAILED_INVENTORY_LINK $($row.path)"
    if ([string]::IsNullOrWhiteSpace($row.locator)) { throw "FAILED_LOCATOR_MISSING $($row.path)" }
    switch ($row.classification) {
        'EVIDENCE_RED' {
            if ([string]::IsNullOrWhiteSpace($row.guard_or_row)) { throw "EVIDENCE_GUARD_MISSING $($row.path)" }
        }
        'SUPERSEDED_RED' {
            if ([string]::IsNullOrWhiteSpace($row.successor_trx)) { throw "SUCCESSOR_MISSING $($row.path)" }
            $successor = @($failedCensus | Where-Object { [IO.Path]::GetFileName($_.path) -eq $row.successor_trx })
            Assert-Equal 1 $successor.Count "SUCCESSOR_NOT_UNIQUE $($row.path)"
            Assert-Equal 'EVIDENCE_RED' $successor[0].classification "SUCCESSOR_NOT_EVIDENCE_RED $($row.path)"
        }
        'EXCLUDED_DIAGNOSTIC' {
            if ([string]::IsNullOrWhiteSpace($row.exclusion_reason)) { throw "EXCLUSION_REASON_MISSING $($row.path)" }
        }
    }
}

$actualFailed = @($found.GetEnumerator() | Where-Object { $_.Value.trx.TestRun.ResultSummary.outcome -eq 'Failed' })
foreach ($entry in $actualFailed) {
    if (-not $censusByPath.ContainsKey($entry.Key)) { throw "UNCLASSIFIED_FAILED_TRX $($entry.Key)" }
}
Assert-Equal $actualFailed.Count $failedCensus.Count 'FAILED_RUN_CENSUS_COUNT'
foreach ($row in $inventory) {
    if ($row.result_summary -ne 'Failed' -and $row.failed_census_id) { throw "NONFAILED_CENSUS_LINK $($row.path)" }
}

$manifestPaths = @{}
foreach ($row in $manifest) { $manifestPaths["$($row.repo)/$($row.repo_relative_path)"] = $row }
$resolutionNames = @{}
foreach ($row in $references) {
    if ($row.disposition -eq 'EXISTING') { $resolutionNames[$row.reference_name] = $true }
}
foreach ($row in $inventory) {
    if (-not $manifestPaths.ContainsKey($row.path)) { throw "CLUSTER_TRX_NOT_IN_MANIFEST $($row.path)" }
    if (-not $resolutionNames.ContainsKey([IO.Path]::GetFileName($row.path))) {
        throw "CLUSTER_TRX_NOT_IN_REFERENCE_INVENTORY $($row.path)"
    }
}

$mutantHashes = @{}
foreach ($row in $mutants) {
    if ($row.sha256 -notmatch '^[0-9A-Fa-f]{64}$') { throw "BAD_MUTANT_SHA $($row.sha256)" }
    if ($mutantHashes.ContainsKey($row.sha256)) { throw "DUPLICATE_MUTANT_SHA $($row.sha256)" }
    $mutantHashes[$row.sha256] = $true
}
$liveMutants = @($manifest | Where-Object { $mutantHashes.ContainsKey($_.sha256) })

$stagedServer = @(& git -C $roots.server diff --cached --name-only).Count
if ($LASTEXITCODE -ne 0) { throw 'GIT_SERVER_STAGED_CHECK_FAILED' }
$conflictedServer = @(& git -C $roots.server ls-files -u).Count
if ($LASTEXITCODE -ne 0) { throw 'GIT_SERVER_CONFLICT_CHECK_FAILED' }
$stagedAgent = @(& git -C $roots.agent diff --cached --name-only).Count
if ($LASTEXITCODE -ne 0) { throw 'GIT_AGENT_STAGED_CHECK_FAILED' }
$conflictedAgent = @(& git -C $roots.agent ls-files -u).Count
if ($LASTEXITCODE -ne 0) { throw 'GIT_AGENT_CONFLICT_CHECK_FAILED' }

"cluster_trx_total=$($found.Count)"
"failed_runs_total=$($actualFailed.Count)"
"evidence_red=$(@($failedCensus | Where-Object classification -eq 'EVIDENCE_RED').Count)"
"superseded_red=$(@($failedCensus | Where-Object classification -eq 'SUPERSEDED_RED').Count)"
"excluded_diagnostic=$(@($failedCensus | Where-Object classification -eq 'EXCLUDED_DIAGNOSTIC').Count)"
"classified_failed_runs=$($failedCensus.Count)"
"unclassified_failed_runs=0"
"failed_runs_in_manifest=$(@($failedCensus | Where-Object { $manifestPaths.ContainsKey($_.path) }).Count)"
"missing_failed_trx=0"
"live_mutant_matches=$($liveMutants.Count)"
"staged_server=$stagedServer conflicted_server=$conflictedServer"
"staged_agent=$stagedAgent conflicted_agent=$conflictedAgent"
