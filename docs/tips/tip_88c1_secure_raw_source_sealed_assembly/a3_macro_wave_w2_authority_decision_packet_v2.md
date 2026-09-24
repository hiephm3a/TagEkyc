# A3 macro-wave W2 — authority decision correction v2

**Status:** HOLD — one Homeowner scope decision required  
**Date:** 2026-09-20  
**Scope:** all 14 W2 rows as one decision  
**Supersedes:** the disposition, not the factual audit, in `a3_macro_wave_w2_authority_decision_packet_v1.md`

## Correction

The v1 packet exceeded builder authority by converting conditional behavior specifications into a delivery mandate. Planning Brief §10.0/§10.2 and the broker/assembly contracts describe the required behavior **if** the relevant shipping workflow is implemented. They do not independently authorize implementation in this delivery. The same Planning Brief still carries `IMPLEMENTATION BLOCKED` and `NOT AUTHORIZED FOR IMPLEMENTATION`; no later in-file authorization was found.

The 14-row audit remains valid as a description of:

- the required behavior if the paths ship;
- the present producer/caller/reachability gaps;
- the exact implementation exit conditions.

Its decision column is therefore corrected to `AWAIT_HOMEOWNER_SCOPE_DECISION`. Partition status returns to `W2_AUTHORITY_DECISION`.

## Single decision for all 14 rows

| Choice | Effect |
| --- | --- |
| `WIRE_PRODUCT` | Authorize one horizontal implementation wave across provider-unknown, O03–O05 producers, P12/P13 reachability and the assembly shipping path. Rows remain open until code, mutations and joined proof pass. |
| `PARK_WITH_A3` | Make no production/test implementation for these 14 rows now. Keep them authority-open/blocking, freeze the audit, and move engineering effort back to the active eKYC delivery. A3 remains HOLD and cannot claim overall PASS. |

No mixed or row-wise choice is requested. Census remains **51 = 50 normative + 1 seam** under either choice until a later ratified reclassification or technical closure.
