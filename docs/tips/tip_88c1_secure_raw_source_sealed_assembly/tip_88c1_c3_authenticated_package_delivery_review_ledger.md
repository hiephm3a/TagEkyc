# TIP-88C1-C3 — Authenticated Package Delivery Review Ledger

**Version:** 0.2.1  
**Status:** ROUND-1 NARROW PATCH REGRESSION RECONCILED — v0.2.1 CLOSURE REVIEW PENDING — APPEND-ONLY  
**Date:** 2026-08-18  
**Owner:** Homeowner + Codex Contractor/Drafter  
**Baseline commit:** `7dd15cca6b2159d08220167fe5f0b4cf95fca195`  
**Scope candidate:** `tip_88c1_c3_authenticated_package_delivery_scope_brief.md` v0.2.1  
**Scope SHA-256:** `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80`  
**Implementation authority:** `NONE`

## 0. Changelog

### v0.2.1 — Narrow patch-regression reconciliation

- Records the independent v0.2 split verdict: CC PASS and OpenAI GPT-5.6
  Thinking FAIL on one new patch regression.
- Accepts stable `C3-R1-F10` (reviewer-native `C3-R1-v02-F10`): v0.2 mixed a
  delivery-local terminal row with an unimplemented package-global denial.
- Selects the narrower delivery-local contract. A new `DeliveryId` may target
  the same package but must independently read and verify it; the failed
  delivery remains terminal and cannot retry/reset/reopen.
- Adds no package tombstone, package-global gate, C2 mutation, route, state or
  new product scope. F01–F09 are not reopened.

### v0.2 — Both Round-1 reviews reconciled

- Preserves the v0.1 candidate SHA and both independent verdicts as historical
  rows; the v0.2 SHA applies only to the reconciliation successor.
- Accepts all eight distinct reviewer findings after source verification. No
  counter-claim is needed. Reviewer-native `F01/F02` labels collided, so stable
  ledger ids `C3-R1-F01` through `C3-R1-F08` are assigned without erasing the
  native attribution.
- Records the shared PackageId-reference recommendation as deferred
  `C3-R1-F09`, pending Homeowner ratification of the v0.2 scope.
- Adds affected-surface and stale-sentinel closure accounting for the v0.2
  closure-only review.

### v0.1 — Ledger opened before Round 1

- Binds the exact C3 v0.1 scope candidate and landed C2 baseline.
- Registers the three-round Homeowner convergence boundary.
- Creates stable finding, counter-claim, affected-surface and review-round
  registers before any independent verdict is received.
- Records initial drafter decisions and explicit deferrals without treating
  them as independent review findings.

## 1. Append-only rule

Accepted review history is never deleted, overwritten, renumbered or silently
rephrased. Later versions may append:

- a new review-round row;
- a new finding/counter-claim row;
- a disposition and successor anchor on an existing open row;
- an affected-surface reconciliation row;
- a stale-sentinel or mechanical-integrity result.

A corrected scope/dispatch receives a new version and SHA. An older FAIL or
PASS remains historical and cannot be projected onto different bytes.

Stable finding ids use:

```text
C3-R<round>-F<sequence>
C3-R<round>-CC<sequence>   (counter-claim)
C3-BUILD-F<sequence>       (later code-exposed finding)
```

## 2. Bound source set

