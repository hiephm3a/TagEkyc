# A3 activation-scope re-baseline — grouped governance review packet v1

## Requested disposition

The Homeowner approved the seven-row/four-mechanism scope and continuous-validity correction. This packet requests review of one governance transaction, **not** proof or ratification of the seven retained rows. A3 remains HOLD and Activated remains blocked by a nonzero authority-open count.

## Exact 46 → 7 accounting

`a3_activation_scope_partition_verify.ps1` derives the predecessor 46-row set from the old partition and reconciles the current ledger, canonical ownership registry, retained partition and deferred backlog:

```text
prior_open=46
reconciled_prior_ratification=1          E01_Race_IssueWithdrawal
deferred_assembly=8                       P29–P36
deferred_candidate=30                     CANDIDATE_ONLY except first-body fence
retained_activation=7
deferred_backlog_rows=38
unassigned=0 duplicate=0 extra=0
```

The seven retained identities are P04, P05, `A3_AgentRawExpectDoesNotSendBeforeCommittedR1`, BP03, BP10, BP13 and `Agent raw HTTP Expect → durable server B/R1 before first body byte`. Their mechanism counts are TRANSPORT-HEADER-GUARD 1, ADMISSION-CAPABILITY 1, TRANSPORT-EXPECT-FENCE 4, ADMISSION-CAPACITY 1. Deferred rows retain prior proof status and production-gap data; no deferred row is marked `IMPLEMENTED`.

The E01 ledger correction is anchored in the pre-existing statement that all ten E01 rows were independently ratified. It is not a fresh ratification or test run. The old 46-row partition remains an immutable predecessor; the seven-row activation partition is a new file.

## Authority and continuous validity

The exact approved decision is `a3_activation_scope_rebaseline_decision_v1.md` (SHA-256 `7E9ACCC89634E1CACB6A76FF3FAF56C2BAF26EBE656BBB2CF5C7C970C7923766`). It pins ownership registry `56EC0194…CCFC07E`, retained partition `B7AC9BB6…77C91E3`, ledger `5CA3C1AB…A04E2C`, count 7, `Activated-Includes-Assembly: NO`, topology `Disabled`.

`a3_outcome_ownership_table_v1.tsv` is the canonical A3 authority-classification registry. A new authority document has no activation effect until represented in this registry in the same governance transaction. Its SHA, plus exact cited authority-source bytes, are bound during seal generation. Authority/topology drift requires a new Homeowner decision; it never silently edits the census. The live host's effective `RawExportAssemblyOptions` topology is compared with the approved topology in the generated seal before routes are selected.

The earlier guard packet's mutation remains a predecessor proof for the equality guard: 17/14/3 unit and 11/10/1 host under the narrow comparison removal, followed by 17/17 and 11/11 restored. The narrow reviewer correction removed the hard-coded `Disabled` value from runtime and generator policy. Current `CaptureRuntimeStartup.cs` SHA-256 is `20D0052998D0A298E9D5AFD9616AE37E8F35824D052DDCF9F8E497AF8171B6E2`; its format-2 branch accepts only a recognized approved topology whose build and effective values match. Current focused unit gate is 20/20 (`338F6C7DFECA811F43C28AC606433F5ED9FC955E767DF3221845C20F85EB2E53`); pre-transaction host gate was 11/11 (`E42540FF7FA4B47BA937EC6A782E06B84412E1D0DBCA2B7ED93D853419EA2633`). Generator scope self-tests are 7/7, including a future approved `YES`/`DurableWorker` pair and rejection of contradictory `YES`/`Disabled`. Those synthetic future-scope tests do not authorize assembly now.

The additional real-host test deliberately leaves the actual generated provider registered. On the format-2/count-7 descriptor it obtains `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`, not `INVALID` or an Activated route. The focused host class passed 12/12 in `a3-activation-scope-generated-seal-host.trx` (SHA-256 `4F10376AED2F5AF4C24165BF16A00E7EE2F813EA39243B70FFE2FD340D64EF1E`); test source SHA-256 is `4BC363233D1E25EB7C67F9D8B131FCE7DE4FE5738A3B5DBD36F53FC74BFC4175`. The generator now invokes the successor manifest verifier for format 2, so a signed manifest with any current-byte drift cannot be used to re-mint merely because its own file SHA still matches the descriptor. The cumulative mutant registry includes the topology mutant and has 106 distinct source hashes; the manifest verifier requires zero live matches. Generator source SHA-256 is `816A30095B692FE00E7FD794F0A461715C9AD6FD90CDB60530F4D4A9733EE4B5`.

The generator has two modes. Explicit `-Strict` is required for a governance re-mint and rejects current-byte drift. Ordinary builds invoke the same strict checks, but on failure compile a **null** evidence provider rather than blocking Prepared builds; Activated then fails `INVALID`. A pre-freeze probe after deliberately leaving the manifest stale showed ordinary mode emit `Current => null` with `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT`, while `-Strict` rejected the same state. No source/descriptor mutation or TRX was needed for this diagnostic. The final strict run must pass after the manifest is rebuilt.

## Freeze requirements

The successor manifest verifier inventories every Git-visible file and every retained TRX in both repositories. Its four derived exclusions are the manifest itself, its sidecar, the ratification record that must cite the manifest SHA, and the seal descriptor that must cite the record SHA. The scope decision, ownership table, retained partition, ledger, deferred backlog, scripts, current product/test bytes and all nine topology-guard TRX remain inside the manifest. The manifest SHA is inserted into the exact Homeowner ratification record; the record SHA and all approved/build identities are then pinned by the format-2 descriptor and compiled provider. The final build must show approved/build count 7 and equal partition/ledger/topology identities; the focused current-host test above shows this nonzero seal still returns `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`.

No full suite, stage, commit, push, landing or production activation is authorized.
