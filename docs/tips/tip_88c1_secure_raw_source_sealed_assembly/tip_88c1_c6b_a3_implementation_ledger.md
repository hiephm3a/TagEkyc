# A3 implementation ledger

**Date:** 2026-09-13. **Status:** implementation in progress, not acceptance evidence.
**Authority:** Homeowner ratification of Parent v0.6 SHA-256
`3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813`,
its five bound companions and exact M/N inventory v0.6, plus the correction below.
Synthetic/non-patient only; no A4/production/stage/commit/push.

## Approved bounded correction

`A3-E01-CLIENT-REGISTRY-CORRECTION = APPROVED` by the Homeowner in this task.
Remove the nonexistent ClientApplication registry FK from the E01 reference table
and its named constraint. Do not create any registry, substitute a credential FK,
or make api_keys.ClientApplicationId unique. Client identity is not credential lifetime.
The baseline executable migration script has 18,732 lines and no client registry
table reference; the only baseline reference is the unimplemented LLD entity.

Preserve non-null/nonzero Client identity, both reference unique keys, the binding
composite FK, session FK and exact Client/SubjectRef equality; authenticated
BusinessConsumer Client/P, current recorder/withdrawer authority, reference locks,
and immutable downstream lineage are unchanged. No Client identity is accepted
in request JSON. Before final review update the normative successor catalogue and
rebind dependencies; preserve the ratified predecessor bytes.

Required corrected proofs: owned Client/session succeeds; foreign session denies
with no reference/event/binding residue; Client identity JSON injection rejects;
cross-client binding mutation rejects; credential revocation/rotation does not
delete or relabel the persisted reference.

## Execution status

A3.0 partial code/model/migration candidate exists; E01 endpoints, typed service,
gateway, configuration profile, SQL-owned model and candidate migration SQL are
implemented. Application/API compilation passed. E01 real PostgreSQL tests now
pass 12/12, zero failed/skipped, in isolated synthetic databases. They execute
the actual candidate Up operations, not substitute DDL. This is NOT CP10's final
EF migration-chain/checkout-representation proof or full A3.0 acceptance.

Evidence (raw SHA-256):

- `tests/TagEkyc.IntegrationTests/TestResults/a3-e01-reviewed-restored.trx`:
  `D4B17BECF39567D1F00EE70460299FEC6909B04C3A848D0FFBC29B3DC2ECBBBD`,
  12/12 PASS: owned/foreign Client, forbidden JSON identity, cross-client graph
  rejection, withdrawal replay, credential revocation/replacement independence,
  and exact existing session SubjectRef with LF/CR/CRLF.
- `tests/TagEkyc.IntegrationTests/TestResults/a3-e01-subject-before-fix.trx`:
  `0F21B878D5FE448F866214E204B2D728CACE2C51F0917536C8709C1B834B8A30`,
  0/3 intentional RED before removing the two unauthorized SubjectRef CR/LF
  predicates. Independent implementation review found this compatibility defect;
  exact equality/nonblank checks remain, with no normalization of session identity.

The earlier 11/12 run (`a3-e01-reviewed.trx`) exposed the existing random API-key
prefix/parser mismatch: generator can emit `_` inside its prefix, while parser
splits at the first `_`. It failed before E01 execution. The new E01 lifecycle
fixture uses deterministic synthetic key material through the existing generator
interface; real provisioning, hash storage, authentication and revocation still
execute. No retry/skip or production authentication fix is introduced here.

E01 policy/permit/race proofs, B2 synchronization, CP lineage/checkpoints, final
Designer/manifest and A3.1–A3.4 remain unfinished. No production schema applied.
Independent bounded V2 source re-review closed the SubjectRef finding with
0 actionable findings; the reviewer did not run PostgreSQL or verify TRX bytes.
Execution results above are Builder-run evidence, not an external/full A3 PASS.

## Approved exact allowlist correction — 2026-09-14

Before the R20 Application amendment, source inspection confirmed an omitted
existing test mutation target:

`tests/TagEkyc.UnitTests/Tip88C1C6BA1ExecutionTests.cs`

Current raw SHA-256:
`5CA63AF2DCF9109FEE643DF8A5065B2FD3B84CEC865C0680A8B377D4FD714E92`.

Lines 15–16 construct the successful BusinessConsumer fixture without PrincipalId;
AuthenticatedClientContext defaults it to Guid.Empty. The first two Issue tests
expect success/gateway invocation. CP11 explicitly requires nonempty authenticated
PrincipalId for both retained and non-retained Issue, so preserving this fixture
would contradict the ratified guard. This path is not an M/N inventory row.
The Homeowner explicitly granted this one added M row on 2026-09-14.

Granted narrow permission: add this single test path to the M allowlist, supply
an explicit nonzero synthetic PrincipalId in its positive fixture, and add a
missing-PrincipalId denial/no-gateway negative plus Client/P fingerprint partition
checks. Preserve all existing replay/secret-once/body-byte assertions; do not add
a product identity fallback or disable tests. No new project/dependency/semantic
decision is required.

Read-only baseline execution: 9/9 PASS in
`tests/TagEkyc.UnitTests/TestResults/a3-r20-existing-fixture-baseline.trx`, SHA-256
`45E18F1547C5F9531956F55F228E889BB72B299A51F421A1B5E689D8E83CCFFB`.
This confirms the old fixture behavior only, not the future A3 R20 behavior.
The pause is closed. The fixture now uses deterministic PrincipalId
`a3000000-0000-4000-8000-000000000020`, without changing ClientApplicationId/category.
Both Issue and Replace reject empty PrincipalId before RNG/pepper/gateway use.
The success test checks exact forwarded PrincipalId and ClientApplicationId.
Before the implementation guard, the two new negative cases failed while all
9 old cases passed (`a3-r20-principal-before-fix.trx`); after the guard, the
focused suite passes 11/11 (`a3-r20-principal-restored.trx`, also rechecked in
`a3-r20-principal-profile.trx`). This is unit-level no-persistence-call evidence;
it is not a claim that the still-incomplete R20 SQL lineage has passed PostgreSQL.
R20 DTO/profile/gateway and 32-byte Client/P actor partition implementation is in
progress under CP11. No new planning RRI, staging, commit or production action.

## CP11 implementation checkpoint — 2026-09-14

R20 now carries authenticated nonempty PrincipalId and the closed server profile
into its single persistence transaction. Capability/binding rows carry the exact
five-field authority lineage; retained Issue composes the actual E01 issuer only
after replay classification. R21 copies and validates frozen lineage. Prepared DI
remains lazy; Activated dependencies are not replaced by fixture defaults.

Executed Builder evidence (not full A3 acceptance):

| Evidence | Result | Exact TRX SHA-256 |
| --- | --- | --- |
| UnitTests/a3-r20-principal-restored.trx | 11/11 PASS | BE277A2357C99C13B59497C0F0CF36045544A860D6EABA63A9003384B831C327 |
| UnitTests/a3-unit-prepared-restored.trx | 324/324 PASS | 1AB477C48D8FCE1016C4A326743851961099850916AC74E2F27ECDE252419E80 |
| IntegrationTests/a3-cp11-lock-clock-mutation.trx | 3 intended RED; 1 matching positive PASS | 1D86B2714CB840A9365E30962AAAE05110953A2C89A6B820E9196B72DEB0AB7D |
| IntegrationTests/a3-cp11-all-restored.trx | 32/32 PASS after exact restoration | 90207CE6A1B3AD72C1A5E3B5E79EB7AFD02A0695974572D0B266BFE12E0DF28E |

TRX paths above are under tests/<project>/TestResults. The 32 cases cover current
E01/R20/migration implementation only, before the subsequent B2 hook addition.
They do not claim R21 retained runtime fixture, CP02–CP10, broker, R2–R6 or Agent
completion. The new migration remains a candidate without its final generated
Designer/model snapshot or complete A3 function catalogue.

Independent CP11 V1 read the complete changed implementation and adjacent
callers/guards: HIGH unlocked Up/Down population census; MEDIUM retained Replace
failed to reclassify predecessor expiry after reference/policy waits. Both were
confirmed and fixed inside authority. Up/Down now acquire transaction-held
session/capability/binding table locks before census; Down also locks all five
E01 tables. Retained Replace uses the post-wait clock to persist the established
Expired operation/event with no successor. Two-connection pg_locks proofs
exercise actual writers, observer preservation and frozen replay.

Removing these mechanisms produced exactly three intended RED cases; the
non-expiring Replace positive remained GREEN. Restored V2 independently verified
both fixes, adjacent ordering and the recomputed R20 body hash, with zero new
actionable findings. Reviewer did not run databases/tests. These are two CP11
review passes, not full A3 completion review; no non-convergence or new authority
decision. Lesson: population/expiry predicates must be evaluated after all
blocking locks, including migration and replacement branches.

B2 withdrawal synchronous projection is implemented at this checkpoint.
Its DecisionRef preserves B2's nullable varchar(256) value verbatim, including
existing valid CR/LF and multi-byte Unicode. The new E01-W JSON route retains its
stricter byte/character grammar; that route grammar is not applied retroactively
to the B2 projection. This fixes an implementation overgeneralization, not a
change to the ratified B2 preservation contract.

The actual B2 withdrawal function derives the reference from its exact target
Granted event, takes session -> E01 reference -> B2 scope locks, then projects
the committed B2 event identity into E01. Two real PostgreSQL connections prove
both B2/E01 withdrawal orderings and independent observer visibility. Three
nullable/CRLF/multibyte DecisionRef controls and missing/mismatched-head controls
preserve the landed B2 behavior. Removing the hook produced 3/3 intended RED
cases, not a skip or mocked resolver. Bounded independent source review returned
PASS with zero actionable findings; the reviewer did not execute the database.

| Evidence (IntegrationTests/TestResults) | Result | Exact TRX SHA-256 |
| --- | --- | --- |
| a3-b2-hook-removed-mutation.trx | 3 intended RED | BA711FD733FD687BE204EC720ECCA9C2984E54019728064D42BFF9C91114104B |
| a3-e01-cp11-b2-restored.trx | 39/39 PASS | 8E06CE70D49BB42AF7F332EE87D2F873AD41E1264B9F46FF2AB4F0E6C2ACE44E |
| a3-snapshot-guarded-roundtrip.trx | 5/5 PASS, intermediate Up/Down with 8 historical bodies | A1A49F7F13E9C9CB848012F10E7D09EC5CF3BBF42E59C2289248E80272457192 |

CP02/CP03 snapshot schema/model, tagged writer/resolver/terminal functions and
focused behavior tests are now in progress. This is not CP05–CP10, final EF
migration-chain/checkout-representation evidence, broker or Agent completion.
The original ratified companion remains unchanged; its eventual normative
successor and final generated model/catalogue remain pending.

## CP02/CP03 intermediate implementation and V1/V2 correction

The additive snapshot columns/model, tagged grant/current resolver, retained
append/helper, and existing withdraw/revoke kind propagation are implemented.
No parallel custody table or early B2 consent is introduced. Eight historical
function bodies participate in the current empty Up/Down proof; the complete
A3 migration/Designer/representation catalogue is still pending.

V1 found four implementation defects: missing broker EXECUTE, retained terminal
checks using transaction-start time, an initially invisible target bypassing the
session/reference prefix, and missing INSERT-time reference-head equality.
All four were confirmed and fixed. Legacy terminal timing remains unchanged;
retained timing samples after scope locks. The post-lock target cannot replace
an absent initial retained probe. INSERT checks the current reference revision.
V2 independently verified the complete correction/sentinels and all seven
current/five restored snapshot-body hashes, with zero actionable findings.
Reviewer did not execute tests or PostgreSQL.

| Evidence (tests/<project>/TestResults) | Result | Exact TRX SHA-256 |
| --- | --- | --- |
| UnitTests/a3-unit-snapshot-model.trx | 324/324 PASS | ED7D24AE05F567E5863BF68D248ABE859CC8D525A661F3C79C23628EF5A89F9E |
| IntegrationTests/a3-snapshot-acl-mutation.trx | 1 intended RED, actual broker permission denial | B59E3D2605FBE69D920A6F8CCDACFD24B852E59F5144378ABA446811E3025488 |
| IntegrationTests/a3-snapshot-clock-mutation.trx | 6 intended RED, 2 unaffected controls PASS | 4903D9D796E6A22751BD1366BA642247F50E9D8C4BEF8F9801811A74C0D80839 |
| IntegrationTests/a3-snapshot-prefix-mutation.trx | 2 intended RED, 2 expiry controls PASS | E3E612248CF4CC6AD0A1356C07045706C72C70DF7D6B367C352DA8FF54C957DE |
| IntegrationTests/a3-snapshot-head-mutation.trx | 1 intended RED, 5 independent lineage controls PASS | 139339E5D3D8E7F300CB0876E6842631BE60E2594FB30980C4B9418CC1B8994C |
| IntegrationTests/a3-snapshot-v2-restored.trx | 57/57 current A3 tests PASS, no skips | BA1676F2AD830551D38A368034D105214F13D28CAFB9202024DB339FF9B747AB |

The reference-head mutation uses a second valid acceptance, so stale lineage
actually INSERTs when the guard is removed; it does not merely hit a duplicate
unique index. The two terminal functions each exercise real PostgreSQL blocking
and separate observer visibility. Same-transaction and older-transaction
positive controls isolate the clock boundary. Migration bytes were restored
exactly after mutations to SHA
8C7C4B8999C364ECFCC42E657590D907CE9C79E6A38E6E47CDE5FA35329F3D35.

These are intermediate CP02/CP03 proofs, not the complete named CP10 proof
inventory or A3 closeout. The 57 cases include E01/CP11/B2 and snapshot work;
CP05+, broker, provider continuation and Agent integration remain unfinished.
Review improvement: test actual role invocation and both visibility/time sides
of every newly tagged branch, rather than relying on privileged fixtures and
fresh transactions. No authority question or new RRI was required.

## CP05 staging implementation checkpoint — not retained-stage proof closure

The single R20 test-file correction remains applied: fixed non-empty principal
`a3000000-0000-4000-8000-000000000020`, unchanged Client/category fixture,
Issue/Replace empty-principal denial before pepper/gateway, and exact successful
principal forwarding. Fresh focused execution is 11/11 PASS
(`a3-r20-principal-current.trx`,
EAEFDC52AD8BEDA3428C592A78AFD771CBE3024C416722D68032B95314BE0F7E).

The same A3 forward migration now contains three tagged R3/R4/R5 replacements
and their exact historical restorations. Retained branches acquire the actual
owned session, shared reference, policy/rule-set and every non-consent policy
requirement before the existing stage locks and fresh clock. Probed immutable
lineage is rechecked before replay. Fresh retained stages reject pending R2
intent and use current retention authority, not pre-Completed B2 export
consent. Historic legacy replay, fingerprints, result shapes and grants remain.
The migration's guarded body census includes all three additional current and
predecessor hashes.

BR4.4's three nullable intent columns/CHECK and their EF mapping are present.
The already-landed terminal outcome is mapped as nullable PostgreSQL text,
not added again. Down checks intent population under the migration table lock
before the stronger retained-population check. TI01/TI02, NPS01, write-guard
adaptations and their behavioral proofs remain UNFINISHED; the candidate
migration is not yet a deployable/completion artifact.

Independent stage V1 found a real HIGH defect: the first implementation
hard-coded three fulfillment locks and omitted supported CrossBorderAssessment.
The source correction now selects the exact policy's requirements excluding
ConsentArtifact, ordered COLLATE C, before the clock in all three functions.
The required actual retained-stage lock-wait/expiry proof is still OPEN.
Roundtrip, source review and the legacy test below do not discharge it.
V2 independently rehashed all three corrected bodies and confirmed the complete
dynamic pre-clock lock set; no new bounded source finding. Behavioral closure
remains pending and is not promoted to PASS by that source verdict.

Additional executed evidence:

| Evidence | Result | TRX SHA-256 |
| --- | --- | --- |
| UnitTests/a3-unit-stage-current.trx | 324/324 PASS | 89E7BE7926809168DB21B029B244460BC249957088DF76065C66828AFADC3802 |
| IntegrationTests/a3-stage-legacy.trx | 1/1 PASS: actual R2/AEAD/Minio verification, then tagged legacy R3/R4/R5 and exact replay | CABA71C76D6AA2A72E6E22302F8D152F40277F53F8EC359BDBC2DA220DA275B3 |
| IntegrationTests/a3-stage-v2-current.trx | 58/58 current A3 cases PASS after the source correction; zero skips | 0B69E3CADF90C3D6AF9A3C42DE5DFBFA68DC46F604BAED35FFF0D22481BFFCFE |

The legacy proof reuses the landed internal fixture against a separate
disposable PostgreSQL database; it does not substitute retained authority or
claim retained success. Migration source after the requirement-lock correction:
C3BFDC5FB5A3A837E93746236164EFCEF98CF980177043CFCB849C758C411A4B.
Normal compile/SQL syntax defects were fixed and retested without a new RRI.
No stage, commit, push, A4 or production action occurred.


## R1 and retained R3/R4/R5 behavioral checkpoint — intermediate only

External CC and GPT accepted intermediate packet
C76975CF81C298756F7716F10F64BAFC0AEC0BAFE608CCE03DFD113B9A77CCF0
within its explicitly incomplete scope. That packet remains byte-immutable.
The implementation below advances beyond it; neither verdict is an A3 overall PASS.

The same authorized forward migration now implements the 24-input/13-column
retained B-B authority+begin boundary and adapts the installed 42-input completion
boundary. Actual legacy begin requires producer identity equal to PrincipalId;
A3 requires runtime producer identity distinct from the frozen BusinessConsumer.
Therefore B-B preserves the landed claim/alias mechanics but joins the exact
retained runtime binding instead of weakening the old function or falsifying
the fixture. Completion keeps the legacy export branch and adds retained
session/reference/policy/authority locks, immutable revalidation, post-lock
clock and bounded handoff. No new consent action or early B2 history is created.
The old completion body is pinned for Down; roundtrip now compares twelve
predecessor function bodies. Full broker facade, B-R, RE01/NPS01/TI01/TI02 remain
unfinished; testing this SQL increment does not claim those paths exist.

Independent R1 review V1 found that a same-key retry could return a stored New
token after the reservation already existed. The added replay proof first
failed exactly at New-versus-ExistingComparison while four consent controls
passed. The locked alias/claim state now selects comparison after Reserved.
V2 found no remaining bounded source finding. Exact retry returns busy with
all four new handoff fields null and preserves all custody rows.

The new retained-stage fixture executes real R1, durable fixture key preparation,
framed encryption, MinIO write/read, AEAD verification and VerifiedCompleted.
It does not seed ciphertext completion evidence. Six R3/R4/R5 success/withdrawal
cases compare full reservation/attempt/head/publication/key/object rows through
an independent observer. Eight R1/R3/R4/R5 withdrawal races use actual winner,
waiter and observer PostgreSQL connections, exact reference advisory key/mode
in pg_locks and pg_blocking_pids, pre-commit invisibility, both commit orders,
and exact residue/authority outcomes.

A temporary mutation removed the shared-reference primitive at all fourteen
A3 acquisition sites, including reentrant helpers, while preserving exclusive
withdrawal locks and predicates. All eight races became RED at the exact
expected-lock assertion; all four sequential consent controls stayed GREEN.
This proves collective dependence of these eight paths on reference locking,
not individual certification of each of the fourteen sites. Restoration was
verified by full raw migration SHA before the complete A3 rerun.

| Executed artifact in IntegrationTests/TestResults | Result | SHA-256 |
| --- | --- | --- |
| a3-r1-replay-red.trx | 1 intended RED; 4 controls PASS | 31C1FC1CCE52A8B6F8918D2B1F138657A8CCA11969C9D8E76B7985AD933D129E |
| a3-r1-replay-fixed.trx | 7/7 PASS | 37DF5DB616D5D4BE097563F7EC7792B351911415B514C7FE95A8DD9AFB2AFB2F |
| a3-retained-stage-second.trx | 6/6 PASS | C5F8433F5F5925C3E688B1870109E960A7773938AD95E2C25537992E25736F9F |
| a3-r1-r5-races-first.trx | 8/8 PASS | 63B2462CACE89855C424252ACE5741BC285DEEBC21DDFA28F6128EEFF6F0E9A5 |
| a3-r1-r5-shared-lock-red.trx | 8 intended RED; 4 controls PASS | 1093925A0BFE9FC5D375170E09D3AF3D0B5636CC44503DD209C2828F2CE0CDD9 |
| a3-r1-r5-restored-full.trx | 77/77 PASS; zero skipped | 42D38A05C2F51739F1E3DC5BFE6606006A9853B4879EE0C72AB668B2E00D8A73 |

Restored migration raw SHA:
8B17947A28BAFC19D396CD46F898BC84CF211C52BA0B7F36450B4CAE835C3EB4.
Current retained B-B body:
b1ec83c8e892a4670f165ddd4efa8f5ab6bc79f90344909fb4400d8f31563f0e.
Current completion body:
4eefedc274d1d2c6152dd4100ed75f4ab4cdccdc997b04290da82bcc83a87b65.
Historical completion restoration:
b7abf8f3f21d70a9af7be33d8ea780773bf09dc41aee82ff9fcaddd79b11e8a9.

V3 free proof review independently inspected both parameterized matrices,
real-provider setup, transaction/observer helpers and production paths and
reported no bounded actionable finding; it did not execute PostgreSQL.
The CrossBorderAssessment wait/expiry clock proof, actual C1/C3 B2 withdrawal
ordering, broker recovery/composition, Agent receipt/restart and final catalogue
closure remain OPEN. The 77-case run is not the full repository integration
suite and is not A3 completion. No stage/commit/push or production action.



## CrossBorderAssessment post-wait clock — behavioral correction closed

The dynamic non-consent policy requirement loop now has an actual six-case
R3/R4/R5 proof, not just a source check. The test adds a real foreign-recipient
policy requirement, records fulfillment through the control-plane authority,
supersedes its revision through the actual append function, and blocks the
checkpoint on the exact CrossBorderAssessment advisory domain. An observer
proves it reached that domain while valid; the expiry cases release only after
the database clock passes the new fulfillment horizon. Denial preserves all
six custody-table row sets and existing verified ciphertext; controls succeed.

The targeted source mutation excludes only CrossBorderAssessment from the
three pre-clock stage loops while retaining the later resolver locks. All three
expiry cases then reach the wait but wrongly return Staged/Committed/Available;
all three live controls remain GREEN. This distinguishes a stale-clock
authorization failure from mere lock reachability. The first mutation launch
failed at fixture connection bootstrap and is NOT a semantic RED; its rerun
below provides the valid evidence. Ordinary fixture supersedes-revision and
C# interpolation mistakes were fixed, not reclassified as production findings.

| IntegrationTests/TestResults artifact | Result | SHA-256 |
| --- | --- | --- |
| a3-crossborder-clock-second.trx | 6/6 PASS | D473D11D6FA4C1CC872CE76931B8D5801E8C9CADD6D803BF096E6B409802AC22 |
| a3-crossborder-prefix-red-rerun.trx | 3 intended outcome RED; 3 controls PASS | 4FA69126BDF3AC36AF4D7D8DA7EDCF74767EB5FFD2B6AB9CEDA4C491409374DF |
| a3-crossborder-restored-full.trx | 83/83 PASS, zero skips | C1D1F16F2B8B676DDD63EF84E8716C8EC91615AFF6E67FD346FE8E31EFCCD228 |

The restored full run also includes the strengthened exact lock classid and
blocker-PID assertions. Migration raw SHA at this checkpoint was unchanged
8B17947A28BAFC19D396CD46F898BC84CF211C52BA0B7F36450B4CAE835C3EB4.
A subsequent broker-reader increment may change that file; this is a historical
execution anchor, not permission to silently rebind prior evidence.
Independent full test/helper/control-plane/clock-chain source review found no
bounded actionable finding and performed no execution. This closes the prior
CP05 CrossBorderAssessment wait/expiry proof gap only. C1/C3 actual B2-withdrawal
ordering, continuation/terminal recovery, broker composition and Agent work
remain unfinished.



## B-R locked broker reader — bounded implementation checkpoint

Implemented the ratified new
capture_runtime_read_bound_raw_ingress(uuid,uuid,uuid,bigint,uuid,bigint,uuid,uuid,integer,text,bigint,timestamptz)
in the same A3 forward migration with exact 15-column return, owner-only
internals, broker-only EXECUTE, current-body guard and exact Down drop.
No PrincipalId/ClientApplicationId input or fake append role is introduced.
Strict binding/acceptance selection derives the frozen actor/profile, checks
current runtime/installation/generation/RawIngress policy/configuration,
and revalidates E01 retention after actual session/reference/all-requirement
locks and a fresh database clock. Publishing newer unassigned catalog heads
does not replace the assigned revision.

The initial executable positive path exposed a varchar/text return mismatch,
fixed by exact casts. Own lock-order inspection also moved the runtime row
locks after all A1 advisory domains. A separate three-connection proof blocks
B-R on exact session domain 70 and obtains the registration row with NOWAIT.
Moving the row block back before domain 70 makes precisely that test fail
with PostgreSQL 55P03 on capture_runtime_registrations; both identity/role
controls remain GREEN. This proves that identified ordering edge, not every
advisory/row combination or concurrent credential-revocation scenario.

The two role-presence cases invoke the actual broker grant with empty actor
context: only RawIngress returns one 15-field result; eleven altered observed
inputs return no rows. Withdrawal returns no row; no snapshot or reservation
is created by this reader. Effective EXECUTE is checked for broker and each
forbidden runtime/application/authenticator/operator capability role.
No cryptographic A1 admission or full private HTTP facade is simulated here.

| IntegrationTests/TestResults artifact | Result | SHA-256 |
| --- | --- | --- |
| a3-bound-reader-third.trx | 3/3 PASS, including empty Up/Down/Up | D51B122374019191C75625342B5560F82B535749351C0148D9584A1E3D37240A |
| a3-bound-reader-lock-control.trx | 4/4 PASS | 4C1A91144752A469D231DC33DA764FDEE73895D52E1003F84C500F55CDBE9828 |
| a3-bound-reader-order-red.trx | 1 intended NOWAIT RED; 2 controls PASS | DE3413D5BAEE5A9AF30FB6232605F9571C0C05AB0653BED85713B1C9D17444F3 |
| a3-bound-reader-restored-full.trx | 86 executed / 86 PASS / 0 failed / 0 skipped | E14AFBCC0D73CCB1DBA899112C7499FE6855F9DC875BECE2D80915535555B660 |

The second reader launch failed solely during fixture connection bootstrap;
it is not behavioral evidence. The complete restored run above is on these
current source bytes (full SHA comparison, no prefix comparison):

| Current source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | CA7780178B3155A7145DB3A673300F63E1BF2C0BC43B33884FD29E0E80A6DF2F |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs | 6AB242F6AC15E92FCAEAD8D49CE7EC4A0D73F5DA0BC807E16C00C8FC1650346A |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs | 801D6F32467E825E6E7618D30094624E7E71780FFD6AF3E8354DAD1647588CD0 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs | D49B77A7817D944410CDD8473028BFC7B334EC3EE6CA7904FAE38ED6F3EBE895 |

B-R canonical body hash:
39bc73673df8801b8c09461636694140dc01d3108edf1c44530151cb053a7f10.
Bounded source review and subsequent free lock-proof review both found no
actionable finding and independently verified the body/raw hashes; neither
reviewer executed tests. The parent executed the evidence above. No mutation
remains in the source. The original intermediate packet remains unchanged.