| Source | SHA-256 | Ledger role |
| --- | --- | --- |
| Planning brief | `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC` | C3 ownership and raw/public-reader boundary |
| C2 scope | `B48AECEEEB2CEA2B71A6BA2951BEC7339195DA10AB72713AD0D834051FC56851` | delivery recheck and C2/C3 split |
| C2 dispatch | `0790D1A6828C3365178011F0E10A1B58757B4FE7E2214828C55A6D720B0BB64D` | ratified executable predecessor |
| C2 as-built | `C36855C44C221A1744570E9943B1659F462DE50169BD61A3BF89DDEA997B95AA` | historical closeout snapshot; current landed status is bound separately |
| C2 ledger | `C4721DD2EA5D2B1910E0B46914EB6DBD7FEA5E9C1CE39DFC2DD9080802423511` | predecessor finding/debt provenance |
| Authenticated caller context | `4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401` | caller identity/category/scopes |
| API authenticator | `72A7E14658FFC4F3990D87CA590CD2A19084FC012C3D0813A2351A12750F9CF4` | API authentication port |
| Existing endpoint convention | `3D5EB692A759087651DBB973C1607B826994580A94FFDD392311A933A98A2B64` | HTTP pipeline/error convention |
| C2 internal contracts | `F3882BFCCA3309610F0D46A151131569BF06DC642F30C2A5342D142CDD50B7F5` | exact package read feasibility |
| C2 options | `F0BE59B691CD2E8F0C04F4AB1B6AFF7EE82BE9449870A033940B4045CD11DCE3` | four-credential predecessor |
| C2 provider | `0F5D1CA658CA62D6B2B261FDC3FEEABE438ABEF8839F9FAA6634E5407321502D` | exact-object read implementation |
| Recipient-key entity | `8D73CA3C4ED2F2C43C62A5D754918D60EE554DA551EEB6014D7588C6B207E903` | eligibility source |
| Package entity | `AAFEAA6DED7D4140B7F02F290EB0BFA1B3ACB7CD8DC89219CE8DCD60D156B62E` | finalized package/frozen key source |
| C2 migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` | state/schema/ACL source |

## 3. Review round register

| Round | Artifact | SHA-256 | Reviewer identity | Verdict | Findings | Successor/disposition | State |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | C3 Scope Brief v0.1 | `3890F6D22521C0A320A1A5E48E1695D40DF9A1D99669D6913E6F1C7C6103A571` | Pending independent reviewer(s) | PENDING | none registered yet | Round-1 reconciliation or ratification decision | OPEN |
| 1A | C3 Scope Brief v0.1 | `3890F6D22521C0A320A1A5E48E1695D40DF9A1D99669D6913E6F1C7C6103A571` | CC independent reviewer | PASS with 2 minor findings | native `C3-R1-F01/F02` | both accepted as stable `C3-R1-F07/F08`; PackageId defer recommended | CLOSED_ON_V0.1_BYTES |
| 1B | C3 Scope Brief v0.1 | `3890F6D22521C0A320A1A5E48E1695D40DF9A1D99669D6913E6F1C7C6103A571` | OpenAI GPT-5.6 Thinking independent reviewer | FAIL — NARROW SCOPE v0.2 SUCCESSOR REQUIRED | native `C3-R1-F01`–`F06` | all accepted as stable `C3-R1-F01`–`F06`; PackageId defer recommended | CLOSED_ON_V0.1_BYTES |
| 1C | C3 Scope Brief v0.2 | `4DA27339ACEC4A7F89865D26FCF209E582D8F7DBC3642EA1BDEB608FDBC2406F` | Pending closure-only reviewer(s) | PENDING_AT_BUNDLE_CREATION | closure of `C3-R1-F01`–`F09` | superseded by completed reviewer rows 1D/1E | CLOSED_ADMINISTRATIVELY |
| 1D | C3 Scope Brief v0.2 | `4DA27339ACEC4A7F89865D26FCF209E582D8F7DBC3642EA1BDEB608FDBC2406F` | CC independent reviewer | PASS — ROUND 1 CLOSED | none new | F01–F09 confirmed closed; F09 Homeowner disposition still required | CLOSED_ON_V0.2_BYTES |
| 1E | C3 Scope Brief v0.2 | `4DA27339ACEC4A7F89865D26FCF209E582D8F7DBC3642EA1BDEB608FDBC2406F` | OpenAI GPT-5.6 Thinking independent reviewer | FAIL — ONE NARROW PATCH REGRESSION | native `C3-R1-v02-F10` | accepted as stable `C3-R1-F10`; v0.2.1 required | CLOSED_ON_V0.2_BYTES |
| 1F | C3 Scope Brief v0.2.1 | `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80` | Pending closure-only reviewer(s) | PENDING | closure of `C3-R1-F10`; F01–F09 stale-sentinel only | Round-2 dispatch authoring only after clean review + Homeowner authority | OPEN |
| 2 | C3 executable dispatch | PENDING | Pending | PENDING | PENDING | Round-3 exact-byte candidate | RESERVED |
| 3 | exact corrected dispatch | PENDING | Pending | PENDING | PENDING | build ratification or STOP/RRI | RESERVED |

Round numbers are documentation review rounds, not test attempts or code
mutation rounds.

## 4. Finding and counter-claim register

| Finding id | Round | Reviewer | Classification | Claim | Evidence/anchor | Drafter verification | Disposition | Successor anchor | State |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `C3-R1-PENDING` | 1 | Pending | N/A | Independent review not yet received | v0.1 SHA `3890F6D22521C0A320A1A5E48E1695D40DF9A1D99669D6913E6F1C7C6103A571` | N/A | historical bundle-creation placeholder; superseded by rows 1A/1B | v0.2 successor | CLOSED_ADMINISTRATIVELY |
| `C3-R1-F01` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-F01`) | BLOCKER | One global precedence table contradicts exact replay and durable status after completion/expiry/revocation | v0.1 §§5.1, 5.2, 11 | VERIFIED: replay is historical idempotency, not content reauthorization | ACCEPTED; split CreateDelivery/Status/Content precedence | scope v0.2 §§5.1–5.3, 11 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F02` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-F02`) | BLOCKER | `OutcomeUnknown` conflates ambiguous completion with known pre-response integrity failure | v0.1 §§8, 9, 11–12 | VERIFIED: deterministic mismatch/positive absence is known before any response byte | ACCEPTED; add terminal `IntegrityUnavailable` | scope v0.2 §§8–12, 15, 17–18 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F03` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-F03`) | LATENT_SPEC_GAP | Two-spool capacity has no admission order or zero-mutation denial | v0.1 §8 | VERIFIED: transitioning before permit can burn attempt/fence/lease for process contention | ACCEPTED; fail-fast permit before locked Streaming admission | scope v0.2 §§8, 11.3, 15, 17–18 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F04` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-F04`) | LATENT_SPEC_GAP | Receipt actor could mean POST creator or content-stream caller; `PrincipalId` may be empty | v0.1 §§6, 10 and `AuthenticatedClientContext` | VERIFIED: landed principal defaults empty and the two authenticated moments may use different keys | ACCEPTED; require non-empty principal and bind exact completed stream attempt actor | scope v0.2 §§6, 10–12, 15, 17–18 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F05` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-F05`) | LATENT_SPEC_GAP | Adding a fifth credential to C2 options could create a C2->C3 reverse dependency | C2 options four-credential resolver + v0.1 §13 | VERIFIED: landed C2 validity must not depend on unconfigured C3 | ACCEPTED; C3-owned additive options/readiness, C2 independently valid | scope v0.2 §§2, 13, 15, 18 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F06` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-F06`) | BOOKKEEPING_ONLY | Included C2 as-built retains pending-review status despite landed C2 predecessor | source table + baseline commit | VERIFIED: bytes are historical; commit `7dd15cca...` is landed baseline | ACCEPTED; bind separate bundle-local landed provenance, do not rewrite C2 artifact | scope v0.2 §§0–2, 18 + review bundle | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F07` | 1 | CC independent reviewer (native `C3-R1-F01`) | LATENT_SPEC_GAP | “Fifth capability” omits DB capability and SQL role-pair census deltas | landed C2 config/roles + v0.1 §§2, 13 | VERIFIED: S3 4->5, DB capability 3->4, capability/login roles 3+3->4+4 | ACCEPTED; exact three census families pinned | scope v0.2 §§2, 13, 18 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F08` | 1 | CC independent reviewer (native `C3-R1-F02`) | BOOKKEEPING_ONLY | PackageId distribution debt lacks an exact closure condition | v0.1 §§5, 18 | VERIFIED: no landed recipient reference surface exists | ACCEPTED; exact authenticated recipient-owned reference condition added | scope v0.2 §§5, 18 | CLOSED_PENDING_v0.2_REVIEW |
| `C3-R1-F09` | 1 | Both independent reviewers | DEFERRED | PackageId discovery/issuance is absent but need not expand the bounded authorization-correctness slice | landed route census 0 + v0.1 gap | VERIFIED: possession is not authority; manual/out-of-band reference can exercise C3, but real pilot cannot | `DEFERRED_TO_INTEGRATION_PRODUCT_REACHABILITY`; Homeowner ratification pending | scope v0.2 §§5, 18 | OPEN_ACTIVATION_GATE |
| `C3-R1-F10` | 1 | OpenAI GPT-5.6 Thinking (native `C3-R1-v02-F10`) | PATCH_REGRESSION | v0.2 declares `IntegrityUnavailable` terminal per `DeliveryId` but also claims the whole PackageId becomes non-deliverable without a package-level durable gate | v0.2 §8 versus §§5.1/11.1 | VERIFIED: new idempotency identity had no package-global denial predicate | ACCEPTED; choose delivery-local terminal semantics, allow fresh new-delivery re-verification of same package | scope v0.2.1 §§0, 8, 15.3 | CLOSED_PENDING_v0.2.1_REVIEW |

The historical `C3-R1-PENDING` row records bundle-creation state only. Rows
1A/1B and `C3-R1-F01`–`F09` supersede its administrative wait condition; they
do not rewrite or erase it.

Finding classification is limited to:

```text
BLOCKER
PATCH_REGRESSION
LATENT_SPEC_GAP
TEST_HARDENING_ONLY
BOOKKEEPING_ONLY
DEFERRED
```

Every reviewer claim is source-verified before acceptance. Unsupported,
repeated, loose or out-of-scope claims remain visible as counter-claims rather
than disappearing.

## 5. Initial drafter decision register

These rows are design decisions proposed by the v0.1 drafter. They are not
independent PASS evidence.

