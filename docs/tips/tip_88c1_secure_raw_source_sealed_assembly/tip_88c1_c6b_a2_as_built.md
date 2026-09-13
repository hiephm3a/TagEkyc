# TIP-88C1-C6B-A2 — Windows Agent as-built review candidate

Version: 0.1
Date: 2026-09-13
Status: IMPLEMENTED REVIEW CANDIDATE — external as-built review pending
Authority: Homeowner bounded implementation grant on Dispatch v0.4
Dispatch SHA-256: `93D06034E8A69F2F13B9CA8BFB4F407E4C7AE6E71FA8AB8B6A435E23E5861C8D`
Scope: Windows x64, synthetic/non-patient only. No A3/A4/production, stage, commit or push.

## Changelog

- v0.1 records executed A2 implementation, final source bytes, positive/negative evidence and limitations. It does not modify, supersede or self-ratify the dispatch.
- Includes final receipt-order proof: GREEN, intentional early-receipt mutation RED, exact restoration, GREEN. No mutation remains in production source.

## 1. Outcome versus intent

| Intent | As-built result | Boundary |
| --- | --- | --- |
| Installation-owned identity, not distributed Client API key | Windows CurrentUser Software KSP P-256 non-exportable keys; exact locator journal; ENROLL1/ROTATE1/CRT1 clients | No new platform accounts, production credentials or enrollment issuer |
| Durable ceremony/recovery | Candidate reserved before creation; exact pending enrollment/rotation bytes and lineage; successor recovery; no secret stored | Server remains A1 authority; no fallback authentication |
| Server-owned configuration | Closed 15-field projection, opaque ETag, 304 without freshness extension, startup/reconnect/polling and fail-closed admission | Same published tuple may legitimately resolve differently through A1 overrides |
| Capability handoff, then one capture attempt | One-shot dual-loopback intake; confirmed binding, secret clearing, durable HandoffAccepted; replayable receipt; one durable DelegationAttempted CAS | Receipt delivery is not execution authority; post-CAS crash may lose attempt |
| Actual Host and WPF consume one runtime | Managed factory gates, real session/coordinator and existing CaptureAgentOrchestrator; no device construction before admission | Embedded SDK implementation, deployment launcher and A3 ingress are not built |
| Existing artifact/evidence submission through runtime binding | R24/R25 installation-only client; frozen six-slot idempotency, preserved decision bases and receipt mapping | No legacy producer API-key fallback; no raw-retention upload added |
| Test-only cross-repository join | Server acceptance opt-in property; ordinary server solution remains standalone | No Agent project/package/reference edits; no product reverse dependency |

## 2. Executed validation

All TRX below were parsed directly. Root paths:
- Agent = `D:/Task/Remote Signing/TagEkyc.CaptureAgent`
- Server = `D:/Task/Remote Signing/TagEkyc`

| Repo | Exact evidence path (relative to root) | Result | Raw SHA-256 |
| --- | --- | --- | --- |
| Agent | `TestResults/a2-full-exact-final/3AVNN_LEARNPC_2026-09-13_03_10_04.trx` | 252/253; failed 0; manual skip 1 | `424A3C95F938F32913C610F1AE69D10BDCBE5E6A3913E52D85F8E7E37DE9F6BA` |
| Agent | `TestResults/a2-crash-final/3AVNN_LEARNPC_2026-09-13_03_04_30.trx` | 8/8; failed 0 | `FF440B09D76BC27B392D9265908F8301A4C5022BC5DB1DB34F1795ABD6A5F12C` |
| Agent | `TestResults/a2-receipt-order/3AVNN_LEARNPC_2026-09-13_03_03_18.trx` | 1/1; failed 0 | `2F91C5902CD93474A03593585409E1AAEA03A134A591D99D660B3417139167BA` |
| Agent | `TestResults/a2-receipt-order-red/3AVNN_LEARNPC_2026-09-13_03_03_46.trx` | 0/1; failed 1 | `D999788EBF79F7A1473C5622728DFA3DAFFC186A0F8BFCD5E836CC0C1A7EDA74` |
| Server | `TestResults/a2-acceptance/3AVNN_LEARNPC_2026-09-13_02_57_31.trx` | 6/6; failed 0 | `F00725BD18F3C674029D1477BFD96A3B8E8DB8047C526BA136423A23A5F70189` |
| Server | `TestResults/a2-arch-final/3AVNN_LEARNPC_2026-09-13_03_01_08.trx` | 153/153; failed 0 | `EF194C63BA8C13DE7384E96A6FBDE12DC9D2455EA8178B6E4E65558B74B90771` |

The intentional RED is expected evidence, not a remaining failing candidate test.
The full Agent run contains exactly one NotExecuted result:
`Tip73AgentContractTests.Manual_opt_in_silent_face_orientation_smoke`.
It requires explicitly supplied synthetic ONNX orientation material; it was not enabled or counted as PASS.
TRX aggregate total/executed is 253/252; the individual NotExecuted result is authoritative for the one skip.

