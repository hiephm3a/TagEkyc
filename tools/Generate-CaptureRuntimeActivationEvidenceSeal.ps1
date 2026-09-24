param(
    [string]$RepoRoot,
    [string]$OutputPath,
    [switch]$SelfTest,
    [switch]$Strict
)

$ErrorActionPreference = 'Stop'

function Get-ExactRatificationFields([string]$record) {
    $requiredNames = @(
        'A3-Ratification-Decision',
        'A3-Authority-Open-After',
        'A3-Reviewed-Manifest-SHA256'
    )
    $fields = @{}
    foreach ($line in ($record -split "`r?`n")) {
        if ($line -notmatch '^(A3-Ratification-Decision|A3-Authority-Open-After|A3-Reviewed-Manifest-SHA256):[ \t]*(\S+)[ \t]*$') {
            continue
        }
        $name = $Matches[1]
        if ($fields.ContainsKey($name)) {
            throw "ACTIVATION_EVIDENCE_RATIFICATION_FIELD_DUPLICATE field=$name"
        }
        $fields[$name] = $Matches[2]
    }
    foreach ($name in $requiredNames) {
        if (-not $fields.ContainsKey($name)) {
            throw "ACTIVATION_EVIDENCE_RATIFICATION_FIELD_MISSING field=$name"
        }
    }
    return $fields
}

function Assert-ExactRatificationRecord([string]$record, [string]$expectedManifestSha, [int]$expectedOpenRows) {
    $fields = Get-ExactRatificationFields $record
    if ($fields['A3-Ratification-Decision'] -cne 'RATIFIED' -or
        $fields['A3-Authority-Open-After'] -cne "$expectedOpenRows" -or
        $fields['A3-Reviewed-Manifest-SHA256'] -cne $expectedManifestSha) {
        throw 'ACTIVATION_EVIDENCE_RATIFICATION_RECORD_INVALID'
    }
}

function Get-ExactScopeFields([string]$decision) {
    $requiredNames = @(
        'A3-Scope-Decision',
        'A3-Authority-Open-After',
        'A3-Activated-Includes-Assembly',
        'A3-Assembly-Topology',
        'A3-Ownership-Table-SHA256',
        'A3-Partition-SHA256',
        'A3-Ledger-SHA256'
    )
    $fields = @{}
    foreach ($line in ($decision -split "`r?`n")) {
        if ($line -notmatch '^(A3-Scope-Decision|A3-Authority-Open-After|A3-Activated-Includes-Assembly|A3-Assembly-Topology|A3-Ownership-Table-SHA256|A3-Partition-SHA256|A3-Ledger-SHA256):[ \t]*(\S+)[ \t]*$') {
            continue
        }
        $name = $Matches[1]
        if ($fields.ContainsKey($name)) {
            throw "ACTIVATION_EVIDENCE_SCOPE_FIELD_DUPLICATE field=$name"
        }
        $fields[$name] = $Matches[2]
    }
    foreach ($name in $requiredNames) {
        if (-not $fields.ContainsKey($name)) {
            throw "ACTIVATION_EVIDENCE_SCOPE_FIELD_MISSING field=$name"
        }
    }
    return $fields
}

function Assert-ExactScopeDecision([string]$decision, [int]$expectedOpenRows,
    [string]$expectedOwnershipSha, [string]$expectedPartitionSha, [string]$expectedLedgerSha) {
    $fields = Get-ExactScopeFields $decision
    $includesAssembly = $fields['A3-Activated-Includes-Assembly']
    $topology = $fields['A3-Assembly-Topology']
    if (-not (($includesAssembly -ceq 'NO' -and $topology -ceq 'Disabled') -or
               ($includesAssembly -ceq 'YES' -and $topology -ceq 'DurableWorker'))) {
        throw 'ACTIVATION_EVIDENCE_ASSEMBLY_SCOPE_CONTRADICTION'
    }
    if ($fields['A3-Scope-Decision'] -cne 'APPROVED' -or
        $fields['A3-Authority-Open-After'] -cne "$expectedOpenRows" -or
        $fields['A3-Ownership-Table-SHA256'] -cne $expectedOwnershipSha -or
        $fields['A3-Partition-SHA256'] -cne $expectedPartitionSha -or
        $fields['A3-Ledger-SHA256'] -cne $expectedLedgerSha) {
        throw 'ACTIVATION_EVIDENCE_SCOPE_DECISION_INVALID'
    }
    return $topology
}

