# TIP-38 Provider Raw Payload Policy Planning v0.1

**File:** `docs/tips/tip_38_provider_raw_payload_policy_planning/tip_38_planning_brief_v0_1.md`
**Version:** 0.1
**Status:** Draft - docs-only / provider-neutral / raw payload policy planning
**Date:** 2026-06-19
**Baseline:** `45b93a3 docs: close TIP-37 S3 evidence scope gates`
**Purpose:** Define provider-neutral raw payload policy requirements for `ART-009` before any provider-specific evidence collection can be authorized.

## Changelog

### v0.1 - Initial provider raw payload policy planning draft

- Opened TIP-38 as docs-only provider-neutral `ART-009` raw payload policy planning.
- Defined conservative raw provider payload allow/deny posture before provider-specific evidence collection.
- Defined redaction, sanitization, retention, expiry, disposal, access, audit, security, and evidence authorization requirements.
- Preserved TIP-37 classification of `ART-009` as a hard blocker for provider-specific evidence collection while unresolved.
- Recorded STOP/RRI gates for provider-specific evidence collection, raw payload storage, GDrive mirror handling, implementation, and readiness claims.
- Introduced the TIP-38-only GDrive review mirror reporting workflow requirement.

## 1. Status / Purpose / Non-Authorization

TIP-38 is docs-only, provider-neutral, raw payload policy planning only, and `ART-009`-focused.

TIP-38 defines provider-neutral policy requirements needed before any provider-specific evidence collection can be authorized. It narrows `ART-009` at planning/policy level only, subject to homeowner/GPT review and later closeout acceptance.

TIP-38 does not collect provider-specific evidence. TIP-38 does not authorize implementation.

TIP-38 does not authorize:

- provider selection;
- provider naming;
- provider comparison;
- provider scoring;
- provider shortlisting;
- provider recommendation;
- provider acceptance or rejection;
- provider-specific evidence collection;
- runtime, source, test, project, package, schema, migration, or index changes;
- public API, DTO, status, or error changes;
- raw artifact, biometric, provider payload, vault byte, credential, token, private key, or API key storage;
- SignFlow dependency;
- readiness, capability, legal, audit, external-audit, production, pilot, or certification claims.

## 2. Section 0 Repo Evidence

Read-only repo evidence used for TIP-38:

| Evidence | Current finding |
| --- | --- |
| Repository root | `D:/Task/Remote Signing/TagEkyc` |
| Current branch | `master` |
| Current HEAD before TIP-38 | `c14ace356548a20acd92530aa02e8b97f93dc73a chore: add GDrive sync index metadata` |
| TIP-35 closeout commit | `d01c2d8 docs: close TIP-35 branch and artifact debt registration` |
| TIP-36 closeout commit | `f1b1b80 docs: close TIP-36 analytical summary governance` |
| TIP-37 closeout commit | `45b93a3 docs: close TIP-37 S3 evidence scope gates` |
| TIP-37 accepted `ART-009` status | `ART-009` is a hard blocker for provider-specific evidence collection while unresolved. |
| Current known dirty out-of-scope files | `.gitignore`, `docs/00_AGENT_COORDINATION_BUS.md`, `tools/TagEkyc.GDriveSync/Program.cs`, `tools/TagEkyc.GDriveSync/README.md`. |
| Files changed by TIP-38 | `docs/tips/README.md`, `docs/tips/tip_38_provider_raw_payload_policy_planning/tip_38_planning_brief_v0_1.md`. |

## 3. TIP Analytical Summary / Intent Ledger

### Intent

Define provider-neutral raw payload policy requirements before provider-specific evidence collection can be authorized.

### Expected Outcome

After TIP-38, S3 has an accepted policy baseline for what may, must, and must not be collected, stored, redacted, retained, accessed, audited, and evidenced for provider raw payloads before any provider-specific evidence work begins.

### Accepted Decisions

