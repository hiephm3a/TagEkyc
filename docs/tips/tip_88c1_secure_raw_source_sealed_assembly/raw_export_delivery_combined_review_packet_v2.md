# Raw-export delivery correction — combined review packet v2

**Status:** `READY_FOR_INDEPENDENT_REVIEW / TECHNICAL_PASS_CANDIDATE`

**Date:** 2026-09-24

**Manifest SHA-256:** `18BAED0415FBE7ACC70C51A60E3D15977D5DD47A0129633E776093764202CB25`

This successor keeps the accepted v1 evidence, closes the three review defects in a3/a4/F1, and fixes the real C5 product blocker that prevented the independent client from reaching the authorization and job APIs with a production-issued credential.

## 1. Response to GPT and CC

Both reviews were correct.

- a3 had not reached the B4 fence comparator.
- a4 used `Guid.Empty` and therefore stopped in request validation.
- the B4 read proof had no same-principal/different-client negative arm.
- C5 issued a delivery-only credential, while the independent client also needs authorization and job-management scopes.
- the two standalone-client projects are not included in `SignFlow.sln`; this packet records a recommendation only and does not alter the SignFlow solution or runtime architecture.

No prior evidence file was deleted or rewritten. All new runs are retained and the failed-run census classifies every RED.

## 2. Product correction — one delivery credential

The successor managed-recipient profile is:

```text
profile  RawExportDeliveryRecipientV2

business.raw-export.authorize
business.raw-export.job.manage
business.raw-export.package.download
business.raw-export.package.references.read
```

The change is implemented at all owning layers:

- `RecipientManagementCodec` defines the exact four-scope set and successor profile;
- `RecipientCredentialAuthenticationPolicy` recognizes all four scopes without consulting the ordinary client-policy fallback;
- `PostgresHashedApiKeyStore` requires exact set equality and the active successor companion policy;
- migration `20260924120000_RawExportDeliveryRecipientCredential` upgrades existing managed policies and active managed API keys and rewrites all five credential/readiness SQL functions;
- the EF model constraint and snapshot require the successor profile.

The first joined C526 attempt exposed a second real predecessor defect: issuance was correct, but the application authentication policy still recognized only the two C3/C4 scopes and returned `CLIENT_APPLICATION_DISABLED`. That run is retained as `PRODUCT_DEFECT_PREDECESSOR`; the successor is `1/1 PASS`. Within the same retained test, the same C5-issued key authenticates all four exact scopes, enters the mapped HTTP authorization/job routes, creates the permit in B3, creates and reads the job in B4, and the recipient also completes the existing real C1-C4 chain. The B3/B4 job and the C1-C4 predecessor fixture are separate jobs; this packet does not mislabel them as one job identity.

The scope mutation removes only `business.raw-export.authorize` and fails `C504-EXACT-ACTIVATION-SCOPE-SET`, listing the remaining three scopes.

The successor migration has a dedicated current-predecessor down/up proof. It observes `C3C4RecipientV1` after Down, `RawExportDeliveryRecipientV2` after reapply, the authorization scope in the issued-credential function, and `HasPendingModelChanges == false`.

## 3. HTTP route closure

The mapped control-plane gate exercises all three public routes in one TestServer exchange:

```text
POST /api/ekyc/raw-export/authorizations        200
POST /api/ekyc/raw-export/jobs                  201 + exact Location
GET  /api/ekyc/raw-export/jobs/{jobId}          200 + exact job/package identity
```

It proves the required-scope sequence `authorize`, `job.manage`, `job.manage`. The pre-existing unauthenticated route case remains in the same `2/2 PASS` TRX and proves service call count stays zero on rejection.

## 4. Corrected a3 and a4 evidence

### a3 — fence comparator

Current source line 162 reads the durable `FencingToken`. The mutation changes only that line to `stored fence + 1`.

```text
raw-export-delivery-correction-a3-fence-mutant.trx
0/1 PASS
first exception: RAW_EXPORT_JOB_FENCE_STALE
```

This is a B4 comparator outcome, not request validation. The production file was restored byte-exact:

```text
RawExportAssemblyRuntimeInfrastructure.cs
AB3995E38321824FBF2CABF224894C651CD515E54DA6757F470CEA01CE81B2E5
```

### a4 — durable actor provenance

