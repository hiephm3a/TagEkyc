# Golden Neutral-Proof Vector Conformance

This contract freezes real TagEkyc ES256-JWS neutral-proof output for cross-implementation verification. Consumers verify the frozen vectors in `golden_neutral_proof_vectors.json`; they must not regenerate them.

## Fixture

- Schema: `tip-67g-golden-neutral-proof-v1`.
- Primary vector: `medium-capture-quality-v1`.
- High-assurance follow-up: add `high-nfc-face-liveness-v1` after the test harness can append the required NFC/FaceMatch/Liveness evidence decision-basis without broadening this contract slice.
- Fixture inputs are synthetic. The vector is a conformance artifact, not patient evidence.

## Payload Claims

The protected payload has exactly these 19 claims:

1. `proofVersion`
2. `purpose`
3. `sessionId`
4. `identityRef`
5. `packageId`
6. `packageVersion`
7. `canonicalizationScheme`
8. `hashAlgorithm`
9. `result`
10. `assuranceLevel`
11. `requiredChecks`
12. `completedChecks`
13. `evidenceEngines`
14. `signedAt`
15. `challenge`
16. `signedManifestHash`
17. `resultHash`
18. `resultHashAlgorithm`
19. `resultHashCanonicalizationScheme`

## Result Hash

`resultHash` is:

```text
"sha256:" + lowerhex(SHA256(UTF8("tip-67b-neutral-proof-result" + "\n" + JCS(preimage))))
```

The preimage has exactly these 16 fields, which are the 19 payload claims minus the three `resultHash*` fields:

```text
proofVersion
purpose
sessionId
identityRef
packageId
packageVersion
canonicalizationScheme
hashAlgorithm
result
assuranceLevel
requiredChecks
completedChecks
evidenceEngines
signedAt
challenge
signedManifestHash
```

## Identity Ref

`identityRef` is a formula, not a constant from the fixture:

```text
"sha256:" + lowerhex(SHA256(UTF8("tip-67b-identity-ref-v1" + "\n" + clientApplicationId:N + "\n" + subjectRef)))
```

`clientApplicationId:N` means the 32-character lowercase/no-dash GUID form. The fixture uses the local-dev business client GUID only to prove known inputs produce the frozen output. Production consumers use their own registered `clientApplicationId`.

## JWS And JWK

- Protected header: JCS JSON with exactly `alg` and `kid`, for example `{"alg":"ES256","kid":"..."}`. No `typ`.
- Signing input: ASCII bytes of `base64url(header) + "." + base64url(payload)`.
- Base64url is unpadded.
- ES256 signature format is raw P1363 `R || S`, 64 bytes, not DER.
- `signedAt` is UTC formatted as `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`; the value is truncated to microseconds before formatting, so the 7th fractional digit is normally `0`.
- Embedded proof JWK has exactly four public fields: `crv`, `kty`, `x`, `y`.
- Public-key fingerprint is computed over the canonical four-field JWK, not over a JWKS metadata object:

```text
"sha256:" + lowerhex(SHA256(UTF8(JCS({ "crv": "...", "kty": "EC", "x": "...", "y": "..." }))))
```

Consumers do not need to preserve the fixture file's whitespace or JSON property order, but they must canonicalize the same four public fields before fingerprinting.

## Semantic Binding

Cryptographic verification alone is not enough.

- Challenge binding: compare the proof `challenge` claim to the consumer's own expected challenge, such as the challenge or binding nonce passed when creating the session. Never read `challenge` from the proof and compare it to itself.
- Trust anchor: pin `(kid, publicKeyFingerprint)` out of band, such as from a trusted JWKS endpoint or a configured anchor. Reject the proof before accepting the embedded JWK if either value differs. Blind-trusting the embedded key allows a forged proof to self-verify.
