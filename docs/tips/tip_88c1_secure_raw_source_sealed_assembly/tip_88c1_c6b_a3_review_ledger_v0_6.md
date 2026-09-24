# A3 v0.6 — bounded correction and independent review ledger

Date: 2026-09-13. Non-normative evidence record. Documentation/source review only; no A3 implementation authority, executable proof, A4/production or Git landing authority.

## 1. Input and bounded scope

User-supplied GPT review/correction attachment:
`C:/Users/Admin/.codex/attachments/cfb00b14-0dc6-4f27-8704-6ceb4b431719/pasted-text.txt`
SHA-256 `828228B5D599845DA1639F60206990DEC6B51B457E8954F4134EB4CE9C6F612B`.

External disposition on exact v0.5 was divergent: CC PASS on withdrawal/lineage/proof specification; GPT HOLD with two HIGH and one MEDIUM implementation-contract findings. CC's narrower PASS did not override GPT's defects. User attachment requested one docs-only successor, preserving predecessors and closed decisions. No new consent question/RRI/TIP was opened.

| Finding | Source reality checked | Bounded disposition / owner |
| --- | --- | --- |
| F01 HIGH | ProductionConfigGate.ValidateCore calls unconditional retention gate; Managed also reaches it before devices | AR §5.1 distinguishes Managed local request from legacy validation and independent current server authority. Add exact gate M row; positive retained + negative old gate/config controls |
| F02 HIGH | Coordinator compares whole receipt with six metadata journal slots; receipt has no retention-mode field; mandatory BodySha256 makes raw journal widening wrong | AR §5.3 keeps six metadata operations, two S-derived receipt-only raw results and exact kind/ID; add coordinator M row. Actual ReceiptWritten/Host/WPF caller remains in existing scope |
| F03 MEDIUM | New multiline SQL migration would compile differently from CRLF working source and LF index snapshot | CP10.1 exact execution transform, separate canonical drift comparison and independently compiled PostgreSQL representation proof; no historical/.gitattributes edit |

Source checks included actual gate, coordinator, receipt DTO, ReceiptWritten, Host/WPF ValidateManaged and receipt callback, six-entry Windows journal, mandatory metadata BodySha256 record, existing A2 direct ACK/restart tests, and both E3 Up/Down execution transforms. The E3 historical two-MD5 exception is deliberately not imported.

## 2. Affected-surface map

| Rule | Sections | DTO/API impact | Identity/hash/audit | Required proof / scope |
| --- | --- | --- | --- | --- |
| Managed RawVault request | Parent census/batch/evidence; AR5.1; inventory two M additions | No public route/DTO/default authority; existing Managed validation and raw sender composition | P/Client/session, consent, CRT1 unchanged | A3_ManagedRetentionGate_RequiresServerAuthority in existing proposed Agent retained-ownership test |
| Receipt6+2, journal6 | AR5.3/proof; parent ordering/batch; inventory existing caller/test rows | Existing receipt DTO only; exact raw kind raw_export_source, SourceArtifactId lowercase UUID-D | No raw digest/secret/journal field; local context only after same delegation CAS wins | Actual coordinator/ReceiptWritten/journal positive and missing/duplicate/unknown/restart negative controls |
| SQL representation | CP10.1/proof; parent batch/evidence; inventory fence/proof join | No route/function signature/DDL semantic change | CRLF→LF→CRLF execution; only CRLF→LF for canonical body SHA256; one expected hash per signature/state | A3_RetentionMigration_CheckoutRepresentationsConverge in existing proposed migration test; runner already in scope |
| Common fences | All successors | No new consent/action, no public upload topology, no extra outcome | E01/broker v0.5 retained exact; source freeze compares all64 hex | Synthetic only after grant; no A4/production/stage/commit/push |

## 3. Review round 1 — two independent lanes, one frozen draft

Frozen initial v0.6 parent:
`DE3A695B01B59C03A90E7835DCA904783809643BC741FF08998D8B3B45B0B5E7`.

