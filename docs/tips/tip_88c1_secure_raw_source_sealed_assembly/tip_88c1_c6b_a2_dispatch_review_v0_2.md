# TIP-88C1-C6B-A2 v0.2 — internal dispatch review record

Date: 2026-09-12. Documentation only; not implementation evidence or authority.

CURRENT STATUS NOTICE: v0.3 was subsequently externally HOLD for F04; see the
v0.4 correction at the end for current status. The v0.2 internal PASS below is historical, superseded by
external HOLD (1 HIGH / 2 MEDIUM / 1 LOW). In particular its ACK-before-R21
disposition is NON-OPERATIVE. The successor correction record at the end owns
current disposition. Historical review claims are preserved, not erased.

## Exact candidate

- File: tip_88c1_c6b_a2_windows_agent_profile_dispatch_v0_2.md
- SHA-256: 5AEABE0DD328962BB92890FD095B27D5460C28B5E3A0432459315E343A748615
- Lines: 599. Bytes: 47562.
- Server HEAD and local origin tracking ref: 4fafa17a43ad38f46c2e0c69d570f65b8d01abfe.
- CaptureAgent HEAD: b193f6316e13a82fb2f9f985f68500feae194cfa.
- v0.1 predecessor remains on disk, unmodified by this drafting turn.

## Procedure and scope

PI-TAG-001 High-risk internal review used contract/source, security/lifecycle and
free adversarial subagents. These are Codex internal reviewers, NOT CC/GPT external
acceptance. Full reviews included the complete candidate and adjacent actual
callers; later bounded reviews checked applied findings and regression boundaries.
Maximum ten rounds; convergence reached at five. No minimum finding quota.

Selected modules: API, crypto/key, raw/restricted-data and governance. No product
SQL mutation is proposed. The PostgreSQL harness is a test dependency. Polling is
an in-process task, not a new distributed worker/queue. External review is pending.

## Round ledger

| Round | Review and findings | Applied disposition |
| --- | --- | --- |
| 1 | Contract: 5 HIGH + 2 MEDIUM; security: 1 HIGH + 5 MEDIUM. Thirteen reports, ten unique findings after three overlaps. | All ten applied: context-carrier reuse; pre-ACK journal; WPF preview/orchestrator gating; exact child command contract; real-server test ownership; rotation input; response casing; UUIDv4 append identity; CNG creation crash recovery; disabled configuration caching. |
| 2 | Security PASS 0 on C393C9A70BA4177E3FD2536FC5FE98E45F60E728370B5FEC1FD446DB9B325DAB. Contract: 2 MEDIUM. | Removed false existing-mutex claim; pinned dedicated fixture/container runner and P06 server proof owner. |
| 3 | Free adversarial review of the same frozen candidate: 2 additional MEDIUM, excluding known Round-2 findings. | Bounded journal retirement/compaction and protected uncertain states; explicit successful-bind secret clearing before device delegation. |
| 4 | Contract and free review of 23E0AE7D9110CA95D450EC260DEA44EB821ED1C63C43CA22C768329255033CDD. Substantive findings closed; contract found 1 LOW literal defect. | Corrected two literal backslashes in the PowerShell mutex name to one. No semantic amendment. |
| 5 | Contract bounded clean confirmation of final SHA above: PASS — 0 actionable findings. | Reviewer reversed only the literal correction in memory and reproduced the exact Round-4 SHA: no other drift. |

## Finding-to-proof disposition

1. Execution context: reuse existing external-session/consent observations,
   snapshot before the slot, compare session and replace identities from enrollment.
2. ACK ordering: non-secret BindPending lineage durably precedes ACK; R21 follows
   successful ACK. No capability secret is journaled.
3. WPF: actual MainWindow caller and preview timer traced; interface decorator,
   lazy devices and preview proxy gate both paths without editing MainWindow.
4. Child commands: exact diagnose/enroll/rotate input, output, limits and recovery;
   A4 launcher is a future consumer, not an assumed landed component.
