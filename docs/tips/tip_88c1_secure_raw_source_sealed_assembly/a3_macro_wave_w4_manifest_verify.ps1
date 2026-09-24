param(
    [ValidateSet('Build', 'Verify')][string]$Mode = 'Verify',
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$roots = @{
    server = (Resolve-Path -LiteralPath $ServerRoot).Path
    agent = (Resolve-Path -LiteralPath $AgentRoot).Path
}
$doc = Join-Path $roots.server 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly'
$manifestPath = Join-Path $doc 'a3_macro_wave_w4_candidate_manifest_v1.tsv'
$sidecarPath = "$manifestPath.sha256"
$excluded = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in @(
    'a3_macro_wave_w4_candidate_manifest_v1.tsv',
    'a3_macro_wave_w4_candidate_manifest_v1.tsv.sha256',
    'a3_macro_wave_w4_review_packet_v1.md'
)) {
    [void]$excluded.Add("server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/$name")
}

$paths = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($repo in @('server', 'agent')) {
    $visible = @(& git -C $roots[$repo] ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw "GIT_INVENTORY_FAILED repo=$repo" }
    foreach ($raw in $visible) {
        $relative = $raw.Replace('\', '/')
        $key = "$repo/$relative"
        if ($excluded.Contains($key)) { continue }
        $full = Join-Path $roots[$repo] $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (-not [IO.File]::Exists($full)) { throw "GIT_VISIBLE_FILE_MISSING path=$key" }
        $paths[$key] = $full
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx')) {
        if ($file.FullName -match '[\/](?:\.git|bin|obj|cache)[\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\', '/')
        $key = "$repo/$relative"
        if ($excluded.Contains($key)) { throw "TRX_EXCLUDED path=$key" }
        $paths[$key] = $file.FullName
    }
}

function Role([string]$relative) {
    if ($relative.EndsWith('.trx', [StringComparison]::OrdinalIgnoreCase)) { return 'evidence_trx' }
    if ($relative -match '(?i)/Migrations/' -or $relative -match '(?i)\.sql$') { return 'sql_or_migration' }
    if ($relative -match '(?i)^tests/.+\.cs$') { return 'test_source' }
    if ($relative -match '(?i)^src/.+\.cs$') { return 'product_source' }
    if ($relative -match '(?i)\.(csproj|sln|props|targets)$') { return 'project' }
    if ($relative -match '(?i)\.(ps1|sh)$') { return 'script' }
    if ($relative.StartsWith('docs/', [StringComparison]::Ordinal)) { return 'authority_or_project_document' }
    return 'other_controlled'
}

$keys = [string[]]@($paths.Keys)
[Array]::Sort($keys, [StringComparer]::Ordinal)
$lines = [Collections.Generic.List[string]]::new()
$lines.Add("repo`trepo_relative_path`tfile_role`tbyte_size`tsha256")
foreach ($key in $keys) {
    $slash = $key.IndexOf('/')
    $repo = $key.Substring(0, $slash)
    $relative = $key.Substring($slash + 1)
    $full = $paths[$key]
    $size = (Get-Item -LiteralPath $full).Length
    $sha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
    $lines.Add("$repo`t$relative`t$(Role $relative)`t$size`t$sha")
}
$utf8 = [Text.UTF8Encoding]::new($false)
$content = [string]::Join("`n", $lines) + "`n"
if ($Mode -eq 'Build') {
    [IO.File]::WriteAllText($manifestPath, $content, $utf8)
    $sha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    [IO.File]::WriteAllText($sidecarPath, "$sha  $([IO.Path]::GetFileName($manifestPath))`n", $utf8)
} else {
    if (-not [IO.File]::Exists($manifestPath) -or -not [IO.File]::Exists($sidecarPath)) {
        throw 'W4_MANIFEST_MISSING'
    }
    if ([IO.File]::ReadAllText($manifestPath, $utf8) -cne $content) {
        throw 'W4_MANIFEST_CURRENT_BYTES_DRIFT'
    }
    $sha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    if ([IO.File]::ReadAllText($sidecarPath, $utf8) -cne "$sha  $([IO.Path]::GetFileName($manifestPath))`n") {
        throw 'W4_MANIFEST_SIDECAR_DRIFT'
    }
}

$rows = @(Import-Csv -LiteralPath $manifestPath -Delimiter "`t")
$manifestIndex = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $rows) { [void]$manifestIndex.Add("$($row.repo)/$($row.repo_relative_path)") }
$w4Trx = @($paths.Keys | Where-Object { $_ -match '/a3-w4[^/]*\.trx$' })
$w4DirectoryTrx = @($paths.Keys | Where-Object { $_ -match '(?i)/TestResults/a3-w4/[^/]+\.trx$' })
$outsideNameFamily = @($w4DirectoryTrx | Where-Object { $_ -notin $w4Trx })
if ($outsideNameFamily.Count -gt 0) {
    throw "W4_OUTSIDE_NAME_FAMILY_TRX count=$($outsideNameFamily.Count) paths=$($outsideNameFamily -join ',')"
}
$failed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($key in $w4Trx) {
    if (-not $manifestIndex.Contains($key)) { throw "W4_TRX_NOT_IN_MANIFEST path=$key" }
    $xml = [xml][IO.File]::ReadAllText($paths[$key])
    if ([int]$xml.TestRun.ResultSummary.Counters.failed -gt 0) { [void]$failed.Add($key) }
}
$census = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_macro_wave_w4_failed_run_census_v1.tsv') -Delimiter "`t")
$censusIndex = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $census) {
    if (-not $censusIndex.Add($entry.path)) { throw "W4_FAILED_CENSUS_DUPLICATE path=$($entry.path)" }
    if (-not $failed.Contains($entry.path)) { throw "W4_CENSUS_NOT_FAILED path=$($entry.path)" }
    if (-not $manifestIndex.Contains($entry.path)) { throw "W4_FAILED_TRX_NOT_IN_MANIFEST path=$($entry.path)" }
    $fileSha = (Get-FileHash -LiteralPath $paths[$entry.path] -Algorithm SHA256).Hash
    if ($entry.sha256 -cne $fileSha) { throw "W4_FAILED_TRX_SHA_MISMATCH path=$($entry.path)" }
}
foreach ($key in $failed) {
    if (-not $censusIndex.Contains($key)) { throw "W4_FAILED_TRX_UNCLASSIFIED path=$key" }
}

