# A3 R2–R6 remaining-cluster ratification v1

Status: **HOMEOWNER RATIFIED / A3 HOLD**

On 2026-09-20 the Homeowner explicitly ratified these six rows after independent technical acceptance by both reviewers:

- P17 / O17
- P18 / O18
- P19 / O19
- P20 / O20
- P22 / O22
- BP17 `CiphertextFailureIsNotProducerMismatch`

The exact reviewed handoff is `a3_r2r6_remaining_cluster_review_packet_v3.md`, SHA-256 `57F3BA3810EBDF254E0E83BAF6BFD05C37ECBB8C6F5D5FFBF57CF9A9D4FB0A1E`. Its reviewed candidate manifest is `a3_r2r6_remaining_cluster_candidate_manifest_v3.tsv`, SHA-256 `149591E75C4F00C5BC2257FD8358DFD8D950A8B6364677C3B860866CEB91412A`.

Ratification changes no product/test bytes and requires no rerun. The mechanical table has **50 normative `NOT_IMPLEMENTED` rows + 1 seam = 51**, and BP17 already carried `IMPLEMENTED`, so its authority ratification is not subtracted a second time. `E01_Race_IssueWithdrawal` remains `CANDIDATE_PENDING_RATIFICATION`, making the canonical authority-open census **51 normative + 1 seam = 52**. A3 remains HOLD. No full suite, stage, commit, push, landing or production activation is authorized.
