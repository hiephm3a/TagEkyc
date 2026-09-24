# A3 macro-wave W3 — admission outcomes review packet v1

Status: **one grouped handoff for all seven W3 rows**. This is not A3 PASS, full-suite approval, landing or production authorization. Incoming official authority-open census is **47 normative + 1 seam = 48**.

## 1. Exact scope and disposition

W3 contains exactly P01, P06, P07, P08, P11, P14 and P16. `a3_macro_wave_w3_final_matrix_v1.tsv` gives a disposition for all seven. Requested ratification is exactly **P08 and P14 together**. P01/P07/P11/P16 stay OPEN and P06 stays PRODUCT_GAP; none is silently deferred or closed by sibling evidence.

## 2. Horizontal execution

The corrected current baseline `a3-w3-horizontal-baseline-v2.trx` is **26/26 PASS** in one PostgreSQL/Kestrel invocation. It covers signed auth, strict expiry boundary, real capacity, public O11/O14/O16 and adjacent controls. Agent `a3-w3-agent-expired-lease-baseline-v2.trx` is **5/5 PASS** and shares one custody test across capacity, idempotency, claim-token, historic-key and reservation outcomes.

P08 uses a held real capacity slot. The denied sibling crosses Kestrel/TLS and the production Agent client, returns exact 503/O08, reads neither HTTP body nor body pipeline, never calls the broker, changes no durable/provider rows, releases the slot exactly once and permits the next request to enter the broker. Agent retains the same buffer only within its lease; after 301 seconds it sends no second body, zeroizes and requires recapture.

P14 creates a canonical claim with commitment selector v1, while the request broker is configured for v2. Provider failure returns exact public 503/O14 with zero body read and only the allowed evaluation shell. After valid token expiry the provider recovers by delegating to the production commitment provider: the broker calls **v1 again**, never v2, reads no second body and advances past O14 to the independent O16 reservation gate. Agent expiry has the same no-second-send/zeroization proof.

## 3. Shared guard mutation

One mutant build changed two independent guards at once:

- W3-G-CAPACITY projected a null capacity lease as O05 instead of O08; mutant SHA `CC788652597A73F4B159A80528A7BE13BA99B2FFB437F7058E35BAE773960A91`.
- W3-G-O14 replaced the locked claim selector with current configured selector v2; mutant façade SHA `A20A5D7A8CE5990912E4A1EC958F7CA3E0D5BDE1AB8035E5EED592A375F2227D`.

`a3-w3-shared-guards-red.trx` is **24 PASS / 2 FAIL**. Only the named P08 and P14 cases fail, at exact expected-vs-actual outcome/selector assertions; twenty-four adjacent controls stay green. Product bytes were restored to admission `54F4CD863AA1F3738CB5DC171F9726E9FD7ED4312EDDC0C6FD4612083AEF078C` and façade `050D627D63B13621C442CDF0645B910A07225065F940E4EDDE8A93E737D120ED`, rebuilt once, then `a3-w3-final-restored-joined.trx` passed **26/26**.

## 4. Failed-run audit

All five failed W3 TRX are classified in `a3_macro_wave_w3_failed_run_census_v1.tsv`: **1 EVIDENCE_RED + 1 SUPERSEDED_RED + 3 EXCLUDED_DIAGNOSTIC**, zero unclassified. Two zero-test invocations are retained in the scratch ledger and TRX inventory as excluded diagnostics but are not failed-run census rows. No failed W3 artifact is omitted from the candidate manifest.

## 5. Limits and requested decision

P01 lacks the Agent denial join/current auth mutation. P06 needs product delivery of class maxima. P07 still lacks five independent `LEAST(...)` member proofs. P11 lacks the retry/internal-begin restart join. P16 lacks independent lease/owner/revision/fence mutations. These gaps remain explicit.

Requested independent review: ratify **P08 + P14 together** only if the exact public paths, residue, Agent custody, shared RED specificity, restoration and machine audit survive. Before review, official census remains **48**. If both are ratified together, it becomes **45 normative + 1 seam = 46**. A3 remains HOLD.

## 6. Candidate freeze

The candidate manifest, TRX inventory, failed-run census, drift allowlist and verifier are W3 artifacts with the same `v1` suffix. The verifier must report exact whole-repo W3 TRX coverage, zero delta outside the allowlist, zero live mutant hashes, partition 48/48, and staged/conflicted 0/0 in both repositories. The manifest deliberately excludes only itself and its sidecar to avoid self-reference; their final SHA is supplied with the handoff.
