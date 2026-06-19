# TIP-43 Artifact Retention / Expiry Policy Planning v0.1

**File:** `docs/tips/tip_43_artifact_retention_expiry_policy_planning/tip_43_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - docs-only / provider-neutral / `ART-004` artifact retention / expiry policy planning
**Date:** 2026-06-19
**Baseline:** `1aa16db31676fcbb9a36a16cae171debdb4733b4 docs: close TIP-42 evidence package completeness planning`
**Purpose:** Define provider-neutral artifact retention and expiry policy requirements before any artifact, reference, or package object can be treated as retained, unexpired, reviewable, or usable for a later reviewed evidence purpose.

## Changelog

### v0.1 - Initial artifact retention / expiry policy planning draft

- Opened TIP-43 as docs-only provider-neutral `ART-004` artifact retention / expiry policy planning.
- Used TIP-38 raw payload policy planning, TIP-39 storage-boundary planning, TIP-40 reference-resolution planning, TIP-41 orphan handling planning, and TIP-42 package completeness planning as source constraints.
- Defined retention terms, default retention posture, retention class matrix, expiry state model, required retention/expiry packet/checklist, related gate dependencies, and STOP/RRI conditions.
- Covered retention classes, expiry semantics, environment separation, evidence review windows, dispute/review behavior, and expired-reference behavior.
- Recorded that TIP-43 does not claim operational retention capability, does not select a store/provider/tool, and does not authorize raw payload collection or persistence.
- Preserved no runtime/source/test/schema/API/package changes, no provider naming/comparison/selection, no provider-specific evidence collection, and no readiness/legal/audit/production/pilot/certification claims.

## 1. Status / Purpose / Non-Authorization

TIP-43 is docs-only, provider-neutral, and `ART-004`-focused artifact retention / expiry policy planning.

TIP-43 defines policy requirements for classifying artifact and artifact-reference retention, expiry, review windows, dispute/review holds, environment separation, and expired-reference behavior before any later reviewed scope may treat an artifact, reference, or package object as retained, unexpired, or reviewable for a declared evidence use.

TIP-43 depends on:

- TIP-38 `ART-009` raw payload policy planning for raw-payload default deny;
- TIP-39 `ART-001` storage-boundary planning for persistence boundaries;
- TIP-40 `ART-002` reference-resolution planning for reference non-proof posture;
- TIP-41 `ART-008` orphan handling planning for orphan/non-success behavior;
- TIP-42 `ART-003` package completeness planning for package object state and packet requirements.

TIP-43 explicitly does not authorize:

- operational retention capability;
- retention engine implementation;
- expiry engine implementation;
- scheduler, deletion, archive, lifecycle, timer, queue, worker, background service, or runtime workflow implementation;
- artifact store implementation;
- resolver implementation;
- purge/disposal implementation;
- legal-hold sync implementation;
- package builder implementation;
- runtime, source, test, project, package, schema, migration, API, DTO, status, error, index, storage, or provider changes;
- provider-specific evidence collection;
- raw payload collection;
- raw payload persistence;
- provider naming, comparison, scoring, shortlisting, recommendation, selection, acceptance, or rejection;
- store, storage provider, resolver, tool, package, runtime, schema, API, database, vault, object-store, blob-store, or adapter selection;
- durable metadata fields that contain raw artifact bytes, biometric bytes, provider payload bytes, vault bytes, secrets, credentials, tokens, private keys, API keys, or retrieval-bearing secrets;
- readiness, legal, audit, external-audit, production, pilot, certification, capability, support, provider readiness, artifact readiness, retention readiness, evidence availability, package completeness proof, or implementation readiness claims.

## 0. Repo Evidence

Read-only repo evidence used for TIP-43:

| Evidence | Current finding |
| --- | --- |
| Repository root | `D:/Task/Remote Signing/TagEkyc` |
| Current branch | `master` |
| Current HEAD before TIP-43 | `1aa16db31676fcbb9a36a16cae171debdb4733b4 docs: close TIP-42 evidence package completeness planning` |
| TIP-35 closeout commit | `d01c2d8b6443b4b1ecdad373aed8e74e9e4f4a0a docs: close TIP-35 branch and artifact debt registration` |
| TIP-36 closeout commit | `f1b1b80497480d2170e235e6f277faa12b3bdc94 docs: close TIP-36 analytical summary governance` |
| TIP-38 closeout commit | `c91df031ca26a0c875f8d2da947bb49a71a11c73 docs: close TIP-38 provider raw payload policy planning` |
| TIP-39 closeout commit | `b5a69233c6efb70ef1b132eb0ccac08399e3e3d6 docs: close TIP-39 artifact raw evidence storage boundary planning` |
| TIP-40 closeout commit | `03b192dbf993caf18616afe8fa84055ee4ca5607 docs: close TIP-40 durable metadata reference resolution planning` |
| TIP-41 closeout commit | `c0246450b474b9a3c1a8a8738339d40af4b268cd docs: close TIP-41 metadata artifact orphan handling planning` |
| TIP-42 closeout commit | `1aa16db31676fcbb9a36a16cae171debdb4733b4 docs: close TIP-42 evidence package completeness planning` |
| TIP-42 accepted baseline | `ART-003` narrowed/accepted at evidence package object completeness planning level only; any future package completeness claim requires a reviewed package completeness packet. |
| Current known dirty out-of-scope files | `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md` |
| Intended changed files only | `docs/tips/README.md`, `docs/tips/tip_43_artifact_retention_expiry_policy_planning/tip_43_planning_brief_v0_1.md` |

## 2. TIP Analytical Summary / Intent Ledger

### Intent

Define provider-neutral retention and expiry policy requirements for `ART-004` before an artifact, artifact reference, package object, manifest entry, evidence summary, or review record can be treated as retained, unexpired, reviewable, or usable for a specific later evidence purpose.

### Expected Outcome

After TIP-43, later reviewed scopes will have retention class definitions, expiry semantics, environment separation rules, evidence review window requirements, dispute/review behavior, expired-reference behavior, packet/checklist requirements, and STOP/RRI boundaries for retention and expiry decisions.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Focus on `ART-004`. | TIP-35 registered retention/expiry as unresolved, and TIP-42 requires retention/expiry classification before package completeness can be claimed. | TIP-43 defines retention/expiry planning requirements only. | Does not implement retention, expiry, deletion, archive, or lifecycle mechanics. |
| Require retention class before evidence use. | An artifact or reference cannot be relied on without knowing its expected retention class and expiry semantics. | Later scopes must classify each object/reference before use. | No operational retention capability. |
| Treat unknown or expired state as non-success by default. | Unknown/expired objects can corrupt reference resolution, orphan handling, and package completeness. | Later scopes must record non-success or explicit reviewed exception. | No evidence availability proof. |
| Separate environments. | Retention policy for local, test, review, staging-like, and production-like contexts must not be inferred across environments. | Later scopes must declare environment boundary for each retention class. | No production readiness or environment enforcement claim. |
| Require review/dispute window behavior. | Evidence may need review or dispute handling before expiry, but this must be bounded and reviewed. | Later scopes must define review window start/end, extension, blocker, and release behavior. | No legal, audit, or operational reliance claim. |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Retention engine implementation now. | Rejected. | TIP-43 is docs-only planning and selects no runtime, scheduler, store, schema, API, package, or provider. | Later reviewed implementation TIP required. |
| Expiry/delete/archive workflow now. | Rejected. | TIP-43 defines expiry semantics only; `ART-005` purge/disposal remains separate. | TIP-44 or later reviewed implementation scope required. |
| Store/provider/tool selection now. | Rejected. | Retention requirements do not choose a storage surface, provider, database, package, resolver, or lifecycle tool. | Later reviewed decision TIP required. |
| Treat unknown retention as acceptable evidence. | Rejected. | Retention class and expiry state are required before evidence use or package completeness claims. | Later retention/expiry packet required. |
| Raw payload collection or persistence now. | Rejected. | TIP-38 and TIP-39 preserve default deny for raw payload collection/persistence. | Later reviewed scope must explicitly approve any exception. |
| Provider-specific evidence collection now. | Rejected. | TIP-43 is provider-neutral and no evidence authorization packet exists. | Later reviewed evidence authorization packet required. |
| Resolving `ART-001`, `ART-002`, `ART-003`, `ART-005` through `ART-009`, or `GOV-001` now. | Deferred except dependency requirements. | TIP-43 can require those gates but supplies no implementation or reviewed operational evidence for them. | Later reviewed TIPs must resolve or carry each gate. |

### Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| `ART-004` Artifact retention / expiry policy unresolved | Primary target. | Retention classes, expiry states, evidence review windows, dispute/review behavior, expired-reference behavior, and packet requirements are defined at planning level only. | Later reviewed scope required before any retention/expiry capability or evidence reliance claim. |
| `ART-001` Artifact/raw evidence storage boundary | Required dependency from TIP-39. | Remains storage-boundary planning only. | Retention classes do not authorize artifact/raw evidence persistence. |
| `ART-002` Durable metadata reference resolution | Required dependency from TIP-40. | Remains reference-resolution planning only. | References still require reviewed resolution packet before evidence/package use. |
| `ART-003` Evidence package object completeness | Required dependency from TIP-42. | Remains package completeness planning only. | Package completeness packet must include retention/expiry status. |
| `ART-005` Artifact purge / disposal workflow unresolved | Downstream dependency. | Unresolved. | Expiry does not equal purge/disposal execution. |
| `ART-006` Artifact legal hold sync unresolved | Related dependency. | Unresolved. | Dispute/review hold behavior is planning only and does not implement legal-hold sync. |
| `ART-007` Artifact access/audit/security unresolved | Related dependency. | Unresolved. | Retention review/access events require later access/audit/security work. |
| `ART-008` Metadata-artifact orphan handling | Required dependency from TIP-41. | Remains orphan handling planning only. | Expired or inaccessible references may create orphan-risk/non-success state. |
| `ART-009` Provider raw payload policy | Required raw-payload boundary. | Remains policy-planning only. | Raw payload collection and provider-specific evidence collection remain blocked. |
| `GOV-001` Branch/deferred-scope debt traceability incomplete | Carried. | Unresolved. | Later relevant work must visibly carry or resolve it. |

### Non-Claims

TIP-43 makes no claim of operational retention capability, expiry capability, deletion capability, archive capability, purge capability, legal-hold capability, access/audit capability, artifact availability, reference resolution capability, orphan handling capability, package completeness proof, storage readiness, raw payload handling readiness, provider readiness, provider suitability, legal reliance, audit reliance, external-audit reliance, production readiness, pilot readiness, certification readiness, operational readiness, implementation readiness, runtime enforcement, support, or capability.

TIP-43 does not claim that `ART-004` is fully resolved. It defines provider-neutral retention/expiry policy requirements at planning level only.

### Dispatch Readiness

TIP-43 is not an implementation TIP.

Implementation dispatch = NO.

No runtime, source, test, project, package, schema, migration, index, API, DTO, status, error, resolver, storage, vault, object/blob store, database, provider, adapter, retention engine, expiry engine, scheduler, worker, archive flow, deletion flow, or SignFlow surface may change under TIP-43.

## 3. Source Map

| Source | Anchor used by TIP-43 |
| --- | --- |
| `docs/tips/README.md` | TIP index v0.73 records TIP-42 closeout and preserves `ART-004` retention/expiry as unresolved or planning-level only unless separately closed by later reviewed TIPs. |
| `docs/tips/tip_35_s2_branch_debt_traceability_artifact_gap_registration/tip_35_planning_brief_v0_1.md` | Registers `ART-004` as artifact retention / expiry policy unresolved and requires retention class, expiry, review period, dispute hold, and environment separation. |
| `docs/tips/tip_35_s2_branch_debt_traceability_artifact_gap_registration/tip_35_closeout_v0_1.md` | Closes TIP-35 with `GOV-001` and `ART-001` through `ART-009` registered but unresolved. |
| `docs/tips/tip_36_tip_analytical_summary_intent_ledger_governance/tip_36_closeout_v0_1.md` | Requires TIP Analytical Summary / Intent Ledger and carry-forward of unresolved GOV/ART gates. |
| `docs/tips/tip_38_provider_raw_payload_policy_planning/tip_38_closeout_v0_1.md` | Accepts `ART-009` at policy-planning level only and denies provider-specific evidence collection, raw payload collection, and raw payload persistence. |
| `docs/tips/tip_39_artifact_raw_evidence_storage_boundary_planning/tip_39_closeout_v0_1.md` | Accepts `ART-001` at storage-boundary planning level only and does not authorize artifact/raw evidence persistence. |
| `docs/tips/tip_40_durable_metadata_reference_resolution_planning/tip_40_closeout_v0_1.md` | Accepts `ART-002` at reference-resolution planning level only and preserves that metadata references are not evidence availability proof. |
| `docs/tips/tip_41_metadata_artifact_orphan_handling_planning/tip_41_closeout_v0_1.md` | Accepts `ART-008` at orphan handling planning level only and treats expired, inaccessible, or orphan-risk references as non-success. |
| `docs/tips/tip_42_evidence_package_object_completeness_planning/tip_42_closeout_v0_1.md` | Accepts `ART-003` at package object completeness planning level only and requires retention/expiry status before package completeness claims. |
| `docs/00_REVIEW_AND_TIP_PLAYBOOK.md` | `L-TAG-Gov-01` requires intent ledger, branch/deferred disposition, non-claims, and STOP/RRI handling. |

## 4. Definitions

| Term | Provider-neutral definition |
| --- | --- |
| Retention class | A provider-neutral policy category that describes how long a metadata-safe artifact object, reference, summary, review record, or package object is expected to remain available for a declared evidence use. |
| Expiry | A policy state where an artifact, reference, access path, review window, or package object is beyond its allowed retention or review period and must not be treated as available evidence unless a later reviewed exception explicitly permits non-success handling. |
| Review window | A bounded time period during which a retained object may be reviewed inside an accepted review boundary before expiry or release. |
| Dispute/review hold | A reviewed planning state that pauses normal expiry treatment for a declared dispute or review purpose without authorizing raw payload collection, persistence, legal reliance, or runtime legal-hold sync. |
| Environment separation | The rule that retention classes, expiry windows, review windows, and hold behavior must be declared per environment boundary and cannot be inferred across local, test, review, staging-like, or production-like contexts. |
| Expired reference | A metadata reference whose target, access path, review window, authorization, or retention period is expired or cannot be proven unexpired. |
| Retention packet | A later reviewed packet that declares retention class, environment, start/end trigger, expiry semantics, review window, dispute/review behavior, expired-reference behavior, dependency state, reviewer approval, validation, and STOP/RRI resolution. |

## 5. Default Retention / Expiry Posture

Unknown retention class is not acceptable evidence.

Unknown expiry state is not acceptable evidence.

Expired, inaccessible, unauthorized, deleted, purged, orphan-suspected, orphan-confirmed, quarantined, unreviewed, or environment-mismatched references must not be treated as retained, available, or package-complete evidence.

Retention policy must be declared before evidence use, reference reliance, or package completeness claims.

Expiry is a non-success state by default. A later reviewed scope may record non-success, quarantine, dispute/review hold, or explicit out-of-scope disposition, but TIP-43 does not authorize runtime enforcement or evidence reliance.

Environment separation is mandatory. Retention and expiry behavior in one environment must not be treated as proof for another environment.

Raw payload bytes, biometric bytes, source document bytes, provider payload bytes, vault bytes, secrets, credentials, tokens, private keys, API keys, and retrieval-bearing secrets must not be included in retention policy docs, README files, review mirror files, generated indexes, logs, or durable metadata under TIP-43.

No retention engine, expiry engine, purge/disposal workflow, archive workflow, scheduler, worker, timer, queue, resolver, artifact store, package builder, access-control mechanism, audit schema, provider adapter, runtime state machine, or runtime enforcement is implemented or authorized.

## 6. Retention Class Matrix

| Retention class | Policy purpose | Required planning fields | Non-success / blocker |
| --- | --- | --- | --- |
| `TransientReviewOnly` | Short-lived review workflow material that should not be durable evidence. | Environment, review boundary, start trigger, end trigger, non-persistence rule, deletion/disposal dependency, reviewer approval. | Treating transient material as durable evidence or syncing raw/restricted material. |
| `MetadataReferenceOnly` | Durable metadata-safe references, hashes, summaries, or ids without raw bytes. | Reference classification, retention window, expiry semantics, access/auth state, orphan behavior, review window, environment. | Treating reference presence as target availability or package completeness proof. |
| `DerivedEvidenceSummary` | Sanitized or derived evidence summary without raw payload bytes. | Source classification, derivation boundary, retention window, review window, access boundary, expiry behavior, dependency gates. | Summary contains raw payload, secret, provider payload, or unreviewed sensitive detail. |
| `ArtifactObjectCandidate` | Candidate artifact object that may require future storage authorization before retention can be meaningful. | Storage-boundary status, object class, expected retention class, environment, reference state, raw-payload policy, reviewer approval. | Persistence is assumed without accepted storage authorization. |
| `PackageReviewRecord` | Review metadata supporting a package completeness packet. | Package scope, reviewer boundary, retention window, expiry state, dependency gate status, dispute/review behavior. | Record is used as package completeness proof without required object checks. |
| `DisputeReviewHoldCandidate` | Object or reference under reviewed dispute/review hold planning. | Hold reason, scope, start/end trigger, release behavior, dependency gates, reviewer approval. | Hold is used to claim legal readiness, bypass expiry, or bypass purge/disposal rules. |
| `ExpiredNonSuccess` | Object/reference whose retention or review window has expired. | Expiry trigger, non-success disposition, package/reference/orphan impact, reviewer record, STOP/RRI decision. | Expired object is counted as retained, available, or complete. |

## 7. Expiry State Model

| State | Planning definition | Evidence/package posture |
| --- | --- | --- |
| `RetentionUnclassified` | No retention class has been assigned. | Not evidence; cannot support package completeness. |
| `RetentionClassifiedNotReviewed` | Retention class is proposed but not reviewed. | Not evidence; review pending. |
| `RetainedWithinWindow` | Later reviewed packet states the object/reference is inside the declared retention/review window. | May be used only inside that later packet scope; not granted by TIP-43. |
| `ReviewWindowOpen` | Object/reference may be reviewed inside a bounded accepted review window. | Review-only; not package completeness proof by itself. |
| `ReviewWindowClosed` | Review window is closed or unproven open. | Non-success unless later reviewed packet records accepted disposition. |
| `Expired` | Retention, access, authorization, or review window is past allowed period. | Non-success; cannot support evidence availability or package completeness. |
| `ExpiryUnknown` | Expiry cannot be calculated or proven. | Non-success; STOP/RRI before evidence use. |
| `DisputeReviewHoldPending` | Dispute/review hold is requested but not accepted. | Non-success; no bypass of expiry. |
| `DisputeReviewHoldAccepted` | Later reviewed packet accepts a narrow hold for a declared purpose. | Hold scope only; not legal/audit/production readiness and not raw-payload authorization. |
| `EnvironmentMismatch` | Retention state belongs to a different environment boundary. | Non-success for the requested environment. |
| `ExpiredReferenceNonSuccess` | Reference target or access path is expired or unproven unexpired. | Non-success; may create orphan-risk and package incompleteness. |

`RetainedWithinWindow` and `DisputeReviewHoldAccepted` are narrow future packet states. TIP-43 does not grant them, implement them, or claim operational capability.

## 8. Required Retention / Expiry Packet / Checklist

Before a future artifact, reference, package object, or review record may be treated as retained, unexpired, or reviewable, a later reviewed scope must provide a retention/expiry packet containing:

- object/reference/package object category;
- environment boundary;
- retention class;
- retention start trigger;
- retention end trigger;
- expiry semantics;
- review window start/end;
- dispute/review behavior;
- expired-reference behavior;
- access/auth boundary;
- raw-payload exclusion confirmation;
- storage-boundary status from `ART-001`;
- reference-resolution state from `ART-002`;
- package-completeness impact from `ART-003`;
- purge/disposal dependency from `ART-005`;
- legal-hold sync dependency from `ART-006`;
- access/audit/security dependency from `ART-007`;
- orphan disposition from `ART-008`;
- raw-payload policy status from `ART-009`;
- reviewer approval;
- validation summary;
- STOP/RRI resolution.

The packet must state whether the object/reference is `RetentionUnclassified`, `RetentionClassifiedNotReviewed`, `RetainedWithinWindow`, `ReviewWindowOpen`, `ReviewWindowClosed`, `Expired`, `ExpiryUnknown`, `DisputeReviewHoldPending`, `DisputeReviewHoldAccepted`, `EnvironmentMismatch`, or `ExpiredReferenceNonSuccess`.

The packet must identify whether expired or unknown-expiry references are treated as non-success, quarantine, package incompleteness, orphan-risk, dispute/review hold candidate, or STOP/RRI.

The packet must identify related unresolved dependencies from `ART-001` through `ART-009` and `GOV-001`, and it must state whether each dependency is resolved by reviewed evidence, accepted as a blocker, or explicitly out of scope for the proposed retention/expiry use.

TIP-43 defines packet requirements only. It does not approve any packet.

## 9. Relationship to Other GOV/ART Gates

`ART-004` is the primary target of TIP-43, planning level only.

`ART-001` storage boundary remains planning only from TIP-39. TIP-43 does not convert storage-boundary requirements into artifact/raw evidence persistence, artifact store readiness, storage provider selection, retention capability, or implementation readiness.

`ART-002` reference resolution remains planning only from TIP-40. TIP-43 does not convert reference-resolution planning into runtime resolver capability, artifact availability proof, retention proof, or implementation readiness.

`ART-003` package completeness remains planning only from TIP-42. TIP-43 supplies retention/expiry requirements for package packets but does not prove package completeness.

`ART-005` purge/disposal remains unresolved. TIP-43 defines expiry semantics but does not implement deletion, disposal, purge, tombstone, quarantine, retry, or failure handling.

`ART-006` legal hold remains unresolved. TIP-43 defines dispute/review hold planning behavior but does not implement legal-hold sync or claim legal reliance.

`ART-007` access/audit/security remains unresolved. TIP-43 requires access/auth and review event boundaries in a later packet but does not implement or prove access control, audit security, monitoring, or incident response.

`ART-008` orphan handling remains TIP-41 planning only. TIP-43 treats expired, inaccessible, unauthorized, or environment-mismatched references as non-success and potential orphan-risk, but it does not implement orphan handling.

`ART-009` remains TIP-38 policy-planning only. TIP-43 does not convert raw payload policy into raw payload collection approval, persistence approval, provider-specific evidence authorization, runtime enforcement, artifact readiness, provider evidence readiness, or production/legal/audit readiness.

`GOV-001` remains carried. Later relevant work must visibly carry or resolve branch/deferred-scope debt traceability with reviewed evidence.

## 10. STOP/RRI Conditions

STOP/RRI before:

- unknown retention class is treated as retained evidence;
- unknown expiry state is treated as unexpired evidence;
- expired, inaccessible, unauthorized, deleted, purged, orphan-suspected, orphan-confirmed, quarantined, unreviewed, or environment-mismatched object/reference is treated as successful evidence;
- retention in one environment is treated as proof for another environment;
- review window is inferred without a reviewed retention/expiry packet;
- dispute/review hold is used to bypass expiry, purge/disposal, package completeness, reference resolution, orphan handling, raw-payload, access/auth, or reviewer requirements;
- retention or expiry policy is treated as operational runtime capability;
- retention engine, expiry engine, scheduler, worker, archive workflow, purge/disposal workflow, artifact store, resolver, legal-hold sync, access mechanism, audit schema, or provider adapter is implemented;
- provider, storage, resolver, tool, schema, migration, API, package, adapter, runtime, object store, blob store, vault, database, status, error, DTO, or source surface is selected or changed;
- raw payload is collected or persisted;
- provider-specific evidence collection starts;
- `ART-004` is claimed as runtime capability/readiness rather than planning;
- durable metadata stores raw artifact bytes, biometric bytes, source document bytes, provider payload bytes, vault bytes, secrets, credentials, tokens, private keys, API keys, or retrieval-bearing secrets;
- docs, README files, TIP docs, logs, test fixtures, generated indexes, GDrive review mirror files, evidence package docs, or external reviewer packages contain raw payloads, secrets, provider payloads, biometric artifacts, vault bytes, credentials, tokens, private keys, API keys, or restricted retrieval-bearing references;
- `ART-001`, `ART-002`, `ART-003`, `ART-005` through `ART-009`, or `GOV-001` are claimed resolved without reviewed evidence;
- public sharing is proposed for review mirror files without explicit user instruction;
- LocalDev evidence or local temporary files are treated as production evidence.

## 11. README Update

README update requirements for TIP-43:

- Add TIP-43 row to `docs/tips/README.md`.
- Add changelog entry: TIP-43 opened as draft docs-only / provider-neutral / `ART-004` artifact retention / expiry policy planning.
- Record that TIP-43 defines retention classes, expiry semantics, environment separation, evidence review windows, dispute/review behavior, and expired-reference behavior.
- Record that TIP-43 does not claim operational retention capability and does not select a store/provider/tool.
- Record that no implementation, no provider/storage/resolver selection, no raw persistence, no raw payload collection, no provider-specific evidence collection, and no readiness claim is authorized.

## 12. Validation

Recommended docs-only validation:

```powershell
git diff -- docs/tips/README.md docs/tips/tip_43_artifact_retention_expiry_policy_planning/tip_43_planning_brief_v0_1.md
git diff --check
git status --short
git diff --cached --name-only
git diff --cached --check
```

Do not run `dotnet test` unless docs-only scope is violated.

Before commit, stage only:

- `docs/tips/README.md`
- `docs/tips/tip_43_artifact_retention_expiry_policy_planning/tip_43_planning_brief_v0_1.md`

Leave unrelated dirty files unstaged:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
```