| Decision id | Decision | Primary scope anchor | Reason | Review state |
| --- | --- | --- | --- | --- |
| `C3-D01` | direct `BusinessConsumer` recipient only | §§5–6 | minimizes authorization ambiguity and cross-client leakage | SUPPORTED_BY_ROUND1; v0.2_CLOSURE_PENDING |
| `C3-D02` | dedicated `business.raw-export.package.download` scope | §5 | delivery is distinct from generic session read | SUPPORTED_BY_ROUND1; v0.2_CLOSURE_PENDING |
| `C3-D03` | POST authorization + GET status/content | §5 | durable idempotency, restart and receipt semantics | RECONCILED_BY_F01; v0.2_CLOSURE_PENDING |
| `C3-D04` | no list/search/presign/range/HEAD | §§4–5 | bounded initial surface and exact admission per stream | SUPPORTED_BY_ROUND1; v0.2_CLOSURE_PENDING |
| `C3-D05` | exact frozen key eligibility rechecked twice | §§7–8 | C2 makes revocation/expiry a later-delivery gate | RECONCILED_BY_F01; v0.2_CLOSURE_PENDING |
| `C3-D06` | key -> package -> delivery lock order; one post-lock clock | §§7, 15.2 | avoids stale admission time and lock inversion | EXTENDED_BY_F03; v0.2_CLOSURE_PENDING |
| `C3-D07` | bounded encrypted spool verifies before response start | §9 | prevents knowingly emitting corrupt bytes | RECONCILED_BY_F02_F03; v0.2_CLOSURE_PENDING |
| `C3-D08` | completion means server stream completion only | §§3, 10 | matches observable evidence and prevents receipt overclaim | RECONCILED_BY_F02_F04; v0.2_CLOSURE_PENDING |
| `C3-D09` | fifth distinct delivery-reader credential | §§2, 13 | no reconciler/lifecycle credential reuse | RECONCILED_BY_F05_F07; v0.2_CLOSURE_PENDING |
| `C3-D10` | fixed 30m/30m/35m timing and two spools/process | §8 | deterministic bounded initial profile | EXTENDED_BY_F03; v0.2_CLOSURE_PENDING |
| `C3-D11` | reference-presented PackageId; no inbox in v0.1 | §5 | keeps delivery slice bounded but baseline has no public issuance channel | DEFERRED_BY_F08_F09; HOMEOWNER_RATIFICATION_PENDING |

## 6. Initial deferred branch register

| Debt/gate | Branch | Reason deferred | Current effect | Owner/future slice | State |
| --- | --- | --- | --- | --- | --- |
| `C3-DELEGATED-DELIVERY-V1` | delegated/operator delivery | authority semantics not ratified | only direct recipient may deliver | later C3 amendment | OPEN |
| `C3-RECIPIENT-ACK-V1` | recipient-signed acknowledgement | no recipient signing protocol/identity | receipt is server-stream-only | separate contract | OPEN |
| `C3-RANGE-RESUME-V1` | Range/resumable/heavy media | integrity/retry/receipt model expands materially | full-object retry only | heavy-media slice | OPEN |
| `C3-PUBLISHER-SIGNATURE-V1` | TagEkyc package/publisher signature | C2 v1 reserved slots but no selected signer | no signature claim | crypto decision slice | OPEN |
| `C3-RECIPIENT-MANAGEMENT-V1` | enrollment/rotation/revocation API | C2 registry is deployment-seeded | C3 read/recheck only | recipient-management slice | OPEN |
| `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` | recipient inbox/reference issuance | no public PackageId channel exists at baseline | reference-presented C3 cannot claim product discovery and cannot support real recipient pilot | later integration/product-reachability slice; closes only on authenticated exact-recipient owned-reference surface | DEFERRED_PENDING_HOMEOWNER_RATIFICATION |
| `C3-PRODUCTION-ACTIVATION-V1` | real provider/raw adapter/deployment | operational/legal gates remain open | fixture/reference evidence only | activation program | OPEN |

## 7. Affected-surface map template

Every accepted finding must append a completed row before a successor artifact
is sent for review.

| Finding/rule changed | Sections patched | DTO/API impact | State/ordering impact | Hash/audit impact | Test/mutation impact | STOP/gate impact | Stale-sentinel sweep |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| `C3-R1-F01` route precedence | §§5.1–5.3, 11, 15, 17–18 | three routes unchanged; semantics split | replay/status historical; content gated | no codec change | replay after revoke/complete/expire + route-crossing mutations | no scope expansion | global precedence and “terminal replay 410” absent |
| `C3-R1-F02` integrity state | §§8–12, 15, 17–18 | status adds one state token | known terminal versus ambiguous terminal split | receipt remains absent | mismatch classification and retry-prohibition mutations | no C2 repair authority | deterministic failure no longer maps OutcomeUnknown |
| `C3-R1-F03` spool admission | §§3, 8, 11.3, 15, 17–18 | content adds capacity 503 | permit before locks/Streaming CAS | none | zero-mutation capacity proof | process bound remains 2 | no durable wait-for-capacity path |
| `C3-R1-F04` receipt actor | §§6, 10–12, 15, 17–18 | non-empty principal 403 | per-attempt actor frozen before I/O | receipt binds completed attempt | different POST/content actor mutation | no auth-model rewrite | no ambiguous `AuthenticatedPrincipalId` source |
| `C3-R1-F05/F07` capability ownership/counts | §§2, 13, 15, 18 | none | C3 additive readiness only | none | C2-disabled-independence + exact census/IAM mutations | path allowlist deferred to dispatch | no C2 five-credential prerequisite |
| `C3-R1-F06` predecessor provenance | §§0–2, 18 + bundle | none | none | exact landed commit evidence | mechanical provenance check | no predecessor rewrite | stale as-built header explicitly historical |
| `C3-R1-F08/F09` PackageId gate | §§5, 18 | no route added | none | none | future integration proof only | real pilot/product activation remains blocked | no discovery/notification claim |
| `C3-R1-F10` integrity ownership | §§0, 8, 15.3 | no route change | `IntegrityUnavailable` terminal only for exact DeliveryId; no package tombstone | no codec change | same-DeliveryId retry forbidden; new DeliveryId performs fresh full verification | no schema/authority expansion at scope | package-global non-deliverability claim absent |

An accepted finding is not closed by one local wording edit. The drafter must
trace it through all affected active sections, matrices, outcomes, tests,
non-claims, counts, changelog and ledger state.

## 8. Required review coverage attestation

Round 1 is a full-system review. Each reviewer report must state coverage of:

| Surface | Required questions |
| --- | --- |
| lifecycle/state | Are Authorized/Streaming/Interrupted/IntegrityUnavailable/OutcomeUnknown/ServerStreamCompleted/Expired reachable, exclusive and restart-safe? |
| security/data boundary | Can any caller enumerate packages, cross recipient identity, obtain locator/key material or reach plaintext? |
| hash/audit/evidence | Is idempotency/receipt derived from durable/runtime output rather than request echo? |
| API/error precedence | Are authentication, validation, non-disclosure, revocation, Range and provider errors deterministic? |
| concurrency/time | Does key/package/delivery lock order avoid deadlock and stale-time admission? |
| provider integrity | Can bytes start before exact object verification or through the wrong credential/member? |
| repo feasibility | Do landed C2 fields, state and provider ports support the proposed contract without reopening C2 crypto? |
| scope/STOP gates | Is any recipient management, private key, production activation, raw adapter or heavy-media behavior smuggled in? |

After Round 1 establishes full coverage, Round 2/3 review only invalidated
surfaces plus a lightweight stale-sentinel sweep unless a patch changes a core
invariant, scope or runtime/API surface.

## 9. Stale-sentinel register

| Sentinel | Round 1 | Round 2 | Round 3 |
| --- | --- | --- | --- |
| old version/status label | v0.1 historical; v0.2 current | PENDING | PENDING |
| unresolved `MAY`/`SHOULD`/`if feasible` affecting implementation | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| missing/pending exact error or state | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| public/presigned/list/range drift | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| `RecipientReceipt`/possession/decryption overclaim | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| recipient id accepted from caller input | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| C2 reconciler/lifecycle credential reused for delivery | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| response begins before integrity verification | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| provider/network I/O under DB lock | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| raw idempotency/API key/object locator logged or persisted publicly | v0.2 DRAFTER_SWEEP_PASS | PENDING | PENDING |
| delivery-local versus package-global integrity authority | v0.2 FAIL_F10; v0.2.1 DRAFTER_SWEEP_PASS | PENDING | PENDING |

