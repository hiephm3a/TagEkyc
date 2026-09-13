# TIP-88C1-C6B-A2 — Windows Agent as-built correction candidate

Version: 0.2
Date: 2026-09-13
Status: READY_FOR_INDEPENDENT_A2_IMPLEMENTATION_REVIEW
Authority: existing Homeowner bounded implementation grant on Dispatch v0.4
Dispatch SHA-256: `93D06034E8A69F2F13B9CA8BFB4F407E4C7AE6E71FA8AB8B6A435E23E5861C8D`
Scope: Windows x64, synthetic/non-patient only. No A3/A4/production, stage, commit or push.

## 1. Predecessor and bounded correction

Preserved predecessor: `tip_88c1_c6b_a2_as_built.md`, raw SHA-256
`E3C36842588CB698B626E917D65453A3FB9CFC4CD5FB29AE5DDC38952E2E938D`.
Its bytes and historical evidence remain unchanged. This successor replaces its current-candidate source/evidence claims, not the ratified dispatch. No new planning/RRI or semantic authority was created.

External review inputs: CC attachment `28292ecc-38e7-488a-8021-dd10f942b2e2/pasted-text.txt` and GPT attachment `d012e9d7-1c2c-42ea-b0fd-c591fd0444b7/pasted-text.txt`.

| Finding | Applied product/test correction | Disposition supported by execution |
| --- | --- | --- |
| GPT F1: cancellation terminalizes recoverable pending lineage early | Removed `CancelUnacceptedHandoff` and its cancellation catch. Close, disposal and caller cancellation cancel/drain transient owners; they do not terminalize nonsecret pending lineage before its original deadline. Existing deadline terminalizer remains the only deadline transition. | Six close/dispose/caller × committed/not-committed cases preserve the entire original pending record, clear outbound bytes and restart R22-first. Known-binding/deadline case preserves binding and forbids late revival. Old behavior mutation gives six RED cases. |
| GPT F2: SessionId accepts representations rejected by A1 binding equality | Coordinator requires nonempty lowercase UUID-N with `Guid.TryParseExact(..., "N", ...)` plus ordinal equality to `ToString("N")`. Reject before handoff/delegation/device. No silent normalization or new carrier. | Four cases: lowercase N succeeds; D, uppercase N and empty reject with EXTERNAL_SESSION_INCOMPLETE and zero handoff/reconcile/device/delegation. Old permissive parser gives exactly two RED cases. |
| CC: nine proof cases have undeclared PowerShell 7 dependency | Child CNG/journal fault probe compiles with the already-required dotnet SDK and runs in a separate dotnet process against exact production Client/Core/Contracts assemblies. Only junction creation uses full-system-path Windows PowerShell 5.1. | Key-custody suite 19/19; full Agent suite 262 PASS, zero failures, one unchanged manual skip with `pwsh` absent from child PATH. No skip, project, package or reference workaround. |

Only five source files differ from predecessor inventory: two product files (intake/coordinator), three test files (handoff/composition/key custody). All other 36 source entries are exact unchanged bytes.

### F1 exact recovery behavior

- Handler is deliberately uncooperative with cancellation: drain must wait until its actual 200/503 response is released, not until a test-generated cancellation exception.
- Close, Dispose and caller-token cancellation are tested independently. The caller case observes the pending caller continuation; it does not call Close to trigger the behavior being tested.
- The original PendingBind record, BindOperationId, capability identity and original deadline remain unchanged. HandoffAccepted and DelegationAttempted remain false. No premature HTTP 200 is observed.
- Fresh coordinator/intake exact redelivery calls R22 first. Found binding is reused without R21. Only exact typed absence permits one R21 with the original BindOperationId/capability/secret; no fresh operation identity is invented.
- Cancelled outbound request storage is zeroed before drain completes. Redelivery request storage is also zeroed; both ports are released.
- A separately seeded known server binding remains intact during cancellation. At the original pending deadline terminalization occurs; no later Accept/Begin can revive it or extend its deadline.
- Already accepted receipt-loss/delegation semantics remain unchanged: receipt is replayable, not execution authority; only durable DelegationAttempted CAS authorizes the one attempt.

### Test-process boundary

The fault child remains .NET 8, not PowerShell 5.1 CLR. The test uses the installed SDK's Roslyn compiler, runtime assemblies and exact production DLLs, then runs the generated probe through dotnet. Probe source stays inside the existing allowed test file; no checked-in project or package is added. Compiler failure is asserted, child time is bounded, timeout kills/waits the exact process, and the exact temporary directory is cleaned.

