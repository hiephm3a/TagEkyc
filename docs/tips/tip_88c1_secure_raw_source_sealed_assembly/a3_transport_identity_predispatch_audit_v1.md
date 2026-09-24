# A3 transport identity and strict HTTP — pre-dispatch gate audit

**Status:** READ-ONLY SOURCE AUDIT / DESIGN CORRECTION; NOT AN IMPLEMENTATION DISPATCH  
**Date:** 2026-09-22  
**Design baseline:** `a3_transport_expect_fence_design_proposal_v3.md`  
**Governance:** activation-open census 7; four TRANSPORT-EXPECT-FENCE rows OPEN; A3 HOLD

## Intent and boundary

Record what the current source actually supports before writing a strict raw HTTP handler or claiming the TagEkyc TLS identity foundation is ready. The reviewed technical direction is a signed, out-of-band installation transport profile, not an environment variable, TOFU, or a pin learned from the connection being qualified. This audit does **not** convert a quoted reviewer approval into a Homeowner ratification record or approve a hospital issuer, endpoint, key custodian, or network topology. No code, tests, seal, census, or production configuration changes are authorized by this document.

The pre-dispatch order is: (A) trust anchor and local-store threat boundary; (B) approved deployment attestation; (C) exact Server fixed-length response impact; (D) partial/lost-final result mapping. A design review must explicitly close each gate. A positive answer for A cannot silently close B–D.

## A. Signed profile: where trust stops

The profile-signing authority and Kestrel endpoint TLS key are **different key pairs and different authorities**. A profile may carry `ProfileId`, monotonic `ProfileRevision`, role, exact origin/host/port, current/optional-next SPKI SHA-256, approved deployment-record identity/revision/SHA, issuer identity, algorithm and signature. Agent verifies exact signed bytes on every load before connecting. The profile is public identity data, not a secret. It does not need a second independent expiry clock; the deployment record's approved validity window controls readiness. If a future signed profile carries `ValidityFrom/Until` for signature-policy reasons, those fields must agree with—not silently extend—the deployment record.

The issuer verification key **must not be stored solely beside the mutable profile** or accepted from the profile itself. A concrete bounded candidate is a verification public key embedded in the signed Agent installation package/binary, installed by the authorized hospital deployment process before profile import. Profile write access and root-key/code replacement must have separate authority. A managed installer can install the signed profile into a machine-local directory with ACLs permitting Agent read and a named deployment service/operator write; the Agent must verify the signature even if ACLs appear correct. The existing `WindowsCaptureRuntimeKeyStore` CNG `UserKey` is for Agent private signing keys and is not an integrity authority for Server SPKI pins. A bare `TAGEKYC_SERVER_SPKI` environment variable is not authority.

**Threat boundary must be signed off, not implied.** ACLs plus signature reject ordinary users or operators who can edit only the profile but lack the issuer private key. They do **not**, by themselves, resist a local administrator who can replace the Agent executable/trust anchor, run a patched Agent, alter local revision state, or disable controls. If the stated requirement includes resistance to local administrators (including hospital IT), deployment must require an independently managed code-integrity/installation control, such as an appropriately signed Windows App Control policy, or an equivalent reviewed control. The signer, policy owner, update path, recovery procedure and actual hospital enforcement are separate deployment evidence. Microsoft documents that signed App Control policy makes local-admin policy tampering harder; this audit does not claim it is configured in TagEkyc.

Likewise, `revision 13 > revision 12` in a signed file is not anti-rollback if an attacker can replay both an older signed profile and the locally stored revision floor. A monotonic floor must be anchored outside that same rollback domain: for example an independently protected managed-installation receipt or reviewed hardware/management-backed monotonic state. Its exact mechanism, recovery and rotation are a **STOP decision**, not a TODO to hide in a filesystem ACL. Reject equal-revision/different-bytes and a lower revision; never auto-promote `next` to `current`.

**Gate A remains open** until the Homeowner/deployment-security owner approves the issuer/root placement, machine-local store/ACL, local-admin threat boundary, and independently anchored revision floor. The reviewed provisioning *direction* alone does not make these mechanisms present.

