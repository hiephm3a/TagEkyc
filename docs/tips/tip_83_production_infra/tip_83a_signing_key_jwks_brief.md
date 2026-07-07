# TIP-83A - Signing Key + JWKS + Startup Validation - Planning Brief

Status: v0.2 - OQ-A/OQ-B LOCKED + review patches (ProductionTrialP12 posture, public-only JWKS previous-key registry, active-signer==JWKS-current, P12PasswordSecretRef, sanitized P12 errors, restore=runbook-evidence). READY_FOR_BUILD. First + highest-priority TIP-83 slice: a STABLE production ES256 signer (P12 trial-grade, no ephemeral), a JWKS distribution (current + previous public keys) so callers pin the trust anchor across rotation, and a P12 startup self-test. Blocker #1 - without a stable pinnable key, SignFlow cannot verify proofs.
Repo: D:\Task\Remote Signing\TagEkyc (server). Baseline: b69bcb8. Umbrella: tip_83_planning_brief.md. OQ-1 LOCKED = P12 trial-grade + HSM roadmap.
Tier: Tier-1 (identity-proof signing key custody + trust-anchor distribution).

## 0. Recon facts to build on
- ES256 JWS signing is REAL. Two backends: LocalDevEs256JwsEvidenceSigner (P12Path OR ephemeral fallback) + Pkcs11Es256JwsEvidenceSigner (HSM).
- GOTCHA: LocalDevEs256JwsEvidenceSigner.cs:~39 generates an EPHEMERAL key when no P12Path -> unstable trust anchor. Default KeyId = "localdev-es256-v1".
- Program.cs:~155 startup: production || RequireHardwareSigner => backend MUST be Pkcs11 (would reject a P12 production signer today).
- EvidenceSignerStartupValidationHostedService only self-tests when signer is Pkcs11 (P12 has no self-test).
- Proof ALREADY carries the key selector: EvidencePackageVerificationViewDto has KeyId + PublicKeyJwk + PublicKeyFingerprint -> a verifier can select the key by kid. So JWKS old+new works ("proof carries keyId" = verify unchanged). NO JWKS endpoint exists today (public key only embedded per-proof).

## 1. Intent
Add a production-allowed, stable P12 ES256 signing posture (distinct from the "LocalDev" ephemeral path), enforce it fail-closed at startup with a P12 self-test, and publish a JWKS endpoint (current + previous public keys) so SignFlow/invoice callers pin the trust anchor and old proofs still verify after a key/HSM cutover.

## 2. Scope - ALLOWED
### 2.1 Production-allowed P12 signer posture - OQ-A LOCKED
- **LOCKED: a NEW explicit backend value `TagEkyc:EvidenceSigning:Backend = ProductionTrialP12`** (real config namespace, `Program.cs:115`). It MAY reuse the P12 implementation internally, but the config/DI/log/error surface MUST NOT read as `LocalDev` in production, and MUST NOT reach the ephemeral fallback.
- **All TIP-83A config lives under `TagEkyc:EvidenceSigning:*`** (env-mapped `TagEkyc__EvidenceSigning__...`): `P12Path`, `P12PasswordSecretRef`, `KeyId`, `Jwks:PreviousKeysFile`, `Jwks:PreviousKeyOverlapDays`.
- Production allowed signer modes: `ProductionTrialP12` (when `RequireHardwareSigner=false`) OR `Pkcs11` (when `RequireHardwareSigner=true`, ALWAYS forced).
- Production FORBIDDEN (=> fail-closed startup, distinct reason codes): `LocalDev` backend, ephemeral generated signer, placeholder, default KeyId `localdev-es256-v1`, missing/relative `P12Path`, missing P12 password.
- `ProductionTrialP12` requires: `P12Path` absolute + file exists; password via `P12PasswordSecretRef` (2.4) - NOT a plaintext appsettings `P12Password`; a production `KeyId` (not the dev default); no ephemeral fallback reachable.

### 2.2 P12 startup self-test (parity with the Pkcs11 self-test)
- At startup, for the P12 posture: load the cert/key, SIGN a self-test payload, VERIFY it with the exported public key, and confirm the KeyId is the production scheme (not the dev default). On any failure => fail-closed startup.
- **Active-signer == JWKS-current consistency (P1/GPT-06):** the active signer's public-key fingerprint AND kid MUST equal the CURRENT JWKS key's fingerprint + kid, and the self-test JWS MUST verify with the JWKS current key. Mismatch => fail-closed (guards signing with key A while publishing key B).
- Logging: allowed = signer posture, `kid`, public-key `fingerprint` (sha256), secret-source TYPE, a REDACTED P12 filename/key-source-id. Forbidden = full P12 path, password, private material, raw cert private fields.

### 2.3 JWKS distribution (current + previous) - OQ-B LOCKED
- `GET /.well-known/jwks.json`: **unauthenticated, public-key-only, LAN-visible** (callers must fetch + pin); `Cache-Control: no-store` or short `max-age` (~300s). Content = public keys only, NEVER private material.
- **Registry source (LOCKED):** CURRENT key = the active signer's public key (from the self-test). PREVIOUS keys = a **config-only public-JWK registry** (`TagEkyc:EvidenceSigning:Jwks:PreviousKeysFile` or equivalent) containing ONLY public JWK metadata per key: `kid`, `kty=EC`, `crv=P-256`, `x`, `y`, `use=sig`, `alg=ES256`, `fingerprint`, `notBefore`/`notAfter` (or `retiredAt`). Startup validation of the registry: **reject** any JWK with a private `d`, a duplicate `kid` (incl. vs current), or a malformed P-256 key => fail-closed; missing validity metadata on a previous key => fail-closed.
- **Rotation overlap window = config** (`TagEkyc:EvidenceSigning:Jwks:PreviousKeyOverlapDays` or per-key `notAfter`): fail on malformed/duplicate-kid; expired-but-retained handling per runbook (warn if runbook allows).
- Because proofs carry `KeyId`, a caller selects the verifying key by `kid`; an old proof MUST still verify against its previous JWKS key after the current key rotates (test this). Proof schema UNCHANGED (KeyId already present).
- HSM-cutover: new `kid` for the HSM key; JWKS serves old (P12) + new (HSM) during overlap so P12-signed proofs still verify post-cutover. Rotation runbook documented (no auto-rotation in this slice).

