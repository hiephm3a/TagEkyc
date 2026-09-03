#!/usr/bin/env python3
"""Executable normative state model for DK-PROD.

This file is documentation tooling.  It does not connect to a database or
authorize implementation.  It makes the transition/pair/sparse-state census
mechanically reproducible instead of counting duplicated Markdown by hand.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import re
import sys


NULL = "null"
SET = "set"
EITHER = "either"
ZERO = "zero"
POSITIVE = "positive"
EITHER_COUNT = "either_count"
FALSE = "false"
TRUE = "true"
EITHER_BOOL = "either_bool"

FIELDS = (
    "prep",
    "lease",
    "token",
    "resolution_deadline",
    "resolution_next",
    "cleanup_deadline",
    "cleanup_next",
    "cleanup_intervention",
    "resolution_count",
    "cleanup_count",
    "wrapped_head",
    "prepared_at",
    "revoked_at",
)


@dataclass(frozen=True)
class Transition:
    id: str
    predecessor: tuple[str, str]
    successor: tuple[str, str]
    set_fields: tuple[str, ...] = ()
    clear_fields: tuple[str, ...] = ()
    overrides: tuple[tuple[str, str], ...] = ()
    note: str = ""


# Pair-specific destination shapes.  A head-only matrix is insufficient for AR:
# AR/CR is actively cleaning, while AR/CU and AR/AP have completed cleanup.
SHAPES: dict[tuple[str, str], dict[str, str]] = {
    ("none", "none"): {f: NULL for f in FIELDS} | {
        "resolution_count": ZERO, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("PL", "IS"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "resolution_deadline": EITHER, "resolution_next": EITHER,
        "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("PL", "RO"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("PE", "IS"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "resolution_deadline": SET, "resolution_next": EITHER,
        "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("POU", "IS"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "resolution_deadline": SET, "resolution_next": SET,
        "resolution_count": POSITIVE, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("PC", "IS"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "resolution_deadline": EITHER, "resolution_next": EITHER,
        "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("PCR", "CR"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "cleanup_deadline": SET, "cleanup_next": EITHER, "cleanup_intervention": EITHER_BOOL,
        "resolution_count": EITHER_COUNT, "cleanup_count": EITHER_COUNT,
    },
    ("RF", "CU"): dict.fromkeys(FIELDS, NULL) | {"resolution_count": EITHER_COUNT, "cleanup_count": EITHER_COUNT, "cleanup_intervention": FALSE},
    ("RF", "AP"): dict.fromkeys(FIELDS, NULL) | {"resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE},
    ("AC", "RO"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "wrapped_head": SET, "prepared_at": SET,
        "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("RV", "RO"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "wrapped_head": SET, "prepared_at": SET, "revoked_at": SET,
        "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE,
    },
    ("AR", "IS"): dict.fromkeys(FIELDS, NULL) | {"prep": SET, "lease": SET, "token": SET, "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE},
    ("AR", "RO"): dict.fromkeys(FIELDS, NULL) | {"prep": SET, "lease": SET, "token": SET, "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE},
    ("AR", "CR"): dict.fromkeys(FIELDS, NULL) | {
        "prep": SET, "lease": SET, "token": SET,
        "cleanup_deadline": SET, "cleanup_next": EITHER, "cleanup_intervention": EITHER_BOOL,
        "resolution_count": EITHER_COUNT, "cleanup_count": EITHER_COUNT,
    },
    ("AR", "CU"): dict.fromkeys(FIELDS, NULL) | {"prep": SET, "lease": SET, "token": SET, "resolution_count": EITHER_COUNT, "cleanup_count": EITHER_COUNT, "cleanup_intervention": FALSE},
    ("AR", "AP"): dict.fromkeys(FIELDS, NULL) | {"prep": SET, "lease": SET, "token": SET, "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE},
    ("RA", "CU"): dict.fromkeys(FIELDS, NULL) | {"prep": SET, "token": SET, "resolution_count": EITHER_COUNT, "cleanup_count": EITHER_COUNT, "cleanup_intervention": FALSE},
    ("RA", "AP"): dict.fromkeys(FIELDS, NULL) | {"prep": SET, "token": SET, "resolution_count": EITHER_COUNT, "cleanup_count": ZERO, "cleanup_intervention": FALSE},
}


def t(id_: str, pred: tuple[str, str], succ: tuple[str, str], *, set_: tuple[str, ...] = (), clear: tuple[str, ...] = (), values: tuple[tuple[str, str], ...] = (), note: str = "") -> Transition:
    return Transition(id_, pred, succ, set_, clear, values, note)


# Every entry has one predecessor and one successor.  Rejections/replays that do
# not introduce a new durable pair are intentionally outside this reachability
# relation and remain ordinary function outcomes in the brief.
TRANSITIONS = (
    t("T1", ("none", "none"), ("PL", "IS"), set_=("prep", "lease", "token"), values=(("resolution_count", ZERO), ("cleanup_count", ZERO), ("cleanup_intervention", FALSE))),
    t("T2a", ("RF", "CU"), ("PL", "IS"), set_=("prep", "lease", "token"), values=(("resolution_count", ZERO), ("cleanup_count", ZERO), ("cleanup_intervention", FALSE))),
    t("T2b", ("RF", "AP"), ("PL", "IS"), set_=("prep", "lease", "token"), values=(("resolution_count", ZERO), ("cleanup_count", ZERO), ("cleanup_intervention", FALSE))),
    t("T4", ("PL", "IS"), ("PL", "RO"), clear=("resolution_deadline", "resolution_next")),
    t("T5", ("PL", "RO"), ("AC", "RO"), set_=("wrapped_head", "prepared_at"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T6", ("PL", "IS"), ("PE", "IS"), set_=("resolution_deadline",)),
    t("T7a", ("PL", "IS"), ("POU", "IS"), set_=("resolution_deadline", "resolution_next"), values=(("resolution_count", POSITIVE),)),
    t("T7b", ("PE", "IS"), ("POU", "IS"), set_=("resolution_next",), values=(("resolution_count", POSITIVE),)),
    t("T8", ("POU", "IS"), ("POU", "IS"), set_=("resolution_next",), values=(("resolution_count", POSITIVE),)),
    t("T9", ("PL", "IS"), ("PL", "IS"), set_=("resolution_deadline", "resolution_next"), values=(("resolution_count", POSITIVE),)),
    t("T10", ("PE", "IS"), ("PE", "IS"), set_=("resolution_next",), values=(("resolution_count", POSITIVE),)),
    t("T11", ("POU", "IS"), ("POU", "IS"), set_=("resolution_next",), values=(("resolution_count", POSITIVE),)),
    t("T12a", ("PL", "IS"), ("PC", "IS")),
    t("T12b", ("PE", "IS"), ("PC", "IS")),
    t("T12c", ("POU", "IS"), ("PC", "IS")),
    t("T13", ("PE", "IS"), ("RF", "AP"), clear=("prep", "lease", "token", "resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T14a", ("PL", "IS"), ("PCR", "CR"), set_=("cleanup_deadline",), clear=("resolution_deadline", "resolution_next")),
    t("T14b", ("PE", "IS"), ("PCR", "CR"), set_=("cleanup_deadline",), clear=("resolution_deadline", "resolution_next")),
    t("T14c", ("POU", "IS"), ("PCR", "CR"), set_=("cleanup_deadline",), clear=("resolution_deadline", "resolution_next")),
    t("T15", ("PCR", "CR"), ("PCR", "CR"), set_=("cleanup_next",), values=(("cleanup_count", POSITIVE),)),
    t("T16", ("PCR", "CR"), ("PCR", "CR"), set_=("cleanup_next",), values=(("cleanup_count", POSITIVE),)),
    t("T17", ("PCR", "CR"), ("PCR", "CR"), set_=("cleanup_next",), values=(("cleanup_count", POSITIVE),)),
    t("T18", ("PCR", "CR"), ("RF", "CU"), clear=("prep", "lease", "token", "resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next"), values=(("cleanup_intervention", FALSE),)),
    t("T19", ("PCR", "CR"), ("RF", "CU"), clear=("prep", "lease", "token", "resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next"), values=(("cleanup_intervention", FALSE),)),
    t("T20a", ("PL", "IS"), ("AC", "RO"), set_=("wrapped_head", "prepared_at"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T20b", ("PE", "IS"), ("AC", "RO"), set_=("wrapped_head", "prepared_at"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T20c", ("POU", "IS"), ("AC", "RO"), set_=("wrapped_head", "prepared_at"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T21a", ("PL", "IS"), ("AR", "IS"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T21b", ("PL", "RO"), ("AR", "RO"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T21c", ("PE", "IS"), ("AR", "IS"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T21d", ("POU", "IS"), ("AR", "IS"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T21e", ("PCR", "CR"), ("AR", "CR")),
    t("T22a", ("AR", "IS"), ("AR", "CR"), set_=("cleanup_deadline",)),
    t("T22b", ("AR", "RO"), ("AR", "CR"), set_=("cleanup_deadline",)),
    t("T23", ("AR", "CR"), ("AR", "CR"), set_=("cleanup_next",), values=(("cleanup_count", POSITIVE),)),
    t("T24", ("AR", "CR"), ("AR", "CR"), set_=("cleanup_next",), values=(("cleanup_count", POSITIVE),)),
    t("T25", ("AR", "CR"), ("AR", "CR"), set_=("cleanup_next",), values=(("cleanup_count", POSITIVE),)),
    t("T26", ("AR", "CR"), ("AR", "CU"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next"), values=(("cleanup_intervention", FALSE),)),
    t("T27", ("AR", "CR"), ("AR", "CU"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next"), values=(("cleanup_intervention", FALSE),)),
    t("T28", ("AR", "IS"), ("AR", "AP"), clear=("resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T29", ("AR", "CU"), ("RA", "CU"), clear=("lease", "resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T30", ("AR", "AP"), ("RA", "AP"), clear=("lease", "resolution_deadline", "resolution_next", "cleanup_deadline", "cleanup_next")),
    t("T32", ("AC", "RO"), ("RV", "RO"), set_=("revoked_at",)),
    t("T33a", ("PE", "IS"), ("PC", "IS")),
    t("T33b", ("POU", "IS"), ("PC", "IS")),
    t("T35", ("PCR", "CR"), ("PCR", "CR"), values=(("cleanup_intervention", TRUE),)),
)

FUTURE_GATE = "DK-PROD-DURABLE-CONFIG-SNAPSHOT-V1"
FUTURE_TRANSITIONS = {
    FUTURE_GATE: (
        t("T34a", ("PE", "IS"), ("PC", "IS")),
        t("T34b", ("POU", "IS"), ("PC", "IS")),
    ),
}

TESTS = {
    "transition": (
        *(str(i) for i in range(1, 9)), "9a", "9b", "9c",
        *(str(i) for i in range(10, 22)),
        *(f"22{x}" for x in "abcdefghijk"),
        *(str(i) for i in range(23, 33) if i != 26),
    ),
    "finding_mutation": ("17b", "51", "53", "54"),
    "guard_integrity_mutation": ("34", "35", "36", "56", "58a", "58b"),
    "fk_mutation": ("33",),
    "catalog": ("17c", "20b", "37", "49", "55a", "55b", "55c", "57"),
    "positive": tuple(str(i) for i in range(38, 49)),
    "migration": ("50",),
    "execution_closure": ("59", "60", "61", "62"),
}

FUTURE_TESTS = {
    FUTURE_GATE: ("26",),
}

SQL_FUNCTIONS = {
    "raw_export_prepare_attempt_key_reservation", "raw_export_record_key_provider_wrapped_result",
    "raw_export_activate_attempt_key_reservation", "raw_export_mark_attempt_key_preparation_expired",
    "raw_export_resolve_attempt_key_provider_outcome", "raw_export_mark_key_provider_cleanup_required",
    "raw_export_record_key_provider_cleanup_observation", "raw_export_acknowledge_key_provider_cleanup",
    "raw_export_record_recovered_key_provider_result", "raw_export_request_abandon_attempt_key_reservation",
    "raw_export_finalize_abandon_attempt_key_reservation", "raw_export_revoke_attempt_key_reservation",
    "raw_export_inspect_attempt_key_reservation", "raw_export_read_current_attempt_key_recovery_context",
    "raw_export_read_active_attempt_key_envelope",
}

ALLOWLIST = {
    "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_prod_state_model.py",
    "src/TagEkyc.Contracts/RawExport/AttemptKeyReservationContracts.cs",
    "src/TagEkyc.Contracts/RawExport/AttemptAeadOperationContracts.cs",
    "src/TagEkyc.Infrastructure/RawExport/KekOperationContracts.cs",
    "src/TagEkyc.Infrastructure/RawExport/KekProvisioningRecoveryOperation.cs",
    "src/TagEkyc.Infrastructure/RawExport/PostgresAttemptKeyReservationProvider.cs",
    "src/TagEkyc.Infrastructure/RawExport/PostgresKeyProviderOperationMap.cs",
    "src/TagEkyc.Infrastructure/RawExport/AttemptKeyRecoveryContextReader.cs",
    "src/TagEkyc.Infrastructure/RawExport/AttemptAeadOperationService.cs",
    "src/TagEkyc.Infrastructure/RawExport/AttemptDekLease.cs",
    "src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyServiceCollectionExtensions.cs",
    "src/TagEkyc.Infrastructure/RawExport/DurableKeyCustodyOptions.cs",
    "src/TagEkyc.Infrastructure/RawExport/DurableKeyTopologyOptions.cs",
    "src/TagEkyc.Infrastructure/RawExport/CustodyRoleReadinessValidator.cs",
    "src/TagEkyc.Infrastructure/RawExport/DurableKeyProviderReadinessValidator.cs",
    "src/TagEkyc.Infrastructure/RawExport/KeyReservationReadinessValidator.cs",
    "src/TagEkyc.Infrastructure/RawExport/CsprngReadinessValidator.cs",
    "src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyReservationRow.cs",
    "src/TagEkyc.Infrastructure/Persistence/Entities/RawExportAttemptKeyPreparationEventRow.cs",
    "src/TagEkyc.Infrastructure/Persistence/Entities/RawExportKeyProviderOperationRow.cs",
    "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAttemptKeyReservationConfig.cs",
    "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportAttemptKeyPreparationEventConfig.cs",
    "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportKeyProviderOperationConfig.cs",
    "src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs",
    "src/TagEkyc.Infrastructure/Persistence/Migrations/{UTCyyyyMMddHHmmss}_Tip88C1B2DurableKeyProd.cs",
    "src/TagEkyc.Infrastructure/Persistence/Migrations/{UTCyyyyMMddHHmmss}_Tip88C1B2DurableKeyProd.Designer.cs",
    "src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs",
    "src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj",
    "src/TagEkyc.Api/Program.cs", "src/TagEkyc.Api/ReadinessEndpoint.cs", "src/TagEkyc.Api/appsettings.json",
    "tests/TagEkyc.IntegrationTests/Tip88C1B2DurableKeyProdTests.cs",
    "tests/TagEkyc.ArchTests/Tip88C1B2DurableKeyProdArchTests.cs",
    "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_b2_durable_key_prod_as_built.md",
    "docs/phase1_scope_and_debt_registry_v0_1.md",
    "tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs",
}


def audit_test_ids(test_ids: list[str]) -> list[str]:
    duplicates = sorted({x for x in test_ids if test_ids.count(x) > 1})
    return [f"duplicate test IDs: {duplicates}"] if duplicates else []


def audit_future_partition(
    active_transition_ids: set[str],
    future_transitions: dict[str, tuple[Transition, ...]],
    active_test_ids: set[str],
    future_tests: dict[str, tuple[str, ...]],
) -> list[str]:
    errors: list[str] = []
    if set(future_transitions) != {FUTURE_GATE}:
        errors.append(f"future transition gates mismatch: {sorted(future_transitions)}")
    if set(future_tests) != {FUTURE_GATE}:
        errors.append(f"future test gates mismatch: {sorted(future_tests)}")
    future_transition_ids = [edge.id for edges in future_transitions.values() for edge in edges]
    future_test_ids = [test_id for tests in future_tests.values() for test_id in tests]
    if future_transition_ids != ["T34a", "T34b"]:
        errors.append(f"future transition IDs mismatch: {future_transition_ids}")
    if future_test_ids != ["26"]:
        errors.append(f"future test IDs mismatch: {future_test_ids}")
    if active_transition_ids.intersection(future_transition_ids):
        errors.append("active/future transition IDs overlap")
    if active_test_ids.intersection(future_test_ids):
        errors.append("active/future test IDs overlap")
    if not {"T33a", "T33b"}.issubset(active_transition_ids):
        errors.append("T33 deadline transitions are not active")
    for test_id in (str(i) for i in range(27, 63)):
        if test_id != "52" and not any(
            candidate == test_id or candidate.startswith(test_id)
            for candidate in active_test_ids
        ):
            # 52 was not part of the pre-existing active manifest.
            errors.append(f"reserved active test identifier missing or renumbered: {test_id}")
    return errors


def audit_brief(brief: str, expected_pairs: set[tuple[str, str]], expected_tests: set[str]) -> list[str]:
    errors: list[str] = []
    normalized = " ".join(brief.split())
    required_claims = (
        "BUILD DISPATCH (v0.5)", "exact reachable pairs = 18", "active test methods **78**",
        "mutating_edges=46", FUTURE_GATE,
        "callable functions **15**", "owner-only helpers **2**", "trigger instances **5**",
        "unique/FK indexes **7**", "active DK-PROD readiness codes **21**",
        "(+3 legacy fixture, separate)", "evidence domains **8**", "golden vectors **8**",
        "authorized files **36**",
    )
    for claim in required_claims:
        if claim not in normalized:
            errors.append(f"brief is missing generated claim: {claim!r}")
    for stale in (
        "40 ungrouped rows", "active test methods **74**", "active test methods **75**",
        "active test methods **79**", "mutating_edges=48", "48 individually named mutating edges",
        "18-pair head CHECK", "authorized files **34**", "callable functions **14**",
    ):
        if stale in brief:
            errors.append(f"stale hand-maintained claim remains: {stale!r}")

    sql_rows = [line for line in brief.splitlines() if line.startswith("| `raw_export_")]
    if len(sql_rows) != 15:
        errors.append(f"SQL manifest rows={len(sql_rows)}, expected 15")
    for row in sql_rows:
        if row.count("|") != 6:
            errors.append(f"SQL manifest row arity != 5: {row[:100]}")
    function_names = {m.group(1) for row in sql_rows if (m := re.match(r"\| `([a-z0-9_]+)\(", row))}
    if function_names != SQL_FUNCTIONS:
        errors.append(f"SQL function set mismatch: missing={sorted(SQL_FUNCTIONS-function_names)} extra={sorted(function_names-SQL_FUNCTIONS)}")

    m8 = re.search(r"(?s)## M8\..*?(?=## M9\.)", brief)
    paths = re.findall(r"`((?:src|tests|docs)/[^`]+)`", m8.group(0) if m8 else "")
    if set(paths) != ALLOWLIST or len(paths) != len(set(paths)):
        errors.append(f"allowlist mismatch: missing={sorted(ALLOWLIST-set(paths))} extra={sorted(set(paths)-ALLOWLIST)} duplicates={len(paths)-len(set(paths))}")

    m6 = re.search(r"(?s)## M6\..*?(?=### M6f\.)", brief)
    listed_tests = set(re.findall(r"#([0-9]+[a-z]?)", m6.group(0) if m6 else ""))
    if listed_tests != expected_tests:
        errors.append(f"M6 test set mismatch: missing={sorted(expected_tests-listed_tests)} extra={sorted(listed_tests-expected_tests)}")

    future_m6 = re.search(r"(?s)### M6f\..*?(?=\*\*Exact active test-method census)", brief)
    listed_future_tests = set(re.findall(r"FUTURE_GATE #([0-9]+[a-z]?)", future_m6.group(0) if future_m6 else ""))
    if listed_future_tests != {"26"}:
        errors.append(f"future M6 test set mismatch: {sorted(listed_future_tests)}")
    if FUTURE_GATE not in (future_m6.group(0) if future_m6 else ""):
        errors.append("future M6 gate identifier missing")

    active_operational = re.search(r"(?s)## 1\. Operational transition summary.*?(?=### Future-gated transition)", brief)
    summary_ids = {m.group(1) for line in (active_operational.group(0) if active_operational else "").splitlines() if (m := re.match(r"\| T(\d+) \|", line))}
    expected_summary_ids = {str(i) for i in range(1, 41)} - {"34"}
    if summary_ids != expected_summary_ids:
        errors.append(f"operational transition rows mismatch: missing={sorted(expected_summary_ids-summary_ids)} extra={sorted(summary_ids-expected_summary_ids)}")
    future_operational = re.search(r"(?s)### Future-gated transition.*?(?=### 1a\.)", brief)
    future_text = future_operational.group(0) if future_operational else ""
    future_rows = [line for line in future_text.splitlines() if line.startswith("| T34 |")]
    if len(future_rows) != 1 or FUTURE_GATE not in future_text or "MaxAttemptExceeded" not in future_text or "#26" not in future_text:
        errors.append("future operational T34 manifest mismatch")

    current_resolve_row = next((row for row in sql_rows if row.startswith("| `raw_export_resolve_attempt_key_provider_outcome")), "")
    if "MaxAttemptExceeded" in current_resolve_row or "T34" in current_resolve_row:
        errors.append("current resolve callable still exposes future-gated MaxAttemptExceeded/T34")

    j1 = re.search(r"(?s)### J1 .*?(?=### J2)", brief)
    legal: set[tuple[str, str]] = set()
    if j1:
        rows = [line for line in j1.group(0).splitlines() if line.startswith("|")]
        header = [x.strip() for x in rows[0].strip("|").split("|")][1:]
        mapping_abbrev = {
            "none": "none", "Issued": "IS", "ResultObserved": "RO",
            "CleanupRequired": "CR", "CleanedUp": "CU", "AbsenceProven": "AP",
        }
        for row in rows[2:]:
            cells = [x.strip() for x in row.strip("|").split("|")]
            for mapping, marker in zip(header, cells[1:]):
                if marker == "L":
                    legal.add((cells[0], mapping_abbrev[mapping]))
    if legal != expected_pairs:
        errors.append(f"J1 pair mismatch: missing={sorted(expected_pairs-legal)} extra={sorted(legal-expected_pairs)}")

    t5 = next((line for line in brief.splitlines() if line.startswith("| T5 |")), "")
    if "(PL,RO)→(AC,RO)" not in t5:
        errors.append("operational T5 pair drift")
    return errors


def run_self_tests(brief: str, expected_pairs: set[tuple[str, str]], expected_tests: set[str]) -> None:
    mutations = {
        "illegal_j1_pair": brief.replace("| PE | X | L | X |", "| PE | X | L | L |", 1),
        "transition_pair_drift": brief.replace("(PL,RO)→(AC,RO)", "(PE,RO)→(AC,RO)", 1),
        "missing_transition": "\n".join(line for line in brief.splitlines() if not line.startswith("| T5 |")) + "\n",
        "extra_allowlist_path": brief.replace("## M9.", "`src/Extra.cs`\n\n## M9.", 1),
        "manifest_arity": brief.replace("| `raw_export_prepare_attempt_key_reservation", "| EXTRA | `raw_export_prepare_attempt_key_reservation", 1),
    }
    for name, mutant in mutations.items():
        if not audit_brief(mutant, expected_pairs, expected_tests):
            raise SystemExit(f"ERROR: self-test stayed GREEN: {name}")
        print(f"SELF_TEST_RED {name}")
    ids = list(expected_tests) + [next(iter(expected_tests))]
    if not audit_test_ids(ids):
        raise SystemExit("ERROR: self-test stayed GREEN: duplicate_test_id")
    print("SELF_TEST_RED duplicate_test_id")
    census_mutant = brief.replace("mutating_edges=46", "mutating_edges=48", 1)
    if not audit_brief(census_mutant, expected_pairs, expected_tests):
        raise SystemExit("ERROR: self-test stayed GREEN: stale_mutating_edge_census")
    print("SELF_TEST_RED stale_mutating_edge_census")
    test_census_mutant = brief.replace("active test methods **78**", "active test methods **79**", 1)
    if not audit_brief(test_census_mutant, expected_pairs, expected_tests):
        raise SystemExit("ERROR: self-test stayed GREEN: stale_active_test_census")
    print("SELF_TEST_RED stale_active_test_census")
    future_edge = FUTURE_TRANSITIONS[FUTURE_GATE][0]
    if not audit_future_partition(
        {edge.id for edge in TRANSITIONS} | {future_edge.id},
        FUTURE_TRANSITIONS,
        expected_tests,
        FUTURE_TESTS,
    ):
        raise SystemExit("ERROR: self-test stayed GREEN: future_transition_activated")
    print("SELF_TEST_RED future_transition_activated")
    if not audit_future_partition(
        {edge.id for edge in TRANSITIONS},
        FUTURE_TRANSITIONS,
        expected_tests | {"26"},
        FUTURE_TESTS,
    ):
        raise SystemExit("ERROR: self-test stayed GREEN: future_test_activated")
    print("SELF_TEST_RED future_test_activated")


def validate() -> None:
    errors: list[str] = []
    ids = [x.id for x in TRANSITIONS]
    if len(ids) != len(set(ids)):
        errors.append("duplicate transition id")

    reachable = {("none", "none")}
    pending = list(TRANSITIONS)
    while pending:
        progressed = False
        for edge in pending[:]:
            if edge.predecessor in reachable:
                reachable.add(edge.successor)
                pending.remove(edge)
                progressed = True
        if not progressed:
            errors.append("unreachable transition predecessors: " + ", ".join(f"{x.id}:{x.predecessor}" for x in pending))
            break

    if reachable != set(SHAPES):
        errors.append(f"reachable/shape mismatch: reachable-only={sorted(reachable-set(SHAPES))}; shape-only={sorted(set(SHAPES)-reachable)}")

    for edge in TRANSITIONS:
        override_fields = tuple(field for field, _ in edge.overrides)
        for field in edge.set_fields + edge.clear_fields + override_fields:
            if field not in FIELDS:
                errors.append(f"{edge.id}: unknown field {field}")
        action_fields = list(edge.set_fields + edge.clear_fields + override_fields)
        if len(action_fields) != len(set(action_fields)):
            errors.append(f"{edge.id}: field has multiple assignments")
        target = SHAPES[edge.successor]
        source = SHAPES[edge.predecessor]
        overrides = dict(edge.overrides)
        for field in edge.set_fields:
            if target[field] == NULL:
                errors.append(f"{edge.id}: SET {field} but destination requires NULL")
        for field in edge.clear_fields:
            if target[field] == SET:
                errors.append(f"{edge.id}: CLEAR {field} but destination requires SET")
        for field in FIELDS:
            actual = overrides.get(field, SET if field in edge.set_fields else NULL if field in edge.clear_fields else source[field])
            required = target[field]
            compatible = (
                required in (EITHER, EITHER_COUNT, EITHER_BOOL)
                or actual == required
                or (required == POSITIVE and actual == POSITIVE)
            )
            if not compatible:
                errors.append(
                    f"{edge.id}: {field} becomes {actual} by SET/CLEAR/PRESERVE, "
                    f"destination {edge.successor} requires {required}"
                )

    if errors:
        raise SystemExit("\n".join("ERROR: " + x for x in errors))

    test_ids = [test_id for group in TESTS.values() for test_id in group]
    errors.extend(audit_test_ids(test_ids))
    errors.extend(audit_future_partition(
        set(ids), FUTURE_TRANSITIONS, set(test_ids), FUTURE_TESTS,
    ))

    brief = Path(__file__).with_name("tip_88c1_b2_durable_key_prod_build_dispatch.md").read_text(encoding="utf-8")
    errors.extend(audit_brief(brief, reachable, set(test_ids)))
    if errors:
        raise SystemExit("\n".join("ERROR: " + x for x in errors))
    if "--self-test" in sys.argv:
        run_self_tests(brief, reachable, set(test_ids))

    heads = {h for h, _ in SHAPES}
    mappings = {m for _, m in SHAPES}
    print(f"mutating_edges={len(TRANSITIONS)}")
    print(f"reachable_pairs={len(reachable)}")
    print(f"conceptual_heads={len(heads)} persisted_heads={len(heads)-1}")
    print(f"conceptual_mappings={len(mappings)} persisted_mappings={len(mappings)-1}")
    print(f"cartesian_cells={len(heads)*len(mappings)} rejected_cells={len(heads)*len(mappings)-len(reachable)}")
    print(f"active_tests={len(test_ids)}")
    for pair in sorted(reachable):
        print(f"PAIR {pair[0]}/{pair[1]}")


if __name__ == "__main__":
    validate()
