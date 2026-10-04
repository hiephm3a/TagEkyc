# SMID50 Reader Provider V0.4 — evidence correction V0.1

## Disposition

`READY_FOR_INDEPENDENT_REVIEW`

This evidence-only successor closes the single GPTW HOLD on V0.4. It does not change Agent or Server product bytes. V0.4 product review remains: GPTW product PASS and CC overall PASS.

## Correction

The per-file hash/read catch now calls:

```powershell
Add-EnumerationError "$Phase-file-hash" $file.FullName $_.Exception
```

Therefore the counterexample `UNREADABLE` before and after can no longer pass: either hash failure increments `EnumerationErrorCount`, and the final exit condition rejects every nonzero count.

The result also publishes and requires both:

- `UnreadableLogCountBefore == 0`;
- `UnreadableLogCountAfter == 0`.

Both counts independently prevent an unreadable file from being hidden by pre/post equality.

## Two non-blocking CC LOW items

- The audit script is now named `Invoke-Smid50V04PhysicalQualification.ps1`; the V0.3 name no longer labels V0.4 evidence.
- Empty harness output is published as `HarnessOutputEmpty` and makes qualification exit nonzero.

The existing harness/helper filenames retain V0.3 because their executable bytes did not change; their exact hashes remain bound in the physical result.

## Physical rerun

One current-path physical read was rerun on the same SMID50 lab machine and same 34-file candidate manifest.

```text
HarnessExitCode             0
HarnessOutputEmpty          false
AccessMethod                BAC
PACE                        false
ChipAuth                    CHIP_AUTH_RESPONSE_VALID
EnumerationErrorCount       0
UnreadableLogCountBefore    0
UnreadableLogCountAfter     0
UnreadableChangedLogCount   0
ChangedLogFileCount         0
Payload needle match        false
MRZ-like text               false
```

Candidate manifest SHA remains `750CEBDAC3CACA4D8C65E47295F8C27AAA61A58A9C0497E440D2F62E7E529DFB`; no product bytes changed.

## Boundaries

- Product test rerun: NO — no product change.
- Schema/migration/contract change: NO.
- Commit/re-freeze/push: NO.
- Raw card data retained: NO.
- Hospital/site evidence claim: NO.

The prior complete V0.4 packet is bound by outer SHA `058580EE3EBC184CDB99DFFF2B3F3F4D1E2EA36A6198D04BB9EF8AA5B724A0F8`; its report, manifest, census, and provenance are retained under `predecessor/`.
