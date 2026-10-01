namespace TagEkyc.Infrastructure.Persistence.Migrations;

public partial class OpenBaoProductionKekProvider
{
    private const string EventValuesConstraint = """
                        "EventSequence" >= 1
                        AND "EventKind" IN ('Opened','Expired','DirectActivated','RecoveredActivated','ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ResolvedCleanupRequired','ProviderUnavailableObserved','CleanupAttemptObserved','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked')
                        AND ("ProviderResolutionEvidenceDigest" IS NULL OR octet_length("ProviderResolutionEvidenceDigest")=32)
                        AND ("ProviderCleanupEvidenceDigest" IS NULL OR octet_length("ProviderCleanupEvidenceDigest")=32)
                        AND ("CleanupObservationEvidenceDigest" IS NULL OR octet_length("CleanupObservationEvidenceDigest")=32)
                        AND ("WrappedDekMetadataDigest" IS NULL OR octet_length("WrappedDekMetadataDigest")=32)
                        AND ("RevocationEvidenceDigest" IS NULL OR octet_length("RevocationEvidenceDigest")=32)
                        AND ("AbandonmentEvidenceDigest" IS NULL OR octet_length("AbandonmentEvidenceDigest")=32)
                        AND CASE
                          WHEN "EventKind"='Opened' THEN "ProviderOperationToken" IS NOT NULL AND "WrappingSuiteId" IN ('AES-256-GCM','OPENBAO_TRANSIT_AES_GCM') AND "WrappingSuiteVersion"=1
                            AND "ResolutionKind" IS NULL AND "CleanupResultKind" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "RevocationReasonCode" IS NULL AND "RevocationEvidenceDigest" IS NULL AND "OperatorReasonCode" IS NULL AND "RequestingActorEvidence" IS NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NULL
                          WHEN "EventKind"='Expired' THEN "ResolutionKind" IS NULL AND "CleanupResultKind" IS NULL AND "ProviderOperationToken" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "RevocationReasonCode" IS NULL AND "RevocationEvidenceDigest" IS NULL AND "OperatorReasonCode" IS NULL AND "RequestingActorEvidence" IS NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NULL
                          WHEN "EventKind"='DirectActivated' THEN "ProviderOperationReceipt" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL AND "WrappingSuiteId" IN ('AES-256-GCM','OPENBAO_TRANSIT_AES_GCM') AND "WrappingSuiteVersion"=1 AND "ResolutionKind" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL
                          WHEN "EventKind"='RecoveredActivated' THEN "ProviderOperationReceipt" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL AND "WrappingSuiteId" IN ('AES-256-GCM','OPENBAO_TRANSIT_AES_GCM') AND "WrappingSuiteVersion"=1 AND "ResolutionKind"='WrappedResultRecovered' AND "ProviderResolutionEvidenceDigest" IS NOT NULL
                          WHEN "EventKind" IN ('ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ProviderUnavailableObserved') THEN "ResolutionKind" IS NOT NULL AND "ProviderResolutionEvidenceDigest" IS NOT NULL AND "CleanupResultKind" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL
                          WHEN "EventKind"='ResolvedCleanupRequired' THEN "ResolutionKind"='ProviderResourceCleanupRequired' AND "ProviderResolutionEvidenceDigest" IS NOT NULL AND "ProviderCleanupReference" IS NOT NULL
                          WHEN "EventKind"='CleanupAttemptObserved' THEN "CleanupResultKind" IS NOT NULL AND "ProviderCleanupReference" IS NOT NULL AND "CleanupObservationEvidenceDigest" IS NOT NULL
                          WHEN "EventKind"='CleanupAcknowledged' THEN "CleanupResultKind" IN ('Cleaned','AlreadyAbsent') AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderCleanupEvidenceDigest" IS NOT NULL
                          WHEN "EventKind"='AbandonRequested' THEN "ProviderOperationToken" IS NOT NULL AND "OperatorReasonCode" IS NOT NULL AND "RequestingActorEvidence" IS NOT NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NOT NULL
                          WHEN "EventKind"='ProviderOperationAbandoned' THEN "ProviderOperationToken" IS NOT NULL AND "OperatorReasonCode" IS NOT NULL AND "RequestingActorEvidence" IS NOT NULL AND "FinalizingActorEvidence" IS NOT NULL AND "AbandonRequestPreparationEventId" IS NOT NULL AND "AbandonmentEvidenceDigest" IS NOT NULL AND "HeadRowRevision" IS NOT NULL
                          WHEN "EventKind"='Revoked' THEN "RevocationReasonCode" IS NOT NULL AND "RevocationEvidenceDigest" IS NOT NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string ReservationSparseConstraint = """
                        CASE
                          WHEN "PreparationDisposition" = 'PreparingLive'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'PreparingExpiredAwaitingResolution'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "ResolutionDeadlineUtc" IS NOT NULL AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired"
                              AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ProviderOutcomeUnknown'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "ResolutionDeadlineUtc" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NOT NULL AND "ResolutionAttemptCount">0
                              AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'Active'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'Revoked'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NOT NULL AND "RevocationReasonCode" IS NOT NULL
                          WHEN "PreparationDisposition" = 'ProviderCleanupRequired'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "CleanupDeadlineUtc" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ReadyForFreshPreparation'
                            THEN "CurrentPreparationId" IS NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired"
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ProviderCorruptOrUnverifiable'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'AbandonRequested'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ReservationAbandoned'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired"
                              AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL
                              AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string ReservationTextConstraint = """
                        "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                          AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                        AND "KekId"=btrim("KekId") AND "KekId"=normalize("KekId",NFC)
                          AND octet_length("KekId") BETWEEN 1 AND 512 AND "KekId" !~ '[\x00-\x1f\x7f]'
                        AND "KekFingerprint"=btrim("KekFingerprint") AND "KekFingerprint"=normalize("KekFingerprint",NFC)
                          AND octet_length("KekFingerprint") BETWEEN 1 AND 512 AND "KekFingerprint" !~ '[\x00-\x1f\x7f]'
                        AND "MaterialRepresentationId"=btrim("MaterialRepresentationId") AND "MaterialRepresentationId"=normalize("MaterialRepresentationId",NFC)
                          AND octet_length("MaterialRepresentationId") BETWEEN 1 AND 512 AND "MaterialRepresentationId" !~ '[\x00-\x1f\x7f]'
                        AND "WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC)
                          AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'
                        AND ("RevocationReasonCode" IS NULL OR (
                          "RevocationReasonCode"=btrim("RevocationReasonCode") AND "RevocationReasonCode"=normalize("RevocationReasonCode",NFC)
                          AND octet_length("RevocationReasonCode") BETWEEN 1 AND 512 AND "RevocationReasonCode" !~ '[\x00-\x1f\x7f]'))
                        
        """;
    private const string ReservationValuesConstraint = """
                        octet_length("EncryptionAttemptFingerprint") = 32
                        AND octet_length("AttemptKeyContextFingerprint") = 32
                        AND "KekVersion" >= 1
                        AND "MaterialRepresentationId" IN ('LEGACY_AES_GCM_SPLIT','OPAQUE_PROVIDER_CIPHERTEXT')
                        AND "MaterialRepresentationVersion" = 1
                        AND "WrappingSuiteVersion" >= 1
                        AND "CurrentPreparationFence" >= 1
                        AND "ResolutionAttemptCount" >= 0
                        AND "CleanupAttemptCount" >= 0
                        AND "RowRevision" >= 1
                        AND "PreparationDisposition" IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown','ProviderCorruptOrUnverifiable','ProviderCleanupRequired','ReadyForFreshPreparation','Active','Revoked','AbandonRequested','ReservationAbandoned')
                        AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL)
                          OR ("MaterialRepresentationId"='LEGACY_AES_GCM_SPLIT' AND octet_length("WrappedDekCiphertext") = 32 AND octet_length("WrappedDekNonce") = 12 AND octet_length("WrappedDekTag") = 16 AND "OpaqueWrappedDekPayload" IS NULL AND octet_length("WrappedDekMetadataDigest") = 32)
                          OR ("MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT' AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND octet_length("WrappedDekMetadataDigest") = 32))
                        AND ("CurrentProviderOperationToken" IS NULL OR "CurrentProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$')
                        
        """;
    private const string ProviderSparseConstraint = """
                        CASE "ProviderOperationState"
                          WHEN 'Issued' THEN "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL
                            AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL
                            AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL
                            AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                          WHEN 'ResultObserved' THEN (("MaterialRepresentationId"='LEGACY_AES_GCM_SPLIT' AND octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND "OpaqueWrappedDekPayload" IS NULL) OR ("MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT' AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096)) AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL AND "ProviderCleanupReference" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                          WHEN 'CleanupRequired' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                            AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL)
                              OR ("MaterialRepresentationId"='LEGACY_AES_GCM_SPLIT' AND octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND "OpaqueWrappedDekPayload" IS NULL AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL)
                              OR ("MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT' AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                          WHEN 'CleanedUp' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL
                            AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL)
                              OR ("MaterialRepresentationId"='LEGACY_AES_GCM_SPLIT' AND octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND "OpaqueWrappedDekPayload" IS NULL AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL)
                              OR ("MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT' AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                          WHEN 'AbsenceProven' THEN "ProviderAbsenceProofReceipt" IS NOT NULL AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "OpaqueWrappedDekPayload" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string ProviderValuesConstraint = """
                        "ProviderOperationState" IN ('Issued','ResultObserved','CleanupRequired','CleanedUp','AbsenceProven')
                        AND "PreparationFence" >= 1
                        AND octet_length("AttemptKeyContextFingerprint") = 32
                        AND "ProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$'
                        AND ("MaterialRepresentationId" IS NULL OR "MaterialRepresentationId" IN ('LEGACY_AES_GCM_SPLIT','OPAQUE_PROVIDER_CIPHERTEXT'))
                        AND ("MaterialRepresentationVersion" IS NULL OR "MaterialRepresentationVersion"=1)
                        
