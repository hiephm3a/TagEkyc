# TIP-83 - Production Infrastructure (hospital on-prem) - PLANNING UMBRELLA

Status: PLANNING UMBRELLA (not a single build). v0.2 after GPT + Codex review (both: too large for one build -> SPLIT into Tier-1 slices). OQ-1 LOCKED (P12 trial-grade + HSM roadmap); OQ-2/OQ-3 RESOLVED below. Each slice gets its own brief -> review -> build.
Repo: D:\Task\Remote Signing\TagEkyc (server). Baseline: b69bcb8.
Scope: SERVER production infra. Agent packaging/installer = TIP-81. Consent gate = TIP-82 (agent) + TIP-82S (server consent-evidence) + TIP-82R (raw-evidence vault, only if DPO requires). This umbrella changes NO eKYC/proof logic.

## 0. Why (recon-confirmed dev-grade gaps)
Signing key EPHEMERAL (regen each restart w/o P12) -> no stable trust anchor. Persistence IN-MEMORY (lost on restart). API keys HARDCODED localdev plaintext. No enforced HTTPS, backup, durable audit/retention, rotation, monitoring/IR.

## 1. Build slices (own brief each) + 2-week priority
- **TIP-83A - Signing key + JWKS + startup validation** (BLOCKER #1: stable P12 signer, JWKS current+previous, P12 startup self-test, reject ephemeral/placeholder/localdev-signer). BUILD FIRST.
- **TIP-83B - Postgres production + migrations + backup/restore drill** (durable records + tested restore artifact).
- **TIP-83C - Secret + config hardening + hospital client-app provisioning** (no plaintext, no localdev-* identities/keys in production).
- **TIP-83D - TLS/transport + deployment runbook** (reverse-proxy-precise HTTPS enforcement, topology).
- **TIP-83E - Audit tamper-evidence + retention/purge + health/monitoring/IR** (append-only + restricted grants baseline; hash-chain roadmap).
Priority: A -> B -> C -> D; E baseline-then-harden. A is the proof-verification blocker for SignFlow.

## 2. Cross-cutting redlines (EVERY slice inherits)
- NO ephemeral/placeholder/dev signing key in production; NO `LocalDev`-named/seed signer as the production posture (P12 mode must be a clearly production-allowed trial signer, not "LocalDev"); private key never in repo/log/JWKS.
- `RequireHardwareSigner=true` ALWAYS forces PKCS#11/HSM. Production reconciled with `Program.cs:155` (see TIP-83A).
- NO plaintext secrets (keys/passwords/connection) in appsettings/.bat/repo; secrets never logged (presence/fingerprint only).
- NO `LocalDevRuntimePolicySource` / `localdev-*` key prefix or value as a production auth provider -> startup fail (TIP-83C).
- NO HTTP reachable in production; NO in-memory persistence in production.
- NO raw biometric stored (verify_and_discard - TIP-82; raw vault = TIP-82R only if DPO-approved).
- NO weakening of the TIP-79 gate or TIP-80/80S trust separation; NO eKYC/proof/decision-basis logic change (infra only).

## 3. OQ resolutions
- **OQ-1 (signing tier): LOCKED** = P12 trial-grade now + dated HSM upgrade roadmap + keyId-migration/JWKS-old+new; gated on hospital security/DPO accepting trial-grade key custody. Detail in TIP-83A.
- **OQ-2 (deployment): RESOLVED (lean)** = Windows Service + hospital-IT-managed Postgres + reverse-proxy TLS termination (trusted-proxy forwarded headers). Hospital IT owns: TLS certs, secret store, Postgres host + backup. Detail in TIP-83D/83B.
- **OQ-3 (retention): RESOLVED** = retention windows are a Legal/DPO-owned INPUT; the build provides config fields + runbook to APPLY them, does NOT hardcode 7/30/90. Raw biometric not stored (TIP-82); raw-retention = TIP-82R (per TIP-82 v0.5 Section 7), never folded into TIP-83.

## 4. Dependencies / companions
- Hospital IT: infra, TLS certs, Postgres + backup, secret store, network/firewall.
- TIP-79 gate already enforces https/creds/model/sdk on the agent side; TIP-83 makes the server actually production.
- SignFlow client-app registration + trust-anchor (JWKS) fold into TIP-83A (JWKS) + TIP-83C (provisioning).
- Consent/data-side gates = TIP-82 / caller biometric-consent / TIP-82S; TIP-83 = infra-side gate.

## STOP / RRI (Homeowner)
STOP + ask if any slice: ships an ephemeral/placeholder/LocalDev production signing key; puts private key/secrets in repo/logs; allows HTTP or in-memory in production; uses localdev-* auth identities in production; stores raw biometric; weakens the TIP-79/80 trust model; hardcodes Legal-owned retention windows; or changes eKYC/proof logic.
