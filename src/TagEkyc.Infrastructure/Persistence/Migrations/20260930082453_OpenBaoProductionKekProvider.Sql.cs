namespace TagEkyc.Infrastructure.Persistence.Migrations;

public partial class OpenBaoProductionKekProvider
{
    private const string PreflightSql = """
        DO $guard$
        DECLARE expected record; actual text;
        BEGIN
          FOR expected IN SELECT * FROM (VALUES
            ('tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)','b98e3708bdd0c4beae2eb81e4c926377afc7a0f7050cdc6a1c20fb15b5c6ea4c'),
            ('tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)','c9207a7fbd9192bea2ad534fa1f26d43c246e03f4704de1e6055c198f956f66b'),
            ('tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint)','3292839f9446df41453efa2d8b0644bd34200970854014d44390b01f396f1004')
          ) AS x(signature,sha256)
          LOOP
            SELECT p.prosrc INTO actual FROM pg_catalog.pg_proc p
            WHERE p.oid=pg_catalog.to_regprocedure(expected.signature);
            IF NOT FOUND OR pg_catalog.encode(tagekyc_extensions.digest(pg_catalog.convert_to(pg_catalog.replace(actual,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10)),'UTF8'),'sha256'),'hex')<>expected.sha256 THEN
              RAISE EXCEPTION 'OPENBAO_REPRESENTATION_PREDECESSOR_BODY_MISMATCH' USING
                DETAIL=expected.signature||' expected='||expected.sha256||' actual='||
                  pg_catalog.encode(tagekyc_extensions.digest(pg_catalog.convert_to(pg_catalog.replace(actual,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10)),'UTF8'),'sha256'),'hex');
            END IF;
          END LOOP;
        END $guard$;
        """;

