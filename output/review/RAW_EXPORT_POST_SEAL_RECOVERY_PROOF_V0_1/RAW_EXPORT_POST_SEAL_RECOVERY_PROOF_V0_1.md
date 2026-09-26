# RAW-EXPORT POST-SEAL RECOVERY — FINAL BOUNDED REVIEW PACKET V0.1

## Disposition

The four accepted post-seal recovery defects, the two accepted R1/R2 review
corrections, and the remaining legacy-finalize capability finding are corrected
on current bytes.

The final narrow correction is committed at
`d9c0e6cb090ef109920da007645d26df2243f1fa`. Its retained evidence and failed-run
accounting snapshot is
`64e5aadc15525eb0eb228f59edc4ce5402ea7e68`.

| Area | Result |
|---|---|
| F1 durable post-seal rediscovery | **PASS** |
| F2 recovery-aware result recording | **PASS** |
| R1 post-lock claim eligibility revalidation | **PASS** |
| R2 mandatory NULL duration rejection | **PASS** |
| Owner/generation/lease claim fencing | **PASS** |
| Legacy unfenced finalize capability | **REVOKED FROM CURRENT SEALER / PASS** |
| Current runtime gate | **24/24 PASS, zero skip** |
| Original C105/C116/C124/C126 sentinel set | **6/6 PASS, zero skip** |
| Adjacent durable source + migration discovery | **2/2 PASS, zero skip** |
| Same-Job public/RAW-ingress SDK E2E | **2/2 PASS twice, zero skip** |
| C125 | **OUT OF SCOPE / UNCHANGED** |
| Process kill / OS restart | **NOT PROVEN** |
| Layer 2 site measurement ownership | **PAUSED / UNCHANGED** |

No seal/governance re-freeze and no push are part of this packet. The current
activation seal is intentionally stale until both independent reviewers accept
the product/test correction.

## Repository posture

```text
Baseline reviewed HEAD       e1e4b6782c2ff0e24c4652222e772245acd2e7f4
Legacy-finalize correction   d9c0e6cb090ef109920da007645d26df2243f1fa
Evidence snapshot            64e5aadc15525eb0eb228f59edc4ce5402ea7e68
Push                         NO
Deploy                       NO
Seal/governance re-freeze    NO
Layer 2                      NOT STARTED
SDK delta                    0
API/raw-ingress delta        0
Site-qualification delta     0
Schema-object delta          0
User file preserved          docs/00_GDRIVE_FILE_INDEX.md
```

The current seal is expected to report
`ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT` because product/test bytes
changed and re-freeze is explicitly reserved until both reviewers accept this
packet.

## Search-before-change result

```text
SEARCHED:
  production migration catalog and grants
  readiness capability allowlist
  assembly repository callers
  post-seal recovery and original C105/C116/C124/C126 tests
  durable-source, migration-discovery and same-Job SDK adjacent proofs

FOUND:
  raw_export_record_assembly_finalized(uuid,bigint,bytea) remained executable
  by tagekyc_raw_export_assembly_sealer, remained required by readiness, and
  had a repository entrypoint, but did not validate recovery claim owner,
  generation or lease

REUSED:
  current post-seal claim table and claimed-finalize capability
  current readiness capability inventory
  current production migration and rollback framework
  existing original and adjacent proof suites

NOT REIMPLEMENTED:
  assembly engine, work source, raw ingress, SDK, API, A3, P29-P36,
  site qualification, recovery schema, or recovery claim protocol
```

Repo-wide production-call search found no current caller of the old repository
`RecordFinalizedAsync` method. Removing that dead method alone would not have
closed the finding because the SQL capability was still granted to Sealer.

## Legacy-finalize correction

The current migration now revokes Sealer EXECUTE on the historical function:

```sql
REVOKE EXECUTE ON FUNCTION
  tagekyc.raw_export_record_assembly_finalized(uuid, bigint, bytea)
FROM tagekyc_raw_export_assembly_sealer;
```

The function is retained for migration-history compatibility. `Down()` restores
the predecessor grant, while reapplying `Up()` removes it again. No function,
table, endpoint, contract field or schema format was added or removed.

The production repository's unused legacy entrypoint is removed. Readiness no
longer treats the legacy grant as required; it now explicitly requires that
Sealer **does not** hold that EXECUTE capability. The current claimed-finalize
path remains the only Sealer-accessible production capability for moving a
post-seal obligation to `Finalized`, and it continues to require owner,
generation and lease fencing.

## Direct bypass proof and mutation

`PostSealRecovery_legacy_finalize_capability_cannot_bypass_current_claim`:

1. creates the `SealCommitted` obligation through the production path;
2. establishes the current recovery claim;
3. calls the historical finalize function using the actual Sealer login with
   the valid preparation revision and fingerprint;
4. requires PostgreSQL insufficient privilege;
5. verifies the preparation and live claim residue are unchanged.

The single mutation changes only the new `REVOKE` back to `GRANT`. The direct
proof becomes **0/1 RED** because the historical function executes and the
required insufficient-privilege assertion is not raised. After restoration,
the proof is present in both the **24/24** runtime gate and the **6/6** sentinel
gate.

This is a SQL capability-boundary proof. It does not claim an anonymous HTTP
exploit, duplicate delivery, or process-restart recovery.

## Current restored evidence

### Runtime correction gate

`runs/correction4-runtime-restored-final/post-seal-correction4-runtime-restored-final.trx`
is **24/24 PASS, zero skip**. It contains the complete current
`PostSealRecovery_*` set plus migration Up/Down/Reapply on the final restored
source bytes. It includes the direct legacy capability proof.

