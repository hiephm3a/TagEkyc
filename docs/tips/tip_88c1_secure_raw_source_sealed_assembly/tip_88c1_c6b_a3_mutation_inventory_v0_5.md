# A3 v0.5 — proposed mutation inventory and source freeze

**Date:** 2026-09-13. **Status:** candidate implementation allowlist; documentation-only this turn.
Server root `D:/Task/Remote Signing/TagEkyc` (S); Agent root `D:/Task/Remote Signing/TagEkyc.CaptureAgent` (A).
Server HEAD `5df5f60a6dc4d992c71fc2b160673e4abca6488d`; Agent HEAD `e3bd625bbe357b1f3c9620d20bcba121cfb61974`.

These rows are proposed future mutation targets, not authority to mutate them now. M means the exact existing file was found/read at raw working-byte SHA; N means Test-Path confirmed absence. No fictional existing gateway path is used: the landed implementation is Infrastructure/CaptureRuntime/CaptureRuntimeExecutionPersistenceBoundary.cs and entity configurations live under Persistence/Configurations.

Owner E01 = reference/permit companion; CP = checkpoint/lineage companion; BR = broker/pipeline companion; AR = Agent/result companion. Each owner defines the output and named tests. The A1 canonical-document rows own synchronized contract changes, not a rewrite of unrelated decisions. No path wildcard grants product mutation.

## 1. Exact closed file rows

