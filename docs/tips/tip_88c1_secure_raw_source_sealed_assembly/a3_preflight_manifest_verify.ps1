param(
    [ValidateSet('Build', 'Verify')]
    [string]$Mode = 'Verify',
    [string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc',
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent',
    [string]$ManifestPath = 'D:\Task\Remote Signing\TagEkyc\docs\tips\tip_88c1_secure_raw_source_sealed_assembly\a3_preflight_baseline_manifest.tsv'
)

$ErrorActionPreference = 'Stop'
$roots = @{ server = (Resolve-Path -LiteralPath $ServerRoot).Path; agent = (Resolve-Path -LiteralPath $AgentRoot).Path }
$docDir = Join-Path $roots.server 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$ledgers = @(
    (Join-Path $docDir 'tip_88c1_c6b_a3_proof_reconciliation_v0_2.md'),
    (Join-Path $docDir 'tip_88c1_c6b_a3_implementation_ledger.md'),
    (Join-Path $docDir 'a3_r2r6_cluster_execution_matrix_v1.md'),
    (Join-Path $docDir 'a3_r2r6_trx_inventory.tsv'),
    (Join-Path $docDir 'a3_r2r6_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_broker_claim_cluster_execution_matrix_v1.md'),
    (Join-Path $docDir 'a3_broker_claim_trx_inventory.tsv'),
    (Join-Path $docDir 'a3_broker_claim_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_broker_claim_cluster_review_packet_v1.md'),
    (Join-Path $docDir 'a3_assembly_cluster_execution_matrix_v1.md'),
    (Join-Path $docDir 'a3_assembly_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_assembly_cluster_review_packet_v1.md'),
    (Join-Path $docDir 'a3_broker_admission_cluster_execution_matrix_v1.md'),
    (Join-Path $docDir 'a3_broker_admission_cluster_execution_matrix_v2.md'),
    (Join-Path $docDir 'a3_broker_admission_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_broker_admission_cluster_review_packet_v1.md'),
    (Join-Path $docDir 'a3_broker_admission_cluster_review_packet_v2.md'),
    (Join-Path $docDir 'a3_broker_admission_cluster_review_packet_v3.md'),
    (Join-Path $docDir 'a3_r2r6_remaining_cluster_execution_matrix_v2.md'),
    (Join-Path $docDir 'a3_r2r6_remaining_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_r2r6_remaining_cluster_review_packet_v2.md'),
    (Join-Path $docDir 'a3_r2r6_remaining_cluster_review_packet_v3.md'),
    (Join-Path $docDir 'a3_r2r6_remaining_cluster_ratification_v1.md'),
    (Join-Path $docDir 'a3_macro_wave_w1_r2r6_trx_inventory.tsv'),
    (Join-Path $docDir 'a3_macro_wave_w1_r2r6_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_macro_wave_w1_r2r6_review_packet_v1.md'),
    (Join-Path $docDir 'a3_macro_wave_w2_trx_inventory.tsv'),
    (Join-Path $docDir 'a3_macro_wave_w2_failed_run_census.tsv'),
    (Join-Path $docDir 'a3_macro_wave_w2_review_packet_v1.md'),
    (Join-Path $docDir 'a3_macro_wave_w2_trx_inventory_v2.tsv'),
    (Join-Path $docDir 'a3_macro_wave_w2_failed_run_census_v2.tsv'),
    (Join-Path $docDir 'a3_macro_wave_w2_review_packet_v2.md'),
    (Join-Path $docDir 'a3_macro_wave_w2_homeowner_scope_decision_v1.md')
)
$utf8 = [Text.UTF8Encoding]::new($false)
$compare = [StringComparer]::Ordinal
$caseInsensitive = [StringComparer]::OrdinalIgnoreCase
$trxIndex = @{}
foreach ($repo in @('server', 'agent')) {
    foreach ($file in @(Get-ChildItem -LiteralPath $roots[$repo] -Recurse -File -Filter '*.trx' -ErrorAction Stop)) {
        if ($file.FullName -match '[\\/](?:\.git|bin|obj|cache)[\\/]') { continue }
        $relative = [IO.Path]::GetRelativePath($roots[$repo], $file.FullName).Replace('\', '/')
        $entry = [pscustomobject]@{ repo = $repo; relative = $relative; full = $file.FullName }
        if (-not $trxIndex.ContainsKey($file.Name)) { $trxIndex[$file.Name] = [Collections.Generic.List[object]]::new() }
        $trxIndex[$file.Name].Add($entry)
    }
}

$references = [Collections.Generic.HashSet[string]]::new($caseInsensitive)
$resolved = [Collections.Generic.Dictionary[string, object]]::new($compare)
$missing = [Collections.Generic.Dictionary[string, object]]::new($caseInsensitive)
$referenceLocators = [Collections.Generic.Dictionary[string, object]]::new($caseInsensitive)
foreach ($ledger in $ledgers) {
    $lines = [IO.File]::ReadAllLines($ledger, $utf8)
    for ($lineNumber = 0; $lineNumber -lt $lines.Length; $lineNumber++) {
        $line = $lines[$lineNumber]
        foreach ($match in [regex]::Matches($line, '(?i)[A-Za-z0-9._-]+\.trx')) {
            $name = $match.Value
            $locator = "$(Split-Path $ledger -Leaf):$($lineNumber + 1)"
            [void]$references.Add($name)
            if (-not $referenceLocators.ContainsKey($name)) {
                $referenceLocators[$name] = [Collections.Generic.List[string]]::new()
            }
            if (-not $referenceLocators[$name].Contains($locator)) { $referenceLocators[$name].Add($locator) }
            $candidates = if ($trxIndex.ContainsKey($name)) { @($trxIndex[$name].ToArray()) } else { @() }
            if ($candidates.Count -gt 1) {
                $linePath = $line.Replace('\', '/')
                $specific = @($candidates | Where-Object {
                    $linePath.Contains($_.relative, [StringComparison]::OrdinalIgnoreCase) -or
                    $linePath.Contains(($_.relative -replace '^tests/', ''), [StringComparison]::OrdinalIgnoreCase)
                })
                if ($specific.Count -eq 1) { $candidates = $specific }
            }
            if ($candidates.Count -gt 1) { throw "AMBIGUOUS_TRX_REFERENCE name=$name locator=$locator" }
            if ($candidates.Count -eq 0) {
                if (-not $missing.ContainsKey($name)) { $missing[$name] = $locator }
                continue
            }
            $candidate = $candidates[0]
            $linePath = $line.Replace('\', '/')
            $explicit = [regex]::Match($linePath, '(?i)tests/[A-Za-z0-9._-]+/TestResults/' + [regex]::Escape($name))
            if ($explicit.Success -and -not $candidate.relative.Equals($explicit.Value, [StringComparison]::OrdinalIgnoreCase)) {
                throw "TRX_REFERENCE_PATH_MISMATCH name=$name locator=$locator path=$($explicit.Value)"
            }
            $key = "$($candidate.repo)`t$($candidate.relative)"
            if (-not $resolved.ContainsKey($key)) { $resolved[$key] = $candidate }
        }
    }
}

$manifestDirectory = Split-Path -Parent $ManifestPath
$resolutionPath = Join-Path $manifestDirectory 'a3_preflight_trx_reference_resolution.tsv'
$missingPath = Join-Path $manifestDirectory 'a3_preflight_referenced_trx_missing.tsv'
if ($Mode -eq 'Build') {
    $resolutionLines = [Collections.Generic.List[string]]::new()
    $resolutionLines.Add("reference_name`trepo`trepo_relative_path`tdisposition`tledger_locators")
    $names = [string[]]@($references)
    [Array]::Sort($names, $compare)
    foreach ($name in $names) {
        $matches = @($resolved.Values | Where-Object { [IO.Path]::GetFileName($_.relative) -ieq $name })
        $locators = [string]::Join(',', $referenceLocators[$name].ToArray())
        if ($matches.Count -eq 0) {
            $resolutionLines.Add("$name`t`t`tMISSING`t$locators")
        } else {
            foreach ($item in $matches) {
                $resolutionLines.Add("$name`t$($item.repo)`t$($item.relative)`tEXISTING`t$locators")
            }
        }
    }
    [IO.File]::WriteAllText($resolutionPath, ([string]::Join("`n", $resolutionLines) + "`n"), $utf8)
    $missingLines = [Collections.Generic.List[string]]::new()
    $missingLines.Add("reference_name`tledger_locator`tdisposition")
    foreach ($name in $names) {
        if ($missing.ContainsKey($name)) { $missingLines.Add("$name`t$($missing[$name])`tMISSING_EVIDENCE_NOT_COVERED") }
    }
    [IO.File]::WriteAllText($missingPath, ([string]::Join("`n", $missingLines) + "`n"), $utf8)
}

$visibleByRepo = @{}
$allPaths = [Collections.Generic.Dictionary[string, object]]::new($compare)
foreach ($repo in @('server', 'agent')) {
    $visible = @(& git -C $roots[$repo] ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw "GIT_INVENTORY_FAILED_$repo" }
    $visibleByRepo[$repo] = $visible.Count
    foreach ($relativeRaw in $visible) {
        $relative = $relativeRaw.Replace('\', '/')
        $key = "$repo`t$relative"
        if ($allPaths.ContainsKey($key)) { throw "DUPLICATE_NORMALIZED_PATH key=$key" }
        $allPaths[$key] = [pscustomobject]@{ repo = $repo; relative = $relative }
    }
}
foreach ($entry in $resolved.GetEnumerator()) {
    if (-not $allPaths.ContainsKey($entry.Key)) {
        $allPaths[$entry.Key] = [pscustomobject]@{ repo = $entry.Value.repo; relative = $entry.Value.relative }
    }
}

function Get-Role([string]$relative) {
    if ($relative.EndsWith('.trx', [StringComparison]::OrdinalIgnoreCase)) { return 'evidence_trx' }
    if ($relative.EndsWith('tip_88c1_c6b_a3_proof_reconciliation_v0_2.md', [StringComparison]::OrdinalIgnoreCase)) { return 'reconciliation_ledger' }
    if ($relative -match '(?i)/Migrations/' -or $relative -match '(?i)\.sql$') { return 'sql_or_migration' }
    if ($relative -match '(?i)^tests/.+\.cs$') { return 'test_source' }
    if ($relative -match '(?i)^src/.+\.cs$') { return 'product_source' }
    if ($relative -match '(?i)\.(csproj|sln|props|targets)$') { return 'project' }
    if ($relative -match '(?i)\.(json|yml|yaml|config)$') { return 'configuration' }
    if ($relative -match '(?i)\.(ps1|sh)$') { return 'script' }
    if ($relative.StartsWith('docs/', [StringComparison]::Ordinal)) { return 'authority_or_project_document' }
    return 'other_controlled'
}

$manifestKey = "server`t$([IO.Path]::GetRelativePath($roots.server, $ManifestPath).Replace('\', '/'))"
$sidecarKey = "$manifestKey.sha256"
if ($Mode -eq 'Build') {
    [void]$allPaths.Remove($manifestKey)
    [void]$allPaths.Remove($sidecarKey)
    $keys = [string[]]@($allPaths.Keys)
    [Array]::Sort($keys, $compare)
    $outputLines = [Collections.Generic.List[string]]::new()
    $outputLines.Add("repo`trepo_relative_path`tfile_role`tbyte_size`tsha256")
    foreach ($key in $keys) {
        $entry = $allPaths[$key]
        $full = Join-Path $roots[$entry.repo] $entry.relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (-not [IO.File]::Exists($full)) { throw "MISSING_SOURCE_FILE key=$key" }
        $size = (Get-Item -LiteralPath $full).Length
        $sha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
        $outputLines.Add("$($entry.repo)`t$($entry.relative)`t$(Get-Role $entry.relative)`t$size`t$sha")
    }
    [IO.File]::WriteAllText($ManifestPath, ([string]::Join("`n", $outputLines) + "`n"), $utf8)
    $manifestHash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash
    [IO.File]::WriteAllText("$ManifestPath.sha256", "$manifestHash  $([IO.Path]::GetFileName($ManifestPath))`n", $utf8)
}

$rows = @(Import-Csv -LiteralPath $ManifestPath -Delimiter "`t")
$lines = [IO.File]::ReadAllLines($ManifestPath, $utf8)
if ($lines.Length -ne $rows.Count + 1 -or $lines[0] -cne "repo`trepo_relative_path`tfile_role`tbyte_size`tsha256") {
    throw 'MANIFEST_SHAPE_INVALID'
}
$listed = [Collections.Generic.Dictionary[string, object]]::new($compare)
$prior = ''
foreach ($row in $rows) {
    $key = "$($row.repo)`t$($row.repo_relative_path)"
    if ($listed.ContainsKey($key)) { throw "DUPLICATE_NORMALIZED_PATH key=$key" }
    if ($prior -and $compare.Compare($prior, $key) -ge 0) { throw "MANIFEST_NOT_ORDINAL key=$key" }
    $listed[$key] = $row
    $prior = $key
}
foreach ($key in $resolved.Keys) {
    if (-not $listed.ContainsKey($key)) { throw "REFERENCED_TRX_OMITTED key=$key" }
}
foreach ($key in $allPaths.Keys) {
    if ($key -eq $manifestKey -or $key -eq $sidecarKey) { continue }
    if (-not $listed.ContainsKey($key)) { throw "GIT_VISIBLE_FILE_OMITTED key=$key" }
}
foreach ($row in $rows) {
    if (-not $roots.ContainsKey($row.repo)) { throw "UNKNOWN_REPO name=$($row.repo)" }
    $full = Join-Path $roots[$row.repo] $row.repo_relative_path.Replace('/', [IO.Path]::DirectorySeparatorChar)
    if (-not [IO.File]::Exists($full)) { throw "MANIFEST_FILE_MISSING key=$($row.repo):$($row.repo_relative_path)" }
    if ((Get-Item -LiteralPath $full).Length -ne [long]$row.byte_size) {
        throw "SIZE_DRIFT key=$($row.repo):$($row.repo_relative_path)"
    }
    if ((Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash -cne $row.sha256) {
        throw "SHA_DRIFT key=$($row.repo):$($row.repo_relative_path)"
    }
}
$manifestHash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash
$sidecar = [IO.File]::ReadAllText("$ManifestPath.sha256", $utf8)
if (-not $sidecar.StartsWith("$manifestHash  $([IO.Path]::GetFileName($ManifestPath))`n", [StringComparison]::Ordinal)) {
    throw 'MANIFEST_SIDECAR_DRIFT'
}

"git_visible_server=$($visibleByRepo.server)"
"git_visible_agent=$($visibleByRepo.agent)"
"ledger_trx_references_distinct=$($references.Count)"
"referenced_trx_existing=$($resolved.Count)"
"referenced_trx_missing=$($missing.Count)"
"referenced_trx_ambiguous=0"
"referenced_trx_in_manifest=$($resolved.Count)"
"referenced_trx_omitted=0"
"server_manifest_files=$(@($rows | Where-Object repo -eq 'server').Count)"
"agent_manifest_files=$(@($rows | Where-Object repo -eq 'agent').Count)"
"manifest_files_total=$($rows.Count)"
"baseline_manifest_sha=$manifestHash"