        """;
    private const string OpenBaoJournalConstraint = """
                        octet_length("ProviderOperationTokenDigest")=32
                        AND octet_length("AttemptKeyContextFingerprint")=32
                        AND "PreparationFence">=1
                        AND "KekVersion">=1
                        AND "JournalState" IN ('Issued','Wrapped','AbsenceProven','CleanupRequired','CleanedUp')
                        AND "MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT'
                        AND "MaterialRepresentationVersion"=1
                        AND "WrappingSchemeVersion">=1
                        AND "RowRevision">=1
                        AND CASE "JournalState"
                          WHEN 'Issued' THEN "OpaqueWrappedDekPayload" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                          WHEN 'Wrapped' THEN octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL AND "ProviderCleanupReceipt" IS NULL
                          WHEN 'AbsenceProven' THEN "OpaqueWrappedDekPayload" IS NULL AND "ProviderAbsenceProofReceipt" IS NOT NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                          WHEN 'CleanupRequired' THEN octet_length("OpaqueWrappedDekPayload") BETWEEN 1 AND 4096 AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                          WHEN 'CleanedUp' THEN "OpaqueWrappedDekPayload" IS NULL AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string LegacyEventValuesConstraint = """
                        "EventSequence" >= 1
                        AND "EventKind" IN ('Opened','Expired','DirectActivated','RecoveredActivated','ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ResolvedCleanupRequired','ProviderUnavailableObserved','CleanupAttemptObserved','CleanupAcknowledged','AbandonRequested','ProviderOperationAbandoned','Revoked')
                        AND ("ProviderResolutionEvidenceDigest" IS NULL OR octet_length("ProviderResolutionEvidenceDigest")=32)
                        AND ("ProviderCleanupEvidenceDigest" IS NULL OR octet_length("ProviderCleanupEvidenceDigest")=32)
                        AND ("CleanupObservationEvidenceDigest" IS NULL OR octet_length("CleanupObservationEvidenceDigest")=32)
                        AND ("WrappedDekMetadataDigest" IS NULL OR octet_length("WrappedDekMetadataDigest")=32)
                        AND ("RevocationEvidenceDigest" IS NULL OR octet_length("RevocationEvidenceDigest")=32)
                        AND ("AbandonmentEvidenceDigest" IS NULL OR octet_length("AbandonmentEvidenceDigest")=32)
                        AND CASE
                          WHEN "EventKind"='Opened' THEN "ProviderOperationToken" IS NOT NULL AND "WrappingSuiteId"='AES-256-GCM' AND "WrappingSuiteVersion"=1
                            AND "ResolutionKind" IS NULL AND "CleanupResultKind" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "RevocationReasonCode" IS NULL AND "RevocationEvidenceDigest" IS NULL AND "OperatorReasonCode" IS NULL AND "RequestingActorEvidence" IS NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NULL
                          WHEN "EventKind"='Expired' THEN "ResolutionKind" IS NULL AND "CleanupResultKind" IS NULL AND "ProviderOperationToken" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "RevocationReasonCode" IS NULL AND "RevocationEvidenceDigest" IS NULL AND "OperatorReasonCode" IS NULL AND "RequestingActorEvidence" IS NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NULL
                          WHEN "EventKind"='DirectActivated' THEN "ProviderOperationReceipt" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL AND "WrappingSuiteId"='AES-256-GCM' AND "WrappingSuiteVersion"=1 AND "ResolutionKind" IS NULL AND "ProviderResolutionEvidenceDigest" IS NULL
                          WHEN "EventKind"='RecoveredActivated' THEN "ProviderOperationReceipt" IS NOT NULL AND "WrappedDekMetadataDigest" IS NOT NULL AND "WrappingSuiteId"='AES-256-GCM' AND "WrappingSuiteVersion"=1 AND "ResolutionKind"='WrappedResultRecovered' AND "ProviderResolutionEvidenceDigest" IS NOT NULL
                          WHEN "EventKind" IN ('ResolvedNoResult','ResolvedOutcomeUnknown','ResolvedCorrupt','ProviderUnavailableObserved') THEN "ResolutionKind" IS NOT NULL AND "ProviderResolutionEvidenceDigest" IS NOT NULL AND "CleanupResultKind" IS NULL AND "ProviderCleanupEvidenceDigest" IS NULL AND "CleanupObservationEvidenceDigest" IS NULL
                          WHEN "EventKind"='ResolvedCleanupRequired' THEN "ResolutionKind"='ProviderResourceCleanupRequired' AND "ProviderResolutionEvidenceDigest" IS NOT NULL AND "ProviderCleanupReference" IS NOT NULL
                          WHEN "EventKind"='CleanupAttemptObserved' THEN "CleanupResultKind" IS NOT NULL AND "ProviderCleanupReference" IS NOT NULL AND "CleanupObservationEvidenceDigest" IS NOT NULL
                          WHEN "EventKind"='CleanupAcknowledged' THEN "CleanupResultKind" IN ('Cleaned','AlreadyAbsent') AND "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderCleanupEvidenceDigest" IS NOT NULL
                          WHEN "EventKind"='AbandonRequested' THEN "ProviderOperationToken" IS NOT NULL AND "OperatorReasonCode" IS NOT NULL AND "RequestingActorEvidence" IS NOT NULL AND "FinalizingActorEvidence" IS NULL AND "AbandonRequestPreparationEventId" IS NULL AND "AbandonmentEvidenceDigest" IS NULL AND "HeadRowRevision" IS NOT NULL
                          WHEN "EventKind"='ProviderOperationAbandoned' THEN "ProviderOperationToken" IS NOT NULL AND "OperatorReasonCode" IS NOT NULL AND "RequestingActorEvidence" IS NOT NULL AND "FinalizingActorEvidence" IS NOT NULL AND "AbandonRequestPreparationEventId" IS NOT NULL AND "AbandonmentEvidenceDigest" IS NOT NULL AND "HeadRowRevision" IS NOT NULL
                          WHEN "EventKind"='Revoked' THEN "RevocationReasonCode" IS NOT NULL AND "RevocationEvidenceDigest" IS NOT NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string LegacyReservationSparseConstraint = """
                        CASE
                          WHEN "PreparationDisposition" = 'PreparingLive'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'PreparingExpiredAwaitingResolution'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "ResolutionDeadlineUtc" IS NOT NULL AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired"
                              AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ProviderOutcomeUnknown'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "ResolutionDeadlineUtc" IS NOT NULL AND "NextResolutionAttemptNotBeforeUtc" IS NOT NULL AND "ResolutionAttemptCount">0
                              AND "CleanupAttemptCount"=0 AND NOT "CleanupOperatorInterventionRequired" AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'Active'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekCiphertext" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'Revoked'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "PreparedAtUtc" IS NOT NULL AND "WrappedDekCiphertext" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired" AND "RevokedAtUtc" IS NOT NULL AND "RevocationReasonCode" IS NOT NULL
                          WHEN "PreparationDisposition" = 'ProviderCleanupRequired'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL AND "CleanupDeadlineUtc" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ReadyForFreshPreparation'
                            THEN "CurrentPreparationId" IS NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired" AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ProviderCorruptOrUnverifiable'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'AbandonRequested'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NOT NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          WHEN "PreparationDisposition" = 'ReservationAbandoned'
                            THEN "CurrentPreparationId" IS NOT NULL AND "CurrentPreparationLeaseExpiresAtUtc" IS NULL AND "CurrentProviderOperationToken" IS NOT NULL
                              AND "NextResolutionAttemptNotBeforeUtc" IS NULL AND "ResolutionDeadlineUtc" IS NULL AND "NextCleanupAttemptNotBeforeUtc" IS NULL AND "CleanupDeadlineUtc" IS NULL
                              AND NOT "CleanupOperatorInterventionRequired" AND "WrappedDekCiphertext" IS NULL AND "PreparedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RevocationReasonCode" IS NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string LegacyReservationTextConstraint = """
                        "KeyProviderId"=btrim("KeyProviderId") AND "KeyProviderId"=normalize("KeyProviderId",NFC)
                          AND octet_length("KeyProviderId") BETWEEN 1 AND 512 AND "KeyProviderId" !~ '[\x00-\x1f\x7f]'
                        AND "KekId"=btrim("KekId") AND "KekId"=normalize("KekId",NFC)
                          AND octet_length("KekId") BETWEEN 1 AND 512 AND "KekId" !~ '[\x00-\x1f\x7f]'
                        AND "KekFingerprint"=btrim("KekFingerprint") AND "KekFingerprint"=normalize("KekFingerprint",NFC)
                          AND octet_length("KekFingerprint") BETWEEN 1 AND 512 AND "KekFingerprint" !~ '[\x00-\x1f\x7f]'
                        AND "WrappingSuiteId"=btrim("WrappingSuiteId") AND "WrappingSuiteId"=normalize("WrappingSuiteId",NFC)
                          AND octet_length("WrappingSuiteId") BETWEEN 1 AND 512 AND "WrappingSuiteId" !~ '[\x00-\x1f\x7f]'
                        AND ("RevocationReasonCode" IS NULL OR (
                          "RevocationReasonCode"=btrim("RevocationReasonCode") AND "RevocationReasonCode"=normalize("RevocationReasonCode",NFC)
                          AND octet_length("RevocationReasonCode") BETWEEN 1 AND 512 AND "RevocationReasonCode" !~ '[\x00-\x1f\x7f]'))
                        
        """;
    private const string LegacyReservationValuesConstraint = """
                        octet_length("EncryptionAttemptFingerprint") = 32
                        AND octet_length("AttemptKeyContextFingerprint") = 32
                        AND "KekVersion" >= 1
                        AND "WrappingSuiteId" = 'AES-256-GCM'
                        AND "WrappingSuiteVersion" = 1
                        AND "CurrentPreparationFence" >= 1
                        AND "ResolutionAttemptCount" >= 0
                        AND "CleanupAttemptCount" >= 0
                        AND "RowRevision" >= 1
                        AND "PreparationDisposition" IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown','ProviderCorruptOrUnverifiable','ProviderCleanupRequired','ReadyForFreshPreparation','Active','Revoked','AbandonRequested','ReservationAbandoned')
                        AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL)
                          OR (octet_length("WrappedDekCiphertext") = 32 AND octet_length("WrappedDekNonce") = 12 AND octet_length("WrappedDekTag") = 16 AND octet_length("WrappedDekMetadataDigest") = 32))
                        AND ("CurrentProviderOperationToken" IS NULL OR "CurrentProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$')
                        
        """;
    private const string LegacyProviderSparseConstraint = """
                        CASE "ProviderOperationState"
                          WHEN 'Issued' THEN "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL
                            AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL
                            AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL
                            AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                          WHEN 'ResultObserved' THEN octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL AND "ProviderCleanupReference" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                          WHEN 'CleanupRequired' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NULL AND "ProviderAbsenceProofReceipt" IS NULL
                            AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL)
                              OR (octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                          WHEN 'CleanedUp' THEN "ProviderCleanupReference" IS NOT NULL AND "ProviderCleanupReceipt" IS NOT NULL AND "ProviderAbsenceProofReceipt" IS NULL
                            AND (("WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL)
                              OR (octet_length("WrappedDekCiphertext")=32 AND octet_length("WrappedDekNonce")=12 AND octet_length("WrappedDekTag")=16 AND octet_length("WrappedDekMetadataDigest")=32 AND "WrappingSuiteId" IS NOT NULL AND "WrappingSuiteVersion" IS NOT NULL AND "ProviderResourceReference" IS NOT NULL AND "ProviderOperationReceipt" IS NOT NULL AND "ResultObservedAtUtc" IS NOT NULL))
                          WHEN 'AbsenceProven' THEN "ProviderAbsenceProofReceipt" IS NOT NULL AND "WrappedDekCiphertext" IS NULL AND "WrappedDekNonce" IS NULL AND "WrappedDekTag" IS NULL AND "WrappedDekMetadataDigest" IS NULL AND "WrappingSuiteId" IS NULL AND "WrappingSuiteVersion" IS NULL AND "ProviderResourceReference" IS NULL AND "ProviderOperationReceipt" IS NULL AND "ResultObservedAtUtc" IS NULL AND "ProviderCleanupReference" IS NULL AND "ProviderCleanupReceipt" IS NULL
                          ELSE FALSE
                        END
                        
        """;
    private const string LegacyProviderValuesConstraint = """
                        "ProviderOperationState" IN ('Issued','ResultObserved','CleanupRequired','CleanedUp','AbsenceProven')
                        AND "PreparationFence" >= 1
                        AND octet_length("AttemptKeyContextFingerprint") = 32
                        AND "ProviderOperationToken" ~ '^[A-Za-z0-9_-]{43}$'
                        
        """;
}
