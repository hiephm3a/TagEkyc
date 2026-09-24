param(
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent',
    [string]$CandidateManifest = ''
)

$ErrorActionPreference = 'Stop'
$roots = @{
    server = (Resolve-Path -LiteralPath $ServerRoot).Path
    agent = (Resolve-Path -LiteralPath $AgentRoot).Path
}
$docDir = Join-Path $roots.server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$baselinePath = Join-Path $docDir 'a3_assembly_baseline_manifest.tsv'
$baselineTrxPath = Join-Path $docDir 'a3_assembly_baseline_trx_inventory.tsv'
$allowlistPath = Join-Path $docDir 'a3_assembly_allowed_delta_v1.tsv'
$mutantsPath = Join-Path $docDir 'a3_preflight_recorded_mutants.tsv'
if ([string]::IsNullOrWhiteSpace($CandidateManifest)) {
    $CandidateManifest = Join-Path $docDir 'a3_assembly_candidate_manifest.tsv'
}

$baseline = @(Import-Csv -LiteralPath $baselinePath -Delimiter "`t")
$baselineTrx = @(Import-Csv -LiteralPath $baselineTrxPath -Delimiter "`t")
$candidate = @(Import-Csv -LiteralPath $CandidateManifest -Delimiter "`t")
$allowlist = @(Import-Csv -LiteralPath $allowlistPath -Delimiter "`t")
$mutants = @(Import-Csv -LiteralPath $mutantsPath -Delimiter "`t")
$selfExcluded = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($path in @(
    'server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_assembly_baseline_manifest.tsv',
    'server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_assembly_baseline_manifest.tsv.sha256',
    'server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_assembly_candidate_manifest.tsv',
    'server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_assembly_candidate_manifest.tsv.sha256'
)) { [void]$selfExcluded.Add($path) }

function Index-Manifest([object[]]$rows, [string]$label) {
    $index = @{}
    foreach ($row in $rows) {
        $key = "$($row.repo)/$($row.repo_relative_path)"
        if ($index.ContainsKey($key)) { throw "DUPLICATE_${label}_PATH $key" }
        if ($row.sha256 -notmatch '^[0-9A-F]{64}$') { throw "INVALID_${label}_SHA $key" }
        $index[$key] = $row
    }
    return $index
}

function Get-AllowlistMatch([string]$repo, [string]$path, [string]$change) {
    $matches = @($allowlist | Where-Object {
        $_.repo -ceq $repo -and $_.allowed_change -ceq $change -and $path -clike $_.path_pattern
    })
    if ($matches.Count -gt 1) { throw "AMBIGUOUS_ALLOWLIST_MATCH $repo/$path change=$change" }
    return $matches
}

$before = Index-Manifest $baseline 'BASELINE'
$after = Index-Manifest $candidate 'CANDIDATE'

# Verify candidate rows against live bytes.
foreach ($key in $after.Keys) {
    $row = $after[$key]
    $full = Join-Path $roots[$row.repo] $row.repo_relative_path
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "CANDIDATE_FILE_MISSING $key" }
    $liveSha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
    if ($liveSha -cne $row.sha256) { throw "CANDIDATE_LIVE_SHA_MISMATCH $key" }
    if ((Get-Item -LiteralPath $full).Length -ne [long]$row.byte_size) { throw "CANDIDATE_SIZE_MISMATCH $key" }
}

