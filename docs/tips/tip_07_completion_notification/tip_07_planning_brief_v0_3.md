# TIP-07 Completion Notification Planning Brief v0.3

**File:** `docs/tips/tip_07_completion_notification/tip_07_planning_brief_v0_3.md`
**Version:** 0.3
**Status:** Historical accepted planning record - TIP-07 Option A implemented
**Date:** 2026-06-11
**Baseline:** `761ad98e83f63e4a6414fa2fa9704e258303758c`
**Purpose:** Define a narrow LocalDev completion notification preparation slice after TIP-06 without drifting into production webhook infrastructure.

## Closeout / Implementation Note

TIP-07 Planning Brief v0.3 was accepted for Option A only: internal/application service completion notification projection with no public HTTP route.

TIP-07 Option A implementation was committed at `916dd2918c2ab47ab0658ebf271fae45e22fb3ca` (`feat: add TIP-07 completion notification projection`).

Post-commit validation passed: `TagEkyc.ContractTests` 9 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 38 passed, total 63 passed and 0 failed.

Review accepted the early implementation after targeted evidence review. Confirmed boundaries: no public route, no webhook dispatcher/subscription, no durable outbox, no retry scheduler, no EF/DbContext/migration/durable persistence, and no SignFlow runtime/source/database dependency.

This document is retained as the accepted planning record and implementation boundary reference. It is not an active implementation gate.

## Changelog

### Post-implementation closeout - 2026-06-11

- Recorded that TIP-07 Planning Brief v0.3 was accepted for Option A only.
- Recorded TIP-07 Option A code/test commit `916dd2918c2ab47ab0658ebf271fae45e22fb3ca`.
- Recorded post-commit validation totals: 63 passed and 0 failed.
- Recorded no public route, webhook, outbox, retry, EF, or SignFlow drift.

### v0.3 - ClientApplicationId source clarification

- Pinned `ClientApplicationId` source to authenticated owner/client application identity already persisted or resolvable through TIP-06 completion/session/package projection.
- Explicitly prohibited deriving `ClientApplicationId` from request body fields, external session/transaction ids, API key text, or raw client-provided correlation values.

### v0.2 - Targeted review patches

- Preserved the existing TIP-06 `VerificationCompletedEventDto.EventType` contract value and recorded current repo evidence as `VERIFICATION_COMPLETED`.
- Made Option A the only default path and clarified it is an internal/application service boundary, not public HTTP/API exposure.
- Moved Option B and Option C behind STOP+ASK/default-deferred boundaries.
- Clarified `ClientApplicationId` is allowed only on `VerificationCompletedEventDto` and must remain absent from default BusinessConsumer completion/session/package DTOs.
- Added STOP gates for EventType contract changes, public endpoint selection, Option B error/exposure decisions, and notification materialization during TIP-06 completion.

### v0.1 - Initial planning brief

- Opened TIP-07 planning for LocalDev completion notification preparation after TIP-06.
- Recommended a narrow `BusinessConsumer.VerificationCompletedEventDto` projection over production webhook infrastructure.
- Recorded initial in-scope/out-of-scope, delivery semantics, data boundaries, test plan, STOP+ASK gates, and review protocol.

## 1. Current Baseline and Commit Anchors

Repository baseline:

- `761ad98e83f63e4a6414fa2fa9704e258303758c` / `761ad98` - `docs: codify TIP-06 review stop rules`.
- `c7fa9a50d303fd1d7f48eb7b8a4296a8c11698ef` - TIP-06 completion and evidence package runtime implementation.
- `51d449f` - TIP-05 capture artifact and evidence recording implementation.
- `5b243ee` - TIP-04 API key policy and session lifecycle implementation.
- `473f766` - TIP-03 closeout.

Preflight at TIP-07 planning open:

- `git status --short`: clean.
- `git log --oneline -8`: matched expected baseline sequence through `761ad98`.
- `dotnet test TagEkyc.sln --no-restore`: passed; `TagEkyc.ContractTests` 8 passed, `TagEkyc.ArchTests` 16 passed, `TagEkyc.UnitTests` 36 passed, total 60 passed and 0 failed.

TIP-06 leaves these relevant anchors for TIP-07:

- Completed session snapshot exists.
- Evidence package summary exists.
- Internal audit manifest and deterministic package/manifest hashes exist.
- `BusinessConsumer.VerificationCompletedEventDto` exists as the TIP-07 compatibility artifact.
- `VerificationCompletedEventDto.EventType`, deterministic LocalDev `DeliveryId = "localdev-not-dispatched"`, and `SentAt = completedAt` are compatibility semantics only; no webhook dispatch is implied by TIP-06.
- `WebhookSignatureStatus` and `EvidencePackageSignatureStatus` remain `PlaceholderUnverified` and non-authoritative.
- `ClientApplicationId` is allowed on the TIP-07 compatibility artifact but must remain absent from default BusinessConsumer completion/session/package API JSON.

## 2. TIP-07 Goal

TIP-07 should prepare a client-safe completion notification projection for LocalDev by materializing or exposing the existing `BusinessConsumer.VerificationCompletedEventDto` from TIP-06 completion/package data.

The goal is notification readiness, not production delivery. A successful TIP-07 implementation should prove that a completed session can produce a deterministic, sanitized completion notification record or read/query result suitable for later webhook work, while preserving TIP-06 completion, package, hash, audit, and data-boundary invariants.

## 3. In-Scope

TIP-07 may include:

- Build or materialize `BusinessConsumer.VerificationCompletedEventDto` from the TIP-06 completed session snapshot, evidence package, final decision, and client-safe completion/package projection.
- Preserve deterministic LocalDev placeholder delivery fields unless explicitly changed by an accepted planning patch:
  - `EventType` must preserve the existing TIP-06 `BusinessConsumer.VerificationCompletedEventDto.EventType` contract value. Current repo evidence uses `VERIFICATION_COMPLETED`; TIP-07 must not introduce a new literal unless reviewers explicitly approve changing the serialization contract.
  - `DeliveryId = "localdev-not-dispatched"` unless TIP-07 chooses a deterministic LocalDev notification-record id.
  - `SentAt = completedAt`.
  - `WebhookSignatureStatus = PlaceholderUnverified`.
  - `EvidencePackageSignatureStatus = PlaceholderUnverified`.
- Add a LocalDev application-layer query only if it is needed to prove materialization behavior.
- Add focused tests for field mapping, deterministic placeholder delivery semantics, no raw/internal leakage, no SignFlow dependency, no durable persistence, and no production webhook drift.
- Keep all runtime behavior LocalDev/non-production.

## 4. Out-of-Scope

TIP-07 must not implement without explicit approval:

- Production webhook signing.
- Durable outbox.
- Retry/backoff scheduler.
- Real external HTTP delivery reliability.
- HMAC, JWS, KMS, HSM, key rotation, replay cache, or production secret lifecycle.
- EF, DbContext, migrations, or durable persistence.
- Production webhook security guarantees.
- SignFlow runtime/source/database dependency.
- Raw/internal data exposure.
- Legal audit, non-repudiation, or production certification claims.
- New evidence package hashing, manifest hashing, final decision, completion lifecycle, or audit-chain semantics beyond the mapping needed for notification projection.

## 5. Runtime/API Shape Options

Option A - application service projection only:

- Add or reuse an application-layer query that returns `VerificationCompletedEventDto` for an already completed session.
- The query must take authenticated client context or run behind an existing authorized caller path.
- It must not be a raw mapper callable by arbitrary session id without ownership context, except in pure unit tests that do not represent an application boundary.
- The application-layer query is an internal/application service boundary only. It must not add a public HTTP route, subscription endpoint, dispatcher endpoint, or BusinessConsumer-visible read API. Any public exposure of this projection requires Option B STOP+ASK.
- No new public route.
- Best fit if TIP-07 is limited to contract/materialization proof.
- Tests can call the application service and contract boundary directly.

