# TIP-88C1-C4 — Authenticated Package Reference Distribution Review Ledger

Version: `0.1`
Status: `ROUND_1_REVIEW_PENDING — APPEND_ONLY`
Date: `2026-08-19`
Baseline commit: `8d76689a746960bd24ca944eaaf4858f39026511`
Scope candidate: `tip_88c1_c4_authenticated_package_reference_distribution_scope_brief.md` v0.1
Owner: `Homeowner + Codex Contractor/Drafter`
Implementation authority: `NONE`

## 0. Changelog

### v0.1 — Ledger opened before Round 1

- Binds the exact Homeowner C4 docs-only authority and landed baseline.
- Registers the Round-1 decisions, debts, review questions and successor rules.
- Assigns but does not close `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1`.
- Preserves the three-round documentation boundary.

## 1. Append-only rule

This ledger is append-only after a candidate is sent for review.

Each review entry must bind:

```text
scope path + exact SHA-256
ledger path + exact SHA-256
baseline commit
bundle SHA-256
reviewer identity
verdict and timestamp
finding/counter-claim ids
successor disposition
```

Do not edit a reviewed row to change its verdict or state. Add a successor row
that names the predecessor SHA and exact closure evidence.

## 2. Authority and bound source set

| Source | SHA-256 | Role |
| --- | --- | --- |
| Homeowner C4 Round-1 authority | `C9469AC246901CD97DF57D0DC7AA81D55831273DE3F8956991BBCD9C39761027` | exact docs-only authority |
| Repository baseline | `8d76689a746960bd24ca944eaaf4858f39026511` | landed C1-C3 provenance |
| C2 scope | `B48AECEEEB2CEA2B71A6BA2951BEC7339195DA10AB72713AD0D834051FC56851` | package ownership intent |
| C2 dispatch | `0790D1A6828C3365178011F0E10A1B58757B4FE7E2214828C55A6D720B0BB64D` | package lifecycle/proof contract |
| C2 as-built | `C36855C44C221A1744570E9943B1659F462DE50169BD61A3BF89DDEA997B95AA` | landed evidence |
| C3 scope | `A07704EAE33A9FFCC189C365C9FB58EF19C3E7D29BD0F390F65080CCB7323D80` | delivery/trust boundary |
| C3 dispatch | `AB415A0526D65307E0036A450374928BA81BEF53C4E8BACAA7D837997BB86988` | independent authorization and proof contract |
| C3 as-built | `C204A40F5C9AD03C852CAD714606403730F3FCC5873DCD7C9D3087B330BF2D33` | open reachability debt and landed behavior |
| Authenticated context | `4F4971FAA92BCDC556B1F4329F7BC9AB7C53F6214FCAD2188ABF13DC5C346401` | actor/client/principal model |
| API authenticator | `72A7E14658FFC4F3990D87CA590CD2A19084FC012C3D0813A2351A12750F9CF4` | authentication boundary |
| API-key provisioning | `540A810A10A9099BCD333243BC056228B5AE6AFA95773D87C07FBDFD3552A7D1` | persisted principal source |
| PostgreSQL API-key store | `96497E131EA7C67DE6307760D007E5BD67A4DA808F2627504EF2B842CFB65A91` | principal readback |
| C2 package row | `AAFEAA6DED7D4140B7F02F290EB0BFA1B3ACB7CD8DC89219CE8DCD60D156B62E` | owner/state/order data |
| C2 migration | `874F11A017588CF015D5523FD135E41BC142DF426FE1D230E50C620801CD1303` | exact package schema/ACL |
| C3 migration | `93444EE69B3442F72C8052FD8A9894FAB1248761199947E7D966F6934782A9AD` | landed fourth capability |

If any anchor cannot be reproduced from exact supplied evidence, reviewers must
report `REVIEW_CHAIN_INCOMPLETE`; they must not infer a predecessor verdict.

## 3. Review round register

| Round | Candidate | Scope SHA | Ledger SHA | Bundle SHA | Reviewer | Verdict | Findings | Successor | State |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | v0.1 | `TO_BE_FROZEN_IN_BUNDLE` | `TO_BE_FROZEN_IN_BUNDLE` | `TO_BE_FROZEN_IN_BUNDLE` | independent reviewer A | `PENDING` | none registered | v0.2 only if required | `OPEN` |
| 1 | same exact v0.1 | same | same | same | independent reviewer B | `PENDING` | none registered | reconcile both reports | `OPEN` |
| 2 | executable dispatch | unset | appended successor | unset | independent reviewers | `NOT_AUTHORIZED` | — | Round 3 | `CLOSED_GATE` |
| 3 | exact-byte reconciliation | unset | appended successor | unset | independent reviewers | `NOT_AUTHORIZED` | — | controlled build | `CLOSED_GATE` |

## 4. Finding and counter-claim register

No finding is pre-closed. Reviewers append one row per finding or counter-claim.

| Finding ID | Reviewer | Severity | Claim | Exact evidence | Drafter disposition | Successor anchor | State |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `C4-R1-PENDING` | — | — | awaiting independent review | scope v0.1 | — | — | `OPEN_SENTINEL` |

The sentinel is removed only by an append-only successor after both exact
Round-1 reviews are reconciled.

## 5. Initial decision register

| Decision | Selected rule | Rejected alternative | Required reviewer challenge | State |
| --- | --- | --- | --- | --- |
| `C4-D01` | C4 is reference reachability only | download/stream authority | show any C3 authority reused or bypassed | `CANDIDATE` |
| `C4-D02` | existing PackageId is opaque reference | second DeliveryReference id | identify a security need, not UX preference | `CANDIDATE` |
| `C4-D03` | owner is RecipientClientApplicationId | derive owner from principal/object key/caller input | test organization-vs-principal semantics | `CANDIDATE` |
| `C4-D04` | non-empty PrincipalId is actor/cursor binding | empty/derived principal | prove landed auth cannot supply it | `CANDIDATE` |
| `C4-D05` | one list route, no exact lookup | search/inbox/multiple routes | prove minimal reachability is insufficient | `CANDIDATE` |
| `C4-D06` | Finalized + non-null time is the current landed lifecycle gate; no independent retention field exists | broad package state set or invented time window | identify a legitimate landed non-Finalized/retention authority | `CANDIDATE` |
| `C4-D07` | key revocation does not hide history | duplicate C3 live-key gate | reconcile with C3 D2 | `CANDIDATE` |
| `C4-D08` | projection-only, no issuance state | reference/event table | state exact safety claim requiring durability | `CANDIDATE` |
| `C4-D09` | `(FinalizedAtUtc, PackageId)` descending | timestamp-only/offset | refute total-order/null handling | `CANDIDATE` |
| `C4-D10` | 15-minute authenticated stateless cursor | unsigned/permanent/stateful cursor | test cross-recipient and replay boundary | `CANDIDATE` |
| `C4-D11` | no durable listing audit | event subsystem | state exact compliance claim | `CANDIDATE` |
| `C4-D12` | fifth DB/SQL role+LOGIN; S3 stays five | provider credential or role reuse | independently census all four families | `CANDIDATE` |
| `C4-D13` | exact SECURITY DEFINER projection | direct table SELECT | prove ACL/owner/readiness closure | `CANDIDATE` |
| `C4-D14` | real-C2-produced package mandatory | direct-seeded canonical substitute | try to satisfy proof without C2 | `CANDIDATE` |
| `C4-D15` | PackageId handoff invokes real C3 auth again | C4 positive evidence reused | bypass mutation must RED | `CANDIDATE` |

