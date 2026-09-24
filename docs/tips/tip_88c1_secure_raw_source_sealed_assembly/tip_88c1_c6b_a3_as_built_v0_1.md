# TIP-88C1-C6B-A3 — as-built implementation review packet v0.1

**Status:** implementation candidate complete; independent final review and
Homeowner ratification remain required.

**Scope:** synthetic/non-patient A3 only. This packet grants no A4, production,
stage, commit or push authority.

## 1. Exact authority

| Artifact | SHA-256 |
| --- | --- |
| Parent A3 v0.6 | `3918F738293747CDB35835044182C7D464D93B5E57294915B76B7B8CF48D3813` |
| E01 contract v0.5 | `53CB0A0FB0F3638D5123C1F11B7CA2B76AB19E96363DEFFD789164FF457F2DAA` |
| Retention checkpoint contract v0.6 | `EC42987AA5F7A8FEA3642C60C23A80D57033645870E4D98DE7B62728E41CB21D` |
| Broker pipeline contract v0.5 | `7CAD0F8DC1BF2E84F859B0C555FFED1884C6FD14AE2BBE0BFF070AE934C12C48` |
| Agent/result contract v0.6 | `D97F33F688F88CAF21F0B8C7EE9384A31D1F11C5C6142804A34530D9056C9B74` |
| Mutation inventory v0.6 | `E2B4EB58C1D0CE0C71BDE1033B4E103C458E45F255BA60C967144D5010FF325D` |

The implementation also incorporates every bounded Homeowner correction recorded
in `tip_88c1_c6b_a3_implementation_ledger.md`, including the client-registry FK
correction, finite allowlist corrections, historical-test compatibility envelope
and final A2+A3+CRT1 acceptance-runner correction.

## 2. Implemented surface

The candidate implements A3.0–A3.4 without adding another consent action:

- authenticated existing-consent reference binding and deterministic withdrawal;
- SourceRetention permit, capability, binding, authority-snapshot and custody
  lineage bound to the authenticated BusinessConsumer principal;
- retention checkpoint revalidation and actual B2-withdrawal serialization;
- one private broker transaction and existing C1/R2–R6 primitive reuse;
- split Writer/Reconciler/Lifecycle provider roots with exact PostgreSQL
  LOGIN/role/ACL qualification;
- NPS01, RE01 and TI01/TI02 durable recovery semantics;
- CP08/CP09 readback, worker continuation and real child-process kill/restart
  recovery to `Available`;
- independent post-Completed C1/C3 export authority;
- Agent retained raw ownership, exact CRT1 metadata/body commitment, six durable
  journal entries plus two receipt-only raw entries, and restart fail-closed
  receipt semantics.

No second raw pipeline, local consent policy, Client API-key fallback, raw journal
slot, receipt-derived restart authority or production activation path was added.

## 3. Final executable evidence

| Gate | Result | Evidence SHA-256 |
| --- | ---: | --- |
| Server Integration | `1339 PASS / 0 failed / 1 manual skip / 1340 total` | `842C96F7BEC63E78844A057900FDD72BF4D393473CD201A817B75644E1E85CDE` |
| Server Unit | `324/324 PASS` | `B01C2F62013447A5F3E68A65D9D64D612B5425396D6D541CE2AB16D657FC658D` |
| Server Arch | `156/156 PASS` | `89A954CA5CB0C13E72E4D177A3FCFB0E380F25CD4244EEFDC8084AF9735A2D76` |
| Server Contract | `15/15 PASS` | `4B82808EEF30841FED23E804050C63BC5CC4757A98A0455CD4D39FDBC39A4687` |
| Agent full | `307 PASS / 0 failed / 1 manual skip / 308 total` | `7100044F4228F724B436748B9920C887794E543A4B2EDE102B626D9D58A10DCA` |
| Cross-repository A2+A3+CRT1 acceptance | `9/9 PASS` | `076739739D89BD2928035B910CE9674D4812A9188EF78F5E3485834B70CE6918` |

