# A3 product-complete / site-qualification scope successor

Date: 2026-09-23. The Homeowner directed that development must not require a real hospital and that the four Expect requirements are operational deployment qualification flags, backed by tooling that can bind a real hospital later. This decision does not claim the development loopback is a hospital and does not waive qualification for any production site.

A3-Scope-Decision: APPROVED
A3-Authority-Open-After: 0
A3-Activated-Includes-Assembly: NO
A3-Assembly-Topology: Disabled
A3-Ownership-Table-SHA256: 2DA0A4C5586C54FBED422B7474EE72DC0B224D3A150B6E54C14382D4C03E962F
A3-Partition-SHA256: BE0C0E1A2B82BF4B9E8B10387F58D4589A71233DCD3483573B6C344D6798B136
A3-Ledger-SHA256: 6250CC537ED67F6FA3C0BC7795065455BEA591C3FC0271B17B6759B6EC54A2C4

The product activation census is zero. The four prior `TRANSPORT-EXPECT-FENCE` rows are `PRODUCT COMPLETE / SITE QUALIFICATION REQUIRED`, not `HOMEOWNER_RATIFIED` and not silently deferred. Their common mechanism has joined runtime proof; the remaining fact is specific to each deployed network.

Seal policy version 1 requires operational site qualification whenever product census is zero. The seal binds the requirement and policy version, not a hospital record SHA. Runtime configuration binds exact `siteId`, HTTPS origin and deployment revision. A matching current PASS record is re-evaluated at startup and on every raw-ingress request. Missing, mismatched, failed or expired evidence blocks raw ingress. Renewal uses atomic replacement and requires no restart.

The development harness must retain `development-loopback` identity and may not impersonate a site. `PrepareSite` emits configuration only and explicitly does not qualify it. A site record may be installed only when all exact fields and zero-early-body/one-POST assertions match the configured deployment.

Assembly remains excluded with topology `Disabled`; the 38-row deferred backlog is unchanged. Product completion does not authorize an unqualified hospital deployment, stage, commit, push or landing.
