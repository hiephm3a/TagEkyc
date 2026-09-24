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
$manifestPath = Join-Path $doc 'a3_macro_wave_w3_ratification_candidate_manifest_v1.tsv'
$sidecarPath = "$manifestPath.sha256"
$relativeDoc = 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/'
$derivedExclusions = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in @(
    'a3_macro_wave_w3_ratification_candidate_manifest_v1.tsv',
    'a3_macro_wave_w3_ratification_candidate_manifest_v1.tsv.sha256',
    'a3_macro_wave_w3_ratification_v1.md',
    'a3_activation_evidence_seal_v1.tsv'
)) { [void]$derivedExclusions.Add("server/$relativeDoc$name") }

# The manifest and the ratification record each name the other's SHA through
# the derived seal descriptor. Excluding exactly these four governance outputs
# breaks that hash cycle; every other Git-visible file and every retained TRX
# in both repositories is part of this candidate's byte inventory.
$paths = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($repo in @('server', 'agent')) {
    $visible = @(& git -C $roots[$repo] ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw "GIT_INVENTORY_FAILED repo=$repo" }
    foreach ($raw in $visible) {
        $relative = $raw.Replace('\', '/')
        $key = "$repo/$relative"
        if ($derivedExclusions.Contains($key)) { continue }
        $full = Join-Path $roots[$repo] $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (-not [IO.File]::Exists($full)) { throw "GIT_VISIBLE_FILE_MISSING path=$key" }
        $paths[$key] = $full
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx')) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\', '/')
        $key = "$repo/$relative"
        if ($derivedExclusions.Contains($key)) { throw "TRX_DERIVED_EXCLUSION_INVALID path=$key" }
        $paths[$key] = $file.FullName
    }
}

function Get-Role([string]$relative) {
    if ($relative.EndsWith('.trx', [StringComparison]::OrdinalIgnoreCase)) { return 'evidence_trx' }
    if ($relative -match '(?i)/Migrations/' -or $relative -match '(?i)\.sql$') { return 'sql_or_migration' }
    if ($relative -match '(?i)^tests/.+\.cs$') { return 'test_source' }
    if ($relative -match '(?i)^src/.+\.cs$') { return 'product_source' }
    if ($relative -match '(?i)\.(csproj|sln|props|targets)$') { return 'project' }
    if ($relative -match '(?i)\.(ps1|sh)$') { return 'script' }
    if ($relative.StartsWith('docs/', [StringComparison]::Ordinal)) { return 'authority_or_project_document' }
    return 'other_controlled'
}

$orderedKeys = [string[]]@($paths.Keys)
[Array]::Sort($orderedKeys, [StringComparer]::Ordinal)
$expected = [Collections.Generic.List[string]]::new()
$expected.Add("repo`trepo_relative_path`tfile_role`tbyte_size`tsha256")
foreach ($key in $orderedKeys) {
    $slash = $key.IndexOf('/')
    $repo = $key.Substring(0, $slash)
    $relative = $key.Substring($slash + 1)
    $file = $paths[$key]
    $size = (Get-Item -LiteralPath $file).Length
    $sha = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    $expected.Add("$repo`t$relative`t$(Get-Role $relative)`t$size`t$sha")
}
$utf8 = [Text.UTF8Encoding]::new($false)
$content = [string]::Join("`n", $expected) + "`n"
if ($Mode -eq 'Build') {
    [IO.File]::WriteAllText($manifestPath, $content, $utf8)
    $sha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    [IO.File]::WriteAllText($sidecarPath, "$sha  $([IO.Path]::GetFileName($manifestPath))`n", $utf8)
} else {
    if (-not [IO.File]::Exists($manifestPath) -or -not [IO.File]::Exists($sidecarPath)) {
        throw 'RATIFICATION_MANIFEST_MISSING'
    }
    if ([IO.File]::ReadAllText($manifestPath, $utf8) -cne $content) {
        throw 'RATIFICATION_MANIFEST_CURRENT_BYTES_DRIFT'
    }
    $sha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    if ([IO.File]::ReadAllText($sidecarPath, $utf8) -cne "$sha  $([IO.Path]::GetFileName($manifestPath))`n") {
        throw 'RATIFICATION_MANIFEST_SIDECAR_DRIFT'
    }
}

$rows = @(Import-Csv -LiteralPath $manifestPath -Delimiter "`t")
$trx = @($rows | Where-Object file_role -eq 'evidence_trx')
$w3Inventory = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_macro_wave_w3_trx_inventory_v2.tsv') -Delimiter "`t")
$manifestPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $rows) { [void]$manifestPaths.Add("$($row.repo)/$($row.repo_relative_path)") }
foreach ($row in $w3Inventory) {
    if (-not $manifestPaths.Contains($row.path)) { throw "W3_TRX_NOT_IN_RATIFICATION_MANIFEST path=$($row.path)" }
}

"manifest_files=$($rows.Count)"
"manifest_sha=$sha"
"trx_in_manifest=$($trx.Count)"
"w3_trx_in_manifest=$($w3Inventory.Count)/$($w3Inventory.Count)"
"derived_governance_exclusions=$($derivedExclusions.Count)"
"unmatched_live_files=0"