| Decision | Why accepted | Scope impact | Non-claims |
| --- | --- | --- | --- |
| Focus on `ART-009` only. | TIP-37 classified `ART-009` as the hard blocker for provider-specific evidence collection. | TIP-38 narrows raw provider payload policy requirements only. | Does not resolve `GOV-001` or `ART-001` through `ART-008`. |
| Keep provider-neutral posture. | Provider-specific evidence collection remains blocked while `ART-009` is unresolved. | No provider facts, names, comparisons, scores, or recommendations may enter TIP-38. | No provider decision or provider suitability claim. |
| Treat raw provider payload collection as prohibited until explicitly authorized by later reviewed scope. | Collection without policy could leak raw biometric, provider payload, secret, or restricted data into evidence surfaces. | Default posture is deny until a later reviewed authorization packet permits a narrowly classified category. | Planning posture is not runtime enforcement. |
| Define default deny for raw provider payload persistence unless a later TIP proves necessity and policy compliance. | Persistence creates retention, access, audit, purge, legal-hold, and breach obligations. | Durable storage remains prohibited by default. | No storage implementation or readiness claim. |
| Require redaction/sanitization classification before any provider-specific evidence. | Evidence cannot be reviewed safely until raw, restricted, derived, and redacted classes are separated. | Later evidence packets must classify payload category and redaction path first. | Classification is not proof of redaction execution. |
| Require retention/access/audit policy before any collection. | Raw payload collection creates lifecycle and access obligations at collection time. | Later scope must define retention class, access boundary, audit events, and disposal evidence before collection. | No operational capability claim. |
| Require evidence authorization packet before collection. | Later reviewers need a complete pre-collection record to avoid accidental provider-specific evidence gathering. | Provider-specific collection remains blocked until packet review approves scope. | Packet definition is not collection authorization. |

### Rejected / Deferred Branches

| Branch / option | Disposition | Why | Follow-up debt/gate |
| --- | --- | --- | --- |
| Provider naming/comparison now. | Rejected. | TIP-38 is provider-neutral and has no authorization to name, compare, score, shortlist, recommend, accept, or reject providers. | Later explicit scope required before any provider name appears. |
| Provider-specific evidence collection now. | Rejected. | TIP-37 blocks provider-specific evidence collection while `ART-009` remains unresolved. | Later evidence authorization packet and reviewer approval required. |
| Raw payload storage implementation now. | Rejected. | TIP-38 is docs-only planning and does not authorize runtime/source/test/project/package/schema/migration/index changes. | Later implementation TIP required if storage is ever authorized. |
| Resolving `ART-001` through `ART-008` now. | Deferred except cross-reference. | TIP-38 focuses on raw provider payload policy and does not provide evidence for other artifact gates. | Carry unresolved gates into later reviewed TIPs. |
| Treating raw provider payload policy as artifact readiness. | Rejected. | Policy planning does not prove artifact storage, retention, purge, access, audit, legal, or operational capability. | Artifact readiness remains blocked pending reviewed evidence. |
| Public sharing of review mirror files. | Rejected unless explicitly instructed. | Review mirror files must remain private/restricted by default. | Public sharing requires explicit user instruction. |

### Debt / Gap Impact

