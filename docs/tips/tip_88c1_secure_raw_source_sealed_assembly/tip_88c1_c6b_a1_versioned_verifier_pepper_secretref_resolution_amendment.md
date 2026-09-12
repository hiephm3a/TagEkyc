TIP-88C1-C6B-A1 — BOUNDED RRI v2 AMENDMENT
EXACT VERIFIER-PEPPER SECRETREF MATERIAL RESOLUTION

AMENDMENT ID:
C6B-A1-VERSIONED-VERIFIER-PEPPER-SOURCE-01-A2

HISTORICAL PRE-AMENDMENT BASELINE — VERIFIED AT STOP / NON-OPERATIVE

The following hashes identify the exact predecessor bytes that triggered this
amendment. They are retained as correction provenance only and MUST NOT be
consumed as the current executable catalogue. The successor catalogue is the
exact bound catalogue in Parent v0.8 after this amendment is incorporated.

RRI v2:
docs/tips/tip_88c1_secure_raw_source_sealed_assembly/
tip_88c1_c6b_a1_versioned_verifier_pepper_source_rri.md

SHA-256:
72E75D3F2BEB70B6D07DEE14D27557ED5A8F6AADE3EFFF0B963313F22975BC30

Parent v0.8:
4FC588CF184979E2FD53581B8F52C5D40A15DA6D5C2BD65F4F39A4406F20BD68

Operation Master:
C90111A012C378A1D1C57543F52AC5228314A2C6C34C3BA5F27EC6A84798336F

PURPOSE:

Close exactly ONE remaining implementation blocker.

The landed generic SecretRefResolver normalizes file-backed material through
behavior equivalent to:

```
File.ReadAllText(path).TrimEnd('\r', '\n')
```

RRI v2 requires exact canonical material and explicitly requires CR/LF-bearing
verifier-pepper files to be rejected.

Because the generic resolver strips the newline before A1 receives the material,
a 44-byte:

```
<43 canonical bytes><LF>
```

file becomes indistinguishable from a canonical 43-byte file before canonical
validation.

Therefore the mandatory negative proof cannot go RED through the generic
material-resolution path.

The Builder MUST NOT weaken RRI canonicality to accommodate the landed resolver.

THIS AMENDMENT DOES NOT ALTER:

* canonical 43-character / 32-byte codec;
* canonical alphabet `[A-Za-z0-9_-]`;
* base64url tail-bit mathematics;
* HKDF-SHA256;
* three digest formulas;
* CurrentVersion;
* R01 version authority;
* historical-version resolution;
* lifecycle supersession;
* retirement semantics;
* BootstrapSecret exact 32-byte rule;
* CaptureCapabilitySecret exact 32-byte rule;
* project graph;
* generic SecretRefResolver behavior.

No product mutation is authorized until this amendment receives bounded
independent PASS.

============================================================

1. RATIFIED RESOLUTION
   ============================================================

Capture Runtime verifier pepper SHALL use one narrow exact-preserving
secret-reference material resolver dedicated to this domain.

It SHALL be implemented inside the already-authorized file:

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimeVerifierPepperProvider.cs

Canonical helper identity:

CaptureRuntimeVerifierPepperSecretRefResolver

No additional product file is authorized.

It is NOT:

* a replacement generic secret resolver;
* a Client API-key resolver;
* a new secret-store abstraction;
* an A2/A3 facility;
* a new service or executable.

The SAME helper MUST be used by:

A. CaptureRuntimeVerifierPepperProvider

and

B. R01 offline platform-operator provisioning in:

tools/TagEkyc.ApiKeyProvisioner/Program.cs

The existing ApiKeyProvisioner project already references:

src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj

Therefore:

* no new ProjectReference is required;
* no `.csproj` mutation is authorized;
* no project-graph change is authorized.

============================================================
2. HELPER ACCESSIBILITY — MANDATORY
===================================

Section 1 requires cross-assembly use by:

tools/TagEkyc.ApiKeyProvisioner

The landed Infrastructure AssemblyInfo exposes internals only to:

