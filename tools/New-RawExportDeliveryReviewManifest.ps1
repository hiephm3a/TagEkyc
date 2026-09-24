param(
    [string]$TagEkycRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent',
    [string]$SignFlowRoot = 'D:\Task\Remote Signing\Codex_SignFlow'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $TagEkycRoot 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_combined_review_manifest_v1.tsv'
$rows = [System.Collections.Generic.List[object]]::new()

function Add-ManifestFile {
    param(
        [string]$Repository,
        [string]$Disposition,
        [string]$Root,
        [string]$Path
    )

    $fullPath = Join-Path $Root ($Path -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Manifest input is missing: $Repository/$Path"
    }

    $rows.Add([pscustomobject]@{
        repository = $Repository
        disposition = $Disposition
        sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash
        path = $Path.Replace('\', '/')
    })
}

$productPaths = @(
    'src/TagEkyc.Api/Program.cs',
    'src/TagEkyc.Api/RawExportControlPlaneEndpoints.cs',
    'src/TagEkyc.Application/Ports/RawExportControlPlanePorts.cs',
    'src/TagEkyc.Application/RawExport/RawExportControlPlaneApplicationService.cs',
    'src/TagEkyc.Contracts/RawExport/RawExportControlPlaneContracts.cs',
    'src/TagEkyc.Infrastructure/Persistence/EfRawExportJobPackageProjectionReader.cs',
    'src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs',
    'src/TagEkyc.Infrastructure/Persistence/Migrations/20260923090000_RawExportAssemblyDurableWorkSource.cs',
    'src/TagEkyc.Infrastructure/ProtectedValues/ProtectedValueContracts.cs',
    'src/TagEkyc.Infrastructure/ProtectedValues/SecretRefProtectedValueProvider.cs',
    'src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRuntimeInfrastructure.cs',
    'src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs'
)
foreach ($path in $productPaths) {
    Add-ManifestFile TagEkyc PRODUCT_SOURCE_AUTHORIZED $TagEkycRoot $path
}

$testPaths = @(
    'tests/TagEkyc.UnitTests/RawExportControlPlaneApplicationTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportControlPlaneHttpTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportAssemblyDurableWorkSourceTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportClientProductionCodecCompatibilityTests.cs',
    'tests/TagEkyc.IntegrationTests/SiteQualificationTestServices.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RawIngressBoundaryTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ClientServerAcceptanceTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2R6ClusterHttpTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs'
)
foreach ($path in $testPaths) {
    Add-ManifestFile TagEkyc TEST_SOURCE $TagEkycRoot $path
}

$authorityAndGovernance = @(
    @('IMPLEMENTATION_AUTHORITY', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_homeowner_build_authority_v1.md'),
    @('W2_AUTHORITY', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_macro_wave_w2_authority_decision_packet_v2.md'),
    @('W2_AUTHORITY', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_macro_wave_w2_homeowner_scope_decision_v1.md'),
    @('GOVERNANCE_FROZEN', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_outcome_ownership_table_v1.tsv'),
    @('GOVERNANCE_FROZEN', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_scope_deferred_backlog_v1.tsv'),
    @('SEAL_NOT_REMINTED', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_evidence_seal_v1.tsv'),
    @('CURRENT_EMPTY_PARTITION', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_activation_scope_partition_v1.tsv'),
    @('WHOLE_REPO_INVENTORY', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_whole_repo_trx_inventory_v1.tsv'),
    @('FAILED_RUN_CENSUS', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_failed_run_census_v1.tsv'),
    @('REPRODUCIBLE_TOOLING', 'tools/New-RawExportDeliveryWholeRepoTrxCensus.ps1'),
    @('REPRODUCIBLE_TOOLING', 'tools/New-RawExportDeliveryReviewManifest.ps1')
)
foreach ($entry in $authorityAndGovernance) {
    Add-ManifestFile TagEkyc $entry[0] $TagEkycRoot $entry[1]
}

$serverResultRoots = @(
    'tests/TagEkyc.IntegrationTests/TestResults/raw-export-delivery',
    'tests/TagEkyc.UnitTests/TestResults/raw-export-delivery'
)
foreach ($relativeRoot in $serverResultRoots) {
    $fullRoot = Join-Path $TagEkycRoot ($relativeRoot -replace '/', [IO.Path]::DirectorySeparatorChar)
    foreach ($file in Get-ChildItem -LiteralPath $fullRoot -File -Filter '*.trx' | Sort-Object Name) {
        $relativePath = $relativeRoot + '/' + $file.Name
        $disposition = if ($file.Name -match '^raw-export-delivery-a(3|4|5|10)\.trx$') {
            'EVIDENCE_RED'
        } elseif ($file.Name -eq 'raw-export-delivery-a6.trx') {
            'SUPERSEDED_FIXTURE_RED'
        } elseif ($file.Name -match '^raw-export-delivery-a(14|15|16|19)\.trx$') {
            'SUPERSEDED_SENTINEL_FIXTURE_RED'
        } elseif ($file.Name -match '^a3-raw-export-work-source-current\.trx$') {
            'EXCLUDED_INFRASTRUCTURE_DIAGNOSTIC'
        } elseif ($file.Name -match '^a3-raw-export-work-source-current-v[2-5]\.trx$') {
            'SUPERSEDED_RED'
        } else {
            'PASS_EVIDENCE'
        }
        Add-ManifestFile TagEkyc $disposition $TagEkycRoot $relativePath
    }
}

Add-ManifestFile TagEkyc_CaptureAgent PASS_SENTINEL $AgentRoot 'tests/TagEkyc.CaptureAgent.Tests/TestResults/raw-export-delivery/raw-export-delivery-a13.trx'

$clientPaths = @(
    @('STANDALONE_CLIENT', 'src/Clients/TagEkyc.RawExport.Client/TagEkyc.RawExport.Client.csproj'),
    @('STANDALONE_CLIENT', 'src/Clients/TagEkyc.RawExport.Client/TagEkycRawExportModels.cs'),
    @('STANDALONE_CLIENT', 'src/Clients/TagEkyc.RawExport.Client/TagEkycRawExportClient.cs'),
    @('STANDALONE_CLIENT', 'src/Clients/TagEkyc.RawExport.Client/TagEkycRecipientPackageDecoder.cs'),
    @('STANDALONE_CLIENT', 'src/Clients/TagEkyc.RawExport.Client/README.md'),
    @('CLIENT_EVIDENCE', 'tests/TagEkyc.RawExport.Client.Tests/TagEkyc.RawExport.Client.Tests.csproj'),
    @('CLIENT_EVIDENCE', 'tests/TagEkyc.RawExport.Client.Tests/TagEkycRawExportClientTests.cs'),
    @('CLIENT_EVIDENCE', 'TestResults/raw-export-client/raw-export-client-successor.trx'),
    @('PREDECESSOR_PACKET', 'docs/ekyc_integration/raw_export_client_review_packet_v2.md')
)
foreach ($entry in $clientPaths) {
    Add-ManifestFile Codex_SignFlow $entry[0] $SignFlowRoot $entry[1]
}

$content = $rows | Sort-Object repository, path | ConvertTo-Csv -Delimiter "`t" -NoTypeInformation
$content | Set-Content -LiteralPath $outputPath -Encoding utf8NoBOM

$hash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash
Write-Output "manifest_rows=$($rows.Count)"
Write-Output "manifest_sha256=$hash"
Write-Output "manifest_path=$outputPath"
