# A3 macro-wave W3 — P08/P14 successor review packet v2

Status: **TECHNICAL SUCCESSOR COMPLETE / GROUPED RATIFICATION REQUESTED / A3 HOLD**.

This successor resolves the sole technical hold from the independent W3 review. It changes one test, no product source, and runs only the affected P08/P14 surface. It does not ratify either row, change the canonical census, re-mint the activation seal, start Wave 4, or run a full suite.

## P08 successor

`CapacityDenialTraversesKestrelAgentReleasesSlotAndCreatesNoCustody` now keeps the exact `deniedRequest.Metadata` and exact `deniedRequest.Lease` that received public 503/O08. After releasing the held first slot, it submits that same pair again through the same production `CaptureRuntimeHttpClient` and Kestrel/TLS host. No replacement request fixture, metadata, lease or body ownership is created.

The first call still proves exact 503/O08, zero broker call for the denied request, zero HTTP/body-pipeline reads, and unchanged durable/provider rows. The retry reaches the broker, returns its independent O02 control, keeps all residue unchanged and still reads no body.

```text
a3-w3-p08-same-buffer-successor.trx
1/1 PASS
SHA-256 B47D9596134179DA5A5E7132CC830FCFAABBFF424262B7855B21D8FD97E7366D
```

Test source SHA-256: `2DCC86DE1D49BE766EC7745B211E211BF95BFC933C7CFC171D75071440C5E26F`.

## Current-source guard discrimination

The registered narrow W3-G-CAPACITY mutant was repinned on the successor test source:

```text
CapacityUnavailable -> CapabilityUnavailable
mutant SHA-256 CC788652597A73F4B159A80528A7BE13BA99B2FFB437F7058E35BAE773960A91

a3-w3-p08-same-buffer-capacity-mutant-red.trx
0/1 RED
Expected RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE
Actual   RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE
SHA-256 D20B9A90DD9F4C8BA2157B519F5D81141F9CA55B0CD7B5CF6A5528DACB8F925A
```

The product source was restored byte-exact to `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C` and rebuilt.

## Restored affected gate

One filter joined the successor P08 test with the shared broker-final family containing P14:

```text
a3-w3-p08-p14-successor-restored-joined.trx
9/9 PASS, zero skip
SHA-256 7064CE12C6663555D7BC3AC5C4093FC8648BFF15B8BA08DC6EA001CB049F61A6
```

P14 product/test semantics did not change. Its predecessor proof remains: public O14, locked selector v1 despite configured v2, exact-old-key recovery uses v1 again and never v2, no second body, and its targeted W3-G-O14 RED. The two expectation corrections during W3 concerned only the independent post-recovery O16 gate and the placeholder provider digest; they did not alter the v1-versus-v2 assertion.

## Failed-run accounting

`a3_macro_wave_w3_failed_run_census_v2.tsv` classifies all six failed W3 TRX:

```text
EVIDENCE_RED        2
SUPERSEDED_RED      1
EXCLUDED_DIAGNOSTIC 3
unclassified        0
```

The failed O14 file formerly named `focused-green` is now result-neutral `a3-w3-o14-recovery-attempt-a4.trx`; bytes and SHA are unchanged.

## Requested grouped decision

Independent reviewers previously accepted P14 technically and held only P08's joined same-buffer requirement. That requirement is now executed directly and mutation-discriminated on current test bytes.

Requested decision: **ratify P08 and P14 together**. Until the Homeowner explicitly does so:

```text
authority-open census = 48
activation seal       = approved count 48
A3                     = HOLD
Wave 4                 = not started
```

After explicit grouped ratification, one governance transaction must update the reconciliation/partition from 48 to 46, generate the ratification record with exact machine-readable fields, rebuild the candidate manifest, and re-mint the activation seal against that exact record. The seal must not be changed before that authority event.