### 2.4 P12 password secret source + custody
- **Password source (enforceable, P1/Codex):** production accepts the P12 password ONLY via `P12PasswordSecretRef` -> resolved from an environment secret variable, a DPAPI/protected secret-file path, or a hospital-approved secret provider. REJECT a plaintext appsettings `P12Password` (if the config model can't distinguish source, only bind `P12PasswordSecretRef`, never a raw password value). Startup fails if the resolved source is plaintext appsettings. Grep/test: appsettings + example files contain NO password / private key / `d`.
- **Sanitize P12 errors (P2):** the current code throws `FileNotFoundException(..., options.P12Path)` (`LocalDevEs256JwsEvidenceSigner.cs:~46`) - a logged startup exception would leak the path. The `ProductionTrialP12` validator MUST catch/wrap P12 load errors and surface reason + kid/fingerprint/presence only (redacted filename/source-id), never the full path/password/private.
- **Custody:** P12 file OUTSIDE the repo/app directory; OS ACL = service account read-only; encrypted + access-controlled backup; documented emergency revoke/rotate.
- **Restore test = RUNBOOK EVIDENCE (not a unit test):** a runbook record/template capturing `kid`, public `fingerprint` before + after restore, date, operator - and a check that an old proof still verifies. NO path/password/private in the record. (Automated tests cover P12 load/self-test/JWKS; the backup-restore drill is operational.)

## 3. STOP / NOT
- NO ephemeral/placeholder key reachable in production; NO `LocalDev`-named/dev-default-KeyId signer as the production posture.
- NO private key material or P12 password/path in repo/logs/JWKS.
- `RequireHardwareSigner=true` MUST still force PKCS#11 (P12 posture only when hardware not required).
- NO JWKS serving only the current key (must serve previous within overlap) - else HSM cutover breaks historical trust.
- NO signing with a key whose public part is not the JWKS current key (active-signer == JWKS-current enforced at startup).
- NO plaintext appsettings P12 password (only `P12PasswordSecretRef`); NO private `d` in the JWKS previous-key registry.
- NO eKYC/proof/manifest logic change beyond adding the JWKS read + startup validation (proof KeyId already exists).

## 4. Definition of Done
- [ ] `TagEkyc:EvidenceSigning:Backend=ProductionTrialP12` posture (OQ-A); production forbids LocalDev/ephemeral/dev-KeyId/missing-P12Path/missing-password => fail-closed distinct reason codes; RequireHardwareSigner=true forces Pkcs11 - tests each.
- [ ] P12 startup self-test (load/sign/verify/KeyId-check) + **active-signer public-key fingerprint+kid == JWKS current** + self-test JWS verifies with JWKS current; fail-closed on any mismatch; logs only posture/kid/fingerprint/source-type/redacted-filename (grep/test: no full path/password/private).
- [ ] `GET /.well-known/jwks.json` unauthenticated public-key-only, `Cache-Control` short/no-store; current from active signer + previous from `TagEkyc:EvidenceSigning:Jwks:PreviousKeysFile` (public-only; reject private `d`/duplicate `kid`/malformed P-256/missing validity => startup fail); test: proof signed by a previous key STILL verifies via its kid after rotation.
- [ ] Rotation overlap = config (`TagEkyc:EvidenceSigning:Jwks:PreviousKeyOverlapDays`/per-key notAfter); HSM-cutover runbook (new kid, old+new JWKS overlap); no auto-rotation.
- [ ] P12 password only via `P12PasswordSecretRef` (env-secret/DPAPI-file/secret-provider); plaintext-appsettings source => startup fail; P12 load errors sanitized (no path/password); appsettings/example grep = no password/private.
- [ ] Restore drill = runbook evidence template (kid + before/after fingerprint + date + operator + old-proof-verifies; no path/password/private).
- [ ] dotnet test green; no eKYC/proof schema change (KeyId already present); no secrets committed.

## 5. Review tier & attacks
Tier-1. (a) any production path still using ephemeral/placeholder/dev-KeyId/LocalDev signer? (b) P12 password/path/private in logs/JWKS/repo? (c) does RequireHardwareSigner still force Pkcs11? (d) JWKS omit previous keys => HSM cutover breaks old-proof verify? (e) does an old-key proof verify after rotation (kid selection)? (f) P12 self-test actually sign+verify, or just presence-check? (g) restore yields the same kid/fingerprint? (h) any eKYC/proof behavior change?

## 6. Open questions - RESOLVED
- OQ-A: RESOLVED = explicit `TagEkyc:EvidenceSigning:Backend = ProductionTrialP12` posture (2.1); config/log/error never reads as LocalDev in production.
- OQ-B: RESOLVED = previous keys from a config-only public-JWK registry file `TagEkyc:EvidenceSigning:Jwks:PreviousKeysFile` (2.3); public-only, validated, no private `d`.

## STOP / RRI (Homeowner)
STOP + ask if the build: ships an ephemeral/placeholder/LocalDev/dev-KeyId production signer; logs or commits P12 password/path/private; lets RequireHardwareSigner bypass PKCS#11; serves JWKS without previous keys; or changes eKYC/proof logic.