Option B - LocalDev read endpoint, STOP+ASK before selecting:

- Add a BusinessConsumer-authenticated LocalDev read endpoint for the prepared notification projection.
- Candidate shape: `GET /api/ekyc/verification-sessions/{id}/completion-notification`.
- Must require BusinessConsumer caller category, owner access, and a read or completion-notification-specific scope.
- Must not dispatch HTTP or imply subscription delivery.
- Needs API pipeline tests because the playbook requires endpoint behavior proof when runtime endpoints are added.
- Must pin whether the endpoint is:
  - a webhook-payload preview endpoint that returns exact `VerificationCompletedEventDto`, including `ClientApplicationId`; or
  - a sanitized BusinessConsumer read DTO that excludes `ClientApplicationId`.
- Must pin exact error-code tokens before implementation.

Option C - in-memory notification record, deferred by default:

- If explicitly approved, materialize one in-memory LocalDev notification record on first notification query only.
- Materializing notification during successful TIP-06 completion is not default TIP-07 scope because it changes the TIP-06 completion finalization surface.
- The record exists only to prove deterministic notification preparation and idempotent read behavior.
- Must not be called an outbox, durable queue, dispatch log, or delivery ledger.
- Must not add retry scheduling or external HTTP attempt behavior.

Recommended v0.3 path: Option A only. Option B and Option C require STOP+ASK before implementation because they widen the runtime/API or completion-finalization surface.

## 6. Event Source and Mapping From TIP-06 Completion/Package Projection

The notification event source is the TIP-06 completed session snapshot plus its evidence package/final decision projection.

Mapping rules:

- `EventType`: preserve the existing TIP-06 `VerificationCompletedEventDto.EventType` contract value. Current repo evidence uses `VERIFICATION_COMPLETED`; do not remap it to a new webhook-style literal in TIP-07 without explicit approval.
- `DeliveryId`: deterministic LocalDev placeholder from TIP-06 unless a TIP-07-specific LocalDev notification id is explicitly approved.
- `SentAt`: exact `completedAt` from the completed session snapshot.
- `VerificationSessionId`: completed session id.
- `ClientApplicationId`: authenticated owner/client application identity from the internal completion/session projection; allowed only on `VerificationCompletedEventDto`.
- `ClientApplicationId` must be sourced only from the authenticated owner/client application identity already persisted or resolvable through TIP-06 completion/session/package projection. It must not be derived from request body fields, external session/transaction ids, API key text, or raw client-provided correlation values.
- `ExternalSessionId`: completed session external session id.
- `FinalResult`, `AssuranceLevel`, `CompletedAt`, `RequestId`, and `CorrelationId`: from the completed session snapshot/effective completion values.
- `EvidencePackageId`, `EvidencePackageHash`, `ManifestHash`, and `EvidencePackageSignatureStatus`: from the TIP-06 evidence package/completed session projection.
- `WebhookSignatureStatus`: LocalDev placeholder only.

The mapping must not source fields from raw capture payloads, internal audit manifest internals, SignFlow state, or untrusted client request body values.

## 7. Delivery Semantics

TIP-07 v0.3 planning prefers "prepared notification" semantics over webhook delivery.

Allowed semantics:

- The notification event is a deterministic LocalDev projection of an already completed session.
- No external HTTP call occurs.
- No subscription model is required.
- No durable dispatch state is required.
- A repeated read or materialization for the same completed session returns the same semantic event data.

Disallowed semantics:

- Claiming that the event was delivered to a business endpoint.
- Treating `SentAt` as a real network send timestamp.
- Treating the placeholder `DeliveryId` as a production replay-safe delivery id.
- Recording attempt counts, final failures, or retry status unless TIP-07 is explicitly widened.

## 8. Idempotency/Retry Boundary

TIP-07 must preserve TIP-06 completion idempotency and must not introduce production retry behavior.

