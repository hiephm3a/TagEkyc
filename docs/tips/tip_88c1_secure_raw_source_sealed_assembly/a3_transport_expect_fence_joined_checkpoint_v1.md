# A3 Expect fence — joined current-source checkpoint v1

Status: TECHNICAL CHECKPOINT, NOT FOUR-ROW RATIFICATION. Date: 2026-09-22.
Authority-open census remains **7**; the four `TRANSPORT-EXPECT-FENCE` rows remain `NOT_IMPLEMENTED`; A3 remains HOLD. Do not re-mint the activation seal from this checkpoint.

## Scope and source identity

The public production Agent client constructor now always chooses `StrictRawIngressTransport` for raw ingress, independent of its optional control-plane handler. Closed component tests can substitute raw transport only through an internal overload; production Host/WPF composition does not do so. The internal overload was made callable from `TagEkyc.IntegrationTests` so this existing acceptance project builds; the joined tests below deliberately use the **public** constructor, with no transport or trust override.

| Current source | SHA-256 |
| --- | --- |
| Agent `CaptureRuntimeHttpClient.cs` | `6E3206AD6B13B821F887C26C0B28DF0E244D3115AE6ED5410679E73E7AD27667` |
| Agent `StrictRawIngressTransport.cs` | `1FA123539BE8F1EE02D44F24BAA0F484E4B47766A4172A7341B77CDCC7FEA8B8` |
| Agent `RawExportRetainedSubmissionOwner.cs` | `B2AD6803334F32B9C498204F051D2C4B720E77403377D6BE41765961A5663AC1` |
| Server `CaptureRuntimeRawIngressAdmissionService.cs` | `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C` |
| Joined test `Tip88C1C6BA3R2R6ClusterHttpTests.cs` | `DCBB006E59BC4E1AD30B787D3A36F585107FD516348D33435B1C40003534D2AE` |
| Activation partition (7 rows) | `B7AC9BB68204FA69F37FB4F16BBFFD305C3437CC6E5BAA136ADFDCB6777C91E3` |

## One joined exchange; two discriminating guards

The test starts a real TLS Kestrel endpoint, production Agent raw transport, broker and isolated PostgreSQL database. A PostgreSQL advisory lock holds the B/R1 transaction before commit. A **separate** database connection observes that no B/R1 is committed while the Agent send waits. A wrapper around the Server request body records the first application read and checks committed B/R1 using another connection. Middleware counts raw POSTs at Kestrel before the endpoint runs.

After release, both joined cases pass (`a3-expect-two-guards-restored-815af0b79e1b4fa9afa7ff740e2371dd.trx`, **2/2**): no body read during the held transaction; committed B/R1 is visible at first body read. The normal case reads the body and returns the expected closed outcome. The deliberately lost-final case reaches `SUBMISSION_STATE_UNKNOWN`, and explicit same-source `RawExportRetainedSubmissionOwner.SubmitAsync` retry does **not** create a second raw POST at Kestrel.

Two mutations use the same joined cases:

| Mutation | TRX | Result and exact target |
| --- | --- | --- |
| Server reads one body byte before broker admission | `a3-expect-early-body-mutant-92abea05f9d84d12bcd7626210aa8cb0.trx` | **0 PASS / 2 FAIL**; held pre-commit first-read assertion changes from 0 to 1. |
| Retained owner ignores `AmbiguousSend` | `a3-expect-no-second-post-mutant-v2-f1a5fac3f41a4a94aea33d0b99b57249.trx` | **1 PASS / 1 FAIL**; only lost-final case sees raw POST count 2 instead of 1. |

Both product files were restored byte-exact to the hashes above. Agent affected-source gate then passed **87/87** (`a3-expect-affected-agent-restored.trx`). No product mutation remains live.

## Rollback and small-body boundary without Windows certificate prompts

The earlier rollback test used `SocketsHttpHandler` and exposed its known small-body behavior: at 1024 bytes its content-copy assertion failed while the 1025-byte control passed. That invocation (`a3-expect-existing-affected-restored.trx`, **2 PASS / 1 FAIL**) is **not** strict-production evidence. The test was changed to public production `CaptureRuntimeHttpClient`, TLS Kestrel and an independent rollback probe. It now asserts no Server body read and no committed B/R1 at **1, 1024 and 1025 bytes**.

