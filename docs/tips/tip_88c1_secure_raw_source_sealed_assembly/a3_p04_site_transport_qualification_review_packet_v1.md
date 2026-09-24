# A3 P04 corrected contract + site transport qualification — review packet v1

Date: 2026-09-23

Status: **TECHNICAL CLOSURE CANDIDATE — INDEPENDENT REVIEW REQUIRED — NOT RATIFIED**

This packet executes dispatch steps 1–4 only. It does not perform independent review, Homeowner ratification, a governance transaction, a row move, a census change, a seal re-mint, staging, commit, push or activation.

```text
activation-open census    5
P04                       NOT_IMPLEMENTED / CANDIDATE_PENDING_RATIFICATION
four Expect rows          NOT_IMPLEMENTED
A3                        HOLD
current seal              format 2 / revision 5 / count 5 (not re-minted)
```

## 1. Corrected P04 contract

The exact successor is `a3_p04_corrected_contract_v1.md`, SHA-256 `C55E22B245E98D4490D0E89C2E490ADA91B97D5C262529C51C2721B451A50E0F`. It contains exactly three groups:

1. Authenticated `Content-Encoding` or `Trailer` is P04: HTTP 400, sole public `RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID`, zero B/R1 and zero application body read.
2. `Transfer-Encoding`, invalid/missing `Content-Length` and bad framing are not P04: they are generic pre-authentication or Kestrel protocol rejection. The ordering is intentional because successful authentication commits its nonce transaction; unframable input must not spend that nonce.
3. Early body before `100 Continue` is not P04 because application/Kestrel has no trustworthy arrival-order observer. It is governed by executable `SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION`.

The canonical reconciliation now says in exact terms that P04 has technical closure under the **CORRECTED contract only, never under the original existing-R1/early-body clause**. It remains `NOT IMPLEMENTED` pending review and ratification. Reconciliation SHA-256: `DDBBC869EEA305C3508BFA1AEF16D37C32DA69AFF9D172EC7D62CEEE5DE93170`.

The ownership registry records one P04 candidate, not a ratification. It cites all three named cases, byte-exact restoration and the restored gate. Registry SHA-256: `886965DF45FF2EF5622A0E39B25B49322A36029B6FF83AD17970B849DE1F8D15`.

## 2. P04 arm proofs and group-2 anchor

Production endpoint source was not changed to obtain closure. Its restored/current SHA-256 is:

```text
RawExportSourceIngressEndpoints.cs
D172F1D094CB9064A80489DB12D84343C30B604BA737F3178C6FB0A0B4C56325
```

The named cases are in `Tip88C1C6BA1RawIngressBoundaryTests.cs`, SHA-256 `CED8062E0AAF57EE0EC2F20D507E511ACB7EAE84180CD2BBE9F78BE84864E276`:

- `P04_AuthenticatedContentEncodingReturnsExactO04WithoutR1OrBodyRead`
- `P04_AuthenticatedTrailerReturnsExactO04WithoutR1OrBodyRead`
- `P04_TransferEncodingIsGenericPreAuthenticationRejectionWithoutCommittedNonce`

The first two assert authenticated exact 400/O04, no admission/B/R1 producer call and zero body reads/bytes. The third asserts generic 400/`REQUEST_INVALID`, zero admission/body and an independently queried PostgreSQL committed-nonce count of zero.

| Run | Result | Purpose | SHA-256 |
|---|---:|---|---|
| `a3-p04-contract-a1.trx` | 3/3 PASS | baseline | `DB8A0A2A506D71E445864D42F2CF945629A221AA7449199D6AF419710BE6AF9A` |
| `a3-p04-contract-a2.trx` | 1/2, one RED | remove only Content-Encoding arm; only that case fails | `74365371C60FE18FA0DA6EE87DFFB71976298DFB5DE034BEDD955B3652D6F9A5` |
| `a3-p04-contract-a3.trx` | 1/2, one RED | remove only Trailer arm; only that case fails | `8534B3D86691311D370221CA42EE63A4C0F449B7AEDD805BC088039318420485` |
| `a3-p04-contract-a4.trx` | 0/1 RED | move Transfer-Encoding rejection after auth; nonce count becomes 1 | `FC828204758FEB5F6C6EEA04B600E3CEB60D254C778DF38C3D337655EBC68A9E` |
| `a3-p04-contract-a5.trx` | 3/3 PASS | byte-restored P04 gate | `E4F7C1CE7C6520356404B9946B840A0E22434A07C73710961B7DD3B405129030` |

