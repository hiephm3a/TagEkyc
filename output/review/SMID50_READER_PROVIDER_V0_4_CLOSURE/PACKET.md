# SMID50 Reader Provider V0.4 — commit closure and rev29 successor packet

## Decision

`EVIDENCE_ONLY_REFREEZE_CANDIDATE`

No Agent or Server product byte changes in this successor. The predecessor product commits and their reviewed physical/evidence manifest are bound without regeneration.

## Immutable pre-commit authority

- Candidate manifest SHA-256: `750CEBDAC3CACA4D8C65E47295F8C27AAA61A58A9C0497E440D2F62E7E529DFB`.
- Hash basis: `WORKING_TREE_BYTES_PRE_COMMIT`.
- Candidate paths: Agent 29 + Server 5 = 34.
- Physical run binds that exact manifest and reports BAC, PACE false, AA valid, zero enumeration/unreadable/log leakage findings.

The original manifest is retained at `v04/candidate-file-manifest.tsv`; it is not rewritten to reflect post-commit Git status.

## Post-commit binding

- Agent commit `5eb4d900d8c6f9e4321c499b9c187fdc0fb33d30`, parent `cbabf18584cfec82adce296398078d1901e77e97`.
- Server commit `a5b5e956ce59ec6f760532a1aa414da633071c52`, parent `6759b624c7e30d2e1fd9db47de26cb89f647513c`.
- Exact commit path set: 34/34.
- Checkout bytes equal reviewed manifest bytes: 34/34.
- Normalized Git blob OID/SHA is recorded separately for every path.
- CRLF/LF basis differences are explicit for 21 paths and are not treated as product drift.
- Server commit is anchored by local branch `codex/smid50-bac-evidence` before push review.

## Review closure

- GPTW V0.4 product/evidence closure: PASS after the bounded evidence correction.
- CC V0.4 product/evidence closure: PASS.
- GPTW commit-object closure: PASS, findings 0.
- CC commit-object closure: PASS, one non-blocking custody LOW resolved by the local branch.

## Scope retained

- SMID50 access method: BAC, never represented as PACE.
- Full profile with BAC: maximum assurance Medium.
- Active Authentication: session-bound DG15 response verification.
- SOD-declared DG exception: exact DG3/DG4 only.
- No raw subject/card bytes in retained evidence.

## Forbidden claims

- No site or hospital qualification.
- No PACE support claim.
- No push or deployment authorization.
- No change to schema, Raw BIO, A3, Layer 2, KEK, Claim Providers, SignFlow, partition, ledger, ownership, topology, or site-policy semantics.

This packet is intended only for evidence-only seal revision 29 review.
