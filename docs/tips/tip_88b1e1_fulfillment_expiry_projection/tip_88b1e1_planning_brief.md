# TIP-88B1-E1 Fulfillment Expiry Projection - Planning Brief

Status: **READY_FOR_BUILD (v1.0)** — flipped by Homeowner 2026-07-20 after GPT+Codex review (ACCEPT WITH MINOR PATCHES applied). BUILD-LOCKED: build to THIS text; any scope change needs a new Homeowner lock. **Additive extension to the LANDED TIP-88B1 control-plane resolver** — surfaces the fulfillment `ValidUntilUtc` that the resolver already reads but drops. Prerequisite for TIP-88B3's decision-expiry formula. Touches a landed+pushed slice — Homeowner-authorized additive scope into TIP-88B1.
**No eligibility-semantic change. This slice intentionally extends the internal B1 resolver contract by one nullable fulfillment-expiry field.** No SQL, no schema, no migration, no Designer/ModelSnapshot change (the column already exists; this is a projection of already-read data).
Grounded on: HEAD `5169cd3` (88B2 closed). Baseline resolved at dispatch.
Date: 2026-07-20

## 0. Grounding (on-code, CC-verified @ 5169cd3)
- **The gap.** `RawExportFulfillmentRef` ([`RawExportControlPlane.cs:71`](src/TagEkyc.Domain/RawExportControlPlane.cs:71)) = `(RequirementType, FulfillmentEventId, Revision, ArtifactRef, ArtifactVersion)` — no expiry. `RawExportEligibilitySnapshot` ([`:78`](src/TagEkyc.Domain/RawExportControlPlane.cs:78)) exposes `FulfillmentRefs` but carries no earliest-expiry field either. So the B1 resolver return does NOT surface fulfillment expiry.
- **The data is already in scope.** The resolver is C# ([`EfRawExportControlPlaneRepository.cs:89`](src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs:89)). At [`:141`](src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs:141) it READS `latest.ValidUntilUtc` (to decide missing/expired), then at [`:147`](src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs:147) constructs `RawExportFulfillmentRef` DROPPING it. `ValidUntilUtc` is persisted by `AcceptFulfillmentAsync` (`RawExportFulfillmentAcceptCommand.ValidUntilUtc`, `RepositoryPorts.cs:215`), nullable (a fulfillment may have no expiry).
- **Why B3 needs it.** TIP-88B3 computes decision/permit expiry as the min of the PRESENT bounds (§3). The consent term is already exposed by the landed B2 snapshot (`RawExportSubjectConsent.cs:51`); only the fulfillment term is missing. B3's STOP forbids re-querying B1 fulfillment outside the resolver, so the projection must live in B1.

## 1. Intent
Surface the CURRENT effective fulfillment's `ValidUntilUtc` on the B1 eligibility resolver return, so TIP-88B3 can compute decision/permit expiry entirely from the resolver output. Nothing else changes.

## 2. Scope - ALLOWED

### 2.1 The projection
- Add `DateTimeOffset? ValidUntilUtc` to `RawExportFulfillmentRef` (the value of the CURRENT effective `Accepted` fulfillment for that requirement; NULL when that fulfillment has no expiry).
- Populate it at the single construction site (`EfRawExportControlPlaneRepository.cs:147`) from the `latest.ValidUntilUtc` already read at `:141` — no new query, no re-read.

### 2.2 Record-shape strategy (PINNED — do not choose an alternative)
- **Append `DateTimeOffset? ValidUntilUtc` as the TRAILING positional field of `RawExportFulfillmentRef`. NO default value.**
- Update EVERY construction / deconstruction / pattern-match / expected-record site EXPLICITLY. A repo-wide grep of `RawExportFulfillmentRef` must PROVE no stale five-field construction or deconstruction remains.
- Add an assertion in the EXISTING ArchTest boundary file `tests/TagEkyc.ArchTests/Tip88B1ControlPlaneBoundaryTests.cs` (census-confirmed present) proving `RawExportFulfillmentRef` is not directly bound/exposed by HTTP controllers, request DTOs, or response DTOs.
  - **"Internal resolver contract" here means SERVICE-INTERNAL usage, NOT the C# `internal` access modifier.** The existing accessibility of `RawExportFulfillmentRef` (currently `public sealed record`) MUST NOT change. The ArchTest proves ONLY the not-bound-by-HTTP/DTO property; it does not assert or require any accessibility change.
