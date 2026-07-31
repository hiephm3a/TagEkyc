# TIP-88C1-A — As-Built: Capture-Acceptance Surface

Maps the landed C1-A code to the planning brief. Records what SQL actually
enforces, the deliberate Path-A boundary, and the gate it opened. Not a design
doc — the design is `tip_88c1_planning_brief.md` §6.1; this is the code that
shipped.

- **Commit:** `d923a06` (branch `tip-88a-raw-export-policy-catalog-build`, unpushed).
- **Brief anchor:** §6.1 (acceptance surface = DB-enforced authority root).
- **Migration:** `20260730090000_Tip88C1ACaptureAcceptanceSurface.cs`.

## Landed surface

Two append-only tables (schema `tagekyc`):

| Table | PK | Uniqueness | FKs (all `RESTRICT`) |
|---|---|---|---|
| `raw_export_capture_acceptance_events` | `CaptureAcceptanceId` | `uq_..._artifact` = (VerificationSessionId, RawClass, CaptureArtifactId, CaptureRevision); `uq_..._revision` = (VerificationSessionId, RawClass, CaptureRevision) | artifact → `capture_artifacts.Id`; session → `verification_sessions.Id` |
| `raw_export_session_capture_selections` | `SessionCaptureSelectionId` | `uq_..._class` = (VerificationSessionId, RawClass) | acceptance → `raw_export_capture_acceptance_events.CaptureAcceptanceId` |

Two `SECURITY DEFINER` functions (owner `tagekyc_raw_export_deployer`,
`search_path = pg_catalog`), `EXECUTE` granted to `tagekyc_runtime`:

- `raw_export_append_capture_acceptance(uuid,uuid,text,uuid,integer,text,text,text,integer) → uuid`
- `raw_export_select_session_capture_acceptance(uuid,text,uuid) → uuid`

Integrity mechanics (all always-on, role-independent):

- **Actor** from `raw_export_current_actor()` (transaction-local GUC, fail-closed).
- **Artifact↔session bind:** `capture_artifacts.VerificationSessionId` must equal the asserted session → else `RAW_EXPORT_CAPTURE_ARTIFACT_SESSION_MISMATCH`.
- **Monotonic revision:** advisory xact lock per `(session, RawClass)`, then `CaptureRevision` must equal `MAX(existing)+1` and `>= 1` → else `RAW_EXPORT_CAPTURE_ACCEPTANCE_REVISION_CONFLICT`.
- **One selection per `(session, RawClass)`** via `uq_..._class`.
- **Append-only:** `deny_append_only_mutation()` trigger on UPDATE/DELETE both tables.
- **No direct DML:** `enforce_raw_export_capture_acceptance_insert()` requires `current_user = deployer` + the intended append-context GUC (`event`/`selection`) → else `RAW_EXPORT_CAPTURE_ACCEPTANCE_DIRECT_INSERT_UNSUPPORTED`.

## Deliberate deviation from brief §6.1 — Path A

Brief §6.1 assigns component-level ownership/challenge and class-specific
artifact-identity validation to this surface. The DB **cannot** prove two of
those on the landed capture schema, so C1-A split the authority (Homeowner
chose Path A):

- **SQL proves:** session/artifact FK, artifact↔session bind, positive monotonic revision, append-only, one selection per `(session, RawClass)`, actor GUC.
- **Caller asserts (SQL does not prove):** `SessionChallengeHash` (computed in C# with `HashCanonical("tip-69-capture-session-challenge", …)`, no DB source); and, for NFC-derived classes, `RawClass` itself, because landed `capture_artifacts.ArtifactType` only records the aggregate `NfcReadArtifact` and cannot distinguish DG1/DG2/DG13/DG15/SOD/AA.

## Gate opened

**`C1-A-CLASS-PROVENANCE-GATE`** (debt registry) — close only when the capture
layer records independently verifiable class-specific artifact identity for
NFC-derived classes and the C1 acceptance producer verifies it. Brief §7/D2
additionally needs a class-specific digest for `ChipDg2Portrait`;
`LiveSelfieImage` is not blocked by this gap.

## Verification at landing

Per-project test isolation green; append-only + revision-conflict + direct-DML
paths mutation-proven. ModelSnapshot updated for the additive tables.