| Debt/gap | Action | Result | Carry-forward gate |
| --- | --- | --- | --- |
| `ART-009` Provider raw payload policy unresolved | Primary target. | Policy narrowed / provider-specific collection still blocked until closeout acceptance and later evidence authorization. | Later reviewed scope must accept the policy and authorize any provider-specific evidence collection packet. |
| `GOV-001` Branch/deferred-scope debt traceability incomplete | Carried. | Unresolved. | Later relevant S3 work must visibly carry or resolve it. |
| `ART-001` Artifact/raw evidence storage boundary unresolved | Cross-referenced. | Unresolved. | Storage boundary must be resolved before raw payload persistence. |
| `ART-002` Durable metadata reference resolution unresolved | Cross-referenced. | Unresolved. | Raw payload must not enter durable metadata records; references require later resolution. |
| `ART-003` Evidence package object completeness unresolved | Cross-referenced. | Unresolved. | Evidence package completeness remains unproven. |
| `ART-004` Artifact retention / expiry policy unresolved | Cross-referenced for raw payload requirements. | Unresolved except raw payload policy requirements are defined. | Later reviewed TIP must resolve retention/expiry before collection. |
| `ART-005` Artifact purge / disposal workflow unresolved | Cross-referenced for raw payload requirements. | Unresolved except raw payload policy requirements are defined. | Later reviewed TIP must resolve purge/disposal before collection. |
| `ART-006` Artifact legal hold sync unresolved | Cross-referenced for raw payload requirements. | Unresolved except raw payload policy requirements are defined. | Later reviewed TIP must resolve legal-hold interaction before collection. |
| `ART-007` Artifact access/audit/security unresolved | Cross-referenced for raw payload requirements. | Unresolved except raw payload policy requirements are defined. | Later reviewed TIP must resolve access/audit/security before collection. |
| `ART-008` Metadata-artifact orphan handling unresolved | Cross-referenced. | Unresolved. | Later reviewed TIP must define orphan behavior for references and artifacts. |

### Non-Claims

TIP-38 makes no claim of provider suitability, provider readiness, artifact readiness, raw payload handling readiness, backup/recovery capability, restore capability, RPO/RTO support, operational readiness, legal reliance, audit reliance, external audit reliance, durable audit-store readiness, pilot readiness, production readiness, certification readiness, implementation readiness, real durability, or provider-specific evidence acceptance.

TIP-38 does not claim that `ART-009` is fully resolved. TIP-38 narrows `ART-009` at planning/policy level only and keeps provider-specific evidence collection blocked until closeout acceptance and later evidence authorization.

### Dispatch Readiness

TIP-38 is not an implementation TIP.

Implementation dispatch = NO.

No runtime, source, test, project, package, schema, migration, index, API, DTO, status, error, storage, vault, provider, or SignFlow surface may change under TIP-38.

## 4. Source Map

| Source | Anchor used by TIP-38 |
| --- | --- |
| `docs/tips/README.md` | TIP index v0.63 records TIP-37 closeout, including `ART-009` as hard blocker for provider-specific evidence collection while unresolved. |
| `docs/tips/tip_35_s2_branch_debt_traceability_artifact_gap_registration/tip_35_closeout_v0_1.md` | TIP-35 closed with `GOV-001` and `ART-001` through `ART-009` registered but unresolved. |
| `docs/tips/tip_35_s2_branch_debt_traceability_artifact_gap_registration/tip_35_planning_brief_v0_1.md` | Defines `ART-009` as unresolved provider raw payload policy and requires raw payload allow/deny, redaction/sanitization, retention, access, evidence authorization, and STOP/RRI before collection. |
| `docs/tips/tip_36_tip_analytical_summary_intent_ledger_governance/tip_36_closeout_v0_1.md` | TIP-36 accepted the mandatory TIP Analytical Summary / Intent Ledger and carry-forward discipline. |
| `docs/tips/tip_37_s3_provider_decision_evidence_scope_gov_art_gate_carry_forward/tip_37_closeout_v0_1.md` | TIP-37 accepted `ART-009` as a hard blocker for provider-specific evidence collection while unresolved. |
| `docs/tips/tip_37_s3_provider_decision_evidence_scope_gov_art_gate_carry_forward/tip_37_planning_brief_v0_1.md` | Defines S3 evidence-scope gates and requires raw provider payload policy before provider-specific evidence. |
| `docs/00_REVIEW_AND_TIP_PLAYBOOK.md` | `L-TAG-Gov-01` requires the intent ledger, branch/deferred disposition, non-claims, and STOP/RRI handling. |

## 5. Raw Payload Policy Definitions