5. Server proof: test-only IntegrationTests-to-AgentClient reference; new runner
   owns a dedicated synthetic PostgreSQL container. No shared Compose lifecycle.
6. Rotation: explicit non-secret authorization input and resume command; no
   configuration probe, predecessor fallback or new reconciliation endpoint.
7. Serialization: A1 request/config shape distinguished from existing camel-case
   capture/evidence responses; no assumed universal serializer compatibility.
8. Append identity: stable per-binding/slot UUIDv4 replaces incompatible RunId/slot
   string; exact replay bytes retained, uncertain missing bytes fail closed.
9. CNG: reserved locator flushed before key creation; no regeneration over an
   ambiguous/enrolled identity and no private-key export.
10. Configuration: valid disabled document and ETag retained, retained mode denied.
11. Fixture ownership and P06: explicit-connection internal fixture constructor,
    dedicated reset/clone, no shared Initialize/Dispose; exact server test owner.
12. Journal: one execution, six slots, bounded cleanup queue, terminal compaction;
    unresolved work cannot be evicted to manufacture capacity.
13. Secrets: successful bind/reconcile clears capability material before capture;
    bounded ambiguous replay is distinguished from successful delegation.
14. Mutex literal: runtime name is `Global\TagEkycA2Acceptance` with one backslash.

These are planned discriminating proofs, not executed test results. Dispatch §8
owns exact methods and RED controls; this report does not redefine their contracts.

## Cumulative convergence analysis

Round 3: initial nonconvergence came from treating conceptual reuse as callable
reuse, then omitting temporal endpoints. Actual WPF callers, fixture initialization
and allocation-to-ACK-to-restart-to-retirement traces replaced generic prose review.
The improvement was to inspect the full lifecycle and test owner, not add features.

Round 4: cumulative semantic joins were closed. Remaining defect was literal shell
syntax, not a missing decision. Review checked resource owners, finite bounds,
pre-device zeroization and fixture constructor/disposal side effects together.

Round 5: no continuing semantic nonconvergence. A mechanical inverse-delta SHA
check proved the one-character correction introduced no further patch drift.
No additional architecture or Homeowner decision was invented to force convergence.

## Final read-only checks and limits

- Recomputed all 24 recorded CaptureAgent consumed-source SHA values: 24/24 exact.
- MainWindow.xaml.cs and server IntegrationTests.csproj supplemental hashes also
  match their dispatch bindings. Existing dirty/untracked source remains untouched.
- Both repositories: staged 0; unmerged 0. No stage/commit/push this turn.
- Only the new dispatch and this new review record were authored by this task.
- Product code/schema/test/config/project/lock-file mutation count: 0.
- Build, restore, architecture and integration tests: NOT EXECUTED this turn.
- Historical NU1004 is not represented as a newly measured failure or a green suite.
- No production credential, retained biometric ingress or A3/A4 implementation.
- No external review or remote document synchronization performed.

INTERNAL_DISPATCH_REVIEW: PASS — 0 ACTIONABLE FINDINGS.
EXTERNAL_CC_GPT_REVIEW: PENDING.
A2_IMPLEMENTATION_AUTHORITY: NOT_GRANTED.
A3_PRODUCTION_STAGE_COMMIT_PUSH_AUTHORITY: NOT_GRANTED_BY_THIS_REVIEW.

## External review and v0.3 bounded correction

Correction closeout recorded: 2026-09-13 (Asia/Bangkok).

Authority: Homeowner requested application of the supplied CC/GPT reviews,
documents only. No product implementation authority was granted by this request.
v0.2 remains byte-exact at 5AEABE0DD328962BB92890FD095B27D5460C28B5E3A0432459315E343A748615.

Successor: tip_88c1_c6b_a2_windows_agent_profile_dispatch_v0_3.md
SHA-256: 439B9B2714BB87C400C1F78772966651424E65CC763464E121686CCF0E65D8DA
Lines: 709. Bytes: 56413.