## 6. Debt and deferred branch register

| Debt/branch | Disposition | Closure condition | Scope effect | State |
| --- | --- | --- | --- | --- |
| `C3-PACKAGE-REFERENCE-DISTRIBUTION-V1` | assigned to C4 | exact implementation, real-C2 ownership proof, independent C3 handoff and controlled closeout | C4 objective | `ASSIGNED_OPEN` |
| `C3-DELEGATED-DELIVERY-V1` | deferred | separate owning slice and authority | prohibited in C4 | `DEFERRED` |
| notification/inbox UX | deferred | separate product slice | no route/state/event | `DEFERRED` |
| recipient-key management | deferred | separate security/operations slice | C4 read-only | `DEFERRED` |
| production raw-source adapter | deferred | real Raw BIO owning slice | no Raw BIO | `DEFERRED` |
| `SHARED-DB-MIGRATION-STATE-DEBT-01` | pre-control only | repository-wide harness owner | disposable DB required | `CARRIED` |
| `LINE-ENDING-FRAGILE-COMPARISON-DEBT-01` | pre-control only | repository-wide harness owner | both-side LF normalization | `CARRIED` |
| `TIP68-HOST-STARTUP-ROOT-CAUSE-01` | observation/hardening debt | separate owner | no C4 scope expansion | `CARRIED` |
| `R2-OBS-DEBT-01` | predecessor debt | separate owner | no C4 scope expansion | `CARRIED` |
| `READINESS-OWNERSHIP-DEBT-01` | exact-census control | separate owner | proof-level rule binds C4 | `CARRIED` |

## 7. Affected-surface map template

Round 2 must fill exact paths and before hashes; no path is authorized here.

| Surface | Expected semantic delta | Explicit non-delta | Round-2 evidence |
| --- | --- | --- | --- |
| API/application | one authenticated list route/service/DTO | C3 routes unchanged | route/auth/non-disclosure proofs |
| persistence | exact read function and optional supporting index | no table/state/event/column | model/catalog diff |
| capability/readiness | fifth DB role/LOGIN and exact readiness | S3 credentials stay five | exact role/ACL/function census |
| cursor | versioned authenticated codec/config | no bearer/download authority | absolute vectors/mutations |
| predecessor compatibility | real C2 -> C4 -> C3 | no direct-seeded substitute | integration proof |
| harness | disposable DB, pre-Up roles, LF normalization | no shared-template Down | harness proofs |

## 8. Required Round-1 review coverage

Each independent reviewer must answer all questions below, not merely verify
that words are present:

| # | Required question | Reviewer A | Reviewer B |
| --- | --- | --- | --- |
| 1 | Reference reachability only, not download authority? | pending | pending |
| 2 | Existing PackageId safe as opaque reference? | pending | pending |
| 3 | Exact recipient identity and principal/application distinction sound? | pending | pending |
| 4 | Eligible C2 states closed? | pending | pending |
| 5 | Revocation visibility consistent with C3 D2? | pending | pending |
| 6 | Foreign existence/count/timing oracle closed? | pending | pending |
| 7 | Cursor binding and total order sufficient? | pending | pending |
| 8 | Projection-only/no issuance state safe? | pending | pending |
| 9 | Audit decision precise and non-overclaiming? | pending | pending |
| 10 | C2/C3 amendments actually unnecessary? | pending | pending |
| 11 | Inbox/notification excluded? | pending | pending |
| 12 | Delegated delivery excluded? | pending | pending |
| 13 | Four census families and exact-catalog rule pinned? | pending | pending |
| 14 | Real-C2 proof unavoidable/non-vacuous? | pending | pending |
| 15 | C3 debt closure conditions sufficient? | pending | pending |
| 16 | Remaining pilot gaps honest? | pending | pending |

## 9. Stale-sentinel register

| Sentinel | Meaning | Removal authority | State |
| --- | --- | --- | --- |
| `ROUND_1_SCOPE_CANDIDATE` | docs not ratified | Homeowner after clean review | active |
| `C4-R1-PENDING` | no exact reviewer result bound | append-only successor | active |
| `ASSIGNED_OPEN` | C3 debt not implemented/closed | controlled closeout | active |
| `Implementation authority: NONE` | no build permission | Homeowner | active |

No reviewed file is rewritten in place to remove these markers.

## 10. Three-round convergence control

```text
Round 1  scope/trust/product decisions
Round 2  executable dispatch
Round 3  exact-byte correction only
```

Round 2 cannot begin while a Round-1 product decision remains materially open.
After Round 3, executable uncertainty moves to code and discriminating tests
unless a genuine contradiction requires Homeowner adjudication.

## 11. Bundle and mechanical integrity register

The Round-1 bundle must contain:

```text
repo/<scope path>
repo/<ledger path>
authority/HOMEOWNER_C4_ROUND1_AUTHORITY.md
metadata/BASELINE_AND_SELF_CHECK.txt
metadata/FILES_MANIFEST.tsv
metadata/SHA256SUMS
metadata/REVIEW_REQUEST.md
```

Required checks:

- bundle sidecar matches outer SHA-256;
- internal sums match every payload file;
- zero duplicate, absolute, traversal or backslash paths;
- metadata is UTF-8 without BOM and LF-only;
- scope and ledger bytes equal repository working bytes;
- baseline/branch match and staged paths remain zero;
- unrelated dirt is neither included nor modified.

## 12. Non-authorization

This ledger and its bundle authorize no implementation, migration, provider
operation, test execution, stage, commit, push, merge, PR, deployment,
production activation, Raw BIO, package download or delivery.

Current lifecycle:

```text
Scope:                ROUND_1_SCOPE_CANDIDATE
Independent reviews: PENDING
Homeowner ratification: NOT_RECORDED
Round-2 authority:    NONE
Implementation:       NONE
```

## 13. Round-1 v0.1 review reconciliation — appended 2026-08-19

This append advances the ledger lifecycle to
`ROUND_1_CLOSURE_REVIEW_PENDING` without rewriting the reviewed v0.1 prefix.
It binds the exact v0.1 scope, ledger and review-bundle hashes, records both
independent verdicts, accepts all three verified findings without a
counter-claim, and points to the v0.2 successor candidate. Homeowner
ratification and Round-2 authority remain unset.

### 13.1 Frozen candidate and review provenance

```text
Scope path:
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/
tip_88c1_c4_authenticated_package_reference_distribution_scope_brief.md

Scope v0.1 SHA-256:
644EC9B75883FA8AC75D37261BD08A409905E5847071B9D3C6337BC1FC860136

Ledger v0.1 SHA-256:
1556DE8490118EFC9D6E027DAD27AE6D1B0855D51634891391FB901B5FB3E4FC

Round-1 review bundle SHA-256:
68051CCF910F860285D602004689E45DA34071A4E66CDC38A0DF6DB5C0CDCD33

GPT independent review artifact SHA-256:
39307D6DC1E2E8D87E33E4DC20D6BCF0AACF80471A449282D00F5EF0CB1B1ACA

CC independent review:
USER_SUPPLIED_INLINE_REVIEW_NO_SEPARATE_ARTIFACT
```

Reviewer GPT returned `FAIL — NARROW v0.2 REQUIRED`. Reviewer CC returned
`PASS — no finding`. The combined contractor adjudication accepts all three
GPT findings. CC's integrity, landed-feasibility, census and non-disclosure
checks remain valid, but its PASS did not resolve the cursor and ownership
semantic contradictions below.

### 13.2 Successor finding register