if ($SelfTest) {
    $testManifest = ('A' * 64)
    $valid = @"
A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 0
A3-Reviewed-Manifest-SHA256: $testManifest
"@
    Assert-ExactRatificationRecord $valid $testManifest 0
    'generator_self_test_valid=PASS'

    $wrongCount = @"
Date: 2026-09-21
A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 1
A3-Reviewed-Manifest-SHA256: $testManifest
"@
    try {
        Assert-ExactRatificationRecord $wrongCount $testManifest 0
        throw 'SELF_TEST_WRONG_COUNT_DID_NOT_FAIL'
    }
    catch {
        if ($_.Exception.Message -eq 'SELF_TEST_WRONG_COUNT_DID_NOT_FAIL') { throw }
    }
    'generator_self_test_wrong_count=PASS'

    $notRatified = @"
A3-Ratification-Decision: PROPOSED
A3-Authority-Open-After: 0
A3-Reviewed-Manifest-SHA256: $testManifest
"@
    try {
        Assert-ExactRatificationRecord $notRatified $testManifest 0
        throw 'SELF_TEST_NON_RATIFICATION_DID_NOT_FAIL'
    }
    catch {
        if ($_.Exception.Message -eq 'SELF_TEST_NON_RATIFICATION_DID_NOT_FAIL') { throw }
    }
    'generator_self_test_non_ratification=PASS'
    $scope = @"
A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 7
A3-Activated-Includes-Assembly: NO
A3-Assembly-Topology: Disabled
A3-Ownership-Table-SHA256: $testManifest
A3-Partition-SHA256: $testManifest
A3-Ledger-SHA256: $testManifest
"@
    if ((Assert-ExactScopeDecision $scope 7 $testManifest $testManifest $testManifest) -cne 'Disabled') {
        throw 'SELF_TEST_VALID_SCOPE_DID_NOT_PASS'
    }
    'generator_self_test_valid_scope=PASS'

    $includedScope = $scope.Replace('A3-Activated-Includes-Assembly: NO',
        'A3-Activated-Includes-Assembly: YES').Replace('A3-Assembly-Topology: Disabled',
        'A3-Assembly-Topology: DurableWorker')
    if ((Assert-ExactScopeDecision $includedScope 7 $testManifest $testManifest $testManifest) -cne 'DurableWorker') {
        throw 'SELF_TEST_APPROVED_WORKER_SCOPE_DID_NOT_PASS'
    }
    'generator_self_test_approved_worker_scope=PASS'

    $contradictory = $scope.Replace('A3-Activated-Includes-Assembly: NO',
        'A3-Activated-Includes-Assembly: YES')
    try {
        Assert-ExactScopeDecision $contradictory 7 $testManifest $testManifest $testManifest | Out-Null
        throw 'SELF_TEST_CONTRADICTORY_SCOPE_DID_NOT_FAIL'
    }
    catch {
        if ($_.Exception.Message -eq 'SELF_TEST_CONTRADICTORY_SCOPE_DID_NOT_FAIL') { throw }
    }
    'generator_self_test_contradictory_scope=PASS'

    try {
        Assert-ExactScopeDecision $scope 6 $testManifest $testManifest $testManifest | Out-Null
        throw 'SELF_TEST_SCOPE_COUNT_DID_NOT_FAIL'
    }
    catch {
        if ($_.Exception.Message -eq 'SELF_TEST_SCOPE_COUNT_DID_NOT_FAIL') { throw }
    }
    'generator_self_test_scope_count=PASS'
    'generator_self_tests=7/7'
    return
}

if ([string]::IsNullOrWhiteSpace($RepoRoot) -or [string]::IsNullOrWhiteSpace($OutputPath)) {
    throw 'ACTIVATION_EVIDENCE_GENERATOR_ARGUMENTS_REQUIRED'
}

