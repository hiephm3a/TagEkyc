# C6B-A1 R27 metadata amendment — bounded evidence report v1

Date: 2026-09-11

## Status

SPECIFIED: C6B-A1-R27-INGRESS-METADATA-BINDING-01 closes the missing CRT1 binding-slot contradiction. Exactly eleven CRT1 lines remain. This is not full A1 completion or a new production grant.

DONE: new successor documents preserve all predecessor files; parser and the existing ArchTests golden proof are updated. No second proof owner, SQL/DDL/migration/project-reference change, stage, commit or push in this amendment.

MEASURED: scoped ArchTests 10/10; full ArchTests 149/151 (C527 and R2A4 fail); solution build succeeds with 9 EF1002 warnings and 0 errors. R27b HTTP 400/403/503 plus zero drains and zero A3 calls remains NOT_MEASURED. Parser zero-body-read is measured, not a substitute for that HTTP proof.

## Successor catalogue

All files below are siblings of this report. Full 64-character SHA comparisons were used.

| Artifact | SHA-256 | Lines | Bytes |
| --- | --- | ---: | ---: |
| [tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch_r27_metadata_v1.md](tip_88c1_c6b_capture_runtime_identity_enrollment_technical_implementation_dispatch_r27_metadata_v1.md) | `661F67153E06734483C809C3667577107F7E0B49A70BF6E5EA45C17B8F5E2062` | 1038 | 63515 |
| [tip_88c1_c6b_a1_crt1_exact_signed_bytes_contract_rri_r27_metadata_v1.md](tip_88c1_c6b_a1_crt1_exact_signed_bytes_contract_rri_r27_metadata_v1.md) | `6131ABABCAE0415AA899D6997E9A4375398CCAF0AD743F64E16517625D97A61A` | 112 | 5345 |
| [tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md](tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md) | `85C8EC8F63758F31E3290E657529719E42DED478A299823E1852C21CC12850B5` | 1386 | 123774 |
| [tip_88c1_c6b_a1_transition_sql_01_root_enrollment_lifecycle_r27_metadata_v1.md](tip_88c1_c6b_a1_transition_sql_01_root_enrollment_lifecycle_r27_metadata_v1.md) | `2A407BEBA86C10BB4CEEA1ABE718E2F31A8CB615F747569DF4A3A520D48CBAD4` | 264 | 33981 |
| [tip_88c1_c6b_a1_transition_sql_02_rotation_catalog_r27_metadata_v1.md](tip_88c1_c6b_a1_transition_sql_02_rotation_catalog_r27_metadata_v1.md) | `EB77818CAE3A68B7867C47212F9415278313F9B7013A194C2A7FB5803A5819E5` | 314 | 35895 |
| [tip_88c1_c6b_a1_transition_sql_03_capability_binding_readiness_r27_metadata_v1.md](tip_88c1_c6b_a1_transition_sql_03_capability_binding_readiness_r27_metadata_v1.md) | `F421620A9D8F35A80F97B7DA5765C953B9B5549A671DB9C6E0B96C1BD3416364` | 708 | 64166 |
| [tip_88c1_c6b_a1_transition_sql_04_auth_nonce_credential_revoke_r27_metadata_v1.md](tip_88c1_c6b_a1_transition_sql_04_auth_nonce_credential_revoke_r27_metadata_v1.md) | `CE309D5BDEBAF23E144B446BD484B5B19ACC5088027D443D3472BDB1AF4E7FCB` | 312 | 23748 |
| [tip_88c1_c6b_a1_server_identity_control_dispatch_r27_metadata_v1.md](tip_88c1_c6b_a1_server_identity_control_dispatch_r27_metadata_v1.md) | `D2221D016A0900677778B95F29257EFE6450C97D6D1057D335E44A9B87111B33` | 909 | 75796 |

## Product/proof files

| Path | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| src/TagEkyc.Api/CaptureRuntimeEndpoints.cs | 3E985A93C768848DCB96B045B337A88CB329FB97F0752BD51E9C1CFD6BF4B1C5 | 9FC6D4E661219E16E8A9C2E98F89C44317C23DB97085ECD0BB84A382B2A1930E |
| tests/TagEkyc.ArchTests/Tip88C1C6BA1ArchitectureTests.cs | C3E36CAC98572189B96747FD69505532B3C53A14C4F03F9DB4D5756C98F705AE | 59DFF90AEC7029F2AFA43D046C4210804E8A4DB2763BFD91643C7F1845B3BC42 |

## Closed set and golden vector

SPECIFIED: exactly 10 metadata headers. Ordered names and literal values follow. The three rejected identity selectors are not members. Media type, content length and body commitment remain separate CRT1 atoms.

Canonical rule: validate the entire set before hashing. For each member in the written fixed order emit exact HeaderName, one ASCII '=', exact received canonical value, then one LF, including the last member. UTF-8 without BOM; no trimming, case folding, timestamp reserialization or runtime sorting. HTTP header-name lookup is case-insensitive but emitted names are the fixed spellings below. Missing, empty, duplicate or extra metadata rejects before computing a digest. Ordinary HTTP transport headers are not extra metadata.

Positive Int64 canonical decimal: configuration revision and retention budget. Positive Int32 canonical decimal: capture revision. No leading zero/sign. Session/artifact/idempotency UUIDs: lowercase N, version 4 and RFC variant. RawClass: ChipDg2Portrait or SelfieImage. Metadata timestamps: exact UTC DateTimeOffset O form yyyy-MM-dd'T'HH:mm:ss.fffffff+00:00. The CRT1 timestamp separately retains its existing Z form.

Visible-separator preimage (each displayed \\n is one byte LF, not two literal characters):