| Term | Provider-neutral definition |
| --- | --- |
| Provider raw payload | Any unredacted request, response, callback, debug trace, media, document image, frame, stream, biometric sample, XML, JSON, binary object, or provider-originated/source payload received from, sent to, or produced for an external verification service boundary. |
| Provider-specific evidence | Evidence that depends on facts, behavior, schemas, responses, scoring, documentation, capabilities, limits, or artifacts of a concrete provider or provider candidate. |
| Raw payload metadata | Non-secret, non-biometric, non-payload descriptors about a payload, such as category, source boundary, timestamp, correlation reference, hash, size class, retention class, redaction status, and authorization packet id. |
| Derived/sanitized evidence | Evidence computed from raw input after removing raw biometric, document, provider payload, secret, token, credential, and other forbidden data, retaining only classified summary values needed for review. |
| Redacted evidence | Evidence in which sensitive fields and values are removed, masked, generalized, hashed, or replaced with references according to a reviewed redaction plan. |
| Forbidden raw payload | Any payload category or value that policy denies from collection, persistence, logs, docs, fixtures, durable metadata, or review mirrors. |
| Collection authorization | A reviewed, explicit permission record allowing a specific provider-specific evidence scope to collect only classified payload categories under stated storage, redaction, retention, access, audit, and disposal boundaries. |
| Retention class | A reviewed lifecycle classification defining whether an item is non-persistent, ephemeral, retained for a bounded duration, subject to legal hold, or prohibited from storage. |
| Access boundary | The roles, environments, approval path, audit obligations, and reference-disclosure rules that govern who may request or view restricted evidence. |
| Audit event | A durable, reviewed record of collection authorization, access, redaction, retention transition, legal-hold interaction, disposal, failure, incident trigger, or policy override. |
| Disposal evidence | Evidence proving deletion, expiry, quarantine, destruction, or non-retention handling occurred according to policy, including failure/retry state when disposal does not complete. |

## 6. Default Policy Posture

Provider raw payload collection is denied by default.

Raw provider payload persistence is denied by default.

Provider-specific evidence collection requires later reviewed authorization.

Derived/sanitized metadata may be allowed only if it contains no raw biometric data, provider payload, secret, credential, token, private key, API key, vault byte, source document image, liveness stream, or forbidden value, and only if explicitly classified.

Raw payload must not enter durable metadata records.

Raw payload must not enter logs, README, TIP docs, test fixtures, review mirror files, or generated indexes.

Raw payload must not be synced to the GDrive review mirror.

Any exception to default deny requires STOP/RRI, reviewer approval, and a later evidence authorization packet. TIP-38 itself grants no exception.

## 7. Allow / Deny Matrix

| Payload category | Default posture | May be collected? | May be persisted? | Redaction required? | Retention requirement | Access/audit requirement | Required gate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Raw biometric image/video/audio | Deny | No, unless later explicit authorization proves necessity. | No by default. | Required before any evidence use; raw remains forbidden in docs/logs/mirror. | Retention class required before any authorized collection. | Restricted access, explicit approval, collection/access/disposal audit. | STOP/RRI; later reviewed authorization; `ART-001`, `ART-004`, `ART-005`, `ART-006`, `ART-007`, `ART-009`. |
| Liveness frames/streams | Deny | No, unless later explicit authorization proves necessity. | No by default. | Required before any evidence use. | Retention class and expiry required before collection. | Restricted access and incident/audit triggers. | STOP/RRI; later reviewed authorization; `ART-001`, `ART-004`, `ART-007`, `ART-009`. |
| OCR image/source document image | Deny | No, unless later explicit authorization proves necessity. | No by default. | Required; document identifiers and images forbidden in docs/logs/mirror. | Retention class and legal-hold interaction required. | Restricted access, audit, disposal evidence. | STOP/RRI; later reviewed authorization; `ART-001`, `ART-004`, `ART-005`, `ART-006`, `ART-007`, `ART-009`. |
| Provider full JSON/XML response | Deny | No, unless later explicit authorization proves necessity. | No by default. | Required; secrets, tokens, raw biometrics, document data, and provider internals removed/masked. | Retention class required; non-persistence preferred. | Restricted access and full audit trail. | STOP/RRI; later reviewed authorization; `ART-009` plus relevant ART gates. |
| Provider debug trace | Deny | No by default. | No by default. | Required if later authorized; debug secrets and raw payload fragments forbidden. | Ephemeral or prohibited unless policy-classified. | Restricted access; incident trigger if raw/secret data appears. | STOP/RRI; later reviewed authorization. |
| Provider score/reason summary | Conditional allow | Yes only if sanitized and policy-classified. | Yes only as derived/sanitized evidence if later authorized. | Required when source may contain raw/restricted fields. | Retention class required before persistence. | Access/audit proportional to sensitivity. | Later evidence authorization packet; `ART-009` policy acceptance. |
| Provider reference/id/correlation only | Conditional allow | Yes if non-secret, non-sensitive, and classified. | Yes if durable metadata reference rules permit. | Masking/hashing required when identifier is restricted. | Retention class or metadata lifecycle required. | Audit reference creation/access where restricted. | Later evidence authorization packet; `ART-002`, `ART-009`. |
| Redacted provider response | Conditional allow | Yes only after deterministic redaction evidence. | Yes only if retention/access/audit requirements are accepted. | Already required and evidenced. | Retention class required before persistence. | Reviewer-approved access boundary and audit. | Later evidence authorization packet; redaction proof; `ART-004`, `ART-007`, `ART-009`. |
| Derived verification result | Conditional allow | Yes if it contains no raw payload or forbidden values. | Yes if classified as durable metadata or evidence summary. | Required if derived from sensitive source. | Metadata/evidence retention class required. | Audit creation and use in decision path. | Later evidence authorization packet; durable metadata rules. |
| Durable metadata reference | Conditional allow | Yes if reference is non-secret and classified. | Yes only as reference/hash/summary, not raw payload. | Required when reference exposes sensitive provider or artifact details. | Metadata retention class required. | Reference access audit when restricted. | `ART-002`, `ART-008`, `ART-009`; later reviewed scope. |
| Error payload | Deny raw | Raw errors no; sanitized error category may be allowed. | Raw no; sanitized category yes if classified. | Required; stack traces, tokens, raw payload snippets, provider internals removed. | Retention class for sanitized errors. | Error audit and incident trigger for forbidden leakage. | STOP/RRI for raw; later authorization for sanitized evidence. |
| Audit summary | Conditional allow | Yes if summary contains no raw payload, secret, biometric, or provider full payload. | Yes if classified and authorized. | Required if derived from restricted evidence. | Audit retention class required. | Audit integrity and access boundary required. | Later evidence authorization packet; `ART-007`, `ART-009`. |

## 8. Redaction / Sanitization Requirements

Redaction and sanitization requirements must be defined before any provider-specific evidence collection.

Fields and values that must be removed, masked, generalized, tokenized, or replaced with references include:

- biometric image, video, audio, liveness frames, streams, templates, and measurements that can reconstruct raw biometric evidence;
- source document images, OCR crops, document numbers, personal identifiers, addresses, dates of birth, and document security features unless explicitly authorized and redacted;
- provider full request/response bodies, callback bodies, XML/JSON nodes, debug traces, and binary attachments;
- credentials, API keys, private keys, tokens, secrets, session cookies, signatures, webhook secrets, certificates, vault bytes, internal URLs, and authorization headers;
- provider account identifiers, restricted reference ids, correlation ids, and request ids when they expose sensitive linkage;
- stack traces, debug logs, model internals, rule internals, and scoring details not explicitly authorized.

Forbidden values must not appear in docs, logs, test fixtures, durable metadata records, review mirror files, or GDrive mirror indexes.

Hash/reference-only alternatives must be preferred when evidence needs linkage without payload disclosure. Hashes must not be treated as safe if source values are low-entropy, reversible by dictionary, or still sensitive when linked.

Payload minimization rules:

- collect the minimum category needed for the reviewed question;
- prefer derived/sanitized summaries over raw payload;
- prefer reference and hash over copied payload;
- prefer ephemeral inspection over persistence;
- reject payload fields not tied to a reviewed evidence question.