Each RED credits one named assertion only. Deleting the whole two-arm guard was not used as evidence.

## 3. Executable site qualification and F3 negative control

The runnable entry point is `tools/Invoke-A3SiteRawIngressTransportQualification.ps1`, SHA-256 `CFD414AEC120607859EAC2CF638AC79817410421A9DA2DA7665AD8E4702280D3`. It drives the production Agent strict raw transport through isolated TLS/Kestrel/PostgreSQL, emits a machine-readable record for the transparent topology and then runs the F3 auto-`100` intermediary as a mandatory negative control.

The joined observer uses a separate PostgreSQL connection while the B/R1 transaction is held. It observes:

```text
while B/R1 held:
  Agent body sends                 0
  Server application body reads   0
  raw POST count                   1

after commit:
  Kestrel 100 relayed              true
  body released                    true
  hidden retry                     false
```

Development qualification record:

```text
path       TestResults/a3-site-qualification/site-transport-qualification.json
sha256     BA6E1615EA0A79E9117718DBC2FDB83457DFDB3B46090AC7430BA2648D6CA41B
site       development-loopback
revision   dev-2026-09-23
status     PASS
```

This record qualifies only the disposable development topology and ephemeral origin. It is not hospital-site evidence and must not be pinned for a hospital deployment.

| Run | Result | Meaning | SHA-256 |
|---|---:|---|---|
| `a3-site-qualification-a1-20260923010200-1.trx` | 1/1 PASS | transparent topology qualifies | `BC35411C0FA0E0DAC5D5E8AAAC1BBF9A0E8A3CBF2158E89FCBA4A672210EB1CC` |
| `a3-site-qualification-a2-20260923010315-1.trx` | 0/1 RED | F3 intermediary releases body before durable B/R1 | `C43C4B5DE25F6DEE2E200C3D1423F4287153600E61F298666747EB3FB9AE4AF3` |

The isolated PostgreSQL/container network was removed after the run. No Windows trust-store certificate was installed. Existing unrelated SignFlow containers were not touched.

## 4. Bounded startup gate

`CaptureRuntimeStartup` can now bind an exact qualification-record SHA through approved/build seal fields. For a future zero-open seal, missing, stale, `FAIL`, malformed, wrong-origin/revision semantics, early/intermediary continue, prebuffer, hidden retry or a SHA mismatch fail closed with the distinct outcome:

```text
CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_INVALID
```

The file provider accepts an exact 15-field JSON record from the configured absolute path and rejects missing, duplicate/unknown or malformed input. Product hashes:

```text
CaptureRuntimeStartup.cs                         D21EF75822150EAD50D399C9B16FDEB117B3B7FEFCCE81BF8BB4C57E2B79F31E
SiteRawIngressTransportQualificationFileProvider.cs 774F0EBF5ED6BB36D1A09A18852E71DAFF007438DE41F14FD541CD6107B1B523
Program.cs                                       553CB8097BE677A59422AA2547DA82341BC68A9F755E07CBC047F6D980FC7A46
```

| Run | Result | Purpose | SHA-256 |
|---|---:|---|---|
| `a3-p04-startup-a1.trx` | 23/23 PASS | pre-mutation startup baseline | `B839C66A923B2B966E49712538E8D64B67CC5FED8ED2156829525A6FA59BC352` |
| `a3-p04-startup-a2.trx` | 0/3 RED | remove only site gate; missing/stale/FAIL stop throwing | `BD0C3A23A5B4A341782709E3881AB2DD9B2761E7AAEA2D948368DBD94C3F5E96` |
| `a3-p04-startup-a3.trx` | 11/12 | excluded diagnostic: generated seal is correctly INVALID after current-byte drift; stronger INCOMPLETE assertion retained | `BF24130532465AAA254A6100CEA8F6FF1F4E370BF0C702E888A4B1B5D93988B2` |
| `a3-p04-startup-a5.trx` | 24/24 PASS | restored unit gate plus exact file-provider parsing | `4FED0D7ADC3CABAA454B03E27B6DDF2CB9C3EAB986F3DEFF444A12258B76E908` |

