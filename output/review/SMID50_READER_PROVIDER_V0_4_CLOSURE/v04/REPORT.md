# SMID50 Reader Provider V0.4 — bounded correction successor

## Disposition

`READY_FOR_INDEPENDENT_REVIEW`

This successor addresses the one HIGH product finding and one MEDIUM evidence finding from the V0.3 GPTW review. The independent CC review of V0.3 was PASS; all unrelated V0.3 product and evidence bytes remain unchanged. No commit, re-freeze, or push was performed.

## Baseline and write set

- Agent baseline: `cbabf18584cfec82adce296398078d1901e77e97`, branch `codex/smid50-reader-provider`.
- Server baseline: `6759b624c7e30d2e1fd9db47de26cb89f647513c`, detached rev28 worktree.
- Current candidate manifest: 34 files — 29 Agent, 5 Server.
- Candidate manifest SHA-256: `750CEBDAC3CACA4D8C65E47295F8C27AAA61A58A9C0497E440D2F62E7E529DFB`.
- V0.4 changes within the existing write set: one Server service, two Server test files, and the qualification audit script/evidence.

## HIGH closure — caller reason codes cannot mint durable NFC facts

The Server now reserves these caller-inaccessible namespaces:

- `PACE_SUCCESS` and `BAC_SUCCESS`;
- `SOD_DECLARED_DG_*`;
- `SOD_SUPPLIED_DG_*`.

Caller-provided values in those namespaces are stripped before persistence. Durable access and DG census tokens are then re-derived only from the validated `NfcEvidenceDecisionBasis.Flags`. Non-reserved caller reason codes preserve their existing compatibility behavior.

Focused negative proof covers:

1. BAC basis plus caller `PACE_SUCCESS`: persistence contains BAC, not PACE; completion remains `Medium`.
2. PACE basis plus caller `BAC_SUCCESS`: persistence contains PACE, not BAC.
3. Caller-only declared/supplied DG16: neither token persists when absent from validated flags.
4. Malformed `SOD_DECLARED_DG_01`: does not persist.

The target mutation removes the reserved-namespace filter. It produces 0/3 PASS: both spoof-persistence cases fail and BAC is upgraded to `High`. The inverse restore is byte-exact (`1FCF8392...1ABB757`) and returns 3/3 PASS.

## MEDIUM closure — physical log enumeration is fail closed

Every recursive `log`/`logd` directory and file traversal now uses `-ErrorAction Stop`. Traversal failures are retained as sanitized phase/path/exception-type entries; qualification exits nonzero whenever `EnumerationErrorCount != 0`. Sensitive-reference artifact enumeration is covered by the same rule.

The physical qualification was rerun on the SMID50 machine against the full current candidate manifest. Result:

- product path exit: 0;
- access method: BAC; PACE: false;
- Active Authentication: `CHIP_AUTH_RESPONSE_VALID`;
- declared DGs: 1,2,3,13,14,15;
- supplied DGs: 1,2,13,14,15;
- enumeration errors: 0;
- log files before/after: 8/8;
- changed logs: 0;
- unreadable changed logs: 0;
- raw-artifact needle matches: 0;
- MRZ-like text observations: 0.

Physical artifact bindings:

- candidate manifest: `750CEBDAC...E529DFB`;
- helper: `2040C56A...CEF2D55`;
- harness: `FD301F04...5328E17`;
- SDK manifest: `C65779E4...DB2A53`;
- audit script: `740CB226...F43E27`;
- sanitized result: `57C9CD96...7200A7`.

No raw DG, MRZ, portrait, signature, access code, or vendor-log content is included in the packet.

## Current proof

- Agent focused gate: 62/62 PASS, zero skip.
- Server focused gate: 50/50 PASS, zero skip.
- Reserved reason-code mutation: 0/3 RED at the intended assertions.
- Inverse restore: 3/3 PASS and byte-exact.
- Server Application Release build: PASS, 0 warnings, 0 errors.
- Physical successor: PASS with `EnumerationErrorCount=0`.
- Census including retained predecessor history: 18 runs, 239 executions, 229 passed, 10 classified failures, 0 skipped, 0 unclassified.

The predecessor V0.3 report, test evidence, physical result, census, ledger, and manifest are retained under `predecessor/V0_3/` rather than silently replaced.

## Boundaries

- No schema/migration, Raw BIO, A3, Layer 2, KEK, Claim Provider, SignFlow, or seal change.
- No PACE implementation or claim.
- No hospital/site qualification claim.
- No full suite; only the SMID50/Tip74 and Tip05/Tip08 affected gates were run.
- Commit: NO. Re-freeze: NO. Push: NO.

## Requested decision

Verify the reserved-namespace strip-and-rederive mechanism, the three-case mutation, byte-exact restore, fail-closed physical enumeration, current physical binding, manifest, census, and ZIP. Return PASS or actionable findings.
