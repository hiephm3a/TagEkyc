# TIP-88C1-C5 — Managed Recipient Enrollment & Activation Readiness Review Ledger

Version: `0.1`
Status: `ROUND_1_REVIEW_PENDING — APPEND_ONLY`
Date: `2026-08-23`
Baseline commit: `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f`
Scope candidate: `tip_88c1_c5_managed_recipient_enrollment_activation_readiness_scope_brief.md` v0.1
Owner: `Homeowner + Codex Contractor/Drafter`
Implementation authority: `NONE`

## 0. Changelog

### v0.1 — ledger opened before Round 1

- Binds the C5 docs-only authority, amendment and landed C4 baseline.
- Records the separately authorized P0 line-ending correction and targeted
  evidence.
- Registers product/trust decisions, predecessor closure comparison, debts,
  proof-construction rules and reviewer questions.
- Starts the exact-artifact review chain as pending; no inline or reconstructed
  review can close it.

## 1. Append-only and exact-artifact rule

After the v0.1 candidate is sent for review, this ledger is append-only. Each
successor entry binds:

```text
candidate path and SHA-256
ledger predecessor and successor SHA-256
baseline commit
bundle SHA-256
reviewer identity
verbatim review artifact path and SHA-256
verdict and timestamp
finding/counter-claim IDs
successor disposition
```

Both independent reviewer artifacts must be carried verbatim under `reviews/`.
An artifact unavailable at initial authoring is not fabricated; the state stays
`REVIEW_CHAIN_INCOMPLETE` until a successor bundle carries both.

## 2. Authority and source register

| Source | SHA-256/identity | Role |
| --- | --- | --- |
| Homeowner C5 Round-1 authority + amendment | `0709C9DAC7845A9010E293E6A0EF9DD53F99F6C515A75C669CD96B3757565DAE` | controlling docs/P0 record; original thread message remains highest authority |
| repository baseline | `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f` | landed C1-C4 provenance |
| C4 controlled commit | same baseline | closes technical C4 implementation |
| C4 integration proof file | `1A92ED6E4A5CCB062F325F6DAB7CD29DEE1E8F8A607739E1370EA5E389BC799B` | C410/C411 cross-slice behavior |
| C4 as-built | `045D5922B1C6166F2C0BA24C3DE62D1E49D05821BEA77D2CC7EFCFABBBAA5464` | 24/24, 34/34, 1158/0/1 closeout |
| C4 review ledger | `9DAE198DAADAAC4E0639F21707D2BD515FEDC4722F104476AF7BD3D4C644AED9` | predecessor findings/debts |
| API-key provisioner | `540A810A10A9099BCD333243BC056228B5AE6AFA95773D87C07FBDFD3552A7D1` | current managed writer/gaps |
| PostgreSQL key store | `96497E131EA7C67DE6307760D007E5BD67A4DA808F2627504EF2B842CFB65A91` | current managed readback |
| runtime policy source | `8C19D7529343B8A27D6DAB73A0E7A0E19C27E97E33019CBB27B4D844B7608475` | LocalDev-only policy gap |
| C2 migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` | key selection/snapshot/FK behavior |
| C3 migration | `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD` | current-key delivery/FK behavior |

Reviewers must report `REVIEW_CHAIN_INCOMPLETE` when an invoked artifact or
verdict cannot be reproduced; they must not infer it.

## 3. P0 correction register

| Item | Before | After/current | Evidence | State |
| --- | --- | --- | --- | --- |
| snapshot raw bytes | CRLF `5F8653C3...E8E3C` | LF `07339BB8...232F` | direct SHA/CR census | corrected |
| `.gitattributes` | no rules for C4 migration/snapshot | three exact `eol=lf` rules | `git check-attr` | corrected |
| C201 pin | CRLF hash | LF hash | targeted PASS | corrected |
| R316 pin | CRLF hash | LF hash | targeted PASS | corrected |
| E3 pin | CRLF hash | LF hash | targeted PASS | corrected |

P0 staged paths: `0`. P0 is not committed by this authority.

## 4. Round register

| Round | Candidate | Scope SHA | Ledger SHA | Bundle SHA | Reviewer artifact | Verdict | Successor | State |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Scope v0.1 | `TO_BE_FROZEN` | `TO_BE_FROZEN` | `TO_BE_FROZEN` | `reviews/GPT_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | pending/unavailable | exact successor | `REVIEW_CHAIN_INCOMPLETE` |
| 1 | same exact v0.1 | same | same | same | `reviews/CC_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | pending/unavailable | reconcile both | `REVIEW_CHAIN_INCOMPLETE` |
| 2 | executable Dispatch | unset | appended successor | unset | both exact artifacts required | not authorized | Round 3 | `CLOSED_GATE` |
| 3 | exact-byte reconciliation | unset | appended successor | unset | both exact artifacts required | not authorized | controlled build | `CLOSED_GATE` |

## 5. Finding and counter-claim register

No finding is pre-closed.

| Finding ID | Reviewer | Severity | Claim | Evidence | Drafter disposition | Successor | State |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `C5-R1-PENDING-GPT` | GPT | — | exact artifact unavailable until review occurs | initial bundle | — | append exact artifact and findings | `OPEN_SENTINEL` |
| `C5-R1-PENDING-CC` | CC | — | exact artifact unavailable until review occurs | initial bundle | — | append exact artifact and findings | `OPEN_SENTINEL` |

## 6. Decision register

| ID | Selected rule | Rejected alternative | Required challenge | State |
| --- | --- | --- | --- | --- |
| `C5-D01` | explicit persisted PrincipalId, distinct in proof | default/generate/derive from client | show current managed path can satisfy without correction | `CANDIDATE` |
| `C5-D02` | exact two-scope managed recipient policy | arbitrary or prefix-admitted scope | show any required landed recipient scope omitted | `CANDIDATE` |
| `C5-D03` | immutable replacement-versioned credentials | mutable in-place scopes/secret | test revoke/auth concurrency | `CANDIDATE` |
| `C5-D04` | OperatorAdmin + exact management scope | BusinessConsumer management | identify safe lower authority | `CANDIDATE` |
| `C5-D05` | authenticated narrow management API | unauthenticated/internal caller claims | prove audit actor cannot be spoofed | `CANDIDATE` |
| `C5-D06` | RSA-OAEP-256 SPKI with recomputed SHA-256 | caller-authoritative fingerprint/profile | mutate SPKI/fingerprint independently | `CANDIDATE` |
| `C5-D07` | one Active key, no overlap | multiple Active or Retired state | reconcile with landed unique index/states | `CANDIDATE` |
| `C5-D08` | atomic K1 revoke/K2 activate commit | overlap or asynchronous cutover | test C2 reserve interleavings | `CANDIDATE` |
| `C5-D09` | historical C2 frozen, C3 current gate, C4 visible | re-key/delete/hide history | reconcile C2/C3 Restrict + C410 | `CANDIDATE` |
| `C5-D10` | separate credential/key timelines | conflated recipient credential/key | identify shared identity authority | `CANDIDATE` |
| `C5-D11` | owning validators plus aggregate | duplicate predecessor checks | enumerate exact ownership | `CANDIDATE` |
| `C5-D12` | append-only actor-bound audit | logs or unaudited management | test durable replay/conflict | `CANDIDATE` |
| `C5-D13` | one post-lock clock and guarded transaction | entry-time transaction timestamp | force wait-past-validity | `CANDIDATE` |
| `C5-D14` | managed end-to-end proof | LocalDev/direct seed/fake gateway | attempt substitution | `CANDIDATE` |

## 7. Debt, closure and non-claim register

| Identifier | Disposition | Closure condition | State |
| --- | --- | --- | --- |
| `C4-AUTH-SCOPE-PROVISIONING-DEBT-01` | assigned to C5 | final managed scope/auth proof | `ASSIGNED_OPEN` |
| `C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01` | assigned to C5 | final PostgreSQL principal-chain proof | `ASSIGNED_OPEN` |
| `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` | closed by C4 after four-row comparison | commit `4d5dfe3e...` | `CLOSED_PREDECESSOR` |
| `C4-SYNTHETIC-SIBLING-CONTROL-NONCLAIM-01` | not C5 objective | separate owner | `CARRIED_OPEN` |
| `C4-ROLE-SIBLING-SURFACE-NONCLAIM-01` | not C5 objective | separate owner | `CARRIED_OPEN` |
| `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` | narrow P0 correction | three producer + three tripwire paths; attributes is control path | `PARTIALLY_CLOSED` |
| `SHARED-DB-MIGRATION-STATE-DEBT-01` | pre-control | disposable DB, no shared Down | `CARRIED` |
| `READINESS-OWNERSHIP-DEBT-01` | exact owned sets | RULE-01 proof/production | `CARRIED` |
| `R2-OBS-DEBT-01` | predecessor owner | separate closeout | `CARRIED_OPEN` |
| `R4R6-DURABLE-O-T15-QUARANTINE-EVIDENCE` | predecessor owner | durable evidence gate | `CARRIED_OPEN` |
| `TIP68-HOST-STARTUP-ROOT-CAUSE-01` | separate owner | passive attribution | `MITIGATED_OPEN` |

## 8. C3 reachability closure comparison

| Condition | Exact C4 evidence | Ledger disposition |
| --- | --- | --- |
| exact implementation | C4 commit `4d5dfe3e...` | satisfied |
| real-C2 ownership | C411 real C1/C2 Finalized package appears only for owner | satisfied |
| independent C3 handoff | C411 authorized C3 create succeeds; missing-scope create does not mutate | satisfied |
| controlled closeout | 24/24 proof, 34/34 mutation, 1158/0/1, 36 paths | satisfied |

Successor record:

```text
C3-PACKAGE-REFERENCE-DISTRIBUTION-V1 = CLOSED BY TIP-88C1-C4
```

## 9. Cross-slice feasibility register

| Ratified rule | Landed mechanism | Requires C2/C3 semantic edit? | State |
| --- | --- | --- | --- |
| one current Active key | partial unique Active index | no | feasible |
| rotation hard cutover | Active/Revoked only; guarded transaction can revoke K1 then add K2 | no | feasible |
| frozen historical key | C2 snapshot fields + Restrict FK | no | feasible |
| revocation blocks new use | C2/C3 current key state/revision checks | no | feasible |
| C4 history remains visible | C410 no live-key join | no | feasible |
| overlap/Retired | absent state and conflicts with current model | yes | rejected |

No current `CROSS-SLICE SEMANTICS REQUIRED` STOP applies to the selected model.

## 10. Proof-design register

Round 2 must carry all columns below per obligation:

| Obligation | Fixture | Positive control | One-dimensional mutation | Named RED bite | Restore |
| --- | --- | --- | --- | --- | --- |
| explicit principal | managed PostgreSQL record with distinct ids | authenticated context matches persisted principal | validator emits client id | `C5-PRINCIPAL-PERSISTED` | source SHA + canonical PASS |
| exact scopes | exact two-scope policy/credential | C3+C4 authorize | add one sibling/lookalike scope | `C5-SCOPE-EXACT-SET` | row/catalog restore |
| key fingerprint | valid RSA SPKI | enrollment/ready | alter one SPKI byte only | `C5-SPKI-FINGERPRINT` | DB/source restore |
| rotation | real K1 + K2 | cutover ordering | invert/remove state comparator | `C5-ROTATION-CUTOVER` | byte/catalog restore |
| historical behavior | real C2 package on K1 | snapshot stable/C3-C4 split | hide C4 or rewrite snapshot | named per comparator | restore and positive control |
| readiness ownership | exact owned set + real sibling | sibling excluded | open-ended ownership predicate | `C5-READINESS-OWNED-SET` | disposable DB cleanup |

Standing rules: `ONE-DIMENSION`, `TWO-STATE`, `NAMED GATING`. An unnamed RED
or a GREEN mandatory mutation is a HARD STOP.

## 11. Affected-surface template for Round 2

Round 2 must enumerate exact paths and pre-change hashes; none are authorized
here.

| Surface | Expected semantic delta | Explicit non-delta | Evidence |
| --- | --- | --- | --- |
| managed policy/auth | persisted exact recipient/operator policy and explicit principal | C3/C4 auth meaning unchanged | managed-chain proofs |
| management API | OperatorAdmin-only commands | no BusinessConsumer management | auth/non-disclosure |
| credential lifecycle | issue/replace/revoke + audit | no in-place scope mutation | replay/race proofs |
| recipient keys | enroll/rotate/revoke + audit | C2 package crypto unchanged | SPKI/rotation proofs |
| readiness | C5 owning validators + aggregate | no predecessor duplication | exact set/mutation |
| migration/model | additive C5 state only | C1-C4 constraints/functions unchanged | additive tripwires |
| harness | disposable DB, LF manifests, Docker volume stability | no shared-template Down/prune | harness guards |

## 12. Required Round-1 coverage

| # | Question | GPT | CC |
| --- | --- | --- | --- |
| 1 | C5 boundary correct? | pending | pending |
| 2 | persisted PrincipalId authority exact? | pending | pending |
| 3 | exact C3/C4 scopes provisionable under proposal? | pending | pending |
| 4 | scope grant/revoke actor correct? | pending | pending |
| 5 | key registry authority exact? | pending | pending |
| 6 | one Active key correct? | pending | pending |
| 7 | rotation linearization sound? | pending | pending |
| 8 | revocation linearization sound? | pending | pending |
| 9 | historical C2 binding preserved? | pending | pending |
| 10 | C3 eligibility consistent? | pending | pending |
| 11 | C4 visibility unchanged? | pending | pending |
| 12 | credential/key lifecycles separated? | pending | pending |
| 13 | audit claims exact? | pending | pending |
| 14 | readiness ownership sound? | pending | pending |
| 15 | C1-C4 production semantics unchanged? | pending | pending |
| 16 | managed rather than LocalDev proof mandatory? | pending | pending |
| 17 | deployment/pilot excluded? | pending | pending |
| 18 | Product Ready residual gates explicit? | pending | pending |
| 19 | P0 portable/raw-byte invariant sound? | pending | pending |
| 20 | three proof-construction rules enforceable? | pending | pending |

## 13. Stale-sentinel register

| Sentinel | Meaning | Removal authority | State |
| --- | --- | --- | --- |
| `ROUND_1_SCOPE_CANDIDATE` | Scope not ratified | Homeowner after clean review | active |
| `C5-R1-PENDING-GPT` | exact GPT artifact absent | append-only successor | active |
| `C5-R1-PENDING-CC` | exact CC artifact absent | append-only successor | active |
| `ASSIGNED_OPEN` | C5 debt not implemented | controlled closeout | active |
| `Implementation authority: NONE` | no build permission | Homeowner | active |

## 14. Bundle and mechanical integrity protocol

Initial Round-1 candidate bundle:

```text
repo/<C5 scope path>
repo/<C5 ledger path>
repo/.gitattributes
repo/<three tripwire paths>
repo/<three LF migration paths>
authority/HOMEOWNER_C5_ROUND1_AUTHORITY_AND_AMENDMENT.md
evidence/C5_P0_TARGETED_TESTS.txt
metadata/BASELINE_AND_SELF_CHECK.txt
metadata/FILES_MANIFEST.tsv
metadata/SHA256SUMS
metadata/REVIEW_REQUEST.md
reviews/REVIEW_CHAIN_STATUS.md
```

The initial review request precedes reviewer output. It must explicitly state
`REVIEW_CHAIN_INCOMPLETE`; a successor bundle carries both exact reviews. From
that successor onward, both `reviews/GPT_...` and `reviews/CC_...` are mandatory.

Mechanical gates:

- outer sidecar equals bundle SHA-256;
- all internal sums pass under PowerShell and POSIX `sha256sum -c`;
- all metadata/manifests/sidecar are UTF-8 without BOM and LF-only;
- duplicate/absolute/traversal/backslash paths are zero;
- repository payload equals live bytes;
- baseline/branch exact, staged zero;
- unrelated dirt excluded;
- manifests disclose P0 as the only executable-byte delta.

## 15. Three-round convergence and STOP gates

```text
Round 1 Scope
Round 2 executable Dispatch
Round 3 exact-byte reconciliation
```

STOP/RRI for cross-slice redesign, per-principal ownership, overlap/Retired
keys, inbox/delegation/notification, Raw BIO adapter, deployment/pilot, private
key/decryption, package re-key/replacement, missing review artifact, or any
implementation before separate build authority.

## 16. Current lifecycle

```text
C5-P0:                         CORRECTED / TARGETED PASS / UNCOMMITTED
Scope v0.1:                    ROUND_1_SCOPE_CANDIDATE
Review chain:                 REVIEW_CHAIN_INCOMPLETE
Independent reviews:          PENDING
Homeowner scope ratification: NOT_RECORDED
Round-2 authority:            NONE
Implementation:               NONE
Stage / commit / push:        NONE
```

## 17. Round-1 v0.1 review reconciliation — appended 2026-08-23

This section is an append-only successor to §§0-16. It does not rewrite the
historical `PENDING` sentinels.

### 17.1 Frozen predecessor and review provenance

```text
Predecessor bundle:
E1044B6025B8CA1A5CBE4BE5A590566FB29CD68DA5608C556C9D031EAD863297

Scope v0.1:
DA6CAD60F0DE34E3B6225E3CCC063C82CCED51ECE20602CFE410AB255279411B