| Finding ID | Reviewer | Severity | Claim | Contractor verification and disposition | v0.2 closure anchor | State |
| --- | --- | --- | --- | --- | --- | --- |
| `C4-R1-F01` | GPT | `CONTRACT_BLOCKER` | absent first-page cursor conflicts with precedence that classifies a missing cursor as invalid | `ACCEPTED`: absence is the valid first-page form; only a cursor parameter that is present but empty/invalid returns cursor-invalid | Scope v0.2 §§6, 9, 10, 15 | `CLOSED_IN_SUCCESSOR_PENDING_REVIEW` |
| `C4-R1-F02` | GPT | `CONTRACT_BLOCKER` | optional/default page size is ambiguous on continuation even though the cursor authenticates page size | `ACCEPTED`: first page defaults to 25; continuation omission uses the cursor value; a supplied mismatch returns cursor-invalid | Scope v0.2 §§6, 9, 10, 15 | `CLOSED_IN_SUCCESSOR_PENDING_REVIEW` |
| `C4-R1-F03` | GPT | `PRODUCT_TRUST_CLARIFICATION` | closure text incorrectly suggests sibling principals cannot obtain the same application-owned reference | `ACCEPTED`: package ownership is client-application scoped; sibling principals may independently list the same PackageId but cannot share cursors; per-principal ownership is an unlanded requirement-dependent gap | Scope v0.2 §§5, 18, 20 | `CLOSED_IN_SUCCESSOR_PENDING_REVIEW` |

No counter-claim is recorded. These corrections do not add a route, durable
state, package authority, provider authority or implementation permission.

### 13.3 Successor state

```text
Predecessor sentinel C4-R1-PENDING:       SUPERSEDED_BY_THIS_APPEND
Scope v0.1 combined disposition:          FAIL — NARROW v0.2 REQUIRED
Scope v0.2:                               ROUND_1_CLOSURE_CANDIDATE
Independent closure review:              PENDING
Homeowner scope ratification:             NOT_RECORDED
Round-2 dispatch authority:               NONE
Implementation authority:                NONE
```

## 14. Round-1 closure and Round-2 dispatch authoring — appended 2026-08-19

This append preserves every predecessor byte and records the Homeowner
transition from reviewed scope to docs-only executable-dispatch authoring.

### 14.1 Round-1 closure evidence

```text
Scope v0.2 SHA-256:
F59B9DB19585AD4C30D8AD0EA98DEEF8069A5AAB28E505D58203684AA9CEBA0E

Closure bundle SHA-256:
D6E0E21AC59670FBB105DE6A32704D53CC111F0B5B21B00CD2B305C6833C5420

GPT closure report SHA-256:
58BAEF508AEAC3212C9665E8C1867365D810EB530793DD9E5B9781921083CA08

GPT verdict:
PASS — C4 SCOPE ROUND 1 CLOSED; READY FOR HOMEOWNER ROUND-2
DISPATCH-AUTHORING DECISION

CC closure review:
USER_SUPPLIED_INLINE_REVIEW_NO_SEPARATE_ARTIFACT

CC verdict:
PASS — Round 1 đóng được
```

Both reviewers independently confirmed `C4-R1-F01`, `C4-R1-F02` and
`C4-R1-F03` closed on exact Scope v0.2 bytes. No new Round-1 product/trust
finding remained. The Homeowner ratified that scope and closed Round 1 in the
Round-2 authority below.

### 14.2 Round-2 authority and candidate

```text
Homeowner Round-2 authority SHA-256:
B7A4C6AC4C9A48EAAC0547AD0457093CB581C0FD4AD134983765DAB5FF93C250

Dispatch path:
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/
tip_88c1_c4_authenticated_package_reference_distribution_build_dispatch.md

Dispatch v0.1 SHA-256:
84B979E3F3334F20B0087628FEDAA7712347839A5921BF17A7449E935F917812

Dispatch census:
34 permanent paths / 1 SQL function / 1 index / 0 tables / 0 states /
0 events / 24 proofs / 34 mutations

Cursor absolute vector:
98-byte payload / 124-byte MAC preimage / 175-character token / recompute PASS
```

Round-2 authoring changed only the new dispatch and this append-only ledger.
It did not execute or authorize implementation, migration, provider, test,
stage, commit, push, merge, PR, deployment or production work.

### 14.3 Current lifecycle successor

```text
Scope v0.2:                               RATIFIED — ROUND 1 CLOSED
Dispatch v0.1:                            ROUND_2_EXECUTABLE_CANDIDATE
Independent Round-2 reviews:              PENDING
Homeowner controlled-build ratification:  NOT_RECORDED
C3-PACKAGE-REFERENCE-DISTRIBUTION-V1:     ASSIGNED_OPEN
Implementation authority:                 NONE
```

## 17. Controlled-build successor — C4-RRI-07

`C4M17` reached the mandatory HARD STOP because C409 compared a
foreign-present and a foreign-absent observation while the seed helper bound
every package row to Client A. Although the helper created key registrations
for both clients, deleting Client B's packages deleted zero rows. The two
observations therefore described the same state and the foreign-partition
comparison was vacuous.

The count-neutral fixture correction seeds two valid Finalized packages owned
by Client B. One is newest in the seeded global order; the other is interleaved
between Client A's first and second rows. C409 now asserts, before comparing
the public responses:

```text
foreign eligible count before deletion:  > 0
foreign eligible count after deletion:   0
own page while foreign present:           non-empty, cursor present
own page while foreign absent:            non-empty, cursor present
seeded global owner order:                 B, A, B, A
```

The corrected C4M17 global-latest mutation made both observations remain valid
pages (`200`, two items, cursor present) and RED only at the named
`foreignPartition` comparison. The repository was then restored byte-exactly
to `9D8617CC0D7620A29C9EE386ED46059DE66E89BBA10D565DC1F04D719A1FBEA6`,
and canonical C409 passed.

This is the third occurrence of the same control-fixture failure class after
RRI-02's same-value identity fixture and RRI-05's two-dimension negative
control. The one-dimension rule and the two-state/non-vacuity rule bind the
remaining mutation sweep.

C4M18 was pre-checked before execution. Its live Active-key join mutation was
placed in the SQL visibility function, where C410 can observe it directly.
C410 RED at `recipient_key_registrations`, the migration was restored
byte-exactly to
`A58FDD7438DEDA4DCA3F4D552E3AFC9E02378B3FBD9EE48067C0827F974ED3A0`,
and the canonical proof passed.

No production/API/schema/migration/cursor semantics changed permanently. No
proof ID, mutation ID or permanent path was added. The affected C4 subset is
green: Integration 18/18, Unit 2/2, Architecture 4/4. The full unfiltered suite
has not run.

## 18. Controlled-build HARD STOP — C4M19 false-green

After C4M17 and C4M18 closed, the sweep stopped at C4M19. Replacing C411's real
C2 Prepare-to-Finalize execution with one directly seeded Finalized package
still produced a passing C4 HTTP list and real C3 Create handoff. C411 therefore
demonstrates the canonical predecessor path but does not yet independently
discriminate a direct-seed substitute.

The scratch proof was restored byte-exactly to
`0F544DCCC5DCC6965389CED2BED8302EFF85C8529574AB3BA53619DE74E6CACE`.
All four production restore anchors remain canonical and staged paths remain
zero. C4M20–C4M34 and the full unfiltered suite were not run.

Required successor: a count-neutral C411 provenance discriminator which stays
active under direct-seed substitution and REDs specifically because durable
evidence from the real C2 path is absent. No production/schema/contract change
is proposed.