No secrets, tokens, API keys, private keys, credentials, raw biometrics, provider raw payloads, vault bytes, or source document images may be included in docs/logs/review mirror files.

Provider raw payload must not enter durable metadata.

Before any redacted evidence is used, deterministic evidence of redaction must exist, including source category, redaction ruleset/version, removed/masked field list, reviewer approval, and verification that forbidden values are absent.

## 9. Retention / Expiry / Disposal Requirements

A retention class must exist before any provider raw payload or provider-specific evidence is collected.

Retention planning requirements:

- define whether the category is prohibited from storage, ephemeral only, short-retained, evidence-retained, legal-hold eligible, or audit-summary only;
- define expiry timestamp semantics and what happens when evidence expires before review, decision, package assembly, or dispute handling;
- define grace periods, if any, without making readiness or compliance claims;
- define whether references outlive payloads and how expired references behave;
- define environment separation for LocalDev, non-production, and production-like evidence.

Deletion/disposal workflow requirements:

- define authority to initiate disposal;
- define disposal execution path and audit event;
- define disposal evidence format;
- define retry/failure handling;
- define quarantine or incident handling if disposal fails;
- define non-delete/legal hold conflict behavior;
- define how disposal interacts with evidence package references and durable metadata.

Legal hold conflicts must stop deletion until the source of truth, release criteria, audit trail, and non-delete evidence are reviewed.

TIP-38 defines planning requirements only and makes no retention, expiry, purge, disposal, legal-hold, or compliance readiness claim.

## 10. Access / Audit / Security Requirements

Access to provider raw payload or restricted derived evidence must require explicit authorization before collection.

Role/category requirements:

- requester category and purpose must be recorded;
- reviewer approval must be recorded before collection or access;
- operator/support access must be denied by default and separately authorized if ever needed;
- developer/local access must not be treated as production evidence access;
- external reviewer access must use sanitized evidence unless raw access is explicitly authorized by later scope.

Audit event requirements:

- collection authorization created, approved, changed, or denied;
- payload category classified;
- raw payload collected, inspected, redacted, or rejected;
- evidence persisted, expired, deleted, quarantined, or placed/released from legal hold;
- restricted reference created or accessed;
- raw payload access requested, approved, denied, or completed;
- redaction failure, forbidden value detection, secret exposure, mirror sync attempt, or logging leak.

Incident trigger requirements:

- raw payload appears in logs, docs, tests, README, generated index, or GDrive mirror;
- secret/token/private key/API key appears in evidence;
- raw payload enters durable metadata;
- unauthorized access request or access occurs;
- retention expiry/disposal fails;
- provider-specific collection starts without authorization.

Restricted references must be handled as sensitive when they enable retrieval, correlation, or linkage to raw payloads.

No public link is allowed for raw payload or restricted evidence.

Raw payload must not be synced to the GDrive review mirror.

LocalDev behavior, LocalDev files, LocalDev access, or LocalDev sync must not be used as production evidence.

## 11. Evidence Authorization Packet Before Collection

Before any provider-specific evidence collection, a later reviewed scope must provide an evidence authorization packet containing:

- provider-specific scope authorization;
- payload category classification;
- allow/deny decision;
- redaction plan;
- retention class;
- access/audit plan;
- legal-hold/purge interaction;
- storage boundary;
- environment separation;
- reviewer approval;
- STOP/RRI resolution.

The packet must also state whether the proposed evidence uses raw payload, redacted payload, derived/sanitized evidence, references, hashes, audit summaries, or no provider payload at all.

No provider facts may be collected in TIP-38.

TIP-38 does not authorize any evidence authorization packet by itself; it defines the packet requirements for later review.

## 12. Relationship to Other GOV/ART Gates

TIP-38 focuses on `ART-009`.

`ART-001` storage boundary remains unresolved.

`ART-002` reference resolution remains unresolved.

`ART-003` evidence package completeness remains unresolved.

`ART-004` retention remains unresolved except raw payload policy requirements are defined.