## 13. GDrive Review Mirror Reporting Requirement

When Codex creates, edits, commits, or syncs TIP-43 docs files, Codex must report changed/synced docs files with:

- local relative path;
- GDrive fileId;
- GDrive webViewLink;
- sizeBytes;
- sha256;
- state.

The review mirror must be private/restricted unless the user explicitly instructs otherwise.

The review mirror must be allowlist-only for the changed docs files in this TIP.

Do not sync secrets, raw payloads, provider payloads, biometric artifacts, logs with tokens, database dumps, certificates, keys, `.env`, appsettings with secrets, `bin/`, `obj/`, or `.git`.

Remove any generated local mirror index if it is not in commit scope.

This workflow section is review process only and does not modify product behavior.

## 14. Next Action

Submit TIP-43 for homeowner/GPT review.

If accepted, TIP-43 should be closed as `ART-004` artifact retention / expiry policy planning only. Any later retention, expiry, review-window, dispute/review hold, expired-reference, or package completeness use must still require a reviewed retention/expiry packet and must carry or resolve related `ART-001`, `ART-002`, `ART-003`, `ART-005` through `ART-009`, and `GOV-001` dependencies.

No provider selection, provider naming, provider comparison, provider scoring, provider shortlisting, provider recommendation, provider acceptance/rejection, provider-specific evidence collection, raw payload collection, raw payload persistence, retention engine implementation, expiry engine implementation, scheduler implementation, storage provider selection, runtime enforcement, artifact readiness, retention capability, legal/audit reliance, external-audit reliance, production readiness, pilot readiness, certification readiness, or capability claim proceeds from TIP-43.
