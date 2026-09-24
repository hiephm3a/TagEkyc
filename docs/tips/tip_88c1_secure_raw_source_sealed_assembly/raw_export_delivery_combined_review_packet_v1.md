# Raw-export delivery client + TagEkyc product delta — combined review packet v1

**Status:** `READY_FOR_INDEPENDENT_REVIEW / TECHNICAL_PASS_CANDIDATE`

**Date:** 2026-09-24

**Manifest SHA-256:** `FCF6B38EFF3714225CD9F95237B5BB862F51E5900642B50078B675EAC23B1423`

**Purpose:** provide one review surface for the independent delivery client, the bounded TagEkyc HTTP/control-plane and assembly work-source implementation, mutation evidence, restored gates, cross-repository sentinels, and whole-repository failed-run accounting.

This packet supersedes the earlier `STOP / AUTHORITY_HOLD` version of this file and the narrower `raw_export_delivery_server_review_packet_v1.md`.

## 1. Response to CC

CC was right that the standalone-client packet did not cover the TagEkyc product changes made on the same day. It was also right that the production assembly work source now exists, so the deferred-backlog phrase “no production producer” is factually stale.

The prior conclusion that product work had to stop was wrong. The Homeowner repeatedly directed the builder to complete the product and then explicitly rejected using governance as a product-development lock. That is sufficient implementation authority for this bounded bridge. The exact authority has now been recorded in `raw_export_delivery_homeowner_build_authority_v1.md`.

The corrected boundary is:

- build and prove the three required TagEkyc routes and their narrow supporting application/projection code;
- reuse the existing B3 authorization, B4 job, C2 packaging and C3 encryption/decryption mechanisms;
- keep the consumer as an independent client library;
- do not wire the client into SignFlow runtime or change SignFlow architecture;
- do not activate assembly, re-mint the A3 seal, mark P29-P36 implemented, deploy, stage, commit or push.

The implementation and proof campaign therefore continued. This packet contains the completed evidence rather than another authority question.

## 2. Authorized product surface

| Surface | Disposition | Boundary |
| --- | --- | --- |
| `POST /api/ekyc/raw-export/authorizations` | `AUTHORIZED / IMPLEMENTED / PROVED` | Creates authorization through the existing B3 repository. |
| `POST /api/ekyc/raw-export/jobs` | `AUTHORIZED / IMPLEMENTED / PROVED` | Creates job through the existing B4 repository; no second job engine. |
| `GET /api/ekyc/raw-export/jobs/{jobId}` | `AUTHORIZED / IMPLEMENTED / PROVED` | Reads the existing C2/C3 package projection; does not expose raw bytes. |
| Durable assembly work source | `W2 AUTHORIZED / IMPLEMENTED / PROVED` | Uses the existing B4 lease/result path and durable actor identity. |
| Standalone client | `IMPLEMENTED / PROVED / INDEPENDENT` | No SignFlow DI registration or runtime wiring. |
| Assembly activation | `NOT PERFORMED` | Approved topology remains `Disabled`. |
| P29-P36 row closure | `NOT CLAIMED` | Producer existence is proved; row implementation/ratification is not inferred. |

Implementation-authority record SHA-256:

```text
770B7ABB127A5F7B11C33A6699B7731E9A64DAD82EB37A1C6478A83BCFD114B7
```

## 3. Reuse audit — no duplicate engine

The control plane is an adapter over mechanisms already present in the repositories:

```text
authorization route
  -> existing raw-export authorization repository (B3)

job create route
  -> existing raw-export job repository (B4)

job package read route
  -> existing durable job/package projection (C2/C3)

durable assembly work source
  -> existing B4 lease/retry/terminalization commands
  -> existing assembly codec and recipient package crypto

independent client
  -> HTTP DTO/client + recipient-package decoder only
```

No new authorization engine, job engine, package format, encryption engine, TSP engine, raw-image store, or SignFlow runtime integration was introduced.

## 4. Exact TagEkyc product delta since accepted A3 revision 9

There are 12 product-source paths in this slice:

```text
src/TagEkyc.Api/Program.cs
src/TagEkyc.Api/RawExportControlPlaneEndpoints.cs
src/TagEkyc.Application/Ports/RawExportControlPlanePorts.cs
src/TagEkyc.Application/RawExport/RawExportControlPlaneApplicationService.cs
src/TagEkyc.Contracts/RawExport/RawExportControlPlaneContracts.cs
src/TagEkyc.Infrastructure/Persistence/EfRawExportJobPackageProjectionReader.cs
src/TagEkyc.Infrastructure/Persistence/TagEkycPersistenceServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/Persistence/Migrations/20260923090000_RawExportAssemblyDurableWorkSource.cs
src/TagEkyc.Infrastructure/ProtectedValues/ProtectedValueContracts.cs
src/TagEkyc.Infrastructure/ProtectedValues/SecretRefProtectedValueProvider.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRuntimeInfrastructure.cs
src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs
```

Every path and current SHA is pinned in the 71-row review manifest.

## 5. Control-plane proof

Current restored evidence:

```text
component/application gate       2/2 PASS
HTTP + durable-work-source gate  2/2 PASS
```

The HTTP negative test sends a real request to the mapped authorization route without the required scope and proves:

- exact `403 Forbidden`;
- exact required scope;
- application service call count remains zero;
- repository/product work is not entered before authorization succeeds.

Mutation:

```text
remove only the endpoint authorization stop
  -> expected 403, actual 200
  -> raw-export-delivery-a5.trx: 0/1 PASS
```

This is route-level evidence, not only a direct component call.

## 6. Durable assembly work-source proof

The PostgreSQL integration test exercises the compiled migration and current production source, including:

- exact candidate predicates and ordering;
- one real winner under competing acquisition;
- fencing-token propagation;
- durable principal/client/API-key actor provenance;
- lease expiry and reclaim;
- retryable and terminal recording;
- terminal and expired-job exclusion;
- migration up/down/up and ACL behavior.

Four load-bearing mutations are retained:

| Guard | Mutation | Result |
| --- | --- | --- |
| Fencing | Pass `fence - 1` to the existing B4 command | `a3` — `0/1`, exact fenced operation fails |
| Actor provenance | Replace durable candidate principal with `Guid.Empty` | `a4` — `0/1`, durable actor assertion fails |
| Route authorization | Remove only the HTTP authorization stop | `a5` — `0/1`, expected `403`, actual `200` |
| Candidate expiry | Remove `JobExpiresAt > clock_timestamp()` from candidate SQL | `a10` — `0/1`, expired job becomes directly observable as a candidate |

The first expiry mutation attempt (`a8`) remained green because another product layer still rejected the row. The test was strengthened with a direct candidate-SQL observation before crediting the discriminating `a10` RED. A green mutation was not laundered into evidence.

Restored current bytes:

```text
RawExportAssemblyRuntimeInfrastructure.cs
AB3995E38321824FBF2CABF224894C651CD515E54DA6757F470CEA01CE81B2E5

20260923090000_RawExportAssemblyDurableWorkSource.cs
79A34F152089D8109CE525E73510B1903FF7B60B0346510013AD4AAC5D7CB309

RawExportControlPlaneEndpoints.cs
57BBD70CA00B92362496064097B288A6A71FFA47267F50B1AFC39BBC3512B41C
```

## 7. Independent client proof

The standalone client remains under `Codex_SignFlow/src/Clients/TagEkyc.RawExport.Client`, but it is not wired into SignFlow runtime, DI, orchestration or TSP architecture.

```text
standalone client tests                       4/4 PASS
TagEkyc production codec -> public decoder   1/1 PASS
```

The production-codec interoperability proof uses the existing TagEkyc assembly codec and recipient-package crypto service, then decodes through the public independent client. It requires the two canonical biometric classes and rejects missing, duplicate or extra class material. This proves reuse of prior packaging/encryption/decryption work rather than rebuilding it.

## 8. Restoration, build and sentinels

After all mutations, the three load-bearing product files above were restored to the exact pinned SHA values.

```text
unit restored gate             2/2 PASS
integration restored gate      2/2 PASS
Agent dependency sentinel     88/88 PASS, zero skip
Server dependency sentinel   107/107 PASS, zero skip
solution build                 0 errors
staged/conflicted              0/0 in Server and Agent repos
```

The build retains the expected activation-seal drift warning because these reviewed product bytes post-date the revision-9 A3 manifest. The warning is a functioning governance fuse; it is not a build failure and this packet does not re-mint the seal.

Predecessor sentinel failures `a14`, `a15`, `a16` and `a19` were caused by incomplete test composition after the already-approved per-site qualification gate and current migration were introduced. The fixture now supplies the current site qualification and migration without weakening the business assertions. The final `a21` Server sentinel is `107/107` on the restored product bytes.

## 9. Whole-repository evidence census

The census scans both repositories and both ordinary and root-level `TestResults` trees.

```text
TRX total                 1,289
Server TRX                1,118
Agent TRX                   171
failed TRX                  508
failed unclassified           0
current-slice failed          14
inventory count mismatch       0
```

Hashes:

```text
whole-repo inventory
9F114F37F4BEC232F0A121250104A8BE4DB0EFAABBEDDF75917D86BE507F44A0

failed-run census
0FD222E810841EBB87C5AD8B62C966D0D63EE835E101D19E21BBCAF05C3A65F1
```

The 14 current-slice RED runs are all named and classified: four intended evidence REDs, one superseded fixture RED, four superseded sentinel-fixture REDs, four earlier superseded work-source REDs, and one excluded infrastructure diagnostic. Historical REDs remain visible and classified rather than deleted.

## 10. Reachability and governance consequence

The work source now exists in production code, but assembly is not reachable in the current approved product topology:

```text
effective assembly topology       Disabled
DurableWorker registration        conditional and currently absent
A3 seal                            format 2 / revision 9 / count 0
seal approved topology             Disabled
assembly activation performed      NO
```

Therefore the existing P29-P36 backlog basis is partly stale. The successor fact should be:

```text
ProductionProducer          EXISTS
ProducerReachable           NO — approved topology remains Disabled
ProofBoundary               PRODUCTION_COMPONENT_WITH_MUTATION_EVIDENCE
RowState                    unchanged; no row is marked IMPLEMENTED
ReasonClass                 ASSEMBLY_NOT_ACTIVATION_PREREQUISITE
```

This packet deliberately does not edit the frozen ownership table/backlog, change the activation census, or re-mint the seal. That correction belongs to a separate governance transaction after technical review. Product completion is not blocked on that bookkeeping transaction.

## 11. Review disposition requested

```text
Standalone client boundary                  ACCEPT candidate
TagEkyc control-plane routes                TECHNICAL PASS candidate
Durable assembly work source                TECHNICAL PASS candidate
Four named mutations                        DISCRIMINATING
Restored focused gates                      PASS
Agent sentinel                              88/88 PASS
Server sentinel                            107/107 PASS
Whole-repository failed-run accounting      508/508 classified
Unclassified failures                       0

SignFlow runtime architecture               unchanged
Assembly topology                           Disabled
A3 activation census                        0, unchanged
A3 seal                                     revision 9, not re-minted
P29-P36                                     not claimed implemented
Stage / commit / push / production activate not performed
```

Requested independent review: verify the bounded product implementation and evidence as one package. Do not re-open the already-withdrawn authority STOP and do not infer activation or P29-P36 ratification from technical acceptance.
