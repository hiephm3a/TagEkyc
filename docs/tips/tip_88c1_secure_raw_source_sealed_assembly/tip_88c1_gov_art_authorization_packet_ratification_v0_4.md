# TIP-88C1 GOV/ART Authorization Packet Ratification Record v0.4

**File:** `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_ratification_v0_4.md`

**RatificationRecordVersion:** `0.4`

**PreviousRatificationRecordSHA256:** `17422B760C93BFDF7179629377883327D5BC98EE4722796C7C59C82C283E6860`

**PreviousRatificationRecordRepositoryCommit:** `880a2005ad08a850dade2fcf476b12f5e912c3cd`

**StateSequence:** `3`

**PacketSHA256:** `83D785002949600010FE1ABEC1E15B5AA0BE79B6984627957C46D17AA14D620E`

**TransitionType:** `COMMITTED_REANCHOR`

**TransitionActor:** `CODEX_DRAFTER_UNDER_HOMEOWNER_V0_5_AUTHORING_AUTHORITY`

**TransitionAtUtc:** `2026-08-04T15:04:37.4658186Z`

**Status:** `PRE_RATIFICATION_INACTIVE`

**Repository baseline:** `672f3e4eeba24b6082bbb3c0696e597c8821e4c7`

## 1. Re-anchor purpose and non-authority

This record is the one provenance-preserving `COMMITTED_REANCHOR` permitted by
packet v0.5. It resolves the ambiguity created when ratification records
v0.1–v0.3 were committed together after carrying an
`UNCOMMITTED_PRE_RATIFICATION` sentinel.

The old records remain immutable `SUPERSEDED_INACTIVE_HISTORY`. They never
granted authority. This record does not rewrite, delete or treat them as active.
It binds their latest exact committed blob and begins a new eligible segment
only after this record itself is committed and a later successor verifies that
commit directly from Git.

Drafting, reviewing, hashing or committing this record does not activate
fixture evidence. This record contains no clean-review binding and no Homeowner
ratification binding.

## 2. Exact predecessor provenance

| Field | Bound value |
| --- | --- |
| Current record path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_ratification_v0_4.md` |
| Previous record path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_ratification_v0_3.md` |
| Previous record version | `0.3` |
| Previous record SHA-256 | `17422B760C93BFDF7179629377883327D5BC98EE4722796C7C59C82C283E6860` |
| Previous record repository commit | `880a2005ad08a850dade2fcf476b12f5e912c3cd` |
| Previous state sequence | `2` |
| Current state sequence | `3` |
| Historical disposition | `SUPERSEDED_INACTIVE_HISTORY` |
| Current activation status | `PRE_RATIFICATION_INACTIVE` |

The predecessor commit must contain the exact v0.3 blob at the bound path. A
later successor must prove that this record's containing commit descends from
`880a2005ad08a850dade2fcf476b12f5e912c3cd` and contains this exact record blob.
`NONE` is not used and is forbidden here because this is not the original
genesis.

## 3. Re-anchor commit binding

This record cannot contain the hash of the commit that will contain its own
bytes. Its first successor must bind:

```text
PreviousRatificationRecordSHA256 = SHA-256 of this exact record blob
PreviousRatificationRecordRepositoryCommit = exact commit containing this blob
StateSequence = 4
```

The successor verifies from Git that the commit contains this blob at the exact
path and descends from the bound predecessor commit. Until that proof exists,
this draft is not an activation-eligible predecessor.

The final active record's own containing commit remains externally verified by
a later Build Brief through exact `RecordPath`, `RecordBlobSHA256` and
`ContainingRepositoryCommit`; no record self-binds its containing commit.

## 4. Bound packet

| Field | Bound value |
| --- | --- |
| Packet ID | `TIP-88C1-GOV-ART-FIXTURE-EVIDENCE-v0.5` |
| Packet version | `0.5` |
| Packet path | `docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_gov_art_authorization_packet_v0_5.md` |
| Packet SHA-256 | `83D785002949600010FE1ABEC1E15B5AA0BE79B6984627957C46D17AA14D620E` |
| Superseded packet | v0.4, SHA-256 `B37D2537793B3D4347D3FB2D8016FC0E6CCF93237CD91F8872A32F6F067A55E5` |
| Locked Planning Brief | v0.17, SHA-256 `D7C985A6D7696D9D68723EA5162C438A60D3363F5C3A18654D3EFF53C53D46AC` |