Additional commands actually executed:
- Agent `dotnet restore TagEkyc.CaptureAgent.sln --locked-mode`: PASS.
- Agent `dotnet build TagEkyc.CaptureAgent.sln --no-restore`: PASS, 0 errors.
- Server `dotnet build TagEkyc.sln -p:RestoreLockedMode=true` with acceptance property absent: PASS, 0 errors. Historical EF1002 test warnings remain.
- Server `pwsh -NoProfile -File tools/Invoke-CaptureAgentA2Acceptance.ps1`: PASS, six actual A1 HTTP/TestServer + PostgreSQL 16 acceptance tests.
- P13 runner: isolated baseline checkout with current two test-project files, no sibling Agent; default solution build PASS, explicit opt-in restore fails with the required missing-sibling error. No silent test skip.
- Agent test compilation retains pre-existing xUnit1031 warning in Tip88C1C6BCaptureAgentTests:85; not suppressed.
- Final real-server run owned isolated container run `5bc9da749dc9412b90393683310c3727`; runner cleaned its exact labeled container. Final docker listing found no tagekyc-a2 containers. Shared Compose/database was not used.

Acceptance evidence predates only final Agent test additions, not product changes. Final Agent full run includes those additions.
No claim of a fresh complete server integration suite: only the six A2 real-boundary tests plus 153 ArchTests were run here.

## 3. Requirement / proof / RED mapping

Class prefixes below are `Tip88C1C6BA2`; server acceptance is `ClientServerAcceptanceTests`.
Proofs compose at the named boundaries; they are not mislabeled as a deployed end-to-end system.

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

The child-fault matrix executes Accept BeforeWrite / AfterFlushBeforeReplace / AfterReplace;
Delegate BeforeWrite / AfterReplace; Append BeforeWrite / AfterReplace.
After committed append reservation, lost in-memory payload returns SUBMISSION_STATE_UNKNOWN without altering journal bytes.
A separate existing child test covers all four journal write checkpoints and reopens/signs with the actual persisted CNG key.

Receipt mutation executed: inserted an early HTTP 200 write before AcceptHandoff.
The no-first-byte assertion failed with “No exception was thrown”, not a generic setup failure.
Removed the mutation; production intake SHA before and after is
`B23267FBBBA7E90797A63CFC0F86F4EE6BF10322FC296D0A7DAC90F6C23BEAB2`.
Restored eight focused tests and final 252-test Agent run PASS. Other negative cases above are input/fault mutations; this report does not claim every possible source mutation was executed.

## 4. Review and correction record

PI-TAG-001 risk modules: API, cryptography/key custody, raw/secret handling and governance; synthetic SQL fixtures only.
Existing contract and free-adversarial reviewers inspected implementation plus adjacent callers/A1 contracts. Final patch verification closed the receipt-order gap; the free reviewer confirmed the re-signed decision-basis controls and exact key cleanup.

| Group | Confirmed defects corrected inside A2 | Final disposition |
| --- | --- | --- |
| Commands | Stdin bounded read/deadline, malformed-input exit mapping, cleanup-safe actual Host command path | Scoped subprocess tests PASS |
| Execution | Lazy device admission, preview freshness, shutdown/drain, cancellation between CAS and device start | Actual caller and race tests PASS; no product scope expansion |
| HTTP/append | Typed 404, exchange body-read deadline, proxy disabled, correlation preservation, status restrictions, disposal drain and orchestrator outcome mapping | Wire tests + actual six-slot acceptance PASS |
| Durable lifecycle | Six-slot bounded ledger, exact complete receipt retirement, retained uncertainty/expiry tombstone, immediate secret clearing | Journal/caller/fault tests PASS |
| Test discrimination | Re-signed aliased nonce, established dual connections, exact pending-successor cleanup, receipt-before-acceptance assertion | All bounded findings closed; early-receipt source mutation demonstrated RED |
| Coverage completion | Actual Host/WPF callers, Windows principal/reparse negatives, real six-slot Face/Liveness controls and child crash checkpoints | Composed evidence documented; no whole deployed/UI-process certification claim |

Non-convergence cause during implementation was incomplete caller/fault coverage and narrow negative controls, not a new architecture decision.
Correction method: trace exact caller -> owner -> durable state -> visible error; add positive control with only the tested variable changed; audit failure cleanup; rerun full suites after focused restoration.
This report does not fabricate a single global round count: reviewers' histories contain component reviews and patch follow-ups, not a recoverable uniform ten-round sequence. Exact full PI pilot metrics/ratification are not claimed here. No new planning/RRI was opened to fix ordinary defects.

## 5. Source freeze and mutation boundaries

