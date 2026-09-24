# A3 P04 corrected-contract — exact Homeowner ratification

Date: 2026-09-23. Decision source: the Homeowner's explicit statement **“Tôi ratify P04 theo corrected contract.”** after independent GPT and CC technical acceptance. This record ratifies P04 only under `a3_p04_corrected_contract_v1.md`. It does not ratify or move any of the four `TRANSPORT-EXPECT-FENCE` rows, does not authorize census zero, and does not authorize production activation.

A3-Ratification-Decision: RATIFIED
A3-Authority-Open-After: 4
A3-Reviewed-Manifest-SHA256: BA2AFC955EC26444BB4026342B32E0CF14A32D8FF95AE84E849B9124A892BE1F

Reviewed packet: `a3_p04_site_transport_qualification_review_packet_v1.md`, SHA-256 `ED4C7BE8F46E7D4E70A3CF10A6E77B49F67E0A146F8343915049679580DFFFEF`. Corrected contract: `a3_p04_corrected_contract_v1.md`, SHA-256 `C55E22B245E98D4490D0E89C2E490ADA91B97D5C262529C51C2721B451A50E0F`. The accepted P04 evidence contains separate Content-Encoding and Trailer mutations, an independent PostgreSQL nonce-ordering mutation for generic pre-auth Transfer-Encoding rejection, byte-exact restoration and a restored 3/3 gate. Neither reviewer found a remaining technical P04 gap under the corrected contract.

Scope successor: `a3_activation_scope_p04_successor_decision_v1.md`, SHA-256 `B812E84765CB57FCCB4F59D4FCB39ED87436CA123678CCDA967A2D33DACB075C`. The reviewed manifest pins current partition SHA-256 `B84B35A87DD1D94447281B845FD3421AEF484768776D4CBC176782B94AB1030B`, reconciliation ledger SHA-256 `57859DA7A99A72D43BF0947B6FFA925C643ACEEF182C782445211EE54C6A017D`, and canonical ownership registry SHA-256 `8F484CCF6204B487923F8E1779368D83B398E304E4CAB4CEEA201ADED4B13E59`. It inventories 2,670 files and 1,248 TRX, records 106 distinct mutants with zero live matches, and excludes only itself, its sidecar, this record and the seal descriptor from the hash cycle.

The predecessor 46-row wave partition, seven-row re-baseline, BP10/P05 ratifications and 38-row deferred backlog remain historical and unchanged. Current activation-open census is four and consists exactly of the four Expect-fence rows. The development-loopback site qualification is not hospital evidence and is not pinned by this count-four transaction. A format-2 revision-6 seal at count four must still block Activated as `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE`. A3 remains HOLD; this record makes no full-suite PASS, stage, commit, push, landing or production-activation claim.
