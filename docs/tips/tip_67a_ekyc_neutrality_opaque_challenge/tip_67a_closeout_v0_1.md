# TIP-67A eKYC Neutrality — Opaque-Challenge Profile — Closeout v0.1

**File:** `docs/tips/tip_67a_ekyc_neutrality_opaque_challenge/tip_67a_closeout_v0_1.md`
**Version:** 0.1
**Status:** Closed — architecture course-correction (shipped-behavior refactor) accepted on Contractor adversarial spot-check (code-verified); ready for Codex commit. Includes one DISCLOSED carried TIP-66 precision fix (scope exception).
**Date:** 2026-06-22
**Baseline:** `e8cf836 docs: finalize TIP-66 closeout v0.2 + README v1.18 (post-commit)` (master).
**Decision:** `docs/adr/ekyc_neutrality_decision_v0_1.md`. First of the 2-TIP split (67A neutralize → 67B proof/view).
**Purpose:** Close TIP-67A after removing SignFlow's "transaction" concept from the eKYC core (neutral opaque-challenge profile) and freezing the challenge/profile contract that is TIP-67B's input.

## Changelog
### v0.1 — Initial closeout
- Closed TIP-67A. Recorded outcome vs intent, decision/branch disposition, the dedicated Scope-Exception/carried-TIP-66-fix section, debt, the spot-check (5 spec-review rounds + code-verified build spot-check), the two-group file list, validation, the playbook review ladder, GDrive posture, lessons, next step.

## Status / Disposition
```text
READY_TO_COMMIT_TIP_67A_WITH_DISCLOSED_TIP66_PRECISION_FIX; opaque-challenge/profile contract FROZEN (67B input); eKYC core has NO transaction concept
```

## Outcome vs Intent
| Intended outcome | Actual result | Status |
| --- | --- | --- |
| Remove "transaction" from the eKYC core | `TRANSACTION_BOUND_EKYC_PROFILE` → `CHALLENGE_BOUND_EKYC_PROFILE`; policy flag + error codes neutral; core leftover scan = zero transaction interpretation | Accepted |
| Challenge is a truly opaque string | `BindingNonceHash`(HashRef) → `Challenge`(string); no `sha256:` requirement; ≤128; reject C0/C1; no trim/normalize; exact echo; non-hash test passes | Accepted |
| Wire format pinned, no global blast radius | Profile-ONLY `[JsonConverter]` emits `CHALLENGE_BOUND_EKYC_PROFILE`; global `JsonStringEnumConverter` unchanged (other enums keep PascalCase, proven by test) | Accepted |
| Compat: wire + persisted row + request field keys | Legacy wire/`TransactionBoundEkycProfile` row tolerant-parsed; custom request converter accepts old field keys; `CONFLICTING_CHALLENGE_FIELDS` on divergence | Accepted |
| Echo on exact surfaces, not in hash | Echoed on Create/Summary/Complete response DTOs; absent from manifest/hash; CompletedEvent notification-only | Accepted |
| No hash/signing change | Hash chain untouched; golden vectors byte-identical. ONE disclosed signing fix (signedAt) — see Scope Exception | Accepted with exception |
| Docs neutral | `signflow_integration_contract` + `lld_01` + `lld_03` reframed; drift marked resolved | Accepted |

## Decision / Branch Disposition
| Decision / option | Final disposition | Why | Follow-up |
| --- | --- | --- | --- |
| Neutral opaque challenge (vs keep transaction-bound) | Accepted | ADR: eKYC neutral, client binds | 67B signs/exposes the proof |
| Profile-ONLY wire converter (vs global naming policy) | Accepted | Global change would mutate every enum's wire | — |
| Keep EF columns (vs column-rename migration) | Accepted | Avoid persisted-data risk; mapper aliases | Optional column rename = hygiene debt |
| signedAt microsecond truncation in the signer | Accepted as a DISCLOSED scope exception | Required for Postgres-roundtrip verify; minimal; no hash impact | Recorded below + lld_01 note |
| Rename `Tip08TransactionBound...` test file | Deferred | Test-only naming; content already challenge-bound | Hygiene rename later |

