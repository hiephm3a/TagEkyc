# TIP-88C1-C6B-A1 — Bounded micro-correction: CRT1 proof owner

**Correction ID:** `C6B-A1-CRT1-PROOF-OWNER-PROJECT-BOUNDARY-01`  
**Status:** REVIEW CANDIDATE — DOCUMENTATION/CATALOGUE ONLY  
**Baseline parent:** `871EEE36BFA59F8ED683941E41DAA5F1943851E746A83D7D0CA552FAAC2FA7D1`  
**Baseline operation master:** `0519D056B76E8FC44AF9A840E25DF904591D603DDAEA574397797B8D633B6DBB`  
**Bound CRT1 micro-RRI:** `EDA8703C3969F6A7043E496553FEC3E2B3AD72462C319EE10BE3491490489903`

## Ratified correction

The sole authoritative owner of `Crt1_GoldenVectorsMatchClientAndServer` is:

```text
tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs
```

Every operative mapping of that proof to
`tests/TagEkyc.ContractTests/Tip88C1C6BA1ProtocolTests.cs` is removed. The
missing ContractTests file must not be created to satisfy stale ownership, and
the already-running ArchTests proof must not be copied or moved.

`TagEkyc.ContractTests` remains pure within its existing Contracts + SignFlow
references. Neither ContractTests nor ArchTests `.csproj` changes. No project
reference, API exposure, substitute parser or second CRT1 builder is authorized.

## Proof semantics retained

The ArchTests proof must exercise the actual API parser and prove client/parser
byte equality, sole parser ownership, authenticator verify-only behavior,
noncanonical timestamp/nonce rejection, RawIngress zero body reads, and failure
for mutation of method, path, timestamp, nonce, media type, content length, body
commitment and operation binding.

It must also prove the final-LF invariant completely: exactly one final LF is
accepted; missing final LF, extra final LF and CRLF substitution are rejected.
The same complete proof has exactly one authoritative owner.

## Fences

This correction changes only catalogue/proof-owner documentation. It does not
change CRT1 grammar, `ExactSignedPreimage`, authenticator ownership, R01/R02,
pepper, SQL, schema, migration, A2 or A3. During correction review there is no
product mutation, stage, commit or push.

PASS restores A1 implementation authority on the successor exact-byte
catalogue but grants no stage, commit, push, A2 or A3 authority.