| Repo | Change | Path | Raw SHA-256 / absence | Owner | Proof/requirement |
| --- | --- | --- | --- | --- | --- |
| S | N | `src/TagEkyc.Contracts/CaptureRuntime/RawSourceConsentContracts.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Application/CaptureRuntime/RawSourceConsentApplicationService.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Application/Ports/RawSourceRetentionPorts.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionGateway.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Infrastructure/Persistence/Entities/RawSourceRetentionEntities.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionEntityConfigurations.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Application/CaptureRuntime/RawSourceRetentionProfile.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `src/TagEkyc.Api/RawSourceConsentEndpoints.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs` | `ACTUAL_MISSING` | E01 | E01 |
| S | M | `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeExecutionApplicationService.cs` | `C512FB6DDE8D834E5D67CF68BC28519AB64C66B51A1A59F2FFBB430932D1A274` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Contracts/CaptureRuntime/CaptureRuntimeExecutionContracts.cs` | `C52CCB3DB8E523817271F42ACFE53DD94300D0C33FC61246B7932B460D1A1CC0` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Application/Ports/CaptureRuntimePorts.cs` | `16C609A9506998C979997DA32B4B24476220A466ED51E8BF45492B7399ED7CED` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Api/CaptureRuntimeExecutionEndpoints.cs` | `8B9AF9351B9E8577ACF588AABE30004A25092DD284B4A425821B21C7CE70820B` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeExecutionPersistenceBoundary.cs` | `390DB3CC36B74EF9C99007DD4ABAEE7DC003F29E84EF0EE3F3AB2A4282097540` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/Entities/CaptureRuntimeEntities.cs` | `B6B58DBA78677B683177036D22547907D8894803BB7D1C12FE5CE7A065382E41` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/Configurations/CaptureRuntimeEntityConfigurations.cs` | `3585D2FE6545FF0AF2884E3D2C87BB1FA103DDA1BB40CA574303A7117FBFD046` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAuthoritySnapshotRow.cs` | `F6B6514A708CAE309CF02E820B8DC63376260973C942C37C0FCD5995F1B9E119` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs` | `C49D4A3C45132BA96ACF26D0CDD6673AC69DD214843BAE0347D376A903EBD293` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs` | `945FD9C6028CE3B310891F21FF3F20E033928CA8C88CDD3DEBAB09F551C7B92E` | CP | CP01–CP10 |
| S | N | `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | N | `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.Designer.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/RawExportControlPlaneReadinessValidator.cs` | `AE4408D7B2BF46A3EAFA9F1801D59C37BF3B5950662EBCA73ED1109D147762C7` | CP | CP01–CP10 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionContinuationRepository.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourceContinuationWorker.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | N | `src/TagEkyc.Infrastructure/Persistence/RawSourceRetentionReadinessValidator.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs` | `ACTUAL_MISSING` | CP | CP01–CP10 |
| S | N | `src/TagEkyc.RawIngressBroker/TagEkyc.RawIngressBroker.csproj` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.RawIngressBroker/Program.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Contracts/RawExport/RawIngressBrokerContracts.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Application/Ports/RawIngressBrokerPorts.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Application/RawExport/CaptureRuntimeRawIngressAdmissionService.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerHttpClient.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerTransactionFacade.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RetainedSourceClaimPreflight.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeCustodyProviderScopes.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerOptions.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | M | `src/TagEkyc.Infrastructure/RawExport/RawExportSourceClaimComparisonBroker.cs` | `690A9DA49BED1580AB4A3455640A375A85F8A78C3280DAB9FBEDB3239FF8CF89` | BR | BP01–BP16 |
| S | M | `src/TagEkyc.Api/RawExportSourceIngressServiceRegistration.cs` | `2C24F2148A65B5465CCB5DE3AF2B1B5ECDA78851AA1458F17E12495BCA29C9AD` | BR | BP01–BP16 |
| S | M | `src/TagEkyc.Api/Program.cs` | `97A41D2C2EE3025C497C203E44ED8B2BD5758CD9EA962D600F65BA412C80C499` | BR | BP01–BP16 |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `tests/TagEkyc.ArchTests/Tip88C1C6BA3CompositionArchTests.cs` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | N | `tools/Invoke-CaptureAgentA3Acceptance.ps1` | `ACTUAL_MISSING` | BR | BP01–BP16 |
| S | M | `src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs` | `C37599AB9D4EE15F666FDBFBF1CCC082342F8D139D14FB37B9BCEA027D548570` | AR | S02/R25/Completion |
| S | M | `src/TagEkyc.Contracts/RawExport/RawExportSourceIngressContracts.cs` | `4F86BAFC3FE4AD21435B8ACFBE4AA6102DC2CF78615DD3CB83D0696462171D66` | AR | S02/R25/Completion |
| S | M | `src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeAppendApplicationService.cs` | `B80D6C037CC01F18BF1004197E65FC23BB66FB5801509954A5FED1E32A6E12E5` | AR | S02/R25/Completion |
| S | M | `src/TagEkyc.Application/Ports/RawExportSourceIngressPorts.cs` | `F63CB0521220DEFE50B96E24C7F2F70387F83835DF50344277D5BC456EE286CA` | AR | S02/R25/Completion |
| S | N | `src/TagEkyc.Infrastructure/Persistence/EfRawExportCaptureAcceptanceWriter.cs` | `ACTUAL_MISSING` | AR | S02/R25/Completion |
| S | M | `src/TagEkyc.Infrastructure/Persistence/EfVerificationFinalizationBoundary.cs` | `C68F5BDC55B65B1300795EB9617C17DE68133D047E344D9A3089463BE06A9525` | AR | S02/R25/Completion |
| S | M | `src/TagEkyc.Application/VerificationSessions/VerificationCompletionApplicationService.cs` | `8FE4F34C87AE73DC5ED9C73424785ED3C1860A3993D9273DBFBD898840E9E76F` | AR | S02/R25/Completion |
| S | M | `src/TagEkyc.Application/Ports/RepositoryPorts.cs` | `C7879B2ACB04F32E18C6DC6BF9DDEE10CEEBED23E39388CEF3536E1E1DEABFFE` | AR | S02/R25/Completion |
| S | M | `tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj` | `162E91C273302159D5A0B5E81BFF13F6236265AB37E088544192B6C7ECB293D6` | AR | S02/R25/Completion |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ResultAcceptanceTests.cs` | `ACTUAL_MISSING` | AR | S02/R25/Completion |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ClientServerAcceptanceTests.cs` | `ACTUAL_MISSING` | AR | S02/R25/Completion |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1RawIngressBoundaryTests.cs` | `D489CBB077A9A8984FAD87ECFB5B070C55BB73CFCF4CBD3CBACE25C7941B18C9` | AR | S02/R25/Completion |
| S | M | `tests/TagEkyc.ArchTests/Tip88C1C6BA1DispatchCatalogueTests.cs` | `A12D58DC24815239F42C371B4EFC1B2B52FC7BF83BD01ACD5735795A6D605270` | AR | S02/R25/Completion |
| S | M | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md` | `54AE16BF69A58DC60B07287C2271FDD7F6A8E3977B3F22ADEE16E0EEB7D9B018` | AR | S02/R25/Completion |
| S | M | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch_r27_metadata_v1.md` | `661F67153E06734483C809C3667577107F7E0B49A70BF6E5EA45C17B8F5E2062` | AR | S02/R25/Completion |
| A | M | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeHttpClient.cs` | `21CFBFC1CDD1E0DFE2250D440AF3654EB9F07767627575E5A97214D2AA5386FF` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeWireCodec.cs` | `2D5273DEB3C5C6935E8BD3DD07B1FB630F861D3992471FEDC5E69698BFD7BFAB` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs` | `112307860A03A2FEC9A540D379C18FF701F00AFA6BCCA51B3D864E01A5D28EBD` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs` | `97A479CDF490261236F62627EE19141643866E1BD5A0C9D0CDCF0C463825C405` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Core/RetainedRawBufferCapacity.cs` | `8200232C63A6DCB6DEFF4E60F8427B7E8D94E18663A360DCF795CD209D1DA60A` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Core/SensitiveByteBuffer.cs` | `B75862BE5AEF0DD7464E66D81E06AF6927233B2B5EF3EF984604B08F73616293` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Core/RawExportIngressContracts.cs` | `6C613E7317FF78F902B4254941381AB58670B68386F1E9CB6C1D727A4F4262A1` | AR | Agent/result §4–§6 |
| A | N | `src/TagEkyc.CaptureAgent.Core/RawExportRetainedSubmissionOwner.cs` | `ACTUAL_MISSING` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Host/Program.cs` | `60DAE1689787D7161886CD662B096A5E090B4017E3903592FF60C8D9471B2194` | AR | Agent/result §4–§6 |
| A | M | `src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs` | `68DDE518B765DB8921B57309783DC52CC0B95322123699D5CCAB88858C7FE444` | AR | Agent/result §4–§6 |
| A | N | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA3RawIngressTests.cs` | `ACTUAL_MISSING` | AR | Agent/result §4–§6 |
| A | N | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA3RetainedOwnershipTests.cs` | `ACTUAL_MISSING` | AR | Agent/result §4–§6 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionProfileValidator.cs` | `ACTUAL_MISSING` | E01 | E01_ProfileHorizonFrozen |
| S | M | `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeStartupCatalogue.cs` | `0CAFDE749E33A596AEA01992410E5572B074F31B6029EC13FF856A81509293C3` | CP | CP10/CP11 |
| S | M | `src/TagEkyc.Infrastructure/Persistence/RawExportAuthoritySnapshotReadinessValidator.cs` | `7395F6DAE7B54302B271905B9105BC6C30301B2FA0AA1C05655C7DD42C624C3C` | CP | CP10/CP11 |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionContinuationContracts.cs` | `ACTUAL_MISSING` | CP | CP10/CP11 |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3CapabilityLineageTests.cs` | `ACTUAL_MISSING` | CP | CP10/CP11 |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1AppendAuthorityTests.cs` | `5F8A91CD78A87E5782AEC75BA463546C771AA01FBFD9BC5EA7FB1718C11E0966` | CP | CP11/R20 signature regression |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1CancellationHttpTests.cs` | `8171A9569618B0E82E67C655DA2DF773E5ED980FBD0D9D53E5CADCC9D22F5CC1` | CP | CP11/R20 signature regression |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1LifecycleRaceTests.cs` | `958FD52EE1F5BDEEFAA76BE96D9792447FF6A8FF58F7EEB639F69F14B2F52296` | CP | CP11/R20 signature regression |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1TransitionCoverageTests.cs` | `94AFB543F6228A8A7DF4AE53D684279A64A4A2D8186CDB182F4F19F30D2FB1F8` | CP | CP11/R20 signature regression |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs` | `67A981401789C014DCF73E4F5BC7A4F18AEC955E2E35F9F3E42093616E9EDA4A` | CP | CP11/R20 signature regression |
| S | M | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA2ClientServerAcceptanceTests.cs` | `3840C2140D7077287BA142650FF225CB3C8825A8C1C40EDFDFAD45DBC14842B1` | CP | CP11/R20 signature regression |
| S | N | `src/TagEkyc.Application/RawExport/RawExportCaptureAcceptancePolicy.cs` | `ACTUAL_MISSING` | AR | A3_AcceptancePolicyIsConfiguredAndReplayUsesStoredVersion |
| S | N | `src/TagEkyc.Infrastructure/RawExport/ConfiguredRawExportCaptureAcceptancePolicyProvider.cs` | `ACTUAL_MISSING` | AR | A3_AcceptancePolicyIsConfiguredAndReplayUsesStoredVersion |
| S | M | `src/TagEkyc.Infrastructure/RawExport/RawExportR2Contracts.cs` | `E3D7EF8BBC55F381D7465530422556C4E9F21BC2294A272EB0F0123043592E4C` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |
| S | M | `src/TagEkyc.Infrastructure/RawExport/RawExportR2FramedCiphertextStream.cs` | `BCCBE6AB26C3797E3906CD54ECEC24C0302C1B92222D5FC17B9D8CCD058C9990` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |
| S | M | `src/TagEkyc.Infrastructure/RawExport/RawExportR2EncryptionOrchestrator.cs` | `B2FF9A3606D269308F2CDCD1EF7C95D43067CD53C7D42DA5727CFF54D575860D` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |
| S | M | `src/TagEkyc.Infrastructure/RawExport/RawExportR2Repository.cs` | `7BE40AC01FE99E5A74B8219258B450C76632906205A06C2D429C7F2514674BF9` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |
| S | M | `src/TagEkyc.Infrastructure/Persistence/Entities/RawExportSourceEncryptionAttemptRow.cs` | `DC0F1DF21AF67EB180775CED4451D138B27618FDD77FDCB5DAD39B4D8A0EE84B` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |
| S | N | `src/TagEkyc.Infrastructure/RawExport/RawExportR2TerminalIntentRecorder.cs` | `ACTUAL_MISSING` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |
| S | N | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `ACTUAL_MISSING` | BR | BP17–BP21/R2 terminal, same-owner and no-provider-start recovery |

## 2. Dependency and representation fence

- The new broker project is explicitly built by its path, not added to TagEkyc.sln in this candidate. Its exact three ProjectReferences/no packages are in BR §8. Existing production .csproj files are NOT mutation targets.
- The sole existing project mutation is Server IntegrationTests.csproj: add `EnableCaptureAgentA3Acceptance` default false; compile the new cross-repo A3 test only under explicit true; use the existing Agent.Client reference when A2 **or** A3 acceptance is true; missing sibling is a build error. No new package/reference version. Enabling A3 does not suppress A2 tests. Standalone Server build keeps both acceptance switches false and must work without the sibling.
- The A3 runner sets both `EnableCaptureAgentA2Acceptance=true` and `EnableCaptureAgentA3Acceptance=true`, requires named A3 tests discovered/executed, no new skips, and reports standalone suites separately from opt-in acceptance. A green ordinary suite is never labelled A3 acceptance. The current A2 runner remains unchanged.
- New migration pair is exactly `20260913120000_Tip88C1C6BA3RetainedIngressComposition`; all SQL/ACL/constraint changes reside in its Up/Down. Do not edit historical migrations. Generated Designer/model snapshot must come from the actual final model, with matching body constraints; not a copied old Designer.
- No .gitattributes, git configuration, blanket LF canonicalization, lockfile or dependency-version mutation is authorized. Raw hashes are full 64-character comparisons. At later separately authorized landing, export exact indexes and check both Git blob/working representations and text-diff closure; no automatic old-or-new SHA acceptance.
- No unrelated dirty file is absorbed. Existing C3/C5 historical migration working changes remain excluded. Reading those current bytes does not grant permission to modify/stage them.
- An implementation-needed path omitted here is a bounded allowlist defect to correct before mutation, not an excuse to silently edit it. Fixes inside a subsequently granted row are FIX + RETEST without another design decision.
- Current docs-only output is this inventory, parent v0.5, its four contract companions and review ledger. No source file from this table was changed by drafting.

## 3. Mechanical joins before granting implementation

Every M row must match raw SHA exactly and belong to the expected repository; every N row must still be absent before first implementation write. Every new contract implementation type must have a row; every path must have an owner and proof. The migration manifest must enumerate all new/replaced function signatures and effective EXECUTE owners, not count function names only. Every changed canonical A1 table/signature/shape must be reflected in the coordinated source/test change.

R20 direct-call regression callers above must be changed to the exact successor15 signature with explicit authenticated fixture PrincipalId and NULL ConsentBindingId/profile for non-retained controls, setting the correct actor context. Update old signature/ACL expectations without deleting tests, weakening lifecycle/secret-once checks or using an empty principal. The A2 acceptance runner remains required and enabled. The two AR protected-table bridges are installed only by the A3 migration and included in the live/readiness function manifest: raw_export_accept_runtime_evidence(uuid,uuid,uuid,text,integer) and raw_export_select_runtime_sources_on_completion(uuid,uuid,uuid). No direct-table privilege is introduced for EF convenience.

A source freeze is not an instruction to keep pre-implementation hashes after intentional implementation. At as-built, record reviewed successor bytes and their diff to this frozen baseline. Fresh unrelated drift needs review/rebind; known implementation output is reviewed as candidate, not auto-accepted.

RE01 `raw_export_reenter_retained_source(uuid,uuid,bigint,bigint,bigint,uuid,integer,integer,integer)` is a new internal helper in the same proposed A3 migration, callable only by the deployer-owned B-C body, with no application/broker EXECUTE. BP20 lives in the already-listed new R2TerminalProjectionTests file. The CP04 common retained session-prefix changes are forward replacements of its finite existing callable list, not edits to the historical migrations that define them. Preserve their grants and legacy branches; the legacy terminal-outcome helper rejects retained attempts so it cannot bypass TI02. The two newly pinned key/object migrations below are read-only evidence for that lock census. No additional product file is authorized by this clarification.

## 4. Additional consumed read-only source freeze

The exact outer C3 `raw_export_begin_recipient_package_delivery_stream(uuid,uuid,uuid,uuid,bytea)` is also a forward body-replacement target under CP04; the historical migration source below is read-only, not permission to edit it. NPS01 `raw_export_terminate_retained_r2_before_provider_start(uuid,uuid,bigint,bigint)` is the one additional reconciler-only SD callable in the proposed A3 migration/manifest; no new source file, table or public route. RE01's derived ReservationExpiresAtUtc guard amendment and NPS01 proofs remain in the existing proposed migration/terminal-projection test rows.

These exact source bytes support the reused transition/provider semantics. They are **not** mutation targets; inherited parent §10 and M rows above complete the source set. A changed source requires bounded review/rebind; a matching hash does not certify code behavior.

| Server-relative read-only path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260819120000_Tip88C1C3AuthenticatedPackageDelivery.cs` | `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260804120000_Tip88C1B2DurableObjectCustody.cs` | `24EB721704EC78C1B925A2E761B2ADF1FF157A3BE1BED957094C30582077AD99` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260802105416_Tip88C1B2DurableKeyProd.cs` | `34ACF5CDE28ECDC181EAB1E500B7688516FC1079B159B5371322F78108150277` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260807120000_Tip88C1B2R2DurableCustodyEncryption.cs` | `78A55EAAE3A043D14CCCA8BAEF846B59A418426AEF2CF99FE84DB367A4BF6BD7` |
| `src/TagEkyc.Infrastructure/Persistence/EfRawExportSubjectConsentRepository.cs` | `EF5D0E625499359B4825C224AFDA685DBDBA6B3869742135F277974CD103D576` |
| `src/TagEkyc.Domain/RawExportPolicyCatalog.cs` | `E6854EC635F53260569E2E560A11984FEC9CA60135DFBB581BD677C7062E8DC8` |
| `src/TagEkyc.Infrastructure/Persistence/Entities/VerificationSessionRow.cs` | `1F4CD6A8AA56B3D90C1E548EDF4948C65CCF91C46F69C96E5EF0741AFCF561E4` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260720022629_Tip88B2SubjectExportConsent.cs` | `AF5B90EA6A7A4956CCD88F8EB4FDACC34CCBFCF5FCBAA66D3221F08490CFA243` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260731045718_Tip88C1B2AuthoritySnapshotLifecycle.cs` | `DB0902CBEE0E5AAC6B67A7D64678F968FBE849948EACEDD3FF1EA965A21D39F7` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260731082733_Tip88C1B2CoreNewCandidate.cs` | `747A4394D65B7DE2DC26208BCBD6EE63229A3C70D9C972A7A491E9D2802ED868` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260731130919_Tip88C1B2BetaExistingCandidates.cs` | `CB131F4FC8E79B7CD989378E185BF10D960FEA4CCEADD5EBA6384212E0FD13F3` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260810120000_Tip88C1B2R3VerifiedCiphertextStaging.cs` | `B0FB2841AEC86FE70847DAAFC42065403E99826550C02C7E9A512E2C78A10D96` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260812120000_Tip88C1B2R4R6SourceFinalization.cs` | `06BF054E6333F40CFF5BBD6AEC88FE3819FD3C0146F2AE2DD5C87A307E9684CB` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260815120000_Tip88C1C1ResolverAssembly.cs` | `125EC8B98ED3F95B2504DA59A548C32814A61C497391E29138D4288A8FC5FBEF` |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260904154848_Tip88C1C6AProductionAuthorityDeliveryBarrier.cs` | `3D4D0106E5CBD568CF3053CA1BFBB529B55070CE975D9561E9F466F9D863F7CF` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportR2CompletionVerifier.cs` | `58AF7E6639382DDF292F080C6E32868F26B83F78ABB86BEF74538391FD4E62A6` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportR3StagingService.cs` | `5D047CF6A4C468DBCE44940E5BC002F7436DEDA6B2D22C041D3B5F5688B2F6BF` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationServices.cs` | `52E12C058CB4294359E2DA7F2836A316E7587D5D07F51C21E8196F508AF15B0B` |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyServiceCollectionExtensions.cs` | `14AF64BADB507396D1705ADB4E29653D8C2B0521A41857F00A7F0CBC2B0A46A2` |
| `src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyReadinessValidator.cs` | `9E43F7A6DF82A4B2FEC87CB4EC46EA30F0F5E4C12A315FBDDAD8449102ADB444` |
| `src/TagEkyc.Infrastructure/RawExport/ContentCommitmentServiceCollectionExtensions.cs` | `6648387D2D48F810A89A0EBEFF9220A9D2266592B0A256EA86A59491B7AE907D` |
| `src/TagEkyc.Infrastructure/RawExport/CustodyProfileProvider.cs` | `E69C8DB733782B673A5816F2CDFC77855D08020DA7B9B6620F12822A8536EB3F` |
| `src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyServiceCollectionExtensions.cs` | `5024202DE94772302BC1ABCBFC8E598FE7BBECF5E9A912D1A948F7F859C358A4` |
| `src/TagEkyc.Api/CaptureRuntimeEndpoints.cs` | `B6DF0DB351DD3428E6474E0C0DE05F832F6C840B90C79BA5660F6690E95557EB` |
| `src/TagEkyc.Application/VerificationSessions/VerificationEvidenceApplicationService.cs` | `9313155659C4E4F1E90E327878384FCD8F1F89456298DE75F2E79029AD99AF8C` |
| `src/TagEkyc.Infrastructure/CaptureRuntime/CaptureRuntimeAppendAuthority.cs` | `6966534F2EF435AB1906A6EFFD886AAD3C65746BC3585E9EC33D731CA6313CCF` |
| `src/TagEkyc.Infrastructure/Persistence/EfAppendIdempotencyBoundary.cs` | `071480B63BB207934DD89CBDD8D791606FFF790B860171855E7BD7A9E20F0679` |
| `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_planning_brief.md` | `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC` |

## Changelog

- v0.5: exact proposed two-repo file set with full raw hashes, explicit missing paths, isolated broker graph and mandatory opt-in acceptance distinction.
