param(
    [string]$TagEkycRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$AgentRoot = 'D:\Task\Remote Signing\TagEkyc.CaptureAgent'
)

$ErrorActionPreference = 'Stop'
$outputPath = Join-Path $TagEkycRoot 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_combined_review_manifest_v3.tsv'
$rows = [System.Collections.Generic.List[object]]::new()

function Get-GitNormalizedSha256 {
    param(
        [string]$Root,
        [string]$Path,
        [string]$FullPath
    )

    # Hash the exact blob bytes Git will retain, including clean filters and
    # text normalization. This keeps the manifest clone-verifiable even when
    # the Windows working tree uses CRLF for a tracked source file.
    $objectId = (& git -C $Root hash-object -w "--path=$Path" -- $FullPath).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($objectId)) {
        throw "Unable to materialize Git blob for $Root/$Path"
    }

    $startInfo = [Diagnostics.ProcessStartInfo]::new('git')
    $startInfo.WorkingDirectory = $Root
    $startInfo.ArgumentList.Add('cat-file')
    $startInfo.ArgumentList.Add('blob')
    $startInfo.ArgumentList.Add($objectId)
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false

    $process = [Diagnostics.Process]::Start($startInfo)
    $content = [IO.MemoryStream]::new()
    $process.StandardOutput.BaseStream.CopyTo($content)
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Unable to read Git blob for $Root/$Path`: $($process.StandardError.ReadToEnd())"
    }

    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($content.ToArray()))
}

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
        sha256 = Get-GitNormalizedSha256 $Root $Path $fullPath
        path = $Path.Replace('\', '/')
    })
}

$productPaths = @(
    'src/TagEkyc.Api/Program.cs',
    'src/TagEkyc.Api/RawExportControlPlaneEndpoints.cs',
    'src/TagEkyc.Application/Ports/RawExportControlPlanePorts.cs',
    'src/TagEkyc.Application/Ports/RecipientManagementPorts.cs',
    'src/TagEkyc.Application/RawExport/RecipientManagementApplicationService.cs',
    'src/TagEkyc.Contracts/RawExport/RecipientManagementContracts.cs',
    'src/TagEkyc.Application/RawExport/RawExportControlPlaneApplicationService.cs',
    'src/TagEkyc.Contracts/RawExport/RawExportControlPlaneContracts.cs',
    'src/TagEkyc.Infrastructure/Persistence/EfRawExportJobPackageProjectionReader.cs',
    'src/TagEkyc.Infrastructure/Auth/PostgresHashedApiKeyStore.cs',
    'src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportManagedRecipientPolicyConfig.cs',
    'src/TagEkyc.Infrastructure/Persistence/Migrations/20260924120000_RawExportDeliveryRecipientCredential.cs',
    'src/TagEkyc.Infrastructure/Persistence/Migrations/20260924130000_RawExportLegacyConsentClassFence.cs',
    'src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs',
    'src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs',
    'src/TagEkyc.Infrastructure/Persistence/Migrations/20260923090000_RawExportAssemblyDurableWorkSource.cs',
    'src/TagEkyc.Infrastructure/ProtectedValues/ProtectedValueContracts.cs',
    'src/TagEkyc.Infrastructure/ProtectedValues/SecretRefProtectedValueProvider.cs',
    'src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRuntimeInfrastructure.cs',
    'src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs',
    'src/TagEkyc.Infrastructure/RawExport/RecipientManagementCodec.cs',
    'src/TagEkyc.Infrastructure/RawExport/RecipientManagementReadinessValidator.cs',
    'src/TagEkyc.Infrastructure/RawExport/RecipientManagementRepository.cs'
)
foreach ($path in $productPaths) {
    Add-ManifestFile TagEkyc PRODUCT_SOURCE_AUTHORIZED $TagEkycRoot $path
}

$testPaths = @(
    'tests/TagEkyc.UnitTests/RawExportControlPlaneApplicationTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportControlPlaneHttpTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportAssemblyDurableWorkSourceTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportDeliveryCredentialMigrationTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportJobClientIsolationTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportClientProductionCodecCompatibilityTests.cs',
    'tests/TagEkyc.IntegrationTests/RawExportDeliverySameJobEndToEndTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs',
    'tests/TagEkyc.IntegrationTests/SiteQualificationTestServices.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RawIngressBoundaryTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ClientServerAcceptanceTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2R6ClusterHttpTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C5RecipientManagementTests.cs',
    'tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs',
    'tests/TagEkyc.ContractTests/Tip88C1C5RecipientManagementContractTests.cs',
    'tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj'
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
    @('PREDECESSOR_PACKET', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_combined_review_packet_v1.md'),
    @('PREDECESSOR_MANIFEST', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_combined_review_manifest_v1.tsv'),
    @('WHOLE_REPO_INVENTORY', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_whole_repo_trx_inventory_v2.tsv'),
    @('FAILED_RUN_CENSUS', 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_failed_run_census_v2.tsv'),
    @('REPRODUCIBLE_TOOLING', 'tools/New-RawExportDeliveryWholeRepoTrxCensus.ps1'),
    @('REPRODUCIBLE_TOOLING', 'tools/New-RawExportDeliveryReviewManifest.ps1')
)
foreach ($entry in $authorityAndGovernance) {
    Add-ManifestFile TagEkyc $entry[0] $TagEkycRoot $entry[1]
}

Add-ManifestFile TagEkyc PROJECT_GRAPH $TagEkycRoot 'TagEkyc.sln'

$serverResultRoots = @(
    'tests/TagEkyc.IntegrationTests/TestResults/raw-export-delivery',
    'tests/TagEkyc.UnitTests/TestResults/raw-export-delivery',
    'tests/TagEkyc.IntegrationTests/TestResults/raw-export-delivery-correction',
    'tests/TagEkyc.UnitTests/TestResults/raw-export-delivery-correction',
    'tests/TagEkyc.RawExport.Client.Tests/TestResults/raw-export-delivery-correction'
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
        } elseif ($file.Name -match '^raw-export-delivery-correction-(a3-fence-mutant|a4-valid-actor-mutant|f1-client-isolation-mutant|c5-authorize-scope-mutant)\.trx$') {
            'EVIDENCE_RED'
        } elseif ($file.Name -match '^raw-export-delivery-correction-(c5-scope-successor-baseline|c5-scope-successor-a2|migration-discovery)\.trx$') {
            'SUPERSEDED_RED'
        } elseif ($file.Name -eq 'raw-export-delivery-correction-c5-c1-c4-managed-chain.trx') {
            'PRODUCT_DEFECT_PREDECESSOR'
        } elseif ($file.Name -eq 'raw-export-delivery-correction-c5-full-restored.trx') {
            'KNOWN_TEST_DEBT'
        } elseif ($file.Name -match '^raw-export-delivery-same-job-e2e(-a2|-a5)?\.trx$') {
            'PRODUCT_DEFECT_PREDECESSOR'
        } elseif ($file.Name -eq 'raw-export-delivery-same-job-e2e-a3.trx') {
            'SUPERSEDED_RED'
        } elseif ($file.Name -eq 'raw-export-delivery-same-job-e2e-a4.trx') {
            'SUPERSEDED_FIXTURE_RED'
        } elseif ($file.Name -eq 'raw-export-delivery-consent-class-mutant.trx') {
            'EVIDENCE_RED'
        } else {
            'PASS_EVIDENCE'
        }
        Add-ManifestFile TagEkyc $disposition $TagEkycRoot $relativePath
    }
}

Add-ManifestFile TagEkyc_CaptureAgent PASS_SENTINEL $AgentRoot 'tests/TagEkyc.CaptureAgent.Tests/TestResults/raw-export-delivery/raw-export-delivery-a13.trx'

$clientPaths = @(
    @('STANDALONE_SDK_CLIENT', 'sdk/TagEkyc.RawExport.Client/TagEkyc.RawExport.Client.csproj'),
    @('STANDALONE_SDK_CLIENT', 'sdk/TagEkyc.RawExport.Client/TagEkycRawExportModels.cs'),
    @('STANDALONE_SDK_CLIENT', 'sdk/TagEkyc.RawExport.Client/TagEkycRawExportClient.cs'),
    @('STANDALONE_SDK_CLIENT', 'sdk/TagEkyc.RawExport.Client/TagEkycRecipientPackageDecoder.cs'),
    @('STANDALONE_SDK_CLIENT', 'sdk/TagEkyc.RawExport.Client/README.md'),
    @('CLIENT_EVIDENCE', 'tests/TagEkyc.RawExport.Client.Tests/TagEkyc.RawExport.Client.Tests.csproj'),
    @('CLIENT_EVIDENCE', 'tests/TagEkyc.RawExport.Client.Tests/TagEkycRawExportClientTests.cs'),
    @('CLIENT_EVIDENCE', 'tests/TagEkyc.RawExport.Client.Tests/TestResults/raw-export-client-successor.trx')
)
foreach ($entry in $clientPaths) {
    Add-ManifestFile TagEkyc $entry[0] $TagEkycRoot $entry[1]
}

$content = $rows | Sort-Object repository, path | ConvertTo-Csv -Delimiter "`t" -NoTypeInformation
$content | Set-Content -LiteralPath $outputPath -Encoding utf8NoBOM

$hash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash
Write-Output "manifest_rows=$($rows.Count)"
Write-Output "manifest_sha256=$hash"
Write-Output "manifest_path=$outputPath"