Named crash checkpoint exit code 97 remains asserted. The child observes secret zeroization before handoff acceptance and exits 96 on violation. Existing seven handoff-fault cases, four journal/CNG checkpoints in their existing fact, and junction negative still execute.

The predecessor's 252 PASS / one skip was the actual earlier TRX, not nine failures counted as PASS. That environment exposed bundled `pwsh`; CC's nine failures exposed an implicit test-tool prerequisite. Current execution removes that prerequisite and proves the suite runs without it.

## 2. Current executed validation

Root paths:
- Agent = `D:/Task/Remote Signing/TagEkyc.CaptureAgent`
- Server = `D:/Task/Remote Signing/TagEkyc`

All rows below are parsed from actual TRX; intentional mutation rows are RED evidence, not remaining candidate failures.

| Repo | Exact evidence path (relative to root) | Result | Raw SHA-256 |
| --- | --- | --- | --- |
| Agent | `TestResults/a2-final-correction-no-pwsh/3AVNN_LEARNPC_2026-09-13_10_30_24.trx` | 262 PASS / 0 failed / 263 total / 1 manual skip | `E7AD3444F5B9E101825C4DD33EDACD8B4C838D82B2B535F89250D99336FB209A` |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/TestResults/a2-keycustody-sdk-probe.trx` | 19 PASS / 0 failed / 19 total | `2C2FC01DAF27408D60C44E8F8827EEA133E62430254F5BE5A3A374DE2D0901F4` |
| Agent | `TestResults/a2-caller-drain-green/3AVNN_LEARNPC_2026-09-13_10_29_39.trx` | 6 PASS / 0 failed / 6 total | `6C0B9836E986ADE6D36CA44EEC2D01AE3B9D6805AB82EB8CC70FCF71A4A36E17` |
| Agent | `TestResults/a2-old-cancel-behavior-red/3AVNN_LEARNPC_2026-09-13_10_26_21.trx` | 0 PASS / 6 failed / 6 total | `BAADF8FC18CACBDC7545A622607251A4AF186C760942E1661E56CB19588A554D` |
| Agent | `TestResults/a2-correction-receipt-red/3AVNN_LEARNPC_2026-09-13_10_26_26.trx` | 0 PASS / 1 failed / 1 total | `BE93B3B65E2E8489D409F0099864F4637DCCD723D498630C524DAD4D824E6F7D` |
| Agent | `TestResults/a2-session-format-red/3AVNN_LEARNPC_2026-09-13_10_26_58.trx` | 2 PASS / 2 failed / 4 total | `58370CE28AFCC4631431554283FE3D8B62FBD15D8D449C13ECED59E8AF61EBC1` |
| Agent | `TestResults/a2-caller-token-red/3AVNN_LEARNPC_2026-09-13_10_30_20.trx` | 4 PASS / 2 failed / 6 total | `8FA715F671DA19D2F30A0873AEAC4EF8519C58891E49EDDCA2698A295795B188` |
| Server | `TestResults/a2-acceptance/3AVNN_LEARNPC_2026-09-13_10_30_53.trx` | 6 PASS / 0 failed / 6 total | `1DF74B2B9804E91BB08E787E00AEF6FB1FF6C0620A2CFBB87BFD5E417CB570E4` |
| Server | `TestResults/a2-correction-arch-final/3AVNN_LEARNPC_2026-09-13_10_36_10.trx` | 153 PASS / 0 failed / 153 total | `FC370A8072D18FBFB114B7D9B36090BFA1E9498232F98879803603168E5FFF0D` |

The final full Agent run was executed after all product mutations were restored and the caller-token proof masking was fixed. Child PATH was restricted to dotnet, Windows/System32, Windows, and WindowsPowerShell/v1.0; `Get-Command pwsh` was checked absent before running tests. The already-running orchestration shell does not supply a pwsh executable to test children.

Full Agent total/executed is 263/262. The only individual NotExecuted result remains
`Tip73AgentContractTests.Manual_opt_in_silent_face_orientation_smoke`.
It is an opt-in synthetic ONNX smoke test, not counted as PASS. There are no newly skipped tests.

Additional executed validation:
- Agent locked restore and full solution build: PASS, zero build errors.
- Server standalone solution build with locked restore: PASS, zero build errors.
- Actual A1 HTTP/TestServer/PostgreSQL 16 acceptance: fresh 6/6 PASS after final product correction.
- P13 isolated absent-sibling default build and explicit opt-in missing-sibling failure: PASS in acceptance runner.
- Acceptance isolated run ID: `b087c4661b004b3a88774f8ec7d10070`. Initial attempt found Docker stopped and ran no acceptance tests; Docker Desktop was started and the runner rerun successfully. Exact test-owned container was cleaned; final listing found no tagekyc-a2 containers. Shared Compose/database was not used.
- Server ArchTests: fresh 153/153 PASS. No claim of a fresh full server integration suite beyond the six A2 acceptance cases.
- Historical test warnings were not suppressed; the existing xUnit1031 warning in Tip88C1C6BCaptureAgentTests remains outside this correction.

## 3. Discriminating mutation runs and restoration

| Mutation actually executed | Expected/observed RED | Restored behavior |
| --- | --- | --- |
| Reintroduce old cancellation helper and cancellation catch | All six cancellation cases fail exact pending-versus-terminal assertions | Nonsecret pending lineage preserved until original deadline |
| Write HTTP 200 before durable acceptance | Receipt-first-byte test fails because early byte is received, not because fixture setup fails | Secret zeroization and durable acceptance precede receipt |
| Restore permissive Guid.TryParse | D and uppercase-N fail the negative controls; lowercase N and empty controls remain green | Exact lowercase UUID-N required |
| Remove linking to caller cancellation token | Both caller-cancellation cases fail; four Close/Dispose controls remain green | Caller cancellation independently cancels/drains |

After all mutations were removed, production coordinator SHA is
`73EBE5B435649EE5E903F7404181F885969B2DA41CB2D5F3B3036B8770D70FC5`
and intake SHA is
`5259EC299D3E96AC917AC2D5994E893CC4B062D43F434A8D5034F456A6B63428`.
Final full suite is GREEN on these exact bytes. No temporary mutation/helper/cancellation-flag residue remains.

## 4. Requirement/proof coverage

The existing composed coverage below is rerun by the final full Agent suite and fresh A1 acceptance; §1–§3 add the corrected F1/F2 and test-tool discrimination. It is not a deployed end-to-end or physical hardware certification claim.

| ID | Executed evidence / owner | Discriminating negative or failure |
| --- | --- | --- |
| P01 | KeyCustodyTests.Cng_CurrentUserNonExportable_ReopensExactKey; ProcessFault_ReopensCngAndRecoversAtomicJournal; Cng_DifferentWindowsSecurityPrincipalCannotOpenCurrentUserKey | Independent ECDSA verifier; private export denied; wrong thumbprint, existing exportable key and missing committed locator rejected; anonymous Windows principal cannot open the key, owner reopens afterward |
| P02 | Server Enroll_LostResponse_ReplaysExactBodyAndKeepsCandidate; Agent ceremony equivalent and actual Host stdin tests | Real committed R05 response dropped; same candidate/body/idempotency and durable-before-ACK; malformed/duplicate/trailing/stdin deadline input denied without secret echo |
| P03 | Server Rotate_SuccessorReplay_RetainsExactBody; CeremonyTests.Rotation_ReserveBeforeCreate_AndDeleteOnlyAfterConfirmedLineage | Real predecessor response dropped, explicit successor replay exact body and stable CredentialId; no early predecessor deletion or second auth attempt |
| P04 | WireTests.Configuration_FullShapeFreshnessAndIdentity and Configuration_304RequiresExactValidatorAndDoesNotRenewHorizon; real A1 configuration response in server fixture | Wrong agent/shape/expired/validator rejected; A/5 to A/3, A/10 to B/1 and same tuple changed projection accepted; 304 cannot renew validity |
| P05 | Server Bind_RestartReconcile_UsesNonsecretJournal; HandoffTests.Restart_ReconcileFirst_OnlyExactTypedAbsencePermitsSameOperationReentry; child-fault and actual caller proofs | Exact typed absence alone permits same-operation re-entry; wrong redelivery/denial/recovery never allocates a new bind or stores capability secret |
| P06 | Server RuntimeAppend_UsesBindingAndNoApiKey; WireTests.RuntimeAppend_ActualOrchestratorKeepsRetryIdentityAndTerminalReceipt | All six real slots: 3 artifacts, 3 evidence results, 6 idempotency rows, 6 acknowledged journal entries, 10 nonces. Remove Face/Liveness decision basis and re-sign changed body with real CNG: exact domain denial; restore original request: accepted |
| P07 | Server Crt1_ExactBytes_ServerVerifierAccepts; WireTests.Crt1_ExactBytes_LocalGoldenAndSignatureMutations | All eleven lines, final LF and body binding; aliased nonce signed over the alias bytes denied by canonical parser (400), foreign API-key header denied (403), no new nonce on denial |
| P08 | HandoffTests.DualLoopback_OneSlot_BoundedAndZeroed; ReceiptFirstByte_WaitsForDurableAcceptanceAndZeroization; PeerResetBeforeReceipt_PreservesAcceptanceAndExactRedelivery | Both sockets connected before competing requests; one receipt/bind/CAS winner. Acceptance write blocked: zero outbound backing and no first response byte. Peer RST before receipt preserves accepted state; exact redelivery does not bind/execute again |
| P09 | CompositionTests.ActualHostAndWpfComposition_AcceptedRestartAndFirstAppendLossDoNotReacquire (Host and WPF cases); AcceptedRestart_ReconcilesExactBindingBeforeOneDelegation; KeyCustody child faults | Real Host RunAsync and WPF Build on STA use real runtime/coordinator/orchestrator with synthetic devices/HTTP. First append returns synthetic 503 and leaves submission uncertainty; new runtime cannot reacquire. Accepted false yields no device, accepted true/unattempted one CAS, attempted restart no second device |
| P10 | KeyCustodyTests.ProcessFault_ReopensCngAndRecoversAtomicJournal; ProcessFault_HandoffAcceptanceAndDelegationRecoverExactTombstone; Journal_ReparseAncestorRejectedBeforeTouchingTarget | Child exits without CLR cleanup before write, before/after flush/replace. Exact old/new journal recovers; reparse/broad ACL/checksum/shape rejected; allocated exact key IDs are cleaned, not prefix-swept |
| P11 | CeremonyTests.ActualHost_ManagedRejectsEachClientSecretWithoutJournalOrLeak; Tip75WpfCaptureAppTests.A2_ManagedWpfFactory_RejectsEachClientSecretBeforeLoggerOrRuntime; existing production gate tests | Each of three API-key env variables separately rejects Managed before runtime/logger/device; no journal creation or secret output; RawVault/production activation remains rejected |
| P12 | ReceiptFirstByte proof; child handoff/append faults; HandoffTests cancellation/body/peer-reset tests; secret serialization tests; actual Host stdout and actual caller buffer assertions | Capability body/secret cleared before acceptance; no secret field in journal; abrupt child cannot persist partial acceptance; cancellation drains owners/sockets. First append checkpoint leaves at most one exact unresolved identity, no reconstructed BIO or reacquisition |
| P13 | Invoke-CaptureAgentA2Acceptance.ps1.AcceptanceGraph_DefaultStandalone_OptInRequired | Absent sibling with property false succeeds; same checkout explicit property true fails before execution; present sibling runs actual six tests |

Additional exact test methods:
- `HandoffTests.CancellationBeforeAcceptance_DrainsOwnersBeforeCloseCompletes`: six independent cancellation/recovery cases.
- `HandoffTests.Cancellation_KnownBindingSurvivesUntilOriginalDeadline_NoLateRevival`: frozen lineage and deadline control.
- `CompositionTests.SessionObservation_RequiresLowercaseUuidNBeforeHandoffOrDevice`: four exact-format cases.

## 5. Bounded internal review

Existing contract and free-adversarial reviewer agents reviewed the bounded correction. A first follow-up caught that the caller-token test also invoked explicit Close, masking the linked-token behavior. That proof was corrected; dropping caller-token linkage was then executed and produced exactly the two intended failures. Final contract review reported no remaining bounded defect.

This review does not replace external CC/GPT re-review. Parent executed the listed tests; reviewers' code inspection is not reported as an independent execution. No artificial ten-round count or new planning/RRI is claimed.

## 6. Source freeze and mutation boundaries

Server HEAD: `4fafa17a43ad38f46c2e0c69d570f65b8d01abfe`.
Agent HEAD: `b193f6316e13a82fb2f9f985f68500feae194cfa`.
Both indexes: zero staged and zero conflicted. No commit/push.

This correction modifies two Agent product files and three existing allowed Agent test files, plus this successor report. No Server product/test/runner change, Agent csproj/package/reference change, schema/config/deployment change or new product path was made for the correction.
The earlier authorized Server test csproj/acceptance class/runner remain exact predecessor bytes. Pre-existing unrelated dirty files are preserved.
The earlier allowed Crypto lock correction remains unchanged; no new dependency pins are introduced.
The following complete 41-row inventory is freshly hashed in full, not prefix-compared.

| Repo | Exact relative path | Current raw SHA-256 | Classification |
| --- | --- | --- | --- |
| Agent | `src/TagEkyc.CaptureAgent.Client/TagEkycHttpClient.cs` | `03771FE5640E2FD057B445F64D0A3C1606523C0A3435B2429F28ABAE05184A2F` | Authorized mutation of consumed candidate |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureAgentPorts.cs` | `97A479CDF490261236F62627EE19141643866E1BD5A0C9D0CDCF0C463825C405` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureAgentProductionConfigGate.cs` | `E4A6EC64CB48740A774FE9A7B05FB0E105CDA344B3F1FCF40F0A0D9456CABB70` | Authorized mutation of consumed candidate |
| Agent | `src/TagEkyc.CaptureAgent.Core/RawExportIngressContracts.cs` | `6C613E7317FF78F902B4254941381AB58670B68386F1E9CB6C1D727A4F4262A1` | Authorized mutation of consumed candidate |
| Agent | `src/TagEkyc.CaptureAgent.Core/RetainedRawBufferCapacity.cs` | `8200232C63A6DCB6DEFF4E60F8427B7E8D94E18663A360DCF795CD209D1DA60A` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Core/SensitiveByteBuffer.cs` | `B75862BE5AEF0DD7464E66D81E06AF6927233B2B5EF3EF984604B08F73616293` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs` | `112307860A03A2FEC9A540D379C18FF701F00AFA6BCCA51B3D864E01A5D28EBD` | Authorized mutation of consumed candidate |
| Agent | `src/TagEkyc.CaptureAgent.Host/Program.cs` | `60DAE1689787D7161886CD662B096A5E090B4017E3903592FF60C8D9471B2194` | Authorized mutation of consumed candidate |
| Agent | `src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs` | `68DDE518B765DB8921B57309783DC52CC0B95322123699D5CCAB88858C7FE444` | Authorized mutation of consumed candidate |
| Agent | `src/TagEkyc.CaptureAgent.Ui/CaptureAgentViewModel.cs` | `6BC53EFB94ADE973684C0CA984DC045064B2E6CF5B117C43F7CE93FCBF0A7185` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Client/TagEkyc.CaptureAgent.Client.csproj` | `D88A723B4061DF4CD9532818E0A3901C6611BE7E7EAAC68C04AD7EE3F611E5FC` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Core/TagEkyc.CaptureAgent.Core.csproj` | `B6BBBE1099C352C044040993D09DC0F004057307129E27D67642077B772BF890` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Host/TagEkyc.CaptureAgent.Host.csproj` | `DB39A36063784AE5A36CDE8B90DF2E3B65B68F847A68D2DA0CF9500F7DC5D6C1` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Ui/TagEkyc.CaptureAgent.Ui.csproj` | `B1A974DAAEF7B1C490FCEECF8742CC2A09AA6B31256393C955E23E3E80B73478` | Unchanged from dispatch freeze |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/TagEkyc.CaptureAgent.Tests.csproj` | `37C954258F96A684144F7DD4EB4F1515963109FB135AF0AD9ECA7DBA1C6E8ADB` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Crypto/TagEkyc.CaptureAgent.Crypto.csproj` | `52FC4CAB3907943A878E47274A994C9A79327BB185C7A91B5F266B2BADEEC4FF` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Crypto/packages.lock.json` | `B5C9B7D7787749A9DEDD59B4E8C3130109E03FAEA4F329751D2F38D416500B30` | Authorized mutation of consumed candidate |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BCaptureAgentTests.cs` | `68CB79C702765CE2867F93170D38F0FA671C3B1A0DAC6E78035D50C723836C29` | Authorized mutation of consumed candidate |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip74HttpClientTests.cs` | `8386970AD78CB0B93001E8E19CEC395F3ACE92AD85861E282989283CCA29AAA9` | Unchanged from dispatch freeze |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip74CaptureAgentHostTests.cs` | `BCDDA84A5B97C3048C4CCD606E45072C43DABCDE45CDE9D890FA8590869B769A` | Unchanged from dispatch freeze |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip75WpfCaptureAppTests.cs` | `BCB5C81FB318DAA778990B4AC34794570884F935421C30C363846AFC1791B154` | Authorized mutation of consumed candidate |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip85CaptureAgentPackagingTests.cs` | `0E8A09C674FAE72CE3766B8D164202A485B4FCF412D2070FEA97D76F8FDA983A` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Ui/CaptureAgentRunLogger.cs` | `C033FA5B49FFAB9E5F02AD9BA1F198B4CD47F750DC6D1B269DF6CD9E5011C624` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureAgentReceiptWriter.cs` | `5A2225730E480FDC8079CC4BF60079162915694CC9412C507C8A9E618A331CD3` | Unchanged from dispatch freeze |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureCapabilityLoopbackIntake.cs` | `5259EC299D3E96AC917AC2D5994E893CC4B062D43F434A8D5034F456A6B63428` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeAgentCommands.cs` | `110E1C36350FF231900C496B872653275132627344E33C41B3AC8FB0685F1E7F` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeHttpClient.cs` | `21CFBFC1CDD1E0DFE2250D440AF3654EB9F07767627575E5A97214D2AA5386FF` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeWireCodec.cs` | `2D5273DEB3C5C6935E8BD3DD07B1FB630F861D3992471FEDC5E69698BFD7BFAB` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/WindowsCaptureRuntimeJournal.cs` | `204C945F032C0AFAD1D533F58438CFCE89ED62D0B772BA5F0974438417C7DBFF` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/WindowsCaptureRuntimeKeyStore.cs` | `2095427165836212E4317D4B77E960BD799BAF972A2F7CE4FC7F7EFBD3343B13` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureRuntimeAgentCoordinator.cs` | `73EBE5B435649EE5E903F7404181F885969B2DA41CB2D5F3B3036B8770D70FC5` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureRuntimeAgentPorts.cs` | `212F5FF3C8DC7644E7CCCF40C3E4382A8174E49A97BD17E36554A742B86F200C` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2CeremonyTests.cs` | `2190C01F606C079E093BF0DE8BB1E74E57C3BAC486C81E71F3C755E635A512D7` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2CompositionTests.cs` | `4795CEAB95F282E073560AE651DEB9D03D5774DD5B5481F2908A0EA69DB9ADD3` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2HandoffTests.cs` | `EC4171E3F432BA5E17E7CF0EE92499B9CF83A9E1D8F7E1E99655616E9B5FC658` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2KeyCustodyTests.cs` | `4628C148724A740F67C780C2BC76F10193FD0D4482C3497E68B2B6B1390863A3` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2WireTests.cs` | `64AF452F7C1245BD83C2C8ACD19E771BDDFE2923FE54FA92997DB9DDDCDBB1D3` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Ui/MainWindow.xaml.cs` | `D40898C259D4B80089C6E9D45FE4A3D7A88A6F13BF87F45EBFE7EBAE066929BD` | Read-only; matches dispatch caller pin |
| Server | `tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj` | `162E91C273302159D5A0B5E81BFF13F6236265AB37E088544192B6C7ECB293D6` | A2 implementation/test surface |
| Server | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA2ClientServerAcceptanceTests.cs` | `3840C2140D7077287BA142650FF225CB3C8825A8C1C40EDFDFAD45DBC14842B1` | A2 implementation/test surface |
| Server | `tools/Invoke-CaptureAgentA2Acceptance.ps1` | `7340246BA9020414E708C97B8DC8421AFA72B22650421BB73B06719671193570` | A2 implementation/test surface |

