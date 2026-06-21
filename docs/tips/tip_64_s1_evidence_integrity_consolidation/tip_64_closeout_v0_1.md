# TIP-64 S1 Evidence-Integrity Consolidation — Closeout v0.1

**File:** `docs/tips/tip_64_s1_evidence_integrity_consolidation/tip_64_closeout_v0_1.md`
**Version:** 0.1
**Status:** Closed — Tier-2 docs-only consolidation accepted (Contractor adversarial spot-check ACCEPT)
**Date:** 2026-06-21
**Baseline:** `dc9d0ee docs: close TIP-63 S1 LLD runtime-contract consolidation (lld_02/lld_03 v0.2)` (master).
**Purpose:** Close TIP-64 after consolidating the as-built S1 evidence-integrity spec into `lld_01` (v0.1 → v0.2) and registering the Tier-2 legal/crypto open items.

## Changelog

### v0.1 — Initial closeout
- Closed TIP-64 as a docs-only Tier-2 consolidation. Recorded outcome vs intent, the multi-round convergence, code-wins reconciliations (timestamp, audit model, signature names), the Contractor adversarial spot-check result, the Tier-2 open items registered as Homeowner+legal debt, the deferred casing nit, validation, lessons, and recommended next step.

## Status

TIP-64 is closed as a docs-only Tier-2 evidence-integrity consolidation. Final disposition:

```text
READY_TO_COMMIT_TIP_64; T2-1/T2-2 ARE HOMEOWNER+LEGAL/CRYPTO DEBT; D-02 SLICE 2 DONE (lld_04 conceptual/forward, deferred)
```

## Outcome vs Intent

| Intended outcome | Actual result | Status | Notes / carry-forward |
| --- | --- | --- | --- |
| Consolidate as-built evidence-integrity into lld_01 | `lld_01` v0.2 has an authoritative Evidence-Integrity section + metadata block | Accepted | Verified exact against code |
| Canonicalization documented as-built (then flagged) | Web JSON, `{label}\n{json}`, property-order implementation-dependent, full-precision timestamp no-truncation | Accepted | T2-1 flagged |
| Hash chain + deterministic ids exact | `manifestBody → packageHash → manifestHash` field sets+order; `DeterministicGuid` labels/fields; version-5 + RFC4122 variant | Accepted | Subtle packageHash-uses-ids distinction captured |
| Audit model as-built (code-wins over TIP-06) | Single `VERIFICATION_COMPLETED`; manifest refs = existing+completion sorted by `EventId`; stale-note for TIP-06 3-event model | Accepted | — |
| Signature-field reconciliation | `payloadSignature → PayloadSignatureStatus`, `evidencePackageSignature → EvidencePackageSignatureStatus`; stale literals only inside notes; `webhook_deliveries` left deferred | Accepted | Casing nit deferred (see Debt) |
| Tier-2 limits flagged, no legal-sufficiency claim | T2-1/T2-2 present; "legally sufficient/non-repudiation/production-grade" appear only as denials | Accepted | Honest limits |
| Stay docs-only, persistence-agnostic, lld_01 only | Only `lld_01` changed; §1 persistence-agnostic | Accepted | — |

## Decision / Branch Disposition

| Decision / option | Final disposition | Why |
| --- | --- | --- |
| Consolidate as-built only; code wins | Accepted | Docs describe reality; code is truth |
| Timestamp: TIP-06 whole-second vs code full-precision | Code-wins (full-precision, no truncation); stale-note | Verified by code |
| Audit model: TIP-06 3-event vs code single-event | Code-wins (single `VERIFICATION_COMPLETED`); stale-note | Verified by code |
| Adopt RFC 8785 JCS / implement signing | Rejected (out of scope) | Homeowner+legal/crypto decision — T2-1/T2-2 |
| Consolidate lld_04 (engine adapters) | Deferred | Conceptual/forward, not as-built |

## Debt / Gap Final State