The current count-five seal is deliberately not re-minted. At count five, Activated still stops at `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE` before the site-record check. A future zero-open governance transaction must bind the reviewed site-record SHA into both approved/build seal values; zero without that binding is invalid.

## 5. Restoration, sentinels and build

All four mutated production sources were restored before the final gates. The two affected-source sentinel sets were recomputed separately:

| Set | Run | Result | SHA-256 |
|---|---|---:|---|
| CaptureRuntimeHttpClient/raw owner dependencies | Agent `a3-p04-sentinel-a1.trx` | 88/88 PASS | `CAF54755BC6EFEBE4CC5C649F85AD2757589EE5DF542BF7684C425B67D166B35` |
| Server response-framing dependencies | Server `a3-p04-sentinel-a4.trx` | 107/107 PASS | `CBBF5B1763ABA8367AD216A3416AD9931AB969A9F3F6703DC447728DA610023B` |

`a3-p04-sentinel-a2.trx` is retained as `EXCLUDED_DIAGNOSTIC_PARTIAL_SENTINEL`: it was green 24/24 but the expected set was 107, so it is not a pass claim. A subsequent attempt produced no TRX because restore had not included the A3 acceptance project reference; after restoring with `EnableCaptureAgentA3Acceptance=true`, the exact 107-case run above passed. The final API build succeeded with zero errors. Its expected fail-closed warning is `ACTIVATION_SCOPE_MANIFEST_CURRENT_BYTES_DRIFT`, because this checkpoint changes current bytes while seal re-mint is forbidden.

## 6. Whole-repository evidence accounting

The inventory scans both repositories recursively, including root-level `TestResults` outside `tests`, while excluding build output directories.

```text
whole-repo TRX          1248
  Server                1078
  Agent                  170
failed TRX               490
unclassified               0
```

Artifacts:

```text
a3_p04_site_qualification_whole_repo_trx_inventory_v1.tsv
SHA-256 67C2859B4D0A83B6AC4D505449BF51EFFFEC6FF560565E13806E2DD7F1E432B7

a3_p04_site_qualification_failed_run_census_v1.tsv
SHA-256 31DF355630E456A06910F1527D5EFCD1051697B1B78B83BBE78090E82934F676
```

Failed-run classifications over the whole retained tree:

```text
EVIDENCE_RED                         49
EVIDENCE_RED_TEST_TOPOLOGY           1
EXCLUDED_DIAGNOSTIC                 55
EXCLUDED_HISTORICAL_OUTSIDE_SLICE  353
REAL_PRODUCT_BLOCKER                 1
SUPERSEDED_RED                      31
total                               490
```

The current bounded slice contributes six failed TRX: three P04 mutations, one startup mutation, one generated-seal drift diagnostic and the F3 topology negative control. Historical runs not used by this packet remain inventoried; an older failed run without an existing canonical census entry is explicitly excluded as outside this slice rather than silently treated as evidence.

Both repositories remain `staged=0 / conflicted=0`.

## 7. Governance sequencing and disposition

The active partition SHA-256 remains `54D04EC2AE304BE3097712FDEE94C7D891B78F220CD651CF3CFAC686E04EEEAE`, with exactly five rows. The current descriptor SHA-256 remains `97F7AA54CF2AFA7AC39B0597F128248CE3660656CC16E66A7F568752A3E32BD2`; it was not re-minted.

Required future order, not performed here:

```text
independent technical review
→ explicit Homeowner ratification of P04 under the CORRECTED contract
→ site-specific PASS qualification for the actual deployment revision
→ zero-row governance transaction binding the exact reviewed qualification SHA
→ generator/descriptor update and successor seal re-mint
→ strict verification
```

The product-open census may reach zero only in or after that same governance transaction. It must never reach zero before the bounded startup gate and exact site record binding are present.

No NPS01 source, precondition, migration role grant or reconciliation responsibility was changed by this slice.

### Candidate disposition

```text
P04 technical closure under CORRECTED contract    PASS / REVIEW CANDIDATE
P04 under original existing-R1 clause             NOT CLAIMED
site qualification mechanism                      EXECUTABLE / F3-DISCRIMINATED
hospital-site qualification                       NOT CLAIMED
startup gate                                      PASS / MUTATION-DISCRIMINATED
census                                             5 (unchanged)
four Expect rows                                   OPEN (unchanged)
A3                                                 HOLD
```
