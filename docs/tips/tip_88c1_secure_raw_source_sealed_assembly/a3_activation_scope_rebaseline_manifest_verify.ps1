param(
    [ValidateSet('Build','Verify')][string]$Mode = 'Verify',
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent',
    [string]$ManifestName = 'a3_activation_scope_rebaseline_candidate_manifest_v1.tsv',
    [string]$RatificationRecordName = 'a3_activation_scope_rebaseline_ratification_v1.md',
    [string]$ScopeDecisionName = 'a3_activation_scope_rebaseline_decision_v1.md',
    [string[]]$AdditionalRequiredTrxNames = @()
)

$ErrorActionPreference = 'Stop'
$roots = @{
    server = (Resolve-Path -LiteralPath $ServerRoot).Path
    agent = (Resolve-Path -LiteralPath $AgentRoot).Path
}
$doc = Join-Path $roots.server 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly'
$manifestName = $ManifestName
$manifestPath = Join-Path $doc $manifestName
$sidecarPath = "$manifestPath.sha256"
$relativeDoc = 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/'
$derivedExclusions = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in @(
    $manifestName,
    "$manifestName.sha256",
    $RatificationRecordName,
    'a3_activation_evidence_seal_v1.tsv'
)) { [void]$derivedExclusions.Add("server/$relativeDoc$name") }