## 19. Controlled-build successor — C4-RRI-08

C411's canonical real C1 -> C2 -> C4 -> C3 chain asserted only outcomes that a
directly seeded Finalized row could also satisfy. The count-neutral correction
adds two named durable provenance anchors before C4 listing:

1. read the exact object through the live C2 store and match persisted length,
   SHA-256 over all package bytes and SHA-256 over the parsed envelope prefix;
2. load the C1 assembly identity for the package lineage, recompute
   `PackageEqualityFingerprint` from those C1 inputs and match the persisted
   fingerprint.

C4M19 direct-seed substitution now REDs in a single composite report at the
named object-exists/length/ciphertext/envelope and C1-equality bites. Canonical
C411 and its restore control pass. The corrected integration-test SHA is:

```text
1896ABDB76E548278EA4DFE17F11ABF032D86D0A034322E4EF691C6D4EBF8CBE
```

The affected C4 subset remains green: Integration 18/18, Unit 2/2 and
Architecture 4/4. Production/API/schema/migration/cursor bytes did not change.

Before any further mutation, C4M20–C4M34 were audited at their actual
observation layers under the one-dimension and two-state rules. Result:

```text
WILL RED:    1
CANNOT RED:  9
UNCERTAIN:   5
```

No C4M20–C4M34 mutation was run. The nine CANNOT RED and five UNCERTAIN pairs
require one batched count-neutral proof correction authority. C4 implementation
remains open and the full unfiltered suite has not run.

## 15. Round-2 v0.1 review reconciliation — appended 2026-08-19

This append preserves the complete reviewed Round-1/Round-2 prefix and records
the combined adjudication of both independent Dispatch v0.1 reviews. The
findings require one narrow Round-3 exact-byte successor; Scope v0.2 and its
product/trust decisions remain closed.

### 15.1 Frozen v0.1 review provenance

```text
Dispatch v0.1 SHA-256:
84B979E3F3334F20B0087628FEDAA7712347839A5921BF17A7449E935F917812

Ledger at v0.1 bundle SHA-256:
AC6BB19CEE314EF7FB690B30D47C9B61097445B67E183E93C515DFE725BF0C61

Round-2 review bundle SHA-256:
BF86604D621968C71B291568E666FDE7B39C17C08D575F777F980F22E2AEA29B

GPT independent review SHA-256:
4570EEFE971890D7B214D0D131808919D3B88654C7123C41F15DE600CE0DF374

GPT verdict:
FAIL — NOT READY FOR HOMEOWNER C4 CONTROLLED-BUILD RATIFICATION

CC independent review:
USER_SUPPLIED_INLINE_REVIEW_NO_SEPARATE_ARTIFACT

CC verdict:
HOLD — NEEDS NARROW v0.2
```

Both reviewers independently recomputed the absolute cursor vector and
confirmed it byte-exact. Mechanical integrity, SQL ordering, 34-path v0.1
census and the real-predecessor proof direction also passed. Those facts are
carried; they do not override the findings below.

### 15.2 Combined finding register and contractor adjudication

| Finding | Source | Severity | Adjudication | Exact v0.2 closure | State |
| --- | --- | --- | --- | --- | --- |
| `C4-R2-F01` | GPT | `HIGH` | `ACCEPTED`: v0.1 omitted the topology gate from public precedence | dependency-free branch/token classification -> Disabled/Invalid unavailable with zero key/DB access; §§3.1, 4.4, 9; C415/C420/C4M28 | `CLOSED_IN_V0_2_PENDING_REVIEW` |
| `C4-R2-F02` | GPT | `HIGH` | `ACCEPTED`: AcceptedCursorKeys lacked an executable IConfiguration shape and case rule | exact indexed contiguous tree, environment form, effective-provider semantics, lowercase ordinal ids, duplicate/unknown-child matrix; §§3.1, 5.1; C415/C4M28 | `CLOSED_IN_V0_2_PENDING_REVIEW` |
| `C4-R2-F03` | GPT | `HIGH` | `ACCEPTED`: concrete PrincipalId hops and auth-scope path census were absent | exact authenticator/validator/store hashes and copy chain; isolated LocalDev C4 key; allowlist 34 -> 35; §2.2, §11, C401 | `CLOSED_IN_V0_2_PENDING_REVIEW` |
| `C4-R2-CC-F01` | CC | `MAJOR` | `ACCEPTED`, same hidden-path class as GPT F03 | adds only `LocalDevApiKeyStore.cs`, exact isolated key identity/scope; no broadening of an existing key | `CLOSED_IN_V0_2_PENDING_REVIEW` |
| `C4-R2-CC-F02` | CC | `MINOR` | `ACCEPTED`: duplicate continuation pageSize was undefined | any continuation cardinality other than zero/one as specified is cursor-invalid; §§4.1, 4.4; C404/C4M06 | `CLOSED_IN_V0_2_PENDING_REVIEW` |
| `C4-R2-CC-F03` | CC | `MINOR` | `ACCEPTED`: branch classification was not explicit before branch-specific pageSize errors | raw cursor presence/cardinality establishes first-page versus continuation before pageSize classification | `CLOSED_IN_V0_2_PENDING_REVIEW` |
| `C4-R2-CC-F04` | CC | `PRECHECK` | `VERIFIED — NO NEW PATH`: real C3 can be composed from the C4 integration owner using public ports/application service plus internals visible to the integration assembly | §10 forbids modifying/calling private C3 test helpers; predecessor C3 test remains unchanged evidence input | `CLOSED_NO_PATH_EXPANSION` |
| `C4-R2-CC-F05` | CC | `MINOR` | `ACCEPTED`: two inherited sentinels were missing from the dispatch carry-forward list | `READINESS-OWNERSHIP-DEBT-01` and `R2-OBS-DEBT-01` restored in §16 | `CLOSED_IN_V0_2_PENDING_REVIEW` |

The auth census additionally established that the landed managed provisioning
policy does not yet authorize the new C4 scope. C4 does not own recipient/API-
key management, so v0.2 records `C4-AUTH-SCOPE-PROVISIONING-DEBT-01` as an
activation blocker instead of silently adding a second auth-policy path.

### 15.3 Round-3 successor candidate

```text
Dispatch v0.2 SHA-256:
A3D6BE8F43699627570120A5BEC4108A093632D90B89DD4510B167F0A7671943

Dispatch census:
35 permanent paths / 1 SQL function / 1 index / 0 tables / 0 states /
0 events / 24 proofs / 34 mutations

Scope v0.2:                               RATIFIED / UNCHANGED
Dispatch v0.1 combined disposition:       FAIL — NARROW SUCCESSOR REQUIRED
Dispatch v0.2:                            ROUND_3_EXACT_BYTE_CLOSURE_CANDIDATE
Independent Round-3 closure review:       PENDING
Homeowner controlled-build ratification:  NOT_RECORDED
Implementation authority:                 NONE
```

## 16. C4-R3-G01 provenance closure — appended 2026-08-19

The Round-3 independent review found no technical defect and returned
`TECHNICAL PASS / GOVERNANCE HOLD — REVIEW_CHAIN_INCOMPLETE` solely because the
exact CC Round-2 review artifact was not carried in the review bundle.

To preserve the ledger at SHA-256
`CD69EF560FEB5C0B374C5934D542B328D76B17888B69661BF663DBBB64F8FC82`
as an exact byte prefix, the historical
`USER_SUPPLIED_INLINE_REVIEW_NO_SEPARATE_ARTIFACT` line above is not rewritten.
This successor binding semantically replaces that sentinel with:

```text
Artifact path:
reviews/CC_C4_DISPATCH_V0_1_ROUND2_REVIEW.md

Artifact SHA-256:
B991AAA0AAD0C3B6D8B444697272CBA5850D485145D1324C2E736263174E2B6A

Artifact authority:
Homeowner-supplied verbatim transcript from the CC Round-2 review session
```

The `C4-R2-CC-F01` through `C4-R2-CC-F05` rows, severities and dispositions in
§15.2 remain byte-identical. The artifact confirms they are the complete CC
Round-2 review findings; no omitted CC finding remains.

### 16.1 Closure provenance

```text
Round-3 independent review artifact SHA-256:
2212569C890B747A986F9B7EBEB685E8D5BA5089FDB2CC7B7CA31419CF34C086

Round-3 technical verdict:
PASS — C4 DISPATCH v0.2 EXECUTABLE CONTRACT IS READY FOR CONTROLLED BUILD

Round-3 governance finding:
C4-R3-G01 REVIEW_CHAIN_INCOMPLETE

Homeowner provenance-closure artifact SHA-256:
DB09E111F29B2DFC42806188F79577D6CFAC35158279B1300771FBABFAE500C8

Homeowner disposition:
C4-R3-G01 ACCEPTED AND CLOSED — GOVERNANCE ONLY
```

Dispatch v0.2 remains byte-identical at:

```text
A3D6BE8F43699627570120A5BEC4108A093632D90B89DD4510B167F0A7671943
```

No Round 4 or additional technical review is opened. The combined disposition
is now:

```text
PASS — READY FOR HOMEOWNER C4 CONTROLLED-BUILD RATIFICATION
```

### 16.2 Standing review-artifact rule

Beginning with TIP-88C1-C5, every review bundle must carry both reviewers'
exact artifacts under `reviews/`. An inline-only reviewer record is itself
`REVIEW_CHAIN_INCOMPLETE` at the next round.

### 16.3 Current lifecycle

```text
Scope v0.2:                               RATIFIED / ROUND 1 CLOSED
Dispatch v0.2:                            TECHNICAL PASS / BYTE-IDENTICAL
C4-R3-G01:                                CLOSED BY HOMEOWNER PROVENANCE
Combined independent disposition:         PASS — READY FOR RATIFICATION
Homeowner controlled-build ratification:  NOT_RECORDED
Implementation authority:                 NONE
```

## 20. C4-RRI-09 proof-observability correction — Tier 1 appended 2026-08-21

Homeowner authority binds the RRI-08 audit bundle
`859CB106AA633D0D021059D594AC6D502C3BEF13EE80CD60E2F0B3C097FB72F0`
and requires three separately acknowledged tiers. No dispatch obligation is
amended.

Tier 1 has completed count-neutrally:

| Owner / mutation | Canonical | Discriminator |
|---|---:|---|
| C411 / C4M20 | PASS | missing C3 scope creates a second delivery only after the exact C3 scope comparator is removed |
| C421 / C4M29 | PASS | zeroization, post-dispose access and redaction each independently RED |
| C412 / C4M30 | PASS | neutral-name provider dependency RED at resolved dependency type; first false-green retained as excluded history |
| C412 / C4M31 | PASS | independent INSERT, UPDATE field change and DELETE each change the stable persistence fingerprint and RED |
| C402 / C4M32 | PASS | extra mapped route, extra DTO property and extra public method each independently RED |
| C420 / C4M28 arbitrary-config branch | PASS | unaccepted key config lookup RED at `C420-ARBITRARY-CONFIG-LOOKUP` |

Affected proof validation is `20 Integration + 2 Unit + 2 Architecture =
24/24 PASS`. Proof ownership is exact: 24 declarations, 24 distinct C401-C424
IDs. Production restore anchors are byte-identical, staged paths are zero and
`git diff --check` passes.

```text
Tier 1:                                  CLOSED_PENDING_HOMEOWNER_ACKNOWLEDGEMENT
Tier 2:                                  NOT STARTED
Tier 3:                                  NOT STARTED
C4M20-C4M34 final ordered sweep:         NOT STARTED
Complete unfiltered Release suite:       NOT RUN
Implementation closeout:                 OPEN
```

## 21. C4-RRI-09 Tier-1 closure and Tier-2 execution — appended 2026-08-21

The Homeowner successor packet accepted Tier 1 with one closing condition:
C412's IL token resolver had to fail closed. The proof now records every
unresolved token with declaring type/member/IL offset and asserts the collection
empty. Canonical PASS, C4M30 dependency RED, forced unresolved-token RED at the
named bite and restored canonical PASS are preserved as machine-readable TRX.

Tier 2 then completed under the released authority:

| Owner | Mutation evidence | Disposition |
|---|---|---|
| C415 | C4M21 / C4M23 / C4M25 | independently RED against disposable real-validator function, table-privilege and index postures |
| C418 | C4M24 / C4M33 | independently RED at unexpected-sibling and role-alias exact-set controls across four census families |
| C415 + C416 | C4M34 | 64-byte identifier and exact-path tripwire drift independently RED |
| C420 | six C4M28 branches | case alias, duplicate, non-contiguous, unknown child and both key/material directions independently RED |

Final restored affected proof set is `5/5 PASS`; all canonical production
restore SHAs match; staged paths are zero and `git diff --check` passes.

```text
Tier 1:                                  CLOSED
Tier 2:                                  COMPLETE_PENDING_HOMEOWNER_REVIEW
Tier 3:                                  NOT STARTED
C4M20-C4M34 final ordered sweep:         NOT STARTED
Complete unfiltered Release suite:       NOT RUN
Implementation closeout:                 OPEN
```

## 22. C4-RRI-09 Tier-2 residual and Tier-3 execution — appended 2026-08-21

The role-sibling prerequisite in the initial Tier-3 packet was unsatisfiable
with disposable-database isolation because PostgreSQL roles are cluster-scoped.
The Homeowner accepted the STOP and amended Part 1 to use real database-local
function and index siblings without creating a role.

| Owner | Mutation evidence | Disposition |
|---|---|---|
| C418 | C4M24 | canonical real function/index siblings excluded; open-ended shared ownership absorbs them and RED at the named catalog-sibling bite; restored PASS |
| C417 | C4M26 | disposable apply/Down/reapply and shared-state preservation PASS; removed cleanup RED at independent post-dispose absence; emergency cleanup and restored PASS |
| C416 | C4M27-B2 | one-sided normalization RED at the two-sided-normalization bite; restored PASS |
| C416 | C4M27-B3 | semantic-hiding normalization RED independently at the semantic-difference bite; restored PASS |

Recorded boundaries:

```text
C4-SYNTHETIC-SIBLING-CONTROL-NONCLAIM-01
C4-ROLE-SIBLING-SURFACE-NONCLAIM-01
SHARED-DB-MIGRATION-STATE-DEBT-01:          OPEN
LINE-ENDING-FRAGILE-COMPARISON-DEBT-01:     OPEN
```

Final restored affected validation is C416/C417/C418 `3/3 PASS`; production
restore hashes match; staged paths are zero and `git diff --check` passes.

```text
Tier 1:                                  CLOSED
Tier 2:                                  CLOSED
Tier 3:                                  COMPLETE_PENDING_HOMEOWNER_REVIEW
C4M20-C4M34 final ordered sweep:         NOT STARTED
Complete unfiltered Release suite:       NOT RUN
Implementation closeout:                 OPEN
```
## 23. C4 final full-suite correction FS-03 — appended 2026-08-22