Still OPEN: B transaction facade/private host and adapter, RE01/NPS01/TI01/TI02
and provider guards, C1/C3 current-retention/export synchronization and actual
B2 withdrawal ordering, Agent retained pipeline/receipt restart, final migration
designer/catalogue/readiness and independent checkout representation proof.
A3.1 is not claimed complete, and these 86 cases are not A3 overall PASS or the
full repository Integration suite. Both repos remain staged=0/conflicted=0.
No new RRI, A4/production, stage, commit or push action.


## Broker preflight extraction — 2026-09-14, partial A3.1

Implemented BR 3.1's shared `RetainedSourceClaimPreflight` and routed the legacy
comparison broker through it. C1 envelope, commitment/subject payloads, NFC,
microsecond timestamps, nonce commitment and framing digest reuse the existing
canonical helpers. The locked claim supplies its commitment selector; the caller
supplies the qualified subject selector explicitly. The helper has no fixture
default, database connection, authority lookup or raw-body input. The legacy
caller alone retains its existing fixture subject selector. No SQL/transaction
or migration bytes changed in this checkpoint; this is not the one-B facade.

V1 independent source review found two real extraction regressions: constructing
the selector before typed envelope/authority denials, and reading custody
configuration before commitment/subject-provider failures. Both were fixed.
Input now holds raw selector ID/version until after those guards; profile digest
computation is separate, preserving the original provider-before-profile order.
V2 full corrected-source/free regression review returned zero findings. The
competing invalid-selector/envelope test exercises the helper; the legacy
authority/configuration competing-failure order is source-reviewed, not claimed
as a new dynamically executed legacy precedence proof.

### Executed evidence and unresolved migration integration

Final combined run: **109 executed, 108 PASS, 1 FAIL, 0 skipped**. Breakdown:
86/86 existing A3 PostgreSQL cases, 10/10 new preflight cases, 4/4 B2 Beta,
8/9 B2 Core. The new preflight tests use recording keyed-service doubles and an
independent length-prefix encoder: they prove input bytes/selection/result
handling, not real keyed cryptography, HTTP, B commit or recovery. B2 Beta's
real PostgreSQL paths also passed after the extraction, including historic-key
unavailability and fresh authority withdrawal precedence.

The one unresolved Core failure is `C1B2CORE_new_candidate_commits_full_recovery_context_atomically`:
PostgreSQL 42703, missing `R2TerminalIntentAtUtc`, at line 102's attempt entity
query, after NewReservation/reservation/admission-fingerprint assertions. The
current A3 model has those fields, but the A3 migration is not yet registered
through its Designer; A3 tests explicitly apply its UpOperations whereas the
ordinary fixture uses EF migration discovery. Temporarily restoring the broker
to HEAD content reproduced exactly the same failure; `git diff HEAD` on that
file was empty before the baseline run. HEAD was
`5df5f60a6dc4d992c71fc2b160673e4abca6488d`, broker Git blob
`46a67c34a261e9db405a1d6638e6ed31e9178a90`. Final broker bytes were then restored
and verified in full. This proves a pre-existing current-A3 model/registration
gap, **not** a clean repository baseline or permission to ignore it. Its owner
is A3 migration registration/Designer and final regression closure; it remains
OPEN and must be fixed before overall A3 acceptance. No test was weakened.

The first wider launch failed because Docker Desktop was stopped (99 fixture
initialization failures, 9 pure tests passed). The isolated synthetic engine was
started and the run repeated; those initialization failures are not mutation
evidence. No unrelated containers/data were cleaned up.