`ART-005` purge remains unresolved except raw payload policy requirements are defined.

`ART-006` legal hold remains unresolved except raw payload policy requirements are defined.

`ART-007` access/audit/security remains unresolved except raw payload policy requirements are defined.

`ART-008` orphan handling remains unresolved.

`GOV-001` remains carried.

TIP-38 must not be used to claim resolution of `GOV-001` or `ART-001` through `ART-008`.

## 13. STOP/RRI Conditions

STOP/RRI before:

- any provider name appears;
- provider-specific evidence collection starts;
- raw provider payload is collected;
- raw provider payload is persisted;
- raw payload is synced to GDrive mirror;
- raw payload appears in logs, docs, tests, README, generated index, or fixtures;
- provider comparison/scoring starts;
- implementation is authorized;
- `ART-009` is claimed as fully resolved without closeout acceptance;
- `ART-001` through `ART-008` are claimed as resolved by TIP-38;
- readiness, legal, audit, production, pilot, external-audit, certification, capability, support, or provider-suitability is claimed;
- docs-only policy is treated as runtime enforcement;
- raw payload is proposed for durable metadata records;
- secrets, tokens, private keys, API keys, credentials, vault bytes, or provider raw payloads are proposed for storage;
- public sharing is proposed for review mirror files without explicit user instruction;
- SignFlow dependency is introduced.

## 14. GDrive Review Mirror Reporting Requirement

TIP-38 introduces a workflow experiment for TIP-38 review only.

When Codex creates, edits, commits, or syncs TIP-38 docs files, Codex must report changed/synced docs files with:

- path;
- fileId;
- webViewLink;
- sha256;
- state.

The review mirror must be private/restricted unless the user explicitly instructs otherwise.

The review mirror must be allowlist-only.

Do not sync secrets, raw payloads, provider payloads, biometric artifacts, logs with tokens, database dumps, certificates, keys, `.env`, appsettings with secrets, `bin/`, `obj/`, or `.git`.

For TIP-38, sync/review only docs files intended by this TIP.

This workflow section is process-only and does not modify product behavior.

## 15. README Update

README update requirements for TIP-38:

- Add TIP-38 row to `docs/tips/README.md`.
- Add changelog entry: TIP-38 opened as docs-only provider-neutral `ART-009` raw payload policy planning.
- Record that no provider is selected, named, compared, or evidenced.
- Record that no provider-specific evidence collection is authorized.
- Record that no implementation is authorized.
- Record that the GDrive mirror reporting experiment is introduced for TIP-38 review workflow only.

## 16. Validation

Recommended docs-only validation:

```powershell
git diff -- docs/tips/README.md docs/tips/tip_38_provider_raw_payload_policy_planning/tip_38_planning_brief_v0_1.md
git diff --check
git status --short
```

Do not run `dotnet test` unless docs-only scope is violated.

Before commit, stage only:

- `docs/tips/README.md`
- `docs/tips/tip_38_provider_raw_payload_policy_planning/tip_38_planning_brief_v0_1.md`

Verify:

```powershell
git diff --cached --name-only
git diff --cached --check
```

Leave unrelated dirty files unstaged:

```text
 M .gitignore
 M docs/00_AGENT_COORDINATION_BUS.md
 M tools/TagEkyc.GDriveSync/Program.cs
 M tools/TagEkyc.GDriveSync/README.md
```

## 17. Next Action

Submit TIP-38 for homeowner/GPT review.

If accepted, TIP-38 should be closed as policy narrowed at planning/policy level only. Provider-specific evidence collection remains blocked until closeout acceptance and a later evidence authorization packet explicitly permits a narrow provider-specific collection scope.

No provider selection, provider naming, provider comparison, provider scoring, provider shortlisting, provider recommendation, provider acceptance/rejection, provider-specific evidence collection, implementation, artifact readiness, legal/audit reliance, external audit reliance, production readiness, pilot readiness, certification readiness, or capability claim proceeds from TIP-38.
