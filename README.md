# TagEkyc Solution Skeleton

TIP-01 adds a .NET 8 solution skeleton only.

## Projects

- `src/TagEkyc.Api`: Web API host with smoke-only endpoints.
- `src/TagEkyc.Application`: orchestration boundary placeholder.
- `src/TagEkyc.Domain`: minimal enum placeholders only.
- `src/TagEkyc.Infrastructure`: empty placeholder boundary for future infrastructure work.
- `src/TagEkyc.Adapters`: empty placeholder boundary for future adapter work.
- `src/TagEkyc.Contracts`: sanitized contract placeholders only.
- `src/TagEkyc.SignFlow`: transaction-bound consumer placeholder contracts only.
- `tests/TagEkyc.UnitTests`: placeholder smoke tests.
- `tests/TagEkyc.ContractTests`: placeholder contract smoke tests.
- `tests/TagEkyc.ArchTests`: project dependency smoke tests.

## Build And Test

```powershell
dotnet restore TagEkyc.sln
dotnet build TagEkyc.sln -c Release
dotnet test TagEkyc.sln -c Release --no-build
```

## TIP-01 Guardrails

- No business API routes beyond `/health` and `/build`.
- No persistence, migrations, EF, repositories, or storage behavior.
- No mock engine results or adapter simulation.
- No raw artifact handling, cryptography, or production eKYC logic.