The corrected rollback theory passed **3/3, zero skip** in a disposable Linux container: `a3-expect-strict-linux-rollback-20260922103617-1.trx`, SHA-256 `9A0769AC3FAAF10EEC9F326D957162AA20BC5E1F673D71194E1E362131C7AF70`. `SslStream` retained `X509RevocationMode.Online`; the container generated a one-run CA, leaf and CRL, served the CRL on its loopback, and isolated its CA bundle. The parent runner removed its PostgreSQL container/network; final `docker ps` and dedicated-network inventory were empty, and Windows `CurrentUser\Root` contained zero `TagEkyc-A3-*` roots. The reproducible runner is `Run-A3IsolatedTlsLinux.ps1` → `Run-A3IsolatedTlsLinux.sh` with `A3IsolatedTlsRunner.Dockerfile`. It does **not** import a root into the user's Windows trust store or request a Windows certificate confirmation.

The first Linux attempts failed before the business assertions: first PostgreSQL was addressed through an unsuitable container-to-host route, then `Online` correctly rejected a test leaf without reachable revocation information (`RevocationStatusUnknown`, `OfflineRevocation`). The latter retained diagnostic TRX is `a3-expect-strict-linux-rollback.trx` (**0/3**); the earlier network-failure TRX was overwritten by a fixed results filename before the runner switched to unique filenames. This loss is disclosed rather than counted as a complete immutable invocation inventory. Neither failure was fixed by lowering production revocation checking.

## Failed-run classification for the retained current-slice TRX

| Retained TRX suffix | Classification | Reason |
| --- | --- | --- |
| `early-body-mutant-92abea05...` | EVIDENCE_RED | Target first-read assertion, 2/2 RED. |
| `no-second-post-mutant-v2-f1a5...` | EVIDENCE_RED | Target second POST count, 1/2 RED. |
| `no-second-post-mutant-df426...` | SUPERSEDED_RED | Initial mutant failed at the transport exception type before the Server POST count; v2 reaches the target. |
| `existing-affected-restored.trx` | SUPERSEDED_RED | Old `SocketsHttpHandler` rollback path; strict-production successor is 3/3. |
| `existing-component-baseline.trx` | EXCLUDED_DIAGNOSTIC | Docker Engine unavailable before test fixture bootstrap. |
| `strict-linux-rollback.trx` | EXCLUDED_DIAGNOSTIC | `Online` revocation lacked CRL in first container setup. |
| `strict-tls-diagnostic-8a4c...`, `strict-tls-diagnostic-v2-7e11...` | EXCLUDED_DIAGNOSTIC | Test certificate private-key load produced pre-header TLS EOF; successor used `UserKeySet`. |
| `strict-tls-joined-attempt-f150...` | EXCLUDED_DIAGNOSTIC | Joined gate never reached before corrected key load. |

These are **9 retained failed TRX: 2 EVIDENCE_RED, 2 SUPERSEDED_RED, 5 EXCLUDED_DIAGNOSTIC**. The overwritten first Linux network diagnostic prevents claiming a whole-repo immutable failed-run census or final provenance freeze from this checkpoint.

## Exact limits and remaining work

The Server wrapper observes the first **application body read**, not a TLS packet capture. The Agent strict-transport component tests observe its content-to-TLS copy boundary; this joined test and those component tests are complementary, not interchangeable. The lost-final assertion covers retained-owner explicit retry and a Server-side POST counter; it does not itself exercise `RetryRetainedSubmissionAsync()` through the full orchestrator. No test here establishes exclusive TLS termination by the hospital Kestrel workload. CA/name validation, ALPN and direct local Kestrel tests cannot prove the hospital deployment topology or exclude a trusted intermediary that emits early `100 Continue`.

Thus the four rows — `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`, `BP03 BrokerCommitObservedBeforeFirstRawRead`, `BP13 ThreeExpectTransportMutationsFailQualification`, and `Agent raw HTTP Expect → durable server B/R1 before first body byte` — remain OPEN. Gate B deployment attestation/qualification and the row-specific remaining negative cases require their own evidence. This checkpoint changes no census, partition, ledger, activation seal or production activation authority. The current Windows API build warns `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` because source bytes changed under the previous seal; this is the intended fail-closed state, not a reason to re-mint before row closure.