TagEkyc.ArchTests
TagEkyc.IntegrationTests

and NOT to:

TagEkyc.ApiKeyProvisioner

Therefore:

* `CaptureRuntimeVerifierPepperSecretRefResolver` MUST be declared `public`;
* every member invoked by R01 MUST be `public`;
* `internal` is FORBIDDEN for the shared callable surface.

The exact shared callable contract is:

```csharp
public static CaptureRuntimeVerifierPepperSecretRefResolution Resolve(
    string secretRef);

public enum CaptureRuntimeVerifierPepperSecretRefStatus
{
    Success,
    ReferenceInvalid,
    MaterialUnavailable,
    MaterialInvalid
}

public sealed record CaptureRuntimeVerifierPepperSecretRefResolution(
    CaptureRuntimeVerifierPepperSecretRefStatus Status,
    CaptureRuntimeVerifierPepperMaterialLease? Material);

public sealed class CaptureRuntimeVerifierPepperMaterialLease : IDisposable
{
    public ReadOnlyMemory<byte> CanonicalAscii { get; }
    public void Dispose();
}
```

`Success` MUST carry one non-null lease whose `CanonicalAscii` is exactly the
43 accepted ASCII bytes. Every non-success status MUST carry `Material = null`.
The lease owns its mutable 43-byte backing array, exposes it only through the
read-only view, and zeroes the whole backing array exactly once on `Dispose`.
Neither resolver nor lease returns a managed secret string. Expected invalid,
missing, unreadable or noncanonical input is represented only by the closed
status taxonomy above; it is not thrown as a caller-visible parser/file-system
exception and carries no path, variable name or material detail.

Mapping is exact:

* malformed/unsupported SecretRef, empty env name, relative/empty file path
  -> `ReferenceInvalid`;
* absent environment variable or missing/unreadable file
  -> `MaterialUnavailable`;
* directory target -> `ReferenceInvalid`;
* present material with wrong byte/character/canonical shape
  -> `MaterialInvalid`;
* exact accepted material -> `Success`.

Both API provider and R01 MUST call this exact method and consume/dispose the
same lease contract. No second adapter, string-returning overload, exception
mapping or caller-specific result translation is authorized.

Also FORBIDDEN:

[assembly: InternalsVisibleTo("TagEkyc.ApiKeyProvisioner")]

and any equivalent friend-assembly change.

Do NOT modify:

src/TagEkyc.Infrastructure/Properties/AssemblyInfo.cs

That file remains out of scope.

"Implementation detail" in this RRI means:

domain-specific and not general-purpose.

It does NOT mean CLR `internal`.

============================================================
3. SUPPORTED SECRETREF SCHEMES
==============================

Exactly two schemes are supported for Capture Runtime verifier-pepper material:

env:<NAME>

file:<ABSOLUTE_PATH>

No other scheme is authorized by A1.

No fallback from one scheme to another is allowed.

============================================================
3.1 env:
========

`env:` requires a nonempty environment-variable name.

Missing variable:

MATERIAL UNAVAILABLE.

A present environment value MUST be consumed EXACTLY as supplied by the process
environment.

It MUST NOT undergo:

* Trim;
* TrimEnd;
* CR/LF removal;
* whitespace normalization;
* Unicode normalization;
* case conversion;
* URL decoding;
* parser repair.

The exact value is passed to the already-ratified RRI v2 canonical
43-character codec.

The canonical alphabet remains exactly:

[A-Za-z0-9_-]

Therefore:

canonical value
-> may PASS.

canonical value containing one or more `-`
-> MUST remain eligible to PASS.

canonical value containing one or more `_`
-> MUST remain eligible to PASS.

canonical value + LF
-> MATERIAL INVALID.

canonical value + CRLF
-> MATERIAL INVALID.

leading/trailing whitespace
-> MATERIAL INVALID.

============================================================
3.2 file:
=========

`file:` requires:

* nonempty path;
* ABSOLUTE path;
* existing file;
* target is not a directory.