## Scope Exception / Carried TIP-66 Fix
This section deliberately separates the carried fix from 67A's main scope (per Homeowner governance; 67A's STOP/RRI says "no signing change").
- **TIP-67A main scope:** neutral challenge/profile refactor (no signing/hash/verification-surface change).
- **Carried technical fix:** TIP-66 `signedAt` precision alignment.
- **Reason:** PostgreSQL `timestamptz` stores/roundtrips microsecond precision; the JWS proof previously signed `DateTimeOffset` at finer (100ns) precision, causing a persisted-readback verification mismatch.
- **Change:** truncate `signedAt` to microsecond precision BEFORE building the signed claim/envelope (`LocalDevEs256JwsEvidenceSigner.TruncateToMicroseconds`); the same value feeds the JWS claim, the envelope, and persistence; the pinned `...fffffffZ` format is preserved (7 digits, trailing 0).
- **Impact:** JWS payload / envelope / persisted row align; **no hash-graph change**; golden `manifestBodyHash`/`packageHash`/`manifestHash` unchanged.
- **Classification:** accepted out-of-scope precision fix — NOT a new signing feature, NOT 67B proof/view work, NOT a re-architecture (still ES256/JWS/`IEvidenceSigner`). Disclosed by the builder, not hidden.

## Debt / Gap Final State
| Debt/gap | Final state | Resolved? | Next gate |
| --- | --- | --- | --- |
| eKYC neutrality drift (SignFlow coupling) | Core neutralized | **Yes for 67A** | Neutral verifiable proof = TIP-67B |
| Profile naming/policy mapping | TRANSACTION_BOUND → CHALLENGE_BOUND | **Yes** (debt-registry row superseded) | — |
| DB columns `ExternalTransactionId`/`BindingNonceHash` retained | Hold the old physical names, store neutral values | Deferred (cosmetic) | Optional column-rename migration (hygiene) |
| `Tip08TransactionBound...` test file name | Content challenge-bound, name stale | Deferred (cosmetic) | Rename in a hygiene pass |
| Neutral verifiable proof (JWS/view/public key) | Not in 67A | No (by design) | **TIP-67B** (now unblocked) |

## Contractor Adversarial Spot-Check (code-verified, not the report)
A parallel adversarial sub-agent read the code + I verified the crux. Verdict: **CLEAN.** Confirmed: profile-ONLY converter (global converter unchanged — proven by `ProjectionBoundaryTests` asserting `VerificationSessionStateDto.Created` → `"Created"`); wire snake-upper vs DB-row PascalCase; legacy-row tolerant-parse (a `switch`, not `Enum.Parse`); Challenge opaque (HashRef→string, no sha256, ≤128, C0/C1 reject, no normalize, exact echo, non-hash test); custom request converter + `CONFLICTING_CHALLENGE_FIELDS` conflict rule (not `[JsonPropertyName]` multi-name); echo on the 3 response DTOs only (absent from manifest); policy/error-codes renamed (old symbols grep-empty in src); zero surviving transaction interpretation in domain/application (only documented input-compat aliases + retained DB column names). The completion-service diff is 4 lines (2 echo fields + 1 profile-name `ToDto` rename); the signer diff is exactly the disclosed `signedAt` fix. Golden vectors byte-identical (verified literals `e8aa856e…`/`0a3cbc9b…`/`124c36f8…`).

## Exact Files Changed (two groups — to confirm against `git status` at commit)
**Group A — TIP-67A (profile/challenge/DTO/policy/docs/tests):**
- `src/TagEkyc.Contracts/Common/ContractEnums.cs` (profile rename + profile-only `[JsonConverter]` + legacy parse)
- `src/TagEkyc.Domain/VerificationProfile.cs`, `VerificationSession.cs` (`Challenge`/`ClientReference`)
- `src/TagEkyc.Application/VerificationSessions/VerificationSessionApplicationService.cs` (validation + neutral error codes + echo), `LocalDevClientPolicy.cs`, `src/TagEkyc.Application/LocalDev/LocalDevRuntimePolicySource.cs` (`AllowsChallengeBoundProfile`)
- `src/TagEkyc.Contracts/BusinessConsumer/BusinessConsumerContracts.cs` (request converter + echo on Create/Summary/Complete DTOs)
- `src/TagEkyc.Infrastructure/Persistence/DomainRowMapper.cs` (Challenge↔column mapping + tolerant profile parse)
- `src/TagEkyc.Api/Program.cs` (profile converter registration; global converter unchanged)
- tests: `Tip04SessionApplicationTests.cs`, `ProjectionBoundaryTests.cs`, `PostgresPersistenceSliceTests.cs`, `Tip08TransactionBoundE2eProofTests.cs` (+ any others)
- docs: `docs/signflow_integration_contract_v0_1.md`, `docs/lld_01_data_model_v0_1.md`, `docs/lld_03_api_contracts_v0_1.md`, `docs/phase1_scope_and_debt_registry_v0_1.md`, `docs/adr/ekyc_neutrality_decision_v0_1.md`, `docs/tips/tip_67a_*`, `docs/tips/tip_67b_planning_brief_v0_1.md`, `docs/tips/README.md`