Rules:

- Completing the same session remains governed by TIP-06 idempotent completed snapshot behavior.
- Notification materialization for an already completed session must not create duplicate decisions, packages, audit events, hashes, or completion snapshots.
- If an in-memory notification record is approved, it may be idempotent per completed session in LocalDev only.
- `DeliveryId = "localdev-not-dispatched"` is not a production deduplication id.
- Retry/backoff scheduling, delivery attempts, retry counters, dead-letter states, and external HTTP replay handling are deferred.

## 9. Security/Data-Boundary Rules

TIP-07 must preserve the TIP-06 BusinessConsumer boundary:

- No raw capture artifacts, raw payloads, VaultRefs, file paths, internal audit manifest contents, adapter internals, CaptureAgent details, TrustedAdapter details, production keys, certificates, HMAC/JWS material, or signing algorithm details in the notification event.
- `ClientApplicationId` may appear only on `VerificationCompletedEventDto` and must remain absent from default BusinessConsumer completion/session/package response JSON.
- If Option B is selected, the endpoint must be classified before implementation as either a webhook-payload preview that returns exact `VerificationCompletedEventDto` including `ClientApplicationId`, or as a sanitized BusinessConsumer read DTO that excludes `ClientApplicationId`.
- Cross-client access must be rejected before any notification projection is returned if a public read route is added.
- Notification projection must be derived from already authorized LocalDev completion data and must not bypass existing caller category, scope, or ownership checks.
- No SignFlow runtime/source/database dependency may be added.

## 10. Placeholder Signature / Non-Production Boundary

TIP-07 remains LocalDev/non-production.

Required wording and behavior:

- `WebhookSignatureStatus = PlaceholderUnverified` means no production webhook signing exists.
- `EvidencePackageSignatureStatus = PlaceholderUnverified` means no production evidence package signature exists.
- Placeholder signature fields are compatibility markers only and are non-authoritative.
- TIP-07 must not claim authenticity, integrity, non-repudiation, legal audit reliance, production certification, or production webhook security.

## 11. Error/Status Behavior

If TIP-07 stays application-service only:

- Non-completed sessions should not produce a completion notification; return or model a not-ready/not-found operation result rather than synthesizing partial event data.
- Missing package/decision/finalization fields on a `Completed` session are invariant failures and should surface as internal consistency errors in tests.
- Cross-client or missing-scope behavior follows the underlying authorized query used by the projection.

If TIP-07 adds a public LocalDev read endpoint:

- Unauthenticated request: existing unauthorized envelope/status behavior.
- Wrong caller category: `403`.
- Missing required read/notification scope: `403`.
- Unknown session: `404 SESSION_NOT_FOUND`.
- Existing session owned by another client: `403 FORBIDDEN_CLIENT_APPLICATION`.
- Existing but not completed session: `409 SESSION_NOT_COMPLETED` or equivalent narrow conflict code.
- Completed session missing package/finalization fields: `500 COMPLETION_NOTIFICATION_INVARIANT_FAILED` or equivalent internal invariant code.

The exact status/error token matrix must be pinned before implementation if Option B is selected.

## 12. Test Plan

Required if TIP-07 implements Option A:

- Mapper/service test proving exact `VerificationCompletedEventDto` field mapping from a completed TIP-06 session/package.
- Test proving `SentAt = completedAt`.
- Test proving placeholder statuses remain `PlaceholderUnverified`.
- Test proving repeated projection is deterministic and does not append decisions, packages, audit events, or notification records.
- Test proving cross-client projection is rejected or impossible through the application service boundary.
- Contract/boundary test proving notification event data excludes raw/internal fields, VaultRefs, InternalAudit, adapter data, SignFlow data, and default BusinessConsumer-only forbidden fields. `ClientApplicationId` is allowed only on `VerificationCompletedEventDto`; tests must also prove it remains absent from default BusinessConsumer completion/session/package DTOs.
- Boundary test proving no new EF/DbContext/migration/durable persistence dependency.
- Boundary test proving no SignFlow runtime/source/database dependency.

