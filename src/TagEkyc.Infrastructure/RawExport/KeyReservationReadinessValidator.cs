using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class KeyReservationReadinessValidator(TagEkycDbContext db) : IDurableKeyReadinessValidator
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_KEY_RESERVATION_STATE_INVALID",
        "PROD_RAW_EXPORT_KEY_PREPARATION_HISTORY_INVALID",
        "PROD_RAW_EXPORT_KEY_PREPARATION_LEASE_INVALID",
        "PROD_RAW_EXPORT_KEY_OPERATION_RECOVERY_UNSUPPORTED",
        "PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_UNSUPPORTED",
        "PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OUTCOME_UNKNOWN",
        "PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OPERATOR_INTERVENTION",
        "PROD_RAW_EXPORT_KEY_PROVIDER_CORRUPT_UNRESOLVED",
        "PROD_RAW_EXPORT_KEY_ABANDON_PENDING",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        var code = await db.Database.SqlQueryRaw<string>("""
            WITH current_pair AS (
              SELECT h.*,m."ProviderOperationState",m."ProviderCleanupReference"
              FROM tagekyc.raw_export_attempt_key_reservations h
              LEFT JOIN tagekyc.raw_export_key_provider_operations m
                ON m."AttemptKeyReservationId"=h."AttemptKeyReservationId"
               AND m."PreparationFence"=h."CurrentPreparationFence"),
            issues AS (
              SELECT 1 AS ord,'PROD_RAW_EXPORT_KEY_RESERVATION_STATE_INVALID'::text AS code
              FROM current_pair p
              WHERE p."ProviderOperationState" IS NULL OR NOT (
                (p."PreparationDisposition"='PreparingLive' AND p."ProviderOperationState" IN ('Issued','ResultObserved')) OR
                (p."PreparationDisposition" IN ('PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown','ProviderCorruptOrUnverifiable') AND p."ProviderOperationState"='Issued') OR
                (p."PreparationDisposition"='ProviderCleanupRequired' AND p."ProviderOperationState"='CleanupRequired') OR
                (p."PreparationDisposition"='ReadyForFreshPreparation' AND p."ProviderOperationState" IN ('CleanedUp','AbsenceProven')) OR
                (p."PreparationDisposition" IN ('Active','Revoked') AND p."ProviderOperationState"='ResultObserved') OR
                (p."PreparationDisposition"='AbandonRequested' AND p."ProviderOperationState" IN ('Issued','ResultObserved','CleanupRequired','CleanedUp','AbsenceProven')) OR
                (p."PreparationDisposition"='ReservationAbandoned' AND p."ProviderOperationState" IN ('CleanedUp','AbsenceProven')))
              UNION ALL
              SELECT 2,'PROD_RAW_EXPORT_KEY_PREPARATION_HISTORY_INVALID'
              FROM current_pair p WHERE NOT EXISTS (
                SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events e
                WHERE e."AttemptKeyReservationId"=p."AttemptKeyReservationId"
                  AND e."PreparationFence"=p."CurrentPreparationFence" AND e."EventKind"='Opened')
              UNION ALL
              SELECT 3,'PROD_RAW_EXPORT_KEY_PREPARATION_LEASE_INVALID'
              FROM current_pair p WHERE p."PreparationDisposition"='PreparingLive'
                AND p."CurrentPreparationLeaseExpiresAtUtc"<=pg_catalog.clock_timestamp()
              UNION ALL
              SELECT 4,'PROD_RAW_EXPORT_KEY_OPERATION_RECOVERY_UNSUPPORTED'
              FROM current_pair p WHERE p."PreparationDisposition" IN ('PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown')
                AND p."CurrentProviderOperationToken" IS NULL
              UNION ALL
              SELECT 5,'PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_UNSUPPORTED'
              FROM current_pair p WHERE p."PreparationDisposition"='ProviderCleanupRequired'
                AND p."ProviderCleanupReference" IS NULL
              UNION ALL
              SELECT 6,'PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OUTCOME_UNKNOWN'
              FROM current_pair p WHERE EXISTS (
                SELECT 1 FROM tagekyc.raw_export_attempt_key_preparation_events e
                WHERE e."AttemptKeyReservationId"=p."AttemptKeyReservationId"
                  AND e."PreparationFence"=p."CurrentPreparationFence"
                  AND e."EventKind"='CleanupAttemptObserved'
                  AND e."CleanupResultKind"='CleanupOutcomeUnknown')
              UNION ALL
              SELECT 7,'PROD_RAW_EXPORT_KEY_PROVIDER_CLEANUP_OPERATOR_INTERVENTION'
              FROM current_pair p WHERE p."CleanupOperatorInterventionRequired"
              UNION ALL
              SELECT 8,'PROD_RAW_EXPORT_KEY_PROVIDER_CORRUPT_UNRESOLVED'
              FROM current_pair p WHERE p."PreparationDisposition"='ProviderCorruptOrUnverifiable'
              UNION ALL
              SELECT 9,'PROD_RAW_EXPORT_KEY_ABANDON_PENDING'
              FROM current_pair p WHERE p."PreparationDisposition"='AbandonRequested')
            SELECT code AS "Value" FROM issues ORDER BY ord LIMIT 1
            """).FirstOrDefaultAsync(cancellationToken);
        if (code is not null) throw new DurableKeyReadinessException(code);
    }
}