**Group B — Carried TIP-66 precision fix (DISCLOSED scope exception):**
- `src/TagEkyc.Infrastructure/Signing/LocalDevEs256JwsEvidenceSigner.cs` (`TruncateToMicroseconds`)
- the related signer/readback test (if any)
- the `lld_01` `signedAt` microsecond note

Excluded (known-dirty): `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `docs/00_GDRIVE_FILE_INDEX.md`, all `bin/obj`.

## Validation
- `dotnet build TagEkyc.sln`: 0 warnings / 0 errors.
- `dotnet test TagEkyc.sln`: 171/171 (Contract 13, Arch 36, Unit 106, Integration 16).
- **TIP-65 golden vectors UNCHANGED** — `manifestBodyHash`/`packageHash`/`manifestHash` byte-identical (no hash drift).
- **Postgres readback verify passes after microsecond normalization** (the carried fix's purpose).
- No new public verification surface, no signing-architecture change.

## Review Ladder Summary
Model: Contractor drafts → GPT/Codex review/converge → Codex builds → Contractor adversarial spot-check.
- **V1 (spec review — brief/kickoff):** 5 external review rounds → v0.6 READY. Files/surfaces: the 67A docs vs the as-built profile/session/policy/serializer/error-code surfaces.
- **V1 findings (material each round):** challenge-opacity (sha256 removed); echo surfaces pinned; persisted-row compat; profile-ONLY converter (vs global blast radius); policy flag + error-code rename; `Challenge` length/string-safety; request field-key alias mechanism (STJ can't multi-name) + conflict rule; wire-vs-persisted canonical forms. All applied.
- **V2 (build spot-check, on CODE):** ACCEPT — sub-agent + Contractor verified the neutralization clean; golden byte-identical.
- **V2 findings:** one DISCLOSED scope exception (signedAt microsecond) — accepted + recorded.
- **V3 free review:** governance handling of the scope exception (this closeout's dedicated section + lld_01 note + two-group file list).
- **Zero-finding justification:** N/A (every round produced material findings — expected for a shipped-behavior refactor with a broad blast radius).
- **Total rounds:** 5 spec + 1 code spot-check + governance pass. **Non-convergence:** No — findings shrank design → scope → mechanism → governance.
- **Lessons:** see below.

## GDrive Sync / Hash Posture
Docs changed (`adr`, `lld_01`/`lld_03`, `signflow_integration_contract`, debt registry, the TIP-67A/67B folders, README). No live GDrive sync/hash evidence is available in repo evidence; `docs/00_GDRIVE_FILE_INDEX.md` remains an excluded known-dirty file. No doc hashes were produced/pushed in this slice; this closeout makes no GDrive sync claim.

## STOP/RRI Result
The 67A contract's STOP "no signing change" was technically hit by the carried TIP-66 `signedAt` fix. Rather than silently absorb it, it is handled as a DISCLOSED scope exception (dedicated section above) — accepted by the Homeowner because it is minimal, hash-neutral, and required for persisted-readback verification. No other STOP condition encountered.

## Lessons Learned
- A shipped-behavior refactor's blast radius is wider than the visible DTO: 5 review rounds surfaced serializer (global-converter blast radius), policy flag, error codes, persisted-row + request-field-key compat, and STJ mechanism limits — none visible in the first framing. For such refactors, budget multiple code-grounded review rounds and front-load a full surface inventory.
- Paper review hit diminishing returns at round 5 (mechanism details); the build + tests were the better forcing function for the remaining implementation specifics — which the adversarial code spot-check then verified.
- A disclosed out-of-scope fix is handled by classification + a dedicated record, not by pretending it was in scope — preserves both the fix and the scope discipline.

## Recommended Next Step
Codex commits TIP-67A by the two-group allowlist (exclude known-dirty) — the closeout/`lld_01` explicitly record the carried TIP-66 precision fix. Then the opaque-challenge/profile contract is FROZEN, **unblocking TIP-67B** (neutral verifiable proof + view): finalize the 67B field-exact kickoff against 67A's frozen names → review → build.