    private const string UpProviderSql = """
        CREATE FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(
          p_attempt_key_reservation_id uuid,p_attempt_id uuid,p_source_artifact_id uuid,
          p_material_representation_id text,p_material_representation_version integer,
          p_wrapping_suite_id text,p_wrapping_suite_version integer)
        RETURNS TABLE(outcome text,provider_operation_id uuid,preparation_id uuid,preparation_fence bigint,
          provider_operation_token text,preparation_lease_expires_at_utc timestamptz,
          attempt_key_context_fingerprint bytea,key_provider_id text,kek_id text,kek_version integer,
          kek_fingerprint text,wrapping_suite_id text,wrapping_suite_version integer)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $prepare$
        DECLARE attempt_row record; existing_head record; operation_row record;
          now_utc timestamptz:=pg_catalog.statement_timestamp(); new_preparation_id uuid:=pg_catalog.gen_random_uuid();
          new_operation_id uuid:=pg_catalog.gen_random_uuid(); new_fence bigint; new_token text; context_fingerprint bytea;
          prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
        BEGIN
          IF NOT ((p_material_representation_id='LEGACY_AES_GCM_SPLIT' AND p_material_representation_version=1 AND p_wrapping_suite_id='AES-256-GCM' AND p_wrapping_suite_version=1)
              OR (p_material_representation_id='OPAQUE_PROVIDER_CIPHERTEXT' AND p_material_representation_version=1 AND p_wrapping_suite_id='OPENBAO_TRANSIT_AES_GCM' AND p_wrapping_suite_version=1)) THEN
            RETURN QUERY SELECT 'ProfileInvalid'::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN;
          END IF;
          SELECT a.*,h."CustodyState",h."CurrentEncryptionAttemptId",r."ReservationExpiresAtUtc" INTO attempt_row
          FROM tagekyc.raw_export_source_encryption_attempts a
          JOIN tagekyc.raw_export_source_head h ON h."SourceArtifactId"=a."SourceArtifactId"
          JOIN tagekyc.raw_export_source_reservations r ON r."SourceArtifactId"=a."SourceArtifactId"
          WHERE a."AttemptId"=p_attempt_id AND a."SourceArtifactId"=p_source_artifact_id
            AND a."AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE OF a,h;
          IF NOT FOUND OR attempt_row."CustodyState"<>'Reserved' OR attempt_row."CurrentEncryptionAttemptId"<>p_attempt_id
             OR attempt_row."R2TerminationDisposition" IS NOT NULL OR attempt_row."OwnershipLeaseExpiresAtUtc"<=now_utc
             OR attempt_row."ReservationExpiresAtUtc"<=now_utc THEN
            RETURN QUERY SELECT 'HeadNotReserved'::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN;
          END IF;
          IF p_material_representation_id='LEGACY_AES_GCM_SPLIT' THEN
            context_fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-attempt-key-context-v1',
              pg_catalog.replace(p_attempt_key_reservation_id::text,'-',''),pg_catalog.replace(p_attempt_id::text,'-',''),
              pg_catalog.encode(attempt_row."EncryptionAttemptFingerprint",'hex'),attempt_row."KeyProviderId",attempt_row."KekId",
              attempt_row."KekVersion"::text,attempt_row."KekFingerprint",p_wrapping_suite_id,p_wrapping_suite_version::text);
          ELSE
            context_fingerprint:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-attempt-key-context-v2',
              pg_catalog.replace(p_attempt_key_reservation_id::text,'-',''),pg_catalog.replace(p_attempt_id::text,'-',''),
              pg_catalog.encode(attempt_row."EncryptionAttemptFingerprint",'hex'),attempt_row."KeyProviderId",attempt_row."KekId",
              attempt_row."KekVersion"::text,attempt_row."KekFingerprint",p_material_representation_id,
              p_material_representation_version::text,p_wrapping_suite_id,p_wrapping_suite_version::text);
          END IF;
          SELECT * INTO existing_head FROM tagekyc.raw_export_attempt_key_reservations
          WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
          IF FOUND THEN
            IF existing_head."AttemptId"<>p_attempt_id OR existing_head."EncryptionAttemptFingerprint" IS DISTINCT FROM attempt_row."EncryptionAttemptFingerprint"
               OR existing_head."AttemptKeyContextFingerprint" IS DISTINCT FROM context_fingerprint
               OR existing_head."MaterialRepresentationId"<>p_material_representation_id
               OR existing_head."MaterialRepresentationVersion"<>p_material_representation_version
               OR existing_head."WrappingSuiteId"<>p_wrapping_suite_id OR existing_head."WrappingSuiteVersion"<>p_wrapping_suite_version THEN
              RETURN QUERY SELECT 'Conflict'::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN;
            END IF;
            IF existing_head."PreparationDisposition"='Active' THEN
              SELECT * INTO operation_row FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=existing_head."CurrentPreparationFence";
              RETURN QUERY SELECT 'ExistingMatch'::text,operation_row."ProviderOperationId",existing_head."CurrentPreparationId",existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion"; RETURN;
            END IF;
            IF existing_head."PreparationDisposition" IN ('Revoked','ProviderCorruptOrUnverifiable','AbandonRequested','ReservationAbandoned') THEN
              RETURN QUERY SELECT 'Terminated'::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN;
            END IF;
            IF existing_head."PreparationDisposition"<>'ReadyForFreshPreparation' THEN
              SELECT * INTO operation_row FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=existing_head."CurrentPreparationFence";
              RETURN QUERY SELECT 'InProgress'::text,operation_row."ProviderOperationId",existing_head."CurrentPreparationId",existing_head."CurrentPreparationFence",existing_head."CurrentProviderOperationToken"::text,existing_head."CurrentPreparationLeaseExpiresAtUtc",existing_head."AttemptKeyContextFingerprint",existing_head."KeyProviderId"::text,existing_head."KekId"::text,existing_head."KekVersion",existing_head."KekFingerprint"::text,existing_head."WrappingSuiteId"::text,existing_head."WrappingSuiteVersion"; RETURN;
            END IF;
            new_fence:=existing_head."CurrentPreparationFence"+1;
          ELSE new_fence:=1; END IF;
          IF EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence">=new_fence) THEN
            RETURN QUERY SELECT 'StaleFence'::text,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::timestamptz,NULL::bytea,NULL::text,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::integer; RETURN;
          END IF;
          new_token:=pg_catalog.rtrim(pg_catalog.translate(pg_catalog.encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'=');
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
          BEGIN
            IF existing_head."AttemptKeyReservationId" IS NULL THEN
              INSERT INTO tagekyc.raw_export_attempt_key_reservations(
                "AttemptKeyReservationId","AttemptId","EncryptionAttemptFingerprint","KeyProviderId","KekId","KekVersion","KekFingerprint",
                "AttemptKeyContextFingerprint","MaterialRepresentationId","MaterialRepresentationVersion","WrappingSuiteId","WrappingSuiteVersion",
                "PreparationDisposition","CurrentPreparationId","CurrentPreparationFence","CurrentPreparationLeaseExpiresAtUtc","CurrentProviderOperationToken",
                "ResolutionAttemptCount","CleanupAttemptCount","CleanupOperatorInterventionRequired","RowRevision","CreatedAtUtc","UpdatedAtUtc")
              VALUES(p_attempt_key_reservation_id,p_attempt_id,attempt_row."EncryptionAttemptFingerprint",attempt_row."KeyProviderId",attempt_row."KekId",attempt_row."KekVersion",attempt_row."KekFingerprint",
                context_fingerprint,p_material_representation_id,p_material_representation_version,p_wrapping_suite_id,p_wrapping_suite_version,
                'PreparingLive',new_preparation_id,new_fence,now_utc+interval '15 minutes',new_token,0,0,false,1,now_utc,now_utc);
            ELSE
              UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='PreparingLive',"CurrentPreparationId"=new_preparation_id,
                "CurrentPreparationFence"=new_fence,"CurrentPreparationLeaseExpiresAtUtc"=now_utc+interval '15 minutes',"CurrentProviderOperationToken"=new_token,
                "ResolutionAttemptCount"=0,"NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,"CleanupAttemptCount"=0,
                "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,"CleanupOperatorInterventionRequired"=false,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
              WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
            END IF;
            INSERT INTO tagekyc.raw_export_key_provider_operations("ProviderOperationId","KeyProviderId","ProviderOperationToken","AttemptKeyReservationId","PreparationId","PreparationFence","AttemptKeyContextFingerprint","ProviderOperationState","IssuedAtUtc","UpdatedAtUtc")
            VALUES(new_operation_id,attempt_row."KeyProviderId",new_token,p_attempt_key_reservation_id,new_preparation_id,new_fence,context_fingerprint,'Issued',now_utc,now_utc);
            INSERT INTO tagekyc.raw_export_attempt_key_preparation_events("PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind","ProviderOperationToken","WrappingSuiteId","WrappingSuiteVersion","EventAtUtc")
            SELECT pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,new_preparation_id,new_fence,COALESCE(MAX("EventSequence"),0)+1,'Opened',new_token,p_wrapping_suite_id,p_wrapping_suite_version,now_utc
            FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
          EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
          RETURN QUERY SELECT 'PreparingLive'::text,new_operation_id,new_preparation_id,new_fence,new_token,now_utc+interval '15 minutes',context_fingerprint,attempt_row."KeyProviderId"::text,attempt_row."KekId"::text,attempt_row."KekVersion",attempt_row."KekFingerprint"::text,p_wrapping_suite_id,p_wrapping_suite_version;
        END $prepare$;

        CREATE FUNCTION tagekyc.raw_export_record_key_provider_wrapped_result(
          p_provider_operation_id uuid,p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,
          p_provider_operation_token text,p_material_representation_id text,p_material_representation_version integer,
          p_wrapped_dek_ciphertext bytea,p_wrapped_dek_nonce bytea,p_wrapped_dek_tag bytea,p_opaque_wrapped_dek_payload bytea,
          p_wrapping_suite_id text,p_wrapping_suite_version integer,p_provider_operation_receipt text,p_provider_resource_reference text)
        RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $record$
        DECLARE h record; op record; digest bytea; prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
        BEGIN
          IF p_provider_operation_receipt IS NULL OR pg_catalog.btrim(p_provider_operation_receipt)='' OR p_provider_resource_reference IS NULL OR pg_catalog.btrim(p_provider_resource_reference)='' THEN RETURN 'ShapeInvalid'; END IF;
          IF p_material_representation_id='LEGACY_AES_GCM_SPLIT' AND p_material_representation_version=1 THEN
            IF pg_catalog.octet_length(p_wrapped_dek_ciphertext)<>32 OR pg_catalog.octet_length(p_wrapped_dek_nonce)<>12 OR pg_catalog.octet_length(p_wrapped_dek_tag)<>16 OR p_opaque_wrapped_dek_payload IS NOT NULL OR p_wrapping_suite_id<>'AES-256-GCM' OR p_wrapping_suite_version<>1 THEN RETURN 'ShapeInvalid'; END IF;
          ELSIF p_material_representation_id='OPAQUE_PROVIDER_CIPHERTEXT' AND p_material_representation_version=1 THEN
            IF p_wrapped_dek_ciphertext IS NOT NULL OR p_wrapped_dek_nonce IS NOT NULL OR p_wrapped_dek_tag IS NOT NULL OR pg_catalog.octet_length(p_opaque_wrapped_dek_payload) NOT BETWEEN 1 AND 4096 OR p_wrapping_suite_id<>'OPENBAO_TRANSIT_AES_GCM' OR p_wrapping_suite_version<>1 THEN RETURN 'ShapeInvalid'; END IF;
          ELSE RETURN 'ShapeInvalid'; END IF;
          SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
          SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationId"=p_provider_operation_id FOR UPDATE;
          IF NOT FOUND OR op."AttemptKeyReservationId"<>p_attempt_key_reservation_id OR op."PreparationId"<>p_preparation_id OR op."PreparationFence"<>p_preparation_fence OR op."ProviderOperationToken"<>p_provider_operation_token THEN RETURN 'StaleOperation'; END IF;
          IF h."MaterialRepresentationId"<>p_material_representation_id OR h."MaterialRepresentationVersion"<>p_material_representation_version OR h."WrappingSuiteId"<>p_wrapping_suite_id OR h."WrappingSuiteVersion"<>p_wrapping_suite_version THEN RETURN 'StateConflict'; END IF;
          digest:=CASE WHEN p_material_representation_id='LEGACY_AES_GCM_SPLIT' THEN tagekyc.raw_export_c1_hash_canonical('tip-88c1-wrapped-dek-metadata-v1',pg_catalog.encode(op."AttemptKeyContextFingerprint",'hex'),p_wrapping_suite_id,p_wrapping_suite_version::text,pg_catalog.encode(p_wrapped_dek_nonce,'hex'),pg_catalog.encode(p_wrapped_dek_ciphertext,'hex'),pg_catalog.encode(p_wrapped_dek_tag,'hex'))
            ELSE tagekyc.raw_export_c1_hash_canonical('tip-88c1-wrapped-dek-metadata-opaque-v1',pg_catalog.encode(op."AttemptKeyContextFingerprint",'hex'),p_material_representation_id,p_material_representation_version::text,pg_catalog.encode(p_opaque_wrapped_dek_payload,'hex')) END;
          IF op."ProviderOperationState"='ResultObserved' THEN RETURN CASE WHEN op."MaterialRepresentationId"=p_material_representation_id AND op."MaterialRepresentationVersion"=p_material_representation_version AND op."WrappedDekCiphertext" IS NOT DISTINCT FROM p_wrapped_dek_ciphertext AND op."WrappedDekNonce" IS NOT DISTINCT FROM p_wrapped_dek_nonce AND op."WrappedDekTag" IS NOT DISTINCT FROM p_wrapped_dek_tag AND op."OpaqueWrappedDekPayload" IS NOT DISTINCT FROM p_opaque_wrapped_dek_payload AND op."WrappedDekMetadataDigest"=digest AND op."ProviderOperationReceipt"=p_provider_operation_receipt AND op."ProviderResourceReference"=p_provider_resource_reference THEN 'ExistingMatch' ELSE 'StateConflict' END; END IF;
          IF op."ProviderOperationState"<>'Issued' OR h."PreparationDisposition"<>'PreparingLive' OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence THEN RETURN 'StateConflict'; END IF;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
          BEGIN
            UPDATE tagekyc.raw_export_key_provider_operations SET "ProviderOperationState"='ResultObserved',"MaterialRepresentationId"=p_material_representation_id,"MaterialRepresentationVersion"=p_material_representation_version,"WrappedDekCiphertext"=p_wrapped_dek_ciphertext,"WrappedDekNonce"=p_wrapped_dek_nonce,"WrappedDekTag"=p_wrapped_dek_tag,"OpaqueWrappedDekPayload"=p_opaque_wrapped_dek_payload,"WrappedDekMetadataDigest"=digest,"WrappingSuiteId"=p_wrapping_suite_id,"WrappingSuiteVersion"=p_wrapping_suite_version,"ProviderResourceReference"=p_provider_resource_reference,"ProviderOperationReceipt"=p_provider_operation_receipt,"ResultObservedAtUtc"=pg_catalog.statement_timestamp(),"UpdatedAtUtc"=pg_catalog.statement_timestamp() WHERE "ProviderOperationId"=p_provider_operation_id;
          EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RETURN 'ResultObserved';
        END $record$;

        CREATE FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(
          p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint,p_provider_operation_token text,
          p_material_representation_id text,p_material_representation_version integer,
          p_wrapped_dek_ciphertext bytea,p_wrapped_dek_nonce bytea,p_wrapped_dek_tag bytea,p_opaque_wrapped_dek_payload bytea,
          p_wrapping_suite_id text,p_wrapping_suite_version integer,p_provider_resource_reference text,p_provider_operation_receipt text)
        RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $recovered$
        DECLARE h record; op record; digest bytea; evidence bytea; seq bigint; now_utc timestamptz:=pg_catalog.statement_timestamp();
          prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
        BEGIN
          IF p_provider_operation_receipt IS NULL OR pg_catalog.btrim(p_provider_operation_receipt)='' OR p_provider_resource_reference IS NULL OR pg_catalog.btrim(p_provider_resource_reference)='' THEN RETURN 'ShapeInvalid'; END IF;
          IF p_material_representation_id='LEGACY_AES_GCM_SPLIT' AND p_material_representation_version=1 THEN
            IF pg_catalog.octet_length(p_wrapped_dek_ciphertext)<>32 OR pg_catalog.octet_length(p_wrapped_dek_nonce)<>12 OR pg_catalog.octet_length(p_wrapped_dek_tag)<>16 OR p_opaque_wrapped_dek_payload IS NOT NULL OR p_wrapping_suite_id<>'AES-256-GCM' OR p_wrapping_suite_version<>1 THEN RETURN 'ShapeInvalid'; END IF;
          ELSIF p_material_representation_id='OPAQUE_PROVIDER_CIPHERTEXT' AND p_material_representation_version=1 THEN
            IF p_wrapped_dek_ciphertext IS NOT NULL OR p_wrapped_dek_nonce IS NOT NULL OR p_wrapped_dek_tag IS NOT NULL OR pg_catalog.octet_length(p_opaque_wrapped_dek_payload) NOT BETWEEN 1 AND 4096 OR p_wrapping_suite_id<>'OPENBAO_TRANSIT_AES_GCM' OR p_wrapping_suite_version<>1 THEN RETURN 'ShapeInvalid'; END IF;
          ELSE RETURN 'ShapeInvalid'; END IF;
          SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
          SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=p_preparation_fence FOR UPDATE;
          IF NOT FOUND OR h."CurrentPreparationId" IS DISTINCT FROM p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence
             OR op."ProviderOperationToken" IS DISTINCT FROM p_provider_operation_token OR op."ProviderOperationState"<>'Issued'
             OR h."PreparationDisposition" NOT IN ('PreparingLive','PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown')
             OR h."MaterialRepresentationId"<>p_material_representation_id OR h."MaterialRepresentationVersion"<>p_material_representation_version
             OR h."WrappingSuiteId"<>p_wrapping_suite_id OR h."WrappingSuiteVersion"<>p_wrapping_suite_version THEN RETURN 'StaleOperation'; END IF;
          digest:=CASE WHEN p_material_representation_id='LEGACY_AES_GCM_SPLIT' THEN tagekyc.raw_export_c1_hash_canonical('tip-88c1-wrapped-dek-metadata-v1',pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),p_wrapping_suite_id,p_wrapping_suite_version::text,pg_catalog.encode(p_wrapped_dek_nonce,'hex'),pg_catalog.encode(p_wrapped_dek_ciphertext,'hex'),pg_catalog.encode(p_wrapped_dek_tag,'hex'))
            ELSE tagekyc.raw_export_c1_hash_canonical('tip-88c1-wrapped-dek-metadata-opaque-v1',pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),p_material_representation_id,p_material_representation_version::text,pg_catalog.encode(p_opaque_wrapped_dek_payload,'hex')) END;
          evidence:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-key-provider-resolution-evidence-v1',pg_catalog.encode(h."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(p_preparation_id::text,'-',''),p_preparation_fence::text,'WrappedResultRecovered',p_provider_operation_token,'1',p_provider_operation_receipt,'0','1',pg_catalog.encode(digest,'hex'));
          SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
          BEGIN
            UPDATE tagekyc.raw_export_key_provider_operations SET "ProviderOperationState"='ResultObserved',"MaterialRepresentationId"=p_material_representation_id,"MaterialRepresentationVersion"=p_material_representation_version,"WrappedDekCiphertext"=p_wrapped_dek_ciphertext,"WrappedDekNonce"=p_wrapped_dek_nonce,"WrappedDekTag"=p_wrapped_dek_tag,"OpaqueWrappedDekPayload"=p_opaque_wrapped_dek_payload,"WrappedDekMetadataDigest"=digest,"WrappingSuiteId"=p_wrapping_suite_id,"WrappingSuiteVersion"=p_wrapping_suite_version,"ProviderResourceReference"=p_provider_resource_reference,"ProviderOperationReceipt"=p_provider_operation_receipt,"ResultObservedAtUtc"=now_utc,"UpdatedAtUtc"=now_utc WHERE "ProviderOperationId"=op."ProviderOperationId";
            UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='Active',"WrappedDekCiphertext"=p_wrapped_dek_ciphertext,"WrappedDekNonce"=p_wrapped_dek_nonce,"WrappedDekTag"=p_wrapped_dek_tag,"OpaqueWrappedDekPayload"=p_opaque_wrapped_dek_payload,"WrappedDekMetadataDigest"=digest,"PreparedAtUtc"=now_utc,"NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,"NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
            INSERT INTO tagekyc.raw_export_attempt_key_preparation_events("PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind","ResolutionKind","ProviderOperationReceipt","ProviderResolutionEvidenceDigest","WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion","EventAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,'RecoveredActivated','WrappedResultRecovered',p_provider_operation_receipt,evidence,digest,p_wrapping_suite_id,p_wrapping_suite_version,now_utc);
          EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RETURN 'RecoveredActivated';
        END $recovered$;

        CREATE OR REPLACE FUNCTION tagekyc.raw_export_activate_attempt_key_reservation(p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint)
        RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $activate$
        DECLARE h record; op record; seq bigint; now_utc timestamptz:=pg_catalog.statement_timestamp(); prior_context text:=pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
        BEGIN
          SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
          IF NOT FOUND THEN RETURN 'HeadNotReserved'; END IF; IF h."PreparationDisposition"='Active' THEN RETURN 'AlreadyActivated'; END IF;
          IF h."PreparationDisposition" IN ('Revoked','AbandonRequested','ReservationAbandoned','ProviderCorruptOrUnverifiable') THEN RETURN 'Terminated'; END IF;
          IF h."CurrentPreparationId"<>p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence THEN RETURN 'StalePreparation'; END IF;
          IF h."PreparationDisposition"<>'PreparingLive' THEN RETURN 'HeadNotCurrent'; END IF;
          SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=p_preparation_fence FOR UPDATE;
          IF NOT FOUND OR op."ProviderOperationState"<>'ResultObserved' OR op."AttemptKeyContextFingerprint"<>h."AttemptKeyContextFingerprint" OR op."MaterialRepresentationId"<>h."MaterialRepresentationId" OR op."MaterialRepresentationVersion"<>h."MaterialRepresentationVersion" THEN RETURN 'DigestMismatch'; END IF;
          SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
          BEGIN
            UPDATE tagekyc.raw_export_attempt_key_reservations SET "PreparationDisposition"='Active',"WrappedDekCiphertext"=op."WrappedDekCiphertext","WrappedDekNonce"=op."WrappedDekNonce","WrappedDekTag"=op."WrappedDekTag","OpaqueWrappedDekPayload"=op."OpaqueWrappedDekPayload","WrappedDekMetadataDigest"=op."WrappedDekMetadataDigest","PreparedAtUtc"=now_utc,"NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,"NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
            INSERT INTO tagekyc.raw_export_attempt_key_preparation_events("PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence","EventKind","ProviderOperationReceipt","WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion","EventAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,'DirectActivated',op."ProviderOperationReceipt",op."WrappedDekMetadataDigest",op."WrappingSuiteId",op."WrappingSuiteVersion",now_utc);
          EXCEPTION WHEN OTHERS THEN PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE; END;
          PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RETURN 'Activated';
        END $activate$;

        CREATE FUNCTION tagekyc.raw_export_read_active_attempt_key_material(p_attempt_key_reservation_id uuid)
        RETURNS TABLE(attempt_id uuid,attempt_key_context_fingerprint bytea,key_provider_id text,kek_id text,kek_version integer,kek_fingerprint text,
          material_representation_id text,material_representation_version integer,wrapping_suite_id text,wrapping_suite_version integer,
          wrapped_dek_ciphertext bytea,wrapped_dek_nonce bytea,wrapped_dek_tag bytea,opaque_wrapped_dek_payload bytea,wrapped_dek_metadata_digest bytea)
        LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        SELECT h."AttemptId",h."AttemptKeyContextFingerprint",h."KeyProviderId",h."KekId",h."KekVersion",h."KekFingerprint",
          h."MaterialRepresentationId",h."MaterialRepresentationVersion",h."WrappingSuiteId",h."WrappingSuiteVersion",
          h."WrappedDekCiphertext",h."WrappedDekNonce",h."WrappedDekTag",h."OpaqueWrappedDekPayload",h."WrappedDekMetadataDigest"
        FROM tagekyc.raw_export_attempt_key_reservations h WHERE h."AttemptKeyReservationId"=p_attempt_key_reservation_id AND h."PreparationDisposition"='Active' $fn$;

        CREATE FUNCTION tagekyc.raw_export_openbao_issue_kek_operation(p_token text,p_context bytea,p_provider text,p_kek text,p_kek_version integer,p_kek_fingerprint text,p_representation text,p_representation_version integer,p_scheme text,p_scheme_version integer)
        RETURNS TABLE(outcome text,provider_operation_id uuid,row_revision bigint,journal_state text,opaque_payload bytea,resource_reference text,operation_receipt text,absence_receipt text,cleanup_reference text,cleanup_receipt text,representation_id text,representation_version integer,scheme_id text,scheme_version integer)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE op record; j record; token_digest bytea;
        BEGIN
          token_digest:=tagekyc.raw_export_c1_hash_canonical('tip-88c1-openbao-operation-token-v1',p_token);
          SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations WHERE "ProviderOperationToken"=p_token FOR UPDATE;
          IF NOT FOUND OR op."AttemptKeyContextFingerprint"<>p_context OR op."KeyProviderId"<>p_provider
             OR op."ProviderOperationState" NOT IN ('Issued','ResultObserved') THEN RETURN; END IF;
          SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderOperationId"=op."ProviderOperationId";
          IF FOUND THEN
            IF j."ProviderOperationTokenDigest"<>token_digest OR j."AttemptKeyContextFingerprint"<>p_context OR j."KeyProviderId"<>p_provider OR j."KekId"<>p_kek OR j."KekVersion"<>p_kek_version OR j."KekFingerprint"<>p_kek_fingerprint OR j."MaterialRepresentationId"<>p_representation OR j."MaterialRepresentationVersion"<>p_representation_version OR j."WrappingSchemeId"<>p_scheme OR j."WrappingSchemeVersion"<>p_scheme_version THEN RETURN; END IF;
            IF op."ProviderOperationState"='ResultObserved' AND j."JournalState"<>'Wrapped' THEN RETURN; END IF;
            RETURN QUERY SELECT 'ExistingMatch'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion"; RETURN;
          END IF;
          IF op."ProviderOperationState"<>'Issued' THEN RETURN; END IF;
          INSERT INTO tagekyc.raw_export_openbao_kek_operation_journal("ProviderOperationId","ProviderOperationTokenDigest","AttemptKeyContextFingerprint","PreparationId","PreparationFence","KeyProviderId","KekId","KekVersion","KekFingerprint","JournalState","MaterialRepresentationId","MaterialRepresentationVersion","WrappingSchemeId","WrappingSchemeVersion","RowRevision","IssuedAtUtc","UpdatedAtUtc")
          VALUES(op."ProviderOperationId",token_digest,p_context,op."PreparationId",op."PreparationFence",p_provider,p_kek,p_kek_version,p_kek_fingerprint,'Issued',p_representation,p_representation_version,p_scheme,p_scheme_version,1,pg_catalog.statement_timestamp(),pg_catalog.statement_timestamp());
          SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderOperationId"=op."ProviderOperationId";
          RETURN QUERY SELECT 'Issued'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion";
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_openbao_record_wrapped(p_token text,p_context bytea,p_expected_revision bigint,p_payload bytea,p_resource text)
        RETURNS TABLE(outcome text,provider_operation_id uuid,row_revision bigint,journal_state text,opaque_payload bytea,resource_reference text,operation_receipt text,absence_receipt text,cleanup_reference text,cleanup_receipt text,representation_id text,representation_version integer,scheme_id text,scheme_version integer)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE j record; receipt text; now_utc timestamptz:=pg_catalog.statement_timestamp();
        BEGIN
          SELECT jn.* INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal jn JOIN tagekyc.raw_export_key_provider_operations op ON op."ProviderOperationId"=jn."ProviderOperationId" WHERE op."ProviderOperationToken"=p_token FOR UPDATE OF jn;
          IF NOT FOUND OR j."AttemptKeyContextFingerprint"<>p_context THEN RETURN; END IF;
          IF j."JournalState"='Wrapped' THEN RETURN QUERY SELECT CASE WHEN j."OpaqueWrappedDekPayload"=p_payload AND j."ProviderResourceReference"=p_resource THEN 'ExistingMatch' ELSE 'Conflict' END,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion"; RETURN; END IF;
          IF j."JournalState"<>'Issued' OR j."RowRevision"<>p_expected_revision OR pg_catalog.octet_length(p_payload) NOT BETWEEN 1 AND 4096 OR p_resource IS NULL THEN RETURN; END IF;
          receipt:='openbao-wrap:'||pg_catalog.encode(tagekyc.raw_export_c1_hash_canonical('tip-88c1-openbao-wrap-receipt-v1',j."KeyProviderId",pg_catalog.encode(j."ProviderOperationTokenDigest",'hex'),pg_catalog.encode(j."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(j."PreparationId"::text,'-',''),j."PreparationFence"::text,j."RowRevision"::text,(j."RowRevision"+1)::text,pg_catalog.encode(p_payload,'hex'),p_resource,now_utc::text),'hex');
          UPDATE tagekyc.raw_export_openbao_kek_operation_journal SET "JournalState"='Wrapped',"OpaqueWrappedDekPayload"=p_payload,"ProviderResourceReference"=p_resource,"ProviderOperationReceipt"=receipt,"ResultObservedAtUtc"=now_utc,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc WHERE "ProviderOperationId"=j."ProviderOperationId" AND "JournalState"='Issued' AND "RowRevision"=p_expected_revision;
          IF NOT FOUND THEN RETURN; END IF; SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderOperationId"=j."ProviderOperationId";
          RETURN QUERY SELECT 'Wrapped'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion";
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_openbao_read_kek_operation(p_token text,p_context bytea)
        RETURNS TABLE(outcome text,provider_operation_id uuid,row_revision bigint,journal_state text,opaque_payload bytea,resource_reference text,operation_receipt text,absence_receipt text,cleanup_reference text,cleanup_receipt text,representation_id text,representation_version integer,scheme_id text,scheme_version integer)
        LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        SELECT 'Found'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference",j."ProviderOperationReceipt",j."ProviderAbsenceProofReceipt",j."ProviderCleanupReference",j."ProviderCleanupReceipt",j."MaterialRepresentationId",j."MaterialRepresentationVersion",j."WrappingSchemeId",j."WrappingSchemeVersion"
        FROM tagekyc.raw_export_openbao_kek_operation_journal j JOIN tagekyc.raw_export_key_provider_operations op ON op."ProviderOperationId"=j."ProviderOperationId" WHERE op."ProviderOperationToken"=p_token AND j."AttemptKeyContextFingerprint"=p_context $fn$;

        CREATE FUNCTION tagekyc.raw_export_openbao_prove_absence(p_token text,p_context bytea,p_expected_revision bigint)
        RETURNS TABLE(outcome text,provider_operation_id uuid,row_revision bigint,journal_state text,opaque_payload bytea,resource_reference text,operation_receipt text,absence_receipt text,cleanup_reference text,cleanup_receipt text,representation_id text,representation_version integer,scheme_id text,scheme_version integer)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE j record; receipt text; now_utc timestamptz:=pg_catalog.statement_timestamp();
        BEGIN
          SELECT jn.* INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal jn JOIN tagekyc.raw_export_key_provider_operations op ON op."ProviderOperationId"=jn."ProviderOperationId" JOIN tagekyc.raw_export_attempt_key_reservations h ON h."AttemptKeyReservationId"=op."AttemptKeyReservationId" WHERE op."ProviderOperationToken"=p_token AND jn."AttemptKeyContextFingerprint"=p_context AND op."PreparationId"=h."CurrentPreparationId" AND op."PreparationFence"=h."CurrentPreparationFence" AND h."PreparationDisposition" IN ('PreparingExpiredAwaitingResolution','ProviderOutcomeUnknown') FOR UPDATE OF jn;
          IF NOT FOUND OR j."JournalState"<>'Issued' OR j."RowRevision"<>p_expected_revision THEN RETURN; END IF;
          receipt:='openbao-absence:'||pg_catalog.encode(tagekyc.raw_export_c1_hash_canonical('tip-88c1-openbao-absence-receipt-v1',j."KeyProviderId",pg_catalog.encode(j."ProviderOperationTokenDigest",'hex'),pg_catalog.encode(j."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(j."PreparationId"::text,'-',''),j."PreparationFence"::text,j."JournalState",j."RowRevision"::text,'AbsenceProven',(j."RowRevision"+1)::text,now_utc::text),'hex');
          UPDATE tagekyc.raw_export_openbao_kek_operation_journal SET "JournalState"='AbsenceProven',"ProviderAbsenceProofReceipt"=receipt,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc WHERE "ProviderOperationId"=j."ProviderOperationId" AND "JournalState"='Issued' AND "RowRevision"=p_expected_revision;
          IF NOT FOUND THEN RETURN; END IF; SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderOperationId"=j."ProviderOperationId";
          RETURN QUERY SELECT 'AbsenceProven'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion";
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_openbao_require_cleanup(p_token text,p_context bytea,p_expected_revision bigint)
        RETURNS TABLE(outcome text,provider_operation_id uuid,row_revision bigint,journal_state text,opaque_payload bytea,resource_reference text,operation_receipt text,absence_receipt text,cleanup_reference text,cleanup_receipt text,representation_id text,representation_version integer,scheme_id text,scheme_version integer)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE j record; cleanup text;
        BEGIN
          SELECT jn.* INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal jn JOIN tagekyc.raw_export_key_provider_operations op ON op."ProviderOperationId"=jn."ProviderOperationId" WHERE op."ProviderOperationToken"=p_token AND jn."AttemptKeyContextFingerprint"=p_context FOR UPDATE OF jn;
          IF NOT FOUND OR j."JournalState"<>'Wrapped' OR j."RowRevision"<>p_expected_revision THEN RETURN; END IF;
          cleanup:='openbao-cleanup:'||pg_catalog.encode(tagekyc.raw_export_c1_hash_canonical('tip-88c1-openbao-cleanup-reference-v1',j."KeyProviderId",pg_catalog.encode(j."ProviderOperationTokenDigest",'hex'),pg_catalog.encode(j."AttemptKeyContextFingerprint",'hex'),j."RowRevision"::text),'hex');
          UPDATE tagekyc.raw_export_openbao_kek_operation_journal SET "JournalState"='CleanupRequired',"ProviderCleanupReference"=cleanup,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=pg_catalog.statement_timestamp() WHERE "ProviderOperationId"=j."ProviderOperationId" AND "JournalState"='Wrapped' AND "RowRevision"=p_expected_revision;
          SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderOperationId"=j."ProviderOperationId";
          RETURN QUERY SELECT 'CleanupRequired'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion";
        END $fn$;

        CREATE FUNCTION tagekyc.raw_export_openbao_complete_cleanup(p_cleanup text,p_context bytea)
        RETURNS TABLE(outcome text,provider_operation_id uuid,row_revision bigint,journal_state text,opaque_payload bytea,resource_reference text,operation_receipt text,absence_receipt text,cleanup_reference text,cleanup_receipt text,representation_id text,representation_version integer,scheme_id text,scheme_version integer)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $fn$
        DECLARE j record; receipt text; now_utc timestamptz:=pg_catalog.statement_timestamp();
        BEGIN
          SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderCleanupReference"=p_cleanup AND "AttemptKeyContextFingerprint"=p_context FOR UPDATE;
          IF NOT FOUND THEN RETURN; END IF;
          IF j."JournalState"='CleanedUp' THEN RETURN QUERY SELECT 'AlreadyAbsent'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion"; RETURN; END IF;
          IF j."JournalState"<>'CleanupRequired' THEN RETURN; END IF;
          receipt:='openbao-cleaned:'||pg_catalog.encode(tagekyc.raw_export_c1_hash_canonical('tip-88c1-openbao-cleanup-receipt-v1',j."KeyProviderId",pg_catalog.encode(j."ProviderOperationTokenDigest",'hex'),pg_catalog.encode(j."AttemptKeyContextFingerprint",'hex'),pg_catalog.replace(j."PreparationId"::text,'-',''),j."PreparationFence"::text,j."JournalState",j."RowRevision"::text,'CleanedUp',(j."RowRevision"+1)::text,now_utc::text),'hex');
          UPDATE tagekyc.raw_export_openbao_kek_operation_journal SET "JournalState"='CleanedUp',"OpaqueWrappedDekPayload"=NULL,"ProviderCleanupReceipt"=receipt,"CleanedAtUtc"=now_utc,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc WHERE "ProviderOperationId"=j."ProviderOperationId" AND "JournalState"='CleanupRequired' AND "RowRevision"=j."RowRevision";
          SELECT * INTO j FROM tagekyc.raw_export_openbao_kek_operation_journal WHERE "ProviderOperationId"=j."ProviderOperationId";
          RETURN QUERY SELECT 'Cleaned'::text,j."ProviderOperationId",j."RowRevision",j."JournalState",j."OpaqueWrappedDekPayload",j."ProviderResourceReference"::text,j."ProviderOperationReceipt"::text,j."ProviderAbsenceProofReceipt"::text,j."ProviderCleanupReference"::text,j."ProviderCleanupReceipt"::text,j."MaterialRepresentationId"::text,j."MaterialRepresentationVersion",j."WrappingSchemeId"::text,j."WrappingSchemeVersion";
        END $fn$;

        ALTER FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid,text,integer,text,integer) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_read_active_attempt_key_material(uuid) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_openbao_issue_kek_operation(text,bytea,text,text,integer,text,text,integer,text,integer) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_openbao_record_wrapped(text,bytea,bigint,bytea,text) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_openbao_read_kek_operation(text,bytea) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_openbao_prove_absence(text,bytea,bigint) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_openbao_require_cleanup(text,bytea,bigint) OWNER TO tagekyc_raw_export_deployer;
        ALTER FUNCTION tagekyc.raw_export_openbao_complete_cleanup(text,bytea) OWNER TO tagekyc_raw_export_deployer;

        REVOKE ALL ON FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid) FROM PUBLIC,tagekyc_raw_export_custody_encryptor;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text) FROM PUBLIC,tagekyc_raw_export_custody_encryptor;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text) FROM PUBLIC,tagekyc_raw_export_reconciler;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_read_active_attempt_key_envelope(uuid) FROM PUBLIC,tagekyc_raw_export_custody_encryptor,tagekyc_raw_export_reconciler;
        REVOKE ALL ON FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid,text,integer,text,integer),tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text),tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text),tagekyc.raw_export_read_active_attempt_key_material(uuid),tagekyc.raw_export_openbao_issue_kek_operation(text,bytea,text,text,integer,text,text,integer,text,integer),tagekyc.raw_export_openbao_record_wrapped(text,bytea,bigint,bytea,text),tagekyc.raw_export_openbao_read_kek_operation(text,bytea),tagekyc.raw_export_openbao_prove_absence(text,bytea,bigint),tagekyc.raw_export_openbao_require_cleanup(text,bytea,bigint),tagekyc.raw_export_openbao_complete_cleanup(text,bytea) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid,text,integer,text,integer),tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text),tagekyc.raw_export_read_active_attempt_key_material(uuid),tagekyc.raw_export_openbao_issue_kek_operation(text,bytea,text,text,integer,text,text,integer,text,integer),tagekyc.raw_export_openbao_record_wrapped(text,bytea,bigint,bytea,text) TO tagekyc_raw_export_custody_encryptor;
        GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_active_attempt_key_material(uuid),tagekyc.raw_export_openbao_read_kek_operation(text,bytea),tagekyc.raw_export_openbao_prove_absence(text,bytea,bigint),tagekyc.raw_export_openbao_require_cleanup(text,bytea,bigint),tagekyc.raw_export_openbao_complete_cleanup(text,bytea) TO tagekyc_raw_export_reconciler;
        GRANT EXECUTE ON FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text) TO tagekyc_raw_export_reconciler;
        """;

