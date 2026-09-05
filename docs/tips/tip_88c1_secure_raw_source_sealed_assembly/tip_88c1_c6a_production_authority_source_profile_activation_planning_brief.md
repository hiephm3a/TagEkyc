# TIP-88C1-C6A — Independent Source-Validity Barrier & Production Profile Planning Brief

**Status:** `CANDIDATE v0.7.1 — AUTHORITY-LIFETIME EVIDENCE LEDGER ADDED — PLANNING ONLY`  
**Version:** 0.7.1  
**Date:** 2026-09-04  
**Risk tier:** High-risk  
**Owner:** Homeowner + Contractor/Drafter  
**Repository baseline:** `0a2d605bc0b1174825803e1982105d680718500c`  
**Implementation dispatch:** **NOT GRANTED**

## Changelog

### v0.7.1 — Bounded authority-lifetime evidence correction

- Added exact baseline file/line evidence for Policy, Grant and Permit lifetime
  at every C3 delivery attempt.
- Reconciled the apparent conflict with B3/B4 permit consumption: atomic
  one-time Permit-to-Job binding prevents a second job but does not terminate
  the frozen Permit validity window. B4 itself continues to enforce permit/job
  expiry during attempt orchestration.
- No census disposition, gate, implementation surface or semantic conclusion
  changed.

### v0.7 — Bounded census reconciliation

- Replaced v0.6's local-resolver predicate with reachability under an
  applicable still-governing admission/linearization point.
- Re-ran all 16 paths, restoring ratified C1 phase barriers, committed replay
  and C2 custody convergence.
- Reclassified RB-02 as future C6B admission-order proof.
- Retained RB-12 as the sole current `ACTUAL_MISSING_BARRIER`: a new C3
  content disclosure lacks the required live access/source-validity admission.
- Resolved Policy/Grant/Permit lifetime at C3 from TIP-88: all remain live
  through each new delivery stream. No Homeowner question is required.

v0.6 correctly retired D3 and ratified D5 but incorrectly equated “not locally
re-run” with “missing.” Its eight-blocker count is superseded.

## 1. Authority and evidence posture

This is planning-only. It authorizes no source/test/schema/config/code edit,
Raw BIO operation, patient data, deployment, activation or Git mutation.

All claims use immutable baseline `0a2d605b...`. It and checkout commit
`edcac28...` have tree `863b03db757be15fadc65985682e907fc399641c`
and zero differing paths. Dirty working-tree source is excluded.

`C5-COMMIT-MERGE-AUTHORITY-PROVENANCE-01` remains an independent governance
gap. The only write is this brief; tracing was read-only and no tests ran.

## 2. Ratified model

```text
authenticated caller
  -> effective grant
  -> effective policy + requirements
  -> valid authorization-domain permit
PLUS
effective subject consent
  AND effective AuthoritySnapshot
  AND applicable source/retention/deadline barriers
ALL applicable barriers pass -> Raw BIO may be read/used
```

| Decision | State | Meaning |
| --- | --- | --- |
| D1 | `REAFFIRM` | TagEkyc evaluates access; no external per-request authorization |
| D2 | `REAFFIRM` | AuthoritySnapshot owns source validity, not access policy |
| D3 | `RETIRED / NOT REQUIRED` | no generic AuthoritySnapshot management workflow |
| D4 | `REAFFIRM` | phase barriers, latest event and forbidden extension remain |
| `C6A-HO-D5-01` | `RATIFIED` | AuthoritySnapshot is independent, necessary where applicable, never sufficient |

AuthoritySnapshot does not cache/mirror Policy, Grant, Permit or Consent.
`Consent=Withdrawn; AuthoritySnapshot=Granted` is representable and denies at
the consent barrier; no compensating snapshot terminal event is required.

Consent lifecycle remains with 88B2; policy/grant lifecycle remains with
88A/88B. Existing snapshot Withdraw/Revoke SQL functions are
deployer/bootstrap-only and prove no production management requirement.
Physical disposal remains separate: TIP-83E-3 did not ship general purge
evaluation/execution.

## 3. Correct census predicate

For every barrier/path ask:

> Can this operation be reached without the barrier having won at an applicable
> admission/linearization point whose authority still governs the operation?

A missing local resolver call is not itself a defect. Classify current
revalidation, prior ratified admission, committed replay, custody convergence,
new disclosure, not-applicable, actual missing barrier and unresolved lifetime
separately. Field presence and grep hits are not barrier proof.

## 4. Policy/Grant/Permit lifetime at C3

The corpus is clear:

- TIP-88 says revocation, withdrawal, session transition, rule-set change,
  fulfillment expiry or permit expiry after assembly but before delivery
  prevents delivery.
- `PermitExpiresAt` is the absolute authorization ceiling; no
  permit-authorized read, seal or delivery occurs after it.
- `DeliveryAuthorityExpiresAt =
  min(PermitExpiresAt, JobExpiresAt, PackageExpiresAt)`.
- TIP-88C1 says authority loss after `Available` blocks read, reuse, new
  assembly use and delivery, while exact committed C2 custody still converges.

Therefore effective Policy and Grant, the original still-valid Permit, current
Consent, current AuthoritySnapshot and applicable source/retention horizons
remain live requirements at every new C3 content stream. C3 recipient/key
authorization does not replace them. Committed replay/finalization is not a new
disclosure.

`C6A-AUTHORITY-LIFETIME-TRACE-01` is
`CLOSED — CORPUS RESOLVED`; no Homeowner question is required.

### 4.1 Exact authority-lifetime evidence ledger

All quotations below are from the bound baseline
`0a2d605bc0b1174825803e1982105d680718500c`.

| Claim | Exact authoritative evidence |
| --- | --- |
| Fresh revalidation occurs at C3 | `docs/tips/tip_88_raw_export_policy_spine/tip_88_planning_brief.md:274-280`: “The assembly/delivery path must freshly revalidate” followed by “before every delivery attempt, including retries.” |
| Grant remains effective | same file `:282-290`: “The base authorization revalidation set includes” followed by “grant remains effective”. |
| Policy remains effective | same file `:282-291`: the same base set includes “policy lifecycle remains export-active”. |
| Permit remains live at delivery | same file `:282-295`: the same base set includes “permit remains within its valid use window”. |
| C3 explicitly owns this checkpoint | same file `:297-304`: “before every C3 delivery attempt” phase-specific revalidation additionally requires unexpired `JobExpiresAt`, `PackageExpiresAt` and `DeliveryAuthorityExpiresAt`. |
| Permit expiry blocks delivery | same file `:318-320`: “permit expiry after assembly but before delivery must prevent delivery”. |
| Permit is the delivery ceiling | same file `:330-336`: `PermitExpiresAt` is the “absolute authorization ceiling”; no permit-authorized delivery may occur after it; `DeliveryAuthorityExpiresAt = min(PermitExpiresAt, JobExpiresAt, PackageExpiresAt)`. |

These statements directly establish all three challenged claims; they do not
derive them from the absence of C3 resolver calls.

### 4.2 Why B3/B4 consumption does not contradict the lifetime

The counter-evidence has a different ownership:

- `tip_88b3_boundary.md:35-36` defers actual read, delivery and the one-time
  consumption enforcement of a Permit beyond B3. It does not state that Permit
  validity ends when the job is created.
- `tip_88b4_closeout.md:77-79` establishes one logical job per immutable
  Permit and freezes authorization identity. That is consumption cardinality
  and immutable binding, not deletion of the validity window.
- `tip_88b4_planning_brief.md:1584-1588` continues to enforce crossed
  Permit/Job deadlines during new/reclaimed attempt orchestration and maps them
  to `Expired`.
- the same file `:1590-1594` distinguishes finite authority loss from
  absolute Permit/Job expiry and maps the latter to
  `Expired / PERMIT_OR_JOB_EXPIRED`.

Thus the ratified model is:

```text
one Permit -> at most one immutable Job
AND
that Permit's frozen valid-use window remains an authorization ceiling
through Raw work and each later C3 delivery attempt
```

“Consumed once” prevents rebinding/reuse into another job; it does not mean
“expired immediately after binding.”

## 5. Reconciled 16-path census