Capture Runtime verifier-pepper MATERIAL MUST NOT be obtained through the
generic normalized SecretRefResolver value path.

The helper MUST inspect exact file bytes.

Canonical procedure:

1. Validate the absolute path.

2. Open/read the target as raw bytes.

3. The accepted material MUST contain EXACTLY 43 bytes.

4. Reject if the ACTUAL material read is fewer or more than 43 bytes.

5. Reject unless every one of the 43 bytes is ASCII and belongs EXACTLY to:

   A-Z
   a-z
   0-9
   `-`

   _

   Equivalently:

   [A-Za-z0-9_-]

   Both `-` and `_` are valid canonical base64url alphabet members and MUST NOT
   be rejected.

6. Convert those exact 43 ASCII bytes one-for-one into a 43-character string.

7. Perform NO textual transformation.

8. Pass that exact string to the already-ratified RRI v2 canonical:

   decode
   -> canonical re-encode
   -> ordinal exact compare

   codec.

An implementation MAY inspect file metadata length before reading as an early
rejection optimization.

It MUST NOT rely solely on metadata length.

Acceptance is determined by the ACTUAL raw bytes read.

There is NO:

* File.ReadAllText;
* ReadToEnd().Trim*;
* Trim;
* TrimEnd;
* newline removal;
* BOM removal;
* encoding auto-detection;
* UTF-8 normalization;
* text normalization.

A verifier-pepper file is protocol material, NOT a human text document.

============================================================
4. EXACT FILE CONSEQUENCES
==========================

Exactly 43 canonical ASCII raw bytes:

PASS material parsing.

Exactly 43 canonical ASCII raw bytes containing valid `-`:

PASS material parsing.

Exactly 43 canonical ASCII raw bytes containing valid `_`:

PASS material parsing.

43 canonical bytes + LF:

44 bytes
-> MATERIAL INVALID.

43 canonical bytes + CRLF:

45 bytes
-> MATERIAL INVALID.

UTF-8 BOM + 43 canonical bytes:

46 bytes
-> MATERIAL INVALID.

43 canonical bytes + trailing ASCII space:

44 bytes
-> MATERIAL INVALID.

Leading ASCII space + 43 canonical bytes:

44 bytes
-> MATERIAL INVALID.

42-byte file:

MATERIAL INVALID.

44-byte file:

MATERIAL INVALID.

Any byte outside:

[A-Za-z0-9_-]

MATERIAL INVALID.

Empty file:

MATERIAL INVALID.

Missing file:

MATERIAL UNAVAILABLE.

Directory target:

REFERENCE INVALID.

The implementation MUST NOT repair any invalid case into a valid secret.

============================================================
5. ERROR TAXONOMY / FAIL-CLOSED
===============================

REFERENCE INVALID includes:

* unsupported scheme;
* blank env variable name;
* blank file path;
* relative file path;
* directory target;
* structurally invalid SecretRef.

MATERIAL UNAVAILABLE includes:

* missing environment variable;
* missing file;
* inaccessible secret material under the existing dependency-failure model.

MATERIAL INVALID includes:

* wrong byte/character length;
* CR;
* LF;
* CRLF;
* BOM;
* whitespace;
* non-ASCII;
* byte outside `[A-Za-z0-9_-]`;
* noncanonical base64url tail bits;
* canonical re-encode mismatch.

All three categories fail closed.

They MUST NOT be mapped solely to:

* credential mismatch;
* secret mismatch;
* ACCESS_DENIED.

They map to the existing verifier-pepper:

dependency failure
/
startup failure
/
NOT_READY

semantics.

No full:

* file path;
* environment value;
* SecretRef;
* secret material;
* decoded key bytes

may enter:

* public HTTP error body;
* durable audit;
* ordinary logs.

============================================================
6. GENERIC SecretRefResolver REMAINS UNCHANGED
==============================================

This amendment does NOT authorize modification of the landed generic:

SecretRefResolver

or its existing semantics.

Existing consumers remain outside this bounded correction.

Capture Runtime verifier pepper bypasses ONLY the normalized MATERIAL-resolution
behavior because its ratified cryptographic protocol requires exact material.