| Artifact (under the indicated test project's TestResults) | Result | SHA-256 |
| --- | --- | --- |
| Integration / a3-preflight-v2-selector-red.trx | Replace locked selector with synthetic-latest: 3 intended RED, 7 controls PASS | 02D1009A1E6D5FB8CD6EC129254C8277143B23EF05955D986932F1283A2CE9F7 |
| Integration / a3-preflight-v2-envelope-red.trx | Remove envelope comparison: 3 intended RED, 7 controls PASS | D12AE05F13A9A900965E45BFF2C30CAA8022EBC78060E1B531BC1F8E7F92183A |
| Integration / a3-preflight-legacy-baseline.trx | Original broker: same 1 Core failure; 10 pure tests PASS | 9460193DDA3B6586B905AB791C633DF2C46443AE3697166A462899853C945499 |
| Integration / a3-preflight-v2-final.trx | Restored current candidate: 108/109 PASS, same 1 Core failure | 84057D2465AA0A836452AF71BC743F4D14E5DB29C68460954E4842CE17E62666 |
| Arch / a3-preflight-arch.trx | 153/153 PASS | 3BB0646E3CE73A5C5A693FCB9443284AD2586D283659DF4FFC5CBEBFF204E361 |
| Unit / a3-preflight-unit.trx | 324/324 PASS | 0870899BB359908A9363550635CE60124F5FB097FF8DCC4A98223A98C10668BE |

The envelope mutation returned material for two invalid cases and allowed the
competing malformed selector to throw for the third; the latter is evidence of
incorrect denial precedence, not a provider success. All mutations are restored.
Independent evidence audit verified the two mutation TRX, baseline failure,
Arch TRX and current source hashes; the parent executed all runs. Source switching
for the baseline is Builder-observed provenance, not something TRX alone proves.

| Current source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/RetainedSourceClaimPreflight.cs | 82B4116FF7243EF0AC7C01588429A8C03638CA977A30041C6480035531814B62 |
| src/TagEkyc.Infrastructure/RawExport/RawExportSourceClaimComparisonBroker.cs | 51D33C0240F33A1E760F4E08BC49B220823258C3E829F8DAF1587A1179D7D7B6 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 50B5D5D33C3B27699416137FA1B18D18D6EAFE11EB8B1D69C69E1A89F51178B7 |

The final Integration bin/Debug/net8.0 Infrastructure DLL SHA is
`60990974E3D44D2B46F114AC355ACB4C574139C128AED0DA391C34D73627FA32`;
IntegrationTests DLL SHA is
`7A11C0A9A5C56A036ECF7D89371CC3F1FB87A681292667C981E114829546BBD3`.
The six ratified authority artifact hashes and original intermediate packet
remain unchanged. Both indexes remain empty and neither repository has conflicts.
No Agent change, stage/commit/push, A4 or production action was performed.
Next remains the same-B facade/private boundary and RE01/NPS01/TI01/TI02 recovery
composition, then C1/C3 and Agent. **A3.1/BP04/BP08 and overall A3 are not closed.**

## EF registration correction — 2026-09-14 (A3 remains in progress)

The authorized A3 migration pair is now registered through generated
`DbContext`/`Migration` attributes on its Designer. The model snapshot was
generated from the actual current model using the installed EF 8 tooling.
The snapshot aligns model generation; it is not itself the discovery mechanism.
Temporary scaffolds were not installed or executed as additional migrations.
The product migration ID remains `20260913120000_Tip88C1C6BA3RetainedIngressComposition`.
Its Up/Down SQL bytes are unchanged in this correction; the already-existing
`R2TerminalOutcomeCode` was not added a second time.

A3 `Prepare` now calls EF migration discovery/history, not direct UpOperations.
The six migration cases exercise from-empty application and mapped-column reads,
model/history agreement, actual PostgreSQL index presence, apply/rollback/reapply,
two real writer/population races, and two atomic body-drift denials. The roundtrip
compares twelve selected legacy function bodies, not the entire final catalogue.
Independent checkout-representation/CP10 proof remains open.

V1 independent review found two convention-created FK indexes absent from the
authorized DDL. The correction reuses the existing scoped FK-index convention
and excludes only snapshot singleton ConsentBindingId/RuntimeBindingId indexes.
The intended explicit indexes and FKs remain. Regeneration confirmed the precise
two-index metadata delta. V2 patch review and V3 free sweep found no further
actionable defect; reviewers did not run PostgreSQL. The Builder ran the proofs.

| Executed evidence | Result | SHA-256 |
| --- | --- | --- |
| Integration/TestResults/a3-ef-registration-first.trx | 109/109 PASS; former missing-column Core failure closed | F917C91FD7E05EBD5BC3A894D2C5FB84F2A0314B8CFBBDB0D42AC259ED829CB6 |
| Integration/TestResults/a3-ef-model-positive.trx | 6/6 PASS after model/index correction | EDA39C80F5BF8A4DD48F3A059898126120878933152CF483E993C91C80FA972D |
| Integration/TestResults/a3-ef-discovery-red.trx | Remove Migration attribute: 1 intended RED, expected discovery count 1 / actual 0 | 2D0F95B6FCB38582515A634F32412B7668F244B3081EE9D7598B09C424C4BF2D |
| Integration/TestResults/a3-ef-index-catalogue-red.trx | Restore unwanted indexes in model + both generated views: 1 intended RED at actual catalogue comparison, not HasPendingModelChanges | 96153DC71A89A03CA1114DA12FD1B8AF42BD99D1CAE70DA1FB09FD28B7F29AB8 |
| Integration/TestResults/a3-ef-registration-final.trx | Restored combined group: 110/110 PASS, 0 failed, 0 skipped | 31D1B8641A4C8CB2D44CB1AA93DCA7983355BF7E8AAC8C70B441E622B0ACD6DD |
| Unit/TestResults/a3-ef-registration-unit.trx | 324/324 PASS | 5C165DD29F8461B232834CD243ACD8FDC279E91F75DB547BAB10973E28001405 |
| Arch/TestResults/a3-ef-registration-arch.trx | 152/153 PASS; C527 strict historical model pins remain stale | 02C710B8ABAF2E6E015902CECBCB34F0D42BC77774E8A2FC10CE7284F47C96E9 |

These paths are relative to the respective `tests/TagEkyc.*Tests` project.
Both mutations were restored. Designer, DbContext and snapshot raw bytes match
their positive-control values, including the snapshot's generated CRLF form.
The final combined restoration run passed all 110 cases: 87 A3 PostgreSQL,
10 pure preflight, 9 B2 Core and 4 Beta. This is a focused group, not the full
Integration suite. The 6 migration cases are included, not six additional cases.

| Current correction artifact | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | CA7780178B3155A7145DB3A673300F63E1BF2C0BC43B33884FD29E0E80A6DF2F |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.Designer.cs | DE03FBE2D529A95196C0F76A2871A1AC7C91B8D071F3B622EE54A8A187180105 |
| src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs | 5C87733D64F4BDAFA54BA5221333A01AC3A0505549EE7E226C257B52DAA4295A |
| src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs | 681652F2E63BBB6DA7088FFFBDA4652F8E9C83A71754FF29B13E42B71D574514 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3ConsentRetentionTests.cs | B0022F7C3CF8223B0F98AC240571A6F819C49705485DF0058F628E4FEDB99C20 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs | 940DCFC6516D082295823DBBE6ED83EECDB3112ECE5429CF6F729B524734261F |

### Historical STOP checkpoint: three model pins (resolved by the addendum below)

C527 computes the current CRLF-to-LF-only model SHA as
`7DC276ED9F1E44F840DB5680940D55F0F7C652316E2A0DA6BD2CEAF221930F5B`.
Exactly three historical test owners still pin
`1B5F09EF3BF78A4D5748021AAEB82AC26A8B00D631F9C071EECEA903A1F17578`:

| File under tests/TagEkyc.IntegrationTests | Exact pin | Current untouched raw SHA-256 |
| --- | --- | --- |
| Tip88B1E3ResolverReadBoundaryTests.cs | ExpectedModelSnapshotSha256 | A9B5F8E0568CD952D4AC0A77334BFC8CFC652E8618D8BC1AEB4803C4C2C58F88 |
| Tip88C1B2R3VerifiedCiphertextStagingTests.cs | ExpectedSnapshotSha256 | 3BEAFB922B5E16DDC0D7C122FC52E405A485926871EB6552D17A25960187B5AA |
| Tip88C1C2RecipientPackageTests.cs | C201 snapshotHash equality literal | B3509F403CAE91149E996C4F5C204F6E1231A0A5CB2DFC837EC17D0C7E2816CA |

These three paths are absent from the exact v0.6 M/N inventory. No permission to
edit them is inferred from permission to regenerate the model. They remain
unchanged; C527 was not weakened, skipped, normalized more broadly or reclassified
as PASS. The bounded requested addition is only the three expected SHA values
and their corresponding A3 inventory/as-built provenance, not business/test
semantics, historical migration edits or a new RRI. Homeowner approval is pending.
This is an A3-owned guard reconciliation, not an unrelated failure.

Separately, already-allowlisted A1/A2 tests still require successor-schema R20
signature/latest-migration adaptations. The focused run is not full Integration
coverage. Broker facade/recovery, C1/C3, Agent, full CP10, overall A3 and landing
remain open. No A4/production, Agent mutation, staging, commit or push occurred.

## Approved three-pin provenance rebind — 2026-09-14

The latest Homeowner-supplied bounded correction explicitly adds the following
three paths to the A3 allowlist solely to replace their expected model-snapshot
SHA literals. This closes the historical permission STOP above; it does not
modify the immutable ratified v0.6 inventory bytes or authorize other changes in
these test files. Canonical inventory successor propagation remains a final
as-built obligation, with this dated record carrying the amendment now.

All three changed from
`1B5F09EF3BF78A4D5748021AAEB82AC26A8B00D631F9C071EECEA903A1F17578`
to independently recomputed
`7DC276ED9F1E44F840DB5680940D55F0F7C652316E2A0DA6BD2CEAF221930F5B`.
The latter hashes CRLF-to-LF-only snapshot bytes, not raw CRLF bytes and not an
all-CR deletion. Raw snapshot remains
`5C87733D64F4BDAFA54BA5221333A01AC3A0505549EE7E226C257B52DAA4295A`.

| Newly allowed exact path | New raw SHA-256 |
| --- | --- |
| tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs | A89CA5285F53D2C7C1C9889D2B4389DF03656CF515713F9E3A21F6033297DB22 |
| tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs | 12EA02525595BE0B0EEBDF1EE6C6F7CC0985D26BAAD0ECC51EAA6F9E340B50DB |
| tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs | E944654C6C9A401608D1F891D2102CB97A2F9DC04D5736D6C9255815A6693AD3 |

Inverse replacement of the one new SHA literal in each file reproduces its
entire prior raw SHA in the historical table above. Thus every other byte,
including line endings, test names, assertions, normalization and other hashes,
is unchanged. C527 itself is untouched. No old-or-new acceptance, prefix compare,
skip, broader normalization, dependency or architecture change is introduced.

| Executed evidence | Result | SHA-256 |
| --- | --- | --- |
| Integration/TestResults/a3-snapshot-rebind-three.trx | F6 + R316 + C201: 3/3 PASS | 20FF5A0AE76F5154D7794EF8DE9F515B7D690D32AF2A1A2326EA240CBBBED3F3 |
| Arch/TestResults/a3-snapshot-rebind-arch.trx | 153/153 PASS, including C527 | 7C9FC47467E9E6C4F208615722065A1598C8B0392269C56684C103B2C0AC3E20 |

Independent bounded V1 and free V3 review returned zero actionable findings;
V2 was unnecessary because the exact three-literal patch required no correction.
The reviewer independently checked the raw-byte inverse replacement, unchanged
C527 behavior and the two TRX artifacts, but did not execute PostgreSQL.
The combined affected-class plus A3/B2 regression completed with 166/169 PASS,
three failures and zero skips, not an overall PASS. Its TRX
`Integration/TestResults/a3-snapshot-rebind-regression.trx` has SHA-256
`0E654322B91D0EFCA3625BEFAB7B7652C54E78F84E9F9EA01450EA26F4E23BFB`.
This rebind does not claim broker/recovery, C1/C3, Agent or A3 completion. No stage/commit/push
or production authority follows; no GDrive synchronization has been performed.

### Historical regression disposition and narrow boundary (superseded below)

R301 intentionally builds the historical R3 schema, then its shared R2 fixture
materializes the entire current attempt entity. PostgreSQL 42703 reports absent
`R2TerminalIntentAtUtc` at `CompleteCandidateAsync` line 3852 in
`tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs`.
Unlike the earlier current-schema registration defect, this database must remain
at historical R3. Independent read-only review confirms a second full-entity
read in `VerifyWrittenSourceAsync` line 2812 would encounter the same problem.
The fixture file is outside the v0.6 inventory and the latest three-pin-only
amendment; it remains untouched at raw SHA-256
`01EBEE71120ADBA0EE61255108B3B7A2AB055CCE9751D10CCFF629CD4758CA32`.

The requested bounded extension is only those two fixture queries: project
`AttemptId`, `AttemptKeyReservationId`, `EncryptionAttemptRevision`, `Fence`,
plus `SourceArtifactId` in the verification query, before `SingleAsync`.
Preserve predicates/cardinality, real persisted evidence, every assertion and
the historical R3 target. Do not remove A3 mappings, migrate the test to latest,
add columns to old schema, skip a test or weaken a guard. No new RRI is proposed.
R301 remains OPEN pending this permission and executable verification.

The other two failures were the E01 direct/B2 withdrawal interleavings: expected
two B2 consent events but found 22. The disposable database clones the current
fixture, including 20 previous events; the actual lock/winner/revision checks
had passed. The already-allowlisted A3 consent test now explicitly seeds unrelated
historical consent, snapshots all prior B2 event JSON rows, asserts exactly two
new rows globally and two for the target session, and asserts every prior row
is unchanged. The real PostgreSQL connections, `pg_locks` checks and reference
event assertions remain unchanged. This is a test-only ordinary FIX + RETEST,
not part of the three-file literal-only exception or a product mutation.

Corrected A3 consent test raw SHA-256:
`8BED8D1F447D94DFECFC63999E8E269D938FC117CC905971D7055D762B32B0FA`.
Focused two-case restoration PASS: `a3-snapshot-rebind-withdrawal-restored.trx`,
SHA-256 `05DD5594FC49AADC7D0D8B8494EFE4AA650C670A8DED757E0E605C50F2DC77F3`.
Complete E01 class: 22/22 PASS, zero failed/skipped,
`a3-snapshot-rebind-e01-final.trx`, SHA-256
`46154C257B6AD53D956495D6B2790ADC563AC27E711F9A95F9C4D79ED4716B1A`.
Final Arch: 153/153 PASS, `a3-snapshot-rebind-arch-final.trx`, SHA-256
`A3404497ADB9E0079134C0C39F87B641E2878CF795633482B4F054FF2FB22655`.
Independent V1/free V3 review of this fixture-history correction found zero
actionable findings and verified the focused TRX; it did not run tests.
The original 169-case failure report is preserved, not relabeled PASS; that
entire group has not been rerun after the E01 correction. Both repositories
still have zero staged and conflicted paths. No broker implementation was
added during this bounded correction. Await the single R2 fixture permission
above before closing this regression gate and continuing broker/recovery.

## Approved historical R3 fixture projections — 2026-09-14

Homeowner authorized exactly one additional file,
`tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs`,
solely for two minimal read-query projections. This resolves the permission
checkpoint above without new RRI or changes to ratified v0.6 bytes.

| Query | Retained selection and cardinality | Projected fields |
| --- | --- | --- |
| VerifyWrittenSourceAsync | Original `AttemptId == written.Source.AttemptId`; `SingleAsync` | AttemptId, SourceArtifactId, AttemptKeyReservationId, EncryptionAttemptRevision, Fence |
| CompleteCandidateAsync | Original `SourceArtifactId == sourceArtifactId` moved unchanged into `Where` before projection; `SingleAsync()` | AttemptId, AttemptKeyReservationId, EncryptionAttemptRevision, Fence |

The second query's actual pre-correction predicate was SourceArtifactId, not
AttemptId; it is preserved, not reinterpreted from the authorization shorthand.
No assertion, write, historical migration target, product mapping, migration,
terminal-intent semantics or R2/R3 business behavior changed. No raw SQL was
introduced. Inverse replacement of these two query blocks reproduces the entire
original raw SHA, proving all other file bytes are unchanged.

Before raw SHA: `01EBEE71120ADBA0EE61255108B3B7A2AB055CCE9751D10CCFF629CD4758CA32`.
After raw SHA: `FD130101174485337F85BFBD8B618B5C81095F68670A737ED3CBA024BC88C74B`.

| Evidence in Integration/TestResults | Result | SHA-256 |
| --- | --- | --- |
| a3-r2-projection-r301.trx | R301: 1/1 PASS, zero failed/skipped; historical apply/occupied-Down rejection/empty rollback/reapply exercised | EBAB0F9E7C0AAACE46D4F1B96D74284264C5AC61CF89C76DDB5AAA1BE89CDA97 |
| a3-r2-projection-r3-class.trx | Full R3 class: 14/14 PASS, zero failed/skipped | 263EF8F77D42FB5BED21A47BBB7B65DE6F4960C3FFCD7904685B2AF7F4BDB3D4 |
| a3-r2-projection-regression-169.trx | Exact original regression group: 169/169 PASS, zero failed/skipped; test-name multiset delta 0 against previous 169-case RED run | 2703B94EC16E9AE33A48FB4ABD65E17D94E153B7411AC479509BBF798671617B |

Arch also reran without rebuilding while the sole PostgreSQL regression runner
was active: 153/153 PASS, `Arch/TestResults/a3-r2-projection-arch.trx`, SHA-256
`47B675E98D4596D3476A143B8465628EAC88C80AA675FD8B5835DD6FE5E06E71`.
The requested sequence R301 -> full R3 class -> identical 169-case group is
complete. This regression gate is CLOSED / PASS, with no remaining permission
request for this correction. The prior failure evidence is not relabeled.
Independent V1 and free V3 review returned zero actionable findings; V2 was not
needed because no patch was requested. Review inspected both complete helpers,
normal/existing-session caller chains, R301, R2/R3 repository boundaries and A3
column additions. It confirmed byte-exact inverse restoration and dismissed
selection/cardinality drift, tracking dependence, input-echo substitution,
future-column reads and historical-target changes. Runtime validation is owned
by Builder, not claimed as reviewer execution. Two review passes, no
non-convergence or new architecture decision. Lesson: shared historical fixtures
must project their consumed historical fields rather than current whole entities.
The earlier 42703 RED report remains preserved. This is fixture
compatibility only, not A3 completion, broker/recovery closure or production
evidence. No stage/commit/push or GDrive synchronization is authorized/performed.
Both repositories have zero staged/conflicted paths; unrelated changes remain.
Next authorized work resumes broker/recovery, then the still-open C1/C3 and
Agent retained/receipt surfaces. No new architecture decision or RRI was needed.

## Initial same-B broker facade — 2026-09-14

Continued under the existing A3 v0.6 inventory, without a new RRI. Added the
metadata-only Application port, closed internal Handoff/Final contract and
Infrastructure transaction facade. One connection/B transaction owns B-R,
server-derived actor context, B-B, shared C1 preflight and B-C; readers close
before the awaited commit. No legacy independently-transacting broker, raw
stream, key/object provider, new R2-R6 pipeline or public handoff is used.
Synthetic composition lives only in the IntegrationTests assembly. Reused the
real E01/R20/R21 retention fixture by changing only Seed/Scope visibility to
internal in the existing A3 checkpoint test file.

Read-only historical sweep of capture_capabilities, capture_execution_bindings
and raw_export_authority_snapshots found no further reachable historical
whole-entity/future-column defect: capability/binding have no EF full-row read,
and the two snapshot EF reads project Max(Revision)/Count. Existing typed SQL
readers and historical call chains were inspected. This is static evidence, not
an additional runtime PASS or permission to change historical tests.

V1 found two ordinary implementation defects: the new retained B-B SQL still
used the copied 300-second/5000-ms maxima, and expected B-B SQL rejections were
escaping as generic failures. Corrected only this A3 overload to 3600/30000.
Closed exact SQLSTATE/message handling exists only at the B-B reader; its typed
exception causes awaited rollback of B before Final. B-C, unexpected SQL,
provider and commit exceptions remain failures. No aborted transaction is
committed and no unknown commit is described as a known rollback. Tests cover
same-key envelope denial, persisted ConflictTombstone replay and actual
independent-connection advisory contention; complete existing row sets remain
unchanged. Private transport/error encoding and full LOGIN qualification remain
unimplemented and are not claimed by this facade proof.

The first migration regression correctly failed 18/21 because the active B-B
body pin had not yet followed the bounds correction. Preserve that diagnostic
TRX, not a PASS. Recomputed CRLF-to-LF-only UTF-8 body SHA is
`f761acfe2123d34bb1bde39aee31eb302ac720b3117ff8dc2f845bc11ae089ba`.
Inverse replacement of only those two numeric literals reproduces the old
`b1ec83c8e892a4670f165ddd4efa8f5ab6bc79f90344909fb4400d8f31563f0e`.
Only the corresponding active current-body guard pin changed; historical
migrations, Designer/model/snapshot and other body pins did not change.

The B proof uses real PostgreSQL plus an independent observer. Test-only
triggers record top-level pg_current_xact_id()/backend PID for six R1 row
families; row xmin is deliberately not treated as the top-level transaction
because PL/pgSQL exception blocks use subtransactions. B-R's specific runtime
advisory lock must still belong to that backend during preflight. Preflight
failure and deferred commit failure return no Handoff and leave no new R1 rows.
Lost response replay returns ReservationBusy, not another handoff/attempt.
Configured commitment v2 versus persisted v1 discriminates latest-key fallback.

| Changed artifact (repository relative) | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Contracts/RawExport/RawIngressBrokerContracts.cs | D560F6BE2DCA7B46E61B557F8F695321C9F2771AA365954AD9BA2CAF60C0BB24 |
| src/TagEkyc.Application/Ports/RawIngressBrokerPorts.cs | AC6448EA4C512B89378DD161D9F2A96D2C1AD9ACDDEA03CC1B58A367743E5D78 |
| src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerTransactionFacade.cs | 9EC44C020484B22DCBC67B0343EE9C3FA05221CDE21043B42AD966D30EFA8EDE |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs | 1B52B71303DF76863E7A32F7C6C53DEF5DC1DD4B9317B84BD0A0CCD3EECF7DDE |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | C17C464D99B49B008996C9799FD5CD1D13DCBBB1AA3C5F3E69B9E0FC1A71CC1E |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs | C9C0FA970D2F274C9CDC912BB0A6F97E5BCE3834E79F07F445EEA5121178003D |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | 7C47B9B3CBE6409AE3D2A7A906F1280B402B3620B62F995E97833C6F51F732A6 |

Evidence under the corresponding test project's TestResults directory:

| TRX | Observed result | Raw SHA-256 |
| --- | --- | --- |
| a3-broker-same-b-and-migration.trx | Diagnostic 18/21, three active-body-pin failures | 5D09C1FB1F6EB06DEE0CC5951EB6B46B03E722AD7D31F0FAD8B83A6AC30F8D46 |
| a3-broker-same-b-migration-restored.trx | 21/21 PASS; 15 broker/preflight and 6 migration cases including roundtrip/population guards | A5BFD88ECF0269B0F8AA4583167293C02B15730F8764DC38B1933EF16EF3E67F |
| a3-broker-mutation-separate-br.trx | 0/4, intended RED: required runtime lock missing after separate B-R commit | 3999F600CC48AC5D2053360004618854DF999A1494403CDB6725FFD12C0FCD7C |
| a3-broker-mutation-commit-bb.trx | 0/4, intended RED: continuously active transaction expected 1, observed 0 after early B-B commit | 7BD81EA92750F0720074FD21EDB7973F261BCF3FD8BF1ADF43A1EE6DF614DF07 |
| a3-broker-mutation-old-sql-bounds.trx | 3/4, only qualified extended-bounds admission RED | 35DDDE315DC74871F392E9DD768267EA361CBDC1FC708DAC65705FACC2062537 |
| a3-broker-mutation-latest-selector.trx | 2/4, success/extended cases RED: expected stored v1, observed configured v2 | 319C136F979AE122C7FECA1B1E50577BB30308F76CDBA5CD385623261BE4255F |
| a3-broker-final-unit.trx | 324/324 PASS after exact restoration | 52A9CAC352D84F46B54F0D4754098D5F181A7DEDBB9988DDFDA59DB77870D689 |
| a3-broker-final-arch.trx | 153/153 PASS after exact restoration | AE295BC624DCA2F9E214CD2A989312DD353D028E2CF1AAAD1C49FC88698913B2 |

All four mutations were individually restored to the exact facade/migration
hashes above. V1 identified the two bounded defects; V2 verified the corrections;
free V3 independently inspected mutation TRX and returned zero actionable
findings. Reviewer did not run tests or mutate PostgreSQL. Convergence lesson:
trace actual SQL result strings/exceptions and active body pins together with
the new reader; generic result-enum assumptions are insufficient. From round 3
the remaining risk audit targeted transaction splits, authority projection and
provider selector identity rather than repeating only patch confirmation.
The first expanded regression completed 170/174, not PASS:
`a3-broker-restored-regression-174.trx`, SHA-256
`234588B6260D90006A8148097FC8D551E0C282CA509C4B208E86C82E57529660`.
All four new transaction cases wrongly assumed empty R1 tables. The actual
CreateDisposableCurrentDatabaseAsync fixture clones the current database WITH
its existing rows, so the integrated run inherited 20 authority snapshots.
This was a new A3 test-harness defect, not a product failure or an unrelated
failure to dismiss. Its reviewed precursor test SHA was
`82EAA5A593D5012B2B3ECC9AE6D6BB8A2CFE3339889291F7E914ABD95D30C9AA`;
the prior 21-case and four mutation results above belong to that test version.

Corrected only the new broker test: capture complete ordered JSON row sets
across six R1 tables plus key/object tables after seed and before B. Preflight
and failed transactions must preserve that exact baseline; success must add
exactly six rows, preserve every inherited row and preserve provider rows
exactly. Attempt SingleAsync reads are now scoped to the actual handoff source.
No database cleanup, migrated target, predicate/cardinality weakening, product
change or mutation of historical tests was used to hide inherited data.
Root-transaction/backend and specific B-R lock assertions remain intact.
Independent V2 plus free review of this successor test returned zero actionable
findings. Focused successor run is 15/15 PASS:
`a3-broker-history-aware-focused.trx`, SHA-256
`109EEF731C38C7DAD3BF38012693C8BD41FC9ED37F2805DF045BA836AA766A94`.
The four mutations were rerun individually against this exact successor and
restored byte-exact after each run; those results are not inferred from the
precursor evidence:

| Successor TRX | Observed result | Raw SHA-256 |
| --- | --- | --- |
| a3-broker-history-mutation-separate-br.trx | 0/4, all intended RED at missing B-R runtime lock | 61F93791CFFAF4230FC1811B8429DFBC4B933575183D73A8E12446FCC7659168 |
| a3-broker-history-mutation-commit-bb.trx | 0/4, all intended RED at active transaction expected 1/observed 0 | E91E16E63099A5478A1E31D9461A8D5E515B3AAECE44340708A13537FA2209D8 |
| a3-broker-history-mutation-old-sql-bounds.trx | 3/4, extended-bounds alone RED at expected Handoff/observed Final | 06797AAE0BF9DD13DE13AAE5386E2787E430C29F2BDF05F7C2ACFA3440A84204 |
| a3-broker-history-mutation-latest-selector.trx | 2/4, success/extended alone RED at persisted v1/configured v2 | B2251B636D61D93D94B0AD0E4E562BFF605A4F50A60A0FB8919D6AA98E59F60D |
| a3-broker-history-final-unit.trx | 324/324 PASS; restored product, existing current Unit build | 276E6034F408C8F34839247B3B203EFC4D4E47CB49913E9914CD15EE485C5E04 |
| a3-broker-history-final-arch.trx | 153/153 PASS; successor harness/ledger, existing current Arch build | D550053223FB17AD050E601B60670073F06A75E514A255DA985B37544F74EADF |
| a3-broker-history-restored-regression-174.trx | 174/174 PASS, all executed, zero failed/skip/abort/error/timeout | 6B81CC961AFF084ACD3ABA3BAEDEAFB99F18FD5FEBE5C83B1B76C575AB7D079F |

The restored 174-case group completed PASS. Exact test-name multiset comparison
against a3-r2-projection-regression-169.trx shows zero removals and exactly five
additions: BrokerSettingsRejectUnqualifiedBoundsBeforeOpeningConnection and
the success/preflight-failure/commit-failure/extended-bounds transaction theory
cases. No mutation remains in the facade or migration; their exact current
hashes still match the table. This regression gate is CLOSED, not an open
allowlist/decision request. The failed precursor evidence remains failed.
Independent successor evidence review inspected all four new mutation TRXs,
their exact assertion failures/counters, seven current source hashes and ledger
scope, and returned zero actionable findings. Reviewer did not execute tests.

This closes neither broker/recovery nor A3. Private HTTP/host and role-login
qualification, RE01/NPS01/TI01/TI02, full R2-R6 composition, C1/C3 and Agent
retained/receipt remain OPEN. No A4/production, stage, commit, push or GDrive
synchronization was performed; both repositories' indexes/conflict sets remain
empty and unrelated changes are preserved.

## Private metadata HTTP and prepared host — 2026-09-14, partial A3.1

The preceding same-B section and its hashes are historical checkpoint evidence,
not the current hashes of files extended below. Authority remains ratified A3
v0.6 plus its already-approved bounded corrections. No normative companion,
historical migration, existing project reference or solution was changed here.
New host dependency direction is exactly Host -> Application/Contracts/
Infrastructure; no reverse reference or new package. Its explicit project build
passes. Activated LOGIN/provider qualification, public API admission/pipeline
wiring and all recovery transitions remain OPEN, not an implied host activation.

Implemented the closed 23-field metadata transport and 11-field Handoff union,
strict UTF-8/duplicate/unknown/null/size/depth validation, phase-aware Final
projection, single-send bounded private client, socket-peer/Host enforcement and
the prepared thin host. Origin is in-process SQL-branch provenance, not an extra
wire field. The codec does not replace CRT1 canonicalization. No raw Stream is
accepted by the broker port; the transport Stream contains JSON metadata only.
Missing explicitly qualified composition services fail closed with empty 503;
the product host does not register fixture defaults. Current real-socket tests
instantiate Kestrel plus the shared transport facade, not the standalone host's
as-yet-unqualified LOGIN/provider composition. Role-scoped PostgreSQL tests are
not proof of an independently authenticated production LOGIN.

Real HTTP success and deferred-commit-failure cases retain the existing root
transaction/backend/lock and complete historical-row-set assertions. Aborting
the response after B commit gives one failed client operation and one durable
attempt observed on a separate connection. An explicit retry returns Busy and
preserves that complete attempt row; there is no automatic retry or new Handoff.
This proves lost-response admission behavior, not RE01/NPS01 settlement.

The initial PostgreSQL run was 24/25, not PASS: its new response-loss test compared
two EF entity object references. Replaced only that assertion mechanism with an
independent observer's exact persisted to_jsonb row equality. The corrected
25/25 run retains existing row-count/tuple/history assertions. The test named
PrivateUnionRejectsMalformedHandoffWithoutBodyRead currently proves codec shape
rejection only; public raw-stream zero-read proof awaits the API adapter join.

| Current source / consumed port | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Contracts/RawExport/RawIngressBrokerContracts.cs | BB1727320224B26B57DDAC756E06B2DD80DE3CC2A0D442DC26A1167B9D1694A8 |
| src/TagEkyc.Application/Ports/RawIngressBrokerPorts.cs | AC6448EA4C512B89378DD161D9F2A96D2C1AD9ACDDEA03CC1B58A367743E5D78 |
| src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerTransactionFacade.cs | 8ADCA9B698B0E738B641BE281686669C2E79F47CFC191EE3C30249AFE2CB5C09 |
| src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerOptions.cs | 4DE7BEBDA00DEB8A2657BDBF0A6D6F601D5FC71143E4170F9830867B6AC49496 |
| src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerHttpClient.cs | B30A3172E37EC817C89FCC6879DFD62F26962689B808C462BE753CEC3212B9CA |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs | F4630910FFB62666DAADFDB55A1AA83E58726DA6FAF0E99D2F48154E636973EF |
| src/TagEkyc.RawIngressBroker/TagEkyc.RawIngressBroker.csproj | 0FAD4364039F886ECEA24273CCA0BE887753142EBDF4CE49BBD424436C11F6F8 |
| src/TagEkyc.RawIngressBroker/Program.cs | 116FF1396B39CA0B8B4D92802D4979EEC4FA1D334CD533F9E691E51C013E483F |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs | 8F8A28E29C776413217FEF2B09BCBD1349386A747A7DCD38D5656BB2BF1F4C89 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | D75BCC90DF2017A86723715D053D9BD9EC1574E9981A9250B8F922AF83D53908 |
| tests/TagEkyc.ArchTests/Tip88C1C6BA3CompositionArchTests.cs | 53FDDF7938AC12C79FDF06B96D6C8E363B2C64C1FCBE71824E2DB2397160F835 |

Evidence lives in each named test project's TestResults, not repository-root
TestResults. Each mutation ran individually against the same seven HTTP tests;
both changed product files were restored to the full 64-character hashes above.

| TRX | Observed result | Raw SHA-256 |
| --- | --- | --- |
| a3-private-http-initial.trx | 7/7 PASS | 5C13AC8D545BED2A523B236FE40A6391279FA2D0D599160858D0DC642A894520 |
| a3-private-http-postgres-initial.trx | 24/25, new helper reference-equality failure | 162EE9C702679227283B3353E5D9ECBE98189A19221FD9D42F88147D3252F110 |
| a3-private-http-postgres-restored.trx | 25/25 PASS | 29B17B915AA44A96D6C6220EA2D6F18D54BDFBD5966154378412209454C749E4 |
| a3-private-http-peer-red.trx | 6/7; removed peer check: expected403, observed200 | 600A165EB69D5F053D0A16CFB21844883F85919C43A2E8083F3AD60FB21B5C95 |
| a3-private-http-duplicate-red.trx | 5/7; last-value-wins: request and Handoff duplicate guards both RED | D8C62255402197742914C0DA2F2659ACADFC39E9B2A1677FD4B1772503345FBF |
| a3-private-http-phase-red.trx | 5/7; removed origin check: fresh terminal accepted in codec and HTTP | 6D7EF2F2C8C87A8F196B69776EECFED0B523C3889700F17EFA31E45565F5E228 |
| a3-private-http-mutations-restored.trx | 7/7 PASS, restored exact product bytes | DE23B3666F20398E7352AC9B981C6E3AE62DBA2AE34EF389104B12BF82F2420D |
| a3-private-http-final-unit.trx | 324/324 PASS, existing current Unit build | C03A65D6F82F8EDC350E4AC5606E8FBCA9B16DC6CA032437F676FDAF5FCF8011 |
| a3-private-http-final-arch.trx | 155/155 PASS, two new boundary proofs | 0EA0853C06CFEF61D3B054E9F2DC8D18A9AF8BA4AAB6A4F04EF49AFAD9B908A7 |
| a3-private-http-regression.trx | 184/184 PASS, all executed; no failure/skip/error/abort/timeout | FB11B9972085B5F3FADDD36D820B1E99BF4953E0B7FC3D6C017E09DF817ECA8F |

V1 independent deep review returned 0 HIGH / 0 MEDIUM / 0 LOW on the frozen
slice and adjacent A1 caller/B-C SQL. Risks examined: socket spoofing, phase/
SQL-field leakage, and hidden retries/premature handoff. No V1 patch was required;
V2 correction verification is therefore not applicable. Independent free V3
read all eleven source/port/test artifacts and adjacent callers/providers/SQL,
verified mutation failures and exact restoration, and also returned 0 HIGH /
0 MEDIUM / 0 LOW. Neither reviewer executed tests or touched PostgreSQL. Two
review passes converged without findings; no round-3/5 escalation or authority
expansion was needed. The explicit remaining timing-proof limit is that the
client deadline covers the complete response read, while the host's final write
uses RequestAborted: no separate slow-flush timing proof is claimed here.
Expanded regression completed 184/184 PASS. Exact sorted test-name multiset
comparison against the previous 174-case result has zero removals and ten
additions: the seven private HTTP cases, two HTTP transaction theory cases and
the committed-response-loss case. All eleven current source/port hashes above
were checked against files with full 64-character equality and an explicit
eleven-row cardinality assertion. An initial malformed regex census was discarded;
this successful check parses literal table delimiters, not empty regex matches.
This is PASS for the private metadata transport checkpoint only. Prepared-host
activation qualification, RE01/NPS01/TI01/TI02, API/R2-R6 composition, C1/C3 and
Agent retained/receipt remain OPEN. No new authority question or RRI is required
by this checkpoint. No stage/commit/push/A4/production action occurred.

## Broker LOGIN / provider qualification — 2026-09-14, partial A3.1

This increment qualifies the explicit synthetic host composition, not production
activation, object-provider scopes, the standalone executable's deployment
bootstrap, or the unfinished R2-R6 pipeline. The product Program remains
Prepared/fail-closed when its explicitly owned services are absent. It does not
load tests or select fixture providers. The test-owned Kestrel host uses the
production DI registration, transport, qualified broker and same-B facade.

The already authorized `tagekyc_raw_export_claim_broker_login` is provisioned
only inside the disposable PostgreSQL fixture. It really authenticates as that
LOGIN; no SET ROLE masquerade or production credential is used. Qualification
reads actual session_user/current_user, LOGIN/capability attributes, exact two
transitive memberships, administrative grant options and schema rights. It
checks three exact B function signatures, owner/SECURITY DEFINER/search_path,
direct grants and effective EXECUTE in both directions. A frozen inventory of
36 stage-function names plus the C6B direct core forbids every overload even
when the legitimate stage ACL is removed and replaced with a broker grant.
Direct LOGIN table/column grants, ownership, inherited table DML and inherited
or PUBLIC column DML are rejected. This SELECT-only qualification is before B;
the same-B facade still executes B-R/B-B/B-C without another business boundary.

Configured commitment/subject versions are exercised through the actual keyed
services with empty public, non-patient probe bytes. An unavailable key is not
successful DI readiness. Profile/KEK/time projections are validated and frozen
once for the request; the facade cannot observe a changed second getter after
qualification. Neither the probes nor catalog qualification creates history.

`BrokerQualification_RealPrivateHttpCommitsAsDedicatedLogin` observes
session_user from a trigger at actual attempt insertion, then reads the durable
attempt from a separate observer. The HTTP receipt matches that committed tuple;
exact retry returns Busy with no second insertion. This is not an RE01 recovery
proof and does not yet touch the public raw request stream.

The 21-case denial theory starts each case with a real qualified positive,
introduces one role/ACL/provider defect, checks empty HTTP 503 and the complete
six-family durable row set unchanged, restores the defect and requalifies.
The added inverse-membership, inherited/PUBLIC column, moved-stage and direct
core cases came from independent review rather than relaxing a failing check.
An intermediate 19-case run failed at SQL parsing (42601, extra parenthesis),
not at business semantics; that implementation defect was fixed and retested.

| Frozen current source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs | DF917CC6D41063D8052864E2E214D08012A37ADCD4CFE8A3D286C6FACC75289B |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs | FD6539F3F3279C4ECF6C4B9CFC84FCF2EC68436AA61943389193F3169058E801 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 41141E6F1AD335AE7DDFC484D33C6E61A1E719065B9F04A2D41407FB5B9747DF |

Other source rows in the preceding checkpoint are unchanged. Mutation probes
altered only the product composition and restored its complete raw SHA above.
TRX files below live under the named project's TestResults directory.

| TRX | Observed result | Raw SHA-256 |
| --- | --- | --- |
| IntegrationTests/tip88c1-a3-broker-qualification-v3.trx | 22/22 PASS | F0265570925BF23E7B603F0E8ACE60C6E6AFEA2190C51A9469998C0EEEAF26C4 |
| IntegrationTests/tip88c1-a3-broker-qualification-dml-red.trx | 18 PASS / 4 intended RED: direct table/column and inherited/PUBLIC column | 10EDAD6E4C3A488A4763DB3056157668015B4D6ACCF844E2F8C1C4EA4304F4C7 |
| IntegrationTests/tip88c1-a3-broker-qualification-functions-red.trx | 16 PASS / 6 intended RED: inverse membership, PUBLIC, search_path, stage/add/move, core | 4EBC70CDD1C00048766BDFBFE0339A8D2B81067093CB345907812D925BCE68C5 |
| IntegrationTests/tip88c1-a3-broker-qualification-provider-red.trx | 21 PASS / 1 intended RED: unavailable configured key | 986D410704717604AAC3A811F2D17A068C928E0D20DD1931125937E093E4ED25 |
| UnitTests/tip88c1-a3-broker-qualification-unit.trx | 324/324 PASS after exact restoration and solution rebuild | A803563FE4A28491AA6EAD3F1F57C6B052727DA7FA25BE0A76F38C28BB12AEBA |
| ArchTests/tip88c1-a3-broker-qualification-arch.trx | 155/155 PASS after restoration | C669179210D4FFA763CC587D6DBDF5ADAF74CAE1C76B38A62C08C3F5C748198A |

Each mutation's failed assertions were Expected 503 / Actual 200, not setup or
SQL syntax failures. No test was removed, skipped or weakened. Solution and
standalone broker builds passed after exact restoration. Expanded integration
regression and frozen V2/V3 review are pending at this ledger checkpoint;
their results must be recorded before calling this increment closed.
Recovery RE01/NPS01/TI01/TI02, API/R2-R6 composition, role-specific object-provider
qualification, C1/C3 and Agent retained/receipt remain OPEN. No stage, commit,
push, A4 or production action is authorized or performed.

### Qualification correction convergence and final frozen candidate

The preceding 22-case source/evidence table is an intermediate checkpoint, not
the final qualification candidate. Its later expanded run
`IntegrationTests/tip88c1-a3-broker-qualification-regression.trx` passed 206/206
with SHA `30A2EB537782023A7E74CC728BA8B524E4307EA911C544669C56B63025B1A843`.
It ran the earlier DF917CC6 product bytes; it is not attributed to the final
source below.

Four source-review iterations were needed. The cumulative nonconvergence cause
was an incomplete permission-graph model: deriving function identity from live
ACLs, applying deployment-owner exemptions before named denials, then confusing
immediate EXECUTE with SET ROLE reachability. Corrections changed the review
method to a source-frozen function census, explicit forbidden-capability
precedence, and membership reachability independent of inheritance options.
The new counterexamples include owner inheritance, forbidden-role SUPERUSER,
and both owner/broker `INHERIT FALSE, SET TRUE`. The latter first assert actual
EXECUTE=false and SET=true, then require empty503 and unchanged durable rows.
The qualifier does not exempt these seven forbidden capabilities as deployment
principals. Legitimate deployment principals remain separate.

Independent targeted and free reviews returned 0 actionable findings on the
corrected product. A final census fact verifies all 36 migrated stage names plus
the core against the frozen 37-name inventory, guarding future additions. It
was added after one reviewer released the freeze while the other was still
reading: that coordination mistake was disclosed, the new full test SHA was
supplied, and the second reviewer reread the entire current test file before
returning its clean verdict. No verdict on older test bytes is represented as
review of the new fact. Neither reviewer ran builds/tests/PostgreSQL.

| Final frozen source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs | 1D1F1A61A580B01EEAC7FE1747C2137FC77A0BA4BE3E40043BE739E0DB4C2B9F |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs | FD6539F3F3279C4ECF6C4B9CFC84FCF2EC68436AA61943389193F3169058E801 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 53CAC7E40016EBE0BCE61273FD29D3853EA65907357D24C3E2F5F4EB262909BD |

| Final-candidate evidence (each project's TestResults) | Observed result | Raw SHA-256 |
| --- | --- | --- |
| IntegrationTests/tip88c1-a3-broker-qualification-final-green.trx | 27/27 PASS | 52C1B65E9903283453C3390824DD873D47F270915C282E81B78E51001B23141C |
| IntegrationTests/tip88c1-a3-broker-qualification-final-functions-red.trx | 17 PASS / 10 intended RED | 40D1CD33E5294CD2436D39A9A7686FAC701EFD15DD2C897A73CE16A8C1B7B6C4 |
| IntegrationTests/tip88c1-a3-broker-qualification-final-dml-red.trx | 23 PASS / 4 intended RED | 6CAFF89FF5A5EF5B72BDB4397A18ED4EA8F15E432ECA98D82AD41E74DF755E99 |
| IntegrationTests/tip88c1-a3-broker-qualification-final-provider-red.trx | 26 PASS / 1 intended RED | C15DC1CAE33DEAE02D3DA6C9763CC762584070EFB0D5F9666B1AF2F4542078E5 |
| IntegrationTests/tip88c1-a3-broker-qualification-final-census-red.trx | 25 PASS / 2 intended RED | B06E8B408D94C79CAEA22B0A2B43130D4E4143DDD00FC1B6DE14FE4AE1EC01F4 |
| UnitTests/tip88c1-a3-broker-qualification-final-unit.trx | 324/324 PASS after restoration/rebuild | 56CF5D0DC42438F262BCB6EC5C7C9F44FE048AA4F0BAA160CFA90237E0805722 |
| ArchTests/tip88c1-a3-broker-qualification-final-arch.trx | 155/155 PASS after restoration/rebuild | 5AF6AFFBD7A518D1946532AF71550DC360C697ABDE0EB67A96928F539020DC1A |
| IntegrationTests/tip88c1-a3-broker-qualification-final-regression.trx | 211/211 PASS, zero failed/skipped | D4773E4522AE876EA228E5B4DF7A66828C5D4EF9324D106DDAE0AE6755BE7A2F |

All four mutation runs have the same sorted 27-test-name multiset as the green
control. All failed assertions are Expected503/Actual200, except the census
comparison at its exact collection-equality assertion after changing only the
core name. xUnit truncates that long string in the message; an initial audit
looking for its full suffix was discarded and replaced by checking the named
assertion/stack and exact mutation plus source restoration. No fixture failure
is counted as intended RED. The complete product raw SHA above was restored
before rebuilding solution/broker and running final Unit/Arch and regression.
Final expanded regression passed 211/211 on the restored current candidate.
Its sorted test-name multiset equals the previous private-HTTP regression's
184 names plus all 27 final qualification names, with zero missing/extra names.
All 211 executed; no skipped or failed case is hidden by the aggregate count.
The final three source hashes remained exact after this run. Both repositories
remain staged=0/conflicted=0; existing unrelated dirty/untracked work is preserved.
Bounded disposition: PASS for synthetic real-LOGIN/catalog/provider qualification
and its private HTTP composition proof, not overall host activation or A3 PASS.
Standalone deployment bootstrap, role-specific object-provider qualification,
recovery and overall A3 retain the OPEN dispositions above. No production or
landing authority is inferred from this synthetic checkpoint.

## NPS01 — retained no-provider-start operational checkpoint (validated; full recovery open)

Continues the ratified broker §3.3 and CP04 contract; no new RRI or semantic
decision. This increment changes the existing A3 forward migration, the broker
forbidden-function census and its test, and adds the allowlisted
`Tip88C1C6BA3R2TerminalProjectionTests.cs`. No historical migration, model,
Designer, package/project or Agent file is changed by this increment.

| Invariant | Executable owner / observation | Discriminating control |
| --- | --- | --- |
| Unstarted is not unsuccessful | NPS01 repeats all four absence queries, including all allocated identities; guard repeats conjunction | Actual prepare/unknown Wrap produces durable evidence and must reject NPS; weaken conjunction -> RED |
| Lease is post-lock | Retained prepare takes actual session prefix, attempt/head/reservation/key locks, then fresh database clock | Hold prefix across original 30-second lease; actual provisioning wrapper returns HeadNotReserved with Wrap count zero; old clock -> RED |
| Operational witness only | Exact NPS deployer context permits only two-column NULL->TerminatedBeforeStart/time update | Observer compares complete attempt excluding those two fields, all other custody/provider rows unchanged; exact replay preserves timestamp |
| No competing entry bypass | Retained prepare, begin-object and operational terminator acquire the same prefix; old semantic writer rejects retained | Actual PostgreSQL wait in both prepare/NPS orders, no stale provider start |
| Role isolation and reversible migration | One new reconciler-only callable, broker denied; five existing function bodies replaced/restored and exact LF body hashes pinned | Registered EF apply/rollback/reapply plus broker frozen census; no .gitattributes changes |

Full RE01, TI01/TI02, CP08/CP09, process-kill-to-Available recovery and R2-R6
composition are NOT claimed by these SQL proofs. The existing synthetic broker
commits R1 through the real dedicated LOGIN and B façade before the tests call
NPS through the reconciler capability. No test seeds a key or object merely to
represent absence; current consent withdrawal is not permission to erase prior
resource evidence. The original business horizons remain unchanged.

V1 found two actionable propagation/proof gaps: incomplete post-prefix lineage
revalidation and unexecuted begin-object/terminator replay prefixes. Both were
patched across the four retained callers; the new adjacent proof uses real
fixture KEK activation, object initiation/NotArmed and operational termination.
The migration roundtrip body census expands 12->17 for all five replacements.
V2 verified those changes and found one false-green ACL test using three
nonexistent role aliases. It now uses the exact A1 role names and asserts all
eight selected roles exist before testing privileges.

From round 3 the cumulative correction focus is explicit: propagate a lock
invariant to every callable in its finite census, execute each changed path,
and assert the input census before a filtered-query equality can appear green.
This is implementation/proof correction, not a new decision or RRI.

The initial execution was aborted after seven reported passing cases when the
race harness hung during teardown; it is NOT a PASS or intended mutation RED.
The harness now releases its held transaction before disposing blocked callers,
so the underlying assertion can be reported rather than concealed by teardown.
Compiler/harness defects are FIX+RETEST. At that initial checkpoint, final
execution/mutation/review results were pending. No staging/commit/push or
production action has occurred.

The released race diagnostic then failed 0/2 with PostgreSQL 42501: NPS had
requested FOR UPDATE on immutable raw_export_authority_snapshots, whose deployer
rights deliberately allow SELECT/INSERT only. Correction reuses the existing
shared `tip88c1:b2-authority:` scope key after session/reference and before attempt
locks, adds CaptureAcceptanceId/RawClass to both exact probe/rejoin projections,
and uses an ordinary exact snapshot read. No table UPDATE grant, owner change,
new helper or current-consent requirement was added. This is an executable lock
mode correction within CP04, not a new authority. The focused rerun is 14/14
(eight NPS plus six registered migration cases), zero skipped. V3 free adversarial
source review read all changed functions/restores/proofs and returned zero
actionable findings; it independently checked all six current and five restored
function body hashes. Runtime mutation and final regression results follow below;
the source review alone is not execution evidence.

### NPS01 discriminating execution and frozen candidate

Each valid mutation below changed only its stated SQL slice, rebuilt the test
assembly, and executed against the registered current EF migration on a fresh
synthetic PostgreSQL database. No test, assertion, timing budget or fixture was
weakened to produce RED. The complete migration restored after each mutation to
`99A2E0BC1940CC70FF7BB750B55E03C50339B62947EE189F5147E34D3049A64C`.
The installed-body comparison manifest was not repinned to mutated SQL; these
focused transition tests do not invoke Down. Normal Down/reapply is tested only
on the restored candidate.

| Mutation | Exact mutated migration SHA | Observed intended failure |
| --- | --- | --- |
| Retained prepare uses statement timestamp after waiting | D7808796544E11A1416EDC9873722792A325834C0CB884A1C89EB1FDA350F0A9 | 1/1 RED: Expected HeadNotReserved, Actual ProviderOutcomeUnknown; actual provider was reached after lease expiry |
| Four absence clauses AND->OR in both NPS and its guard | AFD102B3F0E6BD77CC13B83DFA221CBCC426E23E44B21B3DFA33102803F78BBD | 1/1 RED: Expected ProviderEvidencePresent, Actual TerminatedBeforeStart; key/provider evidence was not absence |
| Remove session/reference prefix from object begin and operational termination | 16EF192B950A24865C01FD6A02E4E6CEF24EF025FDAC1D69C82B7751F0C409DE | 2/2 RED at named actual-PostgreSQL-session-wait assertion, for object creation and already-terminated replay |
| Grant NPS EXECUTE to the three actual A1 application/authenticator/operator roles | 7299AA4C1D6BFC4ADD5909C1BCD3A44F6F6CD586C161BB58F3521003C80BE797 | 1/1 RED: effective role set includes those three extra roles, not just reconciler |

| Evidence (IntegrationTests/TestResults unless indicated) | Result | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-nps-focused-green.trx | 14/14, zero failed/skipped | 388E873B5173D2E6D1C658066CA9B718720EE8257B044BA1B9854C0E0DEC11DD |
| tip88c1-a3-nps-clock-red.trx | 1 intended RED | 56413A64E858495C2D776AF2674DE97AA429CD14DB8F78BA62F87F3F7CB2F88D |
| tip88c1-a3-nps-absence-retry-red.trx | 1 intended RED | 468394EB84C0FAC6BEFA98ECF13EB57523F679195F06D20BBFD0F8B5D9445CDB |
| tip88c1-a3-nps-prefix-exact-red.trx | 2 intended RED | AF11DDF23849DB280FE8060AD4B3BB057278AEE2D186F7A9ECF2EACD22BA6E14 |
| tip88c1-a3-nps-acl-red.trx | 1 intended RED | E05829DE95837AC1109B2038CD84BEB808D7BC27A21A06BBBFA3772CF072C76C |
| UnitTests/TestResults/tip88c1-a3-nps-final-unit.trx | 324/324 after exact restoration and solution rebuild | ECE0BE36527A95763A8BAEDD64A87E36E675C3A50CA9F7E65C9EA5DF64C6DA91 |
| ArchTests/TestResults/tip88c1-a3-nps-final-arch.trx | 155/155 after exact restoration and solution rebuild | 5F33E731CD8B68747D72A352880083FCECCEEE6B35A77E8737E396452E2D8B69 |

Excluded diagnostics remain available and are NOT counted as intended RED or
GREEN: `tip88c1-a3-nps-initial.trx` (aborted run with seven reported passes;
SHA `3D2D08C6A9E0597C07D87C4138AFA2BCD77E94F9278E842033779BFFB0EDFDC0`),
`tip88c1-a3-nps-race-diagnostic.trx` (42501 immutable snapshot lock;
`DEEDEC00DBA8178124E4FE37C69B04FB97D430D4144FF21F646C5EC062181054`),
`tip88c1-a3-nps-absence-red.trx` (fixture connection EOF before invariant;
`D7ED9B387BC5D37FB2975ED12DB4424BA409B2E4285C1DD54EC9340F24D18ACE`),
and `tip88c1-a3-nps-prefix-red.trx` (initial mutation cut at an inline END IF,
causing SQL syntax failure; `D46C8D9AACF3226BCAFDC8798F56E95C6F802C81F98610ED7A32C1833C2A62C7`).
The latter mutation was corrected to the complete prefix block before its
valid RED rerun; the fixture EOF reran unchanged. Neither failure is disguised
as a successful negative control.

| Frozen source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | 99A2E0BC1940CC70FF7BB750B55E03C50339B62947EE189F5147E34D3049A64C |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs | 9ACDC2110A773ED440912E66271DE7EBA3E8595673815E5A9A5F4E7E36EA11D7 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs | 6606F741722749C0F541D7CBEF73E53C491965A3A9A867FA72F1ECD5183015F1 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs | C113EB9E5B81F4379B43186399A000C2C5C2141B7A3B1694D66A4E8241DBD22C |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 4E6D13D25720127F64F8353F4A6378174AA46F076961F7B8B9CEE8A4FA908A2A |

Restored solution build: PASS, zero warnings/errors. Final expanded regression
ran against those frozen bytes and found a genuine propagation regression:
201/219 PASS, 18 FAIL, zero skipped. All 18 failures are the existing retained
real-ciphertext setups: Expected PendingVerification, Actual PreCustodyRetryable.
The failed run is `tip88c1-a3-nps-final-regression.trx`, raw SHA
`79F9EE25C35C87A2D2E193F58AF7BA0FB4D3A820A0AD0F1663D3AD427D8A9DA4`;
its filename does NOT make it a passing final gate. It is not reclassified as
unrelated or as an intended mutation.

V4 cumulative diagnosis adds the missing cross-call obligation: NPS-focused
role connections preset the actor, while the real R2 orchestrator established
the same actor only after calling key preparation. The new retained prepare
predicate therefore rejected real pipeline calls before provider start. This
was Builder integration ordering, not a new business contradiction. Exact
inventory v0.6 already authorizes `RawExportR2EncryptionOrchestrator.cs` (BR,
BP17-BP21); its pre-edit raw SHA matched
`B2FF9A3606D269308F2CDCD1EF7C95D43067CD53C7D42DA5727CFF54D575860D`.

The correction moves the existing SetActorAsync(request.ActorPrincipalId)
before ProvisionAsync on the same scoped connection. It preserves actor-error
PreCustodyRejected, caller cancellation rethrow, provisioning-error
PreCustodyRetryable, and removes the later duplicate setter. No ambient
transaction, new actor source, SQL relaxation or historical-test mutation is
introduced. The corrected orchestrator SHA is
`F3AF1876A1728DFFA50F109AAC9404529E0E37136FC0E1AF6CD7579941D4A023`.
All five earlier frozen SQL/proof source hashes remain unchanged. The four
controlled mutation proofs call their exact SQL/provider boundaries directly;
they do not invoke this orchestrator. Their evidence is retained, while the
real R2 regression must now be rerun on the expanded six-file freeze.

Round-4 improvement: source review includes the complete caller/callee actor
sequence and connection owner, not just a correctly authenticated isolated SQL
call. Existing retained encryption/withdrawal/CrossBorder tests remain unchanged
and are the discriminating integration controls. Checkpoint PASS was withheld
until their restored full regression and independent review finished below.

V4 free review returned zero actionable findings after reading the full
orchestrator/repository/provider, the shared writer-connection construction,
existing R207/R213 contracts, current NPS and the unchanged retained tests.
`tip88c1-a3-nps-actor-order-green.trx` is 6/6 PASS on exactly the six previously
failing CrossBorder cases (SHA
`6A0A8F65C9D00ACEB21417F0F76E82B3315B0CC36EC2EE8AB832CA863FC68369`).
The corrected solution rebuild passed with zero warnings/errors. Current
post-correction Unit is 324/324 in `tip88c1-a3-nps-final-v4-unit.trx`
(`F32E46CD20BBB77BCFDF2A84397D898FE453BFDBC78EDF20FC359F6F0CEDBD51`);
Arch is 155/155 in `tip88c1-a3-nps-final-v4-arch.trx`
(`61C9F2B2F0F054B75B95771452DB7C092B8FB2819342F382B86A9CFE4E1D54B1`).
Full post-correction regression was still pending at that point, not inferred
from those gates.

### Final NPS01 operational-checkpoint disposition

`tip88c1-a3-nps-final-v4-regression.trx`: 219/219 PASS, zero failed/skipped,
raw SHA `797B6B1D9B1DF8368E34F251DF01562D62460E2D6358326CEFB32BF6488C9421`.
This includes all 18 unchanged real-retained tests that failed before the actor
correction, now passing. All eight NPS cases executed again after restoration.
The exact ordinal `(testId, testName)` multiset is the previous qualification
regression's 211 plus the focused baseline's eight NPS cases: 219 unique pairs,
no missing/extra pair. Display names alone contain one repeated xUnit-truncated
JSON case name; multiplicity is preserved and the full IDs distinguish it.
An initial culture-sensitive display-name ordering comparison was discarded;
ordinal multiset and full-ID comparison both match. No count-only join is used.

Additional unchanged R2 compatibility controls R207 and R213 execute on the
corrected orchestrator: `tip88c1-a3-nps-r2-boundary-regression.trx`, 2/2 PASS,
raw SHA `7FCC865D020029D9D0620FE4D54D3ED9C46FF82D32AA2CDF6909A32D88D104F4`.
They preserve provisioning-before-active-context result partition and prove
no database transaction spans source/key/AEAD/object I/O. These are regression
controls, not new parallel R2 implementation. Test names/assertions were not
modified.

Final six-file source freeze is the five-file table above plus orchestrator
`F3AF1876A1728DFFA50F109AAC9404529E0E37136FC0E1AF6CD7579941D4A023`.
V4 free source review has zero actionable findings. Four controlled SQL
mutation groups have five intended failing cases and exact restoration;
the actor integration regression is separately recorded, not hidden or counted
as one of those controlled mutations. Build, current Unit 324/324, Arch 155/155,
full regression 219/219 and R207/R213 2/2 are green on this freeze.

Disposition: PASS for the NPS01 operational SQL/entry-sequencing checkpoint.
This is NOT BP21 full process-kill-to-Available recovery, NOT RE01, NOT
TI01/TI02, NOT CP08/CP09, NOT complete R2-R6/host composition or A3 overall.
Next implementation is the ratified retained-only RE01 insertion in B-C,
then the remaining recovery/composition obligations; no new RRI is required
by this checkpoint. C1/C3 and Agent retained/receipt remain open. Synthetic
only; both repositories remain unstaged/unconflicted. No stage, commit, push,
A4 or production action is authorized or performed.

## RE01 — same-owner reentry operational implementation (in progress)

Continue under the existing ratified A3 v0.6 authority. The new retained-only
helper is internal to the existing B-C function after its exact comparison;
there is no new facade call, public route, provider, table, index or stage grant.
Reuse the existing C1 attempt fingerprint recipe and the production-supported
`fixture-nonce-random96-v1` / `none` profile. Preserve all original business
horizons and renew only the derived reservation/new attempt ownership lease.
The fresh attempt/head CAS/reservation update commit in the same B transaction.

Affected paths are the existing A3 migration, R2TerminalProjectionTests,
MigrationTests, broker qualification composition and BrokerPipelineTests.
No model/Designer, historical migration, Agent or project changes in this increment.
Broker forbidden-function census is 37 stage functions plus two owner-only
functions, not a new EXECUTE grant. Roundtrip body census expands 17 to 19 for
the outer B-C wrapper and head guard; helper removal is verified by reapply.

V1 independent source review found two implementation defects (the new helper
probe reused row-variable names as SQL aliases; the changed/new bodies had not
yet been added/rebound in the migration drift manifest) and one negative-result
proof weakness (accepting any Final instead of exact ReservationBusy). Initial
execution `tip88c1-a3-re01-initial.trx` was 3/10 PASS, 7 FAIL: three Down/body
manifest failures and four PostgreSQL 42702 alias collisions. This is actual
implementation failure evidence, not an intentional mutation run or unrelated
baseline failure. All three V1 findings were patched together inside allowlist.

New operational proofs cover no-provider-start retry, concurrent retry,
provider-unknown exclusion, exact internal actor/binding/tuple/role boundaries,
and positive key-present settlement after actual revocation. The fifth proof
first retains an Active key with NoObjectEstablished + operational termination
and requires O16, proving operational termination alone is insufficient.
The remaining BP20/BP21 process-kill and complete R2-R5-to-Available assertions,
TI01/TI02 and CP08/CP09 are NOT claimed by these operational tests.
Execution and V2/V3 closure remain pending below; no overall A3 or landing PASS.

V2 source review closed the three V1 findings. Executed V2 was 8/10 PASS:
all six migration cases, internal-helper guards and provider-unknown exclusion
passed; two retry tests incorrectly expected B-C O16 while the same alias still
had an Active B-B comparison evaluation. The raw V2 TRX SHA is
`7A88CBD726D6C1409188E12119F00984BC606BD411815CDB61602E53CE34AA9B`.
Initial failure TRX SHA is
`346044C977C27E874B84A629E27B15B4673C6AAEBD661648A3E16B138EE808F1`.

V3 free adversarial review read the full helper/wrapper/guards/restoration,
adjacent B-B/completion/NPS/key/object paths and proofs. It found one Medium
test phase-precedence defect and no additional product defect. The same issue
would affect the fifth settled-key case, not yet compiled in the V2 run.
Correction preserves B-B: exact O10 Final with the persisted alias's retry
timestamp and no other payload, actual wait beyond that observed timestamp,
then exact O16 while the new ownership lease remains live. The key-present
positive also waits past its prior comparison evaluation before reentry.
No arbitrary Final/old-or-new outcome acceptance or B-B mutation is used.
BP20's literal concurrent-loser O16 and full Available/process-kill proof are
still OPEN; the current broker-level same-alias loser reaches the earlier
evaluation gate. These operational results must not be labelled full BP20.

Round-3 accumulated non-convergence causes: manual current/restore/manifest
projections caused drift; new PL/pgSQL row variables collided with aliases;
proof expectations collapsed B-B and B-C phases. Review improvement is full
literal/body-hash joins plus executable migration roundtrip, unambiguous SQL
aliases, and exact phase/result/deadline assertions with observer reads.
Apply all verified findings together; do not add another prose authority or
weaken production semantics to make the tests green. V4 bounded source review
and the rebuilt 19-case NPS/RE01/migration execution are in progress.

V4 bounded review caught a new test-query type mismatch before accepting the
phase correction: the alias idempotency column is UUID, unlike its producer
and installation text columns. Passing its UUID-N string made PostgreSQL report
42883 (`uuid = text`). Correct only that parameter to the existing Guid.
V5 checked all new query/result types against actual DDL/signatures and reports
zero actionable source findings. Current test SHA is
`5AAEEF15B17F90E2379E93D55E5DD693187A43A5672A3633C9C90C57CAAE9FED`;
product migration remains
`CC1D52FBBC929F9D565DCDBDBFFC4966D4EC12204135253043183150B0884375`.

Mandatory round-5 cause check: the fourth accumulated cause is conflating
wire UUID-N grammar with database UUID parameters. Derive each proof parameter
and return type from executable DDL, not the nearby transport format. No new
semantic decision or wider allowlist is needed. Source review alone is not
execution PASS; actual current-binary PostgreSQL and mutation/restoration runs
must finish before checkpoint closure. The already-running V3 binary retains
the former query error and will be recorded separately, not relabelled current.

Executed V3: 19 total, 16 PASS / 3 FAIL, all three at the UUID/text proof query;
TRX `tip88c1-a3-re01-v3.trx`, raw SHA
`18A8C8C2ED264A9DB23DAD746CE4F9940F7CDB3FBA42A4D57C58296916DE2BF8`.
After the typed Guid correction and rebuild, all five RE01 operational cases
PASS in `tip88c1-a3-re01-green.trx`, raw SHA
`E74872F4C33328B063E81721224172DE2DBF6D0156DE505CF3F623BDE72B2F42`.
The two prior exact-shape failures now distinguish B-B O10 and later B-C O16;
the Active-key negative and actual-revocation positive both execute successfully.
No test count reduction, skip or product B-B semantic correction was used.

Controlled mutation evidence (each starts from the same reviewed five-test
source; only the named migration predicate/projection changes):

| Mutation / TRX | Executed result | Exact failure target | Raw TRX SHA-256 |
| --- | --- | --- | --- |
| `tip88c1-a3-re01-old-branch-red.trx` | 5 total; 2 PASS, 3 intended RED | Restore old unconditional ExistingMatch/O16 by disabling only retained B-C reentry; concurrent, lost-R1 and settled-key positives lose their Handoff. Helper-boundary and provider-unknown controls remain green | `9879E28657368E9DA6923017078A4EDF7F641DDEA65800F5055D34916A54A639` |
| `tip88c1-a3-re01-live-key-red.trx` | 1 total; 1 intended RED | Admit Active alongside Revoked/ReservationAbandoned in both RE01 settlement checks; exact first O16 becomes Handoff before revocation | `4C7410DB11014D840AB22D008759F1F277609297FDE6B8F1B4CC44E64F28809D` |
| `tip88c1-a3-re01-old-lease-red.trx` | 1 total; 1 intended RED | Omit derived reservation lease update after head CAS; independent observer's exact new-attempt/reservation projection is false | `C5EE3ABB2A841AD582F10CB07479115E887F4AB7C260FDDC9597D764754539D4` |

Mutation migration raw SHAs respectively are
`513C1732E0F85325F0268C9B6E7DF55C50DB7B2E815BE8CCA38C675FDF570050`,
`DC4A823F25BEF556893D4473B17915171C900ACD2A3EE9836F276BCADB276519`,
`FEBFD140B9DDA2C213F4D1A823A0476E7AA38E294A726F683DB5E822BCD213CE`.
These are temporary negative controls, not candidate identities. After EACH
mutation the full raw migration SHA was restored to `CC1D52FBBC929F9D565DCDBDBFFC4966D4EC12204135253043183150B0884375`.
The final rebuild/regression begins only after that restoration; no mutation
remains active. None of the five intended RED assertions was a fixture,
migration-discovery, body-manifest or SQL setup error.

Current five-file source freeze:

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` | `CC1D52FBBC929F9D565DCDBDBFFC4966D4EC12204135253043183150B0884375` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `5AAEEF15B17F90E2379E93D55E5DD693187A43A5672A3633C9C90C57CAAE9FED` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs` | `253530C7C6DEA9B00EF23E187CD7EEDBBE464CC2B6BFEF187B2AA7C374F2D876` |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs` | `197634ADA4E33FFFCAA65A12B5E4ED16A5F84E5F833C0A3EE42264A96577C66A` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `9D1A95E416D065762DA548ED087B21ACD5CC405523F59956232BCC164AEA299B` |

Final regression is currently executing from the restored rebuild, using the
exact 14 classes from the prior 219-case TRX. Expected membership is that same
ordinal `(testId,testName)` multiset plus the five RE01 cases, not merely a
target count of 224. V6 independent evidence/restoration review is PASS with
zero actionable findings: full five-source SHA join, green/old-branch exact
test-ID/name multiset, all mutation assertion sites and all seven historical/
green/mutation evidence hashes verified. Its verdict excludes the pending
final regression. It ran no PostgreSQL fixture and made no file changes.

Restored build: IntegrationTests build PASS (two existing warnings, zero
errors); separate `TagEkyc.RawIngressBroker.csproj` build PASS (zero warnings,
zero errors). No project/dependency changes. Final Unit 324/324 PASS,
`tip88c1-a3-re01-final-unit.trx` SHA
`43618D0B2C634DA38906C812AF3F09B02888641464A51CFBC0AE7216733AF389`;
final Arch 155/155 PASS, `tip88c1-a3-re01-final-arch.trx` SHA
`DF572B077BEA4EE6CF3AEF25F50CF54066A689EF49CD7D74C7F2C99CFCC91089`.

Final restored PostgreSQL regression: **224/224 PASS**, zero failure/skip,
`tests/TagEkyc.IntegrationTests/TestResults/tip88c1-a3-re01-final-regression.trx`,
raw SHA `96775D5FBA7B4B22F7358E756E31D1F02C27FF94D5B0C7D4B2FAB129D3D4CC5B`.
The full ordinal `(testId,testName)` multiset equals the preceding NPS final
219 plus the five RE01 green cases: expected 224, actual 224, differences 0.
This is stronger than matching totals; it does not infer assertion equivalence
from totals. V5/V6 source review separately checked preservation of existing
proofs and exact new phase assertions. All six migration cases and the existing
withdrawal/session-lock proofs pass again inside this restored regression.

Disposition: **PASS — operational RE01 B-C handoff/settlement/lease checkpoint**.
No new public API, stage EXECUTE grant or parallel R2 pipeline. Five current
source hashes remain the frozen table above. No Agent code, historical migration,
model, Designer, project, dependency or line-ending policy was changed here.
Both repositories remain staged=0/conflicted=0; unrelated dirty/untracked work
is preserved. No stage, commit, push, A4 or production action was performed.

This is explicitly NOT full BP20/BP21 or process-kill-to-Available recovery.
The same-alias broker race proves its earlier B-B O10 plus later B-C O16;
the full B-C/stage/terminal race and complete R2-R5 proof remain obligations.
Next implementation is TI01/TI02 with existing guard/context/settlement reuse,
then CP08/CP09 and R2-R6 composition. Existing intent columns/model and pending
stage checks are present; the two terminal SQL functions, durable readers/scan,
recorder and typed stream observation are still missing, not presumed reusable.
C1/C3 retained integration and Agent retained/receipt remain open. A3 overall
is IN PROGRESS; landing and production are NOT READY / NOT AUTHORIZED.

## TI01/TI02 SQL operational checkpoint

Authority remains parent v0.6 plus the already-approved bounded corrections.
This implementation installs the two literal terminal functions in the same A3
forward migration. TI01 writes only the immutable cause/disposition/time triple;
TI02 independently proves NPS or key/object settlement before atomically adding
the matching semantic result. It reuses the existing four-argument operational
terminator on the key-present branch, never the legacy six-argument writer.

The final attempt guard now rejects intent-bearing INSERTs, preserves intent on
older UPDATE paths, and confines retained final-code addition to the exact TI02
context. Pending intent cannot enter the historical R3 guard. The retained
operational timestamp uses the post-lock clock; legacy statement-time behavior
and existing replay timestamps remain unchanged. New stage grants are included
in the broker forbidden census: 39 stage names plus two owner-only names = 41.
Current-body manifest pins and empty-schema Down/reapply coverage include both
new functions; historical predecessors are not edited.

Initial six real PostgreSQL TI cases passed (three exact-cause/tuple/replay
cases, active-key settlement, injected atomic rollback, actual-session wait
clock). The next three cases add exact eight-role ACL/argument/direct-write
proof, provider-unknown pending cleanup and NPS-before-expired-H O20 routing.
Their execution, controlled mutation RED/restoration and regression are pending
at this entry. The expiry proof uses bounded fixture profile inputs and waits
for observed database time, never mutates persisted horizons.

PI-TAG-001 V1 is reviewing a frozen five-file source candidate independently.
No process-kill, typed stream observation, C# recorder, CP08/CP09 or full BP18
claim is made by these SQL tests. Full R2–R6 recovery, C1/C3 and Agent retained
receipt remain open. No stage/commit/push/A4/production authority is inferred.

### TI SQL review and discriminating controls

V1 independent review found one low-severity authority-order deviation: Down
dropped TI02/TI01 before restoring the pinned predecessor implementations.
The exact drop was moved after NoProviderStartOperationsRestore and before
TerminalIntentSchemaRestore. V2 verified that reversing only that move
reproduces the V1 raw SHA; no other correction or semantic change was made.
The corrected empty apply/rollback/reapply proof passed again.

Final five-file source freeze for this SQL checkpoint:

| File (repo-relative) | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | A7CDD0FE777D41F426F1E64BF596B33EC18ABA330CD6FEC60DC26D5BADAF312C |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs | 6C1F43130AF50141E25909851C431D2E780AF8FF28A68D6022E900776858191B |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs | 3B15EEA2C05F32558A75129A340B3917752F879B150D29EC233BB9BA70EC09C0 |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs | 87781A4FBF920F3125BD1B01C99A8A1D38A9B797D04FF76E25D004DDC9CEEF47 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 419B0F1DA3A904B3DFC3835B0CD03E5D1704CD25DDD6D6D091DA427848D0550B |

All TRX below are in tests/TagEkyc.IntegrationTests/TestResults/.

| TRX | Total / passed / failed | Exact raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-ti-initial.trx | 6 / 6 / 0 | 831006B2B61FADAFCF17FD32F10C2A5964DFD7A1600C2C3AFE859536A9B89B51 |
| tip88c1-a3-ti-v1-green.trx | 16 / 16 / 0 | 521079D92A10B0FA475D9F8CAFF506EF4AB9A6C33A9FBA0F489A7113C4DC9654 |
| tip88c1-a3-ti-down-restored.trx | 1 / 1 / 0 | 2C3305FAFED4453C779A9C1B1BF912355E0EB0E771C4FB8C1713AD941EB9B172 |
| tip88c1-a3-ti-fence-red.trx | 3 / 0 / 3 | 9E60564330FD53EDD56273159C40292ADEDD6DBA9952A41D2F671BD1AD4C4260 |
| tip88c1-a3-ti-live-key-red.trx | 1 / 0 / 1 | 90ED262ED1DDAE4031DDAE0AD35E55511F734E8DC7D759C8B51029B76D2FBCA6 |
| tip88c1-a3-ti-clock-red.trx | 1 / 0 / 1 | 33E7013B9F1CE23E0E54A5E833B2F05E4664CE84C1B0C724B04FE5372464DC33 |

Controlled source mutations (no test or assertion change):

- Fence: omit only TI01's expected-fence comparison. Three exact-code cases
  become Recorded instead of StateConflict: 3/3 intended RED. Mutant migration
  SHA C3741F0C3FD87735435F2C6AF9610CC94A9C4ACE8F36F64B376D99B90345F781.
- Live key: admit Active in the TI02 settlement predicate AND its repeated final
  write guard. The active-key control becomes Finalized instead of CleanupPending:
  1/1 intended RED. Mutant SHA
  943E2324B1CE6E6B748A6EB2DECDE9192ACC6420C3062672B21A88F86A7C3A1B.
  This intentionally removes both enforcement copies of one invariant.
- Pre-lock clock: replace only retained clock_timestamp with statement_timestamp
  in the existing operational terminator. The real session-wait test fails with
  A3_R2_TERMINAL_FINAL_WRITE_FORBIDDEN because ordinary termination predates its
  intent: 1/1 intended RED. The final guard is deliberately NOT weakened.
  Mutant SHA 4E47322D7354033BAA80995811DCAD339DED8F6B2280CB1F62392ED2969391FC.

After EACH mutation, raw migration SHA was restored to the exact A7CDD0FE...312C
value above before the next mutation. Other four source hashes were unchanged.
No mutant is left active. Restored focused execution and the full ordinal
test-identity regression join are the next gates, not assumed passed here.

### TI SQL checkpoint final execution — 2026-09-15

The pending gates in the preceding chronological entries are now executed.
Disposition: **PASS — TI01/TI02 SQL OPERATIONAL CHECKPOINT**, not full BP17/BP18,
not process-kill recovery, and not overall A3. The five-file source freeze above
was recomputed after restoration and remains exact. No implementation bytes
changed during the final regression run.

| Evidence path (repo-relative) | Total / passed / failed | Raw SHA-256 |
| --- | --- | --- |
| tests/TagEkyc.IntegrationTests/TestResults/tip88c1-a3-ti-restored-green.trx | 9 / 9 / 0 | 075DC657BDBF81E407C41E5DBABD84B0BF90C43C79D5622BC90C9061EFC9E9EF |
| tests/TagEkyc.IntegrationTests/TestResults/tip88c1-a3-ti-final-regression.trx | 233 / 233 / 0 | ABD19E22C84A0CB8E2EEA89703EA5CCAD70EB87D896FF42A1140AE4651B2A411 |
| tests/TagEkyc.UnitTests/TestResults/tip88c1-a3-ti-final-unit.trx | 324 / 324 / 0 | 94C8C71774C51BB8EAF8429A585868BA99D43B09E70F32C53DF62DAE31876C26 |
| tests/TagEkyc.ArchTests/TestResults/tip88c1-a3-ti-final-arch.trx | 155 / 155 / 0 | B6E9299EDFB0A84236579004AC7C0AC42B16D1BF3E2CB7E504294A5F8BA6A286 |

The full PostgreSQL regression ran for 18.013 minutes with its real lease and
horizon waits intact. Its ordinal `(testId,testName)` multiset is exactly the
union of the previous 224-case RE01 regression (raw SHA
96775D5FBA7B4B22F7358E756E31D1F02C27FF94D5B0C7D4B2FAB129D3D4CC5B)
and the restored nine-case TI run above: expected 233, actual 233, missing/extra
or multiplicity differences zero. Both input hashes were checked in full before
the join. Count equality alone is not used as proof of unchanged assertions;
the controlled mutation patches changed source only, and their restoration is
bound to the five-file raw hashes above. No test was skipped in these final gates.

The separate RawIngressBroker project build also passed with zero warnings and
zero errors. The final restored solution build had zero errors; this is not a
claim that every existing project warning has been resolved.

Review ladder (PI-TAG-001, three rounds, no non-convergence): V1 inspected the
full terminal SQL/guard, adjacent NPS/RE01 and stage surfaces, grants, migration
ordering/manifests and new proofs. Its one LOW Down-order finding was applied;
V2 returned zero findings and verified the exact one-move delta. V3 independently
read the frozen SQL, adjacent legacy-writer/terminator paths, proof sources,
BR sections 3.3/4.4/4.5 and CP08 semantics, then returned zero actionable findings.
V3 recomputed both TI function-body hashes and inspected the three RED TRX plus
restored 9/9. V3 did not run PostgreSQL or claim the then-running full regression;
the final 233-case execution and identity join above were performed by Builder.

V3's plausible risks were wrong tuple/cause replacement, absent key mistaken
for settlement, half-finalization/pre-lock clock, and alternate-writer/Down
bypass. They were dismissed on the repeated locked tuple/settlement predicates,
immutable guarded writes, actual rollback/clock controls and exact ACL/restore
order. The review found no new semantic decision, module/provider expansion or
need for STOP/RRI. The lesson applied was to validate Down against the literal
restore/drop order as well as against successful empty round-trip execution.

Scope touched is exactly the five source/test files listed above plus this
existing implementation ledger. No new project/reference, provider, role or
public HTTP surface was introduced. No GDrive sync/upload was performed; this
is local implementation evidence, not a new ratification artifact.

Remaining work: typed framed-input observations and the real C# intent recorder;
CP08/CP09 durable readback/scan; TI01 versus R3 race proof; actual child-process
kill/restart recovery and full R2–R6 composition; C1/C3 retained integration;
Agent retained/receipt including restart refusal. SQL success is not a claim
that any of those caller/worker/stream paths already invoke TI01/TI02.

Both repos remain staged=0/conflicted=0. Server HEAD remains
5df5f60a6dc4d992c71fc2b160673e4abca6488d; Agent HEAD remains
e3bd625bbe357b1f3c9620d20bcba121cfb61974. Existing unrelated dirty/untracked work
was preserved. Commits: none. No stage/commit/push/A4/production action.


## Recorder and CP08/CP09 implementation checkpoint — 2026-09-15

Authority is unchanged: exact A3 v0.6 plus approved bounded corrections,
synthetic/non-patient only. No stage/commit/push/A4/production action.

The new terminal-intent recorder owns a suppressed-ambient, independent
transaction with transaction-local frozen actor. Its one deadline links to
host shutdown, not an already-dead request token. Recorded/ExistingMatch are
returned only after commit and exact four-column result validation. Real
PostgreSQL controls cover ambient rollback, disabled pool reset, host/deadline
cancellation while blocked at the real session lock, denied tuples/role, and a
deferred commit failure. No provider/body call was added to the recorder.

CP08 now returns the exact 26-column persisted metadata tuple; an unprepared
allocated key is not confused with a missing source. Present contradictory
key/object/terminal links return zero rows. CP09 uses strict UUID cursor and
1..100 bounds, keeps pending intent/unprepared work discoverable, and excludes
only exact finalized-and-settled work while retaining Available/Pending cleanup.
The new typed repository reads those two functions using borrowed role scopes;
it is not a stage-authority decision, worker, provider or body reconstruction.

Source reality correction: the existing key-reservation table has no
SourceArtifactId column. CP08 proves source through the exact key AttemptId and
fingerprint joined to the current source attempt; it does not invent a column,
registry or additional helper. Actual head and attempt revisions are independent:
real R3/R4/R5 proofs compare each observed revision, never derive the next tuple
by arithmetic. Readback remains available after reference withdrawal.

### Independent review and correction

Recorder V1 and typed-repository V1 independently returned PASS/0 findings.
CP V1 found two actionable items: reachable ordinary termination could disagree
with an existing durable intent; SQL reader/scan branch coverage was incomplete.
The old product trace is reproduced in disposition-defect-red (2/1/1).
The retained ordinary terminator now denies a conflicting disposition before
replay/write; its core write guard independently rejects it; CP08 rejects
contradictory complete operational/intent tuples before final code exists.
Legacy NULL-intent behavior and original timestamps remain unchanged.

Coverage was expanded, not assertions weakened: existing NPS/original-H/O20
proof now reads/scans each intermediate/final state and preserves the earlier
operational timestamp. Existing six real ciphertext R3/R4/R5+withdrawal cases
now consume typed readback. Available/Pending census has an explicitly marked,
rollback-only state fixture after real R5, NOT a claim that R6 cleanup executed.
Five isolated corruption/rollback controls exercise reader defenses separately
from write guards. The disposition proof also enters the actual core write
context; its initial expected error name was corrected from WRITE_FORBIDDEN to
the observed, existing APPEND_ONLY guard. The 30/29/1 run remains preserved.

One expanded command accidentally selected unrelated C404 by a broad
Continuation_ substring. C404 populated the shared template before isolated
clones; whole-database provider counts and unqualified corruption updates then
failed. The retained 13/6/7 TRX is not evidence of a product regression.
The runner was narrowed to exact class prefixes; corruption updates additionally
bind the exact allocated key/attempt ID. No fixture data, CHECK or assertion was
removed. The corrected expanded run was 18/18. Final regression uses the prior
14 exact class prefixes, with a test-identity join against the prior 233 cases.

### Source freeze

| File (repo-relative) | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | 4EA03F14B779640C166C45CF8E31362F71DC06BF15A9842B56B4E646A84A4400 |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2Contracts.cs | A0100CE6475FAA867B720785939D50255CD6BD9CB6BB1FC6F601741F5142DE19 |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2TerminalIntentRecorder.cs | A39C8FCEC35300C668F10B5C38A0F6A386AF0E02C0A34E28A2A0207B44C6042D |
| src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionContinuationContracts.cs | 85D22CB7FF8910F6342152DCA10D91E441A29146AFB72159ED78EB834523C2D6 |
| src/TagEkyc.Infrastructure/RawExport/RawSourceRetentionContinuationRepository.cs | 70B866797488E7959F01C6A96BF7D9468332DCE508B1439E54D6BDFA52E66DA5 |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs | EA003B494BC01E476A4281318959AE5C5E107770DF46A3373CBDAA6958FB8A45 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs | B25CA41BEF127A618567E12471B87D950BE93539C9B96D4417A88DC0BDC47143 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs | 594A83C0D0B266CA39CE02CBC5B887D601DE14FD3DA57948084245ECD1DFE2DA |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs | 272D4520E7D02AA4C78ACE264EDA04444720E341843390E2F107130B7DEC05F0 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 4622D7307E1E42E5B2334C0110714ED2C47CBF7F0CD3CA007B9D73E958AB32B8 |

CP body pins: readback
59cd21082ae0c0b839db38d8696ef8b711f1560abbc2cbd661f1b1f447e9d698;
scan f7b3d7d946ece3eb3cda8731829c3d7cd2849a00a2039cec39ec9d7d8b84e598;
core guard dee5353c80c457a0907f7af33f56453f7b1874db94d9371c2327e1b17dd86430;
four-argument terminator
170a516fd3b2e282651397e2ee152dddd387bf721a606f3b2eb9cb6d19bd2d60.
TI01/TI02 bodies are unchanged from their prior checkpoint. Down restores
predecessors and drops scan before reader. Broker census is now 41 stage names
plus two owner-only names = 43. Four new TI/CP signatures roundtrip together.

### Executed evidence before final regression

All following paths are under tests/TagEkyc.IntegrationTests/TestResults/.

| TRX | Total / passed / failed | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-recorder-initial.trx | 5 / 5 / 0 | 3806389394A45BFFF67C587603C076476B7BD6CBA627EF3733F41B0EEF844482 |
| tip88c1-a3-continuation-initial.trx | 8 / 8 / 0 | 5B63AF173E32EC4C54EFC24CEDED9BC67DF2FFED6C929FB1028F91F93A74602D |
| tip88c1-a3-continuation-migration.trx | 7 / 7 / 0 | 773B9FED354556A8959F56D98872A7CAF9A5A53DAF1FC2D23DB5C112FD773B20 |
| tip88c1-a3-recorder-actor-red.trx | 1 / 0 / 1 | 32B35FFB87D15E2093F07A7203898478C5FA3CDDD178312339CD4C40C35878A4 |
| tip88c1-a3-recorder-request-red.trx | 1 / 0 / 1 | 3402ED0C6497E91D778308BE7F25A610C4E0BA92674753A54E8EB0C2284F34AE |
| tip88c1-a3-recorder-commit-red.trx | 1 / 0 / 1 | 45DDAFBCFAB8D58C915D31AED8485DF533BA2EFF461B8D5B659C82F9E0411563 |
| tip88c1-a3-continuation-disposition-defect-red.trx | 2 / 1 / 1 | 1242F590BCCF51D3F0E2BAE3FFAEABB53CA29C247F71DFF5960B07BB6E3A1951 |
| tip88c1-a3-recorder-disposition-restored.trx | 7 / 7 / 0 | C1FCB6AC0D05FD87DD28051CA89EE1FA5514D726117A85F81AB46E50D066A969 |
| tip88c1-a3-continuation-expanded.trx | 13 / 6 / 7 | 60A6FFBAC487D49014E67ECD17C3433BF61BD4D1ADC5496D32FBA411D017301C |
| tip88c1-a3-continuation-expanded-v2.trx | 18 / 18 / 0 | 5624DD526718B205B8DD653E3EBBF43C464815595FBBF92D992EB400E3471378 |
| tip88c1-a3-cp-key-red.trx | 1 / 0 / 1 | 30F577CACB29A507BFF4B16CA9B548DB65EBAE2734B5E02303357A4F74A7E736 |
| tip88c1-a3-cp-intent-red.trx | 5 / 4 / 1 | 712D493B00CA50B0C1BFDB42F0A4E74BCC132C73E9A5247B2BF6735472D9E89E |
| tip88c1-a3-cp-scan-red.trx | 6 / 5 / 1 | 51A366A7C3B309F5AEE199443E35A1E66E56B2FDF95A363DC061F40A3EC8E72D |
| tip88c1-a3-cp-pending-red.trx | 1 / 0 / 1 | 7B9DC0F07A1AAD170F2D296325CD424EE5265C528A143D3468FCD5A97A5A72D7 |
| tip88c1-a3-cp-core-red.trx | 2 / 1 / 1 | 1E64B5E8A495F09E42A7EC88C9A4BA74C770DB901E2B108F4CCA6C3F29D4EA34 |
| tip88c1-a3-recorder-cp-restored-green.trx | 30 / 29 / 1 | 8DABA5A63A8BC867A279B627A715E6BDA0F8A9BE32D4B09EADA713AB3BA686ED |

Eight controlled implementation mutations changed no test assertions:
recorder session-level actor, request-token linkage and omitted commit each
produce 1/1 RED; CP inner-key join 1/1 RED; omitted pending-disposition agreement
1/5 RED; omitted Available/Pending branch 1/6 RED; excluded intent scan 1/1 RED;
omitted core agreement 1/2 RED. Their failures target actor leakage, cancellation,
false persistence acknowledgement, absent pre-key work, contradictory tuples,
lost cleanup work, lost terminal work and bypassed write protection respectively.
All mutation edits were reversed; final migration/recorder raw hashes equal the
source freeze. Expected SQL body pins were not weakened to admit mutants.

The first unit run named recorder-cp-final-unit was compiled during the final
core-only mutation and is NOT canonical restoration evidence. A separately named
final-v2-unit run rebuilt after restoration: 324/324. Final full regression,
architecture execution and V2/V3 closure are pending at this chronological entry.

Open, not implied by these results: stream observation/counter integration,
actual API/role-provider composition, worker stage routing, real Process.Kill
restart proofs, full R2→R6 recovery, C1/C3, Agent retained/receipt.
No process-kill or complete recovery PASS is claimed.


### Round 3 cumulative convergence audit — recorder/continuation

V1 found a cross-layer contradiction between durable terminal intent and the
ordinary operational terminator, plus incomplete continuation branch proofs.
V2 verified those corrections. V3 then found a proof-isolation defect: the new
pre-key continuation case counts provider rows globally although its database
is cloned from a potentially populated current fixture. The earlier broad
Continuation filter had already exposed this as Expected 0 / Actual 5. Narrowing
the filter separated unrelated executions but did NOT close this isolation gap.

The accumulated cause is incomplete ownership scoping at two different layers:
product transitions must agree on the same intent/attempt, and proof censuses
must distinguish the allocated attempt/key from unrelated durable history.
Remediation is a bounded test correction, not a product change: exact-identity
provider counts, unchanged unrelated-row snapshots, and a deterministic populated
history positive control. A mutation restoring the global count must turn RED.
Review V4 must inspect the complete helper and both empty/populated cases, not
only count totals or the earlier filter change. No assertions, SQL authority,
or fixture schema are to be weakened. The frozen 249-case regression already
running at this point precedes this V3 correction and cannot alone certify its
successor test bytes.

### V4 bounded isolation correction and review

The corrected pre-key proof runs both unpopulated and deliberately populated
provider histories. One Seed is reused; a second ChipDg2Portrait admission goes
through the actual broker and real key preparation before the LiveSelfie target.
Its Unknown provider outcome commits three genuine evidence rows. The target
still has exactly zero then three owned provider rows. All complementary provider
rows are compared byte-text unchanged. The production scan must return the full
baseline plus target in UUID order, including the unrelated source; no scan
filter or shared-fixture cleanup makes the test pass.

Initial correction 2/2 PASS; reintroducing the global-zero assertion is 2 total,
1 PASS / 1 FAIL, exactly populatedHistory:true Expected 0 / Actual 3. Restoration
returns 2/2 and exact source SHA
`622C176E2C27D56AA72F427FEB2DC8CBD67A31386C94699DECD565A8C9BB2281`.
Only the test file changed from the ten-file checkpoint freeze. The admitted
helper extraction preserves every existing default and expiry deadline input.
V4 independently read the complete ownership helper, both cases, real setup,
scan assertions and three TRX files: PASS, zero actionable findings. No reviewer
ran PostgreSQL; executable fixture evidence remains the Builder's responsibility.

The prior frozen full run completed 249/249, zero failures/skips, TRX
`tip88c1-a3-recorder-cp-final-regression.trx`, SHA
`FA2F2EA07DBBED6F85698736D35587C92F18F11424F57970270E4380288F4E6E`.
A separately named final V4 regression is running on the corrected source;
expected count is 250 (the one old Fact is replaced by two named Theory cases).
The old Fact's identity replacement must be explicit in the test-set join;
count arithmetic alone is not the closure proof.

### Executed recorder/CP08/CP09 closure

Final V4 regression completed: **250/250 PASS, zero failed/skipped**. Its exact
testId+testName multiset equals the prior 249 run minus the one explicitly
replaced pre-key Fact plus the two restored Theory identities: expected 250,
actual 250, zero missing/extra/count differences. The prior 249 itself preserves
all 233 TI baseline identities and adds exactly sixteen. This is an identity
join, not only `233 + 17` arithmetic. All ten current source SHA values match
the freeze in full; the only V4 successor is the test SHA recorded above.

| Evidence (under the owning test project's TestResults) | Total / PASS / FAIL | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-cp-v3-isolation-green.trx | 2 / 2 / 0 | F44DF63567769FCFA6E396056603DDBCEA08E17A32ED380FBB2C7492DFCBC4C7 |
| tip88c1-a3-cp-v3-global-count-red.trx | 2 / 1 / 1 | 804750CFB2EBAF012CC4F515D878300DA983BD6C62D96224A0B96F83741AFB67 |
| tip88c1-a3-cp-v3-isolation-restored.trx | 2 / 2 / 0 | 86830BDF8267D584F4FF846053E0A2FDC5338A983D078B9499449C55D9E8D2D6 |
| tip88c1-a3-recorder-cp-v4-final-regression.trx | 250 / 250 / 0 | E93C8E041F39FEC355A0E244ADDE57C2143BD6E659EDEA6CAC3373C4CFEA0F24 |
| tip88c1-a3-recorder-cp-v4-unit.trx | 324 / 324 / 0 | DAE950DEC3B60B2E790535EF6DF9757D85B89D4FD0102AE0A9F9E5C39F6A6C98 |
| tip88c1-a3-recorder-cp-v4-arch.trx | 155 / 155 / 0 | 13A822754E63D17E7AB547F3D86D3B980F5BA671C1705657CD13E11F2CBE5287 |

**Bounded disposition: PASS — recorder transaction + CP08/CP09 SQL/typed
readback/scan checkpoint, zero remaining actionable findings through V4.**
Eight product mutations and the separate global-count proof mutation have their
RED evidence preserved; all edits were restored before the final run. No skip,
assertion removal, generic accepted hash, or provider-outcome invention closed
the findings. Current Server HEAD remains
`5df5f60a6dc4d992c71fc2b160673e4abca6488d`, Agent HEAD remains
`e3bd625bbe357b1f3c9620d20bcba121cfb61974`; both staged/conflicted counts are zero.

This is NOT stream composition, worker execution, Process.Kill recovery,
R2–R6-to-Available, C1/C3, Agent retained/receipt, A3 overall or landing PASS.
The recorder and readback implementations are proven separately; an activated
API/role-provider composition has not yet wired them into the complete route.
Next authorized work reuses the existing R2–R6 services and the already-listed
pipeline/provider-scope/worker paths. Real child-process proof is still required;
no Task cancellation or in-memory restart has been counted as that proof.
Synthetic/non-patient only; no stage/commit/push, A4 or production authority.

## 2026-09-15 — retained input observation / writer / encryption-reader checkpoint

### Implemented boundary, not full recovery

Authority remains exact A3 v0.6 plus approved bounded corrections. No new RRI,
provider, module, project reference, production option or mutation path is used.
This continuation modifies the existing R2 stream/writer and allowed A3 migration
and proof files; it does not create a parallel encryption pipeline.

The stream now exposes metadata-only InputObservation (closed enum plus durable
intent acknowledgement). Plaintext reads cap at the remaining declared/class
allowance plus at most one excess byte, with overflow-safe counter comparison.
Actual excess, clean short EOF and valid-but-different commitment persist the
appropriate TI01 intent before the input exception may escape into a provider
catch. Transport interruption, unavailable commitment provider and encryption
provider failure remain distinct and do not fabricate a deterministic input
cause. First cause is monotonic. After any failure, another Read performs no
source read, cipher operation, commitment call or recorder call. CompleteMatch
is visible only after the last final-frame byte is copied; legacy Completed
still requires the existing extra EOF read.

The writer propagates observation through both the ordinary provider-catch
result and the RecordPutResult persistence-failure result. CompleteMatch is NOT
proof that the object was durably stored. A retained context must have recorder
and positive class bound before Begin/Arm/body. This check currently follows the
existing key provisioning step: it is explicitly NOT the façade's pre-key
readiness guarantee. Activated API composition and its authoritative effective
configuration/class-limit resolver remain OPEN; synthetic class bound24 is an
explicit test input, not a production default or a substitute for that resolver.

The current encryption reader's existing34 fields are preserved, with field35
AuthorityKind read from the exact persisted snapshot join. Current schema with
a missing, NULL or unknown tag fails closed. A genuinely pre-A3 schema is
distinguished by its actual pg_attribute census and retains its34-column reader
with AuthorityKind NULL; no LegacyExport or SourceRetention tag is invented.
The A3 migration installs/restores this same SQL function, not another public
operation, with unchanged owner/search_path and encryptor-only EXECUTE.
Canonical LF function hashes are predecessor
`84848b69cf9afe73a30341fc7159741b9372fbf25a960f411afdf5f52bafedfe`
and successor
`b98e3708bdd0c4beae2eb81e4c926377afc7a0f7050cdc6a1c20fb15b5c6ea4c`.
The roundtrip body census includes this twentieth function. No model/Designer
change is needed for this result-shape-only SQL change.

### Proof ownership and defects actually found

Twenty-two new cases cover declared/class overflow, Int64 arithmetic boundary,
clean EOF versus interrupted/cancelled prefix, exact-length commitment match
versus changed bytes, provider unavailable/throw, cipher failure, recorder
denial/failed commit, held recorder gate, writer catch/persistence failure and
current-schema NULL/unknown/missing AuthorityKind. The new non-arithmetic setup
uses real PostgreSQL and the existing real attempt AEAD/commitment path. Writer
catch tests use a bounded test object adapter to inspect exception propagation;
they are NOT a durable S3 object or process-kill proof. Durable intent assertions
read through an independent observer connection and are scoped to this attempt.
Caller plaintext stream ownership and unchanged attempt columns are asserted.

An initial12/13 failure was a test query using the nonexistent plural head table
and wrong head state column; the actual schema was inspected and only those
query identifiers corrected. The subsequent13/13 run is preserved separately.
The legacy six-case codec guard then exposed R214: computed commitment allocation
had been moved into a catch scope incompatible with the established IL cleanup
proof. The product catch boundary was narrowed around ComputeAsync; owned array
allocation remains immediately before its original try/finally zeroization.
No historical test or assertion changed; the same six cases restored to6/6.
A transient V4 compile failure from the local Record helper shadowing Xunit.Record
was fixed by qualification only, with no project or assertion change.

### PI review convergence and cumulative cause analysis

V1/V2/V3 reviewed the bounded product boundary; V3 challenged premature completion,
overwritten input cause, provider-catch information loss, false historical tag,
buffer ownership and migration restoration. No remaining product finding.
At round3, accumulated defects showed three distinct evidence hazards:
schema-name assumptions in a new test, preservation of an existing cleanup
proof's exact code ownership, and asynchronous assertions failing at a later
condition rather than the intended ordering boundary. The correction strategy
is exact source inspection plus named failure-target inspection, not additional
prose or test-count claims.

V3's two test-hardening observations were applied in V4. A gated recorder is
held across250ms: consuming must time out while durable state remains unchanged,
then the gate is released in finally. Removing await now turns RED at this
specific timeout expectation (no exception thrown), not only at the later
persisted flag. Repeated-read tests capture the exception, assert unchanged
activity/durable state/observation first, then assert its exact type. Removing
the failure latch now makes all12 affected cases fail on extra-activity tuples,
not mostly on exception-type mismatch. V4 independently verified updated bytes,
the three hardening TRX files and all seven unchanged companion source hashes:
PASS, zero actionable findings. Reviewers did not execute the PostgreSQL fixture.

The earlier authority-tag mutation breaks its positive control and is retained
only as coarse evidence. The stronger authority-fallback mutation preserves
SourceRetention positive control, rejects NULL/unknown branches by intended
missing-exception failures (2/3 RED), and leaves the separate34-column shape
guard green. Earlier await/latch evidence is likewise not overstated: the first
await run failed at persisted=false, and eight first latch failures stopped at
exception types. V4 evidence supersedes these narrower claims without deleting
their history.

### Exact V4 source freeze

Provenance qualification: V2 reported a raw intermediate stream hash
`28F3DC9C...AEDF01D`, not the final restored candidate. Those intermediate bytes
are unavailable for exact comparison, so the difference is not attributed to
line endings and V2 is not evidence of final-byte restoration. Main, V3 and V4
independently verified the final full F272254B...EB49 hash below. Current stream
is20377 bytes,495 LF,0 CR,no BOM. No earlier hash is accepted as an alternative.

| Existing allowed path | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2Contracts.cs | BE96DD542BAF35ED5A48567D6A215131F1F565F5822612C4355309395C13F0AB |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2FramedCiphertextStream.cs | F272254B5032F27A688D37D20B29755C4AD514B96CEF606BE23C56C63C55EB49 |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2Repository.cs | 2B7EA9652A06F72D20E78B5488D3D94E36B6234E9310A5B873FB5EBA52086C4B |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2EncryptionOrchestrator.cs | 0214B47B29B605BBBDC2615EE4BB20E089BF8AD538A0E2007A6A446360B5C1E9 |
| src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs | 6EE1479A672ECF9FF2A21BC1A2476A96F45A8C6A6F243B6A0855556EC4E59148 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs | 0ACF10602BBE1DD6826DA629C205EB2BF6C243CB85536C638DAC9368715BBFAB |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs | 9C136C7F29065239B7274CD39F51F7551594E10D078DA6FC20CAE2DD4BE7E6EA |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs | 618CFCDC7B9C24DEFA58D13CF59E2B9250B887A2D3F738E74EDB66317A961673 |

### Executed focused evidence

All files below are under IntegrationTests/TestResults. Counts are total / PASS /
FAIL, not an assertion of full A3 completion. No new skip was introduced.

| TRX | Total / PASS / FAIL | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-input-authority-fallback-red.trx | 3 / 1 / 2 | 0CCEF129133687402D01EDCA013129CA2D5E628210C31FD969A4CC3C06F7F2A2 |
| tip88c1-a3-input-authority-tag-red.trx | 3 / 0 / 3 | 04003ACBF063394043175555EEB649C8F5DEB4849FCBF257DCCDC6CF420BB43E |
| tip88c1-a3-input-await-red.trx | 1 / 0 / 1 | 939F14A4E8E8D690ADC4730EA5611DDA6F27CB161B2C25397CD8D17B6B498C10 |
| tip88c1-a3-input-counter-red.trx | 19 / 17 / 2 | C9BE457AB7B56326A56F7CAF3F47CC8CDE2341076FBFCB5CEACEC151F5905760 |
| tip88c1-a3-input-early-complete-red.trx | 2 / 1 / 1 | 580FC9D96910CE876BD73BB7603474D8AF27D5BF940A2478F54418CD77000254 |
| tip88c1-a3-input-latch-red.trx | 22 / 10 / 12 | 5B6488E5052B8E3676281672927633F5C505A1BE089B3DFB0A64A382314BB6EC |
| tip88c1-a3-input-legacy-codec-restored.trx | 6 / 6 / 0 | FF8DADB851ABB64E03CDC2858247D8D42D47EDC0D87662EAB464662E871CF3E3 |
| tip88c1-a3-input-legacy-codec.trx | 6 / 5 / 1 | 63039007C363818CD0F3D315744142CD1D133F7FFFF132BEDB3F00C0C78E1305 |
| tip88c1-a3-input-overwrite-red.trx | 22 / 9 / 13 | E6CAA339CF3F28A9864F94A62F0BB7CEEC5737B1F09EA38F65F2CCE9C74EB418 |
| tip88c1-a3-input-pre-mutation-green.trx | 22 / 22 / 0 | 4A4D62EE669CBF1F2A5BB24FDA054AD085C3F24BF1695522263F0BC35B71427F |
| tip88c1-a3-input-reader-roundtrip.trx | 1 / 1 / 0 | 0B2D4BEAB2A32631046C66BECA64F26C2086AC5DF5ABDE8766C71A25525348E8 |
| tip88c1-a3-input-restored-green.trx | 30 / 30 / 0 | CB06076A9517E36F0D43EE61E6516E99CD75E98B1987CD1F9AE44332F44980AE |
| tip88c1-a3-input-v1-green.trx | 13 / 1 / 12 | 2EB94A3352B06EC3756D9F7D3594F9C60B7C9A7A5DA6ECCBFB07AA64F6949E75 |
| tip88c1-a3-input-v2-green.trx | 13 / 13 / 0 | 0EDE3DFCE72005DEBCAD8052847642A7A5940F09EE8C519A3DD6747DC1E41F90 |
| tip88c1-a3-input-v4-await-red.trx | 1 / 0 / 1 | CD1778DD93C4B1A97BBD10288B12CC2D6B3EE335F6A72ADB901062E067B1D7AF |
| tip88c1-a3-input-v4-hardening-green.trx | 6 / 6 / 0 | 0EA21226E72119308D5651950E6D8960A0C24A901569996025256B696B58C2E3 |
| tip88c1-a3-input-v4-latch-red.trx | 22 / 10 / 12 | B7EB38BF906AE925E2C39C99648348B661C4EF7A5D567E9B015B0796BCA5993B |
| tip88c1-a3-input-writer-green.trx | 21 / 21 / 0 | C14151819D82E249404C776213FB768F03A574CD20071C0673558CFB7E92CD64 |
| tip88c1-a3-input-writer-observation-red.trx | 5 / 2 / 3 | 1ABBCC40078DBC9411107E8A49F3E604D9AAA305ACBB836A60743BF395BDFC75 |

The30/30 restored run precedes the V4 test-hardening delta and includes22 new
cases, six historical codec/ownership tests, R301 and migration roundtrip. The
V4 hardening6/6 runs the successor assertions. All controlled product mutations
are now restored byte-exact; stream SHA is F272254B5032F27A688D37D20B29755C4AD514B96CEF606BE23C56C63C55EB49.
The final V4 regression completed272/272 executed/PASS, zero failures/skips.
The exact testId+testName multiset equals the prior250-case recorder/CP baseline
plus the22 new input cases:272 expected,272 actual, zero missing/extra/count
differences. Unit and Arch identities also exactly preserve their324 and155
baseline sets. All eight source hashes were independently rechecked in full.

| Final evidence under the owning test project's TestResults | Total / PASS / FAIL | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-input-v4-final-regression.trx | 272 / 272 / 0 | 444DE7B5AAF1782595214566604BDDEBEEC46FA1F728630BA19BF0C7FF0A9C95 |
| tip88c1-a3-input-v4-final-unit.trx | 324 / 324 / 0 | 3CE2AEDB3A7C514A083296F064E671D4101CA88AFED31A19ACE2AB83041F7B4D |
| tip88c1-a3-input-v4-final-arch.trx | 155 / 155 / 0 | 917FCC733244A894CE0EC7C89DB0D93D34A9B4C76E4523E93AB06AA9731F349B |

Bounded disposition: PASS — retained input observation, writer propagation and
persisted encryption-reader authority-kind checkpoint; zero actionable findings
through V4. Server/Agent HEAD remain5df5f60a6dc4d992c71fc2b160673e4abca6488d /
e3bd625bbe357b1f3c9620d20bcba121cfb61974. Both have staged0,conflicted0 and no
project-file diff. Other dirty/untracked work is preserved, not included in this
checkpoint's eight-file freeze.

Stream observation/writer propagation does NOT close activated façade/class
resolution, worker/provider scopes, real child-process kill/restart,
R2–R6-to-Available, C1/C3, Agent retained/receipt, A3 overall or landing.
Synthetic/non-patient only. No stage/commit/push, A4 or production.

## 2026-09-15 — BP09 custody role-scope implementation

Objective: supply the role-isolated connection/provider owners required by BR
v0.5 section4.1 before composing the worker. This is plumbing, not Activated
readiness, provider production qualification, worker execution or recovery PASS.
The ratified parent v0.6 and inventory are unchanged; no new RRI or authority is
inferred. Synthetic/non-patient only; no stage/commit/push, A4 or production.

### Scope and implementation

Only three source/test files changed in this checkpoint, all already in the A3
inventory: new internal CaptureRuntimeCustodyProviderScopes; added tests in
BrokerPipelineTests; explicit synthetic configuration/LOGIN ownership helpers in
SyntheticComposition. The existing ledger is updated here. No product SQL,
schema, migration, project/reference, provider implementation, readonly role
validator or object/GovArt gate changed. Agent was not modified.

The factory freezes three independently configured roots, each owning its own
NpgsqlDataSource. Each child owns a DbContext and one existing S3 implementation:
Writer/encryptor LOGIN, Reconciler/reconciler LOGIN, Lifecycle/lifecycle LOGIN.
Foreign provider interfaces, posture/admin provider and broker are unavailable
from a child. Connection Options/role impersonation, foreign database, invalid
object profile, wrong capability/bucket and combined access-key identity fail
before connection/provider use. Successful DI resolution is NOT object-policy or
production readiness. Existing production object/GovArt gate remains intact.

Open verifies the actual SQL session/current actor, exact outgoing role closure
and no non-superuser incoming membership to that LOGIN, in addition to the
existing six-role/three-membership validator. The physical connection stays
owned by that child. All connections from each owned data source have
Enlist=false: a caller ambient transaction cannot absorb later stage-owned
transactions. This is an ownership implementation detail, not a semantic change.

Synthetic LOGIN helpers start only from the fixture's three PASSWORD NULL roles,
assign random non-output fixture passwords and restore PASSWORD NULL on disposal
or setup failure. Cluster-wide role mutations are restored in finally; the
existing PostgreSQL collection prevents concurrent fixture use.

### V2 intermediate source freeze and prior-byte preservation

| Path | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeCustodyProviderScopes.cs | 9CC874F3203372D0E0BDBE2C9C67E3762BC3D41C4F3237F5AE916CD54450650F |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 9DDEFEBA4E42E0C15143941690A4810B5CD99DCFB3BE237C6814FEC773AF5FB8 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs | FEB88BB65A377AA0D821C2B2FCFDE792F2723CA63C401ABFD9E01FF01B4281A4 |

Deleting only this checkpoint's added methods/using in memory (not on disk)
reproduces prior BrokerPipelineTests SHA
4622D7307E1E42E5B2334C0110714ED2C47CBF7F0CD3CA007B9D73E958AB32B8
and prior SyntheticComposition SHA
FD6539F3F3279C4ECF6C4B9CFC84FCF2EC68436AA61943389193F3169058E801
exactly, without EOL normalization. Existing test/helpers were not weakened.
The preceding input/writer checkpoint's eight raw hashes remain unchanged.

### Executed evidence

All paths below are under the owning project's TestResults directory. The four
RED runs and restored run have the exact same ordinal testId|testName multiset.
Each RED run executes13, passes12 and fails exactly1, with no skip/setup failure.

| Integration TRX | Result / actual failure | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-scopes-incoming-red.trx | incoming-login is accepted after removing inbound predicate | D61ABB9021A478FC52E354753204E3C60A3A08F53938AE18D5ABAAAA108D1B1B |
| tip88c1-a3-scopes-ambient-red.trx | real R3 BeginTransaction rejects ambient enlistment, before property assertion | 2467BF70ABD330D9A87854734EBC9774DDE6C06EF0ACE285027FC1409EA5FA52 |
| tip88c1-a3-scopes-cross-role-red.trx | foreign interface resolves: expected false, actual true | CC0B1D237FB0E225FCC2A2FE7C594FBBED75412538C36B9F89D73141402D6FE0 |
| tip88c1-a3-scopes-combined-credential-red.trx | duplicate object access-key owner no longer rejected | 4B8602A8DC73AA66A443C68226F490CE28E2B1E1337C4B163FCD51109A5DD548 |
| tip88c1-a3-scopes-restored-green.trx | 13/13 PASS | D904EAE79BFEDACA82365F5D71887D925559347BEC0DFC92539B0D0C9CEA0D02 |
| tip88c1-a3-scopes-qualification-regression.trx | 40/40 PASS | F1AED9DD58F2910C7E416D5B324EF9C5431C09AB4D53D4144CE092F6CC98F0CE |

The40-case qualification run equals the prior27 qualification test identities
from the272-case input checkpoint plus these13, with zero multiset difference.
It includes real PostgreSQL LOGINS, actual S3 implementations against MinIO,
positive put/read/delete and forbidden cross-role provider calls. The ambient
case runs the real R3 transaction owner with typed NotFound for an absent target,
then begins/commits a later data-source connection's local transaction, all inside
an uncompleted caller TransactionScope. This proves transaction ownership, not
successful R3 source staging or crash recovery.

Full Unit324/324: tip88c1-a3-scopes-final-unit.trx,
8918971B77DC5A2A6DA5740B42EDEC0A9D83AEF229CF309973E50D48E293AB9B.
Full Arch155/155: tip88c1-a3-scopes-final-arch.trx,
A4AFABEFA1C097A60DBAF0E129360B38897D8506552C6228A1C531615F3B9AF2.
The full272-case historical regression was NOT rerun for this additive scope
checkpoint; its earlier evidence is not relabeled as a current285-case run.

### PI review and bounded disposition

V1 deep review found two P2 implementation defects: reused role validator did
not cover incoming membership to a LOGIN; default ambient enlistment conflicted
with independent stage transactions. Both were fixed in the new factory, without
changing the readonly validator or historical stages. V2 independently verified
both fixes, exact source hashes, all four intended RED failures and restored
test-identity multisets: zero actionable findings on those corrections. The
subsequent V3-V5 review and final freeze below supersede this intermediate state.

The causes were incomplete authority-graph closure and implicit connection
ownership, not a new business decision. Correction/testing now explicitly covers
both membership directions and executes the real stage inside an ambient caller;
checking only a configuration flag is insufficient. No RRI/STOP was required.
That V2 result alone was not a final BP09 verdict.

### V3-V5 correction, cumulative causes and final freeze

V3 found one P1 qualification gap: membership does not prove effective SQL
EXECUTE rights. V4 closed that example but found two related gaps: direct
table/column SELECT (P1) and EXECUTE delegation (P2). Both are closed in V5.
These were incomplete earlier implementation/review coverage, not new semantic
decisions. From round3 the corrective rule is to enumerate privilege classes,
not patch one grant example: membership both directions, function availability,
effective EXECUTE, grant option, direct table/column read/write and schema CREATE.
The41-entry literal function/signature/role manifest derives from actual existing
stage grants and is independently joined against migrated pg_proc ACL in a test.
Missing functions/rights, changed owner/security-definer/search_path, executable
cross-role or broker functions and unexpected overloads all reject scope opening.
No schema/SQL migration or existing provider/readiness validator was edited.

V4's29-case run also exposed an EF internal-provider cache ownership defect
(27 pass,2 fail). A child now owns NpgsqlConnection from its root's data source;
EF receives that connection, not a host-specific data source in its global cache.
No warning suppression/cache-disable was used. The same29 test identities then
passed. V5's first35-case run exposed a test setup typo (State is not a key-table
column); the read-grant uses actual AttemptId now. That34/35 diagnostic is NOT a
mutation proof. Neither correction changed historical assertions or schema.

| Final source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeCustodyProviderScopes.cs | DD774306B5E222D6E2C46AC3DBBFF52D1981A2B21D17FE3A9FD857F0B632FC80 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 0BF6D46EC10DE59BE1FFCF05E34DCCC8CAAAFE58FEB1E614573BEF033DB495C8 |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3SyntheticComposition.cs | FEB88BB65A377AA0D821C2B2FCFDE792F2723CA63C401ABFD9E01FF01B4281A4 |

| Integration TRX in TestResults | Executed/pass/fail | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-scopes-v4-complete-green.trx (diagnostic, despite filename) | 29/27/2 | 9FCCBCF5C867877FFDF1416DB44FAA744189593B2E6DBBC237F591E17132B2F9 |
| tip88c1-a3-scopes-v4-owned-connection-green.trx | 29/29/0 | B3AF762C505A090CF27FCD167939ADE2C17E3D638E1EDB559012F4C98E129435 |
| tip88c1-a3-scopes-v4-acl-red.trx | 29/14/15 | E9B8ADC028EE390F941427170579312627A7BA3F0EE891E56E23DA7E2DC620FA |
| tip88c1-a3-scopes-v4-final-qualification.trx | 56/56/0 | 6451389C7B91D6A773022C20AE0FC9FA5A81450094AFAAE6E4CC3688BD63773A |
| tip88c1-a3-scopes-v5-green.trx (setup diagnostic) | 35/34/1 | 0B1C98241A695D14B071967FAA910BFFA1A20F88653E58C9147F0F3B783272D9 |
| tip88c1-a3-scopes-v5-fixed-green.trx | 35/35/0 | 324CC923AE846F9BD4610C6E1438BAEC1A46E83A991FE89791B17DB9C8819A4E |
| tip88c1-a3-scopes-v5-read-red.trx | 35/32/3 | C2FC8DA5D69E51F1727C172DB398E382606FB0B69E161514A19A1F11B2934FD6 |
| tip88c1-a3-scopes-v5-delegation-red.trx | 35/32/3 | 1335BC57E42D5CF428EB26AA9380A6EADBFF44030184F0786F0B19B70CAAFD8A |
| tip88c1-a3-scopes-v5-final-qualification.trx | 62/62/0 | DA9D639010660D84F9B79AA332ED2A497D04AB72A395C5D00BD10FDD37989FC6 |

The V4 ACL omission fails exactly15 direct/PUBLIC/column-write/missing/broker
cases. V5 read omission fails exactly3 table/PUBLIC/column-read cases; delegation
omission fails exactly3 grant-option cases. All fail because rejection is absent,
not setup. Every controlled mutation restored its own version's full source SHA.
The V4 three29-case runs and V5 four35-case runs respectively have identical
ordinal testId|testName multisets. Final62 equals the prior27 qualification test
identities plus35 new identities, with zero difference. This is NOT a rerun of
the full preceding272-case regression and is not reported as307/307.

Final Unit324/324: tip88c1-a3-scopes-v5-final-unit.trx,
676EC99E3A8BF2B3D48650FCF7BC2DBFDEA666D248F3472D9589EAFDC9F9BB8A.
Final Arch155/155: tip88c1-a3-scopes-v5-final-arch.trx,
670BA6BCE611E3A9C136572239846561DDB7CEAB2AF3DF3CA1F04F38F34EE17B.
All passing runs have zero failed/skipped cases. V2's four earlier mutation
proofs remain versioned evidence; they are not falsely described as V5 reruns.

V5 independent verification: PASS,0 actionable findings; reviewed all three
exact sources, targeted RED failure messages, restoration and62-case evidence;
reviewer did not execute PostgreSQL/tests. Five review rounds total: V1 two P2,
V2 zero, V3 one P1, V4 one P1 plus one P2, V5 zero. No unsupported finding or
semantic RRI; round5 is clean so the non-convergence checkpoint is not invoked.
Disposition: PASS for role-scope plumbing only. No worker/recovery PASS inferred.

Still OPEN: Activated facade/class-limit resolution, complete key/stage provider
composition, worker/readback routing, real child Process.Kill recovery,
R2-R6-to-Available, C1/C3, Agent retained/receipt, A3 overall and landing.
No production call site currently activates this factory. Object-policy/readiness
qualification must still be performed by the eventual Activated composition;
three distinct configured access IDs alone do not prove least-privilege IAM.
No Google Drive synchronization was performed in this local implementation turn.

## 2026-09-15 — BP09 full regression continuity closed before worker

Disposition: PASS_FULL_REGRESSION_CONTINUITY. This closes the explicit open
verification gate recorded above; it does not change the historical fact that
the earlier62-case plumbing checkpoint had not rerun the preceding272 cases.
No source, test, schema, project or normative authority was changed for this run.
Worker implementation did not proceed while this gate was open.

The command mechanically selected all14 class names from the exact preceding
input-v4-final-regression TRX, including the entire current BrokerQualification
class, without per-test exclusions. The final TRX ran2026-09-15
10:46:30–11:05:29 +07:00:307 executed,307 passed,0 failed,0 skipped.
An ordinal testId|testName frequency comparison proves exact equality to
old272 plus the35 BP09 role-scope identities: missing0,extra0,multiplicity
mismatch0,duplicates0,non-passed0. This is measured continuity, not count addition.

| Evidence (owning test project's TestResults directory) | Raw SHA-256 |
| --- | --- |
| tip88c1-a3-scopes-full-regression-continuity.trx | 75AF702B51A73E7B96B04E562849C23CC95531F0AFC2892EB0EACAC56A31240B |
| tip88c1-a3-scopes-continuity-source-freeze.json | B909D0F6D1C62DB906A628173034E655302FFE76A4B00A7DC1740D6AD077E56E |
| tip88c1-a3-scopes-continuity-result.json | 04DF480922DFC850BAF4990B6EB65B0F5E8CCB3B9634D283ED555470664F37C8 |
| tip88c1-a3-scopes-continuity-unit.trx (324/324) | 6AB55A1309E26D1424BE7FE852E7CC69AFE42247F5C7D96018AFE59BB63088CA |
| tip88c1-a3-scopes-continuity-arch.trx (155/155) | 720AC4482EEABC58F8920A73CA71822C0CB84A8C65D7ABD77929A5988A418EEC |

The freeze captured667 entries before execution, materialized that captured
list as JSON during execution, then compared every entry after completion:
667/667 exact,0 changed/missing,0 new paths. Scope is explicitly655 .cs and12
.csproj files under src/tests, NOT a whole-repository/solution freeze; the root
solution and tools are not covered. BP09's three final source hashes remain
DD774306...2FC80,0BF6D46E...495C8,FEB88BB6...81A4 as fully pinned above.

Independent bounded evidence verification returned PASS,0 findings after
recomputing TRX SHA/counters/ordinal multisets, sidecar claims and current raw
source hashes. Reviewer did not execute tests or PostgreSQL. No implementation
review restart or RRI was required: this correction adds missing execution
evidence on unchanged implementation bytes, not a semantic change.

Server HEAD5df5f60a6dc4d992c71fc2b160673e4abca6488d; Agent HEAD
e3bd625bbe357b1f3c9620d20bcba121cfb61974. Both staged0/conflicted0; no csproj
delta; Agent tracked delta0. Server remains dirty/untracked; not called clean.
Still OPEN: worker/Activated composition, real Process.Kill recovery,
R2-R6-to-Available, C1/C3, Agent retained/receipt and A3 overall. No stage,
commit,push,A4 or production action/authority is inferred.

## 2026-09-15 — Bodyless R3–R5 worker and real child-kill recovery

Scope: a bounded continuation checkpoint, NOT an Activated host or complete
R2–R6 recovery. The two new internal product files reuse qualified BP09
Reconciler scopes, CP08/CP09, RawExportR3StagingService and
RawExportSourceFinalizationService. One selected Stage/Commit/Publish call
uses the observed immutable actor/lineage and actual CAS tuple, followed by
one readback. No body, R2 upload, key provisioning, guessed revision or second
pipeline enters this worker. Intent/operational/final terminal states are not
allowed to select publication work.

Infrastructure has no Microsoft.Extensions.Hosting dependency. The loop is
therefore host-agnostic RunAsync with an explicit host-stop token; no project
reference/package was added. API registration/Activated qualification remain
OPEN. A single instance refuses overlapping loops, scans exactly100-entry
pages sequentially, and resets its UUID cursor after an empty page. Exceptions
retain durable work and emit only a sanitized warning, not provider/SQL/source
details. This partial selector deliberately does not process NPS/TI/R2/R6 work.

| Current source | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs | 91FDC932BC79267D90DAF45C309A55AC58DBA53728133D03C02EB874FF74E2C5 |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourceContinuationWorker.cs | D5AD65ABDBB76DFC0230C2C8D136DE5033A0EC40EA001AF2CB8B3125B7BB06FB |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 55AB3E0F9223692777E2D9D5254F535D6C16F10306B40D84302C9DE420ABCCCC |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs | 69DDA1F6F75BE8F7713E41AE3CAC45D0E736BD24134F737C1AFB27D9EF19812A |

Only these four source paths changed in this checkpoint: two new product
files, one new13-case class in the existing BrokerPipelineTests file, and
fixture reuse/identity correction in RetentionCheckpointTests. No SQL,
migration, csproj, provider implementation, API registration or Agent edit.

The new class is Tip88C1C6BA3ContinuationWorkerTests. Its13 cases cover one
actual Stage/Commit/Publish/replay path, one sequential loop/overlap refusal,
three explicitly selector-only terminal guards, four real child kill points,
and four R4/R5 failure-payload cases (each with an all-NULL positive followed
by eight independently non-NULL payload mutations and exact SQL restoration).
The result guards belong in the pipeline: the existing R4/R5 repository
materializes the typed record without enforcing those forbidden fields.

ProcessKill_RestartUsesDurablePublicationState starts with actual synthetic
R2 encryption and verified ciphertext. Child1 invokes production publication
stages to cut0=Reserved/Verified, cut1=Staged, cut2=Committed or cut3=Available,
then waits alive at an emitted PID barrier. Parent calls
Process.Kill(entireProcessTree:true), observes nonzero exit and durable state
through an independent connection, and launches a distinct child2. Child2
runs the production worker from CP09/CP08 until Available; input includes only
synthetic connection/configuration metadata and source ID, not raw bytes,
a receipt or an in-memory checkpoint. Assertions preserve exact source,
attempt/key/object/publication identities and whole key/object history.
The SDK Roslyn probe loads the current test and production assemblies; no
new executable project, pwsh dependency or test-local stage implementation.
This is verified-ciphertext R3–R5 recovery, NOT a proof of incomplete-R2,
terminal-intent settlement, R6 cleanup or full Activated-host recovery.

| Evidence in IntegrationTests/TestResults | Executed/pass/fail | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-worker-v2-green.trx | 13/13/0 | 409C7A712154A02E94C266D3515F32866E3077AF420E293238EBC890D0ADB9C5 |
| tip88c1-a3-worker-cursor-red.trx | 4/2/2 | A024546A189720533D91F7AE970BE305BD0C742D4123E2914A7B4BF1AC2D12FE |
| tip88c1-a3-worker-failure-shape-red.trx | 4/0/4 | F8BE5BDD5E7D636924452620588D21DFFC469E619B162E3B0D5278D2A8AAA038 |
| tip88c1-a3-worker-stale-cas-red.trx | 1/0/1 | A1888E25E7A678297AF5A8247E7D2D03DBB771EBB1F182598F3097826C2A60F1 |
| tip88c1-a3-worker-final-regression.trx (FAILED despite filename) | 320/310/10 | 5EBBD9A45BEBB0E0598A8F7ADDA0FA3354EF050AD506790D3A19B5B42E094358 |
| tip88c1-a3-worker-v4-isolation-green.trx | 13/13/0 | B2D66B9A2875213FE342DFCEA695CDF0C53C4DAF61CBDC23550849F43DDB66D3 |

Cursor mutation omits empty-page reset: cuts0/1 fail at restarted child exit
after the first child was successfully killed, while cuts2/3 pass. Removing
both R4/R5 payload guards fails exactly4 tests because the required exception
is absent, not setup. Substituting attempt revision for reservation revision
fails the stage proof at expected Committed/actual NULL. Every product mutation
was restored to its own full raw SHA before the first320 run. The V2 green
TRX predates mutations and is not mislabeled as post-restoration evidence.

The first320 run retained exact old307 + new13 identities, missing0,extra0,
multiplicity mismatch0,duplicates0; all307 old cases passed. All10 failures
were Sequence contains more than one element at the fixture's global attempt
SingleAsync, before worker behavior. Its669-source freeze had drift0/new0.
A disposable database clones existing shared history; it is not necessarily
empty. The correction keeps the actual Completion result, selects its exact
AttemptId using SingleAsync and asserts source/key identity; selects the
writer-returned ObjectCustodyId with attempt/source assertions; scopes new
publication/attempt/object counts to that source. Whole-history equality is
retained. No shared-history reset, First/latest lookup, cardinality weakening
or product correction was used to mask the fixture defect.

PI-TAG-001 review ladder: V1 found one forbidden-result-payload gap; V2 verified
its correction; V3 free adversarial review found0 actionable findings across
terminal routing, observed CAS/lineage, role ownership, real-child restart and
polling boundaries. Full execution then exposed the fixture defect above;
V4 independent source review verified the bounded correction,0 findings.
Cumulative causes: typed records alone did not enforce forbidden payloads,
and fresh-clone assumptions hid historical rows. Review was improved to
inspect adjacent result materializers and shared-fixture lifecycle, not just
new methods and passing counts. Four rounds, no semantic RRI and no unresolved
round5 non-convergence. Reviewers read exact bytes/evidence, did not run PG.

First failed-run comparison:
tip88c1-a3-worker-first-regression-result.json,
695E1B50C37DCCA46B52399E478341B9301A9C7C742D1E7638FE64C1832E7C62.
Corrected source freeze:
tip88c1-a3-worker-v4-source-freeze.json,
20393597A5A05CCADE82125D295B22F96337495802CFCAAAC41F5C80C4DF29B0.
Scope669 =657 .cs +12 .csproj under src/tests, NOT whole repository.

Corrected Unit324/324:
tip88c1-a3-worker-v4-final-unit.trx,
79D9305DE0BF3623AB869880B9AF8EE3EAD9831295656C0C25E32041D9BA187A.
Corrected Arch155/155:
tip88c1-a3-worker-v4-final-arch.trx,
698ED5B287C6C02E41EB7DC49EAC53B0338BE0EB1588C7574B83EC786F7069BF.
Corrected full320 continuity is CLOSED: the complete run executed2026-09-15
12:16:08–12:35:48 +07:00,320 passed,0 failed/skipped. Ordinal testId|testName
frequency equality to old307 plus corrected13: missing0,extra0,multiplicity
mismatch0,duplicates0,non-passed0. The original V2 thirteen identities also
equal the corrected thirteen; no test was removed or renamed to obtain green.
All669 source/project entries were rehashed after execution: drift0,new paths0.
The first320 failed run and its original source freeze remain unmodified.

Final Integration TRX: tip88c1-a3-worker-v4-final-regression.trx,
AB43F1A56886F1A1640BF3479A3AC3FAFA9B9803FCFAF58D1DF0D645D740EDCA.
Exact comparison sidecar: tip88c1-a3-worker-v4-continuity-result.json,
079A6E68470D2C6889F70202F96E86781D847EB641F775076C7924A52FF565F4.
Disposition: PASS_VERIFIED_CIPHERTEXT_PUBLICATION_WORKER_CONTINUITY.
This does not grant full worker/Activated/TI/R2–R6 or A3 overall PASS.

Still OPEN: Activated facade/class resolver, full key/provider composition,
NPS/TI/resource settlement after real kill, R2 verification recovery, R6,
complete R2–R6 recovery, C1/C3 and Agent retained/receipt. Multi-page fairness,
competing worker processes and shutdown during a blocked stage are not
claimed by this bounded test set. A3 overall and landing remain OPEN.
No stage,commit,push,A4/production or Drive synchronization.

## 2026-09-15 — No-provider-start continuation through durable terminal intent

Bounded implementation under ratified A3 v0.6 and its five unchanged companions.
No SQL, migration/model, package/project, provider selection, Agent, or public
API mutation. Exactly three existing A3 product paths and two existing A3 test
paths changed from the prior669 source freeze. R1 admission is still the real
broker; no second R2 pipeline or writer was introduced.

### Invariant trace and proof boundary

| Requirement | Executable owner / state | Discriminating proof |
| --- | --- | --- |
| Missing CP08 object is only a candidate | AdvanceAsync calls NPS01; SQL checks all four absence predicates and locked lease | NoProviderRecovery_UnknownProviderDoesNotBecomeAnAbsenceWitness; live-lease control |
| Operational no-start is not an O20 business result | One NPS01 commit/readback, then a later TI01 call; SQL alone checks original H | NoProviderRecovery_LiveLeaseAndLiveOriginalHorizonRemainNonterminal |
| Pending intent is not finalized cleanup | One TI02 call from stored intent; CleanupPending defers; exact expected code required | ReaderRejectsMalformedSqlResults(finalize:true), deferred commit-failure control |
| Acknowledgement requires commit | Fresh qualified Reconciler transaction, suppressed ambient, local frozen actor, commit before return | CommitFailureNeverAcknowledges(false/true); omitted-commit RED |
| Cancellation observes a real transaction | Host/deadline acts after observed role/database/query backend waits at session lock | HostStopOrDeadlineRollsBackBlockedTransaction(false/true) |
| Crash loses all process state | Production continuation child is killed; different child runs production worker from CP08/CP09 | ProcessKillUsesDurableNpsIntentAndFinalization cuts0/1/2/3; dispatch RED at cut2 |

All method names above have the NoProviderRecovery_ prefix and live in
Tip88C1C6BA3R2TerminalProjectionTests. The four cuts are before NPS, after NPS,
after TI01 and after TI02. The parent first admits R1 through the real broker;
this is a continuation-process kill proof, not a claim that the admission
process itself was killed. Child input contains connections/configuration,
source ID and test observation controls, not raw, receipt or serialized state.
The Terminal flag chooses the test observer's completion condition, not the
worker's durable-state branch. Resource rows and original H remain unchanged;
old attempt/key identities, fence and committed terminal timestamps survive.
The qualified test connections disable pooling: a clean subsequent actor
checkout does not independently prove cleanup on the same physical backend.

### Exact restored source

| Path | Raw SHA-256 |
| --- | --- |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourceContinuationWorker.cs | 3B8D503837FFA0252D0EDACB2C71F3E6C1029EC88A322271B3E5F0A8DC4280B9 |
| src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs | 0AEE5D1C3E2795B9A78D55ED3DBC7FCCDC596AA541EF707AFC7570692BECE8A4 |
| src/TagEkyc.Infrastructure/RawExport/RawExportR2TerminalIntentRecorder.cs | 7121AA6875AFD02BF8AADFD07DC8540B0E467552450281EC53AA09681BA34A0F |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs | 65EB86CCDC752F06E9897AB612C866F9FD8F223B054C69D4418D86F244C3FF6A |
| tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs | 399792911AF61E93A85918B4D939A197627C38B24E6F596E85B34BB97145AA3D |

Source-freeze artifact:
tests/TagEkyc.IntegrationTests/TestResults/tip88c1-a3-no-provider-v3-source-freeze.json
SHA F8D7256B888E15EBAA36CD234B79CF11E7190564817D16A15FBDAC68C874AEA6.
Scope669 =657 .cs +12 .csproj under src/tests, not whole repository.

### RED evidence and restoration

| TRX filename | Executed / passed / failed | Raw SHA-256 |
| --- | --- | --- |
| tip88c1-a3-no-provider-v3-final-unit.trx | 324/324/0 | 02C22F501942D5DA98FED8F3E3E20C2258FED422AA205171F4A26A7376505A14 |
| tip88c1-a3-no-provider-v3-final-arch.trx | 155/155/0 | 72820E9E69FB04421C7341B46FCD485B619D6471E562A7A5A6E06F9F2EBA1132 |
| tip88c1-a3-no-provider-shape-guards-red.trx | 2/0/2 | A5CD33950E7F0A66BFD37D24920964601F7B589C6F4F02CE4C9B1B832313E0BB |
| tip88c1-a3-no-provider-commit-red.trx | 1/0/1 | 360A189910BAE49945ADEB851A79FD0694A7F30A5E0F0F959AB05C8A0615F1D7 |
| tip88c1-a3-no-provider-worker-dispatch-red.trx | 1/0/1 | 424195A12BC13210309A8C07464B64AB64608766D021DDA98B6958DD88C009FA |

shape-guards-red removes two independent checks: NPS forbidden failure payload
and TI02 expected-code equality. Both readers fail because the required
InvalidOperationException is absent, not because setup fails. commit-red
selects the NPS case only and omits ExecuteRecoveryAsync's commit: the expected
deferred PostgreSQL exception is absent. dispatch-red restores the old
publication-only worker call: cut2 reaches durable intent, is killed, and the
restarted child exits91 rather than0 because it cannot finalize. All three
product files were restored to the exact source SHA above before the final
build, Unit/Arch and full regression.

### Failed-run disposition and Review Ladder

Original six new cases passed6/6 in
3AVNN_LEARNPC_2026-09-15_13_31_40.trx,
SHA 9BB41CDC66922FE7F8B2504DAE98F4C6D70EBD886087C9DD6B90C0631DDEC014.
The shell wrapper failed afterward because the logger's semicolon was not
quoted. This is recorded separately; it did not make the six tests fail.

The next six boundary cases initially produced4/6:
tip88c1-a3-no-provider-v2-boundaries.trx,
SHA 089F5FA40E9F7F587B73005AF8C7F29D9866CF13763C6FEA7034239D9D32A591.
Both failures were the lock tests watching a closed warm-up connection's PID
despite Pooling=false. Correction finds the actual waiting backend by exact
database/role/query and then uses the existing pg_locks/session assertion.
No product, assertion or test identity was removed to obtain green.

V1: one Medium actionable proof gap (new reader shapes lacked negative
controls), no confirmed runtime defect. V2: source correction verified, zero
new actionable findings; execution then exposed the single PID-harness defect.
V3: free independent adversarial review after that correction, zero findings.
It read all five files, actual child entry, NPS/TI/CP08/CP09 SQL, roles and
authority; independently checked each RED failure target and all669 hashes.
Its source verdict did not claim the still-running full regression.

Cumulative causes reviewed at V3: proof inventory initially followed legal
flow but omitted new return-shape negatives; copied wait logic assumed pooling
while the qualified fixture explicitly disables it. Improve review by tracing
each newly executed reader's invalid shapes and obtaining the current blocked
backend rather than trusting an earlier connection. Three source-review
passes; round5 checkpoint and round10 stop not reached. No RRI or authority
expansion. Final execution closure follows only from the result below.

### Final execution and continuity

Final Integration: 332 executed / 332 passed / 0 failed / 0 skipped.
tests/TagEkyc.IntegrationTests/TestResults/tip88c1-a3-no-provider-v3-final-regression.trx
SHA 65BF17A82A2E444571FB7D773F932B9EC610DD0B283E16A951D0B3CBAC070F3D.
Run 2026-09-15T13:45:23.5165609+07:00 through
2026-09-15T14:13:52.0691588+07:00 on the exact restored source.

Continuity result:
tests/TagEkyc.IntegrationTests/TestResults/tip88c1-a3-no-provider-v3-continuity-result.json
SHA AAA518633BB2A6348C4BAB2E15BB5F2E5BE4D6AAF85F89ED3CCB8A585C0EEE17.
Prior320 + added12 = expected332 = actual332; missing0, extra0,
multiplicity mismatch0, duplicates0, non-passed0. Added-set input TRX files
are identity inputs only, including the pre-fix harness failure; all12 added
identities passed in the final run. Post-run comparison of all669 frozen files:
SourceDrift0, NewSourcePaths0.

Bounded disposition: PASS — no-provider-start continuation through NPS01,
original-H TI01 and TI02, including four real continuation-process kill cuts.


Still OPEN: resource-present NPS/TI settlement after real kill, incomplete R2
and verification recovery, R6, activated facade/class resolver and complete
provider composition, full R2–R6 recovery, C1/C3 and Agent retained/receipt.
No A3 overall, landing or production PASS. No stage/commit/push or Drive sync.


## 2026-09-15 — Active-key terminal-resource settlement and restart

Bounded implementation under ratified A3 v0.6 and unchanged companions. This
slice starts from an exact durable terminal intent, an Active attempt key and
`NoObjectEstablished`. It does not infer provider absence, call a key provider,
mutate an object, run R6, or open C1/C3/Agent work.

The reconciler reads CP08 and proves the exact current key context. A distinct
Lifecycle scope then commits the existing key-revocation SQL primitive with the
already-durable terminal intent code as its reason. Only a later reconciler pass
may call TI02. This separates resource settlement from semantic finalization and
preserves the immutable intent tuple across both transactions and process death.

### Invariant trace and exact source

| Requirement | Executable owner / state | Discriminating proof |
| --- | --- | --- |
| Active key cannot be treated as settled | CP08 + Lifecycle `raw_export_revoke_attempt_key_reservation` | `TerminalIntent_ActiveKeyIsRevokedByLifecycleBeforeTi02Finalization` |
| Revocation carries the terminal cause, not an R6 label | persisted key + history event + recomputed canonical evidence digest | terminal-reason mutation RED |
| Resource commit and TI02 are different durable steps | Lifecycle transaction first; later Reconciler transaction | real-process cuts 0/1/2 distinguish intent-only, key-revoked and finalized states |
| Restart uses durable state only | child input contains connection/config/source and expected terminal code; production pipeline rereads CP08 | `TerminalIntent_ActiveKeyProcessKillRecoversFromDurableResourceState` |
| No object/provider side effect is fabricated | `NoObjectEstablished` remains exact; pre-settlement provider/object rows stay unchanged and only one key-revocation event is appended | observer assertions before and after restart |

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `1F8BC262EDF42D38F9590AFB38B17CDB6EE75BAC9391FA2DB71E008AE7CB52CE` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `47852AF5817E762C7ADABE27477978F56F713FCFCD7ACE03FB2268EB3385260D` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `E017E6AE059A090FD8881325E04D00AD10E20F7D4579C2AEF6E910DE2C6BD7C8` |

Base 669-file source freeze remains
`tip88c1-a3-no-provider-v3-source-freeze.json`, SHA
`F8D7256B888E15EBAA36CD234B79CF11E7190564817D16A15FBDAC68C874AEA6`.
Post-run comparison found exactly the three authorized changes above, 666
unchanged entries, zero new paths and zero deleted paths.

### Executable evidence

| Evidence | Executed / passed / failed / skipped | Raw SHA-256 |
| --- | --- | --- |
| targeted four new identities | 4/4/0/0 | `7FD74CEB627C2270077C109BE23D5ACD15F170C97314F94A97AE957BEB03F4AA` |
| full terminal-projection class | 77/77/0/0 | `ABD2CFFABE34F55B066C5975839966F7A09700E9CAB52A295821CA2C64F397AD` |
| Unit | 324/324/0/0 | `4DFB95D6FD6FB39DD0CE6D9D6E1AB2C5818E7780715A86FC24302A84D4669467` |
| Arch | 155/155/0/0 | `223937F92543A60ECA92B91EA5F34D2B0D38743A0422E35E628E2485B85533E1` |
| full Integration continuity | 336/336/0/0 | `2DC9C533AFF27CA97964C5308E1EE743A66600CB2762A95F3CB1FE772B6F5AAA` |

The final run executed 2026-09-15T16:06:12.4376837+07:00 through
2026-09-15T16:34:12.5928068+07:00. Exact identity-multiset comparison is
prior332 + added4 = expected336 = actual336, with missing0, extra0,
multiplicity mismatch0, duplicates0 and non-passed0. Evidence sidecar:
`terminal-resource-v2-continuity-result.json`, SHA
`E16807D5B4B1B06568ECADD7FF04687E094ACA87116F138958B1183D68FB3A71`.

Three mutations were executed and restored byte-exact before the green gates:

| Mutation | Executed / passed / failed | Intended RED target | Raw SHA-256 |
| --- | --- | --- | --- |
| disable terminal-resource dispatch | 1/0/1 | cut1 observes Active instead of Revoked | `4DBAF6C82C43E12BCE36B4395B11835FB5BB566A163CBDDBC5CBA123354D3AAA` |
| substitute R6 supersession reason | 1/0/1 | exact reason/evidence digest no longer matches terminal cause | `175099D307DDD626E224AF4A77DFDAF5AF869BE9C98376389E2DEC019FB85FD7` |
| omit TI02 at process cut2 | 3/2/1 | only cut2 remains without `CONTENT_COMMITMENT_MISMATCH` | `62F033EC175F54324FCF65E3DD5929E0775A49769AE6D6ED109C69C15B585B88` |

The earlier `terminal-resource-class-green.trx` is explicitly excluded: two
concurrent PostgreSQL testhosts collided on the shared fixture and produced 46
`57P03 database system shutting down` failures. It is neither product-failure
evidence nor restoration evidence. The later standalone 77/77 class run and
336/336 continuity run are the valid executions.

### Review ladder and bounded disposition

V1 found two Medium defects: the first implementation reused the R6
`SourceFinalizationSupersededAttempt` reason instead of the durable terminal
cause, and cut2 did not prove TI02 finalization. Both product/proof defects were
corrected. V2 independently verified the corrected source and returned zero
actionable findings. V3 then free-roamed the finalized source, SQL/role seams,
process-kill harness and evidence. It independently rehashed all 669 source
entries, reproduced the exact prior332+added4=current336 identity multiset and
verified all three RED targets. One Low ledger overclaim (`provider/object
histories remain empty`) was corrected to the actual invariant: pre-settlement
rows remain unchanged and exactly one key-revocation event is appended. V3 final
verdict: PASS, zero actionable findings.

Bounded disposition: implementation and executable evidence are complete for
Active-key terminal-resource settlement with real `Process.Kill` cuts 0/1/2.
This is not A3 overall PASS. Still open: provider-outcome/cleanup-required key
states, object unknown/cleanup states, incomplete R2 verification recovery, R6,
activated composition, full R2–R6 recovery, C1/C3 and Agent retained/receipt.
No stage/commit/push, A4 or production authority.


## 2026-09-15 — Provider-outcome terminal-key settlement and restart

Bounded implementation under ratified A3 v0.6 and unchanged companions. This
slice starts with an exact durable terminal intent and a key preparation whose
provider wrap completed in the synthetic durable provider journal but whose
response was lost. It does not reconstruct plaintext, begin an object, run R6,
or open C1/C3/Agent work.

The continuation first requests abandonment through the existing Lifecycle
boundary. A later Reconciler pass resolves the exact provider operation outside
any SQL mutation transaction, rereads the full key recovery tuple under a fresh
transaction, compares the 32-byte fingerprint by byte content, and commits the
existing cleanup-required primitive only if the tuple is unchanged. Cleanup is
also a provider operation outside the SQL transaction; its durable result is
acknowledged only after the same exact reread. Lifecycle may finalize key
abandonment only after `CleanedUp|AbsenceProven`; a later Reconciler pass invokes
TI02. Every provider/SQL/lifecycle boundary is therefore a separate durable
restart point.

### Invariant trace and exact source

| Requirement | Executable owner / state | Discriminating proof |
| --- | --- | --- |
| Lost wrap response is not provider absence | `Issued` provider operation is resolved by the configured recovery operation | missing-recovery mutation selects `FinalizeIntent` instead of provider resolution |
| Both authorized no-object shapes remain executable | no object row, or the exact `NoObjectEstablished` sentinel | zero-object mutation skips required key abandonment |
| Provider result cannot commit against a stale key tuple | fresh Reconciler transaction rereads all recovery fields; fingerprint uses `SequenceEqual` | reference-equality mutation rejects the valid second read |
| Cleanup precedes irreversible key abandonment | request abandon -> resolve -> cleanup -> finalize abandon -> TI02 | cleanup-order mutation is rejected before resolution |
| Provider calls are outside SQL mutation transactions | provider resolve/cleanup returns first; exact tuple is reread before one existing SQL primitive commits | six real kill cuts retain only the last committed state |
| Restart has no body or checkpoint injection | child input contains connections/config/source/cause and synthetic provider-journal connection only | different child runs the production worker and reaches the exact durable terminal code |

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `791BD07D5BBF0D3516137DE514AA12EDBDCC642CA1CC4F0D9FA34576662CC368` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `D48169ACADD543688041AF623BF15B4F34D9D886AE4821E48F5382BBC5FDB0C7` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `0046438CE652FDD63FF312467021099DEE2EC8249305E676A925798C95FC46A7` |

Base 669-file source freeze remains
`tip88c1-a3-no-provider-v3-source-freeze.json`, SHA
`F8D7256B888E15EBAA36CD234B79CF11E7190564817D16A15FBDAC68C874AEA6`.
Mechanical comparison found exactly the three changes above, 666 unchanged
entries, zero new paths and zero deleted paths.

### Executable evidence

| Evidence | Executed / passed / failed / skipped | Raw SHA-256 |
| --- | --- | --- |
| sequential provider settlement | 1/1/0/0 | `DCB7EA79B47FBED383A7B5B13A8B1D43FD1307717E688CA7DDEBA10FC94B0614` |
| real `Process.Kill` cuts 0..5 | 6/6/0/0 | `62F8D6976617826EA2AB398CCBA76B187B08879E4894BBBF60E8992789BAAE70` |
| terminal-projection class | 84/84/0/0 | `91B76F34FF5F5005307A9BC2CF838653F4C414D4AA4DC3FB2FFCDE6B259D4BC9` |
| restored focused set | 7/7/0/0 | `8AAE854E059B91244C5EBF83842D51AFC41B27D066BE05C46B0431B2E55327B6` |
| Unit | 324/324/0/0 | `FB2712E623AA9BA196D7473EAB3F2E2DF3CAFF6C9493BB9B6EC2D20862BB1C2D` |
| Arch | 155/155/0/0 | `6F17477276D422BD90ED0617509FFA0E96254BC96D1CC8F241DB56DE0940F26B` |
| full Integration continuity | 343/343/0/0 | `09369835F1FC0B406F619328AC3847751879326A7A02E74EC410889FC1C383BE` |

The final Integration run executed from
2026-09-15T17:24:24.1106222+07:00 through
2026-09-15T17:53:30.9145574+07:00. Exact ordinal `testId` multiset comparison
is prior336 + added7 = expected343 = actual343, with missing0, extra0,
multiplicity mismatch0, duplicates0 and non-passed0. Display names are not used
as identity because TRX truncates two long theories to the same text.
Continuity sidecar:
`A3TerminalProviderCleanup/terminal-provider-continuity-result.json`, SHA
`270001F8B60DB6C99B66801E03E73C0FBBC5C2696A97229E5E2F0CC0DF7D2C09`.

Four mutations were executed and all three source files were restored to the
exact hashes above before every final green gate:

| Mutation | Executed / passed / failed | Intended RED target | Raw SHA-256 |
| --- | --- | --- | --- |
| discard configured recovery operation | 1/0/1 | expected provider resolution, selected premature finalization | `AF72A51C2472D8359865B85CBF34B4F8B7A0750772F389BD2E5B9F60039B2FDA` |
| reject the zero-object-row settlement shape | 1/0/1 | expected abandon request, selected premature finalization | `7661DEAD46825303B531BDA998F329C1112D9A128F8E0903519E03E30E4610E9` |
| compare the fingerprint array by reference | 1/0/1 | exact reread is falsely rejected and resolution returns `None` | `48BE1046BB57B9020495CAA34EEE0991D1F14F7453A39C62AA6FEF797DB623E9` |
| finalize abandonment before provider settlement | 1/0/1 | SQL ordering guard prevents the provider-resolution step | `6A833314D490B882405FD1520C9BA216A2A5ABFDB3B91258B052863E379E9186` |

### Review ladder and bounded disposition

V1 found three implementation/proof defects while exercising the first
candidate: the proof assumed an object row where the valid state has none; the
selector accepted only the sentinel no-object representation; and C# record
equality compared the recovery fingerprint array by reference. All three were
fixed at their owning layer without changing SQL or authority.

V2 traced the final selector through every existing SQL transition and role:
Reconciler reads/resolves/acknowledges, Lifecycle requests/finalizes abandonment,
provider calls hold no SQL mutation transaction, and TI02 remains a later
Reconciler step. No new function, role, provider type or parallel R2 pipeline
was introduced.

V3 checked the actual child entrypoint, all four RED failure targets, exact
source restoration, ordinal test identities, the 669-file freeze and repo
state. An ambiguous restoration patch briefly swapped two branches; the source
SHA mismatch caught it before a green result was accepted, and the final source
was restored to `791BD07D...CC368` before the recorded 7/7 and 343/343 runs.
`git diff --check` reported no whitespace error. Staged and conflicted counts
remain zero.

Bounded disposition: PASS for lost provider response, exact provider-result
recovery, cleanup-required key settlement, key abandonment, TI02, and real
`Process.Kill` cuts 0..5. This is not A3 overall PASS. Still open:
object-outcome/cleanup states, incomplete R2 verification recovery, R6,
activated composition and full R2-R6 recovery, C1/C3, and Agent
retained/receipt. No stage/commit/push, A4 or production authority.


## 2026-09-15 — Terminal object cleanup, positive absence and restart

Bounded implementation under ratified A3 v0.6 and unchanged companions. This
slice settles a terminal-intent attempt whose exact provisional object is still
`ObjectPresentPendingVerification` or `CleanupPending`. It does not recreate
plaintext, issue a second Put, run R3-R6, or infer provider absence from a SQL
NULL or one provider observation.

The worker first commits the existing `VerificationFailed` cleanup-required
transition. It then reads the exact durable object tuple through the Reconciler
scope and closes that transaction before provider I/O. A present object is
deleted through the Lifecycle provider; only a 204 acknowledgement followed by
a fresh exact tuple reread may commit the existing delete-acknowledgement SQL.
An absent object requires two exact positive-absence observations separated by
a quiescence interval; only then may a fresh Reconciler transaction commit the
existing absence-confirmation SQL and its evidence digest. Provider unavailable,
unknown, a reappearing object, or a stale tuple leaves `CleanupPending`. Key
settlement and TI02 remain later worker passes and are unreachable until the
object is `Deleted`, `Quarantined`, or the already-authorized no-object shape.

### Invariant trace and exact source

| Requirement | Executable owner / state | Discriminating proof |
| --- | --- | --- |
| Terminal intent cannot bypass a live provisional object | `ObjectPresentPendingVerification -> CleanupPending` before key/TI02 | bypass mutation selects `FinalizeIntent` and turns RED |
| Cleanup reason retains the semantic cause | existing cleanup primitive receives exact `VerificationFailed` | `SourceConsumed` substitution is rejected by SQL |
| Provider calls do not hold mutation transactions | exact tuple read commits before inspect/delete; fresh tuple is reread before acknowledgement | five real child-process kill cuts recover from durable state |
| A delete response is not durable truth until SQL acknowledgement | lost 204 leaves `CleanupPending`; restart inspects the exact locator | lost-response proof reaches one terminal object event without a second Put |
| Absence is positive evidence, not a missing row | two provider observations plus quiescence and canonical evidence digest | recreate object between observations; one-observation mutation falsely confirms and turns RED |
| Object settlement precedes key settlement and TI02 | only `Deleted|Quarantined|NoObjectEstablished` enters terminal key branch | remove `Deleted` from settlement and expected key step disappears |

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `91FBF7D92F4A113B6FFD05EFAAFE56FA4A2AE3702D6EACD1E9B21111A0F08223` |
| `src/TagEkyc.Infrastructure/RawExport/RawExportSourceFinalizationRepository.cs` | `6C08AA8C8132893C5E72E12AAEE49652A50D33675E07BC0EEA08306E27547D3D` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `A9A57C544B265B501AAD8ED1705DE3C7AE9EA3BF493CF3C86892CC9EECF7A658` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `0046438CE652FDD63FF312467021099DEE2EC8249305E676A925798C95FC46A7` |

Base 669-file source freeze remains
`tip88c1-a3-no-provider-v3-source-freeze.json`, SHA
`F8D7256B888E15EBAA36CD234B79CF11E7190564817D16A15FBDAC68C874AEA6`.
Mechanical comparison found exactly the four changes above, 665 unchanged
entries, zero new source paths and zero deleted paths.

### Executable evidence

| Evidence | Executed / passed / failed / skipped | Raw SHA-256 |
| --- | --- | --- |
| sequential delete, key settlement and TI02 | 1/1/0/0 | `EA84048BF4E7859F5D43005BD11B1BCE904754ECA2326B5B8ECC4818FCF12503` |
| real `Process.Kill` cuts 0..4 | 5/5/0/0 | `23C8DFA34111F7C3585CC15920FFA90FC95429B2E8B091F162F648C4E05B5AEE` |
| lost delete response and positive absence | 1/1/0/0 | `DA56AA79CD6736CB52F87D0BAC0810B8ADC0DC039E7EA41D05144D2A4AFCB180` |
| object reappears during absence quiescence | 1/1/0/0 | `71D8B719B4590832AA3B5EECAD8FEE1CA63D9235F041A7ED74EBC44301CCDE59` |
| restored focused set | 8/8/0/0 | `405B2FF1EFAF1B636E73C05A1035B9828ED28820BC73F46C3ADA2427B0E85AE8` |
| Unit | 324/324/0/0 | `83FEA174CB9D32752F777E8759001D9F05A41CA12CDB915A2FB4C4A4F00E3352` |
| Arch | 155/155/0/0 | `DCED7C438B92ED42B90572920D331CAD11CB3C8B678687F1F10795BBC22A5B2F` |
| full Integration continuity | 351/351/0/0 | `21B97A99F0FED8D235ED8BD1E543F7BFC1AE857F8C9D708C756D345064E365A5` |

The final Integration run executed from
2026-09-15T18:40:50.5530331+07:00 through
2026-09-15T19:11:53.3816979+07:00. Exact ordinal `testId` multiset comparison
is prior343 + added8 = expected351 = actual351, with missing0, extra0,
multiplicity mismatch0, duplicates0 and non-passed0. Continuity sidecar:
`A3TerminalObjectCleanup/terminal-object-continuity-result.json`, SHA
`8EB5DD962D70065E4CEB90272F06EF52507FD7B3A6C49B9E1C75F5A74A75BA1A`.

Four source mutations were executed and all product/test bytes were restored
before the recorded 8/8, 324/324, 155/155 and 351/351 green gates:

| Mutation | Executed / passed / failed | Intended RED target | Raw SHA-256 |
| --- | --- | --- | --- |
| bypass object settlement | 1/0/1 | expected cleanup mark, selected premature TI02 finalization | `857C29234435A5BC0757142BE1C3FD7D08DB18DBAD6EF9EF45DE3B976595BA5A` |
| substitute `SourceConsumed` cleanup reason | 1/0/1 | SQL returns `RAW_EXPORT_FINALIZATION_OBJECT_STATE_CONFLICT` | `5DFA1CD75EB1474F65DA8FC87CD07239380373D4C25829BA078F012370AD5230` |
| treat `Deleted` as unsettled | 1/0/1 | expected key settlement, worker selects `None` | `6BEF78CAE489A1C08620AD71245F36FC60AC7806294CD9CDB8093B7304494174` |
| reuse first absence observation | 1/0/1 | recreated object is falsely confirmed absent | `83032B62B28ECD53CB2F0E651C47D936F8968C487A561DE7432AE6A954B8B8EF` |

One 7-case run is excluded from product/restoration evidence: PostgreSQL fixture
bootstrap returned `EndOfStream` before any test body ran. Its retained TRX is
`terminal-object-restored-v2.trx`, SHA
`B23CE24F2EB1335574BB258B330BE298A6AF6B4FDD01A399D9F4E1B20081DCA5`;
the same exact source later produced the valid 8/8 and 351/351 runs.

Bounded disposition: PASS for terminal object cleanup, exact delete
acknowledgement, two-observation positive absence, object-before-key/TI02
ordering and real `Process.Kill` cuts 0..4. This is not A3 overall PASS. Still
open: nonterminal Put outcome/verification recovery, R6, activated composition,
full R2-R6 recovery, C1/C3 and Agent retained/receipt. No stage/commit/push, A4
or production authority.


## 2026-09-15 — R6 cleanup continuation and real restart

The bodyless continuation pipeline now consumes the existing R6 read/complete/
finalize primitives after R5 has made the winning source `Available`. Each
worker pass selects one durable R6 stage from a fresh CP08/R6 read. Reconciler
owns pending provider resolution; Lifecycle owns exact object deletion, key
revocation/abandonment, cleanup-item completion and publication cleanup
finalization. No new SQL function, provider interface, role or cleanup ledger
was added.

The proof creates one winning verified attempt plus one obsolete verified
attempt. R5's existing census produces exactly the obsolete object/key cleanup
items and excludes the winning object/key. The production pipeline deletes and
revokes only those obsolete resources, completes exactly two cleanup items,
then finalizes the publication from revision 2/Pending to revision 3/Completed.
The winning object remains `VerifiedCompleted`, the winning key remains
`Active`, and the exact winning object is still present in the provider.

Four real child-process cut points cover zero, one, two and all three R6 stages.
The parent calls `Process.Kill(entireProcessTree:true)`, verifies a non-zero exit
and byte-text durable state equality, then starts a different child. The second
child runs the production continuation worker using only connection/config/source
metadata; it reaches `CleanupDisposition=Completed` from the persisted cleanup
items. It receives no raw body, receipt or prior checkpoint result.

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeSourcePipeline.cs` | `5B57659F63FF5B2379BF8DCBCA76E0D12A5C3FB87BC1A19A30B0FB042E2A6C94` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3R2TerminalProjectionTests.cs` | `CCEBE7A997348A725268C8DEC8E1A4DD2F9C4122E03FAC49DB526B305E8DF2FE` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs` | `C05A511EBD24F39C0CB5B05880A44A00E1EB014C5ABBCA1E8434CA6B9CB329F8` |

### Executable evidence

| Evidence | Executed / passed / failed / skipped | Raw SHA-256 |
| --- | --- | --- |
| sequential R6 cleanup | 1/1/0/0 | `8501031203930CE1BAA1620EE909CE0883059306A835CA876F4290EAC9E0C6A2` |
| real `Process.Kill` cuts 0..3 | 4/4/0/0 | `B9E93F24D7E0B3DE336A0BFBD5036A9E8A77674F8042BCF2108D7FC02D0FF4C9` |
| restored focused set | 5/5/0/0 | `DA6D08A99C0CE76ECF1D12B1D97980BB47F5B3019A2CD7C8AA0A5533B01BDCD4` |
| Unit | 324/324/0/0 | `5CBC55015EF22DC0541F7311552CE68232D5E5272B7780444A570B9B17FCF379` |
| Arch | 155/155/0/0 | `F475C0BB3E80BD9DDDC3BBCE7BA65198C593B4F6966D2B4DB1344094E912C59F` |

The finalize-selection mutation changed the production selector so
`NoPendingItem` no longer invoked R6 finalize. The sequential proof turned RED
at the exact assertion `Expected FinalizeCleanup / Actual None` (1/0/1), TRX
SHA `5F1AA6639B4AAE823BB94F40F5CA720578D935EFA223B3751DCB35675AFB2CBB`.
Production bytes were restored before the recorded 5/5, 324/324 and 155/155
green gates.

One excluded discovery run matched unrelated historical `R6_` tests across
multiple fixture collections and raced their shared fixed PostgreSQL port. All
eight failed in fixture bootstrap before test bodies. It is not product or
restoration evidence; the exact fully-qualified A3 methods were then run
serially and produced the evidence above.

Bounded disposition: PASS for R6 selection, role ownership, obsolete-resource
settlement, publication cleanup finalization and real restart. The prior full
Integration continuity remains 351/351; a successor full regression has not yet
been run because C1/C3 and Agent retained/receipt work remain open. This is not
A3 overall PASS. No stage/commit/push, A4 or production authority.


## 2026-09-15 — C1/C3 independent export authority and CP04 ordering

C1 freeze/seal and the exact outer C3 streaming admission now accept a retained
source only when its `SourceRetention` snapshot is still current **and** the
existing post-Completed B2 export authority remains independently valid.
`SourceRetention` is source eligibility, never an export permit and never a
second subject-consent action. A withdrawn E01 reference therefore denies C1/C3
without fabricating a B2 consent event or erasing retained ciphertext/history.

The forward A3 migration replaces the finite landed C1/C3 function bodies while
preserving their signatures, return shapes, owners and grants. Every retained
path locks the complete participating `verification_sessions` set in ascending
PostgreSQL UUID order before retained-reference, source, job, package or delivery
locks. The same prefix is taken on admission and replay paths. `Down` removes the
prefix and retained-authority clauses symmetrically; empty apply/rollback/reapply
restores all 24 predecessor function bodies byte-for-byte before reapplying.

The actual B2 withdrawal callable races real C1 freeze, C1 seal and outer C3
streaming admission in both acquisition orders. The tests use separate real
PostgreSQL connections and inspect `pg_blocking_pids`/`pg_locks`: the loser waits
at the owned session row, not at a test-local advisory surrogate. Admission-first
commits its existing atomic boundary before withdrawal; withdrawal-first denies
admission and leaves no provider/stream residue.

The multi-session ordering proof composes two independently valid frozen source
bindings into one schema-valid synthetic job with input ordinals deliberately
reversed (`high UUID` then `low UUID`). Holding the high row makes the seal
transaction acquire low first and then wait at high; a third transaction is
observed waiting behind seal on low, while seal owns no retained-reference lock.
This proof exercises the actual C1 seal callable and makes no claim that the
current single-session public freeze route itself creates a multi-session job.

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.Infrastructure/Persistence/Migrations/20260913120000_Tip88C1C6BA3RetainedIngressComposition.cs` | `0DFB10EC3456F12BCBC1E374C68ABABAC6D1935A6B41B170DD40E7C087397D59` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3RetentionCheckpointTests.cs` | `8DE6B6A50D0EF038138F97FB4D696E2830D5C90338B5A760E7B7BF7B96506889` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3MigrationTests.cs` | `F025397C6EF6C14F02651638611DBE100E7687B9CE50DDCB4816C492A8DC0DE3` |

### Executable evidence

| Evidence | Executed / passed / failed / skipped | Raw SHA-256 |
| --- | --- | --- |
| focused C1/C3 + CP04 + migration roundtrip | 11/11/0/0 | `51779A28EAD7A4853E32DFF2A8B47661462C54F79377C2BE16CBFD18037EDABC` |
| complete retention-checkpoint class | 57/57/0/0 | `FD56A2C38607BE8C553A38F25B8C24DDADCF8FDB4A5C8420683E64AC7E2588D3` |
| Unit | 324/324/0/0 | `7310130E3261A2D014A7C23BC44E9B3B302EA2EC3CA2D250E380838F84AD175C` |
| Arch | 155/155/0/0 | `3EA8625709CB44D2140188501D33EE47D918B25EC6ACAAD3BF15E162A1A091F0` |

Four production SQL mutations were executed and restored. Removing the session
prefix while retaining reference locks made the withdrawal-first freeze and C3
proofs RED (one intended failure in each two-case run). The equivalent seal
mutation made the seal proof RED at the exact session-before-reference assertion.
Changing only seal's canonical UUID ordering to descending made the multi-session
proof RED at `third transaction must wait behind seal on the lower UUID row`.

| Mutation | Executed / passed / failed | Raw SHA-256 |
| --- | --- | --- |
| remove C1 freeze session prefix | 2/1/1 | `B6405F35647648F2E2F64F494118E5794D394CE302A4175A21B0CCE8B5C13040` |
| remove outer C3 session prefix | 2/1/1 | `9C62C76C0EB7A0D789922E398C1F842FD37C4BFB952E5062543CFE43D9AFD3A9` |
| remove C1 seal session prefix | 1/0/1 | `23C2F85F0C3C679ACBDA1EA218E4AB940DF0D120182843BF80772EFFD46185B3` |
| reverse C1 seal UUID order | 1/0/1 | `BCA734A31A137D4E1684B496B96159B895ACEFD3FDF3A4781BAABF9F945BA279` |

The migration was restored byte-exact to
`0DFB10EC3456F12BCBC1E374C68ABABAC6D1935A6B41B170DD40E7C087397D59`
before the recorded 11/11, 57/57, 324/324 and 155/155 green gates. A mistakenly
started repository-wide Integration run was interrupted and is excluded from
evidence. The bounded 351-case A3 continuity successor has not been run at this
checkpoint; it is deferred until the Agent retained/receipt surface is present.

Bounded disposition: PASS for independent export authority, C1/C3 current-
retention revalidation, all three actual B2-withdrawal race boundaries, canonical
multi-session session ordering and migration roundtrip. Agent retained/receipt
remains open. This is not A3 overall PASS. No stage/commit/push, A4 or production
authority.


## 2026-09-16 — Homeowner bounded allowlist and final-gate correction

Homeowner approved the cumulative bounded A3 correction without changing A3
semantics. The previously omitted
`RawExportSourceFinalizationRepository.cs` is an authorized Server mutation
target only for reason-aware reuse of the existing cleanup primitive and exact
reconcile-context ownership. The Agent
`CaptureRuntimeAgentPorts.cs` read-only classification is corrected to permit
only the optional/default-null retained-owner service-carrier join; the journal
remains exactly six metadata operations and stores no raw material.

The exact 13-Server/9-Agent historical-test compatibility envelope is active
only after a focused failure proves incompatibility with an authorized A3
successor. It permits bounded projection, deterministic fixture, exact current-
schema expectation or single-successor provenance-pin corrections. It forbids
skips, removed/weakened assertions, old-or-new values, arbitrary row selection,
historical migration edits and business-semantic changes. Earlier narrow
allowlist corrections remain cumulative and keep their original constraints.

The A1 canonical DDL catalogue remains independently exact. A3 additions must
be represented as a finite separately enumerated successor projection, never
copied into the A1 master, inferred from the current database/model or accepted
as arbitrary extra rows.

The authorized A3 acceptance runner correction now discovers and executes all
three cross-repository classes: A2 client/server acceptance, A3 client/server
acceptance and the A3 Agent-CRT1/Server-parser proof. Both MSBuild opt-ins are
enabled. The runner provisions the A2 class's isolated loopback PostgreSQL
container, builds/discovers once, executes with `--no-build`, and requires exact
TRX identity equality with no duplicate, missing, extra, failed or not-executed
case. This runner is a final-gate tool; it is not an inner-loop full regression.

No stage/commit/push, A4 or production authority follows from this correction.


## 2026-09-16 — First bounded successor-compatibility batch

Focused execution, never a full regression, demonstrated four historical-test
incompatibilities and one C3 product defect on the authorized A3 successor:

| Path | Pre-edit raw SHA-256 | Exact bounded correction | Post-edit raw SHA-256 |
| --- | --- | --- | --- |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2AuthoritySnapshotTests.cs` | `6A2556D19C655E12B3BC665A13FBEB5BAEE91FDC939ADB89B0DD0FC9AEEAA9B2` | Add only the six A3 successor columns intentionally added to the current authority-snapshot table expectation. | `A43E64B074BAD1E6F3678DD78FE7C9E7A60305D49C2B4F39EC91373B6EF5467D` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2R2DurableCustodyEncryptionTests.cs` | `FD130101174485337F85BFBD8B618B5C81095F68670A737ED3CBA024BC88C74B` | Add current-schema field 35 `AuthorityKind` to the encryption projection only; keep it excluded from the unchanged verification projection. Earlier historical-R3 query projections remain unchanged. | `91703B9982D24A54D4A06DBBEDAA57C30B4CF72A8C56E3B35D05D1248FB13A69` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1CanonicalDdlProjectionTests.cs` | `BDD11CED2852FCF7524BBC303BB85A2BDB34E027A28FF14365A1C0933F5E470E` | Preserve all 631 independent A1 rows and enumerate exactly 22 separately owned A3 column/constraint/index/trigger rows. Unknown/missing/changed A1 or A3 rows and all three original mutations remain RED. | `788FE961337FD0E36EA3684E41D3CE855C86AF1C0AA74B5BF649FBD682826B86` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1ExecutionHttpTests.cs` | `46588810D7674312945FC6DB3ED97AC958597CAD8A133C7CC0615970B4BD9CC9` | Supply one deterministic non-empty BusinessConsumer PrincipalId and use the ratified lowercase `action` member on first request and replay. No outcome assertion changed. | `ED3E03E8770B6F0427971C42E692249DC856DB6D0725FC8B31FA2D6EF97C0A12` |

The first two focused failures were exact current-schema shape mismatches. The
canonical test reported zero missing A1 rows and exactly the 22 authorized A3
successor rows. The HTTP test first moved from `ACCESS_DENIED` to
`REQUEST_INVALID` when PrincipalId was supplied, then proved the remaining
fixture mismatch was PascalCase `Action` against CP11's closed lowercase wire
grammar. No security guard or business outcome was adapted away.

The outer C3 barrier test separately exposed a product defect in the authorized
A3 migration: the added `SELECT AuthorityKind` overwrote PL/pgSQL `FOUND` from
the current-authority resolver. The migration now checks `NOT FOUND` before
that lookup and removes the exact line symmetrically in `Down`; this restores
`INELIGIBLE` rather than the erroneous `UNAVAILABLE` without changing export
authority semantics. Current migration raw SHA-256 is
`91DD833A50E834145D34501F0C793D987A5A34C31CC011402D7DEAF7A485552A`.

Focused successor evidence contains exactly those four compatibility tests plus
the C3 barrier test: 5/5 PASS, zero failed/skipped. TRX
`TestResults/a3-compatibility-focused-v1/a3-compatibility-focused-v1.trx` has
raw SHA-256
`4B9FB565DCD9E63C71C2B33BEE2C0C7D5B1680D99873490A9A87F1E29B5EDB4B`.
No full suite was run. The final corrected acceptance runner has raw SHA-256
`41C39CB5FB8235BF485959A85473E4298AE23DEECF46B9C0CA2E549280A1E08B`
and remains intentionally unexecuted until the final frozen A3 gate.


## 2026-09-16 — Agent retained ownership, raw ingress and receipt closure

The Agent retained path is now closed on the current candidate without widening
the six-slot durable journal. A `RawVault` capture is admitted only after the
managed configuration gate, exact A1 binding/capability handoff and the existing
durable `DelegationAttempted` CAS. Only the current-process CAS winner creates
the private receipt context. A restart therefore has no receipt authority:
neither a six-entry nor an eight-entry receipt can close the durable attempt,
and receipt content cannot be used to infer the lost mode.

Retained plaintext is owned by two bounded leases from capture return through
the one raw-ingress call. The CRT1 request binds the exact ten-line ingress
metadata digest and body commitment. `Available`/`AlreadyAvailable` contributes
exactly one receipt-only `raw_export_source` entry per raw class; the two raw
entries never consume journal slots and persist no raw digest or retry material.
`ResumePending` returns the sanitized `submission_state_unknown` receipt with
`WAIT_FOR_SOURCE_RECONCILIATION`, disposes plaintext and cannot reconstruct or
redelegate after restart.

Static self-review confirmed the receipt close checks all six acknowledged
metadata operations plus the exact two distinct lowercase UUID-D raw entries in
retained mode. Missing, duplicate, unknown or ninth entries fail closed. The
VerifyAndDiscard six-entry path remains separate. Host/UI composition uses the
managed production gate and introduces no local policy, API-key or principal
fallback.

| Path | Raw SHA-256 |
| --- | --- |
| `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeHttpClient.cs` | `1B3477016DC5F30F1E49B31F44C836A48844451388725F097259047014893439` |
| `src/TagEkyc.CaptureAgent.Client/CaptureRuntimeWireCodec.cs` | `A711F693B8AF336253080AC304AFAC07B2F839D3AAF51F7CB2EF0020796D9EFA` |
| `src/TagEkyc.CaptureAgent.Core/CaptureAgentOrchestrator.cs` | `B70E44A47B44608B2C6168D006DCE5E40B7AAFFAD08AB551EB478B17652C38EA` |
| `src/TagEkyc.CaptureAgent.Core/CaptureAgentProductionConfigGate.cs` | `CE5E953AF362AD60D218AB3C03482AB91BC748B8254617EB9FA85900D62B046F` |
| `src/TagEkyc.CaptureAgent.Core/CaptureRuntimeAgentCoordinator.cs` | `1BFF0098CECD1EB819A4A616E7FDF22F4E8B8AFFAB539418BBEBD0AF5954414A` |
| `src/TagEkyc.CaptureAgent.Core/CaptureRuntimeAgentPorts.cs` | `6AC6373367512E7298084254F56F88B881E8352D62E0D0582CDC8C5A7E880FCA` |
| `src/TagEkyc.CaptureAgent.Core/RetainedRawBufferCapacity.cs` | `E2144983BC25CBB2D79023174012163598D6FA3BCCCA2D5375989D052702756F` |
| `src/TagEkyc.CaptureAgent.Core/SensitiveByteBuffer.cs` | `413D5D045868D8D9945F4996C3A3EC1404778C9D06DE9BEA03DBED99BB2B832C` |
| `src/TagEkyc.CaptureAgent.Core/RawExportRetainedSubmissionOwner.cs` | `EA019FC2D66519F72CD06E0572FE7F21DE1D254D0BDAA15941D7D5428DD3E35F` |
| `src/TagEkyc.CaptureAgent.Host/Program.cs` | `A5D2742648436D2CEB38CAE1B8884172FBB59E69CA3998A5A5D7F67EF88A31AD` |
| `src/TagEkyc.CaptureAgent.Ui/CaptureAgentComposition.cs` | `A1807A65E47338A86D85C18C72A0D04D24C12B5A629EFDA422C0CE9C3C8E0070` |
| `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA3RawIngressTests.cs` | `93F6D742740D45AB195207CBBA040F42AA45F116A4A8C41659AE1B39681B0583` |
| `tests/TagEkyc.CaptureAgent.Tests/Tip88C1C6BA3RetainedOwnershipTests.cs` | `AC92C45B5BD46394A0443668ACB0654D13B18079F57C7FD746CB9598BC558EB8` |

Focused executable evidence contains exactly the two A3 Agent classes:
45/45 PASS, zero failed/skipped. TRX
`TestResults/a3-agent-focused-final/a3-agent-focused-final.trx` has raw SHA-256
`89ABCC0BFA2913F08B505DA7AE83EC18D8296FC0DF2F39665345B22133A03687`.
No Agent full suite was run at this checkpoint. Bounded disposition: PASS for
Agent retained/raw-ingress/receipt semantics. Final whole-candidate gates remain
deferred until static self-review and byte freeze complete.


## 2026-09-16 — Final diagnostic successor-compatibility closure

The first complete Server Integration run after candidate freeze was diagnostic:
1335/1339 executed cases passed, four failed, and the one historical manual case
remained skipped. Its TRX
`TestResults/a3-final/a3-final-integration-v2.trx` has raw SHA-256
`16BD255E752DB905B0082B1BFAC7A89B5A1A3747CDD1488E7DF610CCDD801DF9`.
No final claim is derived from that run.

The four failures resolved to three finite successor-compatibility corrections.
Homeowner approved the two additional historical test paths in one bounded
allowlist correction; the authority-snapshot path was already in the exact
compatibility envelope.

| Path | Pre-edit raw SHA-256 | Exact bounded correction | Post-edit raw SHA-256 |
| --- | --- | --- | --- |
| `tests/TagEkyc.IntegrationTests/PostgresPersistenceSliceTests.cs` | `12403F419A009479E3E8481D37D247849D7E1B90FF07C12151448751303DD278` | Move only the two-party start rendezvous ahead of finalization. Preserve two concurrent calls, both original expected xmin snapshots, `Applied`/`StateMismatch` outcomes and all exact persisted-row counts. The A3 completion-selection function now locks the session before the old pre-save hook, so the old hook could no longer admit two arrivals. | `0032F57F60A0BC001F1505557C2AABDAF3BB2A51758D4456860007CF90742DCE` |
| `tests/TagEkyc.IntegrationTests/Tip88C1B2AuthoritySnapshotTests.cs` | `A43E64B074BAD1E6F3678DD78FE7C9E7A60305D49C2B4F39EC91373B6EF5467D` | Add only exact `ck_a3_snapshot_retention_shape` to the closed acceptable constraint-name set. Exact SQLSTATE and the two historical constraint names remain required; no broad exception acceptance was introduced. | `77D4091541E1B770FD9056BEFC24F1EF755831BE5BB11684C4912034CE4AFAA8` |
| `tests/TagEkyc.IntegrationTests/Tip88C1C6BA1NormativeSqlProofTests.cs` | `A981024FB6DE1B401B288D5C0AFD3F78C021469B73F5C7C767776C1B686B9C2B` | Execute the immutable `core_identity` and `expiry` A1 proof bytes on their exact A1 Foundation schema, assert rollback/no residue, then migrate that same disposable database to the current A3 successor and reassert no residue/latest migration. The runtime family remains executed on current schema. No proof SQL, A3 trigger or assertion was disabled or rewritten. | `5B3818229C71A949BC32BC8AC4802FEB3A7F0CCC11CB1C3DA28E032D8E01B22F` |

The old concurrency hook reproduced the same timeout in a focused 0/1 rerun.
The unchanged normative harness failed both named families with
`A3_CAPTURE_AUTHORITY_LINEAGE_MISMATCH`; the authority test rejected the exact
A3 retention-shape constraint name. These are discriminating predecessor
failures rather than weakened successor expectations.

After the bounded corrections, the Integration project built successfully and
the exact affected set contained five cases: the four former failures plus the
unchanged normative `runtime` positive control. Result: 5/5 PASS, zero
failed/skipped, 21 seconds. TRX
`TestResults/a3-final-focused-compatibility/a3-final-focused-compatibility.trx`
has raw SHA-256
`18C3AF0974EA18E8EC244FA93AE2E0AE1A511C3A1B9D8D24724DC140DB1ADDB2`.
No full regression was run during this correction. The next full Server
Integration execution is the single successor canonical final run.


## 2026-09-16 — A3 final candidate gate

Static self-review and the focused compatibility gate completed before any final
full execution. Unit, Arch and Contract gates then passed 324/324, 156/156 and
15/15. The corrected two-repository runner executed the exact A2, A3 and CRT1
parser class census with both opt-ins enabled: 9/9 PASS, zero failed/skipped,
TRX SHA-256
`076739739D89BD2928035B910CE9674D4812A9188EF78F5E3485834B70CE6918`.

Because that opt-in runner intentionally builds a different test graph, the
ordinary Integration assembly was rebuilt with both opt-ins disabled before the
canonical run. One immediately stopped attempt that reused the opt-in assembly
is excluded from evidence; no source byte changed. The rebuilt ordinary project
reported 0 warnings and 0 errors.

Canonical final Server Integration result:

```text
1339 PASS / 0 failed / 1 historical manual skip / 1340 total
duration 1 h 36 m
TRX SHA-256 842C96F7BEC63E78844A057900FDD72BF4D393473CD201A817B75644E1E85CDE
```

Normalized identity continuity against the 1,340-case diagnostic run is exact:
temporary environment-variable suffixes and synthetic GUID values are replaced
only by typed placeholders; missing, extra and multiplicity mismatch counts are
all zero. Both runs contain 1,337 distinct normalized display identities.

Agent final evidence remains valid on unchanged bytes: 307 PASS, zero failed,
one historical manual skip, TRX SHA-256
`7100044F4228F724B436748B9920C887794E543A4B2EDE102B626D9D58A10DCA`.

Exact raw source/project fingerprints were equal before and after all final
executions:

| Repository | `.cs` + `.csproj` count | Pre/post fingerprint |
| --- | ---: | --- |
| Server | 676 | `8F446014F11D207891A585FCD292763F7D0BD0445F2D745D82740A67FB490109` |
| Agent | 70 | `C6E1F17CF5A28B6964B2F528B40B7C72B46D79AEE7A2D67071E181F95AFE993F` |

Both repositories remain staged=0 and conflicted=0. No stage, commit, push, A4
or production action occurred.

The final as-built review packet is
`tip_88c1_c6b_a3_as_built_v0_1.md`, 6,836 bytes / 139 lines, raw SHA-256
`355F796AA3F84A220849E53B02F66835A00F72BFD417A02D71ED011C3F3DE8F0`.

Builder disposition: `A3_IMPLEMENTATION_CANDIDATE_COMPLETE`. Independent exact-
byte review and Homeowner ratification remain required before any landing action.


## Changelog

- Record the approved registry-FK correction and resumed bounded implementation.
- Close E01 correction with 12 real PostgreSQL cases and discriminating RED
  evidence; record independent-review fix and the one omitted R20 test path.
- Apply Homeowner's single-test-file allowlist correction; restore continuation
  after 11/11 focused R20 tests and proceed with CP11 lineage implementation.
- Record CP11 implementation, 324 full unit tests, 32 intermediate PostgreSQL
  tests, discriminating lock/clock mutations and V1/V2 closure; proceed to B2.
- Record approved three-file snapshot-pin provenance rebind without weakening
  the historical tripwires or changing their other bytes.
- Apply the approved two-query historical R3 compatibility correction in the
  one R2 fixture file; preserve predicates, cardinality and all historical proof.
- Close C1/C3 independent export authority and CP04 ordering with real B2
  withdrawal races, multi-session reverse-input proof and RED/restoration evidence.
- Record the cumulative 2026-09-16 two-product allowlist rebind, finite
  historical-test compatibility envelope and exact A2+A3+CRT1 final runner gate.
- Record the first four focused successor-test corrections, the C3 `FOUND`
  ordering fix and 5/5 bounded evidence without running a full regression.
- Close Agent retained ownership, raw ingress and process-local receipt authority
  with 45/45 focused evidence; defer every full gate until final byte freeze.
- Close the four final diagnostic failures with three bounded historical-test
  compatibility corrections and 5/5 focused evidence; reserve the next full
  Server Integration execution for the single successor canonical final gate.
- Close the A3 final candidate gate with 1339/1339 executed Server Integration,
  307/307 executed Agent, 324/324 Unit, 156/156 Arch, 15/15 Contract and 9/9
  cross-repository acceptance evidence; publish the exact as-built review packet.