| ID | OperationKind | NewAdmission | NewRawRead | NewExternalDisclosure | GoverningAdmissionPoint | GoverningAuthoritySet | CurrentRevalidationRequired | PriorBarrierStillGoverns | ReplayOrRecovery | FirstExternalSideEffect | EvidenceLocation | Disposition | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| RB-01 | source reservation/admission | Yes | No | No | future server-derived B2 production Grant/reservation | capture acceptance, Consent, source horizon, snapshot facts | Yes | No exact production path | none | durable reservation | B2 core/reservation/snapshot migrations | `REQUIRES_RECONCILIATION` | exact server-derived Grant point untraced |
| RB-02 | R2 plaintext custody encryption | No; follows R1 | Yes | No | successful R1 admission before body/R2 | source validity, Consent, retention established by R1; R2 lease/retention | at R1 | Yes if C6B proves byte order | none | first plaintext read/ciphertext write | R1 planning; R2 orchestrator lines 40-104; R2 context SQL 103-146 | `C6B_INTEGRATION_ADMISSION_PROOF_REQUIRED` | no production caller; prove zero byte reaches R2 before R1 wins |
| RB-03 | R3 staging | Yes | No plaintext | No | R3 transaction | current Consent, snapshot, reservation/absolute deadlines | Yes | N/A | exact ExistingMatch | staged state | R3 function lines 381-427 | `CURRENT_REVALIDATION_AT_THIS_ADMISSION` | barriers precede stage |
| RB-04 | R4 commit | Yes | No | No | R4 transaction | current Consent, snapshot, deadlines | Yes | N/A | exact committed match | Committed publication | R4 function | `CURRENT_REVALIDATION_AT_THIS_ADMISSION` | barriers precede commit |
| RB-05 | R5 publish | Yes | No | No recipient disclosure | R5 transaction | current Consent, snapshot, source/reservation horizon | Yes | N/A | exact Available match | source becomes resolver-available | R5 lines 489-540 | `CURRENT_REVALIDATION_AT_THIS_ADMISSION` | authority loss blocks Available |
| RB-06 | B4 permit-to-job claim | Yes | No | No | B4 bind/claim | caller, Policy, Grant, requirements, Permit and binding prerequisites | Yes | N/A | one immutable job/permit | durable job | EF job repository 175-239,743-785; B4 SQL | `CURRENT_REVALIDATION_AT_THIS_ADMISSION` | access admission precedes job |
| RB-07 | C1 source binding | Yes phase | No | No | B4 attempt plus C1 Freeze | governing job/Permit; fresh Consent, snapshot, source deadlines | phase-specific | Yes | exact binding replay | frozen bindings | C1 dispatch §§8-9; migration 430-576; C112 | `GOVERNED_BY_PRIOR_RATIFIED_ADMISSION` | B4 and Freeze jointly govern |
| RB-08 | C1 read/decrypt/assembly | No after Freeze | Yes | No | C1 Freeze immediately before first read | B4 job/Permit plus freshly validated Consent/snapshot/deadlines | at Freeze, then pre-Prepare/Seal | Yes | fresh branch | object read/plaintext chunks | C1 dispatch §9; as-built C112/C113/C117; resolver | `GOVERNED_BY_PRIOR_RATIFIED_ADMISSION` | read unreachable unless Freeze wins |
| RB-09 | committed C1 observation | No | No | No | committed seal | immutable assembly equality/provenance | No | sealed result governs observation | committed replay | return prior result | C1 committed-replay contract/as-built | `NOT_APPLICABLE — IMMUTABLE_COMMITTED_REPLAY` | observation is not reauthorization |
| RB-10 | C2 Prepare/package creation | Yes at C1 handoff | Yes assembly stream | No disclosure | C1 pre-Prepare and Seal | job/Permit + current Consent/snapshot/deadlines; recipient constraints | in C1 | Yes | deterministic preparation recovery | C2 custody write | C1 C113/I07B/I16; C2 migration | `GOVERNED_BY_PRIOR_RATIFIED_ADMISSION` | C2 must not duplicate authority engine |
| RB-11 | C2 finalize/recovery | No | No new Raw interpretation | No | exact committed C1 seal | committed preparation/custody state | No | seal governs exact preparation | custody convergence | finalize/clean exact object | TIP-88C1 post-seal/C1-I34; C2 recovery | `NOT_APPLICABLE — CUSTODY_CONVERGENCE` | authority loss cannot fork/prevent exact convergence |
| RB-12 | C3 content stream | Yes | encrypted package read | Yes | each PrepareContent/begin stream | recipient auth/scope/key plus live Policy, Grant, Permit, Consent, snapshot, horizons | Yes every stream/retry | No; custody is not disclosure authorization | status replay only | provider read then response bytes | TIP-88 §§3.5-3.6; TIP-88C1; C3 dispatch §§7-8; coordinator | `ACTUAL_MISSING_BARRIER — NEW_DISCLOSURE_ADMISSION` | C3 proves recipient/key/delivery expiry, not live upstream set |
| RB-13 | C3 state reconciliation | No | No | No | reconcile transaction | delivery state/lease/deadline | No source refresh | N/A | state-only | durable state transition | C3 reconciler and reconcile SQL | `NOT_APPLICABLE — STATE_ONLY_RECONCILIATION` | later content retry re-enters RB-12 |
| RB-14 | C4 reference list | metadata admission | No | No content | authenticated reference query | caller/category/reference scope/filter | No Raw authority | N/A | cursor replay | opaque metadata | C4 endpoint/service/repository | `NOT_APPLICABLE — METADATA_ONLY` | content remains RB-12 |
| RB-15 | C5 recipient management | management admission | No | No | C5 authorization | OperatorAdmin/recipient scope/state only | No Raw authority | N/A | operation replay | recipient metadata | C5 service/repository/migration | `NOT_APPLICABLE — NO_RAW_CONTENT` | neither reads nor discloses Raw |
| RB-16 | future C6B adapter | Yes | Yes | No direct disclosure | source -> derived authority/Consent/retention -> R1 -> R2 | exact source/capture/Consent/horizon and transition context | Yes before first byte | will govern R2 after proof | to be frozen | first production Raw byte | no implementation; C6B seam | `REQUIRES_C6B_RECONCILIATION` | close RB-01/RB-02 order |