| External finding | Verified evidence | Applied delta / invalidated surfaces |
| --- | --- | --- |
| F01 HIGH: ACK-before-R21 loses non-durable secret after sender release | v0.2 §§3,5,6,7,8; R21/R22 existing boundaries | Binding-confirmation ACK after confirmed binding, local result flush and zeroization; R22-first restart with exact redelivery/same operation; crash cases and P05/P08/P09/P12 |
| F02 MEDIUM: numeric config anti-rollback is not A1 authority | Foundation:1270–1283 and Agent RawExportIngressContracts.cs:87 | Accept valid authenticated server-selected lower/different assignment; §1 census, §5 cache, P04 |
| F03 MEDIUM: unconditional sibling reference breaks standalone server build | TagEkyc.sln includes IntegrationTests; proposed reference crosses Git trees | §§2,8,10,11: EnableCaptureAgentA2Acceptance=false default; reference AND Compile opt-in; true/missing-project fails; runner restores opted-in graph; default/opt-in build proofs |
| CC LOW: missing explicit lowercase CredentialId | CaptureRuntimeEndpoints.cs:424–426 ordinal Guid.ToString("N") comparison | §4 exact CRT1/header pin; P07 real-parser proof retained |

F02 qualification: the external correction's SAME ConfigurationId/Revision plus
changed-content rejection was NOT adopted in final v0.3. Foundation:1270–1281
permits assigning a different ConfigurationOverrideId to the same pair;
1714–1738 merges override fields into the R23 effective projection. The closed
15-field response does not expose override identity. A valid authenticated 200
with the same pair and changed effective content must therefore be accepted.
P04 now proves that positive case; ETag stays opaque and exact 304 consistency/
freshness is preserved. No A1 change, new field, resolver or decision is invented.

## Continued review / convergence checkpoint

Round numbering continues the five historical internal passes above; external
review is round 6, not another clean internal verdict.

| Round | Result | Disposition |
| --- | --- | --- |
| 6 external | HOLD: 1 HIGH / 2 MEDIUM / 1 LOW | Four findings source-verified; same-pair immutability remedy subsequently qualified by actual SQL override projection |
| 7 internal full affected-surface review, E4E9548F981AD3468CE6DC00BF24773218DA3708F73D918172A705061105F161 | Contract 2 HIGH / 1 MEDIUM; root also corrected P13 ownership label | Reject false effective-config immutability; add durable device-start distinction and finite unknown-binding retirement. Free review's clean F02 assessment explicitly withdrawn after missing SQL override assignment was demonstrated |
| 8 bounded patch verification and free sentinel, successor SHA above | Contract and free reviewers: PASS — 0 actionable findings | Both verified SQL override acceptance, durable start claim, finite terminal retirement and opt-in graph/P13 owner; no edits/tests by reviewers |

Checkpoint after external invalidation of round-5 PASS: earlier reviews traced
durability before ACK but not sender secret disposal before server commitment;
accepted cache mechanics without tracing assignment/override SQL; and checked
dependency direction without isolated-checkout build reachability. These were
incomplete prior reviews, not new Homeowner decisions. Corrections remain inside
the authorized docs-only scope, so rounds 6–10 can converge without RRI.

Round-7 cumulative cause: copying an external proposed remedy without traversing
the effective projection repeated the source-of-truth error; ACK recovery changes
also invalidated device-start and pending-terminal lifecycle coverage. Improved
review follows allocation → server commit → local flush → ACK → device start →
first append → restart → retirement, and separately catalog → assignment → override
→ response → cache. It tests absence of a sibling checkout, not just reference
direction. No finding quota, no replay of already closed architecture decisions.

Round-8 scope: source-faithful config acceptance; nonsecret DelegationAttempted
persisted before any device/preview call, restart uncertainty denies reacquisition;
terminal pending tombstone at original deadline without claiming server absence.
At-most-once marking is not proof of capture completion. No product implementation.

## Correction outcome and limits