Do NOT:

* fork the generic resolver globally;
* replace it globally;
* alter Client API-key pepper behavior;
* change unrelated secret consumers.

============================================================
7. API PROVIDER REQUIREMENT
===========================

CaptureRuntimeVerifierPepperProvider MUST resolve every configured:

VerifierPepperVersion
-> SecretRef

through:

CaptureRuntimeVerifierPepperSecretRefResolver

and then through the already-ratified:

canonical codec
+
HKDF/domain cryptography.

For Capture Runtime verifier-pepper MATERIAL it MUST NOT call:

SecretRefResolver.Resolve(secretRef).Value

or any behaviorally equivalent path that may remove CR/LF before canonical
validation.

It MUST NOT call any helper whose `file:` branch trims or normalizes material.

It MUST accept the full canonical base64url alphabet:

[A-Za-z0-9_-]

including both:

`-`

`_`

All other provider semantics remain frozen.

============================================================
8. R01 OFFLINE REQUIREMENT
==========================

R01 continues to accept exactly:

--capture-runtime-verifier-pepper-version

and

--capture-runtime-verifier-pepper-secret-ref

Its Capture Runtime verifier-pepper SecretRef MUST be resolved through the SAME
public:

CaptureRuntimeVerifierPepperSecretRefResolver

used by the API provider.

R01 MUST NOT independently implement:

* another file reader;
* another environment reader;
* another trimming rule;
* another SecretRef parser;
* another canonical alphabet;
* another canonicality policy.

Thus one exact:

file:<ABSOLUTE_PATH>

has identical verifier-pepper material semantics in:

API provider

and

offline R01.

No API-host DI/appsettings requirement is introduced into R01.

============================================================
9. REQUIRED PROOFS
==================

Extend:

VersionedVerifierPepperSource_CurrentAndReferencedVersionsFailClosed

with the following exact material-source cases.

============================================================
9.1 FILE-BACKED CASES
=====================

POSITIVE CONTROLS:

1. exact 43 canonical raw bytes with no `-`/`_`
   -> PASS.

2. exact 43 canonical raw bytes containing at least one valid `-`
   -> PASS.

   This positive control is MANDATORY.

   It exists specifically to detect accidental narrowing of the canonical
   alphabet from:

   [A-Za-z0-9_-]

   to an incomplete alphabet that omits `-`.

3. exact 43 canonical raw bytes containing at least one valid `_`
   -> PASS.

NEGATIVE CONTROLS:

4. 43 canonical bytes + LF
   -> RED.

5. 43 canonical bytes + CRLF
   -> RED.

6. UTF-8 BOM + 43 canonical bytes
   -> RED.

7. trailing ASCII space
   -> RED.

8. leading ASCII space
   -> RED.

9. 42-byte file
   -> RED.

10. 44-byte file
    -> RED.

11. invalid ASCII alphabet byte outside `[A-Za-z0-9_-]`
    -> RED.

12. non-ASCII byte
    -> RED.

13. empty file
    -> RED.

14. missing file
    -> dependency failure.

15. directory path
    -> invalid reference.

============================================================
9.2 ENVIRONMENT CASES
=====================

16. exact canonical environment value
    -> PASS.

17. exact canonical environment value containing valid `-`
    -> PASS.

18. exact canonical environment value containing valid `_`
    -> PASS.

19. canonical value + LF
    -> RED.

20. canonical value + CRLF
    -> RED.

21. leading/trailing whitespace
    -> RED.

22. missing variable
    -> dependency failure.

============================================================
9.3 CROSS-BOUNDARY PROOF
========================

23. The exact public helper MUST be directly testable and produce the expected
    canonical K_master/domain material from one synthetic `file:` fixture.

24. Architecture/source proof MUST separately prove that BOTH:

    CaptureRuntimeVerifierPepperProvider

    and

    tools/TagEkyc.ApiKeyProvisioner/Program.cs

    call that SAME public helper.

25. API-provider and R01 consumer paths MUST therefore share one exact
    material-resolution implementation.