```text
X-TagEkyc-Agent-Configuration-Revision=7\n
X-TagEkyc-Verification-Session-Id=11111111111141118111111111111111\n
X-TagEkyc-Capture-Artifact-Id=22222222222242228222222222222222\n
X-TagEkyc-Capture-Revision=3\n
X-TagEkyc-Raw-Class=ChipDg2Portrait\n
Idempotency-Key=33333333333343338333333333333333\n
X-TagEkyc-Captured-At-Utc=2026-09-10T01:02:00.0000000+00:00\n
X-TagEkyc-Retention-Started-At-Utc=2026-09-10T01:02:00.0000000+00:00\n
X-TagEkyc-Retention-Expires-At-Utc=2026-09-10T01:03:00.0000000+00:00\n
X-TagEkyc-Retention-Budget-Seconds=60\n
```

MEASURED: 521 UTF-8 bytes. Independently reconstructed with SHA-256 by the main agent and V1 reviewer, not inferred from the tested helper.

```text
70d40b8925e302727a705dd0c5d2069164056d64857f0cc5b229a47c0c6c6c2d
```

Complete eleven-line CRT1 preimage (one final LF after the last line):

```text
TAG-EKYC-CRT1
POST
/api/ekyc/raw-export/source-ingress
00112233445546778899aabbccddeeff
7
2026-09-10T01:02:03.4567890Z
AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8
image/jpeg
17
aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
ingress=IngressMetadataSha256=70d40b8925e302727a705dd0c5d2069164056d64857f0cc5b229a47c0c6c6c2d
```

## Mutation matrix — MEASURED

Production cryptographic Verify is invoked with real P-256 ECDSA and a successful baseline using the same verifier/thumbprint/signature as negatives. This is not a full AuthenticateAsync/database/HTTP run.

| Case | Result |
| --- | --- |
| Change only X-TagEkyc-Agent-Configuration-Revision | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Verification-Session-Id | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Capture-Artifact-Id | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Capture-Revision | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Raw-Class | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only Idempotency-Key | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Captured-At-Utc | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Retention-Started-At-Utc | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Retention-Expires-At-Utc | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Change only X-TagEkyc-Retention-Budget-Seconds | PASS: digest differs from written literal; preimage changes; baseline signature rejected; body reads 0 |
| Swap first two canonical metadata members | PASS: wrong digest differs from written literal; binding/parser and baseline signature reject |
| Reorder HTTP header arrival only | PASS: digest remains written literal (fixed canonical order) |
| Method / path / timestamp / nonce / media type / content length / body commitment / binding | PASS: 8/8 signed-atom mutations reject baseline signature; path/binding reject at parser, other six also traverse parser |
| Final LF: exactly one / absent / CRLF / extra LF | PASS: baseline verifies; all three byte mutations reject |
| Missing / empty / duplicate for every member | PASS: 30 cases reject with empty digest and zero body reads |
| Extra X-TagEkyc-Unratified-Metadata | PASS: rejected; empty digest; zero body reads |
| Empty binding on raw ingress | PASS: parser rejects; zero body reads |
| ingress= binding on non-ingress route | PASS: parser rejects; zero body reads |
| Parsed preimage alias isolation | PASS: independent backing arrays; caller mutations do not alter frozen preimage |
| Full HTTP R27b mapping 400/403/503, zero drains, zero A3 calls | NOT_MEASURED / outstanding Stage 2 proof; no HTTP fixture was run |

## Reproduction and review

MEASURED commands:

- dotnet build TagEkyc.sln --no-restore: PASS, 0 errors, 9 EF1002 warnings.
- dotnet test tests/TagEkyc.ArchTests/TagEkyc.ArchTests.csproj --no-restore --filter FullyQualifiedName~Tip88C1C6BA1ArchitectureTests: 10/10 PASS, no skips.
- Full ArchTests --no-build: 151 executed, 149 passed, 2 failed, no skips. Failure identities: C527_model_snapshot_tripwires_are_platform_independent_and_lockstep; R2A4_no_production_raw_source_registration_or_runtime_activation_is_added. These remain unresolved historical guards, not a full-suite PASS.

TRX files: TestResults/r27-metadata/r27-metadata-arch-scoped.trx and r27-metadata-arch-full.trx.

PI-TAG-001 V1 bounded semantic review and V3 free-adversarial review: PASS, 0 actionable findings in this amendment scope. V3 verified 22/22 parent/OM consumed-source full SHA matches; T1-T4 fenced SQL blocks unchanged (5/5, 2/2, 4/4, 3/3). Risks checked: hidden SQL churn, stale active provenance, and confusing parser/crypto PASS with full HTTP authorization. Historical predecessor/checkpoint hashes are lineage, not silently asserted as current consumed bytes. Existing umbrella historical body is explicitly non-operative where superseded.

No PostgreSQL fixture was started or used; no PostgreSQL result from this turn is ratification evidence. Prior runs are not re-ratified here. No cloud synchronization was performed.

## Remaining work and boundaries

No new authority contradiction was found. R27's missing binding-slot STOP is closed. The HTTP denial/A3 proof remains an implementation obligation, not another request for a semantic decision. This report does not assert every amendment proof obligation completed, Stage 2 complete, or A1 complete.

HEAD: c5d9dc0b5ef9b692d0bb18176580a2269a9e24a9. Staged paths: 0. Conflicted paths: 0. Predecessor files remain present and unchanged. Unrelated dirty/untracked work preserved. No SQL/DDL/migration, pepper, ENROLL1, A1/A3 boundary, .csproj or project-graph mutation by this amendment. No stage/commit/push. Production and real retained-biometric activation remain prohibited.