Any packet byte, ID, version, path or SHA mismatch makes this record
inapplicable. A changed packet requires a new packet and a new record successor;
neither existing file may be edited in place.

## 5. Review bindings

| Required review | Reviewer identity | Verdict | Reviewed packet SHA-256 | Review timestamp | State |
| --- | --- | --- | --- | --- | --- |
| Independent review 1 | `UNSET_PRE_RATIFICATION` | `PENDING` | `83D785002949600010FE1ABEC1E15B5AA0BE79B6984627957C46D17AA14D620E` | `UNSET_PRE_RATIFICATION` | Not bound |
| Independent review 2 | `UNSET_PRE_RATIFICATION` | `PENDING` | `83D785002949600010FE1ABEC1E15B5AA0BE79B6984627957C46D17AA14D620E` | `UNSET_PRE_RATIFICATION` | Not bound |

The two reviews must use distinct independent reviewer identities and return
clean verdicts for this exact digest. Reviews of v0.4 do not count. Each binding
is append-only and requires a new successor record after this re-anchor is
committed. Failed, conditional, partial, stale or different-digest reviews are
not clean.

## 6. Homeowner binding

| Field | Current value |
| --- | --- |
| Homeowner identity | `UNSET_PRE_RATIFICATION` |
| Ratified timestamp UTC | `UNSET_PRE_RATIFICATION` |
| Ratification statement/evidence reference | `UNSET_PRE_RATIFICATION` |
| Activation status | `PRE_RATIFICATION_INACTIVE` |
| Effective scope | Generated, non-patient Gate A evidence only, under packet v0.5 and a later separately ratified Build Brief. |

Only an explicit Homeowner instruction may authorize a later chained successor
that binds the completed independent reviews and transitions to
`RATIFIED_ACTIVE`. No wording in this draft is that instruction.

## 7. Exact eligible sequence

```text
SUPERSEDED_INACTIVE_HISTORY (v0.1–v0.3; no authority)
-> COMMITTED_REANCHOR v0.4 (this record; inactive)
-> committed successor binds independent clean review 1
-> committed successor binds distinct independent clean review 2
-> committed successor binds explicit Homeowner identity + statement + UTC
-> RATIFIED_ACTIVE
```

Every arrow after the re-anchor creates a new record version, increments
`StateSequence`, and binds the exact predecessor SHA-256 plus containing commit.
Missing or conflicting evidence leaves the chain inactive. `RATIFIED_ACTIVE`
would permit only a later Build Brief to request the exact Gate A execution; it
does not itself execute anything.

## 8. Invalidation and revocation

An active successor must transition through a new chained record to
`INVALIDATED_INACTIVE` if any packet, record, commit ancestry, reviewer binding,
image/platform pin, Model A posture, capability graph, expected-fault binding,
retention/expiry, deletion evidence, lifecycle, residue, watchdog or deadline
contract changes materially, or if real/patient data can enter scope.

Only the Homeowner may transition an active successor to `REVOKED_INACTIVE`.
Invalidation or revocation never deletes history or reactivates automatically.

## 9. Current disposition

```text
ActivationStatus = PRE_RATIFICATION_INACTIVE
TransitionType = COMMITTED_REANCHOR
ReanchorContainingCommit = NOT_YET_BOUND_BY_SUCCESSOR
IndependentReview1 = PENDING
IndependentReview2 = PENDING
HomeownerRatification = NOT_RECORDED
FixtureEvidenceAuthority = NONE
BuildBriefAuthority = NONE
ImplementationAuthority = NONE
ProviderOperationAuthority = NONE
RawBioAuthority = NONE
```

This record authorizes no Build Brief, implementation, provider operation,
fixture execution, MinIO access, real Raw BIO, production, staging, commit,
push, merge, PR or deployment.