    private const string DownProviderSql = """
        DO $guard$
        BEGIN
          IF EXISTS(SELECT 1 FROM tagekyc.raw_export_attempt_key_reservations WHERE "MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT')
             OR EXISTS(SELECT 1 FROM tagekyc.raw_export_key_provider_operations WHERE "MaterialRepresentationId"='OPAQUE_PROVIDER_CIPHERTEXT')
             OR EXISTS(SELECT 1 FROM tagekyc.raw_export_openbao_kek_operation_journal) THEN
            RAISE EXCEPTION 'REPRESENTATION_DOWNGRADE_NOT_LOSSLESS';
          END IF;
        END $guard$;
        DROP FUNCTION tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid,text,integer,text,integer);
        DROP FUNCTION tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text);
        DROP FUNCTION tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,text,integer,bytea,bytea,bytea,bytea,text,integer,text,text);
        DROP FUNCTION tagekyc.raw_export_read_active_attempt_key_material(uuid);
        DROP FUNCTION tagekyc.raw_export_openbao_issue_kek_operation(text,bytea,text,text,integer,text,text,integer,text,integer);
        DROP FUNCTION tagekyc.raw_export_openbao_record_wrapped(text,bytea,bigint,bytea,text);
        DROP FUNCTION tagekyc.raw_export_openbao_read_kek_operation(text,bytea);
        DROP FUNCTION tagekyc.raw_export_openbao_prove_absence(text,bytea,bigint);
        DROP FUNCTION tagekyc.raw_export_openbao_require_cleanup(text,bytea,bigint);
        DROP FUNCTION tagekyc.raw_export_openbao_complete_cleanup(text,bytea);

        CREATE OR REPLACE FUNCTION tagekyc.raw_export_activate_attempt_key_reservation(
            p_attempt_key_reservation_id uuid,p_preparation_id uuid,p_preparation_fence bigint)
        RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
        AS $activate$
        DECLARE
            h record; op record; seq bigint; now_utc timestamptz:=pg_catalog.statement_timestamp();
            prior_context text := pg_catalog.current_setting('tagekyc.raw_export_attempt_key_write_context',true);
        BEGIN
            SELECT * INTO h FROM tagekyc.raw_export_attempt_key_reservations
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id FOR UPDATE;
            IF NOT FOUND THEN RETURN 'HeadNotReserved'; END IF;
            IF h."PreparationDisposition"='Active' THEN RETURN 'AlreadyActivated'; END IF;
            IF h."PreparationDisposition" IN ('Revoked','AbandonRequested','ReservationAbandoned','ProviderCorruptOrUnverifiable') THEN RETURN 'Terminated'; END IF;
            IF h."CurrentPreparationId"<>p_preparation_id OR h."CurrentPreparationFence"<>p_preparation_fence THEN RETURN 'StalePreparation'; END IF;
            IF h."PreparationDisposition"<>'PreparingLive' THEN RETURN 'HeadNotCurrent'; END IF;
            SELECT * INTO op FROM tagekyc.raw_export_key_provider_operations
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id AND "PreparationFence"=p_preparation_fence FOR UPDATE;
            IF NOT FOUND OR op."ProviderOperationState"<>'ResultObserved' THEN RETURN 'DigestMismatch'; END IF;
            IF op."AttemptKeyContextFingerprint"<>h."AttemptKeyContextFingerprint" THEN RETURN 'DigestMismatch'; END IF;
            SELECT COALESCE(MAX("EventSequence"),0)+1 INTO seq FROM tagekyc.raw_export_attempt_key_preparation_events
            WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
            PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
            BEGIN
                UPDATE tagekyc.raw_export_attempt_key_reservations SET
                    "PreparationDisposition"='Active',"WrappedDekCiphertext"=op."WrappedDekCiphertext",
                    "WrappedDekNonce"=op."WrappedDekNonce","WrappedDekTag"=op."WrappedDekTag",
                    "WrappedDekMetadataDigest"=op."WrappedDekMetadataDigest","PreparedAtUtc"=now_utc,
                    "NextResolutionAttemptNotBeforeUtc"=NULL,"ResolutionDeadlineUtc"=NULL,
                    "NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
                    "RowRevision"="RowRevision"+1,"UpdatedAtUtc"=now_utc
                WHERE "AttemptKeyReservationId"=p_attempt_key_reservation_id;
                INSERT INTO tagekyc.raw_export_attempt_key_preparation_events(
                    "PreparationEventId","AttemptKeyReservationId","PreparationId","PreparationFence","EventSequence",
                    "EventKind","ProviderOperationReceipt","WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion","EventAtUtc")
                VALUES(pg_catalog.gen_random_uuid(),p_attempt_key_reservation_id,p_preparation_id,p_preparation_fence,seq,
                    'DirectActivated',op."ProviderOperationReceipt",op."WrappedDekMetadataDigest",op."WrappingSuiteId",op."WrappingSuiteVersion",now_utc);
            EXCEPTION WHEN OTHERS THEN
                PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true); RAISE;
            END;
            PERFORM pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context',COALESCE(prior_context,''),true);
            RETURN 'Activated';
        END
        $activate$;

        GRANT EXECUTE ON FUNCTION
            tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid),
            tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text),
            tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint),
            tagekyc.raw_export_read_active_attempt_key_envelope(uuid)
            TO tagekyc_raw_export_custody_encryptor;
        GRANT EXECUTE ON FUNCTION
            tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text),
            tagekyc.raw_export_read_active_attempt_key_envelope(uuid)
            TO tagekyc_raw_export_reconciler;

        DO $guard$
        DECLARE expected record; actual text;
        BEGIN
          FOR expected IN SELECT * FROM (VALUES
            ('tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)','c9207a7fbd9192bea2ad534fa1f26d43c246e03f4704de1e6055c198f956f66b'),
            ('tagekyc.raw_export_record_key_provider_wrapped_result(uuid,uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)','9e4e0375b2cf55c293bd92a1cf79563ae7d3aec9e6d92d20a37979f4f506236b'),
            ('tagekyc.raw_export_record_recovered_key_provider_result(uuid,uuid,bigint,text,bytea,bytea,bytea,text,integer,text,text)','7598eadb380484c395959e7fca164c34d166499ff99920d7dbebf5df24a87148'),
            ('tagekyc.raw_export_read_active_attempt_key_envelope(uuid)','daa1de12ecdbf1b73e72e87bbd50ff892581962031966e681073c06caa3057e9'),
            ('tagekyc.raw_export_activate_attempt_key_reservation(uuid,uuid,bigint)','3292839f9446df41453efa2d8b0644bd34200970854014d44390b01f396f1004')
          ) AS x(signature,sha256)
          LOOP
            SELECT p.prosrc INTO actual FROM pg_catalog.pg_proc p
            WHERE p.oid=pg_catalog.to_regprocedure(expected.signature);
            IF NOT FOUND OR pg_catalog.encode(tagekyc_extensions.digest(pg_catalog.convert_to(pg_catalog.replace(actual,pg_catalog.chr(13)||pg_catalog.chr(10),pg_catalog.chr(10)),'UTF8'),'sha256'),'hex')<>expected.sha256 THEN
              RAISE EXCEPTION 'OPENBAO_REPRESENTATION_DOWN_BODY_MISMATCH' USING DETAIL=expected.signature;
            END IF;
          END LOOP;
        END $guard$;
        """;
}
