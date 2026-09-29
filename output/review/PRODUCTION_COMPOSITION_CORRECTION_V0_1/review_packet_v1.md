# PRODUCTION COMPOSITION CORRECTION — REVIEW PACKET V0.1 (BOUNDARY 3 NARROW CORRECTION)

## Review boundary

Homeowner ratification remains bounded to four items:

1. Wire the existing `RuntimeOwners` into production API composition.
2. Add the production `IRecipientPackageConnectionFactory` using existing configuration and database-role primitives.
3. Complete `RawIngressBroker` composition with the existing database and service graph, while refusing fixture claim providers in Production.
4. Extend the existing API-key provisioner for exactly the two Site Qualification credentials.

Baseline: `9761963aab14ce83dcc56881178c178d9830c3cb`.

No commit, push, seal re-mint, schema/migration, endpoint, protocol, engine, SignFlow, A3, Layer 2 semantic, or P0 catalog implementation is part of this candidate.

`docs/00_GDRIVE_FILE_INDEX.md` is a pre-existing Homeowner change and is excluded from this packet.

## Boundary 3 correction

The V0.1 predecessor stopped the production flag one layer too early: production broker composition called the same claim-comparison helper that installed fixture content-commitment, subject-token, and custody-profile providers.

The corrected composition now has two explicit paths:

- Development/Test registers the existing fixture commitment, subject-token, and custody-profile providers and resolves the broker graph.
- Production never installs those fixtures. It rejects each detected fixture arm with a distinct code and otherwise fails closed because this repository has no ratified production implementation for the three P0 key/profile catalogs.

The four production outcomes are:

```text
No qualified providers               PROD_RAW_EXPORT_CLAIM_PROVIDERS_MISSING
Fixture content commitment active    PROD_RAW_EXPORT_CONTENT_COMMITMENT_FIXTURE_ACTIVE
Fixture subject token active         PROD_RAW_EXPORT_SUBJECT_REF_TOKEN_FIXTURE_ACTIVE
Fixture custody profile active       PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE
```

`TagEkyc.Api/Program.cs` passes the real environment into custody-profile registration. The final correction keeps `CustodyTimeBoundsState` in every environment, but a Production graph returns before registering `FixtureSourceEncryptionProfileCatalog`, `FixtureKekReferenceCatalog`, or `FixtureCustodyProfileProvider`. It does not throw while the service graph is being registered.

`RawExportCustodyProfileReadinessValidator` remains the authority for the configured profile. Production with `CustodyProfile:Profile=Fixture` is still rejected with `PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE` by readiness, after the host, `/health`, and site-qualification surfaces can exist. The site gate is not converted into a startup gate.

This is deliberately fail-closed. It does not claim that the P0 production key/profile catalogs now exist, and it does not create a marker that would treat an unknown registration as production-qualified.

## Other three ratified boundaries

### 1. Runtime owners

`TagEkyc.Api` calls `AddTagEkycCaptureRuntimeRawIngress(configuration, isProduction)`. When `TagEkyc:RawExport:RuntimeOwners` is present, the existing writer/reconciler/lifecycle owners are composed with exact LOGIN validation, no `Options`/`SET ROLE`, secret-reference-only database strings in Production, the existing object-custody options, and the existing maximum plaintext-window configuration. The absent configuration keeps the Prepared graph lazy.

### 2. Recipient package database factory

`RecipientPackageConnectionFactory` implements the existing `IRecipientPackageConnectionFactory`. It opens the existing preparer/reconciler/lifecycle roles, verifies both `session_user` and `current_user`, requires all three roles to address the same database, and rejects direct connection strings in Production. Recipient readiness opens all three capabilities before declaring the provider ready.

### 4. Site Qualification API-key provisioning

The existing provisioner exposes two fixed profiles only:

- `operator-enrollment` → `OperatorAdmin` + `operator.site-qualification.enroll` + existing client `10000000-0000-0000-0000-000000000001`.
- `capture-agent-measurement` → `CaptureAgent` + `capture.raw-export.site-qualification` + existing client `10000000-0000-0000-0000-000000000002`.

It uses `ApiKeyProvisioningService`; it does not insert rows directly. Wrong profile/client mappings are rejected before secret resolution or database access. The fixed 16-character managed-key prefix parser retains its 512-key round-trip proof.

## Focused proof

### Final local Boundary 3 gate

`production-composition-boundary3-final-restored.trx`: 20/20 PASS, 0 failed, 0 skipped.