## 10. Three-round convergence control

The Homeowner C3 workflow is:

```text
Round 1: Scope Brief full-system review
Round 2: executable Dispatch review
Round 3: exact-byte closure review
```

At the end of Round 3:

- if no contract blocker remains, freeze documentation and move executable
  uncertainty to code and discriminating tests under separate build authority;
- if a genuine contradiction, unsafe boundary or required unauthorized path
  remains, STOP/RRI for Homeowner decision;
- do not continue whole-document review/patch loops by relabeling the round;
- hardening/bookkeeping-only observations become explicit deferred rows unless
  they invalidate a load-bearing invariant.

This local three-round rule is a Homeowner process decision for C3. It does not
weaken the active TIP-88C1 PI-TAG-001 semantic matrices, evidence-source rules
or STOP/RRI boundaries.

## 11. Bundle and mechanical integrity register

| Artifact | SHA-256 | Encoding/path checks | State |
| --- | --- | --- | --- |
| Scope v0.1 | `3890F6D22521C0A320A1A5E48E1695D40DF9A1D99669D6913E6F1C7C6103A571` | UTF-8, repository path fixed | READY_FOR_BUNDLE |
| Ledger v0.1 | computed after creation; bound by bundle `SHA256SUMS` | UTF-8, repository path fixed | READY_FOR_BUNDLE |
| Round-1 v0.1 review bundle | `498A4DFC9A561CBCDB819062286EDD97CFADFA921DE4B49092527DFCFD8F0C68` | 21 entries; 20/20 sums; no absolute/backslash/traversal/duplicate paths | VERIFIED_HISTORICAL |
| Scope v0.2 | `4DA27339ACEC4A7F89865D26FCF209E582D8F7DBC3642EA1BDEB608FDBC2406F` | UTF-8, repository path fixed | READY_FOR_CLOSURE_BUNDLE |
| Ledger v0.2 | bound after finalization by bundle `SHA256SUMS` | UTF-8, repository path fixed | READY_FOR_CLOSURE_BUNDLE |
| Round-1 v0.2 closure bundle | `A98A4570FEA6E175F3AE378FA20B9A4089D641C9C6AB0CD62584E5E3B8F7C0E5` | 25 entries; 24/24 sums; no absolute/backslash/traversal/duplicate paths; includes C2 landed provenance | VERIFIED_HISTORICAL |
| Scope v0.2.1 | `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80` | UTF-8, repository path fixed | READY_FOR_NARROW_CLOSURE_BUNDLE |
| Ledger v0.2.1 | bound after finalization by bundle `SHA256SUMS` | UTF-8, repository path fixed | READY_FOR_NARROW_CLOSURE_BUNDLE |
| Round-1 v0.2.1 narrow closure bundle | computed outside repository | no absolute/backslash/traversal/duplicate paths; includes both v0.2 verdicts | PENDING |

Bundle SHA and sidecar are external review transport evidence and do not
authorize implementation or commit.

## 12. Non-authorization

This ledger records documentation review only. It grants no source/test/API,
schema/migration, provider operation, environment, package, stage, commit,
push, merge, PR, deployment, production activation, real Raw BIO, recipient
management, decryption, acknowledgement or delivery execution authority.

## 13. Round-1 ratification successor — appended 2026-08-18

This section is the current append-only successor to the historical header and
rows above. It does not rewrite their then-current PENDING states.

| Item | Exact disposition |
| --- | --- |
| Baseline | `7dd15cca6b2159d08220167fe5f0b4cf95fca195` |
| Scope | v0.2.1, SHA `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80` |
| Reviewed bundle | `205CC41823995F9F882305CF3FE4F139DC2CCD125C7F21770C03A71E27B1E0A1` |
| Review disposition | both independent closure reviews accepted as PASS; Round 1 CLOSED |
| `C3-R1-F10` | CLOSED on exact v0.2.1 bytes; delivery-local terminal semantics retained |
| `C3-R1-F09` | Homeowner-ratified `DEFERRED_TO_INTEGRATION_PRODUCT_REACHABILITY` |
| Product effect | reference-presented only; real recipient/hospital pilot and end-to-end delivery UX blocked |
| Next authority | Round-2 dispatch authoring only; no implementation |

The F09 gate closes only when a separately authorized authenticated surface
allows the exact recipient to list or receive opaque PackageId references it
owns without disclosing another recipient's package. Manual or authorized
out-of-band reference presentation does not close the gate.

## 14. Round-2 executable dispatch candidate — appended 2026-08-18

| Round | Artifact | SHA-256 | Drafter state | Independent verdict | Successor |
| --- | --- | --- | --- | --- | --- |
| 2A | `tip_88c1_c3_authenticated_package_delivery_build_dispatch.md` v0.1 | bound by the Round-2 review bundle `SHA256SUMS` | `ROUND_2_REVIEW_CANDIDATE` | PENDING | one exact-byte Round-3 correction cycle or Homeowner decision |

Task-0 concrete principal census:

```text
PostgresHashedApiKeyStore
-> persisted ApiKeyRow.PrincipalId
-> ResolvedApiKey.PrincipalId
-> AuthenticatedClientContext.PrincipalId

Disposition: PASS_ON_LANDED_AUTHORITY
Runtime fallback to ClientApplicationId: FORBIDDEN
Guid.Empty admission: FORBIDDEN / 403
Authentication-surface edit: NOT REQUIRED and not allowlisted
```

Round-2 candidate census:

```text
permanent paths:       38
C3 tables:              2
C3 SQL functions:       9 (8 callable + 1 owner-only trigger)
C3 capability roles:    1
C3 LOGIN roles:         1
C3 S3 credentials:      1 additive DeliveryReader
proof methods:         26 (C301-C326)
scratch mutations:     34 (C3M01-C3M34)
durable states:         7
active transitions:    10
public routes:          3
```

Inherited-debt disposition:

| Debt | Round-2 handling | Waiver? |
| --- | --- | --- |
| `SHARED-DB-MIGRATION-STATE-DEBT-01` | disposable current-migration database, exact finally cleanup, no shared-template Down | no |
| `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` | normalize old/new catalog text to LF then require exact visible-content equality; preserve Git mapping evidence | no |

Round-2 stale-sentinel sweep at candidate creation:

| Sentinel | Result |
| --- | --- |
| old scope/version/current status projected as authority | PASS — historical labels remain historical; §13 is current successor |
| implementation-affecting `MAY`/`SHOULD`/`if feasible` | PASS — none in active dispatch requirements |
| pending state/outcome/error/count | PASS — closed sets pinned |
| discovery/inbox/pilot overclaim | PASS — F09 gate remains active |
| recipient id accepted from request | PASS — authenticated ClientApplicationId only |
| empty/fallback PrincipalId | PASS — persisted non-empty or 403 |
| C2 credential/options reverse dependency | PASS — additive C3 options/readiness only |
| package-global IntegrityUnavailable | PASS — exact DeliveryId only |
| response before verification | PASS — sealed full spool first |
| provider I/O under DB lock | PASS — commit precedes GET |
| receipt means client receipt/decryption | PASS — server-stream completion only |
| shared DB or line-ending debt silently waived | PASS — both pre-allowlisted and explicit |

