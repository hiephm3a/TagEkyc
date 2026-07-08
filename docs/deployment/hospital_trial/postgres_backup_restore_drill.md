# Hospital Trial Postgres Backup/Restore Drill

## Purpose

Prove that a restored Postgres backup preserves TagEkyc evidence, append idempotency, and neutral-proof verification. This is operational evidence, not a unit test.

## Companion Backup Items

A database backup alone is not enough to verify old proofs. The restore environment must have one of:

- the same current ProductionTrialP12 signing key and password secret, or
- a restored `Jwks:PreviousKeysFile` entry containing the proof `kid` and public key fingerprint.

Record the proof `kid` and fingerprint before backup and after restore. Do not record private keys, P12 paths, passwords, or secret values.

## Synthetic Drill Fixture

Use a synthetic session only. Do not use a real customer session.

The drill test pack must include:

- `sessionId`: opaque synthetic session id.
- `appendEndpoint`: `/api/ekyc/verification-sessions/{id}/capture-artifacts` or `/api/ekyc/verification-sessions/{id}/evidence-results`.
- `idempotencyKey`: exact `Idempotency-Key` sent before backup.
- `requestJson`: exact sanitized append request JSON.
- `expectedMintedId`: capture artifact id or evidence result id minted before backup.
- `callerCategory`: `CaptureAgent` or `TrustedAdapter`.
- `requiredScope`: append scope required by the endpoint.
- `apiKeyAlias`: non-secret alias for the credential stored in the approved secret store.

The fixture must contain no subjectRef, patient identifiers, biometric media, raw payloads, connection strings, secrets, or PII.

## Drill Steps

1. Create the synthetic drill session and complete it so a signed proof exists.
2. Perform an append with the stored `Idempotency-Key` and request JSON; record the minted id.
3. Back up the Postgres database and companion key/JWKS item.
4. Restore into a clean database.
5. Start the server with production config and the restored/continuous signing trust anchor.
6. Confirm readiness passes: no pending migrations and `tagekyc.append_idempotency_records` exists.
7. Re-send the stored append request with the same `Idempotency-Key`; assert `Deduplicated=true` and the same minted id.
8. Fetch the verification view for the completed synthetic session.
9. Verify the proof JWS by `kid` against current JWKS or restored previous JWKS; assert the fingerprint matches the pre-backup value.

## Failure Handling

- Same-key/same-payload not deduplicated: fail the drill.
- Same-key/different-payload returns anything other than conflict: fail the drill.
- Proof `kid`/fingerprint cannot be verified from restored trust anchors: fail the drill.
- Any record contains secrets or PII: discard the record and rerun with sanitized evidence.