The first authorized complete unfiltered Release suite produced
`1154 passed / 4 failed / 1 skipped`. Homeowner accepted the STOP/RRI report
`C4_FINAL_FULL_SUITE_RRI_20260822.md` with SHA-256
`F4A8B0BFEED0172C361628AE66C80A855192FBFA9DCEB4678B0B89BB532B077A`.
The failures were two independent test-only predecessor-harness defects; no C4
production defect was observed.

The effective allowlist is amended append-only from 35 to exactly **36 paths**
by adding:

```text
tests/TagEkyc.IntegrationTests/Tip88C1C3RecipientPackageDeliveryTests.cs
```

Correction A adds the missing
`tagekyc_raw_export_package_reference_login` prerequisite to the isolated E3
PostgreSQL role bootstrap before migration Up. The E3 absolute ModelSnapshot
tripwire remains unchanged. Correction B replaces C325's hard-coded C3 latest-
migration expectation with the current EF migration manifest's dynamic last
identifier. C325 uses `PostgresPersistenceFixture`, whose disposable-database
bootstrap already includes the C4 package-reference LOGIN role; no second role-
list edit is required in the newly added path.

```text
Dispatch bytes / SHA:                      UNCHANGED / A3D6BE8F...1943
Effective permanent-path allowlist:        36
C4M01-C4M34 ordered mutation sweep:         CLOSED / NOT RE-RUN
Replacement complete Release suite:        AUTHORIZED AFTER AFFECTED GATES
Stage / commit / push authority:            NONE
```
## 24. FS-03 replacement suite and implementation closeout — appended 2026-08-22

FS-03 affected validation passed: the exact four tests which failed the first
suite plus all C401-C424 owners completed as Integration `24/24`, Unit `2/2`
and Architecture `2/2`. The corrected E3 and C325 SHAs are respectively
`225136B58791D8D6D8B06F2ACF2CEF91C7A226A18496AE2BBBB94E0FCC971AF6`
and `81058DE8218827A284F178E1C498883F634731E8A56A181A2D590E3E301390C7`.

Exactly one replacement complete unfiltered Release suite then completed on
the frozen corrected bytes:

```text
Contract:       13 / 0 / 0
Architecture:  138 / 0 / 0
Unit:          197 / 0 / 0
Integration:   810 / 0 / 1
Aggregate:     1158 passed / 0 failed / 1 skipped / 1159 total
```

The only skip is the canonical manual golden-vector generator. All 36 frozen
paths were byte-identical before versus after the suite. Production restore
anchors remained exact, staged paths were zero, `git diff --check` passed, and
Docker returned to the same `10 total / 7 dangling` preserved SignFlow volume
set with no TagEkyc test residue.

```text
Effective permanent-path allowlist:        36
C401-C424:                                  24/24 PASS
C4M01-C4M34:                                34/34 CLOSED
Complete unfiltered Release suite:         GREEN
Implementation state:                      CLOSED
Review state:                              INDEPENDENT_CLOSEOUT_REVIEW_PENDING
Commit authority:                          NONE
```

## 25. Closeout R1 rejection and CO-02 readiness correction — appended 2026-08-23

The FS-03 R1 closeout bundle
`11FFB8A2A0A4B250C03F9907773386C96F026E4A963157A04584FBE0E85497E5`
is retained as `REJECTED_FOR_COMMIT_HISTORICAL_CLOSEOUT_EVIDENCE`.
Independent semantic review found two HIGH production-readiness defects:

| Finding | Accepted defect | Corrected disposition |
|---|---|---|
| `C4-CR-F01` / H1 | mandatory function `EXECUTE` absence passed a one-way ACL subset check | exact positive non-owner ACL entry plus existing no-extra guard; C415 disposable revoke partition; C4M21 RED |
| `C4-CR-F02` / H2 | outbound C4-capability membership into a foreign role was outside the membership census | both-direction incident-edge census in `RoleSql` and `CatalogSql`; C414 self-cleaning cluster probe; C4M23 RED |

CO-02 supersedes CO-01. CO-01's engineering acceptance is withdrawn; its four
documentation requirements remain carried into the successor closeout.

Canonical corrected hashes:

```text
RecipientPackageReferenceReadinessValidator.cs
C95EC5EA7C055E2A77D7720294139DAFDFFBD5FC60CD570B83CECB902A785C99

Tip88C1C4RecipientPackageReferenceTests.cs
ABE33DA2494D003104CC1BD4D3C9FA368D4C2B19E815929B66A47FE9FBB54805

Tip88C1C4RecipientPackageReferenceArchTests.cs
417DBB8E8453C4188EE5FE28CB2A43289BDF9C6D70012ADA617EF9417C5330E9
```

Affected validation passed C401-C424 `24/24`; C4M21 and C4M23 independently
RED at their new named bites, restored byte-exactly and passed their canonical
owners. Other mutations were not re-run because neither their production
comparators nor owner discriminators changed.

Exactly one replacement complete unfiltered Release suite passed:

```text
Contract       13 / 0 / 0
Architecture  138 / 0 / 0
Unit          197 / 0 / 0
Integration   810 / 0 / 1
Aggregate    1158 passed / 0 failed / 1 skipped / 1159 total
```

## 26. CO-01 documentation carry-forward and remaining review-chain gate — appended 2026-08-23

The as-built now ends with a terminal current-state successor, contains the
complete nine-entry debt/non-claim register, records RRI-06 and CO-02 plainly,
and adds `C4-PROOF-MIRRORS-IMPLEMENTATION-LESSON-01`.

The successor bundle's mutation ledger must expose, for every C4M01-C4M34 row,
the mutation edit, owning proof, exact named bite, RED evidence and restore SHA.

`REVIEW_CHAIN_INCOMPLETE` remains the only packaging blocker. CO-02 requires
both reviewers' exact artifacts for every round. The missing CC artifacts must
be supplied verbatim by the Homeowner; the builder is explicitly prohibited
from reconstructing them from ledger summaries. No new closeout bundle may be
represented as complete until those artifacts and their SHA-256 values are
present under `reviews/`.

```text
Implementation state:                       IMPLEMENTATION_CLOSED
Technical revalidation:                     GREEN
Effective permanent allowlist:              36
Review-chain state:                         BLOCKED_PENDING_HOMEOWNER_VERBATIM_CC_ARTIFACTS
Independent closeout review:                NOT YET REQUESTABLE
Commit authority:                           NONE
```

## 27. CO-03 exact-graph proof correction — appended 2026-08-23

Homeowner authority CO-03 accepted the remaining canonical C414 proof gap.
The production readiness validator was not changed and remains at SHA-256
`C95EC5EA7C055E2A77D7720294139DAFDFFBD5FC60CD570B83CECB902A785C99`.

C414 now exact-compares the only incident membership edge as
`tagekyc_raw_export_package_reference_login ->
tagekyc_raw_export_package_reference` with `admin=false`, `inherit=true` and
`set=false`. The proof retains its two-way incident-edge census, exact function
ACL and zero-table-privilege assertions. Corrected integration proof SHA-256:
`1A92ED6E4A5CCB062F325F6DAB7CD29DEE1E8F8A607739E1370EA5E389BC799B`.