Round-2 review must bind the exact dispatch and ledger hashes from the external
review bundle. A PASS on Scope v0.2.1 does not substitute for a dispatch review.
The only documentation successor after Round 2 is one exact-byte Round-3
correction cycle. After Round 3, remaining executable uncertainty moves to code
and discriminating tests unless a genuine contract contradiction requires
Homeowner adjudication.

## 15. Round-2 independent review reconciliation — appended 2026-08-18

The exact Round-2 candidate and transport were:

```text
Dispatch v0.1 SHA-256:
016DCB116B6AC3D277CED6301DB1DC1A96E88CE9D4D6134234BC8A561EA6C96C

Ledger SHA-256 at Round-2 bundle freeze:
2E3111C6133B727AED0BDD517A4783F77727BBE9CE4B0FB749293879152CD0CD

Round-2 review bundle SHA-256:
A9BF58120EBA1F7A2251A8AC9EBBE4EB28E3B4366B40115543E41DE22D7A6D87
```

Both independent verdicts bind those exact bytes:

| Row | Reviewer | Verdict | Native findings | Disposition |
| --- | --- | --- | --- | --- |
| 2B | CC independent reviewer | `FAIL — 1 CONTRACT_BLOCKER` | native `C3-R2-F01`: no canonical proof consumes a real C2-produced package | accepted as stable `C3-R2-F01` |
| 2C | OpenAI GPT-5.6 Thinking independent reviewer | `FAIL — NOT READY FOR HOMEOWNER C3 CONTROLLED-BUILD RATIFICATION` | native `C3-R2-F01`–`F05` plus editorial receipt wording | accepted as stable `C3-R2-F02`–`F07` |

The reviewer-native `C3-R2-F01` labels collide. Stable ledger ids below retain
reviewer attribution and do not renumber either historical report.

| Finding id | Reviewer/native id | Classification | Verified claim | Round-3 disposition | State |
| --- | --- | --- | --- | --- | --- |
| `C3-R2-F01` | CC / native `C3-R2-F01` | BLOCKER | C306/C317 could be satisfied by a self-seeded Finalized row, object and matching metadata; no executable C2-to-C3 compatibility proof existed | C317 now runs landed C2 Prepare-to-Finalize and real C3; test substitution forbidden; tampered real-C2 object drives C3M25/C3M26 RED before output | CLOSED_PENDING_ROUND3_REVIEW |
| `C3-R2-F02` | GPT / native `C3-R2-F01` | BLOCKER | non-null event actor/correlation/evidence fields lacked exact source semantics, especially reconciler terminal events | exact request-correlation domain, six lifecycle evidence domains and seven-event causal-source matrix pinned; reconciler reuses persisted causal actor/correlation | CLOSED_PENDING_ROUND3_REVIEW |
| `C3-R2-F03` | GPT / native `C3-R2-F02` | BLOCKER | Disabled topology and capacity STOP wording contradicted route/auth/nonlocking-probe execution | topology gate placed after auth/shape and before C3 DB/provider; STOP now forbids provider I/O, blocking C3 locks and durable mutation before permit while allowing auth DB and exact nonlocking pre-probe | CLOSED_PENDING_ROUND3_REVIEW |
| `C3-R2-F04` | GPT / native `C3-R2-F03` | BLOCKER | successful copy near 30 minutes had no legal completion transition before the 35-minute lease | 30 minutes is provider/copy cancellation; SQL completion uses the still-live 35-minute lease; clock at/after lease performs existing T09 OutcomeUnknown | CLOSED_PENDING_ROUND3_REVIEW |
| `C3-R2-F05` | GPT / native `C3-R2-F04` | BLOCKER | dispatch renamed the ratified public Range error | exact Scope token `RAW_EXPORT_PACKAGE_RANGE_NOT_SUPPORTED` restored | CLOSED_PENDING_ROUND3_REVIEW |
| `C3-R2-F06` | GPT / native `C3-R2-F05` | LATENT_SPEC_GAP | Task-0 principal handoff could not be reproduced because concrete authenticator bytes were absent from the bundle | Round-3 bundle must carry unchanged `LocalDevApiKeyAuthenticator.cs` and `LocalDevApiKeyValidator.cs` with exact hashes; no path 39 | CLOSED_PENDING_ROUND3_REVIEW |
| `C3-R2-F07` | GPT / editorial observation | BOOKKEEPING_ONLY | “No DTO contains receipt” contradicted public `DeliveryReceiptDigest` | wording narrowed to provider receipt/internal evidence; public C3 receipt remains exact | CLOSED_PENDING_ROUND3_REVIEW |

No Round-2 finding was rejected or counter-claimed. None requires Scope change,
path 39, function 10, state 8, transition 11, proof 27 or mutation 35.

## 16. Round-3 exact-byte candidate — appended 2026-08-18

| Item | Exact value |
| --- | --- |
| Dispatch | v0.2 `ROUND_3_EXACT_BYTE_CANDIDATE` |
| Dispatch SHA-256 | `AB415A0526D65307E0036A450374928BA81BEF53C4E8BACAA7D837997BB86988` |
| Ratified Scope | v0.2.1 `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80` |
| Baseline | `7dd15cca6b2159d08220167fe5f0b4cf95fca195` |
| Permanent paths | 38, unchanged |
| SQL functions | 9, unchanged |
| States/transitions | 7 / 10, unchanged |
| Proofs/mutations | 26 / 34, unchanged |
| Independent verdict | `PENDING_ROUND_3_EXACT_BYTE_REVIEW` |
| Implementation authority | `NONE` |

Round-3 affected-surface closure:

| Finding | Dispatch anchors | Proof/mutation owner | Stale-sentinel result |
| --- | --- | --- | --- |
| `C3-R2-F01` | §§2.3, 12 | C306/C317; C3M25/C3M26 | direct-seeded canonical substitute explicitly forbidden |
| `C3-R2-F02` | §§5.4, 6.2, 12 | C309/C321; C3M12/C3M31 | every event has exact actor/correlation/evidence source; reconciler identity forbidden |
| `C3-R2-F03` | §§3.1, 4.3, 12, 15 | C302/C310; C3M02/C3M14 | “any DB before capacity” absent; auth DB and nonlocking pre-probe explicit |
| `C3-R2-F04` | §§7, 8.4–8.5, 12 | C313/C320; C3M17/C3M30 | no completion check against 30-minute SQL clock remains |
| `C3-R2-F05` | §§4.2, 12 | C319 | stale `RAW_EXPORT_PACKAGE_DELIVERY_RANGE_NOT_SUPPORTED` count zero |
| `C3-R2-F06` | §2.2 + bundle evidence | C301/Task-0 | concrete authenticator and validator included as unchanged evidence |
| `C3-R2-F07` | §4 | mechanical wording | provider receipt is private; public DeliveryReceiptDigest remains declared |

Round-3 closure review is delta-focused but must perform a lightweight
stale-sentinel sweep across Scope binding, counts, route/error tokens,
state/outcome closure, C2/C3 ownership and the three-round boundary. A clean
Round-3 verdict may advance only to Homeowner controlled-build ratification.
It grants no implementation, provider operation, stage, commit, push, merge,
PR, deployment, production activation, PackageId discovery or delivery.

## 17. Controlled-build amendments and proof self-correction — appended 2026-08-18

