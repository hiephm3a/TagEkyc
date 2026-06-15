# TIP-17 Closeout v0.1 - Provider-Neutral Durable Metadata Repository Boundary

**Status:** Draft closeout for homeowner/GPT review
**Date:** 2026-06-15
**Kickoff commit:** `a9b1903` (`docs: open TIP-17 durable metadata repository boundary kickoff`)
**Implementation commit:** `f6f65b8` (`feat: add TIP-17 durable metadata repository boundary`)
**Validation:** `dotnet test TagEkyc.sln --no-restore` = 103 passed, 0 failed, 0 skipped

## Summary

TIP-17 implemented the provider-neutral durable metadata repository boundary only. It added domain/application contracts and tests that define the metadata shape required by later durable-state work without selecting a database, implementing an adapter, promoting LocalDev API keys into production credentials, or changing public API/DTO behavior.

This closeout does not open TIP-18 and does not authorize a persistence implementation.

## Scope Implemented

- Added Application port `IDurableMetadataRepository`.
- Added durable metadata records for:
  - session metadata;
  - actor credential metadata;
  - audit identity metadata;
  - evidence package metadata;
  - completion authority metadata;
  - atomic metadata write set.
- Added Domain value objects/enums:
  - `PrincipalId`;
  - `CredentialRef`;
  - `CredentialType`;
  - `CredentialStatus`;
  - `ScopeGrantSetId`.
- Added TIP-17 unit tests for safe credential references, redacted value-object string output, actor category typing, LocalDev API key separation, and metadata write-set shape.
- Added TIP-17 architecture tests for dependency boundaries, BusinessConsumer contract isolation, repository contract location, and SignFlow runtime isolation.

## Security And Boundary Guarantees

- `CredentialRef` is a safe non-secret reference only.
- `CredentialRef.ToString()` is redacted as `[credential-ref]`.
- `PrincipalId.ToString()` is redacted as `[principal-id]`.
- `ScopeGrantSetId.ToString()` is redacted as `[scope-grant-set-id]`.
- Actor category metadata uses existing `AuthenticatedCallerCategory`, not raw string category fields.
- No raw secret storage was added.
- No hashed secret storage was added.
- LocalDev API keys are not promoted to production credentials.
- BusinessConsumer public contracts do not expose TIP-17 durable metadata.
- Application does not reference SignFlow runtime.

## Preserved Non-Goals

TIP-17 preserved all of the following non-goals:

- no DB/EF/migrations/packages;
- no Infrastructure adapter;
- no LocalDev adapter;
- no durable repository implementation;
- no production auth implementation;
- no credential store;
- no secret backend;
- no raw or hashed secret storage;
- no raw artifact/biometric/vault storage;
- no retention/legal enforcement;
- no webhook/outbox/retry/delivery;
- no crypto/signing/replay;
- no provider/vendor integration;
- no public API/DTO/JSON/status/error behavior change;
- no pilot/production/certification readiness claim;
- no SignFlow runtime/source/database/network/package/internal-model dependency.

## Deferred Debt After TIP-17

- Actual durable repository implementation.
- DB/provider choice.
- EF/migration policy.
- Infrastructure adapter.
- LocalDev adapter decision.
- Transaction consistency implementation.
- Durable audit store.
- Policy catalog durability.
- Completion authority production decision.
- Retention/legal hold/delete enforcement.
- Outbox substrate.
- Backup/recovery.
- Production auth / credential store.
- Vault/raw artifact lifecycle.

## Recommended Next Step

Do not open TIP-18 implementation immediately from this closeout.

The safest next governed slice is a DB/provider posture decision TIP. TIP-17 intentionally stopped at provider-neutral contracts, and the next irreversible choice is not code shape but persistence posture: DB/provider selection, migration policy, transaction model, and whether LocalDev should get an adapter before production persistence.

Other valid later slices, after review, include:

- LocalDev-only durable metadata adapter kickoff;
- transaction/audit consistency planning;
- policy catalog durability planning.

## Closeout Review Checklist

- Confirm TIP-17 implementation commit `f6f65b8` is accepted as the durable metadata boundary baseline.
- Confirm validation remains 103 passed, 0 failed, 0 skipped.
- Confirm no TIP-18 implementation is opened by this closeout.
- Confirm the known dirty GDriveSync tooling files remain unrelated local tooling and are not part of TIP-17 closeout scope.