| Owner / mutation | Canonical | Exact named discriminator | Restore |
|---|---:|---|---|
| C414 + C415 | 2/2 PASS | exact graph, ACL and topology positive control | validator `C95EC5EA...5C99`; C414/C415 2/2 PASS |
| C415 / C4M21 | RED | `C415-c4_missing_function_execute-READINESS-REJECTION` (`actual=<none>`) | byte-exact; C414/C415 2/2 PASS |
| C414 / C4M23 | RED | `C414-OUTBOUND-MEMBERSHIP-READINESS-REJECTION` (`actual=<none>`) | byte-exact; C414/C415 2/2 PASS |

Affected owners passed Integration `20/20`, Unit `2/2` and Architecture
`2/2`. Release build passed with zero warnings and zero errors. No probe role,
membership edge, test database/container or anonymous Docker volume remained;
staged paths were zero and `git diff --check` passed.

## 28. CO-03 final exact-byte suite and packaging gate — appended 2026-08-23

Exactly one replacement complete unfiltered Release suite ran after the C414
proof correction. The previous CO-02 suite is retained as historical evidence
and is not represented as final exact-byte closeout evidence.

```text
Contract       13 / 0 / 0
Architecture  138 / 0 / 0
Unit          197 / 0 / 0
Integration   810 / 0 / 1
Aggregate    1158 passed / 0 failed / 1 skipped / 1159 total
```

Final TRX SHA-256 values are:

```text
contract       E2D8725876F6383D23EEFA52FD8F2249835024A4D1FF1BB4F49EDA750EA40D3F
architecture   67CC27A0F6E8985DED27A1DE16A006EC63C58BF104ECFC6CAF72D6218B9194E8
unit           5D85C1430172CE38F69F7AC3E508A642D8DE3E9D1ABB97382F030E9449BFD168
integration    051A3B93AD031AE8D8CF1475A37242DF5838B307DEC844FC6A8AC8AB05D2B09C
```

Pre/post executable hashes matched. Docker returned to `10 total / 7 dangling`
with zero TagEkyc test containers. Final package manifests are required to be
LF-only, PowerShell-verifiable and POSIX `sha256sum -c`-verifiable.

```text
Implementation state:                       IMPLEMENTATION_CLOSED
Technical closeout:                         GREEN
Independent closeout review:                PENDING
F4 review chain:                            OPEN
F4 closure route:                           exact historical CC artifacts OR explicit Homeowner provenance disposition
Commit authority:                           NONE
```

## 29. CC finding register — Round 3 onward — supplied by the reviewer 2026-08-23

This is a FINDING REGISTER authored by the CC reviewer. It is NOT a transcript
of the reviews. It exists so the inline-only rounds have an auditable record to
ratify. No unrecorded finding is represented as closed.

| ID | Round | Severity | Finding | Disposition |
| --- | --- | --- | --- | --- |
| `C4-R3-CC-N01` | Dispatch v0.2 Round 3 | NOTE | Verdict PASS, no findings. `C4-AUTH-SCOPE-PROVISIONING-DEBT-01` was minted by the builder at dispatch stage and needs Homeowner ratification; §2.2 pins two files by basename only | NOTED / ratified in build authority |
| `C4-G01-CC-A01` | C4-R3-G01 | ATTESTATION | GPT's `REVIEW_CHAIN_INCOMPLETE` upheld; the §15.2 register of `C4-R2-CC-F01..F05` verified line-by-line against the original review — severities and verdict faithful, exactly five findings, no sixth | CLOSED by provenance amendment |
| `C4-RRI01-CC-F01` | RRI-01 | MAJOR | Proposed remedy placed the C4M02 discriminator in C419, but the authorization obligation belongs to C401; C401 would keep naming an obligation it cannot bite for | CLOSED in RRI-02 |
| `C4-RRI01-CC-F02` | RRI-01 | MAJOR | Unreported second gap: no test exercised the concrete authenticator→validator→store chain; C401 read the store record directly | CLOSED in RRI-02 |
| `C4-RRI02-CC-F01` | RRI-02 | BLOCKER | The fix required deviating from a ratified §2.2 pin (`PrincipalId = BusinessClientId`), so it needed a dispatch amendment recorded append-only, not merely edit permission | CLOSED by amendment |
| `C4-RRI02-CC-N01` | RRI-02 | NON-CLAIM | `C4-PERSISTED-PRINCIPAL-CHAIN-NONCLAIM-01` — only the LocalDev half of the §2.2 chain is proven | OPEN / recorded |
| `C4-RRI03-CC-F01` | RRI-03 | MAJOR | C403 was a pure identity assertion — it asserted the fake gateway received the record the test built, never invoking production `Classify` | CLOSED |
| `C4-RRI03-CC-F02` | RRI-03 | MAJOR | Three execution conditions were missing: delete the superseded C403 declaration; `Classify` is `internal` not `private` so the prohibition needs enforcing by construction; the inline C411 host must be extractable | CLOSED |
| `C4-RRI04-CC-F01` | RRI-04 | MAJOR | C404 carried the identical defect, so C4M04/M05/M06 would all be false-green | CLOSED |
| `C4-RRI04-CC-F02` | RRI-04 | MAJOR | `NextCursor` was never minted and replayed anywhere, leaving C4M08/C4M09 undiscriminated at the enforcement layer | CLOSED |
| `C4-RRI05-CC-F01` | RRI-05 | MAJOR | The B1 negative control differed from the cursor owner in BOTH identity dimensions and could isolate neither comparator. Cause: an error in the reviewer's own RRI-04 packet | CLOSED by adding B2 = Client B + P1 |
| `C4-RRI05-CC-F02` | RRI-05 | BLOCKER | The proposed RRI-05 packet forbade the very actor-construction A1/A2/B1 already used, and would have forced an unnecessary credential-source edit | CLOSED by clarification |
| `C4-RRI06-CC-F01` | RRI-06 | HIGH | Unreported symmetric production defect on the mint side: a cursor could be signed with an old key while declaring the active key identity, yielding a permanently unverifiable cursor | CLOSED |
| `C4-RRI06-CC-F02` | RRI-06 | CORRECTION | The `?? []` path was NOT a forgery vector — the 32-byte length guard rejects it. Severity restated accurately | RECORDED |
| `C4-RRI06-CC-F03` | RRI-06 | MAJOR | The canonical classifier SHA became obsolete; every later restore had to re-pin or would silently reinstate the defect | CLOSED |
| `C4-RRI07-CC-F01` | RRI-07 | MAJOR | Seeding a foreign row was insufficient: without deliberate placement (globally newest and interleaved) a leak stays invisible in the observed window | CLOSED |
| `C4-RRI07-CC-F02` | RRI-07 | MAJOR | Non-vacuity preconditions were absent; the two contrasted states were never asserted to differ. TWO-STATE RULE established | CLOSED |
| `C4-RRI08-CC-F01` | RRI-08 | VERIFICATION | C4M19 RED confirmed at all five named provenance bites; 5 of 15 audit rows independently re-verified on code and all accurate | CLOSED |
| `C4-RRI09-T1-CC-F01` | RRI-09 Tier 1 | MAJOR | The IL token resolver swallowed `ArgumentException`, making an absence proof rest on a fail-open detector | CLOSED |
| `C4-RRI09-T2-CC-F01` | RRI-09 Tier 2 | MAJOR | The open-ended-construct ban was still literal `" LIKE "` only, leaving `SIMILAR TO`, regex and prefix helpers unbanned | CLOSED in Tier 3 |
| `C4-RRI09-T2-CC-F02` | RRI-09 Tier 2 | NON-CLAIM | `C4-SYNTHETIC-SIBLING-CONTROL-NONCLAIM-01` — the append-style sibling controls prove the comparator, not the discovery chain | OPEN / recorded |
| `C4-RRI09-T2-CC-A01` | RRI-09 Tier 2 | ADJUDICATION | GPT `T2-F01`/`T2-F02` rejected as wrong-tier — C417/C4M26 and C416/C4M27 were Tier 3 and correctly not executed. GPT `T2-F03` upheld and strengthened: the C4M24 RED was on the wrong surface AND the control was synthetic | GPT retracted F01/F02 |
| `C4-A1-CC-F01` | Amendment A1 | BLOCKER | The reviewer's own P1.1/P1.2 were mutually unsatisfiable — PostgreSQL roles are cluster-scoped, so a role sibling cannot be isolated in a disposable database | CLOSED by A1 |
| `C4-A1-CC-N01` | Amendment A1 | NON-CLAIM | `C4-ROLE-SIBLING-SURFACE-NONCLAIM-01` — the `pg_roles` surface is not exercised with a real sibling | OPEN / recorded |
| `C4-FS01-CC-F01` | FS-01 | MAJOR | The unnamed-gating-assertion defect was not confined to the C415 helper; C413 — owner of the very next mutation — and several others shared it, so a helper-only fix would stop again immediately | CLOSED by naming pass |
| `C4-FS02-CC-F01` | FS-02 | MAJOR | Targeting constraints verified on code: `raw_export_recipient_package_delivery_events` carries an append-only trigger and `raw_export_recipient_key_registrations` has two `Restrict` inbound FKs, either of which would produce another wrong-reason RED | CLOSED |
| `C4-FS03-CC-F01` | FS-03 | MAJOR | Swept for the same class and found exactly one hard-coded latest-migration site; required the repair be DYNAMIC, since re-hardcoding to C4 would merely relocate the failure to C5 | CLOSED |
| `C4-CO01-CC-F01` | CO-01 | MAJOR | The as-built ended on a superseded disposition stating the sweep and suite had not run and the census was 35 paths | CLOSED in CO-03 |
| `C4-CO01-CC-F02` | CO-01 | MAJOR | The mutation ledger lacked Owner, MutationEdit, NamedBite and RestoreSha columns | OPEN — 2 of 34 rows only |
| `C4-CO01-CC-F03` | CO-01 | MAJOR | The debt/non-claim register carried 4 of 9 entries, omitting the activation blocker `C4-AUTH-SCOPE-PROVISIONING-DEBT-01` | CLOSED in CO-03 |
| `C4-CO01-CC-F04` | CO-01 | MAJOR | The review chain was incomplete under the §16.2 standing rule | OPEN — this register addresses it |
| `C4-CO02-CC-R01` | CO-02 | RETRACTION | CO-01's statement that the engineering was accepted is WITHDRAWN. Both HIGHs independently verified on code: the function ACL was a subset check violating §7.2's two-way requirement, and the `pg_auth_members` census covered only inbound edges | CLOSED in CO-02/CO-03 |
| `C4-CO02-CC-F01` | CO-02 | MAJOR | `pg_auth_members` is cluster-scoped, so the outbound-edge negative partition cannot be isolated by a disposable database; the probe role must be named outside the census namespace and its absence asserted | CLOSED |
| `C4-CO02-CC-F02` | CO-02 verification | MAJOR | The CO-02 bundle's `SHA256SUMS` was CRLF, so POSIX `sha256sum -c` failed all 26 lines; content verified intact at 26/26 with CR stripped | CLOSED in CO-03 |
| `C4-CO03-CC-F01` | CO-03 | MAJOR | F2 only partially done — the 7-column schema covers 2 rows while the 34-row ledger keeps the old 4-column schema | OPEN — CO-04 |
| `C4-CO03-CC-F02` | CO-03 | MAJOR | F4 regression — the three review artifacts carried in the FS03 bundle are absent from CO-03 | OPEN — CO-04 |
| `C4-CO04-CC-F01` | CO-04 | BLOCKER | The ratification precondition did not exist: the ledger held no CC finding IDs beyond `C4-R2-CC-F01..F05`, so item 4 would have ratified an empty set | CLOSED by this register |