## B. Deployment attestation: what a signed statement can and cannot prove

The v3 `ApprovedTransportDeployment` fields remain the minimum: exact endpoint, profile identity/SHA, certificate identity, named Kestrel serving workload, exclusive private-key **use** authorization, no intermediary TLS termination, direct Agent→Kestrel route, owners, evidence locators, validity and rotation authority. A key may reside in a certificate store, protected mount or HSM/KMS; physical co-location is not the property. No proxy, sidecar or load balancer may possess **or exercise** the approved endpoint key.

The readiness verifier can parse, hash-bind and compare the approved record to profile/configuration, and Agent can check chain, hostname, additive SPKI and exact ALPN. Neither a SHA nor pin proves the live route or exclusive key use. A named deployment/security operator must independently inspect the hospital key-access and network configuration and sign an attestation with timestamp and evidence locators. The operational control must detect later route, termination or key-permission drift and invalidate readiness; no self-approval or automatically extended record is allowed. The v3 30-day/7-day expiry warnings and distinct expired reason remain proposed operational defaults for first deployment approval.

**Gate B remains open** until actual owners, evidence locations, verifier inputs and fail-closed runtime/readiness binding are approved and tested. The existing A3 activation seal is a pattern, not a pre-existing transport-identity integration.

## C. Exact fixed-length Server response impact

The Activated route selected by `CaptureRuntimeRouteRegistration.MapCaptureRuntimeSelectedEndpoints` maps `MapCaptureRuntimeRawIngressEndpoints`; Prepared maps the different `MapRawExportSourceIngressEndpoints`. The Activated `RuntimeIngressAsync` in `RawExportSourceIngressEndpoints.cs` has these **application-owned** final-response sources, all currently using `Results.Json` through `RuntimeError` or `MapRuntimeResult`:

| Path in Activated raw endpoint | Response source | Required frame proof |
| --- | --- | --- |
| forbidden legacy API/operator key; invalid metadata/signature; missing services; authentication failure; caught exception | `RuntimeError` | fixed `Content-Length`, exact existing status/code/correlation shape, no body read before B/R1 |
| post-auth invalid encoding/trailer | `MapRuntimeResult` O04 branch | same, no accidental early body read |
| broker/admission mapped outcome, including pre-body final and post-body result | `MapRuntimeResult` branches for Available/AlreadyAvailable, EvaluationInProgress, all other listed O-codes, and unmapped fallback | fixed length without changing status/outcome/JSON fields or broker behavior |

`Program.cs` selects routes before `app.UseHttpsRedirection()` and maps them later. HTTPS redirection, Kestrel-generated failures, and any middleware/platform response *outside* the endpoint's recognized application contract cannot be relabeled as a public O-code. The strict Agent must fail closed on an unrecognized frame, with no raw send or automatic resend. The implementation must inventory and test the complete set of recognizable pre-`100` finals and the platform-error boundary on the actual Kestrel runtime. If required recognized responses cannot be made fixed-length, stop for design review rather than adding response chunking by accident.

The Server-framing affected-source sentinel set is **independent** of the Agent-client set. Scan current ratified proof dependencies for direct HTTP callers, error mapping, authentication and admission—not just tests that use `CaptureRuntimeHttpClient`. No current test run is claimed here.

**Gate C remains open** pending exact response-shape/framing implementation plan and source-dependent sentinel inventory. The table is a source inventory, not wire evidence that `Results.Json` already emits fixed length.

## D. Raw partial/lost-final mapping: current call chain has a real gap

The current raw path is:

```text
CaptureRuntimeHttpClient.SubmitRawExportSourceAsync
  -> RawExportRetainedSubmissionOwner.SubmitAsync
  -> CaptureAgentOrchestrator.SubmitRetainedContinuationOnceAsync
  -> RetryRetainedSubmissionAsync / receipt
```

