# Postgres Restore Drill Record Template

## Drill Metadata

- Date:
- Operator:
- Release SHA:
- Backup id:
- Restored database id:
- Restore environment:

## Migration And Readiness

- Migration state:
- `append_idempotency_records` present: yes/no
- Startup readiness result:
- Sanitized failure code, if any:

## Synthetic Idempotency Replay

- Synthetic session id:
- Append endpoint:
- Idempotency-Key hash or opaque test id:
- Caller category:
- Required scope:
- API-key alias:
- Expected minted id:
- Replay result:
- Deduplicated: yes/no
- Replayed minted id:

## Proof Verification

- Evidence package id:
- Proof kid:
- Proof public key fingerprint:
- Key continuity source: current-P12 / restored-previous-JWKS
- JWKS entry matched: yes/no
- JWS verification result:

## Security Notes

Record only opaque ids, hashes, fingerprints, aliases, and pass/fail results. Do not record subjectRef, patient data, biometric data, connection strings, passwords, file paths, P12 private material, API key values, or secret values.

## Outcome

- Pass/fail:
- Follow-up actions:
