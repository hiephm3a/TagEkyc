# TIP-88C1-C5 Managed Recipient Enrollment & Activation Readiness — As-Built

## Authority

- Baseline: `4d5dfe3e7c96bf0c136e2c1c1fc36c47fe71fb3f`
- Scope v0.3: `7EEB2D378AEE7A0F840D620846C6D6C71A6BB1AF3A71DC7FAD3528CFF1425DB5`
- Dispatch v0.6: `6F4B34631E8D7C1C4C827EBE50E5B9AB2F2624701834617037A9DC1A67381482`
- Round-3 bundle: `ED63B70BC428EEBE9A7E1C1D895BC2B2C46843D25D106D19CCE420FBFEE9F14C`

## Current controlled-build state

`IMPLEMENTATION_IN_PROGRESS` — this document is append-only during the controlled build.

Ratified census:

```text
47 permanent paths
5 C5 tables
6 C5 indexes
9 C5 functions
30 proof owners C501-C530
78 allocated mutations
```

Allocated mutations are `C5M01-C5M64`, `C5M66-C5M77`, and `C5M79-C5M80`.
`C5M65` and `C5M78` remain `CONTROL / NOT_ALLOCATED`.

## Carried debt and non-claims

- `C4-AUTH-SCOPE-PROVISIONING-DEBT-01 = ASSIGNED_OPEN` until controlled closeout proves the PostgreSQL managed provisioning path.
- `C5-C2-PRELOCK-CLOCK-NONCLAIM-01 = OPEN / PREDECESSOR_OWNED`.
- `C4-SYNTHETIC-SIBLING-CONTROL-NONCLAIM-01 = UNCLOSED / NOT_C5_OBJECTIVE`.
- `C4-ROLE-SIBLING-SURFACE-NONCLAIM-01 = UNCLOSED / NOT_C5_OBJECTIVE`.

Product Ready is not claimed. It still requires the production Raw BIO source adapter with real-source end-to-end evidence and a separate deployment/hospital-pilot activation packet.

## Execution evidence

Pre-audit and mutation/suite evidence are produced outside the repository and will be bound here only after final-byte closeout. No stage, commit, push, merge, PR, deployment, pilot, or production activation is authorized.