# Candidate coverage is required for every Git-visible file and every TRX created after the
# separately frozen all-repository baseline TRX inventory. Historical ignored TRX remain governed
# by that inventory and do not become candidate-manifest drift merely because they predate this cluster.
$missingControlled = @()
$baselineTrxSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $baselineTrx) { [void]$baselineTrxSet.Add($row.path) }
foreach ($repo in @('server','agent')) {
    $gitPaths = @(& git -C $roots[$repo] ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw "GIT_INVENTORY_FAILED $repo" }
    $required = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($path in $gitPaths) { if (-not [string]::IsNullOrWhiteSpace($path)) { [void]$required.Add($path.Replace('\','/')) } }
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx' -ErrorAction Stop)) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\','/')
        if (-not $baselineTrxSet.Contains("$repo/$relative")) { [void]$required.Add($relative) }
    }
    foreach ($path in $required) {
        $key = "$repo/$path"
        if ($selfExcluded.Contains($key)) { continue }
        if (-not $after.ContainsKey($key)) { $missingControlled += $key }
    }
}
if ($missingControlled.Count -ne 0) {
    throw "CANDIDATE_MANIFEST_OMITS_CONTROLLED count=$($missingControlled.Count) paths=$([string]::Join(',', $missingControlled))"
}

$added = @($after.Keys | Where-Object { -not $before.ContainsKey($_) } | Sort-Object)
$deleted = @($before.Keys | Where-Object { -not $after.ContainsKey($_) } | Sort-Object)
$modified = @($after.Keys | Where-Object { $before.ContainsKey($_) -and $before[$_].sha256 -cne $after[$_].sha256 } | Sort-Object)

$outsideAdded = @()
$outsideDeleted = @()
$outsideModified = @()
foreach ($key in $added) {
    $row = $after[$key]
    if ((Get-AllowlistMatch $row.repo $row.repo_relative_path 'ADDED').Count -ne 1) { $outsideAdded += $key }
}
foreach ($key in $deleted) {
    $row = $before[$key]
    if ((Get-AllowlistMatch $row.repo $row.repo_relative_path 'DELETED').Count -ne 1) { $outsideDeleted += $key }
}
foreach ($key in $modified) {
    $row = $after[$key]
    if ((Get-AllowlistMatch $row.repo $row.repo_relative_path 'MODIFIED').Count -ne 1) { $outsideModified += $key }
}

if ($outsideAdded.Count -ne 0) { throw "NEW_PATH_OUTSIDE_ALLOWLIST count=$($outsideAdded.Count) paths=$([string]::Join(',', $outsideAdded))" }
if ($outsideDeleted.Count -ne 0) { throw "DELETED_PATH_OUTSIDE_ALLOWLIST count=$($outsideDeleted.Count) paths=$([string]::Join(',', $outsideDeleted))" }
if ($outsideModified.Count -ne 0) { throw "MODIFIED_PATH_OUTSIDE_ALLOWLIST count=$($outsideModified.Count) paths=$([string]::Join(',', $outsideModified))" }

# This component-only batch has no authorized final production-source delta.
$productionMismatch = @($modified | Where-Object {
    $repo, $path = $_ -split '/', 2
    $path -like 'src/*' -or $path -like 'db/*' -or $path -like 'migrations/*'
})
if ($productionMismatch.Count -ne 0) {
    throw "RESTORED_PRODUCTION_SHA_MISMATCH count=$($productionMismatch.Count) paths=$([string]::Join(',', $productionMismatch))"
}

$mutantSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $mutants) {
    if ($row.sha256 -notmatch '^[0-9A-F]{64}$') { throw "INVALID_MUTANT_SHA $($row.sha256)" }
    [void]$mutantSet.Add($row.sha256)
}
$liveMutants = @($after.Values | Where-Object { $mutantSet.Contains($_.sha256) })
if ($liveMutants.Count -ne 0) {
    throw "LIVE_MUTANT_MATCH count=$($liveMutants.Count) paths=$([string]::Join(',', @($liveMutants | ForEach-Object { "$($_.repo)/$($_.repo_relative_path)" })))"
}

"baseline_manifest_files=$($baseline.Count)"
"candidate_manifest_files=$($candidate.Count)"
"added_paths=$($added.Count)"
"modified_paths=$($modified.Count)"
"deleted_paths=$($deleted.Count)"
"new_paths_outside_allowlist=0"
"modified_paths_outside_allowlist=0"
"deleted_paths_outside_allowlist=0"
"restored_production_sha_mismatch=0"
"recorded_mutants=$($mutantSet.Count)"
"live_mutant_matches=0"
"manifest_drift=0"
