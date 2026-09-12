# TIP-88C1-C6B-A1 — Bounded RRI: R01/R02 root request fingerprint

**RRI ID:** `C6B-A1-R01-R02-REQUEST-FINGERPRINT-01`  
**Status:** REVIEW CANDIDATE — NO PRODUCT MUTATION AUTHORIZED  
**Baseline parent:** `3CA3AD2E6E852A12D3537EB4A197BA755A86B10E6732212C14CFEDA7C7BF27DF`  
**Baseline operation master:** `7A5D1F2695E041D180E7BD44A99B0D5917A76F50E2D8387B3288A852E8744C77`

## 1. Scope

This RRI closes only the missing canonical request-fingerprint preimages for
offline root operations R01 and R02. It does not reopen verifier-pepper or
SecretRef resolution, HKDF, credential-secret format, SQL signatures, schema,
root operation/event semantics, A1 architecture, A2 or A3.

No product, migration, API, authentication, test, configuration, project,
stage, commit or push mutation is authorized by this review candidate.

## 2. Common canonical encoding

The SQL parameter is the raw 32-byte result of `SHA-256(preimage)`, never the
64 ASCII hexadecimal proof representation. The preimage is UTF-8 restricted to
ASCII, with no BOM or CR, one field per line, exact field order, one-byte LF
separators and exactly one final LF. Platform newline conversion, JSON,
reserialization, whitespace and locale-dependent formatting are forbidden.

Canonical atoms are:

- UUID: lower-case RFC-4122 `N`, exactly 32 hexadecimal ASCII characters;
- timestamp: UTC `yyyy-MM-ddTHH:mm:ss.fffffffZ`;
- positive integer: invariant decimal ASCII, no sign or leading zero.

## 3. R01 platform-provision fingerprint

Domain label:

```text
TAG-EKYC-A1-R01-PLATFORM-PROVISION-FINGERPRINT-v1
```

The preimage has exactly four lines and one final LF:

```text
TAG-EKYC-A1-R01-PLATFORM-PROVISION-FINGERPRINT-v1
<OperationId N>
<PrincipalId N>
<ExpiresAtUtc T>
```

It binds exactly `OperationId`, `PrincipalId` and `ExpiresAtUtc`. It excludes
the generated plaintext secret, `KeyLookupPrefix`, `SecretDigest`,
`VerifierPepperVersion`, verifier-pepper `SecretRef`, resolved pepper material,
derived HKDF key, `p_now_utc`, generated `CredentialId`, completion/revision and
all SQL result fields.

Consequently, a lost-response retry with the same operation ID, principal and
expiry but newly generated secret/verifier material has the same fingerprint
and returns `ExistingMatchSecretUnavailable`; it is not `Conflict`, creates no
second credential/operation/event and never re-emits plaintext. Changing the
principal or expiry under the same operation ID changes the fingerprint and is
`Conflict`.

Golden vector (the displayed text has one final LF):

```text
TAG-EKYC-A1-R01-PLATFORM-PROVISION-FINGERPRINT-v1
01000000000040008000000000000001
01000000000040008000000000000002
2026-09-10T06:00:00.0000000Z
```

- exact preimage length: `145` bytes;
- SHA-256: `BF365A4BCBFBAD1AE637F396887D20C3382DD996260DFD3A7A1B3A55D80429E7`.

## 4. R02 platform-revoke fingerprint

Domain label:

```text
TAG-EKYC-A1-R02-PLATFORM-REVOKE-FINGERPRINT-v1
```

The preimage has exactly five lines and one final LF:

```text
TAG-EKYC-A1-R02-PLATFORM-REVOKE-FINGERPRINT-v1
<OperationId N>
<CredentialId N>
<ExpectedRevision invariant-decimal>
DeploymentRevocation
```

It binds exactly `OperationId`, `CredentialId`, `ExpectedRevision` and the
literal `DeploymentRevocation`. It excludes `p_now_utc`, resolved principal,
current state discovered by SQL, resulting revision, `RevokedAtUtc` and every
SQL result field. No arbitrary caller-selected reason is a fingerprint variant.

Golden vector (the displayed text has one final LF):

```text
TAG-EKYC-A1-R02-PLATFORM-REVOKE-FINGERPRINT-v1
02000000000040008000000000000001
02000000000040008000000000000002
3
DeploymentRevocation
```

- exact preimage length: `136` bytes;
- SHA-256: `7793EA47F3170F101BEE163EB3E5BF1DA5DBDF4133E6F57F5E37CDF58E986D07`.

Exact replay returns the frozen revoked result without a second root operation
or event. Changing credential or expected revision under the same operation ID
is `Conflict`.

## 5. Domain separation and implementation owner

R01 and R02 use the two distinct labels above. An unlabeled preimage or one
shared root label with operation kind outside the hash is forbidden.

After this RRI passes, the sole implementation owner is the existing authorized
`src/TagEkyc.Application/CaptureRuntime/CaptureRuntimeVerifierCryptography.cs`:

```text
ComputePlatformProvisionRequestFingerprint(
  Guid operationId, Guid principalId, DateTimeOffset expiresAtUtc)

ComputePlatformRevokeRequestFingerprint(
  Guid operationId, Guid credentialId, long expectedRevision)
```

The R01 helper cannot accept secret, prefix, digest, pepper version or SecretRef.
The R02 helper internally binds `DeploymentRevocation` and cannot accept reason
text. Both return exactly 32 bytes. `tools/TagEkyc.ApiKeyProvisioner/Program.cs`
must call these helpers; a second implementation or project-reference change is
forbidden.

## 6. Required proofs after PASS

`RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret` proves the
golden vector; operation/principal/expiry sensitivity; LF/final-LF/culture
invariance; exclusion of generated secret, prefix, digest, pepper version,
SecretRef and `p_now_utc`; regenerated-verifier exact replay; no duplicate
credential/operation/event or secret replay; and changed-intent conflict.

`RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain` proves the
golden vector; operation/credential/revision sensitivity; `p_now_utc`
exclusion; exact replay without duplicate root history; changed-target/revision
conflict; and rejection of any reason other than `DeploymentRevocation`.

A cross-domain proof mutates only the domain label and must change SHA-256.
Architecture/catalogue proof fails if the labels or implementation are shared,
R01 accepts generated verifier inputs, R02 accepts reason text, the CLI computes
a competing fingerprint, or another product implementation appears. Expected
goldens must be externally materialized, not generated only by code under test.

## 7. SQL, catalogue and checkpoint

No SQL signature, SQL body, DDL, table, constraint or replay/conflict semantic
change is required or authorized. The generic HTTP/API fingerprint grammar for
R03–R18, R20a, R20b, R21 and R24–R26 remains unchanged and must not be silently
broadened to R01/R02.

The successor catalogue must bind this RRI, state both root grammars literally,
name both proofs, and mechanically rebind every companion that embeds the
operation-master SHA. Existing valid implementation work remains at its STOP
checkpoint; persistence conversion remains non-candidate.

PASS restores the previously granted A1 implementation authority only on the
successor exact-byte catalogue. It grants no stage, commit, push, A2 or A3.
