# TIP-88C1-C6B-A1 — Typed Gateway and R27 Proof Boundary RRI

**Status:** STOP — BOUNDED EXECUTABILITY CORRECTION REQUIRED  
**Date:** 2026-09-10  
**Authority basis:** Homeowner-ratified Parent v0.12 SHA-256 `BB3B2AEED01753F563B5DC56720172DBD9EB1EA27EE88EEBA11F65038CCB2881`  
**Operation Master:** SHA-256 `B1E3F320C32691756DACEF5A206B6D0303E811377B7304623C43C56ECBEAC277`  

## Purpose

Record two implementation-executability contradictions found while resuming A1.
This report does not reopen A1 semantics, select new operations, authorize A2/A3,
or grant product mutation, stage, commit or push authority.

## Finding 1 — generic gateway surface cannot execute the literal catalogue

Current `CaptureRuntimePorts.cs` exposes:

```text
ICaptureRuntimeManagementGateway.ExecuteAsync<T>(string operation, ..., object? request, ...)
ICaptureRuntimeControlGateway.PublishAsync(string catalogKind, ..., object request, ...)
ICaptureRuntimeExecutionGateway.ExecuteAsync<T>(string operation, object actor, object? request, ...)
```

The corresponding Application services forward ungoverned string literals and
generic `object`/`T`. No Infrastructure gateway implementation exists, and
`CaptureRuntimePersistenceBoundary.cs` is absent.

Operation Master R03–R26 instead freezes exact physical operations, DTOs, auth
contexts, SQL function signatures, result shapes and proof owners. It provides no
authority for a Builder-created string-to-request-to-result registry, arbitrary
generic cast, fallback dispatch, reflection dispatch or default result mapping.

R05 has an additional concrete gap: the wire DTO reaches the enrollment gateway,
while the frozen SQL input requires verified ENROLL1 material and the bootstrap
digest derived with the persisted verifier-pepper version/domain. The current port
does not assign that conversion and dependency boundary to either the Application
service or persistence gateway.

### Required bounded correction

Replace generic dispatch ports with a closed typed port surface mechanically
projected from every applicable Operation Master row R03–R26:

```text
one physical operation
→ one exact method
→ one exact request type
→ one exact actor context
→ one exact response type
→ one exact SQL function/signature
→ one exact public-result mapping
```

The correction must explicitly place R05 ENROLL1 verification, persisted-version
pepper resolution, digest derivation and zeroization before the typed SQL call.
It must not invent operation strings, a generic registry, reflection routing,
`object` requests or unconstrained `T` results.

## Finding 2 — R27 proof owner cannot execute its required proof

Operation Master assigns both:

```text
RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce
RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3
```

to `tests/TagEkyc.ContractTests/Tip88C1C6BA1ProtocolTests.cs`.

That file does not exist. `TagEkyc.ContractTests.csproj` references only
`TagEkyc.Contracts` and `TagEkyc.SignFlow`. The required proof is not pure contract
shape: it must exercise the HTTP/API authentication boundary, assert exact
400/403/503 behavior, observe `Request.Body` read count, and observe whether the A3
admission port was invoked. None of those callable surfaces is reachable from the
current ContractTests project graph.

Creating an API project reference is forbidden by the already-ratified CRT1
proof-owner correction. Creating a test-local endpoint/authenticator/A3 substitute
would be a vacuous proof.

### Required bounded correction

Move sole ownership of both R27 executable proofs to the existing
`tests/TagEkyc.IntegrationTests` project, which already references Api, Application,
Contracts and Infrastructure and already carries `Microsoft.AspNetCore.Mvc.Testing`.
Pin one existing authorized A1 test path as the exact owner; do not add or modify a
project reference. Remove the now-impossible ContractTests file row from the A1
mutation catalogue rather than creating that file.

The proof must preserve the A1/A3 seam:

```text
A1 parses and authenticates CRT1
→ N commits
→ body is still unread
→ A3 admission port is invoked exactly once only on success
```

A1 must not implement binding resolution, R1, body consumption or retained-source
behavior. Activated route selection remains fail-closed while A3 is unavailable;
Prepared remains legacy-only.

## Required review and resume condition

The successor correction must update Parent, Operation Master, port/service
contracts, proof ownership and all derived ledgers together. Review must confirm:

1. every R03–R26 callable row has one typed method and no generic string/object/T
   dispatcher remains;
2. R05 cryptographic preprocessing has one owner and no fallback;
3. every typed method joins to one SQL signature and public result mapping;
4. the two R27 proofs have one executable IntegrationTests owner;
5. ContractTests project graph remains unchanged;
6. R27 never reads body or invokes A3 on denial;
7. R27 success invokes A3 exactly once with the still-unread body;
8. no A2/A3 implementation or route activation is introduced.

Until that successor receives bounded PASS and Homeowner authority, implementation
is paused at this checkpoint.

## Checkpoint evidence

- Full solution build: PASS, 0 warnings, 0 errors.
- CRT1 correction remains PASS.
- Builder/API audit edits: 0.
- Builder/persistence audit edits: 0.
- `CaptureRuntimePersistenceBoundary.cs`: not created.
- `CaptureRuntimeManagementEndpoints.cs`: not created.
- `CaptureRuntimeControlEndpoints.cs`: not created.
- Staged: 0.
- Conflicted: 0.
- Commit/push: not performed.