26. The same newline-bearing fixture MUST be rejected by the shared helper
    consumed by both paths.

27. A valid Client API-key pepper or successful generic SecretRefResolver result
    MUST NOT substitute for Capture Runtime exact-resolution failure.

28. No test may obtain success by manually pre-trimming a fixture.

Do NOT spawn the ApiKeyProvisioner CLI process solely to satisfy this proof.

The bounded proof model is:

* executable helper proof for material semantics;
* architecture/source proof for both production consumers joining to the same
  helper.

============================================================
10. ARCHITECTURE RED PROOF
==========================

Extend:

CaptureRuntimeVerifierPepper_HasDedicatedDomainAndNoApiKeyPepperDependency

to turn RED if Capture Runtime verifier-pepper MATERIAL uses any path equivalent
to:

File.ReadAllText(...).Trim...

ReadToEnd().Trim...

SecretRefResolver.Resolve(...).Value

where file-backed material can have CR/LF removed before canonical validation.

Also RED if:

* a second competing Capture Runtime exact SecretRef resolver is introduced;
* R01 and API provider use different resolution implementations;
* canonical alphabet differs from `[A-Za-z0-9_-]`;
* valid `-` is rejected;
* valid `_` is rejected;
* the generic SecretRefResolver is modified by this RRI;
* a new product file is created for this helper;
* `.csproj` changes;
* project graph changes;
* helper is not public/reachable from ApiKeyProvisioner;
* InternalsVisibleTo is added;
* AssemblyInfo.cs changes;
* proof attempts to satisfy shared-resolution solely by spawning the CLI instead
  of proving the common implementation join.

============================================================
11. EXACT AUTHORITY DELTA
=========================

No new product path is added.

Implementation authority is added only within already-authorized:

src/TagEkyc.Infrastructure/CaptureRuntime/
CaptureRuntimeVerifierPepperProvider.cs

for public:

CaptureRuntimeVerifierPepperSecretRefResolver

and within already-authorized:

tools/TagEkyc.ApiKeyProvisioner/Program.cs

to consume that SAME public helper for R01.

Existing test files may be modified only as already authorized by RRI v2.

No:

* DDL semantic change;
* T1/T2/T3/T4 semantic change;
* migration change;
* schema change;
* new file;
* `.csproj` change;
* AssemblyInfo.cs change.

============================================================
12. MANDATORY CATALOGUE RECONCILIATION
======================================

The currently materialized Parent, Operation Master and RRI v2 contain operative
statements identifying the generic SecretRefResolver as the Capture Runtime
verifier-pepper material resolver.

This amendment mechanically changes that material-source contract.

Therefore the following are MANDATORY, not conditional:

1. Update and rebind Parent.

2. Update and rebind Operation Master.

3. Update and rebind RRI v2 wherever it still names the generic
   SecretRefResolver as the Capture Runtime verifier-pepper MATERIAL resolver.

4. Rebind every affected derived:

   * artifact catalogue;
   * operation ledger;
   * material/secret ledger;
   * provider mapping;
   * proof mapping;
   * readiness mapping.

For Capture Runtime verifier-pepper MATERIAL, the sole operative resolver after
this amendment is:

CaptureRuntimeVerifierPepperSecretRefResolver

The generic SecretRefResolver may remain mentioned only as:

* historical/superseded material-source text;
* explicitly unchanged resolver for unrelated domains;
* comparison/evidence explaining why Capture Runtime uses its exact resolver.

There MUST be zero operative statement claiming Capture Runtime verifier-pepper
MATERIAL is resolved through:

SecretRefResolver.Resolve(...).Value

or equivalent generic normalized material resolution.

============================================================
13. MECHANICAL RED CONDITIONS
=============================

Bounded review MUST HOLD if ANY is true:

1. `file:` Capture Runtime pepper can pass through a newline-trimming resolver.

2. a 44-byte `<canonical><LF>` file becomes valid.

3. a 45-byte `<canonical><CRLF>` file becomes valid.