| Record | Observed condition | Homeowner disposition | Evidence/state |
| --- | --- | --- | --- |
| `C3-BUILD-A01` | Three raw-worktree ModelSnapshot tripwires existed; two were outside the original 38 paths | exact count-neutral allowlist amendment 38 -> 40; all three pins move together | snapshot/pins `467D7B65128C5BDCE18B2417E19AFD4C74E21C2D468087E52511695604D71A9E`; C201 PASS; `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` updated |
| `C3-BUILD-RRI01` | C3M11 inverted the real package/key locks but C308 stayed GREEN because whole-function `IndexOf` matched `%ROWTYPE` declarations | HARD STOP accepted; correct existing C308 count-neutrally by extracting only `SELECT ... FOR UPDATE` statements | corrected C308 canonical PASS; C3M11 RED at order assertion; fourth-lock scratch RED `3/4`; C3M10 RED; migration restored `3AB1F7F838ABF9D914B7149C73E3A5CD8F15C95FFD4B2A2A1CB427D244EF250F`; CLOSED |

The false-green mechanism was declaration-order versus lock-order, not a
production defect. The mandatory-mutation STOP prevented unsupported closeout
and operated as designed. Census remains 40 permanent paths, 9 SQL functions,
7 states, 10 transitions, 26 proofs and 34 mutations.

Independent closure bound narrow bundle SHA-256
`A9D37871BF7624EF5375D709DF9713828DF7FB4BC136006A4BF5EE9058FB9C96`.
Both reviewers returned `PASS — C308 PROOF CORRECTION CLOSED; CONTROLLED C3
BUILD MAY RESUME`; neither verdict grants C3 implementation closeout or commit.

| Record | Observed condition | Required disposition | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI02` | C3M24 removed the final length/ciphertext rejection but C316 stayed GREEN because `[..^1]` truncates the minimal two-byte envelope itself; neighboring `EnvelopeMalformed` still maps to the same generic `IntegrityUnavailable` assertion | existing C316 now uses a complete envelope plus body and truncates only the body; exact persisted kind asserted at C316 and all three sibling sites; C3M24/wrong-kind/C3M25/C3M26 RED independently; positive controls 3/3, 3/3 and 8/8 PASS; no production change | `CLOSED_BY_COUNT_NEUTRAL_PROOF_CORRECTION` |

`C3-BUILD-RRI02` also closed the class-level evidence gap: the sparse matrix
requires non-null `IntegrityFailureKind` in `IntegrityUnavailable`, but no proof
previously asserted its exact value. The four exact values now covered are
`CiphertextMismatch`, `CiphertextMismatch`, `EnvelopeMismatch` and
`ObjectAbsent`. Census remains 40/9/7/10/26/34.

| Record | Observed condition | Required disposition | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI03` | C3M34 removed the exact `SECURITY DEFINER` (`p.prosecdef`) readiness predicate but C324 remained GREEN; C324 only performs a coarse wildcard function/grant census and never executes valid durable readiness or asserts exact function posture | strengthen existing C324 count-neutrally so valid C3 readiness is exercised and independently mutated function posture/ACL/catalog predicates RED; preserve proof/path census | `OPEN_STOP_RRI` |

Before this STOP, C3M27-C3M33 each RED for the assigned response-start,
delivery-local error, fresh-read, live-lease, causal-actor, receipt-codec and
process-cache discriminator, followed by canonical restore PASS. C3M34 is not
closed by selecting an easier snapshot-only mutation; the exact readiness
posture class remains unproved. Census remains 40/9/7/10/26/34, staged paths
remain zero, and no production mutation is retained.

| Record | Observed condition | Required disposition | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI04` | authorized exact C324 census observed PostgreSQL's 63-byte catalog identifier `raw_export_record_recipient_package_delivery_integrity_unavaila`, while the migration/validator contract pins the longer `...integrity_unavailable`; SQL calls resolve through truncation but validator text equality cannot | Homeowner must adjudicate one canonical <=63-byte function identifier and authorize coordinated migration, validator, repository and count-neutral proof updates; then rerun C324 canonical plus all C3M34 posture mutations | `OPEN_CONTRACT_CONTRADICTION` |

The incomplete C324 candidate was removed and the C3 test restored to SHA-256
`13B682B61F1D3F46DE5A5CB5DBEDB6F9D4A209DE7855134688603F2B196624FD`.
No production byte was changed. A later Docker read-only-filesystem startup
failure is environment-invalid evidence and is excluded from adjudication.

| Record | Observed condition | Required disposition | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI04-CLOSURE` | Homeowner-authorized 57-byte canonical function name replaced the single 66-byte offender at exactly seven production sites; old-name census is zero and production semantics/signature/ACL are unchanged | retain coordinated rename and complete the standing length guard/catalog round trip only after canonical durable readiness can execute | `RENAME_APPLIED_PROOF_COMPLETION_PENDING` |
| `C3-BUILD-RRI05` | real C324 positive control reached `RecipientPackageDeliveryReadinessValidator`, whose synthetic key `raw-export/c2-package/v1/readiness-missing-{Guid:N}` is rejected by the landed C2 object-key codec before provider probing | narrow production authority is required to construct the readiness missing-object key with `RecipientPackageCodec.ObjectKey(Guid.NewGuid())`, then reapply count-neutral C324 correction and execute canonical/posture/length-guard evidence | `OPEN_STOP_RRI` |

RRI05 is independent of identifier truncation. It is a deterministic production
readiness defect, not Docker or provider instability. The failed C324 run is
preserved at TRX SHA-256
`899AB2F53A4AA0BEDDF44C163EA6706AAB7053E1B00EF8506FBC9371A91E3A64`.
The proof candidate was restored to
`13B682B61F1D3F46DE5A5CB5DBEDB6F9D4A209DE7855134688603F2B196624FD`;
the controlled sweep remains stopped before C3M34. Census remains
40/9/7/10/26/34 and no stage, commit or push occurred.