Ledger v0.1:
E20599131C704369E538716F48610FDA5367065549F2A4E8FE817B56706EF2AF
```

| Reviewer input | Carried path | SHA-256 | Provenance state |
| --- | --- | --- | --- |
| CC Round-1 review supplied inline by Homeowner | `reviews/CC_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | `2B2DD5E7716DC827FBDAF0CD1400711FA547337099BBDAD5C5E69FC559FE300B` | exact carried artifact |
| GPT review summary supplied by Homeowner | `reviews/GPT_C5_SCOPE_V0_1_ROUND1_REVIEW_SUMMARY_NOT_ARTIFACT.md` | `18AAAA2ED778D334092D635028808567E14075C9BCE7BACF21AB05F983595336` | summary only; not chain-closing |
| GPT exact downloadable artifact claimed by reviewer | required `reviews/GPT_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | claimed `689335CF3CC32EFC3B4C233CAE4DA23FA638C0C4D2005DD9DF1FD2B062AE3D15` | `MISSING — REVIEW_CHAIN_INCOMPLETE` |

The builder does not reconstruct the missing GPT artifact from its summary.
The v0.2 technical correction may be reviewed, but Round 1 cannot be ratified
until the exact artifact bytes are supplied and their hash matches the claim.

### 17.2 Combined finding register and adjudication

| Finding ID | Reviewer | Severity | Finding | Contractor adjudication | v0.2 anchor | State |
| --- | --- | --- | --- | --- | --- | --- |
| `C5-R1-CC-F01` | CC | PRODUCT/TRUST | hard cutover affects pre-existing C3 delivery admission/retry and lacked an operational drain rule | accepted; clarified that `Authorized` and `Interrupted` cannot begin/retry after revoke, while an already admitted active stream is not retroactively killed | Scope §§10, 11, 13, 22 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F01` | GPT | HIGH | old SPKI/fingerprint reuse contradicts recipient-wide historical unique fingerprint | accepted; reuse is permanently prohibited | Scope §10 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F02` | GPT | HIGH | C2 selection/reserve can straddle rotation and return `Unavailable` | accepted; three outcomes plus explicit retry boundary pinned | Scope §§10, 15 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F03` | GPT | HIGH | one-time plaintext cannot be reproduced by durable exact replay | accepted; metadata-only `ExistingMatchSecretUnavailable`, explicit replacement recovery | Scope §§5, 15, 16 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F04` | GPT | HIGH | credential replacement could drift PrincipalId | accepted; managed recipient identity owns canonical pair and every credential binds it | Scope §§5, 13, 16 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F05` | GPT | MAJOR | exact two-scope recipient credential was conflated with broader client policy | accepted; exact activation profile separated from unrelated policy scopes | Scope §6 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F06` | GPT | MAJOR | revoke affects Authorized/Interrupted stream admission, not only new delivery | accepted; same closure as CC finding with landed state precision | Scope §§3.3, 11 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F07` | GPT | MAJOR | no route back to readiness after standalone key revoke | accepted; explicit replacement-after-revocation with next version/new fingerprint | Scope §§11, 13 | `CLOSED_IN_CANDIDATE` |
| `C5-R1-GPT-F08` | GPT | MINOR | P0 debt row said five paths instead of three producers + three tripwires + attributes control | accepted; corrected census | Scope §§0, 18 | `CLOSED_IN_CANDIDATE` |

No finding requires C1-C4 semantic redesign. The selected model remains within
C5: managed metadata/operations/readiness plus unchanged C2/C3 interpretation.

### 17.3 Product/trust successor decisions

```text
planned rotation:
  blocks with DeliveryDrainRequired while K1 has Authorized, Streaming or
  Interrupted C3 deliveries

emergency standalone revoke:
  may proceed fail-closed; reports/audits affected delivery counts; stranded
  work requires a new export because C5 has no re-key

C2 selection/reserve race:
  K1 | K2 | Unavailable; no silent key substitution inside one reserve

credential lost response:
  metadata-only replay; no plaintext recovery; explicit replacement revokes
  the orphaned credential

stable principal:
  managed recipient identity owns PrincipalId; credential versions cannot
  change it

historical key material:
  never reusable for the same recipient because fingerprint uniqueness is
  historical, not Active-only
```

## 18. Round-1 v0.2 lifecycle successor — appended 2026-08-23

```text
C5-P0:                         CORRECTED / TARGETED 3 OF 3 PASS / UNCOMMITTED
Scope v0.2:                    ROUND_1_CLOSURE_CANDIDATE
CC v0.1 artifact:              EXACT / CARRIED
GPT v0.1 summary:              CARRIED AS NON-ARTIFACT
GPT exact v0.1 artifact:       MISSING
Finding reconciliation:       9 OF 9 CLOSED IN V0.2 CANDIDATE
Review chain:                 REVIEW_CHAIN_INCOMPLETE
Homeowner scope ratification: NOT_RECORDED
Round-2 authority:            NONE
Implementation:               NONE
Stage / commit / push:        NONE
```

## 19. Scope v0.3 Round-1 closure successor — appended 2026-08-23

This section is an append-only successor to §§0-18. The complete v0.2 ledger
with SHA-256
`BD477FACB694925833444004071DFC63C228E6312FFAC33EA35E47DA2A399599`
is its exact byte prefix.

### 19.1 Authority and frozen input

```text
Homeowner v0.3 authority artifact:
C919D9C31DA865E21D298E9B30AC57E8DEA0D39D841D3486831C55530732DAF1

Scope v0.2:
9265E655278E0CCD7A735254887B2CD845D53B0AEA644501E98FFA47D36FB9D8

Ledger v0.2:
BD477FACB694925833444004071DFC63C228E6312FFAC33EA35E47DA2A399599

Bundle v0.2:
A0CF5189F2EDFA12204C6256348E0CAA7A581BBA338AC286819C06F92ED7392E
```

### 19.2 Gate 1 — GPT F02 expanded technical closure

The v0.2 statement that all blocking locks precede one clock was overbroad.
Landed C2 `raw_export_reserve_recipient_package` captures
`clock_timestamp()` before its blocking recipient-key `FOR UPDATE`, then uses
that earlier value in the post-wait key validity-window admission check. C5
rotation makes contention on that row reachable, but C5 has no authority to
retrofit C2.

| Required correction | v0.3 disposition | Anchor |
| --- | --- | --- |
| qualify post-lock clock | applies only to C5-owned mutations; landed C2/C3 remain predecessor-owned | Scope §15 |
| record exposure | `C5-C2-PRELOCK-CLOCK-NONCLAIM-01` added with exact pre-lock-clock mechanism, bounded exposure and predecessor ownership | Scope §18 |
| constrain future Dispatch | planned rotation must return `DeliveryDrainRequired` and release without waiting under the key-row lock; longer hold promotes the non-claim to a defect | Scope §§15, 17.4 |

The historical v0.2 `CLOSED_IN_CANDIDATE` row for `C5-R1-GPT-F02` did not yet
cover this clock/lock issue and is superseded by this entry.

```text
C5-R1-GPT-F02: CLOSED_IN_V0.3_CANDIDATE
C5-C2-PRELOCK-CLOCK-NONCLAIM-01: OPEN / PREDECESSOR_OWNED
C2/C3 executable bytes changed: NO
```

All other technical findings remain closed exactly as accepted by the v0.3
authority and are not reopened.

### 19.3 Gate 2 — exact review artifacts

The already carried CC v0.1 review remains evidence. The v0.2 summary
placeholder is not a review artifact and must be absent from the final v0.3
successor bundle.

| Required exact artifact | Required SHA-256 | Current state |
| --- | --- | --- |
| `reviews/GPT_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | `689335CF3CC32EFC3B4C233CAE4DA23FA638C0C4D2005DD9DF1FD2B062AE3D15` | `MISSING` |
| `reviews/GPT_C5_SCOPE_V0_2_ROUND1_CLOSURE_REVIEW.md` | `3427FF2E7F856090B7DF0A52CFB52242EE8496E48836694C24AC2049BC7828B2` | `MISSING` |
| `reviews/CC_C5_SCOPE_V0_2_ROUND1_CLOSURE_REVIEW.md` | supplied verbatim by Homeowner; hash must be measured from supplied bytes | `MISSING` |

The builder will copy supplied bytes only and will not regenerate, summarize or
paraphrase these artifacts. Gate 2 therefore remains
`REVIEW_CHAIN_INCOMPLETE`; no final v0.3 closure bundle is issued yet.

### 19.4 Current lifecycle successor

```text
C5-P0:                         CORRECTED / TARGETED 3 OF 3 PASS / UNCOMMITTED
Scope v0.3:                    ROUND_1_CLOSURE_SUCCESSOR_CANDIDATE
Gate 1 — technical:            CLOSED IN V0.3 CANDIDATE
Gate 2 — review provenance:    REVIEW_CHAIN_INCOMPLETE / 3 ARTIFACTS MISSING
Other accepted findings:       PRESERVED CLOSED / NOT REOPENED
Homeowner scope ratification: NOT_RECORDED
Round-2 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

Round-1 ratification remains a separate Homeowner act. This successor does not
self-grant it.

## 20. Exact-artifact provenance closure — appended 2026-08-23

This section supersedes only the missing-artifact state in §19.3-§19.4. It
does not rewrite the historical record and does not change the Gate-1 technical
disposition.

### 20.1 Exact artifacts received and verified

| Exact artifact carried in v0.3 | SHA-256 | Verification |
| --- | --- | --- |
| `reviews/CC_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | `2B2DD5E7716DC827FBDAF0CD1400711FA547337099BBDAD5C5E69FC559FE300B` | byte-exact carry from v0.2 predecessor |
| `reviews/GPT_C5_SCOPE_V0_1_ROUND1_REVIEW.md` | `689335CF3CC32EFC3B4C233CAE4DA23FA638C0C4D2005DD9DF1FD2B062AE3D15` | supplied file matches mandatory hash |
| `reviews/GPT_C5_SCOPE_V0_2_ROUND1_CLOSURE_REVIEW.md` | `3427FF2E7F856090B7DF0A52CFB52242EE8496E48836694C24AC2049BC7828B2` | supplied file matches mandatory hash |
| `reviews/CC_C5_SCOPE_V0_2_ROUND1_CLOSURE_REVIEW.md` | `E9C0540A44923F99C8A884128F55C147B7D1C0EE10004726F3423A167DE2EB56` | supplied verbatim file; measured from supplied bytes |

`reviews/GPT_C5_SCOPE_V0_1_ROUND1_REVIEW_SUMMARY_NOT_ARTIFACT.md` is retired
and is not present in the v0.3 successor bundle. No reviewer artifact was
regenerated, summarized or paraphrased by the builder.

### 20.2 Review-chain disposition

```text
Gate 1 — technical correction: CLOSED IN V0.3 CANDIDATE
Gate 2 — exact provenance:     CLOSED / 4 EXACT ARTIFACTS CARRIED
Review chain:                  COMPLETE FOR ROUND-1 SUCCESSOR REVIEW
Round-1 ratification:          NOT SELF-GRANTED / HOMEOWNER ACT REQUIRED
Round-2 authority:             NONE
Implementation authority:     NONE
Stage / commit / push:         NONE
```

The next-turn C5 protocol returns to Homeowner-supplied review text plus the
append-only finding/history ledger. It does not require recovery of an
inaccessible reviewer sandbox file unless a future explicit authority says
otherwise.

## 21. Round-1 ratification and Round-2 Dispatch candidate — appended 2026-08-23

This section is an append-only successor to §§0-20. The complete ratified
Round-1 ledger with SHA-256
`87A68407A4E0AAE5D9EDA99D9B8B2C3C9C6332DE98D3A69CED918D0FC9694CC2`
is its exact byte prefix.

### 21.1 Homeowner ratification and authority

```text
Baseline:
4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f

Ratified Scope v0.3:
7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5

Ratified predecessor ledger:
87A68407A4E0AAE5D9EDA99D9B8B2C3C9C6332DE98D3A69CED918D0FC9694CC2

Ratified Round-1 bundle:
EC98C388AFE96A076FE558B2B59714771828632A89588AD3525B294E9E238DD2

Round-2 docs-only authority:
0BF276DDDDD756467525748A9E9C91952E8265E085FC205646E75B8FC0EBC0A5
```

Homeowner records Round 1 closed. `C5-R1-GPT-F01..F08` and
`C5-R1-CC-F01` remain CLOSED; the four exact Round-1 review artifacts form a
complete predecessor chain. Scope v0.3 is the settled product/trust baseline
and is not reopened by Dispatch.

Authority permits only Dispatch authoring, this append and a review-only
bundle. It grants no implementation, migration, code/test edit, stage, commit,
push, merge, PR, deployment or production activation.

### 21.2 Dispatch v0.1 candidate

| Artifact | SHA-256 | State |
| --- | --- | --- |
| `tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md` | `6BC232DF27C7B93C2CD73B3D9273E5752DEEC2D3E3FDBA90CE0F0FE38FC42AFB` | `ROUND_2_EXECUTABLE_CANDIDATE_REVIEW_PENDING` |

Pinned census:

```text
permanent paths     47
new C5 tables        5
existing columns     1 additive key-reason column
supporting indexes   6
SQL functions        9 (8 callable + 1 event guard)
capability / LOGIN    1 / 1
proofs               C501-C530 = 30
mutations            C5M01-C5M48 = 48
```

### 21.3 Feasibility adjudication

| Question | Landed evidence | Dispatch result |
| --- | --- | --- |
| stable managed principal | PostgreSQL row already authenticates persisted PrincipalId; generic writer fallback is bypassed by C5-owned guarded path | feasible without changing authenticated-context semantics |
| exact activation scopes | C3/C4 exact strings are landed; C5 overlay accepts no caller scope list | exact two-scope profile pinned |
| key lifecycle | landed Active/Revoked, historical fingerprint uniqueness and one-Active index | no-overlap hard cutover pinned |
| C2 concurrency | exact key `FOR UPDATE`, stale state/revision returns `Unavailable` | K1/K2/Unavailable proof pinned; no silent reselection |
| C3 drain/current eligibility | create/begin-stream lock exact key and revalidate state/revision/fingerprint | plain post-key-lock MVCC drain query is compatible |
| C4 history | C4 listing does not live-join recipient key | historical PackageId remains visible |
| lock/clock nonclaim | C2 clock precedes its key lock | C5 post-key-lock wait forbidden; nonclaim stays open |
| proof construction | C4 false-green history carried by authority | every proof pins fixture/control/mutation/bite/restore |

No `STOP/RRI — CROSS-SLICE SEMANTICS REQUIRED` is raised by the v0.1 design.
The Dispatch does not repair or overclaim the predecessor C2 clock behavior.

### 21.4 Round-2 review protocol

```text
GPT C5 Dispatch v0.1 Round-2 review: PENDING_INLINE_REVIEW
CC  C5 Dispatch v0.1 Round-2 review: PENDING_INLINE_REVIEW
```

The first candidate bundle records these pending slots rather than inventing
review content. When the Homeowner forwards each exact inline review, the next
successor stores it verbatim under `reviews/`, measures SHA-256 and appends its
finding register. A summary is not a substitute. Round 3 is exact-byte
reconciliation only after both reviews exist.

### 21.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1:             ROUND_2_EXECUTABLE_CANDIDATE_REVIEW_PENDING
Round-2 review chain:         PENDING 0 OF 2
C4 activation debt:           ASSIGNED_OPEN
C4 persisted-principal claim: ASSIGNED_OPEN
C5 C2 clock nonclaim:         OPEN / PREDECESSOR_OWNED
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 22. Dispatch v0.1 Round-2 reviews and v0.2 successor — appended 2026-08-23

This section is append-only. The ledger ending at §21, SHA-256
`0FD72FF4E90D2FD50FA827FD024AE2A0D85B2B06AE5430A1116A03432006DDD8`,
is its exact byte prefix.

### 22.1 Exact Round-2 v0.1 review artifacts

| Reviewer artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_DISPATCH_V0_1_ROUND2_REVIEW.md` | `B3FEA541BAC2C2599405C8FF8B17BA179778371692C9D56F3259E8E7C295A455` | `HOLD — ONE NARROW FINDING` |
| `reviews/GPT_C5_DISPATCH_V0_1_ROUND2_REVIEW.md` | `0685D14704260BF60B145B6BB3C13A56CB2DAA20BAFD1915E3893909CD4958D9` | `ROUND_2_CHANGES_REQUIRED` |

Both artifacts are verbatim Homeowner-forwarded reviews. They are retained as
predecessor review evidence and do not constitute review of v0.2.

### 22.2 Finding register and reconciliation

| ID | Reviewer | Severity | Finding | v0.2 disposition |
| --- | --- | --- | --- | --- |
| `C5-R2-CC-F01` | CC | blocker | strict key revocation CHECK breaks landed predecessor SQL lacking reason | `CLOSED_CANDIDATE`: §5.6 identifies all eight actual sites (the review identified seven); all receive explicit reason, CHECK remains strict, C529/C5M45 own the bite |
| `C5-R2-GPT-F01` | GPT | HIGH | managed auth still depends on LocalDev policy provider | `CLOSED_CANDIDATE`: managed-first persisted recipient/admin policy adapter in allowlisted path, exact DI precedence and zero-LocalDev-call C505/C5M09 |
| `C5-R2-GPT-F02` | GPT | HIGH | emergency warning/counts absent from DTO, operation, event and evidence/replay | `CLOSED_CANDIDATE`: exact result DTO, sparse durable counts, count-bound audit digest and original-count replay in C516 |
| `C5-R2-GPT-F03` | GPT | HIGH | unratified global PrincipalId uniqueness and missing composite api_keys FK index | `CLOSED_CANDIDATE`: global uniqueness removed; six-index slot becomes exact api_keys composite alternate key |
| `C5-R2-GPT-F04` | GPT | MAJOR | credential replacement write order incompatible with partial Active index/self-FK | `CLOSED_CANDIDATE`: exact six-step transaction order plus deterministic rollback control in C507 |
| `C5-R2-GPT-F05` | GPT | HIGH | several owner fixtures/mutations violate ONE-DIMENSION/NAMED-GATING | `CLOSED_CANDIDATE`: C502/C503/C509-C513/C519/C523-C530 corrected count-neutrally; every C5M row now one edit/one bite |
| `C5-R2-GPT-F06` | GPT | HIGH | manager DB connection has no executable configuration/secret/role contract | `CLOSED_CANDIDATE`: exact section, secret authority, no runtime fallback, current_user check and C524/C525 controls in existing paths |
| `C5-R2-GPT-F07` | GPT | MAJOR | algorithm token and audit event/target/reason vocabulary underspecified | `CLOSED_CANDIDATE`: exact case-sensitive algorithm, mapping, seven event types, target encodings and reason authority enumerated |