$inventory = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_macro_wave_w4_trx_inventory_v1.tsv') -Delimiter "`t")
$inventoryIndex = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $inventory) {
    if (-not $inventoryIndex.Add($entry.path)) { throw "W4_TRX_INVENTORY_DUPLICATE path=$($entry.path)" }
    if (-not $manifestIndex.Contains($entry.path)) { throw "W4_TRX_INVENTORY_NOT_IN_MANIFEST path=$($entry.path)" }
    $fileSha = (Get-FileHash -LiteralPath $paths[$entry.path] -Algorithm SHA256).Hash
    if ($entry.sha256 -cne $fileSha) { throw "W4_TRX_INVENTORY_SHA_MISMATCH path=$($entry.path)" }
    $xml = [xml][IO.File]::ReadAllText($paths[$entry.path])
    $c = $xml.TestRun.ResultSummary.Counters
    if ([int]$entry.total -ne [int]$c.total -or [int]$entry.passed -ne [int]$c.passed -or
        [int]$entry.failed -ne [int]$c.failed -or [int]$entry.skipped -ne [int]$c.notExecuted) {
        throw "W4_TRX_INVENTORY_COUNTER_MISMATCH path=$($entry.path)"
    }
}
foreach ($key in $w4Trx) {
    if (-not $inventoryIndex.Contains($key)) { throw "W4_TRX_OMITTED_FROM_INVENTORY path=$key" }
}

$predecessor = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_macro_wave_w3_ratification_candidate_manifest_v1.tsv') -Delimiter "`t")
$predecessorIndex = @{}
foreach ($entry in $predecessor) { $predecessorIndex["$($entry.repo)/$($entry.repo_relative_path)"] = $entry.sha256 }
$currentIndex = @{}
foreach ($entry in $rows) { $currentIndex["$($entry.repo)/$($entry.repo_relative_path)"] = $entry.sha256 }
$observed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($key in $currentIndex.Keys) {
    if (-not $predecessorIndex.ContainsKey($key)) { [void]$observed.Add("ADDED`t$key") }
    elseif ($predecessorIndex[$key] -cne $currentIndex[$key]) { [void]$observed.Add("MODIFIED`t$key") }
}
foreach ($key in $predecessorIndex.Keys) {
    if (-not $currentIndex.ContainsKey($key)) { [void]$observed.Add("DELETED`t$key") }
}
$allowed = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_macro_wave_w4_allowed_delta_v1.tsv') -Delimiter "`t")
$allowedIndex = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $allowed) {
    if (-not $allowedIndex.Add("$($entry.change)`t$($entry.path)")) { throw "W4_ALLOWED_DELTA_DUPLICATE path=$($entry.path)" }
    if ([string]::IsNullOrWhiteSpace($entry.reason)) { throw "W4_ALLOWED_DELTA_REASON_MISSING path=$($entry.path)" }
}
foreach ($entry in $observed) { if (-not $allowedIndex.Contains($entry)) { throw "W4_UNDECLARED_DELTA change_path=$entry" } }
foreach ($entry in $allowedIndex) { if (-not $observed.Contains($entry)) { throw "W4_ALLOWED_DELTA_NOT_OBSERVED change_path=$entry" } }

$mutants = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_preflight_recorded_mutants.tsv') -Delimiter "`t")
$mutantIndex = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($mutant in $mutants) {
    if (-not $mutantIndex.Add($mutant.sha256)) { throw "RECORDED_MUTANT_DUPLICATE sha=$($mutant.sha256)" }
}
$liveMutants = @($rows | Where-Object {
    $_.file_role -in @('product_source', 'test_source') -and $mutantIndex.Contains($_.sha256)
})
if ($liveMutants.Count -gt 0) { throw "LIVE_MUTANT_MATCHES count=$($liveMutants.Count)" }

"manifest_files=$($rows.Count)"
"manifest_sha=$sha"
"w4_trx_in_manifest=$($w4Trx.Count)/$($w4Trx.Count)"
"outside_name_family_failed=0"
"w4_trx_inventory=$($inventory.Count)/$($w4Trx.Count)"
"observed_delta=$($observed.Count) allowed_delta=$($allowed.Count) outside_allowlist=0"
"w4_failed_runs=$($failed.Count)"
"classified_failed_runs=$($census.Count)"
"unclassified_failed_runs=0"
"mutants=$($mutants.Count)"
"live_mutant_matches=$($liveMutants.Count)"
foreach ($repo in @('server', 'agent')) {
    $staged = @(& git -C $roots[$repo] diff --cached --name-only 2>$null)
    $conflicted = @(& git -C $roots[$repo] diff --name-only --diff-filter=U 2>$null)
    "staged_$repo=$($staged.Count) conflicted_$repo=$($conflicted.Count)"
}
