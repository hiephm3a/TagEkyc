# TIP-88C1-B2-DURABLE-KEY-CSPRNG-PREREQ — Deployment-Policy Record (finding C)

**Status: TOPOLOGY RATIFIED (Option 1) — ONE OPEN PRODUCTION-ACTIVATION ITEM (extension-owner literal + install
authorization).** The Homeowner selected **Option 1 (controlled extension schema)**; it is NO LONGER an open topology choice
and application-side CSPRNG (Option 2) is rejected for this slice. What remains is a PRODUCTION-ACTIVATION gate only — it does
NOT block DK-PROD docs reconciliation, proof-build, or implementation against the approved dev/test topology. Not a Contractor docs edit.
Decomposed from `tip_88c1_b2_durable_key_custody_build_dispatch.md` v0.6. The production owner/provisioning item blocks only
production activation; provider-operation-token generation may be implemented and proof-built against the approved dev/test
topology in `tip_88c1_b2_durable_key_prod_build_dispatch.md`. DO NOT INSTALL/MIGRATE/COMMIT/PUSH/DEPLOY from this docs record.

## The problem (confirmed on-code, v0.6 line 394)
DK-PROD `prepare` must mint a 256-bit provider-operation token server-side. v0.6 wrote an **unqualified**
`gen_random_bytes(32)` inside a SECURITY DEFINER function pinned to `SET search_path = pg_catalog`. Under that search_path an
unqualified `gen_random_bytes` (which lives in whatever schema `pgcrypto` is installed in — not `pg_catalog`) does NOT
resolve, and an unqualified call is also shadowable. This cannot be closed by a docs edit alone: it needs a deployment-policy
decision about WHERE `pgcrypto` lives and WHO may install it.

## Ratified topology (Option 1 — SELECTED; Option 2 rejected)
**Controlled extension schema `tagekyc_extensions`, extension `pgcrypto`, function identity
`tagekyc_extensions.gen_random_bytes(integer) RETURNS bytea`.** The token is generated inside the DK-PROD `prepare` SECURITY
DEFINER function via `tagekyc_extensions.gen_random_bytes(32)`; no unqualified call; non-shadowable under
`search_path=pg_catalog`; no caller-supplied token. The migration performs a NON-MUTATING prerequisite assertion only — it
does not install, relocate, alter ownership of, or drop the deployment-owned extension; Down preserves the extension + schema.
DK-PROD §6c pins the exact call, readiness catalog checks and effective-ACL proof.
~~Option 2 (application-side C# CSPRNG)~~ — **rejected for this slice.**

## Fixed token rendering (independent of the choice)
256 random bits → standard base64 → `+`→`-`, `/`→`_`, trailing `=` removed → exactly 43 ASCII base64url chars, no padding.
No caller-provided token is ever accepted.

## Readiness + tests (to be pinned in DK-PROD once decided)
`PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE` fails when the extension/function/schema/signature/owner/ACL is wrong or (option 2)
the trusted call site is misconfigured. Named tests: missing extension/function, wrong schema, wrong owner/ACL, shadow
function, output length ≠ 32, token length ≠ 43, invalid base64url char, caller-token path introduced.

## Remaining open item (production-activation only)
The exact production role that OWNS `pgcrypto`/`tagekyc_extensions`, and the authorization to install it, are deployment-
specific and remain a PRODUCTION-ACTIVATION gate. This does NOT block DK-PROD docs reconciliation, proof-build, or
implementation/testing against the approved dev/test topology (DK-PROD §6c, item L).