No finding requires path 48, a sixth table, seventh index, tenth function,
31st proof or 49th mutation. No Round-1 product/trust decision is reopened.

### 22.3 Dispatch v0.2 successor

| Artifact | SHA-256 | State |
| --- | --- | --- |
| `tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md` | `AF7D4428612400F8B84F90E34DF28FCA12670C9DD8BB2B4CAE9B216A9AA8A89B` | `ROUND_2_SUCCESSOR_REVIEW_PENDING` |

Count-neutral census:

```text
permanent paths        47
new C5 tables           5
existing columns        1 additive key-reason column
supporting indexes      6
SQL functions           9 (8 callable + 1 event guard)
capability / LOGIN       1 / 1
proofs                  C501-C530 = 30
mutations               C5M01-C5M48 = 48
predecessor revoke SQL   8 exact sites
```

The audit vector changed intentionally because three durable emergency-count
slots are now included for every event (`-` when absent). The other three
absolute vectors remain unchanged; all four must be recomputed in the v0.2
successor bundle.

### 22.4 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1:             REVIEWED / CHANGES_REQUIRED
C5 Dispatch v0.2:             ROUND_2_SUCCESSOR_REVIEW_PENDING
v0.1 review chain:            COMPLETE 2 OF 2
v0.2 successor review chain:  PENDING 0 OF 2
C4 activation debt:           ASSIGNED_OPEN
C4 persisted-principal claim: ASSIGNED_OPEN
C5 C2 clock nonclaim:         OPEN / PREDECESSOR_OWNED
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 23. Dispatch v0.2 Round-2 reviews and v0.3 successor — appended 2026-08-23

This section is append-only. After the §22 relocation correction, the complete
v0.1 ledger ending at §21 is the exact byte prefix of §22. The ledger ending at
§22, SHA-256
`EE53B87E059400A6B3315ED24FF675E9B9E6D0746B1D3999B8ECC78870D6CAE3`,
is the exact byte prefix of this section.

### 23.1 Exact Round-2 v0.2 review artifacts

| Reviewer artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_DISPATCH_V0_2_ROUND2_SUCCESSOR_REVIEW.md` | `94E68660C95FBB7A21BDADA4F3FFE07750521BD71181A73E166EE3B7A367F859` | `PASS` |
| `reviews/GPT_C5_DISPATCH_V0_2_ROUND2_SUCCESSOR_REVIEW.md` | `ADBF15C76AE9E116DFA40FC6CB189DC7BCC25E8DB76E85D9BEBDBDC8997FDBF1` | `ROUND_2_CHANGES_REQUIRED` |

Both artifacts are verbatim Homeowner-forwarded reviews. The CC review
independently recomputed all five published vector facts and raised no new
finding. The GPT review identified the three successor findings below. These
artifacts are retained as predecessor review evidence and do not constitute
review of v0.3.

### 23.2 Finding register and reconciliation

| ID | Reviewer | Severity | Finding | v0.3 disposition |
| --- | --- | --- | --- | --- |
| `C5-R2-V02-GPT-F01` | GPT | BLOCKER | §22 was inserted before historical §§17-21, so the stated append-only exact-prefix claim was false | `CLOSED_CANDIDATE`: §22 was mechanically relocated after the exact complete v0.1 ledger; the predecessor SHA `0FD72FF4...6DDD8` is now a true byte prefix and historical bytes were not rewritten |
| `C5-R2-V02-GPT-F02` | GPT | HIGH | globally replacing the policy provider could shadow same-client unrelated behavior and expose a generic-provisioner alternate writer | `CLOSED_CANDIDATE`: the global landed provider remains unchanged; only the authenticator factory uses the C5 persisted provider; activation-scope rows require an exact C5 companion, and C504/C505 own same-client behavior, alternate-writer rejection and zero-LocalDev-call controls |
| `C5-R2-V02-GPT-F03` | GPT | HIGH | C5M11/M12/M14/M24/M28/M32/M35/M40 lacked an actual input, isolated layer, executable edit or discriminating negative state | `CLOSED_CANDIDATE`: all eight owners and mutation rows are corrected count-neutrally as recorded in §23.3 |

### 23.3 Eight proof-construction corrections

| Mutation | Corrected fixture/edit | Named bite |
| --- | --- | --- |
| `C5M11` | assign the real wrong source `RecipientClientApplicationId` instead of persisted PrincipalId in replacement rows | `C507-REPLACEMENT-STABLE-PRINCIPAL` |
| `C5M12` | isolate companion Active/Revoked while `api_keys` remains Active and remove only companion-State enforcement | `C508-MANAGED-COMPANION-STATE-ENFORCED` |
| `C5M14` | remove only application algorithm-literal comparison; wrong literal must fail before repository call; direct SQL remains an independent control | `C509-APPLICATION-ALGORITHM-BEFORE-REPOSITORY` |
| `C5M24` | remove only the required rotation event append; canonical mid-transaction exception separately proves rollback | `C513-REQUIRED-ROTATION-EVENT` |
| `C5M28` | isolate C3 current-key State while revision, fingerprint, validity and package binding remain equal | `C516-C3-STATE-COMPARATOR` |
| `C5M32` | write target RecipientClientApplicationId into authenticated ManagerPrincipalId | `C519-AUDIT-AUTHENTICATED-ACTOR` |
| `C5M35` | owner/deployer direct UPDATE reaches the event trigger; remove only its UPDATE rejection branch | `C519-AUDIT-APPEND-ONLY-UPDATE` |
| `C5M40` | scratch-revoke exact manager EXECUTE, then remove only the corresponding readiness comparator | `C524-MANDATORY-MANAGER-EXECUTE` |

Each row retains neighboring controls. A GREEN, wrong-reason RED or unnamed
RED remains a HARD STOP. The correction adds no path, proof or mutation.

### 23.4 Dispatch v0.3 successor

| Artifact | SHA-256 | State |
| --- | --- | --- |
| `tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md` | `E862C44FD1FFB1A49372B4D42504785C62588A466A5D4ACCDA96B6C0FE8CF8CC` | `ROUND_2_SUCCESSOR_REVIEW_PENDING` |

The authoritative census remains:

```text
permanent paths        47
new C5 tables           5
existing columns        1 additive key-reason column
supporting indexes      6
SQL functions           9 (8 callable + 1 event guard)
capability / LOGIN       1 / 1
proofs                  C501-C530 = 30
mutations               C5M01-C5M48 = 48
```

No product/trust decision, durable model, codec or vector changed from v0.2.
No path 48, proof 31 or mutation 49 is required.

### 23.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1:             REVIEWED / CHANGES_REQUIRED
C5 Dispatch v0.2:             REVIEWED / SPLIT VERDICT / CHANGES_REQUIRED
C5 Dispatch v0.3:             ROUND_2_SUCCESSOR_REVIEW_PENDING
v0.1 review chain:            COMPLETE 2 OF 2
v0.2 successor review chain:  COMPLETE 2 OF 2
v0.3 successor review chain:  PENDING 0 OF 2
C4 activation debt:           ASSIGNED_OPEN
C4 persisted-principal claim: ASSIGNED_OPEN
C5 C2 clock nonclaim:         OPEN / PREDECESSOR_OWNED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 24. Dispatch v0.3 Round-2 reviews and v0.4 successor — appended 2026-08-23

This section is append-only. The ledger ending at §23, SHA-256
`57805C99020EA473DA5FAFC19556AD70998A9BD7778C4CD1029F93FE9658358D`,
is its exact byte prefix. The ratified Round-1 v0.1 ledger remains the root
append-only anchor.

### 24.1 Exact Round-2 v0.3 review artifacts

| Reviewer artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_DISPATCH_V0_3_ROUND2_SUCCESSOR_REVIEW.md` | `A3CA670DD35BF8CC911390D4C8231CC9ECA12B0008D5EE2B67F9FEA116D890D4` | `PASS` |
| `reviews/GPT_C5_DISPATCH_V0_3_ROUND2_SUCCESSOR_REVIEW.md` | `78DA63FA4C1197E2B53F60DF8D529E202E9F650270A2B974BEB15280EEB9A2FA` | `ROUND_2_CHANGES_REQUIRED` |

Both artifacts are verbatim Homeowner-forwarded reviews. The CC review found no
new issue. The GPT review confirmed the eight v0.3 mutation corrections and the
ledger fix, then identified the two residual findings below. Neither artifact
is a PASS review of v0.4.

### 24.2 Finding register and reconciliation

| ID | Reviewer | Severity | Finding | v0.4 disposition |
| --- | --- | --- | --- | --- |
| `C5-R2-V03-GPT-F01` | GPT | HIGH | a policy provider receiving only ClientApplicationId cannot distinguish a canonical C5 credential from an unrelated credential under the same client, so C5 enrollment can enable or disable unrelated authentication | `CLOSED_CANDIDATE`: §2.2 routes after one resolved-credential lookup; exact recipient/admin credentials use persisted C5 authority with zero LocalDev calls, every unrelated same-client credential uses the unchanged global policy path, and C504/C505 prove both Active and Disabled behavior |
| `C5-R2-V03-GPT-F02` | GPT | HIGH | C5M04/M16/M23/M31 remain masked by SQL nonempty validation, historical-fingerprint uniqueness or the partial one-Active index | `CLOSED_CANDIDATE`: all four owners and mutation rows are replaced count-neutrally with layer-isolated or committable negative states in §24.3 |

### 24.3 Residual mutation corrections

| Mutation | Corrected fixture/edit | Named bite |
| --- | --- | --- |
| `C5M04` | remove only the application empty-principal guard; repository-call-zero becomes nonzero while direct SQL remains an independent defense | `C503-NONEMPTY-PRINCIPAL-BEFORE-REPOSITORY` |
| `C5M16` | remove only RecipientClientApplicationId from the historical-fingerprint lookup; a distinct recipient with the same fingerprint is incorrectly rejected | `C510-HISTORICAL-FINGERPRINT-RECIPIENT-SCOPED` |
| `C5M23` | append one CHECK-valid K2 `Revoked` update after insert, allowing K1 Revoked/K2 Revoked to commit without colliding with the one-Active index | `C513-HARD-CUTOVER` |
| `C5M31` | assign a different RecipientKeyId to the otherwise-valid post-revoke recovery row | `C518-RECOVERY-SAME-KEY-FAMILY` |

Each mutation is one source edit over a real construct. Neighboring controls
remain active. A DB rejection, wrong-reason RED, unnamed RED or GREEN does not
close the mutation.

### 24.4 Dispatch v0.4 successor

| Artifact | SHA-256 | State |
| --- | --- | --- |
| `tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md` | `25455AE3AF437FFE5A4C3F76EB7856B676F7FFDED7DBBEAD7B4C1DCE8498EBA4` | `ROUND_2_SUCCESSOR_REVIEW_PENDING` |

The census remains 47 paths / 5 tables / 1 existing column / 6 indexes /
9 functions / 1 capability / 1 LOGIN / 30 proofs / 48 mutations. No durable
model, product/trust decision, codec or vector changed from v0.3.

### 24.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1:             REVIEWED / CHANGES_REQUIRED
C5 Dispatch v0.2:             REVIEWED / SPLIT VERDICT / CHANGES_REQUIRED
C5 Dispatch v0.3:             REVIEWED / SPLIT VERDICT / CHANGES_REQUIRED
C5 Dispatch v0.4:             ROUND_2_SUCCESSOR_REVIEW_PENDING
v0.1 review chain:            COMPLETE 2 OF 2
v0.2 successor review chain:  COMPLETE 2 OF 2
v0.3 successor review chain:  COMPLETE 2 OF 2
v0.4 successor review chain:  PENDING 0 OF 2
C4 activation debt:           ASSIGNED_OPEN
C4 persisted-principal claim: ASSIGNED_OPEN
C5 C2 clock nonclaim:         OPEN / PREDECESSOR_OWNED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 25. Dispatch v0.4 Round-2 reviews and v0.5 successor — appended 2026-08-23

This section is append-only. The ledger ending at §24, SHA-256
`2B2C496B253F3D8017A2E0ADD65568788737C98C1354D7B3935009DEEA32184A`,
is its exact byte prefix.

### 25.1 Exact Round-2 v0.4 review artifacts