4. a BOM-bearing file becomes valid through encoding normalization.

5. file material is decoded using text APIs before exact raw-byte validation.

6. actual bytes read are not verified and implementation relies solely on file
   metadata length.

7. canonical alphabet is anything other than:

   [A-Za-z0-9_-]

8. valid canonical `-` is rejected.

9. valid canonical `_` is rejected.

10. positive proof with a valid canonical pepper containing `-` is absent or
    fails.

11. API provider and R01 use different material-resolution implementations.

12. generic SecretRefResolver is modified.

13. file support is silently removed instead of implementing exact resolution.

14. Client API-key pepper/resolver becomes fallback.

15. missing/invalid verifier pepper becomes authentication mismatch.

16. new file/project/reference is required.

17. unrelated A1 semantics change.

18. helper is not `public`.

19. InternalsVisibleTo is added for ApiKeyProvisioner.

20. AssemblyInfo.cs is modified.

21. proof requires spawning the CLI solely to demonstrate shared resolver
    semantics.

22. Parent, OP, RRI or any operative derived catalogue still identifies generic
    SecretRefResolver as the Capture Runtime verifier-pepper MATERIAL resolver
    after reconciliation.

============================================================
14. BOUNDED SCOPE AND EXECUTION
===============================

Review ONLY this blocker and its direct authority/proof/catalogue consequences.

DO NOT reopen:

* canonical base64url codec except verifying exact alphabet
  `[A-Za-z0-9_-]`;
* tail-bit mathematics;
* HKDF vectors;
* digest domains;
* exact 32-byte BootstrapSecret rule;
* exact 32-byte CaptureCapabilitySecret rule;
* CurrentVersion;
* lifecycle supersession;
* retirement semantics;
* R01 version authority;
* A1/A3 seam;
* A2;
* A3.

During this amendment/review round:

NO product mutation.

Allowed work:

* materialize/reconcile authority documents;
* rebind exact SHAs;
* inspect repo evidence;
* execute bounded review.

After bounded PASS, existing A1 implementation authority resumes from the
previous STOP checkpoint.

Still NOT granted:

* stage;
* commit;
* push;
* A2;
* A3.

============================================================
15. REQUIRED TERMINAL OUTPUT
============================

Return ALL of:

1. Successor Parent SHA + exact line count.

2. Successor Operation Master SHA + exact line count.

3. Successor RRI v2 SHA + exact line count.

4. Amendment SHA + exact line count if materialized separately.

5. Changed companion/catalogue SHA list.

6. Exact helper identity:

   CaptureRuntimeVerifierPepperSecretRefResolver

7. Exact owning file:

   src/TagEkyc.Infrastructure/CaptureRuntime/
   CaptureRuntimeVerifierPepperProvider.cs

8. Declared accessibility:

   public

9. Exact canonical byte alphabet:

   [A-Za-z0-9_-]

10. Positive valid-`-` file proof result.

11. Positive valid-`_` file proof result.

12. `env:` exact-resolution result.

13. `file:` raw-byte-resolution result.

14. LF negative proof result.

15. CRLF negative proof result.

16. BOM negative proof result.

17. API-provider/R01 common-helper join proof.

18. Confirmation:

    AssemblyInfo.cs unchanged.

19. Confirmation:

    no InternalsVisibleTo added.

20. Confirmation:

    generic SecretRefResolver unchanged.

21. Confirmation:

    no new product file.

22. Confirmation:

    no `.csproj` / project-graph mutation.

23. Operative generic-SecretRefResolver-as-CaptureRuntime-material-resolver
    occurrence count after reconciliation.

Required value:

0

24. staged count.

25. conflicted count.

26. Complete bounded findings.

Terminal verdict MUST be exactly:

PASS — 0 ACTIONABLE FINDINGS

or:

HOLD — <exact actionable findings>

A PASS restores the previously granted A1 implementation authority on the
successor exact-byte catalogue.

A PASS DOES NOT grant:

* stage;
* commit;
* push;
* A2;
* A3.

============================================================
END AMENDMENT
=============
