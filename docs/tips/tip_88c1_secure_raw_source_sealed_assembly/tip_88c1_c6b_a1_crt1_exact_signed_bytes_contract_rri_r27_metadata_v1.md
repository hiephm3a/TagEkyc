# TIP-88C1-C6B-A1 — Bounded micro-RRI: CRT1 exact signed bytes

> R27 metadata amendment successor, 2026-09-11. Implements Homeowner attachment 033da395-183e-45b6-b716-7d5816d14594. Preserved predecessor: `tip_88c1_c6b_a1_crt1_exact_signed_bytes_contract_rri.md`, SHA-256 `EDA8703C3969F6A7043E496553FEC3E2B3AD72462C319EE10BE3491490489903`. This successor changes only R27 binding/vector and mechanical references; prior decisions otherwise remain operative.

**RRI ID:** `C6B-A1-CRT1-EXACT-SIGNED-BYTES-CONTRACT-01`  
**Status:** REVIEW CANDIDATE — NO PRODUCT MUTATION AUTHORIZED  
**Baseline parent:** `85984BFF26D84D1F76B3133F74099146F5B4403B4F704C6BF9A81C7CF583CCFB`

## 1. Purpose and ownership

CRT1's signed-byte grammar is already ratified. This RRI closes only the
contract join needed to preserve those exact bytes. The API HTTP parsing
boundary is the sole server-side CRT1 preimage constructor. It validates the
exact lexical envelope, constructs the exact preimage once, and passes it to
`ICaptureRuntimeRequestAuthenticator`. The authenticator verifies that supplied
preimage and must never reconstruct it from parsed values.

## 2. Corrected contract

`CaptureRuntimeSignedRequest` carries exactly the authentication facts required
by the current boundary:

```csharp
public sealed record CaptureRuntimeSignedRequest(
    Guid CredentialId,
    long CredentialGeneration,
    DateTimeOffset SignedAtUtc,
    byte[] Nonce,
    byte[] Signature,
    string RequiredRole,
    ReadOnlyMemory<byte> ExactSignedPreimage);
```

The API parser creates a dedicated backing buffer for `ExactSignedPreimage` and
retains no mutable alias capable of changing it after contract construction.
Parsed credential, generation, time and nonce come from the same validated
HTTP envelope as the exact bytes. A request with disagreement between parsed
fields and signed bytes is structurally forbidden. Noncanonical/malformed
header text is rejected before this contract exists.

`Method`, `CanonicalPath` and `BodyDigest` are removed from the authentication
contract unless a separately identified already-ratified consumer requires
them; they confer no signature-reconstruction authority.

## 3. Sole parser construction rule

The parser constructs the existing CRT1 sequence using:

- uppercase HTTP method;
- `Path.Value` exactly as route-matched;
- exact validated timestamp header text, never a reformatted `SignedAtUtc`;
- exact validated nonce header text, never a re-encoded nonce;
- canonical lowercase media-type token or empty;
- invariant content length or `0`;
- the existing body-commitment rule;
- the exact existing route-specific operation-binding line;
- LF only and exactly one final LF.

Closed JSON routes retain SHA-256 of exact received closed JSON bytes. GET
retains SHA-256(empty). Raw ingress uses its already-ratified declared body
commitment and must read zero `Request.Body` bytes while constructing the CRT1
preimage. This changes neither A3 nor R1 body-read ownership.

Existing operation-binding lines remain unchanged, including bind
`CaptureCapabilityId=<N>;BindOperationId=<N>;SecretSha256=<hex>`, reconcile
`CaptureCapabilityId=<N>;BindOperationId=<N>`, bound-operation
`BindingId=<N>;IdempotencyKey=<N>`, raw ingress
`ingress=IngressMetadataSha256=<hex>` as defined by the R27-amended Operation Master,
and the empty line for other operations. Raw ingress requires the nonempty ingress
form; other operations must reject it.

## 4. Authenticator rule

`CaptureRuntimeRequestAuthenticator` verifies ECDSA-SHA256 over
`ExactSignedPreimage`, then may use the parsed fields for timestamp window,
credential/generation/current-lineage, route-role and transaction-N nonce
processing. It must never regenerate timestamp, nonce, path, media type,
content length, body commitment or operation binding, and must never serialize
a DTO to recreate CRT1 bytes.

## 5. Required proof

Extend `Crt1_GoldenVectorsMatchClientAndServer` to prove:

1. client golden bytes equal the API parser's `ExactSignedPreimage` byte for byte;
2. the authenticator verifies exactly those bytes;
3. one-byte mutations of method, path, timestamp lexeme, nonce lexeme, media
   type, content length, body commitment and operation binding each fail;
4. missing or changed final LF fails;
5. noncanonical timestamp/nonce is rejected before contract construction;
6. authenticator/source architecture contains no alternate preimage builder;
7. RawIngress construction opens/reads `Request.Body` zero times.

Architecture/source proof must turn RED if both parser and authenticator contain
competing CRT1 builders.

## 6. Authority delta and fences

After PASS, correction may modify only already-authorized A1 contract, ports,
API parser/endpoint, authenticator and CRT1 contract/architecture test surfaces.
No new project/reference and no SQL, schema or migration change is required or
authorized.

This RRI does not reopen CRT1 cryptography or sequence, ENROLL1, ROTATE1,
verifier pepper, R01/R02 fingerprints, nonce/role/capability semantics,
RawIngress R1 ownership, A2 or A3.

PASS resumes the existing A1 implementation authority at the current
checkpoint. Ordinary compile/test defects are implementation work. A new STOP
is justified only by a genuinely new normative contradiction. PASS grants no
stage, commit, push, A2 or A3 authority.