| Reviewer artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_DISPATCH_V0_4_ROUND2_REVIEW.md` | `84E77FEB491F2CCC86FEE147CF071C0700D31BD98A1AFACDE259AB7C62607F8E` | `HOLD — ONE SYSTEMIC FINDING` |
| `reviews/GPT_C5_DISPATCH_V0_4_ROUND2_REVIEW.md` | `0728CC865FE3D0E40DFAF1BC4E45ECCA292870339A76710BDB53BD4256680BF8` | `ROUND_2_CHANGES_REQUIRED` |

Both artifacts are verbatim Homeowner-forwarded reviews and are not v0.5 PASS
evidence.

### 25.2 Finding register and reconciliation

| ID | Reviewer | Severity | Finding | v0.5 disposition |
| --- | --- | --- | --- | --- |
| `C5-R2-CC-F02` | CC | systemic blocker | named proof observations grew from 53 to 64 while mutation-targeted names stayed 46; discriminator and control observations were mixed in one column | `CLOSED_CANDIDATE`: every §12 name is explicitly `D` or `C`; the 47 unique discriminator names equal the 47 unique §13 target names, the 19 controls have bounded reasons, overlap/unclassified are zero, and the no-orphan rule is a Task-0/STOP gate |
| `C5-R2-V04-GPT-F01` | GPT | HIGH | OperatorAdmin singleton scope-set routing contradicts §3 required-scope containment and sends a valid admin-superset credential to LocalDev fallback | `CLOSED_CANDIDATE`: classification is OperatorAdmin plus containment of the exact management scope; C505 includes an unrelated-scope positive and C5M09 restores singleton equality to RED at `C505-ADMIN-SUPERSET-ROUTING` |
| `C5-R2-V04-GPT-F02` | GPT | HIGH | C5M17 cannot isolate the partial index because the canonical recipient-identity lock serializes the two real enrollment calls first | `CLOSED_CANDIDATE`: C511 separates canonical real-function serialization (`CONTROL`) from a direct CHECK-valid scratch double-insert; C5M17 drops only the scratch partial index and REDs at `C511-ONE-ACTIVE-KEY` |

### 25.3 Proof taxonomy freeze

```text
proof owners                 30
mutation IDs                 48
unique discriminator names  47
unique mutation target names 47
control names                19
unclassified names           0
discriminator/control overlap 0
discriminator without target 0
target without discriminator 0
```

Controls remain mandatory canonical observations but are not represented as
mutation-closed. A later successor cannot add a named observation without an
immediate discriminator mapping or a bounded control classification.

### 25.4 Dispatch v0.5 successor

| Artifact | SHA-256 | State |
| --- | --- | --- |
| `tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md` | `484C96D2C2EA2EEF5CC2E448472BDAA945956F3499EA965570A07ADFCDBE55B1` | `ROUND_2_SUCCESSOR_REVIEW_PENDING` |

The authoritative census remains 47 paths / 5 tables / 1 existing column /
6 indexes / 9 functions / 1 capability / 1 LOGIN / 30 proofs / 48 mutations.
No durable model, product/trust decision, codec or vector changed from v0.4.

### 25.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.4:        REVIEWED / CHANGES_REQUIRED
C5 Dispatch v0.5:             ROUND_2_SUCCESSOR_REVIEW_PENDING
review chain through v0.4:    COMPLETE 8 OF 8
v0.5 successor review chain:  PENDING 0 OF 2
C4 activation debt:           ASSIGNED_OPEN
C4 persisted-principal claim: ASSIGNED_OPEN
C5 C2 clock nonclaim:         OPEN / PREDECESSOR_OWNED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 26. Dispatch v0.5 exhaustive hold and v0.6 Phase-A requirement register — appended 2026-08-23

This section is append-only. The ledger ending at §25, SHA-256
`CAA617855C49E61C8A1FFF7885B2A713C03B74DD3893E7D6D7773D17C5E66E97`,
is its exact byte prefix.

### 26.1 Hold and two-phase authority

| Artifact | SHA-256 | Disposition |
| --- | --- | --- |
| `reviews/GPT_C5_DISPATCH_V0_5_FULL_SWEEP_REVIEW.md` | `C5DEC95C17A842A67DC8AE5CED0D0F006E3AD66B9964CCE94C57761377DB9167` | `ROUND_2_CHANGES_REQUIRED — EXHAUSTIVE_FULL_SWEEP_HOLD`; F01-F14 registered |
| `authority/HOMEOWNER_C5_DISPATCH_V0_6_TWO_PHASES.md` | `722EE68FB6788AF0AD6C090EA3970AC2896B0414E0F6FDC1DADECB3D671B66F7` | Phase A register only; Phase B blocked pending two independent Phase-A reviews |

The Homeowner authority records CC's independent landed-code confirmations for
C5M08, colon-bearing RecipientKeyId, C5M25 and candidate-conflict exhaustion.
No separate verbatim CC v0.5 full-sweep artifact was supplied to the Builder;
the Builder has not reconstructed one. The Phase-A successor starts with both
new independent register reviews pending.

### 26.2 Mechanical Scope requirement census

The exact Scope v0.3 sections 5-18 were parsed by the carried syntax-driven
enumerator. It emits every unordered/ordered list item, table body row,
non-empty fenced-code line and prose sentence in source order. Headings, blanks,
fence markers and table headers/separators are structural. Only prose sentences
ending in `:` are context introductions; no modal-keyword filter is used.

```text
all emitted leaves                272
context-only introductions         16
ratified requirement leaves       256
register rows                     256
```

| Phase-A artifact | SHA-256 |
| --- | --- |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER.md` | `B572BACDC620176D5683F875C333DF9502A794812A06B36113585806E6F8F247` |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER.tsv` | `9A7BE86D6B506F8ADEBBF926CC9FBA5A4C194F39848A464F15C34395A66D7DE2` |
| `requirements/SCOPE_LEAF_CENSUS.tsv` | `4352A8BF4A57ABD03E42D99BDB2F7B208929EE228BD117A046F0E4090E6750E0` |
| `requirements/ENUMERATION_METADATA.json` | `AABCA37937544EF510134FDC9D8683E01BED8FF23BE7ADFCA2D00A389435A587` |
| `requirements/OBSERVATION_TO_REQUIREMENTS.tsv` | `816013B5B0EA16A2D69D1F226AAD646C3A03AE3806A66FB2891FEB573374BCEE` |
| `requirements/TASK0_COVERAGE_VERIFICATION.json` | `BD4774141FFABBB5EC7FC57107D540A77EE6DD64D40C788A115B74880D1250F7` |
| `tools/enumerate_scope_requirements.py` | `C7D52E8828FD0A4B90B28332DC21D834CFF639A5E1D33367F7E94F36C8083CD9` |
| `tools/build_requirement_register.py` | `FC0519B784DC84E17634BEEE3BCDAE29EE0328690B2053141C587CD887A60E5C` |

### 26.3 Task-0 two-way equality

```text
enumerated Scope requirement IDs == register requirement IDs       PASS 256/256
every requirement              -> >=1 named observation            PASS 256/256
every registered observation   -> >=1 ratified requirement         PASS 91/91
every Dispatch v0.5 observation-> >=1 ratified requirement         PASS 66/66
blank/untyped names                                                PASS 0
```

The register contains all 47 v0.5 discriminator names, all 19 v0.5 control
names and 25 explicit Phase-B planned bounded controls. A planned control is a
gap marker and assignment only; it is not evidence that Dispatch v0.5 already
enforces the requirement. The 142 rows marked for Phase-B correction/review
trace the 14 finding groups without editing Dispatch bytes.

### 26.4 Pinned-census assessment

Every existing proof owner C501-C530 is used. The register adds no discriminator
and no mutation ID. The 25 planned names are assigned as canonical,
preservation, layered-defense or failure-injection controls within existing
owners.

```text
permanent paths  47   minimum expansion 0
proof owners     30   minimum expansion 0
mutations        48   minimum expansion 0
```

Current disposition:

`FITS_COUNT_NEUTRALLY_PENDING_PHASE_A_INDEPENDENT_REVIEW`

This is not a Phase-B design approval. If either reviewer establishes that a
planned control requires a new permanent path, proof owner or mutation, the
required successor is `STOP/RRI — CENSUS EXPANSION REQUIRED` naming the exact
requirement and minimum expansion.

### 26.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
v0.6 Phase A register:        REVIEW_PENDING 0 OF 2
v0.6 Phase B:                 NOT AUTHORIZED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 27. Phase-A Requirement Register R1 review and R2 semantic successor — appended 2026-08-24

This section is append-only. The Phase-A R1 ledger ending at §26, SHA-256
`4E1D3E4CAC221A29C33B155D1AD18B9BAB45D9092FD7AB6714CB29AFA745A08F`,
is its exact byte prefix.

### 27.1 R1 independent reviews

| Artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_PHASE_A_R1_REQUIREMENT_REGISTER_REVIEW.md` | `1AFB5BC5980CA5369DD89D168AB2ED3547605206068F561293B4E9DDD65AEA01` | `PHASE_A_CHANGES_REQUIRED — SEMANTIC REGISTER NOT YET CLOSED` |
| `reviews/GPT_C5_PHASE_A_R1_REQUIREMENT_REGISTER_REVIEW.md` | `B29EDB09CF3F21962035F819231270ADCD381DC847684B5B4B438E4E92764F01` | `HOLD — D/C NATURE AUDIT REQUIRED` |

Both reviews accepted the 256-requirement mechanical census and rejected the
R1 semantic/census conclusion. The first review found a deterministic
`C5-REQ-167` credential-vs-key domain error, a reverse-map false positive and
compound-leaf gaps across identity, key lifecycle, concurrency, readiness,
audit and exact ownership. The second review required a per-observation
one-dimensional D/C audit rather than labeling every new observation CONTROL
to preserve the 48-mutation census.

### 27.2 R2 semantic artifacts

| Artifact | SHA-256 |
| --- | --- |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER.md` | `7E7A3ED9661524C61C60BA60A74CA7035C23961D9ED079C99B8A084F6402A9DC` |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER.tsv` | `0418DB0CDF5749D0FC0F6BA611119FEDBAFEDF65CE92766E7908A5590A25F4E2` |
| `requirements/OBSERVATION_TO_REQUIREMENTS.tsv` | `D0607570D7FC280B3F3D237E60E86BB6467ED0DD8FEECAD6BDC3D539873BB82F` |
| `requirements/PLANNED_OBSERVATION_NATURE_AUDIT.tsv` | `084EA28D585F6D2AC8FA6CAA48FE636596961EE9F3527F5C48C29D5A13A9F593` |
| `requirements/SEMANTIC_RECONCILIATION_REPORT.md` | `5BD091746624EF278267B951C269B10471F49A33BA62CF404DED0FE59C607ED9` |
| `requirements/TASK0_COVERAGE_VERIFICATION.json` | `F16C6FFF906FCC5081FE5D1918473B9738C130A0AE33F2088C3CABFA121CF2AB` |
| `tools/build_requirement_register.py` | `47D4A311B239188769B9BFBD4CCE7349C54233DE0703BDC97B0D444F9282ADE2` |

R2 keeps Scope v0.3 and Dispatch v0.5 byte-identical. Its Task-0 checks are:

```text
requirements/register IDs                  PASS 256/256
requirements with named observation        PASS 256/256
registered observations with requirement   PASS 120/120
Dispatch v0.5 observations represented     PASS 66/66
proof owners                               PASS 30/30
```

The semantic register now contains an explicit conjunct assignment for every
row. `C5-REQ-167` is assigned to C508. Candidate-conflict exhaustion is
assigned to one-time credential generation/admission (`C5-REQ-018`), not to
lost-response replay.

### 27.3 D/C nature audit and A4 activation

The exact 25 R1 planned observations were audited without assuming a desired
census. Twenty-three remain bounded controls with a reason stating why no
separate one-dimensional mutation represents the observation. Two are true
discriminators:

| Proposed ID | Owner/bite | Exact proposed edit | State |
| --- | --- | --- | --- |
| `C5M49` | `C509-RSA-SIZE-RANGE` | remove only the RSA size-range comparator; keep algorithm literal and SPKI parsing active | `PROPOSED / NOT IN DISPATCH` |
| `C5M50` | `C511-ABSENT-IDENTITY-LINEARIZATION` | remove only absent-identity pre-clock lifecycle authority; keep tuple/one-active constraints active | `PROPOSED / NOT IN DISPATCH` |

Every existing C5M01–C5M48 remains bound to its existing discriminator. No ID
can be retargeted without silently dropping a ratified obligation. The minimum
honest successor census is therefore:

```text
paths       47 -> 47   (+0)
proofs      30 -> 30   (+0)
mutations   48 -> 50   (+2)
```

Current disposition:

`STOP/RRI — MUTATION CENSUS EXPANSION REQUIRED (48 -> 50)`

This records the Phase-A A4 gate. It does not add C5M49/C5M50 to the Dispatch
and does not authorize Phase B. Homeowner adjudication is required.

### 27.4 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
v0.6 Phase A register R1:     REVIEWED / CHANGES_REQUIRED
v0.6 Phase A register R2:     REVIEW_PENDING 0 OF 2
census gate:                  STOP/RRI — 48 -> 50 MUTATIONS REQUIRED
v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 28. Phase-A Register R2 reviews and R3 full semantic/census successor — appended 2026-08-24

This section is append-only. The R2 ledger ending at §27, SHA-256
`F5DD9C5A7A96E600AF199040FC0627175587910F659874A6E9A6DB81A5EA3379`,
is its exact byte prefix.

### 28.1 Conflicting R2 reviews

| Artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_PHASE_A_R2_REQUIREMENT_REGISTER_REVIEW.md` | `E2BFB91C3A1C899D0C4D679D2B1B4B9B6791FEB7527EEB48F6BC9731B24D24A3` | `PASS`; recommends 48→50 |
| `reviews/GPT_C5_PHASE_A_R2_REQUIREMENT_REGISTER_REVIEW.md` | `EB4FF2785DDCCDB13A6FA6391985E99B29F56DED052BB7ACD426F0095AB25F54` | `PHASE_A_R2_CHANGES_REQUIRED — DO NOT RATIFY 48→50 YET` |

The PASS review's embedded Homeowner packet is a recommendation, not executed
Homeowner authority. The CHANGES_REQUIRED review is accepted because it carries
reproducible counterexamples: wrong/partial semantic rows, 25/54 rather than
full nature audit, wrong absent-row owner and a one-value mutation dictionary
that overwrote C5M06 with C5M07.

### 28.2 R3 artifacts

| Artifact | SHA-256 |
| --- | --- |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER.md` | `AE7A89C5389759810AECCDCB66118C891F754395E6F6F4989C583A6BE8AFF78F` |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER.tsv` | `3720BCC8D6396B68612AF961AF63EB9FA64E3A452A87404E8D2BE5BE7DDBD273` |
| `requirements/OBSERVATION_TO_REQUIREMENTS.tsv` | `E9DB9CE74B85FFD056D8357871841E689603D210B4FE9F87AF5B344CEB56224A` |
| `requirements/PLANNED_OBSERVATION_NATURE_AUDIT.tsv` | `42F46E6CC144CD68D089AEE55D91DCB6D9370DACFD3BD32B2646C21BDE87050F` |
| `requirements/MUTATION_TO_REQUIREMENTS.tsv` | `E0AD71FD387505A2449165E239144E5E55782D5B55E6FAD6BFED78DFFA066801` |
| `requirements/SEMANTIC_RECONCILIATION_REPORT.md` | `09BDF8E3F7409774342314310E0FF8F3A3DA03045A28531CD0E4556519E97BCF` |
| `requirements/TASK0_COVERAGE_VERIFICATION.json` | `CF766BD3EBF807BD491768D2F783CB04AE193B6978E36117100BF5103F2CCDE9` |
| `tools/build_requirement_register.py` | `AFF4EF77618EE10F1C6D08D9C734F894115396A6A6592A34EE6E06946E21FE36` |

### 28.3 Task-0 and mutation allocation

```text
requirements/register IDs                    PASS 256/256
registered observations with requirement     PASS 142/142
Dispatch v0.5 observations represented       PASS 66/66
planned observation nature audit             PASS 76/76
existing C5M01-C5M48 represented             PASS 48/48
existing mutations with requirement          PASS 48/48
existing edit dimensions unique              PASS 48/48
```

The mutation mapping is now a multimap. C5M06 and C5M07 both remain allocated
to distinct missing-scope and added-sibling dimensions under the shared exact
scope-set bite. Neither is silently lost or treated as reusable.

### 28.4 R3 census candidate

The full audit classifies 50 planned observations as bounded controls and 26
as one-dimensional discriminators. Proposed C5M49-C5M74 are register entries
only; Dispatch v0.5 remains byte-identical.

```text
paths       47 -> 47   (+0)
proofs      30 -> 30   (+0)
mutations   48 -> 74   (+26)
```

Current disposition:

`STOP/RRI — MUTATION CENSUS EXPANSION REQUIRED (48 -> 74)`

The earlier 48→50 proposal is superseded as unproven. Independent R3 review
must verify every proposed edit and the exact minimum before Homeowner
ratification.

### 28.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
v0.6 Phase A R1:              REVIEWED / CHANGES_REQUIRED
v0.6 Phase A R2:              CONFLICTING REVIEWS / CHANGES_REQUIRED PREVAILS
v0.6 Phase A R3:              REVIEW_PENDING 0 OF 2
census candidate:             STOP/RRI — 48 -> 74 MUTATIONS
v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 29. Phase-A Register R3 reviews and R4 multi-authority successor — appended 2026-08-24

This section is append-only. The R3 ledger ending at §28, SHA-256
`6C9EAD1F40535F0AE6D837E5E18AB893ED760960E233328C930B2A6D5600FD63`,
is its exact byte prefix.

### 29.1 Conflicting R3 reviews

| Artifact | SHA-256 | Verdict |
| --- | --- | --- |
| `reviews/CC_C5_PHASE_A_R3_REQUIREMENT_REGISTER_REVIEW.md` | `AFB9AC2ECEE2CF20315F1F00D3DC02E42A6587EE0325AE82EA2257AB28936A1C` | `PASS`; recommends 48→74, while stating the full CONTROL/DISCRIMINATOR branch was sample-checked rather than exhaustively re-derived |
| `reviews/GPT_C5_PHASE_A_R3_REQUIREMENT_REGISTER_REVIEW.md` | `219DA1254D5FBA9AAAA9B14B9BDF979510F6D60709E6DAC8B77EE616F33C4277` | `PHASE_A_R3_CHANGES_REQUIRED — DO NOT RATIFY 48→74` |

The inline PASS packet is not Homeowner ratification. The CHANGES_REQUIRED
review prevails because it supplies concrete semantic counterexamples:

- the `74` result was still `48 + hard-coded planned D count`;
- v0.5 edit-string uniqueness did not model known C5M25 and C5M33/C5M48
  semantic defects;
- F11/F13 authority requirements were forced into nearby Scope leaves;
- C5M60 retained alternative edits, C5M66 had no exact comparator, and
  C5M67/C5M68 were compound;
- C5M74 under-covered the four conjuncts of Scope requirement 126;
- the 19 v0.5 controls had not joined the planned-observation nature audit.

The R3 48→74 census candidate is therefore rejected as unproven. Phase B stays
blocked.

### 29.2 R4 exact artifacts

| Artifact | SHA-256 |
| --- | --- |
| `requirements/C5_REQUIREMENT_COVERAGE_REGISTER_R4.md` | `5D4B7E135FE6391BD3A253F2A2562EEF6BE8823D396560F8A20CC494DF46282B` |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R4.tsv` | `904DC25D612CB8410B2C2C634C28C4367ED6937F40727F57E8D769C3B54A990C` |
| `requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER.tsv` | `6F609A0A424BEB2EED0B77CFC45B633E1BE5BC42D23CA8008539D0B0521DFB33` |
| `requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS.tsv` | `85FE8334760E040D304F0CBC8D8C3DE874C7F9C222EA4766CB198483DD1D603C` |
| `requirements/ALL_OBSERVATION_NATURE_AUDIT.tsv` | `4612650DF9E4B71D6C67A68DC7E834A8D7BD6FE7A9FE87DD694EF15FF3B2A864` |
| `requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION.tsv` | `C05D2716B3F22C1094A5795475E094FAE9AA5E76443298667D58D85DD8BDDB79` |
| `requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS.tsv` | `8CA3E55CFD2ED988E5C59CB8DB591BB24E59733E7A9C572C970E968705B48586` |
| `requirements/SEMANTIC_RECONCILIATION_R4.md` | `8845CF8ED5AF7909E07B17D1696AC20BB055981871E47F68088058209BA8FA51` |
| `requirements/TASK0_R4_COVERAGE_VERIFICATION.json` | `1B5A905BC41BBCBF241EE899647A4E1EC3E85BD2DF6949D9EDA908E8E58B3A8E` |
| `tools/build_requirement_register_r4.py` | `EAFC9B8F300B869F84D8F21E7E984B9C4DB615A2A374C52CB47EA654C82C65B1` |

### 29.3 Two-source denominator and full nature audit

R4 keeps the mechanically enumerated 256 Scope requirements and adds 22 exact
Homeowner Phase-B F02-F14 authority requirements. Task-0 checks two-way equality
over the 278-row union instead of creating false Scope anchors for authority-only
requirements.

```text
Scope requirements                         256/256
Phase-B authority requirements              22/22
union requirements                         278/278
registered observations                    151
full observation nature audit              151/151
DISCRIMINATOR / CONTROL                     76 / 75
proof owners                               30/30
```

The five-candidate conflict outcome is now authority requirement
`C5-PB-REQ-021`, not Scope `C5-REQ-018`. The Disabled/Invalid topology partition
is authority requirement `C5-PB-REQ-019`, not Scope leaves 132/133.

### 29.4 Corrected mutation allocation