`CaptureRuntimeHttpClient` uses `rawHttp.SendAsync`, and its timeout catch currently changes an internal `OperationCanceledException` into `CaptureAgentFlowException("RECAPTURE_REQUIRED")` without a send-stage/byte-count distinction. `RawExportRetainedSubmissionOwner` sets `RetryAllowedAfterRun = true` for `HttpRequestException`, internal timeout, and `NOT_READY`/`SUBMISSION_STATE_UNKNOWN`. In `CaptureAgentOrchestrator`, transport exceptions set `LastOutcomeCode` to `SUBMISSION_STATE_UNKNOWN`, but `PendingRetainedResult` currently builds a `FailedAfterPartialSubmit` receipt. Other `SubmissionStateUnknown` terminal-status paths exist for other submission flows; their existence does **not** prove this raw path has the required mapping. An explicit later retained retry entrypoint exists, so "not automatically resent in this one call" is weaker than "unknown result cannot be treated as definitely unsent at the next entrypoint".

The strict handler must emit a typed internal result carrying at least `HEADERS_SENT_NO_100`, `BODY_PARTIAL`, or `BODY_COMPLETE_NO_FINAL`, plus whether an exact qualified `100` was seen and the content-byte copy count. Both partial and complete-without-final **collapse to the existing external `SubmissionStateUnknown` semantic**, but keep internal stage for safe custody and diagnostics. The owner and orchestrator must preserve ambiguous ownership and prohibit an unqualified same-buffer replay, not translate either stage to ordinary `RECAPTURE_REQUIRED` or generic retryable `HttpRequestException`. A pre-body final O-code remains a fully parsed business result and uses its existing retry policy. Any explicit post-unknown reconciliation/retry needs a separately proven durable state check and idempotency/custody rule; it is not authorized by a catch clause alone.

Required joined tests: partial byte copy then loss; full body then lost final; zero-copy pre-`100` timeout/final; subsequent explicit `RetryRetainedSubmissionAsync`; receipt terminal status and next action; no second raw POST without the required reconciliation. A mutation that classifies an ambiguous send as `RECAPTURE_REQUIRED` or retryable must RED at a named assertion while adjacent pre-send and parsed-final controls stay GREEN.

**Gate D remains open.** Current source contains an unknown-state vocabulary and some catch paths, but not the proven stage-aware raw-ingress mapping required by v3.

## Pre-dispatch disposition

```text
Signed out-of-band provisioning direction     TECHNICALLY ACCEPTED
Issuer trust-anchor placement and local-admin boundary   OPEN (A)
Deployment attestation and runtime/readiness binding      OPEN (B)
Server fixed-length final response impact                OPEN (C)
Raw stage-aware unknown-result mapping                    OPEN (D)

Implementation     HOLD
Expect rows        4 OPEN
Activation census  7
A3                 HOLD
```

Close A–D in one reviewed pre-dispatch decision/implementation matrix. Do not dispatch production transport code based only on the signed-profile direction, and do not manufacture four-row closure from one mechanism-level RED. No full suite, staging, commit, push, seal re-mint or production activation is authorized by this audit.

## Source and platform anchors

- `TagEkyc.CaptureAgent/src/TagEkyc.CaptureAgent.Client/CaptureRuntimeHttpClient.cs`: `SubmitRawExportSourceAsync` and its timeout mapping.
- `TagEkyc.CaptureAgent/src/TagEkyc.CaptureAgent.Core/RawExportRetainedSubmissionOwner.cs`: retained lease and `RetryAllowedAfterRun` handling.
- `TagEkyc.CaptureAgent/src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs`: retained continuation/receipt and explicit retry entrypoint.
- `TagEkyc/src/TagEkyc.Api/RawExportSourceIngressEndpoints.cs`, `CaptureRuntimeRouteRegistration.cs`, `Program.cs`: Activated endpoint, response mappers and route selection.
- [Microsoft: App Control with signed policies and local-administrator tamper resistance](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/introduction-to-virtualization-based-security-and-appcontrol).
- [Microsoft: machine-scoped DPAPI permits any user on that machine to decrypt](https://learn.microsoft.com/en-us/windows/win32/api/dpapi/nf-dpapi-cryptprotectdata); DPAPI is not a substitute for a separate pin-signing authority or code-integrity boundary.