### 29.1 Reviewer attestation

I authored every CC review from the Dispatch v0.2 Round-3 review through CO-04.
This register records every finding, adjudication, retraction and non-claim I
raised in those rounds. It is complete to the best of my knowledge and no
finding of mine outside this register remains unrepresented.

It is a register, not a transcript. It does not reproduce the reasoning,
on-code evidence or verdict text of the original reviews, and it does not
substitute for the verbatim artifacts required from TIP-88C1-C5 onward under the
§16.2 standing rule.

Ratification of this register is a Homeowner act and is not self-granted.

## 30. CO-04 governance successor — F2/F4 closure candidate

The reviewer-authored register above was supplied by the Homeowner as exact
artifact `reviews/CC_C4_FINDING_REGISTER_ROUND3_ONWARD.md`, SHA-256
`0FAF527EDA253A4E37C8227409578C19D1DDA7AA23F67136D3E643FF69482273`.
Independent builder verification found 37 rows, 37 unique IDs, zero blank
required fields and no disposition that overstates closure. The Homeowner's
conditional ratification therefore became effective. It is authoritative as a
finding register only, not as a transcript; no unrecorded finding is represented
as closed.

F2 is closed by the single LF-only 34-row ledger
`metadata/MUTATION_PROVENANCE.tsv`, SHA-256
`BE9D2D97E2E865470962E4864C461CBCEB0DEF350C0FA1E817211BDDAE7E4CB1`.
It replaces the retired split FS03/CO03 presentations and contains exactly the
seven required columns with non-empty Owner and NamedBite cells for every
C4M01-C4M34 row.

The review-chain artifacts carried by the successor are:

| Artifact | SHA-256 | Disposition |
| --- | --- | --- |
| `reviews/CC_C4_DISPATCH_V0_1_ROUND2_REVIEW.md` | `B991AAA0AAD0C3B6D8B444697272CBA5850D485145D1324C2E736263174E2B6A` | exact FS03 bytes |
| `reviews/GPT_C4_DISPATCH_V0_1_ROUND2_REVIEW.md` | `4570EEFE971890D7B214D0D131808919D3B88654C7123C41F15DE600CE0DF374` | exact FS03 bytes |
| `reviews/GPT_C4_DISPATCH_V0_2_ROUND3_REVIEW.md` | `2212569C890B747A986F9B7EBEB685E8D5BA5089FDB2CC7B7CA31419CF34C086` | exact FS03 bytes |
| `reviews/CC_C4_CO03_INDEPENDENT_REVIEW.md` | `28F0650DCB059EE6F711C5B2E94DC2C012875B945FC7BE558986111690E32461` | verbatim current CO-03 review |
| `reviews/GPT_C4_CO03_INDEPENDENT_REVIEW.md` | `1C5A584B087AD2227A2101043779658D9CC02C3B1D7DE4755ACA33E54DAABCF3` | verbatim current CO-03 review |

The §16.2 standing rule remains unchanged for C5 onward: both exact reviewer
artifacts are mandatory. This C4 ratified-register closure is not precedent for
an inline-only C5 review.

```text
Implementation state:                       IMPLEMENTATION_CLOSED
Technical closeout:                         GREEN / CO-03 PASS PRESERVED
F2 mutation provenance:                     CLOSED — 34/34 complete rows
F4 review chain:                            CLOSED-BY-HOMEOWNER-RATIFICATION
Independent F2/F4 review:                   PENDING
Commit gate:                                NOT GRANTED
Stage / commit / push:                      NONE
```