## 7. Explicit limits and next gate

- This is an as-built review candidate, not Homeowner implementation ratification or permission to land.
- No A3 retained-body/provider/broker execution, A4 installer/launcher/cutover, production activation, mTLS hardening or real patient capture.
- Existing SignFlow backend channel remains an input assumption; TagEkyc tests its own loopback boundary, not SignFlow governance.
- CNG proof is Windows x64 CurrentUser Microsoft Software KSP, including actual anonymous-principal denial; not TPM/hardware certification or a separately provisioned user-profile deployment test.
- Actual Host method and WPF composition execute in the test process; separate child probes provide abrupt process-death evidence at production journal operations. This is not a claim of killing the complete WPF UI at every checkpoint or physical power-loss testing.
- A1 HTTP acceptance uses actual TestServer/DI/gateway/PostgreSQL, not a deployed reverse-proxy/mTLS network.
- Known test-owned key IDs are deleted and absence-asserted by final acceptance fixture. No global key-store sweep was performed or claimed; unrelated/pre-existing keys were never removed.
- Full Agent suite ran with the one explicit manual skip above. A2 does not certify native biometric accuracy, field hardware or unattended production.
- Next: independent CC/GPT review of this source/evidence candidate. Any ordinary defect remains FIX + RETEST under the existing bounded authority; no fresh implementation permission is needed for such a fix.