Additional required if Option B adds an endpoint:

- API pipeline happy-path test through the actual route.
- API pipeline tests for unauthenticated, wrong category, missing scope, cross-client, unknown session, and not-completed session.
- API JSON negative assertions for `clientApplicationId` boundaries on default BusinessConsumer completion/session/package endpoints remain in force; `VerificationCompletedEventDto` may include it.

Additional required if Option C adds an in-memory record:

- Test proving one LocalDev record per completed session or one deterministic projection result per session, depending on the chosen model.
- Test proving no retry attempts, attempt count, HTTP target, subscription secret, or durable outbox state is introduced.

## 13. STOP+ASK Gates

Stop and ask before implementation if the selected TIP-07 scope requires:

- Any change to `VerificationCompletedEventDto.EventType` value or serialization contract.
- Public endpoint addition instead of application-service projection only.
- Implementing Option B before exact error-code tokens and `ClientApplicationId` exposure classification are pinned.
- Materializing notification during successful TIP-06 completion instead of on a post-completion query.
- Any external HTTP delivery.
- Any webhook subscription model.
- Any retry/backoff scheduler, attempt count, dead-letter state, or final failure state.
- Any durable outbox or database-backed notification storage.
- Any production webhook signature, key, HMAC, JWS, KMS, HSM, replay cache, credential, or secret lifecycle.
- Any change to TIP-06 finalization, package, manifest, audit hash, or completion idempotency semantics.
- Any exposure of raw/internal evidence, VaultRefs, adapter internals, audit manifest internals, or default BusinessConsumer `clientApplicationId`.
- Any SignFlow runtime/source/database dependency.
- Any legal audit, non-repudiation, or production certification claim.

## 14. Review Protocol

TIP-07 planning and implementation review must apply the playbook rules added at `761ad98`.

Required review flow:

- Apply `L-TAG-Review-01 - Full Coverage First, Then Invalidation Review`.
- Apply `L-TAG-Review-02 - Finding Classification and Stop Rule`.
- Run one full-system review with coverage attestation for the first serious TIP-07 draft.
- Full-system review must cover lifecycle/state behavior, security and public/private data boundaries, hash/audit/evidence chain/idempotency/determinism, API/error precedence and DTO defaults, test/proof level, scope boundaries and STOP gates, builder ambiguity, and repo-real feasibility.
- After full coverage exists, use an affected-surface map for later patches.
- Re-review only invalidated areas plus a lightweight blocker-only sentinel sweep for stale labels, superseded gates, deprecated field names, and direct contradictions.
- Classify findings as `BLOCKER`, `PATCH_REGRESSION`, `LATENT_SPEC_GAP`, `TEST_HARDENING_ONLY`, `BOOKKEEPING_ONLY`, or `DEFERRED`.
- Patch and re-review only `BLOCKER`, `PATCH_REGRESSION`, and implementation-blocking `LATENT_SPEC_GAP` findings.
- After two consecutive no-blocker rounds, stop planning review and move hardening/bookkeeping items to Deferred Notes.
- Do not reintroduce endless full A/B reruns after every patch.

Blocker-category examples for TIP-07:

- Builder can implement production webhook delivery by mistake.
- Notification event can leak raw/internal data or default BusinessConsumer `clientApplicationId`.
- Notification projection can break TIP-06 completion/package/hash/audit/idempotency invariants.
- Placeholder signature semantics can be misread as production signing.
- Tests fail to prove representative mapping and boundary behavior for the chosen runtime/API shape.

## Deferred Notes

- Production webhook delivery and retry remain better candidates for a later TIP after this LocalDev notification preparation slice is accepted.
- A future production webhook TIP should reopen subscription model, signing, replay, retry, outbox, delivery attempt status, endpoint credentials, and operational monitoring as first-class scope instead of inheriting LocalDev placeholder semantics.
