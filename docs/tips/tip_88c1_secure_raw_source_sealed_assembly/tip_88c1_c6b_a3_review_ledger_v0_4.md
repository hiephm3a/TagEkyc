# C6B-A3 v0.4 — existing-consent reuse correction

Date: 2026-09-13. PI-TAG-001, TIP-88C1 High-risk; documentation only.

## Exact candidate and authority

- Candidate: `tip_88c1_c6b_a3_broker_composition_dispatch_v0_4.md`
- SHA-256: `17C7E47D891454894172DC807CD97D744B48FEE77EEE28A8227DF80CAE27C611`
- 55,438 bytes / 535 lines / LF.
- v0.3 preserved: `F18885D92750139F0A1C5D3F375020772A20E1CA6CAB8CDF41548C5E21AFFD7D`.
- Current Homeowner direction: reuse the existing personal-data processing consent; do not add separate DG2/selfie consent or a new subject workflow; continue A3.
- Scope: replace the former E01 new-consent/input-question framing with technical reference integration. S01/S05 and distinct retention/export permits remain closed semantics. This is not a legal sufficiency opinion or implementation authority.

## Changes and validation

- §1 records the clarification; §3 separates existing consent from its missing local reference binding. No new collection UI/signature/checkbox, consent TIP, generic framework or mandatory separate consent aggregate.
- §4 uses a local binding identity referencing existing source evidence, not a forged consent UUID or B2 export permit. Ordering, proof, work batches and terminal disposition propagated.
- The resolver must validate actual evidence/ownership/status; Client authentication or any nonempty string is not treated as consent. No uninspected external API/storage is claimed landed.
- All25 evidence hashes compared as full64-character SHA strings: match. RET rows12; outcome rows36, unchanged. v0.2/v0.3 hashes preserved.

## Review ladder and affected surfaces

V1 independent bounded review read the entire v0.4, affected v0.3, user authority, AGENTS/playbook/PI, E3 append, consent Domain and current Client/runtime contracts. It also sampled adjacent/free-adversarial risks: false source-existence claims, Client-auth-as-consent, and reopening business consent or weakening export. All three were dismissed with cited boundaries.

One PATCH_REGRESSION: proof wording “No extra subject consent event/UI interaction” could forbid the existing necessary B2 Granted projection after Completed. Builder verified E3 :120–128 and changed the assertion to prohibit a new subject agreement/signature/collection/UI only, explicitly preserving authorized Granted/Withdrawn projections and audit. Local binding was clarified not to mandate a separate consent aggregate/table. Two corresponding source hash rows were added.

V2 on the final SHA above: **PASS — 0 actionable findings for this bounded correction**. No old event-prohibition residue; same-reference reuse, distinct permits, export Completed gate and honest NOT READY dispatch status remain.

Metrics: two review passes; one accepted patch regression, fixed; zero unsupported/repeated findings; no non-convergence; rounds3/5/10 not reached. No CC/GPT external review claimed. Census and mechanical checks are not runtime proof.

## Final scope / remaining work

- Only new v0.4 candidate and this ledger written. No product/schema/test/config/project edits or build/test/DB/provider execution. No GDrive sync requested/performed.
- Server HEAD unchanged `5df5f60a6dc4d992c71fc2b160673e4abca6488d`; Agent unchanged `e3bd625bbe357b1f3c9620d20bcba121cfb61974`.
- Both repos staged0/conflicted0; unrelated work preserved; no stage/commit/push.
- E01 business question closed; no further request for a separate subject consent. Remaining work is technical reference-binding/resolver/withdrawal integration and the already-declared DDL/ACL/private-union/allowlist/canonical projection closure. It is not completed by this bounded correction.
- A3 implementation authority remains ungranted; A4/production closed. Next task is executable contract materialization within the same A3, not another consent decision or TIP.
