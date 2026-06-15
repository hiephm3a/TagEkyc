# TIP-18 Closeout v0.1 - DB / Provider Posture Decision

**File:** `docs/tips/tip_18_db_provider_posture_decision/tip_18_closeout_v0_1.md`
**Version:** 0.1
**Status:** Draft closeout for homeowner/GPT review
**Date:** 2026-06-15
**Planning commit:** `5cd808f` (`docs: open TIP-18 DB provider posture decision`)
**Validation at planning commit:** `dotnet test TagEkyc.sln --no-restore` = 103 passed, 0 failed, 0 skipped

## Summary

TIP-18 is closed as a docs-only DB/provider posture decision. The accepted posture is provider-neutral hold.

TIP-18 does not open runtime implementation. It does not select a production database provider, does not decide EF versus non-EF, does not implement migration policy, and does not authorize schema, packages, repository implementation, infrastructure adapter, or LocalDev adapter work.

TagEkyc remains non-production and non-certified. TIP-18 makes no production readiness, pilot readiness, certification readiness, legal reliance, backup/recovery capability, real durability, transaction consistency, or durable audit-store implementation claim.

## Closed Decision

Accepted TIP-18 posture:

- keep production DB/provider selection deferred;
- keep SQL Server, PostgreSQL, SQLite, cloud DB, document DB, and other providers as future options only;
- keep EF versus non-EF decision deferred;
- keep migration policy and ownership deferred;
- keep `IDurableMetadataRepository` as the Application port from TIP-17;
- keep `DurableMetadataWriteSet` as a future consistency expectation only;
- keep backup/recovery as a future decision requirement only;
- keep any LocalDev adapter as a future candidate slice only, strictly non-production and separately authorized.

## Preserved Non-Goals

TIP-18 preserves all of the following non-goals:

- no runtime implementation;
- no source, test, project, solution, package, or migration change;
- no production DB/provider selection;
- no EF/non-EF decision;
- no migration policy implementation;
- no DB schema;
- no packages;
- no durable repository implementation;
- no infrastructure adapter;
- no LocalDev adapter;
- no production auth implementation;
- no credential store;
- no raw or hashed secret storage;
- no raw artifact, biometric, provider payload, or vault storage;
- no retention, legal hold, delete, or purge enforcement;
- no webhook, outbox, retry, delivery, signing, or replay implementation;
- no public API/DTO/JSON/status/error behavior change;
- no production readiness, pilot readiness, certification readiness, legal reliance, backup/recovery capability, real durability, transaction consistency, or durable audit-store implementation claim;
- no SignFlow runtime/source/database/network/package/internal-model dependency.

SignFlow remains an external consumer profile only.

## Deferred Debt After TIP-18

TIP-18 deliberately leaves these debts deferred:

- production DB/provider selection;
- EF versus non-EF decision;
- migration policy and ownership;
- schema design;
- durable repository implementation;
- infrastructure adapter boundary;
- LocalDev-only adapter decision;
- transaction/audit consistency implementation;
- backup/recovery posture;
- policy catalog durability;
- outbox substrate;
- production auth and credential store;
- vault/raw artifact lifecycle;
- retention, legal hold, delete, and purge enforcement.

## Candidate Later Slices

TIP-18 does not open any next implementation. The following are candidate later slices only:

- DB/provider decision criteria refinement;
- transaction/audit consistency planning;
- LocalDev-only adapter planning, strictly non-production;
- backup/recovery requirements planning;
- policy catalog durability planning.

Each later slice must be separately reviewed and accepted before any implementation, provider selection, package, schema, migration, repository, adapter, or runtime test work begins.

## Validation and Status Verification

Recommended validation for this closeout:

```text
dotnet test TagEkyc.sln --no-restore
```

Status verification must compare `git status --short` against the accepted pre-existing dirty baseline and out-of-band automation dirt.

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

The TIP-18 closeout staged and committed diff must be limited to:

```text
docs/tips/README.md
docs/tips/tip_18_db_provider_posture_decision/tip_18_closeout_v0_1.md
```

Out-of-scope dirty files must be excluded from the TIP-18 closeout commit:

```text
.gitignore
docs/00_AGENT_COORDINATION_BUS.md
tools/TagEkyc.GDriveSync/Program.cs
tools/TagEkyc.GDriveSync/README.md
```

`docs/00_AGENT_COORDINATION_BUS.md` may be touched by the local Codex automation `tagekyc-coordinator-bus` and requires separate review/authorization before commit.

## Closeout Review Checklist

- Confirm TIP-18 planning commit `5cd808f` is accepted as the DB/provider posture decision baseline.
- Confirm TIP-18 is closed as docs-only provider-neutral hold.
- Confirm no production DB/provider is selected.
- Confirm no EF, migrations, schema, packages, repository implementation, infrastructure adapter, or LocalDev adapter are authorized.
- Confirm no readiness, legal reliance, real durability, transaction consistency, backup/recovery, or audit-store implementation claim is made.
- Confirm no SignFlow runtime/source/database/network/package/internal-model dependency is created.
- Confirm TIP-18 opens no runtime implementation.
