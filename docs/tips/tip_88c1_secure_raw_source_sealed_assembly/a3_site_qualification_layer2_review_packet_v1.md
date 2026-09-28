# A3 site qualification Layer 2 — implementation review packet v1

Status: uncommitted review candidate  
Baseline: TagEkyc `54d8644`, activation seal revision 21  
Hash basis: working-tree bytes, as enumerated by `a3_site_qualification_layer2_review_manifest_v1.tsv`. This is not an immutable git-object manifest; commit and evidence-only re-freeze are deliberately deferred until review.

## Outcome

The production `ProbeSite` path no longer relies on six safe constants. It can launch a provisioned synthetic Capture Agent, run the existing CRT1 raw-ingress sender twice (`FullBodyHeldCommit`, then `LostFinalNoRetry`), combine those reports with the existing header-only early-continue probe, and mint a 15-field `PASS` candidate only when all seven measurements are complete and safe.

No real hospital hostname, certificate, IP address, site id, subject, session, artifact identifier, or biometric byte appears in this packet or the telemetry report.

## Reused production path

```text
synthetic enrolled Capture Agent
  -> existing CaptureRuntimeHttpClient / StrictRawIngressTransport
  -> existing CRT1 /api/ekyc/raw-export/source-ingress
  -> existing authenticator
  -> existing broker transaction B/R1
  -> existing body pipeline
```

The control channel is a dedicated API-key scope, `capture.raw-export.site-qualification`. It registers and reads only opaque run ids, phase timestamps, counters, and booleans. The raw request never carries the API key. Correlation reuses the already-signed tuple `(CredentialId, CredentialGeneration, IngressIdempotencyKey)`.

## Three non-contradictory scenarios

1. Header-only probe writes no body and fails on any intermediary-generated `100 Continue`.
2. Full-body run holds the real broker transaction before commit and proves zero Agent sends/Server reads until commit acknowledgement.
3. Lost-final run commits the ordinary admission result, aborts only that exact qualification response, and proves one client transport entry equals one Server raw POST with no hidden repost.

## Bypass boundary

The site gate can be crossed only by an exact active, short-lived run registered through the dedicated admin scope and bound to current site/origin/deployment plus signed credential/generation/idempotency. Consume occurs atomically after ordinary CRT1 authentication. Missing, wrong binding, expired, consumed, and API-key-on-raw requests remain `503` before admission/body.

The PostgreSQL table is not directly accessible to runtime or broker roles. Twelve SECURITY DEFINER functions have owner/search-path/ACL checks. The binding index is unique, so concurrent registrations cannot create two live authorities for one signed operation.

## Candidate/install safety correction

`ProbeSite` now has a separate `CandidateRecordPath`. It rejects any candidate path equal to the installed qualification target. Probe output therefore cannot replace an active record before explicit `-InstallOnPass`; incomplete and failed candidates remain diagnostic files only.

## Evidence

```text
Server focused restored                 14/14 PASS, 0 skip
Agent coordinator/measurement            3/3 PASS, 0 skip
Migration discovery                      1/1 PASS
Tool incomplete containment              PASS
Tool measured positive control           PASS
Tool seven single-field negatives        7/7 PASS

combined Server mutant                   3/10 RED exactly:
  Expired_run_remains_site_blocked
  Already_consumed_run_cannot_be_reused
  Lost_final_mode_aborts_after_one_admission_without_reposting

candidate/install alias mutant           RED at named guard invocation assertion
restored Server                           14/14 PASS
restored tools                            PASS
```

The real PostgreSQL test covers exact credential/generation/idempotency binding, one-time consume, causal held/release/commit/ack phases, safe and hostile observations, raw-post/client-entry comparison, migration Down/reapply, model parity, and privilege metadata.

## Locked boundaries

- Qualification record remains exactly 15 fields, format version 1.
- Existing seven consumer guards, runtime site policy, A3, P29–P36, and raw URL are unchanged.
- Normal submissions have no coordinator and retain their existing behavior.
- No SDK source changed.
- No seal, partition, census, topology, or ratification semantics changed.
- No commit, push, deployment, or site qualification was performed.
- `docs/00_GDRIVE_FILE_INDEX.md` was pre-existing dirty and was not touched by this work.

## Expected post-review transaction

If both reviewers accept the exact bytes: commit Server and Agent changes, then perform one evidence-only successor/re-freeze because the current whole-tree manifest is intentionally drifted. The successor must keep count `0`, topology `DurableWorker`, site policy `YES/v1`, and all ratified semantics unchanged.