R4 models every existing C5M01-C5M48 using its Phase-B corrected future
dimension. In particular C5M08, C5M18, C5M25, C5M27, C5M33 and C5M48 are pinned
count-neutrally before any census calculation. Semantic dimension keys, not edit
string inequality, are the allocation authority.

R4 then reconciles the R3 counterexamples:

- splits policy, credential and key audit-lineage revision comparators;
- splits exact policy-name and readiness-item ownership;
- replaces the nonexistent recipient/client equality with a bounded
  single-authority durable readback;
- narrows K2 mutation coverage to post-lock validity and keeps the remaining K2
  matrix as controls owned by their existing discriminators;
- makes common authority/validity/revision revalidation a conjunct matrix
  control rather than one three-edit mutation;
- reclassifies recipient BusinessConsumer readiness and exact connected manager
  LOGIN identity as discriminators;
- assigns Scope requirement 126 to separate idempotency/revision/event/timeline
  controls and leaves identifier substitution to requirement 127.

The exact candidate allocation is:

```text
paths       47 -> 47   (+0)
proofs      30 -> 30   (+0)
mutations   48 -> 77   (+29)
```

All 77 rows have distinct semantic dimension keys, a named bite, at least one
Scope-or-Homeowner requirement and `StillNeeded=YES`. This is a Phase-A census
candidate, not a ratification.

Current disposition:

`STOP/RRI — EXACT MUTATION CENSUS 48 -> 77 REQUIRES HOMEOWNER RATIFICATION`

### 29.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
v0.6 Phase A R3:              CONFLICTING REVIEWS / CHANGES_REQUIRED PREVAILS
v0.6 Phase A R4:              REVIEW_PENDING 0 OF 2
census candidate:             STOP/RRI — 48 -> 77 MUTATIONS
v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 30. Phase-A requirement register R5 — R4 semantic reconciliation

Date: 2026-08-24

R5 is a Phase-A-only successor. Dispatch v0.5 remains byte-identical at
`484C96D2C2EA2EEF5CC2E448472BDAA945956F3499EA965570A07ADFCDBE55B1`.
No Phase-B correction or executable edit is authorized.

### 30.1 R4 review disposition

| Reviewer | Exact artifact | SHA-256 | Disposition |
| --- | --- | --- | --- |
| CC | `reviews/CC_C5_PHASE_A_R4_REQUIREMENT_REGISTER_REVIEW.md` | `E8ED6FF4278C1783308CD3C9722D352DD50B9B9F920CE99822AF9910703EFAE2` | PASS recommendation; recommends ratification at 77 |
| GPT | `reviews/GPT_C5_PHASE_A_R4_REQUIREMENT_REGISTER_REVIEW.md` | `73A60FD2281518F0EBCC426A6D2F0CB01FBD51B708DFDEFABD823A3C440042C6` | CHANGES_REQUIRED; do not ratify 77 |

The reviews conflict. The CC recommendation is not Homeowner ratification. The
GPT review supplies concrete semantic counterexamples, so CHANGES_REQUIRED
prevails and drives this R5 successor.

### 30.2 R5 exact artifacts

| Artifact | SHA-256 |
| --- | --- |
| `requirements/C5_REQUIREMENT_COVERAGE_REGISTER_R5.md` | `118CD5F8649E77B7D529B8D59DBF41252D5D788FBB36BFF206634EFA07E278FB` |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R5.tsv` | `9589CE523A76A3760D9A24EDFF8C8A8FC92D398E8E45DEB6A5E7FE809072EFA1` |
| `requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R5.tsv` | `6F609A0A424BEB2EED0B77CFC45B633E1BE5BC42D23CA8008539D0B0521DFB33` |
| `requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R5.tsv` | `BEBFDFD557FA3110CD4879B15840C70621756C6D7D4EEB17DB27BDB724124C3A` |
| `requirements/ALL_OBSERVATION_NATURE_AUDIT_R5.tsv` | `D2CEBEB47FF574BEC987140466E252DB642B1671E101E49BBF9BFB511DB75A5B` |
| `requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R5.tsv` | `C05D2716B3F22C1094A5795475E094FAE9AA5E76443298667D58D85DD8BDDB79` |
| `requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R5.tsv` | `BCFBE8F39283558DE91DA766910E7C8221F5DF1A0636DE9D8063C4D3B258B4D7` |
| `requirements/SEMANTIC_RECONCILIATION_R5.md` | `8CC553A4842E81323F489D3C82B01BDCED0B4B3AAAAE8B75944FE122C14FBA82` |
| `requirements/TASK0_R5_COVERAGE_VERIFICATION.json` | `C13B030D9E1355B85A445E4A9513846A47FD1AA39C3EA7D7621285B4503DFED9` |
| `tools/build_requirement_register_r5.py` | `7A2E96B6389B47727A8E89E48D3E2CBA1B1DEBB80728431FC1992192FE0B585A` |

### 30.3 R4 finding closure

R5 adds two bounded CONTROL observations and three independent discriminator
dimensions:

```text
CONTROL  C509-REGISTERED-TIME-AUTHORITY
CONTROL  C519-DELIVERY-COUNT-EVIDENCE-BINDING

C5M78   C509-C5-VALIDITY-ORDER
C5M79   C509-C5-NOT-EXPIRED-ADMISSION
C5M80   C524-EXACT-GRANT-OWNERSHIP
```

The two validity mutations cannot be collapsed: an inverted future interval
isolates validity ordering, while a well-ordered expired interval isolates the
fresh-clock admission comparator. Exact grant ownership is a third independent
finite-set comparator.

R5 also corrects mappings for requirements 033, 035, 079, 117, 138, 171 and
238. C5M73 is pinned to the C520 source/call-shape assertion so it REDs before
the neighboring C524 runtime ACL defense can mask it.

### 30.4 Complete R5 denominator and candidate census

```text
Scope requirements                         256/256
Phase-B authority requirements              22/22
union requirements                         278/278
registered observations                    155
full observation nature audit              155/155
DISCRIMINATOR / CONTROL                     79 / 76
candidate mutations                    C5M01-C5M80
unique semantic dimensions                  80/80
proof owners                                30/30
```

The resulting candidate census is:

```text
paths       47 -> 47   (+0)
proofs      30 -> 30   (+0)
mutations   48 -> 80   (+32)
```

Current disposition:

`STOP/RRI — EXACT MUTATION CENSUS 48 -> 80 REQUIRES HOMEOWNER RATIFICATION`

### 30.5 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
v0.6 Phase A R4:              CONFLICTING REVIEWS / CHANGES_REQUIRED PREVAILS
v0.6 Phase A R5:              REVIEW_PENDING 0 OF 2
census candidate:             STOP/RRI — 48 -> 80 MUTATIONS
v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 31. Phase-A requirement register R6 — narrow exact-census reconciliation

Date: 2026-08-24

R6 is authorized by exact artifact
`authority/HOMEOWNER_C5_PHASE_A_R6_NARROW_RECONCILIATION.md`, SHA-256
`1807A6681B0AB6FE8288AE465EA4F3EFAB6D4210BD69BE4B68C60050A86B5F24`.
Dispatch v0.5 remains byte-identical at
`484C96D2C2EA2EEF5CC2E448472BDAA945956F3499EA965570A07ADFCDBE55B1`.

### 31.1 R5 review disposition

| Reviewer | Exact artifact | SHA-256 | Disposition |
| --- | --- | --- | --- |
| CC | `reviews/CC_C5_PHASE_A_R5_REQUIREMENT_REGISTER_REVIEW.md` | `79B4EBA0C2D3D83ADC71285DF10B23900A96BBEBE6FC4ED5A818B397E2A97FCA` | technical PASS; requests a bounded stopping rule |
| GPT | `reviews/GPT_C5_PHASE_A_R5_REQUIREMENT_REGISTER_REVIEW.md` | `9D7F628A58950BAB5F53B3F31DF1FA7C4658DDE9CE21B6E561DE72E1DE09122F` | CHANGES_REQUIRED; do not ratify 80 |

The reviews conflict. The counterexample-bearing CHANGES_REQUIRED disposition
prevails. The Homeowner selected a narrow R6 and explicitly reclassified M78.

### 31.2 R6 exact artifacts

| Artifact | SHA-256 |
| --- | --- |
| `requirements/C5_REQUIREMENT_COVERAGE_REGISTER_R6.md` | `F96F31120B31EB435C4C340C803D71BED26E58F5F3EDD722F6EA1AFB2BD961EA` |
| `requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R6.tsv` | `906B10A148966FE821722B2D6CE95E1D8248083CF3489239061798757095869D` |
| `requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R6.tsv` | `28919B03C88A65E9193B5D22FA2B95A72855D5A4860B5184D2DF2538F579FFD3` |
| `requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R6.tsv` | `EB93827143581748152AB30174CC1BB9852C3B3FD96FD2BF2CB4221DB9134191` |
| `requirements/ALL_OBSERVATION_NATURE_AUDIT_R6.tsv` | `993F37AC6F0C1E244699200E8A534B02586EF00F5CE5726136E78011920D7788` |
| `requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R6.tsv` | `CFE8AE002C5FB31136C592CA19080361D57E9C40F6903765CE514C825970FF9C` |
| `requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R6.tsv` | `74F486FEA9EA5661DF4783C793CFC4C15DE81CCC1BB78CF7DFAB9C5981AF894F` |
| `requirements/MUTATION_RECLASSIFICATIONS_R6.tsv` | `7A5ED656BE7A5CED1782647432E9329DBD9A04E67AF83021C73948FEE8D8B7EF` |
| `requirements/SEMANTIC_RECONCILIATION_R6.md` | `55559106831514C48F8415733186BECDD4DD5E20B57212CF024A22B95FE76A64` |
| `requirements/TASK0_R6_COVERAGE_VERIFICATION.json` | `75D20DCE538128390589A05AE15AAED14ABD3635549BE1B305EFD2580CA20B83` |
| `tools/build_requirement_register_r6.py` | `81D57F6B91F6ACB02BB3D8C8E3D3FB286190AE4CE9C043A76498ED01821E0CF2` |

### 31.3 R5 counterexample closure

R6 performs the authorized count-neutral mapping corrections for Scope
requirements 148, 149, 150, 239 and 253. Task-0 now rejects any row where an
exact observation token in `SemanticConjunctAssignment` is absent from that
row's `NamedObservation`.

Mutation isolation is pinned as follows:

```text
C5M11  canonical PrincipalId argument source; source-shape RED before SQL/FK
C5M49  RSA bit-size public rejection; repository calls = 0
C5M59  omit only predecessor api_keys.CredentialStatus Active -> Revoked
C5M79  omit only post-lock SQL ValidUntilUtc > fresh_now_utc
C5M80  relax only exact two-way grant/ACL owned-set equality
```

### 31.4 M78 reclassification and provenance

The Homeowner disposition is exact:

```text
C5M78
  RECLASSIFIED_TO_CONTROL / NOT_ALLOCATED_IN_SUCCESSOR_CENSUS
