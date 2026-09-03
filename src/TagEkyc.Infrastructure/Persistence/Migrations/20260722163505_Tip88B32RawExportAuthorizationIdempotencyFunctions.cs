using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88B32RawExportAuthorizationIdempotencyFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION tagekyc.raw_export_lock_verification_session_for_authorization(
                    verification_session_id uuid)
                RETURNS TABLE("ClientApplicationId" uuid, "SubjectRef" text, "State" text)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                    SELECT session."ClientApplicationId", session."SubjectRef", session."State"
                    FROM tagekyc.verification_sessions AS session
                    WHERE session."Id" = verification_session_id
                    FOR UPDATE;
                $$;

                CREATE FUNCTION tagekyc.raw_export_claim_or_read_authorization_idempotency(
                    principal_id uuid,
                    client_application_id uuid,
                    requested_verification_session_id uuid,
                    idempotency_key text,
                    fingerprint_hash bytea,
                    prospective_fresh_export_decision_id uuid)
                RETURNS TABLE(outcome text, export_decision_id uuid)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $$
                DECLARE
                    actor_id uuid;
                    existing_fingerprint bytea;
                    existing_decision_id uuid;
                    claimed_decision_id uuid;
                BEGIN
                    actor_id := tagekyc.raw_export_current_actor();
                    IF principal_id IS DISTINCT FROM actor_id THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'P0001',
                            MESSAGE = 'RAW_EXPORT_AUTHORIZATION_ACTOR_MISMATCH';
                    END IF;

                    LOOP
                        SELECT claim."FingerprintHash", claim."ExportDecisionId"
                        INTO existing_fingerprint, existing_decision_id
                        FROM tagekyc.raw_export_authorization_idempotency AS claim
                        WHERE claim."PrincipalId" = principal_id
                          AND claim."ClientApplicationId" = client_application_id
                          AND claim."RequestedVerificationSessionId" = requested_verification_session_id
                          AND claim."IdempotencyKey" = idempotency_key;

                        IF FOUND THEN
                            IF existing_fingerprint = fingerprint_hash THEN
                                outcome := 'ExistingMatch';
                                export_decision_id := existing_decision_id;
                            ELSE
                                outcome := 'FingerprintConflict';
                                export_decision_id := NULL;
                            END IF;
                            RETURN NEXT;
                            RETURN;
                        END IF;

                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_authorization_insert_context',
                            'idempotency',
                            true);
                        INSERT INTO tagekyc.raw_export_authorization_idempotency AS claim
                            ("PrincipalId", "ClientApplicationId", "RequestedVerificationSessionId",
                             "IdempotencyKey", "FingerprintHash", "ExportDecisionId", "CreatedAt")
                        VALUES
                            (principal_id, client_application_id, requested_verification_session_id,
                             idempotency_key, fingerprint_hash, prospective_fresh_export_decision_id,
                             pg_catalog.transaction_timestamp())
                        ON CONFLICT
                            ("PrincipalId", "ClientApplicationId", "RequestedVerificationSessionId", "IdempotencyKey")
                        DO NOTHING
                        RETURNING claim."ExportDecisionId" INTO claimed_decision_id;

                        IF FOUND THEN
                            outcome := 'NewClaim';
                            export_decision_id := claimed_decision_id;
                            RETURN NEXT;
                            RETURN;
                        END IF;
                    END LOOP;
                END;
                $$;

                ALTER FUNCTION tagekyc.raw_export_lock_verification_session_for_authorization(uuid)
                    OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_claim_or_read_authorization_idempotency(uuid,uuid,uuid,text,bytea,uuid)
                    OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON FUNCTION
                    tagekyc.raw_export_lock_verification_session_for_authorization(uuid),
                    tagekyc.raw_export_claim_or_read_authorization_idempotency(uuid,uuid,uuid,text,bytea,uuid)
                FROM PUBLIC;

                GRANT EXECUTE ON FUNCTION
                    tagekyc.raw_export_lock_verification_session_for_authorization(uuid),
                    tagekyc.raw_export_claim_or_read_authorization_idempotency(uuid,uuid,uuid,text,bytea,uuid)
                TO tagekyc_runtime;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE EXECUTE ON FUNCTION
                    tagekyc.raw_export_lock_verification_session_for_authorization(uuid),
                    tagekyc.raw_export_claim_or_read_authorization_idempotency(uuid,uuid,uuid,text,bytea,uuid)
                FROM tagekyc_runtime;

                DROP FUNCTION IF EXISTS tagekyc.raw_export_claim_or_read_authorization_idempotency(uuid,uuid,uuid,text,bytea,uuid);
                DROP FUNCTION IF EXISTS tagekyc.raw_export_lock_verification_session_for_authorization(uuid);
                """);

        }
    }
}
