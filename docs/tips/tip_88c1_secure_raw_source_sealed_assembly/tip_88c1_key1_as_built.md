# TIP-88C1-KEY1 — As-Built: Operation-Scoped Content-Commitment Service

Maps the landed KEY1 code to the planning brief and records the seam decision
the Homeowner drove: the interface is operation-scoped so an HSM/Transit
backend can replace the in-process one with zero caller change. Design anchor:
`tip_88c1_planning_brief.md` §D4 (capability graph) + §5.6.1 (ContentCommitment).

- **Commit:** `5befc4d` (branch `tip-88a-raw-export-policy-catalog-build`, unpushed).
- **Scope:** key foundation for C1-B2. **No DB migration** (ModelSnapshot unchanged `8EC86A35…`). Key-free at the DB layer.

## The seam (ArchTest-enforced)

The only broker-facing surface (in `TagEkyc.Contracts`):

```csharp
ValueTask<ContentCommitmentResult> ComputeAsync(
    CommitmentKeySelector selector,      // (KeyId, KeyVersion) — identifier, not key
    ReadOnlyMemory<byte> lpPayload,      // caller-built, domain-separated
    CancellationToken cancellationToken);
```

`ContentCommitmentResult` exposes the 32-byte **MAC output** (defensive copy,
redacted `ToString`) or a typed `ProviderFailure`. **No member returns key
material.**

## Landed structure

- **`TagEkyc.Contracts/RawExport/ContentCommitmentContracts.cs`** — `IContentCommitmentService`, `CommitmentKeySelector`, `ContentCommitmentResult`.
- **`TagEkyc.Infrastructure/ProtectedValues/*`** — the SignFlow ProtectedValues resolution layer **copied** in (namespace-adapted, **no SignFlow project reference**): `ProtectedValueMaterialLease` (zero-on-dispose, redacted, finalizer), provider registry (scheme grammar, cycle detection), catalog, `ProtectedValueResolver` (5s budget, 64 KiB cap, fail-closed), `ConfigurationProtectedValueProvider` (scheme `config`), `FixtureContentCommitmentCatalog`. **All effective-`internal`.**
- **`TagEkyc.Infrastructure/RawExport/InProcessContentCommitmentService.cs`** — the **sole** type touching both the operation seam and the key lease: resolve lease → `HMACSHA256.HashData(lease.Material.Span, …)` → dispose (buffer zeroed) → return MAC.
- DI: `AddTagEkycContentCommitment(IConfiguration)`.

`TagEkyc.Application` has **zero** reference to ProtectedValues (verified by grep + ArchTest). The service is **domain-agnostic**: the `TAG-EKYC:…:C1:V1` domain and the 10-field LP layout of §5.6.1 belong to the **caller (C1-B2)**, not here.

## Seam decision (Homeowner-driven)

"Key never exported" is ambiguous between **Model A** (HMAC inside vault/HSM/
OpenBao-Transit — key never in process; locks into HSM) and **Model B** (key
resolved into an isolated in-process service, HMAC in code, key confined,
never crossing to the broker). Brief §D4 requires only the operation-scoped
**interface**, which both satisfy — so it does **not** mandate HSM.

Because the seam is at the **operation** (not at the bytes), upgrading later =
add one `VaultCommitmentService` behind the same `IContentCommitmentService`,
**zero caller refactor**. KEY1 ships Model B with a fixture key; Model A stays a
pluggable later option. SignFlow's `IProtectedValueResolver` (bytes-returning)
is reused **only inside** the Model-B implementation, never exposed to the
broker. The resolution layer was **copied** (not shared) to keep TagEkyc
independent of a consumer's library.

## Golden vector (CC-recomputed, byte-exact)

- Fixture key (NON-SECRET): `3031323334353637383961626364656630313233343536373839616263646566` ("0123…cdef").
- LP payload: `0000000464656D6F` (LP of `demo`).
- MAC: `37A3B8A69368ECEB4943679AF63B8DC39F14A52A894C5F2195BABEB061E99AD3`.
- Independent recompute: `node -e "…createHmac('sha256',k).update(p).digest('hex')"`.

## Gates opened

- **`C1-KEY-VAULT-ADAPTER-GATE`** — real OpenBao-Transit/HSM `VaultCommitmentService` behind the same interface (key never in process).
- **`C1-KEY-CATALOG-GATE`** — real selector→reference/key-version binding (versioned, rotation/validity-aware) replacing `FixtureContentCommitmentCatalog`.

## Verification at landing

Golden vector CC-recomputed; ArchTests prove the seam (Application↔ProtectedValues
isolation; no key-returning public member), lease hygiene (dispose→zeroed), and
typed resolver failure. Per-project suites green; no migration.