- **STOP condition:** if the repo census shows the existing positional signature MUST be preserved (e.g. a positional consumer that cannot be updated in this slice's allowlist), do NOT force it — STOP and patch this brief before build.

### 2.3 Time representation (PINNED)
`RawExportFulfillmentRef.ValidUntilUtc`:
- copied from the EXACT persisted current `Accepted` fulfillment row selected by the resolver;
- the authoritative Postgres `timestamptz` instant, following the repo UTC / offset-zero convention;
- NEVER recomputed from a duration; NEVER derived from application `UtcNow`;
- read in the SAME resolver transaction / evaluation as the other fulfillment fields (`FulfillmentEventId`/`Revision`/`ArtifactRef`/`ArtifactVersion`).

## 3. B3 consumption contract (documented here; BUILT in TIP-88B3, not this slice)
B3 derives the fulfillment-expiry bound from `FulfillmentRefs[]`:

```
finiteFulfillmentExpiries =
    FulfillmentRefs where ValidUntilUtc is not null select ValidUntilUtc

EarliestFulfillmentValidUntilUtc =
    minimum(finiteFulfillmentExpiries) when any exist; otherwise NULL (no fulfillment-derived bound)
```

Cases: empty `FulfillmentRefs` -> NULL/no bound; all expiries NULL -> NULL/no bound; finite + NULL mixed -> minimum finite; several finite -> earliest finite; **B1 snapshot not `Active` -> B3 does not calculate or issue a permit at all.**

B3 final expiry = min of ONLY the PRESENT bounds: `EvaluatedAtUtc + PermitTtl`; `Consent.ValidUntilUtc` when non-null; `EarliestFulfillmentValidUntilUtc` when non-null. **Do NOT persist or expose `DateTimeOffset.MaxValue` as evidence** — an absent bound is absent, not a sentinel. This slice VERIFIES the finite-min algorithm in test only (§5); it adds NO B3 production logic.

## 4. STOP / NOT
- NO change to the eligibility DECISION or its observable semantics — see the PINNED-UNCHANGED list in §6.
- NO SQL, NO schema, NO migration, NO Designer/ModelSnapshot change; NO new query; NO re-read outside the existing resolver evaluation.
- NO change to grants/ACL/readiness; NO change to the fulfillment state machine; NO reshaping of `RawExportEligibilitySnapshot`'s other fields; NO separate `EarliestFulfillmentValidUntilUtc` field on the snapshot (rejected — B3 computes it, §7).
- NO public/HTTP/JSON DTO exposure of `RawExportFulfillmentRef`; NO PermitTtl / permit / decision / idempotency logic (all TIP-88B3); NO raw-byte path.
- Any migration, ACL, readiness, public-DTO, or unrelated B1 semantic change encountered mid-build = STOP and report.

## 5. Verify gates
- **Decision unchanged (explicit, not whole-record equality):** the full existing TIP-88B1 suite stays green; assert EACH pre-existing decision field and cause ordering is unchanged for a representative `Active` case and each `Inactive` cause. Do NOT assert byte-identical snapshots; assert the enumerated fields (§6). New tests use INDEPENDENT expected values — never derived through the production mapping under test.
- **Projection correctness:** accept a fulfillment with a concrete `ValidUntilUtc` -> the ref's field equals the NORMALIZED persisted instant (not merely the caller's original offset representation); accept one with `ValidUntilUtc = null` -> the ref's field is NULL.
- **Row coherence / no-fallback** (all of `FulfillmentEventId`, `Revision`, `ArtifactRef`, `ArtifactVersion`, `ValidUntilUtc` must come from the SAME current `Accepted` event):
  - rev1 `Accepted` `ValidUntilUtc = NULL`; rev2 `Accepted` finite `ValidUntilUtc`; BEFORE rev2 expiry the ref is rev2 with the finite expiry; AFTER rev2 expiry the snapshot is `Inactive`/`MissingOrInvalidFulfillment` and rev1 must NOT reappear.
    - **Deterministic mechanics (PINNED — no timing-duration assertions):** the "after rev2 expiry" case must NOT rely on an arbitrary fixed `Task.Delay` as the assertion basis, and must NOT introduce an application clock abstraction or change production DB-time semantics. Use the established PostgreSQL integration-test pattern: set rev2's `ValidUntilUtc` to a near instant, and BEFORE invoking the resolver PROVE via DB `transaction_timestamp()` that DB time `>= rev2.ValidUntilUtc` (bounded polling is allowed ONLY with an explicit timeout AND that DB-time predicate). Assertions are STATE-BASED (`Inactive`/`MissingOrInvalidFulfillment`, rev1 absent), never duration-based. NO production code may be added solely to manipulate time. The test must remain stable under slow CI / container execution.
  - `Accepted -> Withdrawn -> new Accepted` renewal: the projected event id / revision / artifact / expiry are ALL from the NEW `Accepted` row.
  - multi-requirement with one NULL expiry and at least two finite expiries: each ref retains ITS OWN value; test the finite-min algorithm in test code WITHOUT adding B3 production logic.