Server HEAD: `4fafa17a43ad38f46c2e0c69d570f65b8d01abfe`.
Agent HEAD: `b193f6316e13a82fb2f9f985f68500feae194cfa`.
Both indexes: 0 staged, 0 conflicted at final validation. No commits/push.
Server A2 mutation is exactly the test csproj, acceptance class and runner below, plus this requested as-built report.
Other dirty Server migrations/governance/docs were pre-existing and remain untouched by A2.
No Agent csproj change; only opt-in Server IntegrationTests -> Agent.Client test dependency. Existing package pins remain unchanged.
The explicitly permitted Crypto lock correction removed only the surplus empty win-x86 target from the preimplementation dirty lock; compared with HEAD its remaining delta is EOF newline, not dependency pins.
Of 24 consumed Agent rows, 15 remain byte-identical to dispatch freeze; 9 are authorized changed candidate surfaces. MainWindow is also byte-identical to its additional read-only pin. Pre-existing dirty/untracked source is not mislabeled as newly landed code.

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
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureCapabilityLoopbackIntake.cs` | `B23267FBBBA7E90797A63CFC0F86F4EE6BF10322FC296D0A7DAC90F6C23BEAB2` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeAgentCommands.cs` | `110E1C36350FF231900C496B872653275132627344E33C41B3AC8FB0685F1E7F` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeHttpClient.cs` | `21CFBFC1CDD1E0DFE2250D440AF3654EB9F07767627575E5A97214D2AA5386FF` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeWireCodec.cs` | `2D5273DEB3C5C6935E8BD3DD07B1FB630F861D3992471FEDC5E69698BFD7BFAB` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/WindowsCaptureRuntimeJournal.cs` | `204C945F032C0AFAD1D533F58438CFCE89ED62D0B772BA5F0974438417C7DBFF` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Client/WindowsCaptureRuntimeKeyStore.cs` | `2095427165836212E4317D4B77E960BD799BAF972A2F7CE4FC7F7EFBD3343B13` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureRuntimeAgentCoordinator.cs` | `E88C900AC317541FA6AC42D864E41AC9E863BDD4436C1E9AC8C2E27B21583C95` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Core/CaptureRuntimeAgentPorts.cs` | `212F5FF3C8DC7644E7CCCF40C3E4382A8174E49A97BD17E36554A742B86F200C` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2CeremonyTests.cs` | `2190C01F606C079E093BF0DE8BB1E74E57C3BAC486C81E71F3C755E635A512D7` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2CompositionTests.cs` | `878FA570D119A31088801A37006D974C6E4581658ED4965F7C361D5DF3BE95C7` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2HandoffTests.cs` | `5878F7BC35B52903DFADE8F91C9B089A21C61F8DE19F425BA8FD9D1E0A40D9B2` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2KeyCustodyTests.cs` | `E55B1C91D943A827F29237E9C9A29FD71DDEBC09347D40767FA03F6A653E9C23` | A2 implementation/test surface |
| Agent | `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA2WireTests.cs` | `64AF452F7C1245BD83C2C8ACD19E771BDDFE2923FE54FA92997DB9DDDCDBB1D3` | A2 implementation/test surface |
| Agent | `src/TagEkyc.CaptureAgent.Ui/MainWindow.xaml.cs` | `D40898C259D4B80089C6E9D45FE4A3D7A88A6F13BF87F45EBFE7EBAE066929BD` | Read-only; matches dispatch caller pin |
| Server | `tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj` | `162E91C273302159D5A0B5E81BFF13F6236265AB37E088544192B6C7ECB293D6` | A2 implementation/test surface |
| Server | `tests/TagEkyc.IntegrationTests/Tip88C1C6BA2ClientServerAcceptanceTests.cs` | `3840C2140D7077287BA142650FF225CB3C8825A8C1C40EDFDFAD45DBC14842B1` | A2 implementation/test surface |
| Server | `tools/Invoke-CaptureAgentA2Acceptance.ps1` | `7340246BA9020414E708C97B8DC8421AFA72B22650421BB73B06719671193570` | A2 implementation/test surface |

## 6. Explicit limits and next gate

- This is an as-built review candidate, not Homeowner implementation ratification or permission to land.
- No A3 retained-body/provider/broker execution, A4 installer/launcher/cutover, production activation, mTLS hardening or real patient capture.
- Existing SignFlow backend channel remains an input assumption; TagEkyc tests its own loopback boundary, not SignFlow governance.
- CNG proof is Windows x64 CurrentUser Microsoft Software KSP, including actual anonymous-principal denial; not TPM/hardware certification or a separately provisioned user-profile deployment test.
- Actual Host method and WPF composition execute in the test process; separate child probes provide abrupt process-death evidence at production journal operations. This is not a claim of killing the complete WPF UI at every checkpoint or physical power-loss testing.
- A1 HTTP acceptance uses actual TestServer/DI/gateway/PostgreSQL, not a deployed reverse-proxy/mTLS network.
- Known test-owned key IDs are deleted and absence-asserted by final acceptance fixture. No global key-store sweep was performed or claimed; unrelated/pre-existing keys were never removed.
- Full Agent suite ran with the one explicit manual skip above. A2 does not certify native biometric accuracy, field hardware or unattended production.
- Next: independent CC/GPT review of this source/evidence candidate. Any ordinary defect remains FIX + RETEST under the existing bounded authority; no fresh implementation permission is needed for such a fix.