| Record | Observed condition | Homeowner disposition and closure evidence | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI05-CLOSURE` | readiness used a hand-built prefix-plus-50-character key rejected by the exact C2 codec before provider probing | one-line correction now uses `RecipientPackageCodec.ObjectKey(Guid.NewGuid())`; canonical real durable readiness PASS; validator restored `C873C283CA9AFCD3BE7C3310A19931EA2E2CF9A4AC5AAF7650F7F3F037BA7A03` | `CLOSED_BY_CANONICAL_OBJECT_KEY_PRODUCER` |
| `C3-BUILD-RRI03-CLOSURE` | C324 did not previously execute durable readiness or discriminate exact catalog posture | C309/C324 now use exact nine-name/signature census; C324 runs real validator, catalog round trip and negative posture partition; SECURITY DEFINER/owner/search-path/grantable/extra-overload comparator mutations all RED; every catalog mutation restored and canonical C309/C324 `2/2 PASS` | `CLOSED_BY_COUNT_NEUTRAL_BEHAVIORAL_PROOF` |
| `C3-BUILD-GUARD01` | PostgreSQL identifier truncation had recurred for the third time | existing C324 now reflects the readiness manifest and rejects any 64+ UTF-8-byte declared identifier; deliberate 64-byte mutation RED and canonical restore PASS | `CLOSED_BY_STANDING_LENGTH_GUARD` |

Both the 66-byte identifier contradiction and the invalid readiness probe key
were exposed only after C324 was strengthened to invoke the exact catalog and
real validator. The proof correction therefore performed its intended
contract-discovery role rather than merely satisfying a textual census.

Final affected validation is Integration `22/22`, Unit `3/3`, Architecture
`1/1`; Release build is `0 warnings / 0 errors`. Exact final hashes are:

- validator `C873C283CA9AFCD3BE7C3310A19931EA2E2CF9A4AC5AAF7650F7F3F037BA7A03`;
- repository `3652ABC50AA4C088290E30FA29C487FCAD281A4362272ECCDB3984B453999FB9`;
- migration `75EC3815FD75878B9A143727D41044B95A0C5C808F239828E3B4FB6566FE7D36`;
- C3 integration test `7AE80441EF8D7F927910F27093407B48D5A43518095E9B8312136FBC087A0516`.

The first post-key-fix SECURITY DEFINER mutation run occurred before the
negative posture partition was added and remained GREEN; it is rejected
historical evidence. The final discriminating run RED with `No exception was
thrown` at the exact SECURITY INVOKER subcase. No stage, commit or push occurred.
The next permitted state is controlled closeout preparation, not commit.

## 18. Mutation-provenance reconstruction and C3M07 correction — appended 2026-08-19

| Record | Observed condition | Homeowner disposition and evidence | State |
| --- | --- | --- | --- |
| `C3-BUILD-EVIDENCE01` | Durable artifact search found no retained provenance for C3M01-C3M09, C3M12-C3M13, C3M16 or C3M17; summary prose alone was insufficient for final 34/34 closeout | reconstruct from canonical positive controls; retain TRX under `TIP88C1-C3-working-evidence/mutations-reconstructed`; do not run full suite until complete | `CLOSED_BY_RECONSTRUCTED_DURABLE_EVIDENCE` |
| `C3-BUILD-RRI06` | removing only `k."State"<>'Active'` remained GREEN because C307's revoked fixture also incremented revision and the revision comparator rejected first | count-neutral C307-only correction isolates state while revision/fingerprint/identity/version/window match | `CLOSED_BY_COUNT_NEUTRAL_PROOF_CORRECTION` |

Corrected C307 canonical PASS. Independent mutation outcomes:

- C3M07 state RED: expected `Ineligible`, actual `Created`;
- C3M08 fingerprint RED and revision RED at their own scenarios;
- C3M09 `ValidFromUtc` RED and `ValidUntilUtc` RED at their own scenarios;
- C3M12 event-shape NULL weakening RED because no exception was thrown;
- C3M13 append-only trigger removal RED because no exception was thrown;
- C3M16 revision comparator removal RED: expected `StateConflict`, actual
  `Completed`;
- C3M17 post-lease classification bypass RED: expected `OutcomeUnknown`, actual
  `Completed`.

Final C3 integration proof SHA-256 after the authorized C307 correction is
`AED07D3CDEB1C71DBAA493C4DCF1BA50366D2089747D3DE9B6AD8A55926EF89E`.
Migration restored SHA-256 is
`75EC3815FD75878B9A143727D41044B95A0C5C808F239828E3B4FB6566FE7D36`.
Artifact census resolves every C3M01-C3M34 identifier with no missing cell.

Final authority is bound as `dispatch v0.2 + Homeowner canonical-function-name
amendment`; the dispatch's superseded 66-byte spelling is historical and is not
claimed as the final byte-exact function-name manifest. C324 correction review
R1 received two independent PASS verdicts. Closeout remains at final validation
pending; no stage, commit or push authority exists.

| Record | Observed condition | Required disposition | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI07` | final unfiltered run: Architecture 133/135, Unit 185/195, Integration host startup fails before valid census | reconcile C3 composition and local-dev scope without weakening predecessor behavior; amend allowlist for the exact C2 architecture owner; rerun affected proofs before any replacement full suite | `CLOSED_BY_BOUNDED_COMPOSITION_CORRECTION` |

RRI07 is code/proof integration debt, not environment instability. Deterministic
causes are: unconditional C3 hosted-service registration without a Disabled or
Invalid reconciler; application-service registration without a topology-safe
gateway; modification of the exact existing local-dev business-key scope
profile; and stale C2 architecture assertions that reject the separately owned
C3 public delivery surface. The failed/aborted run is diagnostic evidence only.

RRI07 closure is bound to the Homeowner allowlist amendment 40 -> 41. Program
now gates the hosted reconciler on valid durable topology, Disabled/Invalid use
a dependency-free unavailable gateway, the original business-key scope set is
restored, and one separate C3-only local-dev business identity is added. C2
C225/C226 exempt only the exact two C3 contract types and retain their frozen
36-path census. A rogue `UnexpectedPublicDeliveryContract` made both proofs RED
and was restored byte-identically. C3M02 independently RED for unconditional
hosted-service composition and existing-key scope widening, with canonical
restore PASS. Startup/route evidence covers Disabled, Invalid and valid durable;
affected C3 controls pass 22/22 Integration, 3/3 Unit and 1/1 Architecture.
Census is 41/9/7/10/26/34. The single authorized replacement full suite is the
remaining closeout gate; no stage, commit or push authority exists.

| Record | Observed condition | Homeowner disposition and closure evidence | State |
| --- | --- | --- | --- |
| `C3-BUILD-RRI08` | replacement suite exposed six host-startup failures from topology-unsafe C2 readiness composition, three E3 isolated applies missing the C3 delivery LOGIN prerequisite, and C202 counting C3 objects through three open-ended wildcards | bounded correction in Program, E3 pre-Up bootstrap and C202 only; host `6/6`, E3 `3/3`, C202 canonical PASS; C3 sibling objects leave census unchanged; genuine C2-function removal RED; affected C3 `22/22 + 3/3 + 1/1` | `CLOSED_BY_BOUNDED_COMPOSITION_BOOTSTRAP_AND_EXACT_CENSUS` |

RRI08 did not change schema, migration, SQL functions, roles, states, codecs,
vectors, proof IDs or mutation IDs. The failed replacement run with Integration
TRX SHA-256
`981618633D9DBBD742BE1A288B3A1D924D2BFE837D8777461E5610D7EEC1F255`
is the third rejected historical run and is not closeout evidence. The three
TIP-68 failures were masked signer tests caused by earlier DI validation; no
TIP-68 assertion was weakened and SoftHSM discovery was successful.

Project rule registry amendment:

- `READINESS-OWNERSHIP-RULE-01 / PROOF-LEVEL`: no proof may establish a catalog
  census through an open-ended `LIKE`, prefix or suffix match over `pg_class`,
  `pg_proc`, `pg_roles` or `pg_auth_members`. Proofs must enumerate the exact
  finite set owned by their slice; pattern matching is allowed only when the
  pattern itself is the explicit invariant under test.
- Harness pre-control for an additive migration must allocate both a disposable
  database and pre-Up provisioning/verification of every new LOGIN role the
  migration requires.

Census remains 41/9/7/10/26/34. C3M02 RED for unconditional C2 composition and
restored canonical host controls pass. Other C3M01-C3M34 owner bytes retain
their previously closed canonical hashes. One replacement complete unfiltered
Release suite remains the only closeout execution gate; no stage, commit or
push authority exists.

| Record | Evidence | State |
| --- | --- | --- |
| `C3-BUILD-RRI08-FINAL` | exactly one replacement suite: Contract 13/13, Architecture 135/135, Unit 195/195, Integration 790 PASS / 0 FAIL / 1 intentional manual-generator SKIP; aggregate 1133/0/1/1134; final source hashes unchanged across run | `IMPLEMENTATION_CLOSED_INDEPENDENT_CLOSEOUT_REVIEW_PENDING` |