- `a3_v06_bounded_review`: HOLD, one MEDIUM LATENT_SPEC_GAP.
- `a3_v06_free_review`: HOLD, same defect classified MEDIUM PATCH_REGRESSION.
- Synthesis: **one unique F02 receipt-context tail**, not two independent defects.

Both read corrected AR/CP surfaces, parent/inventory joins and actual caller/journal/test seams. They found that absent process-local retained mode must not imply the six-slot A2 shape: after restart both durable histories have identical six ACK entries, and the old ACK boundary would clear PendingBind. This was a contract ambiguity, not a demonstrated network exploit.

F01/F03 were clean in both lanes. Free review considered loss of mode context, receipt callback timing, unchanged A2 fixture paths and SQL representation non-vacuity, not merely diff strings. Census/drafting assistance is not counted as a review.

### Applied correction

Require live context for both receipt shapes. A shared private ClaimDelegation core creates exact-binding/mode context only after the current-process existing CAS wins. Public ClaimDelegation(Guid) supplies explicit six-only VerifyAndDiscard; actual Managed RunSubmitOnly uses its validated requested mode. Failed/replayed CAS never creates context. Keep context through RunSubmitOnly finally and receipt writing; clear at successful ACK or terminal retirement. Unknown context rejects both six/eight without journal mutation.

Main agent and reviewers verified existing A2 Wire, Composition and KeyCustody direct ACK controls call ClaimDelegation first, so the correction does not need additional test mutation rows or a journal schema change. Two unchanged Agent record/journal files were added as read-only SHA evidence, not M rows.

Root cause: the first draft combined an inherited six-slot success statement with new process-local mode without defining the missing-context branch. Correction pins all branches and ties the discriminator to the existing real CAS, rather than adding another persisted field or inferring mode from receipt content. No third round/non-convergence checkpoint was needed.

## 4. Review round 2 — patch and adversarial sentinel verification

Final frozen parent:
`3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813`.

- `a3_v06_bounded_review`: **PASS — 0 ACTIONABLE FINDINGS**.
- `a3_v06_free_review`: **PASS — 0 ACTIONABLE FINDINGS**.

Both independently checked actual final bytes, full companion hashes, the new read-only hashes, corrected unknown-context branch, CAS winner setup, receipt-after-finally lifecycle, and A2 direct-call test feasibility. F01/F03 and inherited authority sentinels remain clean.

Zero-finding justification:
1. A2 direct-call regression risk dismissed because the existing three test families claim delegation before ACK; explicit six-only context is created by that same successful call.
2. Restart authority/receipt downgrade risk dismissed because the already-won CAS cannot recreate context; both six and eight are rejected when Unknown.
3. Premature context disposal risk dismissed because the contract requires context through RunSubmitOnly finally and ReceiptWritten.
4. Journal/representation scope drift dismissed because journal/record are read-only and F03 uses existing proposed migration/test/runner paths with strictly separated execution/comparison transforms.

Remaining uncertainty: none of these future production changes or proofs has been implemented/run by this correction. Real Windows, HTTP and PostgreSQL behavior must be shown by implementation evidence after a grant. Internal review is not external final ratification.

Total this correction: **2 review rounds, 4 independent lane passes**, with one unique finding patched. Prior v0.5 rounds remain historical in the preserved ledger and are not recounted as this correction's reviews. No round3/5/10 trigger was reached. No quota limited findings.

## 5. Final exact catalogue