`production-composition-custody-registration-green.trx`: 2/2 PASS, 0 failed, 0 skipped.

`production-program-health-green-a1.trx`: 1/1 PASS, 0 failed, 0 skipped.

`production-composition-route-selection-restored.trx`: 2/2 PASS, 0 failed, 0 skipped.

The named assertions cover:

```text
Production + missing providers        distinct missing-provider code
Production + fixture commitment       distinct commitment-fixture code
Production + fixture subject token    distinct subject-fixture code
Production + fixture custody profile  distinct custody-fixture code
Development/Test + fixtures           graph resolves successfully
API Production custody registration   no throw; time bounds present; fixtures absent
Production custody readiness           Fixture rejected with the existing exact code
Actual Program Production host         reaches Build and serves /health
Activated controlled A3 host           RuntimeIngressAsync; site-gate fixed 503 before body
```

The same final source was rebuilt on the Ubuntu lab and retained as `production-composition-boundary3-ubuntu-final.trx`: 20/20 PASS, 0 failed, 0 skipped.

The Activated proof is not a Prepared-handler substitute. Its durable startup row is `Activated`; endpoint metadata names `RuntimeIngressAsync`; `/health` is 200; site health is reachable; raw ingress is fixed-length JSON 503 with the site-qualification code and explicit `Content-Length`; the response is not an activation-evidence/startup failure; admission calls remain zero and request-body read calls remain zero. Because the candidate intentionally drifts revision-22 manifest bytes before review, this controlled proof supplies the unchanged zero-open/DurableWorker seal rather than falsely claiming that the build-generated provider is current.

### Actual Production host proof on the Ubuntu lab

`production-host-proof.txt` and `production-host.log` retain the real process proof. The candidate `TagEkyc.Api` was published from the final corrected source and launched with `DOTNET_ENVIRONMENT=Production` against the existing lab PostgreSQL and secret-reference configuration. It reached the listener, `/health` returned HTTP 200, and `/health/site-transport-qualification` returned a real HTTP 200 `NotRequired` result under the intentionally drift-invalid build seal. No service-registration fixture exception occurred.

This proof establishes that the corrected Production graph reaches `builder.Build()`, starts the real listener, and exposes both health endpoints without the predecessor registration-time fixture exception. It does **not** establish current site-qualification policy evaluation: the drift-invalid generated seal is unavailable, so `CaptureRuntimeSiteTransportQualificationPolicy.Evaluate()` takes its initial `seal is null` short-circuit and returns `NotRequired` before evaluating the required-policy flag, policy version, settings, or installed qualification. The controlled Activated proof above separately establishes the fail-closed site-gate semantics with an explicit valid zero-open/DurableWorker seal; it is not evidence that the current generated provider was exercised by this real process.

After the reviewed correction is committed and absorbed by the authorized evidence-only revision-23 successor, the actual Production-process proof must be rerun before push. With the generated revision-23 seal current (`authority_open_row_count=0`, `DurableWorker`, site qualification required, policy version 1) and no installed lab qualification, `/health` must remain HTTP 200 while `/health/site-transport-qualification` must change from the predecessor `200 NotRequired` short-circuit to HTTP 503 `Missing`. A continuing `200 NotRequired` result is a push-blocking generated-seal/runtime-binding failure. Neither the predecessor proof nor this required successor run qualifies a site or deploys the candidate.

The exact final custody registration source SHA-256 on both Windows and Ubuntu is:

```text
D860F2BE3DF8493A516989346F65FEAB84FAAF92038BF64F9D6988C2D6F5EE17
```

### Registration-time throw mutation

`production-program-registration-throw-mutant-red.trx`: 0/1 RED, 0 skipped.

The mutation restored the exact predecessor behavior: Production registration threw `PROD_RAW_EXPORT_CUSTODY_PROFILE_FIXTURE_ACTIVE` before `builder.Build()`. The actual `Program` regression failed at `Program.cs:45`, so this is not helper-only mutation credit. The source was restored byte-exact to `D860F2BE…EE17`; the final 20/20 local and 20/20 Ubuntu gates are the green successors.

### Distinct-code mutation

`production-composition-boundary3-generic-code-mutant.trx`: 12 passed / 5 failed, 0 skipped.

The earlier mutation collapsed all four production codes to one generic code. The four Broker arms plus the predecessor API custody assertion failed by their named expected codes. Boundary-3 Broker behavior remains unchanged in the final 20/20 gate. Production files were restored byte-for-byte before the final green run:

```text
RawExportSourceClaimComparisonServiceCollectionExtensions.cs
2AD0E5AD525FA172E14FAAE6E92562DF9B66AD88C01DF1329C9184EA4E57AFEE

CustodyProfileReadinessValidator.cs
53C324DFBF3D81FB68BD84B99CBF15A2B9F1B1677905618C08DCC1B9C52B16AC
```

### Current live lab proof

`production-composition-boundary3-live-lab.trx`: 3/3 PASS, 0 failed, 0 skipped.

Against the live lab PostgreSQL instance, this proves:

- writer/reconciler/lifecycle `RuntimeOwners` open with exact `session_user == current_user`;
- recipient preparer/reconciler/lifecycle connections open with exact roles;
- the broker database configuration opens with the exact broker LOGIN, while the production claim graph fails closed with `PROD_RAW_EXPORT_CLAIM_PROVIDERS_MISSING`.

The retained predecessor `production-composition-live-lab-final.trx` is not cited as current production-graph proof. Its third assertion resolved fixture commitment/subject/custody services under a production label and is superseded by the current named lab assertion above.

### Provisioning proof retained from V0.1

The provisioner rejected the wrong profile/client pair before secret or database access:

```text
EXIT=2
SITE_QUALIFICATION_APIKEY_CLIENT_POLICY_INVALID
```

It then created exactly the two lab credentials through `ApiKeyProvisioningService`; no manual API-key row insertion was used. The existing Site Qualification surfaces authenticated the two credentials (operator enrollment HTTP 204, capture-agent run registration HTTP 200).

## Failed-run accounting

`failed_run_census_v1.tsv` retains every failed focused run. It includes the old registration-time throw mutation and the superseded A3 test-harness run where the controlled InMemory host had not registered the already-existing Layer-2 run-store port, causing minimal-API metadata inference to fail before routing. The harness successor registers a never-invoked test stub; it does not change product behavior. No failed run was deleted or renamed.

## Product write-set

Exactly sixteen production/tool files are changed or added:

```text
src/TagEkyc.Api/Program.cs
src/TagEkyc.Infrastructure/Auth/ApiKeyProvisioningService.cs
src/TagEkyc.Infrastructure/Auth/ManagedApiKeyParser.cs
src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs
src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressProductionOptions.cs
src/TagEkyc.Infrastructure/RawExport/CustodyProfileServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyOptions.cs
src/TagEkyc.Infrastructure/RawExport/RawExportSourceClaimComparisonServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/RawExport/RawIngressBrokerProductionComposition.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageConnectionFactory.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageReadinessValidator.cs
src/TagEkyc.Infrastructure/RawExport/RecipientPackageServiceCollectionExtensions.cs
src/TagEkyc.Infrastructure/RawExport/S3CompatibleRecipientPackageProvider.cs
src/TagEkyc.Infrastructure/RawExport/SubjectRefTokenServiceCollectionExtensions.cs
src/TagEkyc.RawIngressBroker/Program.cs
tools/TagEkyc.ApiKeyProvisioner/Program.cs
```

Focused proof is contained in `tests/TagEkyc.IntegrationTests/ProductionCompositionCorrectionTests.cs` plus one bounded host-graph addition in `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs`. The latter reuses the production route mapping and adds no product implementation.

`DurableKeyTopology.ProcessLocalFixture` remains visible in current configuration/code. It is recorded as a separate production-posture debt only; this transaction does not fix or reinterpret it.

## Governance and requested disposition

Seal revision 22 is intentionally untouched. Candidate source drift is expected until review PASS and a later, separately authorized evidence-only successor. No commit or push has occurred.

```text
RuntimeOwners production DI                 TECHNICAL PASS
Recipient package connection factory        TECHNICAL PASS
RawIngressBroker database composition       TECHNICAL PASS
Production claim-provider graph              FAIL-CLOSED / P0 IMPLEMENTATIONS ABSENT
Development/Test fixture graph               PASS
Production host Build + /health               PASS
Activated RuntimeIngressAsync site gate       PASS / 503 BEFORE BODY
Site Qualification key provisioning          TECHNICAL PASS
New endpoint/protocol/engine/schema          NONE
SignFlow/A3/Layer2 semantic change            NONE
Seal/governance change                        NONE
Commit/push                                  NO
```

Review this corrected candidate before any commit, re-freeze, or push.