Changed sections: metadata; §§1,2,3,4,5,6,7,8,10,11,12. §9 source hashes unchanged.
No new server DTO/API/SQL, A1 semantic decision, A3/A4 or production activation.
Only successor dispatch and this review record changed in this correction turn.
v0.2 preserved. Both repositories staged=0, conflicted=0; source freeze 24/24 exact.
No code/schema/test/project/config/lock mutation; no build/restore/test execution.
No stage/commit/push or GDrive synchronization. No implemented-test claim.
External review of successor remains pending; predecessor PASS does not transfer.

CURRENT_STATE: READY_FOR_EXTERNAL_A2_REVIEW.
IMPLEMENTATION_AUTHORITY: NOT_GRANTED.

## v0.4 — external F04 ACK-delivery ambiguity correction

Date: 2026-09-13. Authorized by Homeowner instruction to apply the confirmed
finding immediately. Scope remains successor dispatch plus this review record.
v0.3 is preserved byte-exact at
439B9B2714BB87C400C1F78772966651424E65CC763464E121686CCF0E65D8DA.

External round 9: GPT HOLD — 1 HIGH (A2-F04); CC bounded PASS on its narrower
delta scope does not close this finding. Earlier findings remain closed.

Root cause: remote HTTP receipt arrival and a local write are not one atomic
fact. Bound+DelegationAttempted=false cannot distinguish a response received by
the sender from a lost response. Round-8 review verified at-most-once acquisition
but left execution eligibility dependent on that unobservable remote event.
This is an incomplete earlier review and ordering propagation defect, not a new
A1 decision. Remedy stays within the authorized F04 correction; no new RRI.

Successor: tip_88c1_c6b_a2_windows_agent_profile_dispatch_v0_4.md
SHA-256: 93D06034E8A69F2F13B9CA8BFB4F407E4C7AE6E71FA8AB8B6A435E23E5861C8D
Lines: 750. Bytes: 59444.

Affected surfaces: metadata; §3 bind-material ledger; §5 ordering; §6 journal
atoms, receipt/recovery/crash/retirement contracts; §7 outcome row; §8
P05/P08/P09/P12 and discriminating controls; §12 changelog. No new HTTP shape,
server DTO, signature preimage, SQL or external authentication surface.

Exact delta: binding confirmation → local result flush → capability-secret
zeroization → durable HandoffAccepted=true → replayable HTTP200 receipt.
Execution requires accepted=true and the single durable DelegationAttempted CAS,
not remote receipt delivery. Lost receipt cannot undo acceptance or force sender
redelivery before continuation. Accepted execution remains bounded by its frozen
execution horizon/readiness; unaccepted pending intake retains the existing
finite-deadline/tombstone rule. No at-least-once capture or completion claim.

Proof plan now independently distinguishes acceptance false (device count 0),
accepted/unattempted (one CAS winner), accepted/attempted (restart acquisition 0),
crash before response bytes, response write failure, delivered receipt then
crash, and concurrent exact redelivery. These are planned tests, NOT executions.

Round 10 bounded review: contract and free reviewers both PASS — 0 actionable
findings on the exact successor SHA above. Both verified the two durable guards,
receipt-delivery independence, zeroization order, pending versus accepted horizons
and the proof propagation; free review also read the full v0.3→v0.4 diff and
whole-document sentinel scan. No reviewer edits or test execution. Converged at
the hard review-loop limit; no extra round is claimed or authorized.

Correction non-claims: only successor and review record edited; no product,
test, project, config, schema or lock-file mutation; no stage/commit/push;
no A1/A3/A4/production reopening; no build/test execution or remote sync.
Both repositories staged=0/conflicted=0; 24/24 consumed Agent SHA values match.
CURRENT_STATE: READY_FOR_EXTERNAL_A2_REVIEW (v0.4 exact SHA above).
EXTERNAL_V0_4_REVIEW: PENDING. IMPLEMENTATION_AUTHORITY: NOT_GRANTED.