The Integration TRX is
`TestResults/a3-final-v4/a3-final-integration-v4.trx` (2,126,013 bytes). The
acceptance runner mechanically discovered and executed all three required named
classes with both A2 and A3 MSBuild opt-ins enabled; no class was merely compiled
and filtered out.

The one Server skip remains the historical manual golden-vector generator
`Tip67GGoldenNeutralProofVectorTests.Manual_generate_tip67g_golden_vectors`.
The one Agent skip remains the historical manual silent-face-orientation smoke
test. No new skip or not-executed A3 case was introduced.

## 4. Final diagnostic correction

The first full Server run was diagnostic: 1335/1339 executed cases passed and
four failed. No product semantic correction was needed. Exact bounded
compatibility corrections were applied to:

- move the old finalization concurrency rendezvous before A3's earlier session
  lock while preserving the two concurrent calls and every outcome/count;
- admit exact `ck_a3_snapshot_retention_shape` in the closed constraint-name set;
- execute immutable A1 `core_identity`/`expiry` proof bytes on the exact A1 schema,
  then migrate the same residue-free database to A3 and reassert current state.

The affected set plus the unchanged normative runtime positive control passed
5/5 in 21 seconds. Evidence SHA-256:
`18C3AF0974EA18E8EC244FA93AE2E0AE1A511C3A1B9D8D24724DC140DB1ADDB2`.
The final full run was executed only after this focused gate and a default
non-opt-in rebuild.

## 5. Test-identity continuity

The diagnostic and final Server Integration runs each contain exactly 1,340
results and 1,337 distinct normalized display identities. Test display data has
two intentional nondeterministic families: temporary environment-variable
suffixes and synthetic GUID fixture values. After replacing only those values
with typed placeholders:

```text
missing identities       0
extra identities         0
multiplicity mismatches  0
duplicate drift          0
non-passed final cases   0 (excluding the one unchanged manual skip)
```

No test was removed, renamed to evade comparison, skipped or made conditional.

## 6. Exact source/project freeze

Freeze algorithm: enumerate `.cs` and `.csproj` files under `src/` and `tests/`,
exclude `bin/` and `obj/`, sort case-sensitively by repository-relative `/` path,
form `path|raw-SHA256\n`, then SHA-256 the UTF-8 manifest bytes.

| Repository | Files | Pre-run fingerprint | Post-run fingerprint |
| --- | ---: | --- | --- |
| Server | 676 | `8F446014F11D207891A585FCD292763F7D0BD0445F2D745D82740A67FB490109` | `8F446014F11D207891A585FCD292763F7D0BD0445F2D745D82740A67FB490109` |
| Agent | 70 | `C6E1F17CF5A28B6964B2F528B40B7C72B46D79AEE7A2D67071E181F95AFE993F` | `C6E1F17CF5A28B6964B2F528B40B7C72B46D79AEE7A2D67071E181F95AFE993F` |

Line-ending/BOM census was measured on the same raw-byte scope. It is descriptive,
not a normalization rewrite: Server `LF=593`, `CRLF=31`, `MIXED=52`, UTF-8 BOM
files `62`; Agent `LF=64`, `MIXED=6`, UTF-8 BOM files `5`. No bare-CR file was
found. Exact pre/post fingerprints prove the test executions changed none of
these representations.

## 7. Repository state and disposition

Both repositories have:

```text
staged     0
conflicted 0
```

The Server and Agent worktrees remain intentionally dirty/untracked with the A3
candidate and preserved unrelated work. No cleanup, stage, commit or push was
performed.

**Builder disposition:** `A3_IMPLEMENTATION_CANDIDATE_COMPLETE`.

This is not reviewer PASS and not Homeowner ratification. Landing remains closed
until independent review of the exact candidate and this packet. A4 and production
remain outside scope.