### Original sentinels requested by the independent review

`runs/correction4-sentinels-restored/post-seal-correction4-sentinels-restored.trx`
is **6/6 PASS, zero skip** and contains:

```text
C105_resolver_and_sealer_acl_manifests_are_separate
C116_global_lock_subsequence_is_preserved
C124_schema_function_owner_and_acl_shapes_are_exact
C126_disabled_is_valid_and_fixtureproof_is_nonproduction_only
Post_seal_recovery_apply_down_reapply_preserves_catalog_security_and_guards
PostSealRecovery_legacy_finalize_capability_cannot_bypass_current_claim
```

This corrects the predecessor packet's overstatement: C105/C116/C124/C126 were
not members of its 23/23 run. They now have a named current-byte run identity.

### Adjacent proof groups requested by the independent review

`runs/correction4-adjacent-restored-final/post-seal-correction4-adjacent-restored-final.trx`
is **2/2 PASS**:

```text
Durable_source_owns_candidate_concurrency_recovery_result_and_acl_contracts
A3_MigrationDiscovery_FromEmptyMatchesCurrentModelAndHistory
```

`runs/correction4-adjacent-e2e-context-owned/post-seal-correction4-adjacent-e2e-context-owned.trx`
is **2/2 PASS**, and the independent repeat run
`runs/correction4-adjacent-e2e-context-owned-repeat/post-seal-correction4-adjacent-e2e-context-owned-repeat.trx`
is also **2/2 PASS**:

```text
Public_sdk_uses_one_job_through_durable_assembly_listing_delivery_and_decode
Raw_ingress_publications_feed_the_same_job_durable_delivery_and_sdk_decode
```

These four named adjacent proofs cover the durable production work source,
current migration discovery/model, public same-Job SDK delivery, and raw-ingress
to the same Job and SDK decode.

### Repeated test-fixture failure was investigated, not called transient

Two predecessor E2E runs each failed one of two cases because the
TestServer-owned unpooled Npgsql data source was disposed between cases. A
unique connection-pool identity alone did not fix it. The final test harness
owns one explicit `TagEkycDbContext` for both variants and registers the actual
application/control-plane repositories and services around it. Product APIs,
authorization, durable work source, assembly, delivery and SDK decode remain
real. Both cases then pass twice.

The two failed runs remain retained and classified; neither receives product
credit. They are not classified as another transient retry.

## Migration and readiness proof

The migration proof now checks the legacy Sealer EXECUTE capability explicitly:

```text
after Up       false
after Down     true
after reapply  false
```

It still compares complete current function definitions and preserves owner,
`SECURITY DEFINER`, `search_path`, ACL and model cleanliness. C105 verifies the
separate resolver/sealer ACL manifests. C124 verifies the exact production
function/ACL surface and proves that re-granting the old capability makes
readiness fail. C126 verifies the default graph and non-production fixture
boundary.

## Mutation accounting

The predecessor packet's thirteen credited mutation boundaries remain retained.
The final correction adds one fourteenth discriminating boundary:

| Mutation | Result |
|---|---:|
| Re-grant legacy unfenced finalize to Sealer | **0/1 RED** |

All product mutations were restored before the final runtime, sentinel and
adjacent gates. Non-discriminating mutants and superseded harness attempts
remain retained and receive no product credit.

## Exact final correction write-set

Production/migration:

```text
src/TagEkyc.Infrastructure/Persistence/Migrations/20260926120000_RawExportAssemblyPostSealRecovery.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRepository.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs
```

Tests:

```text
tests/TagEkyc.IntegrationTests/RawExportAssemblyPostSealRecoveryMigrationTests.cs
tests/TagEkyc.IntegrationTests/RawExportDeliverySameJobEndToEndTests.cs
tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs
```

No SDK, API, raw-ingress, Agent, site-qualification, A3 partition/ledger,
P29-P36 or project/solution source changed. C125 is untouched.

## Evidence accounting and portable hashing

`failed_run_census_v1.tsv` classifies **99/99 failed results across 44 retained
failed runs**. The final correction adds seven classified failures: one
discriminating product mutation, three superseded combined-gate failures, one
superseded migration-test syntax defect and two superseded E2E fixture/lifetime
failures.

`evidence_manifest_v1.tsv` contains **90 entries**. Every recorded SHA-256 and
byte count is over Git object content at evidence snapshot
`64e5aadc15525eb0eb228f59edc4ce5402ea7e68`, identified per row as
`GIT_OBJECT_CONTENT@64e5aad`. The reference verification procedure is:

```text
git cat-file blob 64e5aadc15525eb0eb228f59edc4ce5402ea7e68:<path>
→ byte count and SHA-256 over those exact bytes
```

Working-tree hashes are not authoritative because checkout line-ending
conversion can change them. The packet and manifest exclude their own hashes to
avoid a circular dependency.

## Final boundary

```text
POST-SEAL RECOVERY CORRECTION       TECHNICAL PASS / READY FOR REVIEW
Legacy-finalize correction          d9c0e6cb090ef109920da007645d26df2243f1fa
Evidence snapshot                   64e5aadc15525eb0eb228f59edc4ce5402ea7e68
Seal/governance                     INTENTIONALLY STALE; NOT RE-MINTED
Process kill / OS restart           NOT PROVEN
C125                                OUT OF SCOPE / UNCHANGED
Layer 2                             PAUSED
Push / deploy                       NO / NO
```

After both independent reviewers accept this packet, the next action is one
minimal evidence-only re-freeze. It must not alter product semantics.