- **No positional-break:** a repo-wide grep of `RawExportFulfillmentRef` proves every construction/deconstruction/pattern-match/expected-record site is updated; zero stale five-field usages remain.
- **Contract-not-DTO:** the ArchTest from §2.2 passes.

## 6. Locked invariants
Additive projection of already-read data. **PINNED UNCHANGED (assert explicitly):** `State`; `PrimaryCause`; the ordered `Causes[]`; `EvaluatedAtUtc`; `GrantRef`; `LifecycleRef`; `FulfillmentRefs` membership AND ordering; ALL pre-existing fields of each `RawExportFulfillmentRef`; the lock order and ambient-transaction behaviour. **MAY CHANGE (acknowledged, by design):** record equality of `RawExportFulfillmentRef`, its positional constructor/deconstruction arity, and its diagnostic/`ToString` representation — because the contract shape gains one field. The projected `ValidUntilUtc` equals the current effective fulfillment's persisted instant (NULL when none); a withdrawn/expired/superseded fulfillment contributes exactly as before.

## 7. TIP analytical summary / intent ledger

**Intent.** Give the B1 resolver output enough to let TIP-88B3 compute decision/permit expiry with no B1 re-query.

**Expected outcome.** `RawExportFulfillmentRef` carries `ValidUntilUtc`; the eligibility decision is semantically unchanged; B3 gate-2 closes on landing.

**Accepted decisions.**
- A PER-REF `ValidUntilUtc` field (not an aggregate snapshot field).
- TIP-88B3 OWNS the finite-min calculation (`EarliestFulfillmentValidUntilUtc`); this slice only projects + tests the algorithm.
- NO query / schema / migration change (projection of already-read data).
- EXPLICIT positional-contract update at every site (trailing field, no default).
- NULL `ValidUntilUtc` means "no fulfillment-derived expiry bound", never a sentinel.

**Rejected / deferred branches.**
- A separate `EarliestFulfillmentValidUntilUtc` field on `RawExportEligibilitySnapshot` — REJECTED (B3 computes it from the per-ref list).
- B3 directly re-querying B1 fulfillment — FORBIDDEN (B3 STOP).
- Doing the expiry calculation inside B1 — REJECTED (B3 owns permit/expiry).
- Public/HTTP API exposure of `RawExportFulfillmentRef` — NOT AUTHORIZED.
- PermitTtl / permit logic — DEFERRED to TIP-88B3.