# Ordinary builds must remain possible while a future candidate is changing.
# Only a strict governance re-mint can claim a current seal. If the strict
# check fails, compile a null seal so Prepared remains usable and Activated
# fails closed before route selection.
if (-not $Strict) {
    try {
        & $PSCommandPath -RepoRoot $RepoRoot -OutputPath $OutputPath -Strict | Out-Null
        return
    } catch {
        $outputDirectory = Split-Path -Parent $OutputPath
        [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
        $invalidSource = @'
// <auto-generated />
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Api;

internal sealed class BuildGeneratedCaptureRuntimeActivationEvidenceSealProvider : ICaptureRuntimeActivationEvidenceSealProvider
{
    public CaptureRuntimeActivationEvidenceSeal? Current => null;
}
'@
        [IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $invalidSource,
            [Text.UTF8Encoding]::new($false))
        Write-Warning "ACTIVATION_EVIDENCE_BUILD_SEAL_INVALID reason=$($_.Exception.Message)"
        return
    }
}

$root = [IO.Path]::GetFullPath($RepoRoot)
$docDir = Join-Path $root 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly'
$ledgerPath = Join-Path $docDir 'tip_88c1_c6b_a3_proof_reconciliation_v0_2.md'
$descriptorPath = Join-Path $docDir 'a3_activation_evidence_seal_v1.tsv'

$descriptor = @(Import-Csv -LiteralPath $descriptorPath -Delimiter "`t")
if ($descriptor.Count -ne 1) { throw 'ACTIVATION_EVIDENCE_DESCRIPTOR_ROW_INVALID' }
$seal = $descriptor[0]
$required = @('format_version', 'evidence_revision', 'partition_sha256', 'ledger_sha256',
    'authority_open_row_count', 'ratification_manifest_path', 'ratification_manifest_sha256',
    'ratification_record_path', 'ratification_record_sha256')
foreach ($field in $required) {
    if ([string]::IsNullOrWhiteSpace($seal.$field)) {
        throw "ACTIVATION_EVIDENCE_DESCRIPTOR_FIELD_INVALID field=$field"
    }
}
if ($seal.format_version -cnotin @('1','2') -or $seal.evidence_revision -notmatch '^[1-9][0-9]*$' -or
    $seal.authority_open_row_count -notmatch '^(0|[1-9][0-9]*)$') {
    throw 'ACTIVATION_EVIDENCE_DESCRIPTOR_NUMBER_INVALID'
}
if ($seal.format_version -ceq '1' -and [int]$seal.authority_open_row_count -eq 0) {
    throw 'ACTIVATION_EVIDENCE_LEGACY_ZERO_OPEN_FORBIDDEN'
}
foreach ($field in @('partition_sha256', 'ledger_sha256', 'ratification_manifest_sha256', 'ratification_record_sha256')) {
    if ($seal.$field -cnotmatch '^[0-9A-F]{64}$') {
        throw "ACTIVATION_EVIDENCE_DESCRIPTOR_SHA_INVALID field=$field"
    }
}
if ($seal.format_version -ceq '2') {
    foreach ($field in @('scope_decision_path','scope_decision_sha256',
            'ownership_table_sha256','approved_assembly_topology')) {
        if ([string]::IsNullOrWhiteSpace($seal.$field)) {
            throw "ACTIVATION_EVIDENCE_SCOPE_DESCRIPTOR_FIELD_INVALID field=$field"
        }
    }
    foreach ($field in @('scope_decision_sha256','ownership_table_sha256')) {
        if ($seal.$field -cnotmatch '^[0-9A-F]{64}$') {
            throw "ACTIVATION_EVIDENCE_SCOPE_DESCRIPTOR_SHA_INVALID field=$field"
        }
    }
    if ($seal.approved_assembly_topology -cnotin @('Disabled','DurableWorker')) {
        throw 'ACTIVATION_EVIDENCE_SCOPE_DESCRIPTOR_TOPOLOGY_INVALID'
    }
    $siteRequired = $seal.site_transport_qualification_required -ceq 'YES'
    $sitePolicyVersion = if ($seal.site_transport_qualification_policy_version -match '^[1-9][0-9]*$') {
        [int]$seal.site_transport_qualification_policy_version
    } else { 0 }
    if ([int]$seal.authority_open_row_count -eq 0 -and
        (-not $siteRequired -or $sitePolicyVersion -le 0)) {
        throw 'ACTIVATION_EVIDENCE_SITE_QUALIFICATION_POLICY_REQUIRED'
    }
    $manifestVerifier = Join-Path $docDir 'a3_activation_scope_rebaseline_manifest_verify.ps1'
    $agentRootForVerification = [IO.Path]::GetFullPath((Join-Path $root '..\TagEkyc.CaptureAgent'))
    & $manifestVerifier -Mode Verify -ServerRoot $root -AgentRoot $agentRootForVerification `
        -ManifestName ([IO.Path]::GetFileName($seal.ratification_manifest_path)) `
        -RatificationRecordName ([IO.Path]::GetFileName($seal.ratification_record_path)) `
        -ScopeDecisionName ([IO.Path]::GetFileName($seal.scope_decision_path)) | Out-Null
}

$partitionName = if ($seal.format_version -ceq '2') {
    'a3_activation_scope_partition_v1.tsv'
} else { 'a3_macro_wave_partition_v1.tsv' }
$verifierName = if ($seal.format_version -ceq '2') {
    'a3_activation_scope_partition_verify.ps1'
} else { 'a3_macro_wave_partition_verify.ps1' }
$partitionPath = Join-Path $docDir $partitionName
$verification = @(& (Join-Path $docDir $verifierName) -DocDir $docDir)
$openLine = @($verification | Where-Object { $_ -match '^official_open_rows=\d+$' })
if ($openLine.Count -ne 1) { throw 'ACTIVATION_EVIDENCE_VERIFIER_OUTPUT_INVALID' }
$buildOpenRows = [int](($openLine[0] -split '=', 2)[1])

function Resolve-ControlledPath([string]$relativePath) {
    if ([IO.Path]::IsPathRooted($relativePath)) { throw 'ACTIVATION_EVIDENCE_ABSOLUTE_PROVENANCE_PATH_FORBIDDEN' }
    $resolved = [IO.Path]::GetFullPath((Join-Path $root $relativePath))
    $prefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ACTIVATION_EVIDENCE_PROVENANCE_PATH_ESCAPE'
    }
    return $resolved
}

$manifestPath = Resolve-ControlledPath $seal.ratification_manifest_path
$recordPath = Resolve-ControlledPath $seal.ratification_record_path
foreach ($item in @(@($manifestPath, $seal.ratification_manifest_sha256), @($recordPath, $seal.ratification_record_sha256))) {
    if (-not (Test-Path -LiteralPath $item[0] -PathType Leaf) -or
        (Get-FileHash -LiteralPath $item[0] -Algorithm SHA256).Hash -cne $item[1]) {
        throw 'ACTIVATION_EVIDENCE_RATIFICATION_PROVENANCE_INVALID'
    }
}

$manifest = @(Import-Csv -LiteralPath $manifestPath -Delimiter "`t")
$partitionRelative = "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/$partitionName"
$ledgerRelative = 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a3_proof_reconciliation_v0_2.md'
function Assert-ManifestIdentity([string]$path, [string]$expectedSha) {
    $matches = @($manifest | Where-Object { $_.repo -ceq 'server' -and $_.repo_relative_path -ceq $path })
    if ($matches.Count -ne 1 -or $matches[0].sha256 -cne $expectedSha) {
        throw "ACTIVATION_EVIDENCE_RATIFIED_MANIFEST_IDENTITY_INVALID path=$path"
    }
}
Assert-ManifestIdentity $partitionRelative $seal.partition_sha256
Assert-ManifestIdentity $ledgerRelative $seal.ledger_sha256

$record = Get-Content -LiteralPath $recordPath -Raw
Assert-ExactRatificationRecord $record $seal.ratification_manifest_sha256 ([int]$seal.authority_open_row_count)

$buildAssemblyTopology = $null
if ($seal.format_version -ceq '2') {
    $ownershipRelative = 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_outcome_ownership_table_v1.tsv'
    $scopeDecisionPath = Resolve-ControlledPath $seal.scope_decision_path
    $ownershipPath = Resolve-ControlledPath $ownershipRelative
    if (-not (Test-Path -LiteralPath $scopeDecisionPath -PathType Leaf) -or
        (Get-FileHash -LiteralPath $scopeDecisionPath -Algorithm SHA256).Hash -cne $seal.scope_decision_sha256 -or
        (Get-FileHash -LiteralPath $ownershipPath -Algorithm SHA256).Hash -cne $seal.ownership_table_sha256) {
        throw 'ACTIVATION_EVIDENCE_SCOPE_PROVENANCE_INVALID'
    }
    Assert-ManifestIdentity $seal.scope_decision_path $seal.scope_decision_sha256
    Assert-ManifestIdentity $ownershipRelative $seal.ownership_table_sha256
    $buildAssemblyTopology = Assert-ExactScopeDecision (Get-Content -LiteralPath $scopeDecisionPath -Raw) ([int]$seal.authority_open_row_count) $seal.ownership_table_sha256 $seal.partition_sha256 $seal.ledger_sha256
    if ($buildAssemblyTopology -cne $seal.approved_assembly_topology) {
        throw 'ACTIVATION_EVIDENCE_ASSEMBLY_TOPOLOGY_DRIFT'
    }

    # Bind the current bytes of every authority source cited by the deferred
    # candidate/assurance inventory, not just the inventory's own SHA.
    $ownershipRows = @(Import-Csv -LiteralPath $ownershipPath -Delimiter "`t")
    $authorityCitations = @($ownershipRows |
        Where-Object { $_.AuthorityStatus -in @('CANDIDATE_ONLY','DERIVED_ASSURANCE') } |
        ForEach-Object { $_.Authority -split ';' } |
        Where-Object { $_ -ne 'NOT_TRACED' } | Sort-Object -Unique)
    $agentRoot = [IO.Path]::GetFullPath((Join-Path $root '..\TagEkyc.CaptureAgent'))
    foreach ($citation in $authorityCitations) {
        if ($citation -notmatch '^(server|agent)/(.+):[1-9][0-9]*$') {
            throw "ACTIVATION_EVIDENCE_AUTHORITY_CITATION_INVALID citation=$citation"
        }
        $repoName = $Matches[1]
        $relative = $Matches[2]
        $entry = @($manifest | Where-Object {
            $_.repo -ceq $repoName -and $_.repo_relative_path -ceq $relative
        })
        if ($entry.Count -ne 1 -or $entry[0].sha256 -cnotmatch '^[0-9A-F]{64}$') {
            throw "ACTIVATION_EVIDENCE_AUTHORITY_MANIFEST_MISSING citation=$citation"
        }
        $sourceRoot = if ($repoName -ceq 'server') { $root } else { $agentRoot }
        $sourcePath = [IO.Path]::GetFullPath((Join-Path $sourceRoot $relative))
        $prefix = $sourceRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        if (-not $sourcePath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $sourcePath -PathType Leaf) -or
            (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -cne $entry[0].sha256) {
            throw "ACTIVATION_EVIDENCE_AUTHORITY_SOURCE_DRIFT citation=$citation"
        }
    }
}

$buildPartitionSha = (Get-FileHash -LiteralPath $partitionPath -Algorithm SHA256).Hash
$buildLedgerSha = (Get-FileHash -LiteralPath $ledgerPath -Algorithm SHA256).Hash
$outputDirectory = Split-Path -Parent $OutputPath
[IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$scopeArguments = if ($seal.format_version -ceq '2') {
    $siteRequiredLiteral = if ($siteRequired) { 'true' } else { 'false' }
    @"
,
        "$($seal.approved_assembly_topology)",
        "$buildAssemblyTopology",
        "$($seal.scope_decision_sha256)",
        "$($seal.ownership_table_sha256)",
        null,
        null,
        $siteRequiredLiteral,
        $siteRequiredLiteral,
        $sitePolicyVersion,
        $sitePolicyVersion
"@
} else { '' }
$source = @"
// <auto-generated />
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Api;

internal sealed class BuildGeneratedCaptureRuntimeActivationEvidenceSealProvider : ICaptureRuntimeActivationEvidenceSealProvider
{
    public CaptureRuntimeActivationEvidenceSeal Current { get; } = new(
        $($seal.format_version),
        $($seal.evidence_revision),
        "$($seal.partition_sha256)",
        "$($seal.ledger_sha256)",
        $($seal.authority_open_row_count),
        "$buildPartitionSha",
        "$buildLedgerSha",
        $buildOpenRows,
        "$($seal.ratification_manifest_sha256)",
        "$($seal.ratification_record_sha256)"$scopeArguments);
}
"@
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $source, [Text.UTF8Encoding]::new($false))