| Debt/gap | Final state | Resolved? | Next gate |
| --- | --- | --- | --- |
| EBS-01 canonicalization not JCS (T2-1) | Documented + flagged in lld_01 | No (by design) | **Homeowner + legal/crypto sign-off** before production/legal reliance; candidate RFC 8785 JCS/COSE/JWS |
| EBS-07 placeholder signatures (T2-2) | Documented + flagged in lld_01 | No (by design) | **Homeowner + legal/crypto sign-off** + production signing (HSM/KMS) decision; P0 debt |
| D-02 stranded as-built spec | lld_01/02/03 consolidated | Substantially reduced | lld_04 forward-design slice if/when S2 engines start |
| Field-casing nit | `PayloadSignatureStatus`/`EvidencePackageSignatureStatus` use PascalCase (C# property names) vs camelCase elsewhere in lld_01 | Deferred (cosmetic) | Optional normalization in a later hygiene pass |

## Exact Files Changed

| Path | Change |
| --- | --- |
| `docs/lld_01_data_model_v0_1.md` | v0.1 → v0.2: added metadata block + Changelog + `## Evidence-Integrity` (Canonicalization, Deterministic Ids, Hash Chain, Audit Hashing And Manifest Audit Refs, Signature-Status Model, Tier-2 Open Items); reconciled `evidence_results`/`evidence_packages` signature fields; left `webhook_deliveries` deferred |
| `docs/tips/tip_64_.../tip_64_planning_brief_v0_1.md` | Planning brief (v0.3.1) |
| `docs/tips/tip_64_.../tip_64_kickoff_v0_1.md` | Kickoff (v0.3.1) |
| `docs/tips/tip_64_.../tip_64_closeout_v0_1.md` | This closeout |
| `docs/tips/README.md` | v1.14 (open) → v1.15 (close) + TIP-64 row |

TIP-64 scoped diff (lld file): only `docs/lld_01_data_model_v0_1.md`. No `src/**` or `tests/**` change.

## Code-Wins Reconciliations

- Timestamp: TIP-06 §14 whole-second UTC is stale; as-built uses full-precision `DateTimeOffset.UtcNow` via Web JSON, no truncation.
- Audit model: TIP-06 §16 three-event (`FINAL_DECISION_CALCULATED`/`EVIDENCE_PACKAGE_CREATED`/`SESSION_COMPLETED`) + pre/post split + exclude-prior is stale; as-built is a single `VERIFICATION_COMPLETED` event with manifest refs = existing + completion sorted by `EventId`.
- Signature names: `payloadSignature`/`evidencePackageSignature` → as-built `...SignatureStatus`; stale literals retained only inside explicit stale/deferred notes.

## Tier-2 Open Items (registered as Homeowner + legal/crypto debt)

- **T2-1** Canonicalization is implementation-deterministic (Web JSON + anonymous-object order), not RFC 8785 JCS — not portable/independently-reproducible, refactor-fragile. Requires legal/crypto sign-off. EBS-01.
- **T2-2** All signatures `PlaceholderUnverified` — no key/signing/verification/HSM/KMS/replay/non-repudiation. Requires legal/crypto sign-off + production signing decision. EBS-07 / P0.

These are NOT resolved by TIP-64; they are decisions for the Homeowner with legal/crypto review.

## Contractor Adversarial Spot-Check (EBS-01/02/07 — highest stakes)

Verdict: **ACCEPT.** Verified the lld_01 Evidence-Integrity description against the as-built code (`VerificationCompletionApplicationService`, domain records, manifest/BusinessConsumer contracts), not the build report:

- Canonicalization label/format/property-order/timestamp — exact.
- Deterministic-id labels + input field order + version-5/RFC4122 variant — exact.
- Hash-chain three steps, labels, field sets and order; including the subtle `packageHash` uses-evidence-ids (not full ref objects) — exact.
- Audit single-event model, payload-hash fields, existing+completion sorted by `EventId` — exact.
- Signature-status locations + webhook projection-only — exact.
- Tier-2 limits flagged honestly; the phrases "legally sufficient/non-repudiation/production-grade" appear only as denials (no over-claim).
- Entity reconciliations correct; stale literals only inside notes; `webhook_deliveries` left deferred.

No drift, no invented rules, no new design, no resolved Tier-2 item. OPEN list: none found.

## Validation

Docs-only; no test run (per kickoff §9). Ran `git diff --stat` (only `lld_01` changed), `git diff` of `lld_01`, both `rg` checks (Tier-2 limits stated, no over-claim, stale names only in notes), and `git diff --check` (no whitespace errors).

## Review Ladder Summary (Contractor drafts → GPT + Codex review/converge → Codex builds → Contractor verifies)

- Contractor draft v0.1.
- Review round 1: ACCEPT WITH PATCHES — P1-1 timestamp false-as-built ("whole-second"), P1-2 webhook projection-only, P2-1 stale-name validation, P2-2 lld_01 metadata-block → v0.2; Contractor self-check (audit_events not a signature field) → v0.2.1.
- Review round 2: ACCEPT WITH PATCHES — P1 STOP/RRI vs code-wins contradiction, P1 audit-model as-built divergence, P2 legal-weight wording → v0.3.
- Review round 3: ACCEPT WITH PATCHES — P1 brief §8 STOP-trap lag (Contractor incomplete propagation), P1 missing `BusinessConsumerContracts.cs` source, P2 TIP-07 context → v0.3.1; Contractor cross-doc consistency check passed.
- Re-confirm: READY TO DISPATCH.
- Codex build.
- Contractor adversarial spot-check: ACCEPT.

Total: 3 external review rounds + 2 Contractor self-checks + re-confirm + build + adversarial verification. Each round produced material findings (false as-built, self-contradictory STOP rule, doc-set inconsistency) — not bikeshed.

## STOP/RRI Result

No STOP/RRI condition during build. The known code↔TIP-06 discrepancies (timestamp, audit model, signature names) were resolved by code-wins per kickoff §5, not STOP.

## Lessons Learned

- For a Tier-2 evidence-bearing surface, the decisive check is "doc matches CODE, exactly" verified by read/grep — the build report looked complete, but acceptance came only after verifying canonicalization/hash-chain/audit details against code.
- A "code↔TIP discrepancy → STOP" rule self-contradicts "code wins absolutely"; the correct rule is code-wins resolves discrepancies, STOP only on genuine unread-able ambiguity. (Latent in TIP-63 too.)
- Contractor propagation discipline: when a rule changes in the kickoff, propagate to the paired brief and cross-doc-grep — two review rounds were spent on incomplete propagation (TIP-63 sweep, TIP-64 brief §8). Cross-doc grep is now part of self-check.

## Recommended Next Step

Commit TIP-64 (lld_01 + TIP-64 docs + README; allowlist, exclude known-dirty). Then: surface T2-1/T2-2 to the Homeowner for the legal/crypto decision (these gate any production/legal reliance on the evidence hashing/signatures). D-02 is substantially reduced (lld_01/02/03 consolidated); lld_04 remains a conceptual/forward-design item for if/when S2 engines start.