### Exact counts

| Category | Count | Rows |
| --- | ---: | --- |
| `ACTUAL_MISSING_BARRIER` | 1 | RB-12 |
| `AUTHORITY_LIFETIME_UNRESOLVED` | 0 | none |
| `GOVERNED_BY_PRIOR_RATIFIED_ADMISSION` | 3 | RB-07, RB-08, RB-10 |
| replay/recovery N/A | 3 | RB-09, RB-11, RB-13 |
| current fresh admission pass | 4 | RB-03–RB-06 |
| other N/A | 2 | RB-14, RB-15 |
| reconciliation/integration proof | 3 | RB-01, RB-02, RB-16 |

Coverage is 16/16. Only RB-12 is a current executable missing barrier. RB-02 is
not a current production exposure because C6B has no production caller.

## 6. Technical gates

| Gate | State | Closure |
| --- | --- | --- |
| `C6A-SOURCE-GRANT-DERIVATION-01` | `OPEN / TECHNICAL` | freeze server-derived production snapshot Grant point |
| `C6A-C6B-INGRESS-ADMISSION-01` | `OPEN / TECHNICAL PROOF` | prove current source/Consent/retention + successful R1 precede every byte reaching R2 |
| `C6A-C3-DELIVERY-SOURCE-VALIDITY-01` | `OPEN / BLOCKING TECHNICAL` | prove/add live Policy, Grant, Permit, Consent, snapshot and horizons before every C3 stream/retry |
| `C6A-PROFILE-01` | `OPEN / TECHNICAL` | distinct non-Fixture profile preserving exact three errors |
| `C6A-AUTHORITY-LIFETIME-TRACE-01` | `CLOSED` | corpus resolves access authorities live through C3 |
| `C6A-ACL-01` | `CONDITIONAL` | only if final path proves privilege delta; no role presumed |
| `C6A-IDEMPOTENCY-01` | `CONDITIONAL` | only final derivation/admission need; no generic ledger |

The exact retained current blocking gate is
`C6A-C3-DELIVERY-SOURCE-VALIDITY-01`.

## 7. Reuse, profile and scope ceiling

Reuse authenticated context, policy/grant/permit domain, consent lifecycle,
snapshot ledger/resolver, actor/audit, B2/C1 deadlines, C1 phase barriers, C2
convergence and readiness infrastructure.

Do not reuse C5 recipient semantics:
`operator.raw-export.recipient.manage`, recipient operation/event tables,
recipient capability or DTO/service ownership.

No table, role, endpoint, management scope, operation ledger, framework or
package is pre-approved. No external provider, generic snapshot management
surface, duplicate authority engine, committed-replay break, purge-completeness
claim, patient data, C6B implementation, C6C run or activation is authorized.

Production profile errors remain exact:

```text
missing/empty          -> PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_MISSING
unknown                -> PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_INVALID
Production + Fixture   -> PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE
```

Readiness proves capability/composition only.

## 8. STOP/RRI and next action

STOP while Grant derivation/C6B order is unproven, RB-12 remains, C3 has a
withdraw/revoke/expiry race, a fix breaks committed replay/C2 convergence, a
second authority engine or generic management surface appears, Fixture/error
semantics drift, or any new surface lacks reuse analysis and reviewed authority.

Next:

1. run bounded independent v0.7.1 review;
2. if confirmed, draft a technical dispatch for the four open primary gates,
   centered on RB-12 without disturbing C1/C2 replay/convergence;
3. independently review dispatch before implementation.

Terminal state:
`READY_FOR_BOUNDED_INDEPENDENT_C6A_V0_7_1_REVIEW`.
