param(
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$roots = @{ server = (Resolve-Path -LiteralPath $ServerRoot).Path; agent = (Resolve-Path -LiteralPath $AgentRoot).Path }
$docDir = Join-Path $roots.server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$baseline = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_assembly_baseline_trx_inventory.tsv') -Delimiter "`t")
$census = @(Import-Csv -LiteralPath (Join-Path $docDir 'a3_assembly_failed_run_census.tsv') -Delimiter "`t")

$before = @{}
foreach ($row in $baseline) {
    if ($before.ContainsKey($row.path)) { throw "DUPLICATE_BASELINE_TRX $($row.path)" }
    $before[$row.path] = $row.sha256
}

$after = @{}
foreach ($repo in @('server', 'agent')) {
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx' -ErrorAction Stop)) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\', '/')
        $path = "$repo/$relative"
        if ($after.ContainsKey($path)) { throw "DUPLICATE_LIVE_TRX $path" }
        $after[$path] = [pscustomobject]@{
            file = $file
            sha = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        }
    }
}

foreach ($path in $before.Keys) {
    if (-not $after.ContainsKey($path)) { throw "BASELINE_TRX_MISSING $path" }
    if ($after[$path].sha -cne $before[$path]) { throw "BASELINE_TRX_DRIFT $path" }
}

$new = @($after.Keys | Where-Object { -not $before.ContainsKey($_) } | Sort-Object)
$failed = @{}
$outside = @()
foreach ($path in $new) {
    [xml]$trx = Get-Content -LiteralPath $after[$path].file.FullName -Raw
    if ($trx.TestRun.ResultSummary.outcome -ne 'Failed') { continue }
    $failed[$path] = [pscustomobject]@{ trx = $trx; sha = $after[$path].sha }
    if ([IO.Path]::GetFileName($path) -notmatch '^a3-assembly-[A-Za-z0-9._-]+\.trx$') {
        $outside += $path
    }
}

$classified = @{}
$censusIds = @{}
foreach ($row in $census) {
    if ([string]::IsNullOrWhiteSpace($row.id)) { throw "FAILED_CENSUS_ID_MISSING $($row.path)" }
    if ($censusIds.ContainsKey($row.id)) { throw "DUPLICATE_FAILED_CENSUS_ID $($row.id)" }
    $censusIds[$row.id] = $row.path
}

foreach ($row in $census) {
    if ($classified.ContainsKey($row.path)) { throw "DUPLICATE_FAILED_CENSUS_PATH $($row.path)" }
    if (-not $failed.ContainsKey($row.path)) { throw "CENSUS_NOT_NEW_FAILED_TRX $($row.path)" }
    if ($row.classification -notin @('EVIDENCE_RED','SUPERSEDED_RED','EXCLUDED_DIAGNOSTIC')) {
        throw "UNCLASSIFIED_FAILED_RUN $($row.path)"
    }
    if ($row.sha256 -cne $failed[$row.path].sha) { throw "FAILED_SHA_MISMATCH $($row.path)" }

    $trx = $failed[$row.path].trx
    $counters = $trx.TestRun.ResultSummary.Counters
    foreach ($field in @('total','passed','failed')) {
        if ([int]$row.$field -ne [int]$counters.$field) { throw "FAILED_COUNTER_MISMATCH $field $($row.path)" }
    }
    if ([int]$row.skipped -ne [int]$counters.notExecuted) { throw "FAILED_SKIP_MISMATCH $($row.path)" }

    $bad = @($trx.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed')
    $names = [string]::Join('; ', @($bad | ForEach-Object testName))
    if ($row.failing_tests -cne $names) { throw "FAILED_TEST_MISMATCH $($row.path)" }
    $message = [regex]::Replace([string]$bad[0].Output.ErrorInfo.Message, '\s+', ' ').Trim()
    $prefix = $message.Substring(0, [Math]::Min(500, $message.Length))
    if ($row.first_assertion_or_exception -cne $prefix) { throw "FAILED_ERROR_MISMATCH $($row.path)" }
    if ([string]::IsNullOrWhiteSpace($row.locator)) { throw "FAILED_LOCATOR_MISSING $($row.path)" }

    if ($row.classification -eq 'EVIDENCE_RED' -and [string]::IsNullOrWhiteSpace($row.guard_or_row)) {
        throw "EVIDENCE_GUARD_MISSING $($row.path)"
    }
    if ($row.classification -eq 'SUPERSEDED_RED') {
        if ([string]::IsNullOrWhiteSpace($row.successor_trx)) { throw "SUCCESSOR_MISSING $($row.path)" }
        $successors = @($census | Where-Object {
            $_.path -ceq $row.successor_trx -or
            [IO.Path]::GetFileName($_.path) -ceq [IO.Path]::GetFileName($row.successor_trx)
        })
        if ($successors.Count -ne 1) {
            throw "SUCCESSOR_NOT_UNIQUE count=$($successors.Count) path=$($row.path) successor=$($row.successor_trx)"
        }
        if ($successors[0].classification -cne 'EVIDENCE_RED') {
            throw "SUCCESSOR_NOT_EVIDENCE_RED $($row.path) successor=$($successors[0].path)"
        }
    }
    if ($row.classification -eq 'EXCLUDED_DIAGNOSTIC' -and [string]::IsNullOrWhiteSpace($row.exclusion_reason)) {
        throw "EXCLUSION_REASON_MISSING $($row.path)"
    }
    $classified[$row.path] = $true
}

foreach ($path in $failed.Keys) {
    if (-not $classified.ContainsKey($path)) { throw "FAILED_RUN_NOT_CLASSIFIED $path" }
}
if ($outside.Count -ne 0) {
    throw "OUTSIDE_NAME_FAMILY_FAILED count=$($outside.Count) paths=$([string]::Join(',', $outside))"
}

"baseline_trx_total=$($baseline.Count)"
"new_trx_total=$($new.Count)"
"failed_runs_total=$($failed.Count)"
"classified_failed_runs=$($classified.Count)"
"unclassified_failed_runs=0"
"outside_name_family_failed=$($outside.Count)"
"baseline_trx_drift=0"
