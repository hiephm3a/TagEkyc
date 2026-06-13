# TIP-13 Application Authorization Boundary Foundation Closeout v0.1

## Final Status

TIP-13 Option A: CLOSED

Implementation commit: `6b9c67248cd05e47a2f71ef4e5cc2e10968ecdf0`

Kickoff commit: `ec9c19669fed745a037627d5f756d863889be29f`

Planning predecessor: TIP-12 commit `cc42076299ed7a1d7fac4f64ced554740633f2b4`

## Scope Implemented

- Application-layer authorization boundary hardening using current LocalDev auth only.
- Centralized current actor, scope, and client-ownership checks through `ApplicationAuthorization`.
- Preserved BusinessConsumer / CaptureAgent / TrustedAdapter separation.
- Preserved Operator/Admin/System as no new runtime behavior.
- Preserved SignFlow as external consumer profile only.

## Intentional Hardening Nuance

TIP-13 intentionally hardens direct application-service session create/read authorization to require BusinessConsumer caller category in addition to the existing `business.session.create`/`business.session.read` scopes.

Configured LocalDev public behavior remains unchanged because current LocalDev runtime keys grant business session scopes only to BusinessConsumer keys.

A synthetic or future non-BusinessConsumer caller that possesses business session scopes is now denied with `CALLER_CATEGORY_NOT_ALLOWED`, matching accepted TIP-12/TIP-13 actor-boundary requirements.

## Evidence And Validation

```powershell
dotnet test TagEkyc.sln --no-restore
```

Result:

```text
81 passed, 0 failed, 0 skipped
```

Test coverage added:

- BusinessConsumer allowed current owned business flow.
- BusinessConsumer cannot append capture artifact or trusted evidence.
- CaptureAgent remains capture write-only.
- TrustedAdapter remains trusted-evidence write-only.
- Cross-client access remains denied.
- OperatorAdmin/reserved caller gains no runtime behavior.
- Wrong-category callers with business session scopes are denied.
- Application assembly has no `TagEkyc.SignFlow` reference.

## Non-Goals Preserved

TIP-13 did not introduce:

- public API route changes;
- request/response DTO changes;
- JSON shape changes;
- status/error behavior changes;
- completion response/package summary/notification projection behavior changes;
- production auth provider;
- credential store;
- DB/EF/migrations/durable persistence;
- webhook/outbox/retry;
- crypto/signing/replay;
- provider/vendor selection;
- pilot/production readiness claim;
- SignFlow runtime/source/database/network/package/internal-model dependency.

## Remaining Deferred Items

These are deferred production-readiness items, not TIP-13 failures:

- production authentication / credential lifecycle;
- durable persistence / migration / recovery;
- vault/raw artifact lifecycle / retention / legal hold;
- webhook/outbox/retry/delivery ledger;
- webhook signing/replay protection;
- production crypto/signing/key management;
- provider/vendor trust;
- Operator/Admin/System production support workflow;
- production completion authority;
- pilot/production readiness certification.

## Recommended Next Step

Do not open a random TIP-14 directly from momentum.

Run a short post-TIP-13 reclassification / S2 debt convergence check to choose the next production-readiness slice.

Candidate directions:

- Durable persistence foundation
- Production auth / credential lifecycle
- S2 debt registry consolidation
- Webhook/outbox foundation