**Debt / gap impact.** Closes TIP-88B3 gate-2 on landing. No new debt. Does not touch the P1/P2/P3 raw-export debt rows.

**Non-claims.** Does not change eligibility semantics, cause taxonomy, locking, grants, readiness, or any DB artifact; does not compute or issue any permit; does not expose a public DTO.

**Dispatch readiness (repo census @ `5169cd3` complete).** EXACT allowed files:
- Source (2): `src/TagEkyc.Domain/RawExportControlPlane.cs` · `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs`
- Integration test (1): `tests/TagEkyc.IntegrationTests/Tip88B1RawExportControlPlaneTests.cs`
- ArchTest (1): `tests/TagEkyc.ArchTests/Tip88B1ControlPlaneBoundaryTests.cs` (existing file — add the contract-not-DTO assertion here)
- E1 docs: `docs/tips/tip_88b1e1_fulfillment_expiry_projection/tip_88b1e1_planning_brief.md` + (on land) `tip_88b1e1_closeout.md`
- Allowed ONLY during landing docs: `docs/tips/tip_88b1_raw_export_control_plane/tip_88b1_closeout.md` (append non-normative note) · `docs/tips/tip_88b3_raw_export_authorization_decision/tip_88b3_planning_brief.md` (gate-2 → CLOSED) · `docs/tips/tip_88_raw_export_policy_spine/tip_88_planning_brief.md`
- **Any additional source/test/project file needed after dispatch = STOP and report before modification.** Any migration, ACL, readiness, public-DTO, or Designer/ModelSnapshot change = STOP.

**Consumer inventory (census @ `5169cd3` — every `RawExportFulfillmentRef` site):**
- `src/TagEkyc.Domain/RawExportControlPlane.cs:71` — record DEFINITION (add the trailing field here).
- `src/TagEkyc.Domain/RawExportControlPlane.cs:87` — type reference inside `RawExportEligibilitySnapshot` (`IReadOnlyList<RawExportFulfillmentRef>`); no change.
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs:130` — `new List<RawExportFulfillmentRef>()` type reference; no change.
- `src/TagEkyc.Infrastructure/Persistence/EfRawExportControlPlaneRepository.cs:147` — the **ONLY positional construction** (add the argument here).
- `tests/TagEkyc.IntegrationTests/Tip88B1RawExportControlPlaneTests.cs:180-181` — the ONLY consumer, via PROPERTY access (`FulfillmentRefs[0].RequirementType`) — does NOT break on a trailing field; extend it to assert the new field.
- **Zero positional deconstruction, zero `with`-expression, zero whole-record equality assertion** on `RawExportFulfillmentRef` anywhere in the repo. So the record-shape change touches exactly one construction site plus the additive test assertion.

## 8. Review tier & attacks
Tier-2 (small additive projection on a landed slice, no decision change). Attacks: (a) does adding the field alter the eligibility decision or cause ordering (the §6 pinned list must hold)? (b) is the projected value the CURRENT effective fulfillment's, or a stale/withdrawn/superseded row's? (c) does a NULL (no-expiry) fulfillment project a wrong non-null value or throw? (d) any positional consumer left un-updated? (e) is `ValidUntilUtc` the normalized persisted instant, or an app-recomputed/UtcNow value? (f) any scope creep into `RawExportEligibilitySnapshot`, a public DTO, or TIP-88B3 expiry logic?

## 9. Landing docs (on implementation land)
- Create `tip_88b1e1_closeout.md`.
- Append a NON-NORMATIVE extension note to the TIP-88B1 closeout (the resolver contract gained `RawExportFulfillmentRef.ValidUntilUtc`; eligibility semantics unchanged).
- Update TIP-88B3 gate-2 to CLOSED with the landed commit and the ACTUAL as-built field shape (reconfirm on-code).
- Do NOT mark TIP-88B3 READY_FOR_BUILD until its remaining gates (PermitTtl pin, brief rewrite) also close.