function Get-GitNormalizedEvidence([string]$repositoryRoot, [string]$relativePath, [string]$fullPath) {
    # Hash the exact blob content selected for the next commit. For tracked
    # paths this is the index object, so unrelated unstaged checkout changes
    # cannot leak into a governance manifest. New untracked paths use the blob
    # Git would retain after applying attributes. Working-tree bytes are not a
    # portable authority on Windows because checkout line endings may differ.
    $objectIdOutput = & git -C $repositoryRoot rev-parse --verify ":$relativePath" 2>$null
    $objectId = if ($null -eq $objectIdOutput) { '' } else { "$objectIdOutput".Trim() }
    if ($LASTEXITCODE -ne 0 -or $objectId -notmatch '^[0-9a-f]{40,64}$') {
        $objectId = (& git -C $repositoryRoot hash-object -w "--path=$relativePath" -- $fullPath 2>$null).Trim()
    }
    if ($LASTEXITCODE -ne 0 -or $objectId -notmatch '^[0-9a-f]{40,64}$') {
        throw "ACTIVATION_SCOPE_GIT_BLOB_CREATE_FAILED path=$relativePath"
    }

    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'git'
    $start.WorkingDirectory = $repositoryRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = 'cat-file blob "' + $objectId + '"'
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
        if ($process.ExitCode -ne 0) {
            throw "ACTIVATION_SCOPE_GIT_BLOB_READ_FAILED path=$relativePath error=$errorText"
        }
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

$paths = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
foreach ($repo in @('server','agent')) {
    $visible = @(& git -C $roots[$repo] ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw "ACTIVATION_SCOPE_GIT_INVENTORY_FAILED repo=$repo" }
    foreach ($raw in $visible) {
        $relative = $raw.Replace('\','/')
        $key = "$repo/$relative"
        if ($derivedExclusions.Contains($key)) { continue }
        $full = Join-Path $roots[$repo] $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (-not [IO.File]::Exists($full)) { throw "ACTIVATION_SCOPE_VISIBLE_FILE_MISSING path=$key" }
        $paths[$key] = $full
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx')) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\','/')
        $key = "$repo/$relative"
        if ($derivedExclusions.Contains($key)) { throw "ACTIVATION_SCOPE_TRX_EXCLUSION_INVALID path=$key" }
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
$lines = [Collections.Generic.List[string]]::new()
$lines.Add("repo`trepo_relative_path`tfile_role`tbyte_size`tsha256")
foreach ($key in $orderedKeys) {
    $slash = $key.IndexOf('/')
    $repo = $key.Substring(0, $slash)
    $relative = $key.Substring($slash + 1)
    $file = $paths[$key]
    $evidence = Get-GitNormalizedEvidence $roots[$repo] $relative $file
    $lines.Add("$repo`t$relative`t$(Get-Role $relative)`t$($evidence.Bytes)`t$($evidence.Sha256)")
}
$utf8 = [Text.UTF8Encoding]::new($false)
$content = [string]::Join("`n", $lines) + "`n"
if ($Mode -ceq 'Build') {
    [IO.File]::WriteAllText($manifestPath, $content, $utf8)
    $sha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    [IO.File]::WriteAllText($sidecarPath, "$sha  $manifestName`n", $utf8)
} else {
    if (-not [IO.File]::Exists($manifestPath) -or -not [IO.File]::Exists($sidecarPath) -or
        [IO.File]::ReadAllText($manifestPath, $utf8) -cne $content) {
        throw 'ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT'
    }
    $sha = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    if ([IO.File]::ReadAllText($sidecarPath, $utf8) -cne "$sha  $manifestName`n") {
        throw 'ACTIVATION_SCOPE_MANIFEST_SIDECAR_DRIFT'
    }
}

$rows = @(Import-Csv -LiteralPath $manifestPath -Delimiter "`t")
$manifestKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $rows) { [void]$manifestKeys.Add("$($row.repo)/$($row.repo_relative_path)") }
$required = @(
    "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/$ScopeDecisionName",
    'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_outcome_ownership_table_v1.tsv',
    'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_scope_partition_v1.tsv',
    'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_proof_reconciliation_v0_2.md',
    'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_scope_deferred_backlog_v1.tsv'
)
foreach ($path in $required) {
    if (-not $manifestKeys.Contains("server/$path")) { throw "ACTIVATION_SCOPE_MANIFEST_REQUIRED_MISSING path=$path" }
}
$trxNames = @(
    'a3-scope-topology-baseline-unit.trx', 'a3-scope-topology-baseline-host.trx',
    'a3-scope-topology-guard-mutant-unit.trx', 'a3-scope-topology-guard-mutant-host.trx',
    'a3-scope-topology-restored-unit.trx', 'a3-scope-topology-restored-host.trx',
    'a3-scope-topology-generic-approved-unit.trx', 'a3-scope-topology-generic-approved-host.trx',
    'a3-activation-scope-generated-seal-host.trx'
) + $AdditionalRequiredTrxNames
foreach ($name in $trxNames) {
    if (@($rows | Where-Object { $_.file_role -ceq 'evidence_trx' -and
            $_.repo_relative_path.EndsWith("/$name", [StringComparison]::Ordinal) }).Count -ne 1) {
        throw "ACTIVATION_SCOPE_TRX_INVENTORY_INVALID name=$name"
    }
}
$registry = @(Import-Csv -LiteralPath (Join-Path $doc 'a3_preflight_recorded_mutants.tsv') -Delimiter "`t")
$mutantHashes = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($mutant in $registry) {
    if ($mutant.sha256 -cnotmatch '^[0-9A-F]{64}$' -or -not $mutantHashes.Add($mutant.sha256)) {
        throw "ACTIVATION_SCOPE_MUTANT_REGISTRY_INVALID hash=$($mutant.sha256)"
    }
}
$liveMutants = @($rows | Where-Object { $mutantHashes.Contains($_.sha256) })
if ($liveMutants.Count -ne 0) {
    throw "ACTIVATION_SCOPE_LIVE_MUTANT_MATCHES count=$($liveMutants.Count)"
}
"manifest_files=$($rows.Count)"
"manifest_sha=$sha"
"trx_in_manifest=$(@($rows | Where-Object file_role -eq 'evidence_trx').Count)"
"scope_trx_in_manifest=$($trxNames.Count)/$($trxNames.Count)"
"mutants_distinct=$($mutantHashes.Count)"
"live_mutant_matches=$($liveMutants.Count)"
"derived_governance_exclusions=$($derivedExclusions.Count)"
"unmatched_live_files=0"