| Artifact | SHA-256 | Bytes | Lines |
| --- | --- | --- | --- |
| `tip_88c1_c6b_a3_broker_composition_dispatch_v0_6.md` | `3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813` | 39495 | 320 |
| `tip_88c1_c6b_a3_e01_contract_v0_5.md` | `53CB0A0FB0F3638D5123C1F11B7CA2B76AB19E96363DEFFD789164FF457F2DAA` | 42623 | 217 |
| `tip_88c1_c6b_a3_retention_checkpoint_contract_v0_6.md` | `EC42987AA5F7A8FEA3642C60C23A80D57033645870E4D98DE7B62728E41CB21D` | 63217 | 323 |
| `tip_88c1_c6b_a3_broker_pipeline_contract_v0_5.md` | `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48` | 84714 | 414 |
| `tip_88c1_c6b_a3_agent_result_contract_v0_6.md` | `D97F33F688F88CAF21F0B8C7EE9384A31D1F11C5C6142804A34530D9056C9B74` | 37841 | 261 |
| `tip_88c1_c6b_a3_mutation_inventory_v0_6.md` | `E2B4EB58C1D0CE0C71BDE1033B4E103C458E45F255BA60C967144D5010FF325D` | 28474 | 186 |

Parent binds exactly five companions; E01 and broker remain byte-exact v0.5, explicitly adopted unchanged. All six files above are UTF-8 LF-only without BOM.

Mechanical source audit: **150/150 rows matched**, zero mismatch:
- 95 future mutation rows = 50 M exact existing bytes + 45 N confirmed absent.
- 55 read-only/inherited references = 53 Server references + two Agent references.
- Duplicate source citations are counted as references, not distinct files.
- Full64 SHA comparison, never prefix/suffix or old-or-new acceptance.
- Exactly two new M paths relative to v0.5; no journal/project/migration-history scope expansion.

## 6. Preserved v0.5 provenance

All seven files remain present and exact; nothing deleted or overwritten:

| Preserved artifact | SHA-256 |
| --- | --- |
| `tip_88c1_c6b_a3_agent_result_contract_v0_5.md` | `1749DEE69B9DD177BF99BAC64FB29C8BF7E95282EFCC86B5CAEF44301B79189D` |
| `tip_88c1_c6b_a3_broker_composition_dispatch_v0_5.md` | `9A0254769139577D78F0FD6374D1E052ECE4B3D500BFAD5FBB7337531F7FD38F` |
| `tip_88c1_c6b_a3_broker_pipeline_contract_v0_5.md` | `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48` |
| `tip_88c1_c6b_a3_e01_contract_v0_5.md` | `53CB0A0FB0F3638D5123C1F11B7CA2B76AB19E96363DEFFD789164FF457F2DAA` |
| `tip_88c1_c6b_a3_mutation_inventory_v0_5.md` | `94E27FAC32BB55EFD6E3195F25064AC27F350483D20805BDAD081D9CF0D4B910` |
| `tip_88c1_c6b_a3_retention_checkpoint_contract_v0_5.md` | `601DA50B691C026FB4E285E96CFBCA08B22A3D53E36E189CB604D7B6FBF6D491` |
| `tip_88c1_c6b_a3_review_ledger_v0_5.md` | `9302B4E9E133D89C9B21C909B34F792F9374EAA57538DCC9A534B874D62608CE` |

Earlier v0.4 parent also remains preserved; no historical verdict is rewritten.

## 7. Execution / Git / terminal disposition

- Current changes: five new v0.6 Markdown files only (parent, AR, CP, inventory, this ledger). E01/broker/predecessors not edited.
- Product/schema/test/runtime-config/project/migration mutations by this turn: **0**.
- Tests/build/PostgreSQL/provider execution: **NOT EXECUTED — documentation-only correction**.
- Server HEAD remains `5df5f60a6dc4d992c71fc2b160673e4abca6488d`; Agent HEAD remains `e3bd625bbe357b1f3c9620d20bcba121cfb61974`.
- Staged=0, conflicted=0 in both repositories. No stage/commit/push; unrelated dirty/untracked work preserved.
- GDrive publication/sync: not performed; no external write authorization inferred.
- S01/S05/ONE-consent, independent post-Completed export authority, 36 outcomes, private broker/mTLS deferral and lifecycle production HARD STOP unchanged.

`A3_V0_6_READY_FOR_EXTERNAL_FINAL_REVIEW`

This is a bounded documentation/source-review PASS, **not implementation authority or an A3 execution PASS**. Submit this exact catalogue to external CC/GPT review; implementation remains NOT GRANTED pending Homeowner.