The mutation replaces only the durable principal at source line 164 with a different non-empty valid GUID: `88c10000-0000-5000-8000-00000000a401`.

```text
raw-export-delivery-correction-a4-valid-actor-mutant.trx
0/1 PASS
first assertion: Assert.NotNull() Failure: Value is null
```

It passes request validation and fails ownership lookup. The restored test also asserts the acquired request carries the exact durable principal and a positive fence.

## 5. F1 — client-application isolation

The new PostgreSQL test creates two clients under the same principal, gives each a separate real B3 permit and B4 job, and proves own reads are `Found` while both cross-client reads are `NotFound`.

The mutation deletes only this SQL arm at migration source line 488:

```sql
AND i."ClientApplicationId"=client_application_id
```

The named cross-read changes from `NotFound` to `Found` and the test is `0/1`. The migration is restored byte-exact:

```text
20260726145547_Tip88B4RawExportJobFoundation.cs
79E288B87C39145B996501A716AE29283034CA7D501E40EA0AD6B4C750C304F7
```

## 6. Current-byte PASS evidence

```text
restored joined control/work-source/C5/F1 gate       6/6 PASS
standalone public client production-codec decrypt    1/1 PASS
HTTP route gate                                      2/2 PASS
C5 exact successor-profile gate                      1/1 PASS
C5 HTTP B3/B4 + real C1-C4 + production C5 key      1/1 PASS
successor migration down/up/model gate               1/1 PASS
current migration discovery/model gate               1/1 PASS
C5 unit source/composition gate                       1/1 PASS
```

The standalone client decrypt proof produces the encrypted package with TagEkyc production assembly and crypto codecs, then obtains both `ChipDg2Portrait` and `LiveSelfieImage` through the public independent client decoder.

One broad C5 diagnostic is not presented as a green gate:

```text
raw-export-delivery-correction-c5-full-restored.trx  24/25
```

Its only RED is legacy C528, which attempts to roll the database back across the later A3 current-body guarded migration and is stopped by `A3_CAPTURE_CURRENT_BODY_MISMATCH`. It is classified `EXCLUDED_HISTORICAL_OUTSIDE_SLICE`. The bounded successor migration's own Down/Reapply behavior is proved by the separate `1/1 PASS` round-trip.

## 7. Failed-run accounting

The v2 census scans Server and Agent repositories, including root-level TestResults trees.

```text
TRX total                    1,309
Server TRX                   1,138
Agent TRX                      171
failed TRX                    517
current-slice failed           23
historical outside slice      494
unclassified                    0
count mismatch                  0
```

The correction adds nine classified RED runs: `4 EVIDENCE_RED + 3 SUPERSEDED_RED + 1 PRODUCT_DEFECT_PREDECESSOR + 1 EXCLUDED_HISTORICAL_OUTSIDE_SLICE`.

```text
whole-repo inventory v2
6DD88856514E867F2BA514551F461AD051ADA7BC5CF224EDD531CC95E34E9140

failed-run census v2
2C867D02F488C8C34802931086605F42AE9DE14284C35F4601F81318352DDD3A
```

## 8. F2 recommendation — not implemented

Recommended choice: add both independent client projects to `SignFlow.sln` so ordinary solution CI compiles and tests them. This is solution membership only; it must not add a runtime ProjectReference or wire the client into SignFlow DI/TSP architecture.

This packet does **not** implement that choice because the correction dispatch reserves it for the Homeowner. If solution membership is declined, the required explicit CI command is:

```text
dotnet test tests/TagEkyc.RawExport.Client.Tests/TagEkyc.RawExport.Client.Tests.csproj --configuration Release
```

## 9. Boundaries and requested disposition

```text
SignFlow runtime/TSP architecture     unchanged
standalone client                     remains independent
assembly topology                     Disabled
A3 seal                               not re-minted
activation census                     unchanged
P29-P36                               no ratification claim
push                                  not performed
```

Requested review:

```text
C5 delivery credential blocker        CLOSE candidate
authorization + job HTTP routes       CLOSE candidate
a3 fence mutation                     ACCEPT corrected evidence
a4 actor mutation                     ACCEPT corrected evidence
F1 client isolation                   ACCEPT corrected evidence
standalone client delivery chain      ACCEPT candidate
F2 solution membership                DECISION PENDING; recommendation only
```