Canonical final TRX SHA-256 values are Contract
`60CACC92154CC8CB80E7A3A2F0EA56A5015CFADDF1238C73B951E62BDCC8DF64`,
Architecture
`65BF317E1BC37BFA5BFF8CAE7D792BF12685F6137856878E6B743F3EA741D35E`,
Unit `D79CF95521BF81E8DC9F1BBE6449B3ED3C2537D1723E699FA838B24962C7DEE0`,
and Integration
`AAC552217ED489DD21B930727CC3A65FD243CDA63C736D8E6F9CC6551106A95B`.
The three superseded full-suite attempts remain historical-only. No stage,
commit, push, merge, PR, deployment or production activation occurred.

## 19. C3-CR-F01 T05/T06 bounded correction — appended 2026-08-19

| Record | Exact finding/evidence | Disposition | State |
| --- | --- | --- | --- |
| `C3-CR-F01` | Begin returned `Expired` for due `Authorized`/`Interrupted` rows without persisting `Expired`, revision, timestamp or canonical event | bounded correction inside existing Begin SQL and C313; no new function/state/transition/path/proof/mutation ID | `CLOSED_BY_DURABLE_T05_T06_LINEARIZATION` |
| `C3-CR-F01-M17` | C3M17 retained outward `Expired` but removed durable Begin expiry; C313 RED with `Assert.Single() Failure: The collection was empty` | exact migration restore SHA `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD`; C313 restore PASS | `CLOSED_BY_DISCRIMINATING_MUTATION` |
| `C3-CR-F01-TARGETED` | C3 Integration `22/22`, Unit `3/3`, Architecture `1/1`; Release build `0 warnings / 0 errors`; volume census `0 -> 0` | C301-C326 remain `26/26`; census remains `41/9/7/10/26/34` | `GREEN` |
| `C3-CR-F01-FULL` | exactly one replacement unfiltered Release suite: Contract `13/13`, Architecture `136/136`, Unit `195/195`, Integration `790 PASS / 0 FAIL / 1 canonical SKIP`; aggregate `1134/0/1/1135` | final corrected bytes accepted for independent closeout review only | `IMPLEMENTATION_CLOSED_INDEPENDENT_CLOSEOUT_REVIEW_PENDING` |

The rejected RRI08 bundle
`67D287CE576C071B752FFB41822BA286BD64FDA1DDA361AE4013CF6E10437133`
is retained only as historical rejected evidence and must not be committed.
The Architecture `+1` is the separately owned Docker lifecycle guard, outside
the exact 41-path C3 payload; it was exercised because the authorized suite was
complete and unfiltered.

Three already-allowlisted harness paths also carry separately authorized Docker
volume-lifecycle hardening: compose teardown uses `-v --remove-orphans`, and
the two direct PostgreSQL fixtures use `--rm` plus tmpfs data directories.
This is disclosed for byte provenance and does not expand C3 to path 42 or
change production/schema/SQL behavior. The outside-path architecture guard is
reviewed as unrelated dirt and must remain outside a C3 controlled commit.

Final source anchors changed by this bounded correction are migration
`93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD`
and C3 integration proof
`672CE05ED049355DF6E9BE73FB19A5058B4B29026E322CBA90C21E9EDF768071`.
Replacement TRX hashes are Contract
`DA4D299C58140CE3B6B0815004CED75C201A1DD1B1EEB6D96B4A7E633654B063`,
Architecture
`4F0F8ECFE47E683C21F8963A742314A5694ACA185B1C1DCFA5E434372CFD230B`,
Unit `A2F513EDA4425CD5F596A9D4B7B159F7D25D2FA3C074F6809D0DDCF738BBE517`
and Integration
`0978CE11CFC36642093C83D5FFB8D657AAC6B6BD43B1575E01D286D50CB49E7D`.

Anonymous Docker volume census remained `0 -> 0`; no test container residue
remained and SignFlow resources were untouched. Staged paths remain zero. No
commit, push, merge, PR, deployment or production activation authority exists.

## 20. R5 path-42 amendment and targeted TRX closure — appended 2026-08-19

| Record | Binding/evidence | State |
| --- | --- | --- |
| `C3-R5-PATH42` | `DockerTestResourceLifecycleTests.cs` SHA `9C1E7AE07CDBBCC04B0C293C0F5F31D0DA879F12E55744A1585F0169F29CA24D`; Homeowner ratified `41 -> 42` and the existing `1134/0/1` run | `RATIFIED_HARNESS_ONLY` |
| `C3-R5-C313-CANONICAL` | PASS TRX SHA `684EDCF4ACDDC1035D0B08673A62DF1C3DAB21A4FA6F4A32CE7A5082962F8354` | `GREEN` |
| `C3-R5-C3M17` | RED TRX SHA `CCA4E5A0942A49C4B48BCBC17F7AE06726886E999AA9EBF7B09AB688A9F48707`; `Assert.Single() Failure: The collection was empty` | `DISCRIMINATING_RED` |
| `C3-R5-C313-RESTORE` | PASS TRX SHA `521E22E82294E4309B7401ED438CC0CA2CAE2DF6CBBBE9BDE51367220CB8A59E`; migration exact restore `93444EE6...2A9AD` | `GREEN_AFTER_EXACT_RESTORE` |
| `C3-R5-FULL-REUSE` | Homeowner accepts the existing Contract 13 / Architecture 136 / Unit 195 / Integration 790+1 run; Architecture DLL `F5683AF6...7D41`, Integration DLL `A9D820DA...710F` unchanged | `CANONICAL_UNCHANGED_BYTE_EVIDENCE` |

The final census is `42/9/7/10/26/34`. No replacement full suite was run.
Every targeted PostgreSQL run preserved anonymous volumes at `0 -> 0`; Release
build remained `0 warnings / 0 errors`; staged paths remained zero.

For mutation-chain self-containment, the named evidence-of-record for unchanged
C3M01-C3M16 and C3M18-C3M34 is
`TIP88C1-C3-closeout-RRI08-20260819-R2.zip` at SHA-256
`67D287CE576C071B752FFB41822BA286BD64FDA1DDA361AE4013CF6E10437133`.
That bundle remains rejected as a commit candidate but retained as immutable
historical mutation evidence. R5 supersedes only the C3M17 cell with the new
machine-readable RED/restore artifacts.

## 21. Controlled-commit mechanical normalization — appended 2026-08-19

Homeowner authorized the exact reviewed-working-byte to staged-LF-blob
mappings below as representation-only Git index normalization:

| Path | Reviewed working SHA-256 | Staged LF blob SHA-256 |
| --- | --- | --- |
| `20260819120000_Tip88C1C3AuthenticatedPackageDelivery.cs` | `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD` | `E70A0D131BB9A5C73DA05252ABFC4B87A222A020F961A852FB43832DAB192F1E` |
| `20260819120000_Tip88C1C3AuthenticatedPackageDelivery.Designer.cs` | `DF5351FDCEA9D6914C435048385FA246959FC1EFDF60AFBB6CEBAAA7DBAF17C1` | `8499E0F1F89B6D8EBB7890AEA60E7E471FCF3F19499E761CDE54630D32C10FA4` |
| `TagEkycDbContextModelSnapshot.cs` | `467D7B65128C5BDCE18B2417E19AFD4C74E21C2D468087E52511695604D71A9E` | `BA87659488635A585C72648B619D0199F6396FC0CFE96C38F5F441B8F4213EA8` |

Homeowner also authorized the exact 17 reviewed Markdown hard-break
trailing-space exceptions in the C3 as-built, build dispatch and review ledger.
This record changes no executable behavior and grants no push authority.
