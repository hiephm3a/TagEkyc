# A3 continuous batch — fixture-corrected READINESS P24/P25 + P10

This packet **supersedes v1**, which is retained as a historical HOLD artifact. Request only a bounded review of the two product corrections and P10 component proof. No P-row is proposed for closure. E01 remains ratified 10/10; A3 remains HOLD with **65 normative + 1 seam = 66** open. No full-suite, landing, stage/commit/push or production claim.

## Source and fixture freeze

- `src/TagEkyc.Api/Program.cs`: SHA-256 `FD5610C1981BA0AB33C18E586AD691AA2106795A9A6FB9838C95BB97CF91CF40`. Its four upper bounds remain 32/256/2,147,483,647/16,777,216; existing positive/relationship guards and the capacity primitive are unchanged.
- `src/TagEkyc.Infrastructure/RawExport/CaptureRuntimeRawIngressComposition.cs`: SHA-256 `4923D50B21EBC1537DCDAC427C03293FDF1EEEF4836994FEC79B535E5C1F2508`. Activated readiness requires the **resolved `CustodyTimeBoundsState.Value`**; Prepared remains lazy. This does not invoke another validator.
- `tests/TagEkyc.IntegrationTests/Tip88C1C6BA3BrokerPipelineTests.cs`: SHA-256 `2C9E5599E1972327836F59DF5574D8359E068E2FAF55591FA74D7403711C5C70`. The real `WebApplicationFactory<Program>` positive host now uses ratified synthetic capacity per-producer=1, per-deployment=2, aggregate window=2,097,152, per-stream window=1,048,576; its three related `RuntimeOwners` windows are 1,048,576, matching active `ChunkSize=1,048,576`. The relationship negative is now producer=3/deployment=2. The earlier 1/1/1024/1024 positive fixture in v1 must not be cited as a contract-valid Activated control.
- P10 A3 migration remains SHA-256 `91DD833A50E834145D34501F0C793D987A5A34C31CC011402D7DEAF7A485552A`; its test source remains `2706F9C18F0F32F5CE87BD2A739DF034F2A2084AA98D6ABB7001B01CCA290DCB`. P10 was not rerun for this fixture-only correction.

## Current-byte discriminating evidence

All four runs below rebuilt from source (no restoration run used `--no-build`). TRX are under `tests/TagEkyc.IntegrationTests/TestResults/`.

| Evidence | SHA-256 | Counter and target |
| --- | --- | --- |
| `a3-readiness-fixture-corrected-focused-green.trx` | `6C2EF71DC87DDE28BC95CA40B5199BC497BFD39A67E95E04C23D6DBAF15375BB` | P24/P25 2/2 GREEN before mutants |
| `a3-readiness-fixture-p25-upper-red.trx` | `ECB11F6F2C58FD4C1DF6317696B5B97F6258208D3DF4FDC50BF833BB9B6E3433` | 0/1 RED: removing `value <= maximum` from `Program.cs` makes upper-bound case fail `Assert.ThrowsAny` at line 641, called from upper loop line 675; no exception was thrown |
| `a3-readiness-fixture-p24-timebounds-red.trx` | `62EEE0A65233FA8BBD85969B1322481FEB79856E826D8055E3631CBB563840F0` | 0/1 RED: bypassing the Activated time-bounds gate makes startup-denial assertion line 732 fail; no exception was thrown |
| `a3-readiness-fixture-corrected-final-joined.trx` | `5B8BF02887A199D4BEBA8C320CDD5EDAA256D232C9B4578F952584003D14EB35` | 6/6 GREEN after byte-exact restoration; includes P24/P25, Prepared, positive Activated and missing-owner negative controls |

P25 one-line mutant source SHA-256 was `BFD82FE8242C96EB560507ADC42152F89CAD9CB37A8555231EBB3EE81A5972BB`; it was restored to the Program freeze before P24 mutation. P24 one-line mutant composition SHA-256 was `8417421B4BD6A0EF75A6F863425E209D4DCD168284DFA059CBDF5C14FF4745C2`; it was restored to the composition freeze before final joined GREEN. The historical RED locators at line 729 in v1 belong to test SHA `B4B65BB8…`; line 732 above belongs to the corrected test SHA.

## Preserved P10 evidence and scope

The earlier P10 real broker/PG control remains valid because neither its migration nor test source changed. Baseline `a3-broker-p10-current-baseline-green.trx` SHA-256 `D3D74C971E2A8C412CB9D3C79C60BCDD5DCA05516D227762AE3CECD5C665215C` was 1/1. A one-line SQL mutant substituted `issued_at` for the Active alias's persisted `CurrentTokenExpiresAtUtc` (mutant migration SHA-256 `5E7A96E9D4975A79CA34841DFE3AEF007E6145E95243CBA62FFF5DB22B856904`); `a3-broker-p10-persisted-retry-red.trx` SHA-256 `EA6A97360B6D0E32F3E0F15839DEAB9F74A200B2E6EDC1C4059EE79F0CF87DDD` was 0/1 at the exact Final-result timestamp assertion. After byte restoration/rebuild, `a3-broker-p10-persisted-retry-restored-green.trx` SHA-256 `CA98BFC8F02FE865C7324CAD9EA2FBAFFD0F2879B6F512749E273424EE454FD8` was 1/1. P10 still lacks public HTTP/body-read and Agent custody proof.

## Drift and unresolved work

The ratified E01 manifest (`F7F3BA68D0C7EC898A80211C804BE8A3CFBE75F772D6F06A51A77B2873E3A4C0`) is the comparison baseline. The successor `a3_continuous_batch_candidate_manifest.tsv` inventories **1,609 files** and verifies **399/399 ledger-referenced TRX existing/in manifest**, none omitted. The declared delta is **20 ADDED / 7 MODIFIED / 0 DELETED**: additions comprise v1/v2 packets, four prior manifest/sidecar files and 14 focused TRX; modifications comprise the three source/test paths above, the route matrix, reconciliation, mutant registry and mechanically regenerated TRX resolution inventory. No Agent source changed. The mutant registry has **55 distinct hashes**, with **0 live matches across all 1,609 byte-verified manifest files**; only the P25 mutant hash is new in this correction, because the P24 mutant hash was already registered. Both repositories are staged/conflicted **0/0**.

BP19's route-matrix row is corrected from `BROKER_CLAIM` to `R2_R6` because the binding companion defines a writer/verifier/R3/R4/R5/read/complete/finalize stage-enum projection. BP19 remains open; this is matrix bookkeeping, not product implementation. The P25 framing rule is specified at admission (`window >= min(active ChunkSize, declared body)`); implementation/proof remains open, **not** a Homeowner interpretation choice. P12, O03–O05 and P26–P28 remain unresolved. No synthetic broker Final or fixture provider is introduced to make those rows green.