```

The ratified validity-order invariant does not pin an application-owned
comparator. The landed reachable DB/SQL CHECK owns the same
`ValidFromUtc < ValidUntilUtc` invariant, so the redundant application
comparator is not a minimum-required mutation dimension. M79 and M80 retain
their historical IDs; no successor renumbering hides the reclassification.

### 31.5 Task-0 and candidate census

```text
Scope requirements                         256/256
Phase-B authority requirements              22/22
union requirements                         278/278
semantic assignment subset gate              PASS / 0 mismatch
registered observations                    155
full observation nature audit              155/155
DISCRIMINATOR / CONTROL                     78 / 77
allocated mutation IDs                       79
unique semantic dimensions                  79/79
reclassified historical IDs                   1 / C5M78
proof owners                                30/30
```

The candidate census is:

```text
paths       47 -> 47   (+0)
proofs      30 -> 30   (+0)
mutations   48 -> 79   (+31)
```

Current disposition:

`STOP/RRI — EXACT MUTATION CENSUS 48 -> 79 REQUIRES HOMEOWNER RATIFICATION`

### 31.6 Current lifecycle successor

```text
C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
v0.6 Phase A R5:              CONFLICTING REVIEWS / CHANGES_REQUIRED PREVAILS
v0.6 Phase A R6:              REVIEW_PENDING 0 OF 2
census candidate:             STOP/RRI — 48 -> 79 MUTATIONS
v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
Round 3 authority:            NONE
Implementation authority:    NONE
Stage / commit / push:        NONE
```

## 32. Phase-A R7 narrow reconciliation — 2026-08-24

### 32.1 Authority and conflicting R6 reviews

| Artifact | Delivery form | SHA-256 | Disposition |
| --- | --- | --- | --- |
| authority/HOMEOWNER_C5_PHASE_A_R7_NARROW_RECONCILIATION.md | Homeowner authority | 1D9FEA9F69EA927E2C359BEA62D9B5A6EBA69F6532A9B1335899CC98C5555107 | R7 docs/register/bundle only |
| reviews/CC_C5_PHASE_A_R6_REQUIREMENT_REGISTER_REVIEW.md | delivered inline; forwarded text stored verbatim | 27E7DA46C865CF6C5BE4B4FE0A1C793AD92CBD8396537D76DA3C5B14FF14FA0F | PASS recommendation for 79 |
| reviews/GPT_C5_PHASE_A_R6_REQUIREMENT_REGISTER_REVIEW.md | supplied attachment bytes stored unchanged | 0C30F6E6958BD1BD278D267BA6BD824C5338E4C77C67C6C27E508FD63F17E6F3 | CHANGES_REQUIRED; F01-F03 |

The reviews conflict. The Homeowner accepted GPT F01-F03, declined to ratify
79 and kept Phase B blocked.

### 32.2 R7 exact artifacts

| Artifact | SHA-256 |
| --- | --- |
| requirements/C5_REQUIREMENT_COVERAGE_REGISTER_R7.md | 198AE9EBBA3357AC12E1EC2B996D18224F25D2B0E0A32A7BC9FBD95F0FC6A460 |
| requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R7.tsv | 47D39359E372410971B99641758F80823603D8E7AF75D99733E979D83862F6DB |
| requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R7.tsv | 28919B03C88A65E9193B5D22FA2B95A72855D5A4860B5184D2DF2538F579FFD3 |
| requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R7.tsv | 21778B2B36FDDA66E61D52AD1BBF232D2FF6642A4B90D1A3A5B6B9159F0CE390 |
| requirements/ALL_OBSERVATION_NATURE_AUDIT_R7.tsv | 7E408A9B698C9ABEF524215A4D3152534CD09E96E064806D23F1CB6A6A3831B0 |
| requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R7.tsv | E5BF10EC6D79B86697E0CFD456F9F857BFC96A0E35677CFD07E2A9ABA64D2DC4 |
| requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R7.tsv | CC5554623A07D7352A96D722DA342990F590F628A9E082D5727B49D1735D5B48 |
| requirements/MUTATION_DEMOTIONS_R7.tsv | 4478F7F846C4FB50FD6A6B2BF909527E795E11E63616F25FB0A852F69D0D714C |
| requirements/SEMANTIC_RECONCILIATION_R7.md | A648F38A2781A861F3AA747C5336449E5D79C8AF244278FA6C54E324E4E341F3 |
| requirements/DEMOTION_LOAD_BEARING_CONSEQUENCE_R7.md | F459A35E385CA6F87D8A2FC0E6ACC0B63CB67596696A0349E8F609935B4DEBA8 |
| requirements/TASK0_R7_COVERAGE_VERIFICATION.json | 03595507287076DD80796BA28964EB67844BFA6B18FFE3969EFCFF3F2183E6BB |
| tools/build_requirement_register_r7.py | F74E7E59A2D0DD871EE4E4D7FC2EC3B253A3A0117DC276FBA031573B249A48CA |

### 32.3 Demotion register and load-bearing predecessor constraint

The complete demotion register contains exactly two rows:

| Historical mutation | Demoted bite | Exact landed constraint | Protected requirements |
| --- | --- | --- | --- |
| C5M65 | C509-KEY-REVISION-POSITIVE | ck_raw_export_recipient_key_registration_shape | C5-REQ-061 |
| C5M78 | C509-C5-VALIDITY-ORDER | ck_raw_export_recipient_key_registration_shape | C5-REQ-058, C5-REQ-071 |

For C5 allocation, the cited landed constraint is now the sole enforcement of
Revision > 0 and ValidFromUtc < ValidUntilUtc. This dependency is bound to the
existing C529 predecessor-regression obligation. C5 must not drop, weaken or
replace the exact constraint while relying on either demotion. Such a change
invalidates the denominator and is STOP/RRI.

### 32.4 Neighboring-isolation correction

C5M10 returns a fixed replay canary while generator calls remain zero.
C5M54 invokes the generator before replay resolution while PresentedKey remains
null. C5M40 removes mandatory manager EXECUTE with no unexpected grant present.
C5M80 admits exactly one unexpected sibling grant while mandatory manager
EXECUTE remains present.

The fixture dimensions are mutually exclusive. Each mutation can RED only its
named observation while its neighbor remains green.

### 32.5 Task-0 and candidate census

    Scope requirements                         256/256
    Phase-B authority requirements              22/22
    union requirements                         278/278
    semantic assignment subset gate              PASS / 0 mismatch
    registered observations                    155
    full observation nature audit              155/155
    DISCRIMINATOR / CONTROL                     77 / 78
    allocated mutation IDs                       78
    unique semantic dimensions                  78/78
    reclassified historical IDs                   2 / C5M65,C5M78
    proof owners                                30/30

The candidate census is:

    paths       47 -> 47   (+0)
    proofs      30 -> 30   (+0)
    mutations   48 -> 78   (+30)

Current disposition:

STOP/RRI — EXACT MUTATION CENSUS 48 -> 78 REQUIRES TWO CLEAN R7 REVIEWS AND HOMEOWNER RATIFICATION

### 32.6 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
    v0.6 Phase A R6:              CONFLICTING REVIEWS / CHANGES_REQUIRED PREVAILS
    v0.6 Phase A R7:              REVIEW_PENDING 0 OF 2
    census candidate:             STOP/RRI — 48 -> 78 MUTATIONS
    v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Stage / commit / push:        NONE

## 33. Phase-A R8 authority-denominator reconciliation — 2026-08-24

### 33.1 Authority, R7 reviews and finding disposition

| Artifact | Delivery form | SHA-256 | Disposition |
| --- | --- | --- | --- |
| authority/HOMEOWNER_C5_PHASE_A_R7_NARROW_RECONCILIATION.md | exact carried Homeowner authority | 1D9FEA9F69EA927E2C359BEA62D9B5A6EBA69F6532A9B1335899CC98C5555107 | authoritative R7 clauses, including 5b/5c |
| authority/HOMEOWNER_C5_PHASE_A_R8_INLINE_DIRECTION.md | one-line Homeowner direction plus explicitly labelled bounded binding | 80676EA4317EF45F2B01A2E83C2062C22B354F4BF5C7FFBDA1730E1C14251C02 | apply narrow R8 reconciliation |
| reviews/CC_C5_PHASE_A_R7_REQUIREMENT_REGISTER_REVIEW.md | delivered inline; forwarded text stored verbatim | BA9537035267F414FDA877805034B3DFDBB644391ED1BE2CAD1DA236278E74E4 | PASS |
| reviews/GPT_C5_PHASE_A_R7_REQUIREMENT_REGISTER_REVIEW.md | supplied attachment bytes stored unchanged | A4AC4E987CBCC1E6400CED66E693E975AD3207F8C1FF16D6903A143F2BFCE16F | technical 47/30/78 PASS candidate; F01-F03 raised |

GPT R7 F01 is rejected by exact authority text: R7 clause 6 states that the GPT
R6 review was supplied as attachment bytes and must be stored unchanged. GPT R7
F02 is rejected by the same exact artifact: clauses 5b and 5c and the
named-constraint demotion prohibition are present in the Homeowner authority.
The R7 authority artifact remains byte-identical; history is not rewritten.

GPT R7 F03 is accepted. Clause 5c creates a future predecessor-regression
obligation and is now registered as `C5-R7-REQ-001` with the bounded named
control `C529-DEMOTION-CONSTRAINT-PRESERVATION`.

### 33.2 R8 exact artifacts

| Artifact | SHA-256 |
| --- | --- |
| requirements/C5_REQUIREMENT_COVERAGE_REGISTER_R8.md | 869DCA4CA645E3FF097CC3C165C1C69DD6121AB6BC8622715323C1406524C1FC |
| requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R8.tsv | 47D39359E372410971B99641758F80823603D8E7AF75D99733E979D83862F6DB |
| requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R8.tsv | 8DBB62180859C91A3A6A28ADDA47DD8322DE394AB5FC9506414DB40054294F61 |
| requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv | C376570FE798EB1971FBBFF8964FE05654AA2EC53976EB830E681DAEA000EDD7 |
| requirements/ALL_OBSERVATION_NATURE_AUDIT_R8.tsv | 6AC12C59B26E207F06B099A89CCA628298E5FA6F6710648FD850CD298FEEAC14 |
| requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R8.tsv | E5BF10EC6D79B86697E0CFD456F9F857BFC96A0E35677CFD07E2A9ABA64D2DC4 |
| requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv | 350EAD7DB8382EBCF6551CDD49D688A64D2FF32F7753FAF06251B151444B9490 |
| requirements/MUTATION_DEMOTIONS_R8.tsv | 4478F7F846C4FB50FD6A6B2BF909527E795E11E63616F25FB0A852F69D0D714C |
| requirements/AUTHORITY_CLAUSE_CLASSIFICATION_R8.tsv | AFAC07C21C707BBD90F297D359DC4C2D59F76079C18FF61833D3F4E4A8595031 |
| requirements/SEMANTIC_RECONCILIATION_R8.md | 01D0C4232E8078F0CE9F9976F519C638FDFBFFEAE2706376B507D982A88C9BD7 |
| requirements/DEMOTION_LOAD_BEARING_CONSEQUENCE_R8.md | F184EA1988089BA9E85F3F96FAE0A3802D4144871F8915A24B5A919DE857A331 |
| requirements/TASK0_R8_COVERAGE_VERIFICATION.json | 9AF6D0E1AAC3831E2E484E160FAA92293F21DAF8952D42E9D1F2DD1C106A9B8B |
| tools/build_requirement_register_r8.py | 17C402D25C83446A095AECF227366C70E38CB80EF510FF63F53EB0929166FA7E |

### 33.3 Finite successor-authority classification

The R8 authority-clause table classifies all 19 explicit R6/R7 clauses or
blocks: 10 are `SEMANTIC_REGISTERED` and 9 are `PROCESS_ONLY`. Every semantic
row maps to an existing union requirement; every process row carries a bounded
reason. R7 clause 5c is registered exactly once. A later successor authority
cannot add semantic prose outside the denominator without failing this gate.

### 33.4 Task-0 and candidate census

    Scope requirements                         256/256
    original Phase-B authority requirements     22/22
    R7 clause 5c authority requirement            1/1
    union requirements                         279/279
    successor authority clauses                  19/19 classified
    semantic / process-only clauses               10 / 9
    semantic assignment subset gate              PASS / 0 mismatch
    registered observations                    156
    full observation nature audit              156/156
    DISCRIMINATOR / CONTROL                     77 / 79
    allocated mutation IDs                       78
    unique semantic dimensions                  78/78
    reclassified historical IDs                   2 / C5M65,C5M78
    proof owners                                30/30

The candidate census remains:

    paths       47 -> 47   (+0)
    proofs      30 -> 30   (+0)
    mutations   48 -> 78   (+30)

Current disposition:

STOP/RRI — EXACT MUTATION CENSUS 48 -> 78 REQUIRES TWO CLEAN R8 REVIEWS AND HOMEOWNER RATIFICATION

### 33.5 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    C5 Dispatch v0.1-v0.5:        REVIEWED / CHANGES_REQUIRED
    v0.6 Phase A R7:              CONFLICTING REVIEWS / F03 ACCEPTED
    v0.6 Phase A R8:              REVIEW_PENDING 0 OF 2
    census candidate:             STOP/RRI — 48 -> 78 MUTATIONS
    v0.6 Phase B:                 BLOCKED / NOT AUTHORIZED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Stage / commit / push:        NONE

## 34. Phase-A R8 provenance successor — 2026-08-24

### 34.1 Exact review artifacts

| Artifact | Delivery form | SHA-256 | Disposition |
| --- | --- | --- | --- |
| reviews/CC_C5_DISPATCH_V0_5_ROUND2_REVIEW.md | delivered inline historically; exact text supplied by the Homeowner and stored verbatim | 364A179B7D580F0DE97945E2C8C0B771A91A9A3F071DD3310AA9C33DC4225F7A | historical PASS retained, including its later-proven narrow scope |
| reviews/CC_C5_PHASE_A_R8_REQUIREMENT_REGISTER_REVIEW.md | delivered inline; forwarded text stored verbatim | D9B9A23F86E4CEEA8C57B1F55852765FF8306A440BF0FCA235FC7AFC1D90024A | census PASS; provenance correction requested |
| reviews/GPT_C5_PHASE_A_R8_REQUIREMENT_REGISTER_REVIEW.md | supplied attachment bytes stored unchanged | 75E4945B9D77659D3178DFDACBB6E1455506920EC7978F97FB593A0F8A626817 | PASS — PHASE A R8 CLEAN |

The previously missing CC Dispatch v0.5 review is now present. The review chain
is symmetric for both reviewers across all 14 reviewed rounds carried by this
successor.

### 34.2 Delivery-form correction

Current authoritative provenance records that both the CC R6 review and GPT R6
review were delivered inline and that Codex stored the forwarded text verbatim
under `reviews/`.

The sentence in the exact R7 authority describing GPT R6 as “supplied artifact
bytes” was a CC-authored delivery-form error, subsequently identified and
corrected by CC. It is preserved as historical authority text and is not
rewritten. Sections 32.1 and 33.1 are likewise preserved as historical records;
this section supersedes only their GPT R6 delivery-form description.

This correction does not reopen or weaken the R7 authority. Clauses 5b/5c and
the named-constraint prohibitions remain valid Homeowner-approved authority.

### 34.3 Frozen technical state

The R8 technical register, generator outputs, Dispatch and census are
byte-identical to reviewed bundle
`BC7B20BFCDED06F74C1BC3AABC1CF06B8E9749C73342BB940D4486DF71E77B74`.

    union requirements                         279/279
    observations                               156/156
    DISCRIMINATOR / CONTROL                     77 / 79
    mutation IDs / semantic dimensions          78 / 78
    paths / proofs / mutations                   47 / 30 / 78
    Dispatch v0.5 SHA-256                        484C96D2C2EA2EEF5CC2E448472BDAA945956F3499EA965570A07ADFCDBE55B1

No test rerun is required because no executable or technical-register byte
changed.

### 34.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    C5 Dispatch v0.1-v0.5:        REVIEWED
    v0.6 Phase A R8 technical:    GPT PASS / CC CENSUS PASS
    R8 review-chain finding:      CLOSED IN PROVENANCE SUCCESSOR
    census candidate:             TECHNICALLY RATIFIABLE 48 -> 78
    v0.6 Phase B:                 BLOCKED PENDING HOMEOWNER RATIFICATION
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Stage / commit / push:        NONE

## 35. Dispatch v0.6 Phase-B review candidate — 2026-08-24

### 35.1 Binding authority and predecessor

| Item | SHA-256 | Disposition |
| --- | --- | --- |
| baseline | `4D5DFE3E7C96BF0C136E2C1C1FC36C47FE71FB3F` | exact landed baseline |
| Scope v0.3 | `7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5` | ratified / Round 1 closed |
| Dispatch v0.5 | `484C96D2C2EA2EEF5CC2E448472BDAA945956F3499EA965570A07ADFCDBE55B1` | exact predecessor |
| reviewed Phase-A R8 bundle | `BC7B20BFCDED06F74C1BC3AABC1CF06B8E9749C73342BB940D4486DF71E77B74` | dual technical review complete |
| R8 provenance successor | `7B8C326EE2C458CD4303F589662BD6A2F3A7974A6057CA69630CC48EEEFA1F09` | review chain complete |
| authority/HOMEOWNER_C5_PHASE_B_AUTHORITY.md | `21042D3F02E7A006E685A9F343B9E4FB49F9F15A826802BB0DFCBBE938DEC158` | Phase A closed; Phase B docs/register only |
| predecessor ledger through §34 | `D868ACF44ED014B4FE9B7620AE10AEB6462FCBE92ED84E8F23ACE5D19400EE94` | exact byte prefix of this successor |

The Homeowner ratified `47 paths / 30 proofs / 78 mutations`, with exactly
`C5M01-C5M64`, `C5M66-C5M77` and `C5M79-C5M80` allocated. `C5M65` and
`C5M78` remain `CONTROL / NOT_ALLOCATED`. Phase B grants documentation,
register and evidence work only; it grants no implementation or Round-3 work.

### 35.2 Dispatch v0.6 normative incorporation

Dispatch v0.6 SHA-256 is
`3CEFA3069E9BDE871DA5760F920F1EB91A8F49A28BABB12CBE65F992AB6EA4F8`.
Its §§19-22 supersede the historical v0.5 proof/mutation/Task-0 presentation
without rewriting it. The successor binds the following unchanged R8
technical artifacts by exact SHA:

| Artifact | SHA-256 |
| --- | --- |
| requirements/C5_SCOPE_REQUIREMENT_COVERAGE_REGISTER_R8.tsv | `47D39359E372410971B99641758F80823603D8E7AF75D99733E979D83862F6DB` |
| requirements/AUTHORITY_FINDING_REQUIREMENT_REGISTER_R8.tsv | `8DBB62180859C91A3A6A28ADDA47DD8322DE394AB5FC9506414DB40054294F61` |
| requirements/OBSERVATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv | `C376570FE798EB1971FBBFF8964FE05654AA2EC53976EB830E681DAEA000EDD7` |
| requirements/ALL_OBSERVATION_NATURE_AUDIT_R8.tsv | `6AC12C59B26E207F06B099A89CCA628298E5FA6F6710648FD850CD298FEEAC14` |
| requirements/MUTATION_TO_AUTHORITY_REQUIREMENTS_R8.tsv | `350EAD7DB8382EBCF6551CDD49D688A64D2FF32F7753FAF06251B151444B9490` |
| requirements/EXISTING_MUTATION_CORRECTED_ALLOCATION_R8.tsv | `E5BF10EC6D79B86697E0CFD456F9F857BFC96A0E35677CFD07E2A9ABA64D2DC4` |
| requirements/MUTATION_DEMOTIONS_R8.tsv | `4478F7F846C4FB50FD6A6B2BF909527E795E11E63616F25FB0A852F69D0D714C` |
| requirements/AUTHORITY_CLAUSE_CLASSIFICATION_R8.tsv | `AFAC07C21C707BBD90F297D359DC4C2D59F76079C18FF61833D3F4E4A8595031` |
| requirements/SEMANTIC_RECONCILIATION_R8.md | `01D0C4232E8078F0CE9F9976F519C638FDFBFFEAE2706376B507D982A88C9BD7` |
| requirements/DEMOTION_LOAD_BEARING_CONSEQUENCE_R8.md | `F184EA1988089BA9E85F3F96FAE0A3802D4144871F8915A24B5A919DE857A331` |

The finite machine contract also binds the exact 47-path allowlist SHA
`CD6D1BFB385BC1B6213E650080E23AB41CC935A124DE10192C95E87DEC7F47E7`.

### 35.3 Fresh v0.6 Task-0

| Artifact | SHA-256 |
| --- | --- |
| tools/verify_dispatch_v06_task0.py | `43516C4C10B9E3B43C6B19C39D9178472815CA2B723FE4FE9C270DF15BD19F72` |
| task0/TASK0_DISPATCH_V06_PHASE_B.json | `53981F8171132C8B52EC8C99A15678A4571B843E4F9E4BE741BA1F205D630001` |

The verifier parsed the machine contract from the actual Dispatch v0.6 bytes,
reported Dispatch SHA
`3CEFA3069E9BDE871DA5760F920F1EB91A8F49A28BABB12CBE65F992AB6EA4F8`,
hashed every normative artifact and freshly recomputed:

    Scope requirements                         256/256
    Phase-B authority requirements              23/23
    union requirements                         279/279
    observations                               156/156
    DISCRIMINATOR / CONTROL                     77 / 79
    allocated mutations / dimensions            78 / 78
    proof owners                                30/30
    permanent paths                             47/47
    successor authority clauses                 19/19
    demotions                                     2 / C5M65,C5M78

All mandatory gates are true:

    every_union_requirement_has_observation
    every_observation_has_union_requirement
    semantic_assignment_observations_subset_named
    nature_audit_denominator_complete
    every_discriminator_has_mutation
    every_mutation_has_requirement
    authority_clause_denominator_complete
    artifact_hash_bindings_match_dispatch
    mutation_dimensions_unique
    proof_owners_exact
    permanent_paths_exact
    demotions_exact_and_not_allocated

No denominator tripwire fired. The result is derived from v0.6 rather than
copied from the Phase-A/v0.5 Task-0 evidence.

### 35.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               PHASE-B REVIEW CANDIDATE
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    Phase-B dual review:          PENDING 0 OF 2
    Phase B closure:              NOT SELF-CLAIMED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 36. Dispatch v0.6 Phase-B exact-contract correction — 2026-08-24

### 36.1 Dual-review inputs and disposition

| Artifact | Delivery form | SHA-256 | Verdict |
| --- | --- | --- | --- |
| reviews/CC_C5_DISPATCH_V0_6_PHASE_B_REVIEW.md | delivered inline; forwarded text stored verbatim | `FF3335083FB31D18D924FEEF189426AD129A04AE794C43584D19696BDBEEF3D1` | PASS |
| reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_REVIEW.md | supplied attachment bytes stored unchanged | `993AA8BD58FD4B1D3F50DF629E4CB7752283FCD28E85597F771F0203270D8EB2` | PHASE_B_CHANGES_REQUIRED |

Both reviewers inspected exact predecessor bundle
`617010302156B4D9A3DA6C7314501EE4DFE4442FEAF401E494EBA68777C0A641`.
Both independently accepted the B2 fresh-Task-0 mechanics and unchanged
`279/156/77/79/78/30/47` denominator. GPT F01-F08 and F12-F14 are accepted as
substantive count-neutral Dispatch-contract gaps. F09-F11 remain closed. No
requirement 280, observation 157, mutation 79, proof 31 or path 48 was found.

Ledger SHA
`9A9A7EC4EA3A70AB366C2BF59D04292CA3BDCB88ECCB529E8262F969C429E872`
through §35 is an exact byte prefix of this successor.

### 36.2 Exact contract reconciliation

Corrected Dispatch v0.6 SHA-256 is
`37B2FB34E818083EF98D5D92C445D02534BA94EC6B4C9FF78347ACEAFE52BBF7`.
The correction changes exact specification text, not the normative R8
registers or their SHA bindings:

| Finding | Exact correction |
| --- | --- |
| F01/F02 | §2.2 routes using server-owned `requiredScope`; §§2-9 and §19.1 are explicitly current normative contracts |
| F03 | §2.3/§6.1 use one exact recipient-scoped transaction advisory lock before claim, row locks and fresh clock, including absent identity |
| F04 | §5.4/§6.1 define an uncommitted provisional claim shape, completed CHECK shape and rollback-to-absence |
| F05 | §6.1/§7 resolve replay/claim before candidate generation; replay generator calls are zero |
| F06-A | §4/§5.5 define LP-framed base64url TargetIdentity and a colon-bearing absolute vector |
| F06-B | §5.5 names the authoritative revision row for all seven operations |
| F06-C | §4/§5.2/§5.5 define scope-set digest domain/framing, before/after values and audit-v2 binding |
| F07 | §19.1 defines the full ordered C526 managed/replacement/replay/readiness/C1-C4 fixture |
| F08 | §6.3/§19.1 define already-authorized completion versus next-auth rejection |
| F12 | §5.4/§6.1/§7 replay only the immutable committed `ResultSnapshot`, never live rows |
| F13 | §3.2/§6.3/§7 map five conflicts to the exact credential-conflict 409 with a four-plus-fifth success control |
| F14 | §3 pins HttpContext-first auth and manual post-auth JSON parsing; C502 observes parser/repository counters zero |

Historical §12 now states directly that it is not an implementation or proof-
execution source. §19.1 owns the exact Phase-B constructions for C502, C505,
C506, C508, C519, C520, C521 and C526.

### 36.3 Fresh corrected-byte evidence

| Artifact | SHA-256 |
| --- | --- |
| tools/verify_dispatch_v06_task0.py | `B4EB96521E4594EBB0CD199DD40A2A347BCB6BEA56A56C2E0C429EEA455769E3` |
| task0/TASK0_DISPATCH_V06_PHASE_B.json | `A3659985F82705A8FEB4045E38399CA36190A6B1A8B311BAB0522B279EACBC46` |
| tools/verify_dispatch_v06_codecs.py | `2884E590DC9D471DEEC663AD5CB026898AD7005D761C790F71A5F89BAFF3F085` |
| task0/DISPATCH_V06_CODEC_RECOMPUTE.json | `5AC96E2CE78915375ACBF82629A867276ADC63E12AA9FBC8969ED769B315B50D` |

Fresh Task-0 parsed corrected Dispatch SHA `37B2FB34...52BBF7`, recomputed the
unchanged denominator and passed every original gate plus:

    exact_contract_reconciliation_present                 true
    historical_sections_explicitly_non_executable         true

The codec verifier independently recomputed and found all five values bound:

    empty scope-set digest                                 PASS
    exact C3/C4 scope-set digest                           PASS
    colon-bearing framed TargetIdentity / length           PASS
    audit-v2 evidence digest                               PASS

### 36.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               CORRECTED PHASE-B REVIEW CANDIDATE
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    predecessor Phase-B review:  CC PASS / GPT CHANGES REQUIRED
    correction dual review:      PENDING 0 OF 2
    Phase B closure:              NOT SELF-CLAIMED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 37. Dispatch v0.6 Phase-B correction R2 successor — 2026-08-25

### 37.1 Exact predecessor and review provenance

This successor binds exact correction-R1 bundle SHA-256
`08BD3F7F9C3EE4780E413145FDF5782BAF3DB05BA5BFC36E8FBA1C2BDC4DC873`.
Ledger SHA
`83D7C7DE6773AA3AAA9F3DB9B9D5290F9C43B74E44D1D720C88A3E81CA21B221`
through §36 is an exact byte prefix of this successor.

Both correction-R1 reviews were delivered inline and their forwarded text is
stored verbatim:

| Review artifact | Original delivery form | SHA-256 | Verdict |
| --- | --- | --- | --- |
| `reviews/CC_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R1_REVIEW.md` | delivered inline; forwarded verbatim text stored under `reviews/` | `31FAB93209FC1F3C8D2F8696602970AAA4EF1EBA4E4391484C0858C0FF77EA5C` | PASS |
| `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R1_REVIEW.md` | delivered inline; forwarded verbatim text stored under `reviews/` | `E392588CDB734678A02F6005D4A27ABA3D16DC15515C4C5837356254B4DDAB6D` | PHASE_B_CHANGES_REQUIRED |

Provenance correction F01: predecessor artifact
`reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_REVIEW.md` was also delivered inline and
its forwarded verbatim text was stored under `reviews/`. The §36.1 phrase
`supplied attachment bytes stored unchanged` is historically retained but is
superseded as a delivery-form description only. The review bytes, SHA-256
`993AA8BD58FD4B1D3F50DF629E4CB7752283FCD28E85597F771F0203270D8EB2`
and verdict are unchanged.

### 37.2 Count-neutral exact-contract reconciliation

Corrected Dispatch v0.6 SHA-256 is
`1CE09168DCEF7FD76C702172ECE1538FA74B2BDE8B643D4A062A400342EBFB49`.

| Finding | Exact R2 correction |
| --- | --- |
| F01 | §37.1 and the review-chain status record the true inline delivery form without modifying any review text or historical ledger byte. |
| F02 | §2.3/§6.1 pin `authenticate -> initial completed-operation probe -> advisory lifecycle authority -> mandatory re-probe -> provisional claim -> lifecycle rows -> fresh clock`; the first probe is read-only and the re-probe closes its race. |
| F03 | §6.1/§6.3/§7 distinguish intermediate internal `CandidateConflict`, which retains transaction/advisory/claim for retry, from terminal exhaustion after candidate five, which rolls back and returns the exact 409. |
| F04 | §5.5/§6.3 define exact scope-digest transitions for recipient enrollment and credential issue/replacement/revoke; all key events retain NULL digests. |
| F05 | §19.1 pins C508 operation A to the real C3 authenticated delivery response stream, paused after authorized context acquisition and resumed without credential re-resolution; a new C3/C4 request is denied. |
| F06 | §5.4 enumerates the exact seven-operation Outcome/result/count/snapshot sparse matrix, names each `ResultRevision` authority and forbids fields outside the owning operation row. |

The correction changes specification/proof-construction text only. It adds no
requirement, observation, mutation dimension, proof owner or permanent path.
The ratified denominator remains exactly:

    279 requirements / 156 observations / 77 D / 79 C
    78 allocated mutations / 30 proof owners / 47 permanent paths

### 37.3 Fresh R2 evidence

| Artifact | SHA-256 |
| --- | --- |
| `tools/verify_dispatch_v06_task0.py` | `58DFF70943699A0E5A6E08A12997EE8A35096A92987026EEBED4B021D5E60DD5` |
| `task0/TASK0_DISPATCH_V06_PHASE_B.json` | `1BF5D63399092A2462C87D6645CECF321DF199356897FC8EAF4A592A67E1AA7E` |
| `tools/verify_dispatch_v06_codecs.py` | `2884E590DC9D471DEEC663AD5CB026898AD7005D761C790F71A5F89BAFF3F085` |
| `task0/DISPATCH_V06_CODEC_RECOMPUTE.json` | `D34855CB1C60720DBD30A42F0B4BD6982C73ECC2A17C92C59F1123BC3923AD4F` |

Fresh Task-0 parsed corrected Dispatch SHA `1CE09168...EBFB49`, verified all
14 gates, all 12 register/hash bindings and the unchanged denominator. The
fresh codec verifier independently reproduced and bound all five current
values: empty and exact activation scope digests, colon-bearing framed target
identity and length, and audit-v2 evidence digest. The absolute vector remains
unchanged because its `ManagedRecipientEnrolled` scope transition remains
empty-to-exact under the R2 lifecycle table.

### 37.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               PHASE-B CORRECTION R2 REVIEW CANDIDATE
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    correction R1 review:        CC PASS / GPT CHANGES REQUIRED
    correction R2 dual review:   PENDING 0 OF 2
    Phase B closure:              NOT SELF-CLAIMED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 38. Dispatch v0.6 Phase-B correction R3 successor — 2026-08-25

### 38.1 Predecessor, inline review and process direction

This successor binds exact correction-R2 bundle SHA-256
`31FE40D924D3658A1AE8F13022F1D5161546D9D2F9402E961FB6D6FED75F3376`.
Ledger SHA
`4760DE4B2D508C6F23F46D9BF5DBFECB7DE0E18F75E87695C8E4F8FC917C0B0F`
through §37 is an exact byte prefix of this successor.

GPT correction-R2 review was delivered inline and its forwarded text is stored
verbatim:

| Review record | Original delivery form | SHA-256 | Verdict |
| --- | --- | --- | --- |
| `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R2_REVIEW.md` | delivered inline; forwarded verbatim text stored under `reviews/` | `E6E03881983B034C7387A4281B86640CC9E26353EF9A8E8DFCC36EF0FC283D4B` | PHASE_B_CHANGES_REQUIRED |

No separate CC correction-R2 review was supplied. No CC wording or verdict is
inferred. Homeowner process direction
`authority/HOMEOWNER_C5_INLINE_REVIEW_PROCESS_DIRECTION.md`, SHA-256
`357AC045CD5C2BCEADEF96A477FAD4EAFB1151CC82D8818473B87E1456E5F273`,
supersedes the file-artifact gate: reviewer reports are inline messages, and a
successor must not pause merely to demand a separate CC artifact. Forwarded
inline review text is stored verbatim when it exists; an absent review remains
explicitly absent and is never reconstructed. Both reviewers receive this same
R3 ZIP/SHA for the next review gate.

### 38.2 Count-neutral exact-contract reconciliation

Corrected Dispatch v0.6 SHA-256 is
`CD7BA59D4EAC33729072DE594FAE5C187456649F495978D0310FE011D8A01198`.

| Finding | Exact R3 correction |
| --- | --- |
| F01 | §5.4 adds internal `AdmissionAtUtc` to the new C5 operation claim. The first credential candidate reaching post-lock admission captures and stores the sole fresh clock; retries keep the transaction/advisory/claim and reuse that value. C521 drives four conflicts plus a unique fifth and requires exactly one capture plus identical final validity/audit time. |
| F02 | §5.5/§6.2/§6.3 define the management event as sole scope-digest authority. `ResultSnapshot` remains exactly the public response without `PresentedKey` and without audit-only fields; event and operation bind by the same `OperationId`. |
| F03 | §3 uses explicit C5-local Web JSON options with case-sensitive properties and `JsonUnmappedMemberHandling.Disallow`. C501 pins the options; C509 sends ten representative forbidden/server-owned members and requires request-invalid with repository calls zero. Global API JSON behavior is unchanged. |

The correction changes the planned new C5 operation-row shape and exact proof
construction inside the already-authorized Dispatch. It changes no landed
source/test/schema/migration byte and adds no permanent path, proof,
observation, requirement or mutation dimension. Denominator remains:

    279 requirements / 156 observations / 77 D / 79 C
    78 allocated mutations / 30 proof owners / 47 permanent paths

### 38.3 Fresh corrected-byte evidence

| Artifact | SHA-256 |
| --- | --- |
| `tools/verify_dispatch_v06_task0.py` | `518D037842848E49F7B30AA49E85FC4B807779956B273B8CBFE5B3B801EB32FC` |
| `task0/TASK0_DISPATCH_V06_PHASE_B.json` | `08176794C8C9E7710CA5AE4E5C1D4259C21161DEF9E80E9D688EECC8057ECA2A` |
| `tools/verify_dispatch_v06_codecs.py` | `2884E590DC9D471DEEC663AD5CB026898AD7005D761C790F71A5F89BAFF3F085` |
| `task0/DISPATCH_V06_CODEC_RECOMPUTE.json` | `2F7B766C5658B65E4F97E4AD25B3A8B198F1973C6154E61FFBCA9306079FF655` |

Fresh Task-0 parsed Dispatch SHA `CD7BA59D...A01198`, passed all 14 gates,
all register/hash bindings and the unchanged denominator. Fresh codec evidence
remains 5/5 because R3 changes neither the scope-set/target framing nor audit-v2
preimage. The response-snapshot correction removes an overclaim; it does not
alter the event audit codec.

### 38.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               PHASE-B CORRECTION R3 REVIEW CANDIDATE
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    correction R2 review:        GPT CHANGES REQUIRED / CC NOT SUPPLIED
    correction R3 review:        PENDING INLINE REVIEW FROM BOTH
    Phase B closure:              NOT SELF-CLAIMED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 39. Dispatch v0.6 Phase-B correction R4 successor — 2026-08-25

### 39.1 Predecessor, R3 review and explicit governance disposition

This successor binds exact correction-R3 bundle SHA-256
`FF105CDAF35923DFB3300D8734EB5B729AA879754E693726029B925F5AA3E418`.
Ledger SHA
`3BBA00F68E2C6E558DB1EDAF78BF171DCDB9D9877DA173D030D0DF964186BFCC`
through §38 is an exact byte prefix of this successor.

The GPT correction-R3 review is carried verbatim:

| Review record | Original delivery form | SHA-256 | Verdict |
| --- | --- | --- | --- |
| `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R3_REVIEW.md` | Homeowner-supplied attachment text stored byte-exact | `8A88B3DB47A6D9DB08F38E48284EB495DD8F358A65E57E0B5962F0F1E4A1FCB4` | PHASE_B_CHANGES_REQUIRED |

No CC correction-R2 review is represented as having occurred. Exact Homeowner
disposition
`authority/HOMEOWNER_C5_PHASE_B_R3_F01_GOVERNANCE_DISPOSITION.md`, SHA-256
`8C2F66233111A5FEBA2591E7CCA7E18CC3D115995647E37AF5E7F84E2CED0E83`,
explicitly waives and supersedes that missing intermediate review and requires
dual independent review of this exact R4 successor as its replacement for
Phase-B closure. Inline review delivery is authoritative; Codex stores
forwarded text verbatim and does not invent a separate reviewer artifact.

No CC correction-R3 review was supplied and none is reconstructed. The R3 GPT
finding record remains complete; both reviewers must now review this same R4
ZIP/SHA. Phase B cannot close from either review alone.

### 39.2 Count-neutral R3 finding reconciliation

Corrected Dispatch v0.6 SHA-256 is
`6F4B34631E8D7C1C4C827EBE50E5B9AB2F2624701834617037A9DC1A67381482`.

| Finding | Exact R4 correction |
| --- | --- |
| F01 | §39.1 records the explicit Homeowner waiver/supersession for the absent CC correction-R2 review and binds Phase-B closure to dual independent review of this exact R4 successor. |
| F02 | §6.3 pins every landed `api_keys` insert column: identity, server-owned credential reference/type/status, 16-character prefix, 32-byte peppered hash, canonical two-scope JSON, expiry/category, four NULL authority fields and `CreatedAt=AdmissionAtUtc`. Replacement uses the same projection without inheriting predecessor authority fields. |
| F03 | §5.5.1 provides a finite seven-operation server-time authority table. Every C5-created lifecycle timestamp, operation completion and event occurrence derives from the sole persisted post-lock `AdmissionAtUtc`; second clocks and caller/application times are prohibited. |
| F04 | §5.4 admits pre-admission and post-admission provisional sub-shapes for every operation kind while outcome/snapshot/completion/results remain NULL. C520 adds a non-credential `EnrollKey` intermediate-shape control. |
| F05 | §6.1 targets `ON CONFLICT ("ManagerPrincipalId","IdempotencyKeyDigest")`. An unrelated `PK_raw_export_recipient_management_operations` collision is unsuppressed, rolls back and maps to `Unavailable`. |
| F06 | §3/§6.3 make `CandidateConflict` iff SQLSTATE `23505` names exact landed index `IX_api_keys_KeyPrefix`. Every other uniqueness authority consumes zero candidate retries and maps through its explicit fail-closed `Conflict`/`Unavailable` path. |

The correction changes only Dispatch exact-contract/proof-construction text and
this append-only ledger. It changes no landed source/test/schema/migration byte
and adds no permanent path, proof, observation, requirement or mutation
dimension. The denominator remains exactly:

    279 requirements / 156 observations / 77 D / 79 C
    78 allocated mutations / 30 proof owners / 47 permanent paths

### 39.3 Fresh corrected-byte evidence

| Artifact | SHA-256 |
| --- | --- |
| `tools/verify_dispatch_v06_task0.py` | `518D037842848E49F7B30AA49E85FC4B807779956B273B8CBFE5B3B801EB32FC` |
| `task0/TASK0_DISPATCH_V06_PHASE_B.json` | `8542FA867F964665F9F3964D1004FCD538E9AA6359B1255194BF67B03B7A213F` |
| `tools/verify_dispatch_v06_codecs.py` | `2884E590DC9D471DEEC663AD5CB026898AD7005D761C790F71A5F89BAFF3F085` |
| `task0/DISPATCH_V06_CODEC_RECOMPUTE.json` | `4EB984CBF206C7F01997EB37682916D3898AEF19BB12E0DB9A690D409EA5650E` |

Fresh Task-0 parsed Dispatch SHA `6F4B3463...381482`, passed all 14 gates,
all register/hash bindings and the unchanged denominator. Fresh codec evidence
passes 5/5; R4 changes no scope-set, framed target or audit-v2 preimage byte.
The live Dispatch is LF-only.

### 39.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               PHASE-B CORRECTION R4 REVIEW CANDIDATE
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    correction R2 review:        GPT CHANGES REQUIRED / CC WAIVED-SUPERSEDED
    correction R3 review:        GPT CHANGES REQUIRED / CC NOT SUPPLIED
    correction R4 dual review:   PENDING 0 OF 2
    Phase B closure:              NOT SELF-CLAIMED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 40. Dispatch v0.6 Phase-B governance-only successor R5 — 2026-08-25

### 40.1 Exact predecessor and R4 independent review

This successor binds exact correction-R4 bundle SHA-256
`6238A202709AF94841760E2F832FC556BB9894808584C039DAB2BEA84E80A664`.
Ledger SHA
`2A8BAF91C0FC013FDC1D3BF80065825C2CF87B037DEE49015067EC06F81C9EC9`
through §39 is an exact byte prefix of this successor.

The GPT correction-R4 review was delivered inline and its forwarded text is
stored verbatim:

| Review record | Original delivery form | SHA-256 | Verdict |
| --- | --- | --- | --- |
| `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R4_REVIEW.md` | originally delivered inline; Homeowner-forwarded text stored verbatim | `FB195980DB8DB4E7C1BAD93D586C59B9E59465F078E9F28C822080CA86628C67` | TECHNICAL PASS / GOVERNANCE CHANGES REQUIRED |

The review independently passed exact R4 ZIP/Dispatch/Ledger mechanics, fresh
Task-0 14/14, codec 5/5, the unchanged denominator and the full technical sweep.
It closed R3 F02-F06 and opened governance findings G01/G02 only. No CC R4
review is represented as supplied; this R5 successor is the explicit dual
replacement review gate.

### 40.2 Homeowner governance disposition

Exact authority
`authority/HOMEOWNER_C5_PHASE_B_R4_G01_G02_GOVERNANCE_DISPOSITION.md`,
SHA-256
`AF65A139AE10BA37FA4F0709294527CC421953B99AE6A4C74E3D4C5E7AC2CD5F`,
closes the two R4 governance defects as follows:

1. the missing CC correction-R3 review for bundle
   `FF105CDAF...3E418` is explicitly waived and superseded without being
   represented as having occurred;
2. dual independent review of this same governance-only R5 ZIP/SHA replaces
   that missing intermediate review for Phase-B closure;
3. GPT correction-R3 review SHA
   `8A88B3DB...1FCB4` is unchanged, but §39.1's phrase
   `Homeowner-supplied attachment text stored byte-exact` is superseded as
   incorrect delivery-form metadata only;
4. the correct provenance is: originally delivered inline; forwarded text
   stored verbatim under
   `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R3_REVIEW.md`.

Review bytes, SHA, findings and verdict are unchanged. No absent CC text is
reconstructed. Inline successor reviews are authoritative and may be stored
verbatim without a separately generated reviewer file.

### 40.3 Exact-byte preservation and fresh evidence

Dispatch v0.6 remains byte-identical at SHA-256
`6F4B34631E8D7C1C4C827EBE50E5B9AB2F2624701834617037A9DC1A67381482`.
The source snapshot changes from R4 in exactly this append-only ledger; no
Dispatch, source, test, schema or migration byte changes.

| Artifact | SHA-256 |
| --- | --- |
| `tools/verify_dispatch_v06_task0.py` | `518D037842848E49F7B30AA49E85FC4B807779956B273B8CBFE5B3B801EB32FC` |
| `task0/TASK0_DISPATCH_V06_PHASE_B.json` | `7B241782EBEB5CDCD6989F861782B43C576FE31E7474F4C12F35B07D3BAC5AFF` |
| `tools/verify_dispatch_v06_codecs.py` | `2884E590DC9D471DEEC663AD5CB026898AD7005D761C790F71A5F89BAFF3F085` |
| `task0/DISPATCH_V06_CODEC_RECOMPUTE.json` | `4EB984CBF206C7F01997EB37682916D3898AEF19BB12E0DB9A690D409EA5650E` |

Fresh Task-0 passes all 14 gates and derives the unchanged denominator:

    279 requirements / 156 observations / 77 D / 79 C
    78 allocated mutations / 30 proof owners / 47 permanent paths

Fresh codec remains 5/5 on the same Dispatch bytes. No denominator STOP fires.

### 40.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               TECHNICAL PASS / BYTE-IDENTICAL
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    correction R2 missing CC:    WAIVED-SUPERSEDED / NOT REPRESENTED
    correction R3 missing CC:    WAIVED-SUPERSEDED / NOT REPRESENTED
    GPT R3 provenance:           INLINE / FORWARDED VERBATIM
    R5 governance dual review:   PENDING 0 OF 2
    Phase B closure:              NOT SELF-CLAIMED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 41. Dispatch v0.6 Phase-B review-chain successor R6 — 2026-08-25

### 41.1 Exact predecessor and late-supplied review records

This provenance-only successor binds exact governance-R5 bundle SHA-256
`1A42CF1D90511B8E815610E84E6FD8833CFC73E24C5C5369FEF1CA2EF2F5CD1E`.
Ledger SHA
`2F4305F17BD569F0B5ADCB57D1DA6AFF9CA0500E5A0BC83BB93A545F560AB2F5`
through §40 is an exact byte prefix of this successor.

The Homeowner later supplied the following CC records verbatim. Historical
waiver and missing-review statements in §39/§40 remain immutable descriptions
of what was available at those gates; this successor records the later review
events and supersedes the missing-evidence condition for current provenance.

| Review coverage | Review record | Delivery/provenance | SHA-256 | Verdict |
| --- | --- | --- | --- | --- |
| correction R2 | `reviews/CC_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R2_REVIEW.md` | originally delivered inline; later forwarded and stored verbatim | `8D4352BC7C1796C5F81B2BD25A190FA2B0EC3D86F7E88DF970A77D08BC08E4BB` | PASS |
| corrections R3 and R4 | `reviews/CC_C5_DISPATCH_V0_6_PHASE_B_CORRECTION_R3_R4_REVIEW.md` | one later review of both exact bundles; one artifact intentionally covers both rounds | `EEB4C072E3B73722860F28C73D93950D88F5A33D5A9A89EB140B01FF3B8B3B13` | PASS R3 / PASS R4 |
| governance R5 | `reviews/CC_C5_DISPATCH_V0_6_PHASE_B_GOVERNANCE_R5_REVIEW.md` | delivered inline; Homeowner-forwarded text stored verbatim | `A55C0D26F8418E1174AB8455453143BE2C490BC9337CBE7F3C1C5685FDA6781B` | TECHNICAL PASS / REVIEW-CHAIN HOLD |
| governance R5 | `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_GOVERNANCE_R5_REVIEW.md` | supplied attachment bytes copied unchanged | `C5DB7AA321C4EDD53FF00956C12F74FCDDA8BE557C953ABA8C999D331C8C3DC2` | PASS |

The combined CC R3/R4 record is not split into two fabricated artifacts. It
explicitly identifies and independently reviews exact bundles
`FF105CDAF35923DFB3300D8734EB5B729AA879754E693726029B925F5AA3E418`
and
`6238A202709AF94841760E2F832FC556BB9894808584C039DAB2BEA84E80A664`.
Coverage accounting therefore counts it once as a file and once for each of
the two review rounds it actually covers.

### 41.2 Review-chain reconciliation

The complete review history now covers 21 of 21 review rounds for each
reviewer:

```text
Scope v0.1-v0.2                 CC 2 / GPT 2
Dispatch v0.1-v0.5              CC 5 / GPT 5
Phase-A R1-R8                   CC 8 / GPT 8
Phase-B predecessor review      CC 1 / GPT 1
Phase-B correction R1 review    CC 1 / GPT 1
Phase-B correction R2 review    CC 1 / GPT 1
Phase-B correction R3 review    CC 1 / GPT 1
Phase-B correction R4 review    CC 1 / GPT 1
Phase-B governance R5 review    CC 1 / GPT 1
Round coverage                  CC 21 / GPT 21
Stored review artifacts         CC 20 / GPT 21
```

The artifact-count asymmetry is intentional: one CC artifact covers both R3
and R4. It is not a missing review and must not be repaired by copying or
splitting the record.

CC R5 finding `C5-PB-R5-CC-F01` is CLOSED by the supplied R2 record and the
combined retrospective R3/R4 review. GPT R5 reports no new finding. Both
reviewers reviewed exact R5 ZIP SHA
`1A42CF1D90511B8E815610E84E6FD8833CFC73E24C5C5369FEF1CA2EF2F5CD1E`;
the Phase-B independent closing gate is therefore 2/2 PASS after this
provenance reconciliation.

### 41.3 Exact-byte preservation

Dispatch v0.6 remains byte-identical at SHA-256
`6F4B34631E8D7C1C4C827EBE50E5B9AB2F2624701834617037A9DC1A67381482`.
The R5-to-R6 repository snapshot delta is exactly this append-only ledger. No
source, production, test, schema or migration byte changes. Task-0, codec and
the denominator remain carried from the exact same Dispatch bytes:

```text
279 requirements / 156 observations / 77 D / 79 C
78 allocated mutations / 30 proof owners / 47 permanent paths
Task-0 14/14 / codec 5/5
```

No test rerun is required or authorized for this provenance-only successor.

### 41.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               TECHNICAL PASS / BYTE-IDENTICAL
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    review round coverage:        CC 21/21 / GPT 21/21
    stored review artifacts:      CC 20 / GPT 21 (CC R3+R4 COMBINED)
    R5 governance dual review:    PASS 2 OF 2
    C5-PB-R5-CC-F01:              CLOSED
    Phase B closeout eligibility: READY FOR HOMEOWNER RATIFICATION
    Phase B closure:              NOT SELF-RATIFIED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 42. Dispatch v0.6 Phase-B final governance correction R7 — 2026-08-25

### 42.1 Exact predecessor, authority and R6 independent reviews

This governance-only correction binds exact R6 bundle SHA-256
`FC887F38DF9055BDF99462A9E51660C267CEB6402D0EFCE5F955F3732B835EE8`.
Ledger SHA
`67E50E45C3010B7BDCF4ED595480819DA08E5724F3B3EF04DF2A4AA0196CC93E`
through §41 is an exact byte prefix of this successor.

Exact Homeowner authority is carried at
`authority/HOMEOWNER_C5_PHASE_B_R7_FINAL_GOVERNANCE_CORRECTION.md`, SHA-256
`D882512989CE6E2473EF31018A81772D7C737AE1D3548BF8DD123CDED79244C4`.

Both R6 reviews are carried verbatim:

| Review record | Original delivery form | SHA-256 | Verdict |
| --- | --- | --- | --- |
| `reviews/CC_C5_DISPATCH_V0_6_PHASE_B_PROVENANCE_R6_REVIEW.md` | delivered inline; Homeowner-forwarded text stored verbatim | `769CC76624F62FF07E3851411C9552473C6E95918F81466C7E5C67E22D6D09CE` | PASS |
| `reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_PROVENANCE_R6_REVIEW.md` | supplied attachment containing the forwarded inline review; attachment bytes copied unchanged | `AA75E4FEEBAF63C832AC5E87D4F1C5E70870AF0CFBCE1786D452483AA5C69ED8` | TECHNICAL PASS / GOVERNANCE CHANGES REQUIRED |

GPT R6 findings F01-F03 are accepted. R6 is not treated as the Phase-B closing
gate and its incorrect current-state declarations are superseded below without
rewriting immutable history.

### 42.2 F01 — GPT governance-R5 delivery provenance

GPT governance-R5 review SHA-256 remains
`C5DB7AA321C4EDD53FF00956C12F74FCDDA8BE557C953ABA8C999D331C8C3DC2`.
It was originally delivered inline; the Homeowner-forwarded text was stored
verbatim at
`reviews/GPT_C5_DISPATCH_V0_6_PHASE_B_GOVERNANCE_R5_REVIEW.md`.

The §41.1 phrase `supplied attachment bytes copied unchanged` and the matching
R6 line-ending-register reason are superseded as incorrect delivery-form
metadata only. Review bytes, SHA-256, verdict, findings and exact R5 bundle
binding remain unchanged.

### 42.3 F02 — preserve the R5 HOLD and restore the successor gate

The historical CC governance-R5 verdict remains exactly:

    TECHNICAL PASS / REVIEW-CHAIN HOLD

R5 did not become PASS 2/2. The late review records first appeared in R6, so
§41.2's statement that the R5 independent closing gate was 2/2 PASS and §41.4's
`C5-PB-R5-CC-F01: CLOSED` / `READY FOR HOMEOWNER RATIFICATION` declarations are
explicitly superseded as premature current-state claims.

The controlling state is:

    C5-PB-R5-CC-F01:              ADDRESSED_BY_R7 / PENDING_R7_DUAL_REVIEW
    R7 dual review:               PENDING 0 OF 2
    Phase B:                      NOT YET RATIFIED

### 42.4 F03 — current line-ending register

The current slim-bundle notice points only to
`metadata/LINE_ENDING_EXCEPTIONS_PHASE_B_R7.tsv`. The stale R6 reference to the
nonexistent `_R5.tsv` file is superseded. Exact carried bytes remain unchanged;
the R7 register enumerates every actual CR-bearing file and its current count.

### 42.5 Terminating rule

If both reviewers PASS the exact R7 ZIP/SHA, the Homeowner ratification records
their names, verdicts and the exact closing bundle SHA directly. No R8 is
created merely to package those R7 PASS reviews. The ratification is the
terminating governance record and is not itself a review bundle requiring
another review.

### 42.6 Exact-byte preservation and current lifecycle

Dispatch v0.6 remains byte-identical at SHA-256
`6F4B34631E8D7C1C4C827EBE50E5B9AB2F2624701834617037A9DC1A67381482`.
The R6-to-R7 repository snapshot delta is exactly this append-only ledger. No
source, production, test, schema or migration byte changes; no tests are run.

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Dispatch v0.6:               TECHNICAL PASS / BYTE-IDENTICAL
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    R6 CC review:                 PASS
    R6 GPT review:                TECHNICAL PASS / GOVERNANCE CHANGES REQUIRED
    C5-PB-R5-CC-F01:              ADDRESSED_BY_R7 / PENDING_R7_DUAL_REVIEW
    R7 dual review:               PENDING 0 OF 2
    Phase B:                      NOT YET RATIFIED
    Round 3 authority:            NONE
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE

## 43. Phase-B terminating ratification and Round-3 exact-byte reconciliation — 2026-08-25

### 43.1 Terminating Homeowner ratification

Exact Homeowner ratification is carried at
`authority/HOMEOWNER_C5_PHASE_B_CLOSED_ROUND3_AUTHORITY.md`, SHA-256
`5A1BB5BF7DDA38B098F4932FAE6E54FE95D14278FCC226565214FBCDAC70975F`.
It binds:

```text
Baseline        4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f
Scope v0.3      7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5
Dispatch v0.6   6F4B34631E8D7C1C4C827EBE50E5B9AB2F2624701834617037A9DC1A67381482
Closing bundle  3B7F4B63C632846CDC2A1C83ADC64AF39F4F76BB23604900C851A37BCCBE4837
Closing ledger  41C53EF38B5BF9C1AE8B5461155329095F38A0566D1DCCF93C13EE3FB47F38DA
R7 CC verdict   PASS
R7 GPT verdict  PASS
```

The ratification is the terminating record of the review chain. No R8 is
created merely to store the two R7 PASS verdicts. `C5-PB-R5-CC-F01` is CLOSED
BY R7 and Phase B is CLOSED. Dispatch v0.6 at the SHA above is the ratified
executable specification.

### 43.2 Round-3 authority boundary

Round 3 is exact-byte reconciliation only. It cannot introduce a product/trust
decision, requirement, observation, mutation, proof, permanent path or census
change. Discovery of such a need is
`STOP/RRI — PHASE B NOT ACTUALLY CLOSED`.

Implementation, source/test/schema/migration change, test execution, stage,
commit, push, merge, PR, deployment and production activation remain
unauthorized.

### 43.3 Five exact-byte gates

| Gate | Reconciliation result |
| --- | --- |
| Byte diff | R7-to-Round-3 repository snapshot changes only this append-only Review Ledger. Dispatch and every executable/source/test/schema/migration path remain byte-identical. |
| Census invariance | `279 requirements / 156 observations / 77 D / 79 C / 78 allocated mutations / 30 proof owners / 47 permanent paths`; C5M65 and C5M78 remain CONTROL / NOT_ALLOCATED. |
| Append-only ledger | R7 ledger SHA `41C53EF38B5BF9C1AE8B5461155329095F38A0566D1DCCF93C13EE3FB47F38DA` is the exact byte prefix of this Round-3 successor. |
| §19 register binding | All ten incorporated artifacts are present and their actual SHA-256 equals the exact Dispatch-bound SHA-256. |
| Absolute vectors | Fresh codec recomputation binds all five values: activation scope-set digest, audit-v2 digest, colon-bearing target identity and length, and empty scope-set digest. |

Fresh Task-0 must independently recompute all 14 gates from the same Dispatch
bytes. Round-3 review must verify these gates mechanically and must not reopen
the ratified content.

### 43.4 Current lifecycle successor

    C5 Scope v0.3:                RATIFIED / ROUND 1 CLOSED
    Phase A:                      CLOSED
    Phase B:                      CLOSED / HOMEOWNER RATIFIED
    C5-PB-R5-CC-F01:              CLOSED BY R7
    Dispatch v0.6:               RATIFIED EXECUTABLE SPECIFICATION
    census:                       47 PATHS / 30 PROOFS / 78 MUTATIONS
    Round 3:                      EXACT-BYTE RECONCILIATION REVIEW CANDIDATE
    Round-3 dual review:          PENDING 0 OF 2
    Implementation authority:    NONE
    Tests:                        NOT RUN / NOT AUTHORIZED
    Stage / commit / push:        NONE
    Merge / PR / deployment:      NONE
    Production activation:        NONE
