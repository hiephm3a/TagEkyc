using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tip88C1B2DurableKeyFixtureProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $roles$
                DECLARE role_name text; role_row record;
                BEGIN
                    FOREACH role_name IN ARRAY ARRAY[
                        'tagekyc_raw_export_fixture_kek_wrap_executor',
                        'tagekyc_raw_export_fixture_kek_lookup_executor']
                    LOOP
                        SELECT * INTO role_row FROM pg_catalog.pg_roles WHERE rolname=role_name;
                        IF FOUND THEN
                            IF role_row.rolcanlogin OR role_row.rolsuper OR role_row.rolcreatedb
                               OR role_row.rolcreaterole OR role_row.rolreplication
                               OR role_row.rolbypassrls OR NOT role_row.rolinherit THEN
                                RAISE EXCEPTION USING ERRCODE='P0001',
                                    MESSAGE='RAW_EXPORT_FIXTURE_KEK_ROLE_INVALID';
                            END IF;
                        ELSE
                            EXECUTE pg_catalog.format(
                                'CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',
                                role_name);
                        END IF;
                    END LOOP;
                END $roles$;
                """);

            migrationBuilder.CreateTable(
                name: "raw_export_fixture_kek_wrap_journal",
                schema: "tagekyc",
                columns: table => new
                {
                    FixtureWrapId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyProviderId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProviderOperationToken = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                    AttemptKeyContextFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    WrappingSuiteId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    WrappingSuiteVersion = table.Column<int>(type: "integer", nullable: false),
                    WrappedDekCiphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    WrappedDekNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    WrappedDekTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    ProviderResourceReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProviderOperationReceipt = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    WrappedDekMetadataDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_raw_export_fixture_kek_wrap_journal", x => x.FixtureWrapId);
                    table.CheckConstraint("ck_raw_export_fixture_kek_wrap_shape", "\"KeyProviderId\" = 'fixture-kek-provider-v1'\nAND \"ProviderOperationToken\" ~ '^[A-Za-z0-9_-]{43}$'\nAND octet_length(\"AttemptKeyContextFingerprint\") = 32\nAND \"WrappingSuiteId\" = 'AES-256-GCM'\nAND \"WrappingSuiteVersion\" = 1\nAND octet_length(\"WrappedDekCiphertext\") = 32\nAND octet_length(\"WrappedDekNonce\") = 12\nAND octet_length(\"WrappedDekTag\") = 16\nAND octet_length(\"WrappedDekMetadataDigest\") = 32\nAND \"ProviderResourceReference\" = 'fixture-wrap:' || replace(\"FixtureWrapId\"::text, '-', '')");
                    table.CheckConstraint("ck_raw_export_fixture_kek_wrap_text", "\"KeyProviderId\"=btrim(\"KeyProviderId\") AND \"KeyProviderId\"=normalize(\"KeyProviderId\",NFC)\n  AND octet_length(\"KeyProviderId\") BETWEEN 1 AND 512 AND \"KeyProviderId\" !~ '[\\x00-\\x1f\\x7f]'\nAND \"WrappingSuiteId\"=btrim(\"WrappingSuiteId\") AND \"WrappingSuiteId\"=normalize(\"WrappingSuiteId\",NFC)\n  AND octet_length(\"WrappingSuiteId\") BETWEEN 1 AND 512 AND \"WrappingSuiteId\" !~ '[\\x00-\\x1f\\x7f]'\nAND \"ProviderResourceReference\"=btrim(\"ProviderResourceReference\") AND \"ProviderResourceReference\"=normalize(\"ProviderResourceReference\",NFC)\n  AND octet_length(\"ProviderResourceReference\") BETWEEN 1 AND 512 AND \"ProviderResourceReference\" !~ '[\\x00-\\x1f\\x7f]'\nAND \"ProviderOperationReceipt\"=btrim(\"ProviderOperationReceipt\") AND \"ProviderOperationReceipt\"=normalize(\"ProviderOperationReceipt\",NFC)\n  AND octet_length(\"ProviderOperationReceipt\") BETWEEN 1 AND 512 AND \"ProviderOperationReceipt\" !~ '[\\x00-\\x1f\\x7f]'");
                });

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_fixture_kek_provider_token",
                schema: "tagekyc",
                table: "raw_export_fixture_kek_wrap_journal",
                columns: new[] { "KeyProviderId", "ProviderOperationToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_raw_export_fixture_kek_resource_ref",
                schema: "tagekyc",
                table: "raw_export_fixture_kek_wrap_journal",
                column: "ProviderResourceReference",
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE tagekyc.raw_export_fixture_kek_wrap_journal
                  OWNER TO tagekyc_raw_export_deployer;

                CREATE FUNCTION tagekyc.enforce_raw_export_fixture_kek_wrap_guard()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $guard$
                BEGIN
                    IF TG_OP IN ('UPDATE','DELETE') THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_FIXTURE_KEK_JOURNAL_APPEND_ONLY';
                    END IF;
                    IF current_user <> 'tagekyc_raw_export_deployer'
                       OR pg_catalog.current_setting(
                            'tagekyc.raw_export_fixture_kek_write_context',true)
                          IS DISTINCT FROM 'active' THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_FIXTURE_KEK_WRITE_CONTEXT_INVALID';
                    END IF;
                    RETURN NEW;
                END $guard$;

                CREATE TRIGGER trg_raw_export_fixture_kek_wrap_guard
                BEFORE INSERT OR UPDATE OR DELETE
                ON tagekyc.raw_export_fixture_kek_wrap_journal
                FOR EACH ROW EXECUTE FUNCTION
                  tagekyc.enforce_raw_export_fixture_kek_wrap_guard();

                CREATE FUNCTION tagekyc.raw_export_fixture_kek_wrap(
                    p_key_provider_id text,
                    p_provider_operation_token text,
                    p_attempt_key_context_fingerprint bytea,
                    p_wrapped_dek_ciphertext bytea,
                    p_wrapped_dek_nonce bytea,
                    p_wrapped_dek_tag bytea,
                    p_wrapping_suite_id text,
                    p_wrapping_suite_version integer)
                RETURNS TABLE(
                    outcome text,
                    fixture_wrap_id uuid,
                    wrapped_dek_ciphertext bytea,
                    wrapped_dek_nonce bytea,
                    wrapped_dek_tag bytea,
                    wrapping_suite_id text,
                    wrapping_suite_version integer,
                    provider_resource_reference text,
                    provider_operation_receipt text,
                    wrapped_dek_metadata_digest bytea)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $wrap$
                DECLARE
                    existing tagekyc.raw_export_fixture_kek_wrap_journal%ROWTYPE;
                    candidate_id uuid;
                    candidate_digest bytea;
                    candidate_resource text;
                    candidate_receipt text;
                    expected_digest bytea;
                    expected_resource text;
                    expected_receipt text;
                    prior_context text;
                    inserted_count integer;
                    creation_null boolean;
                    creation_present boolean;
                BEGIN
                    creation_null := p_wrapped_dek_ciphertext IS NULL
                        AND p_wrapped_dek_nonce IS NULL AND p_wrapped_dek_tag IS NULL
                        AND p_wrapping_suite_id IS NULL AND p_wrapping_suite_version IS NULL;
                    creation_present := p_wrapped_dek_ciphertext IS NOT NULL
                        AND p_wrapped_dek_nonce IS NOT NULL AND p_wrapped_dek_tag IS NOT NULL
                        AND p_wrapping_suite_id IS NOT NULL AND p_wrapping_suite_version IS NOT NULL;
                    IF p_key_provider_id IS DISTINCT FROM 'fixture-kek-provider-v1'
                       OR p_provider_operation_token IS NULL
                       OR p_provider_operation_token !~ '^[A-Za-z0-9_-]{43}$'
                       OR p_attempt_key_context_fingerprint IS NULL
                       OR pg_catalog.octet_length(p_attempt_key_context_fingerprint)<>32
                       OR NOT (creation_null OR creation_present)
                       OR (creation_present AND (
                            pg_catalog.octet_length(p_wrapped_dek_ciphertext)<>32
                            OR pg_catalog.octet_length(p_wrapped_dek_nonce)<>12
                            OR pg_catalog.octet_length(p_wrapped_dek_tag)<>16
                            OR p_wrapping_suite_id IS DISTINCT FROM 'AES-256-GCM'
                            OR p_wrapping_suite_version IS DISTINCT FROM 1)) THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID';
                    END IF;

                    SELECT * INTO existing
                    FROM tagekyc.raw_export_fixture_kek_wrap_journal j
                    WHERE j."KeyProviderId"=p_key_provider_id
                      AND j."ProviderOperationToken"=p_provider_operation_token;
                    IF FOUND THEN
                        IF existing."AttemptKeyContextFingerprint"
                           IS DISTINCT FROM p_attempt_key_context_fingerprint THEN
                            RAISE EXCEPTION USING ERRCODE='P0001',
                                MESSAGE='RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH';
                        END IF;
                        expected_digest:=tagekyc.raw_export_c1_hash_canonical(
                            'tip-88c1-wrapped-dek-metadata-v1',
                            pg_catalog.encode(existing."AttemptKeyContextFingerprint",'hex'),
                            existing."WrappingSuiteId",existing."WrappingSuiteVersion"::text,
                            pg_catalog.encode(existing."WrappedDekNonce",'hex'),
                            pg_catalog.encode(existing."WrappedDekCiphertext",'hex'),
                            pg_catalog.encode(existing."WrappedDekTag",'hex'));
                        expected_resource:='fixture-wrap:' ||
                            pg_catalog.replace(existing."FixtureWrapId"::text,'-','');
                        expected_receipt:='fixture-receipt:' || pg_catalog.encode(
                            tagekyc.raw_export_c1_hash_canonical(
                                'tip-88c1-fixture-kek-receipt-v1',expected_resource,
                                pg_catalog.encode(existing."AttemptKeyContextFingerprint",'hex'),
                                pg_catalog.encode(expected_digest,'hex')),'hex');
                        IF existing."WrappedDekMetadataDigest" IS DISTINCT FROM expected_digest
                           OR existing."ProviderResourceReference" IS DISTINCT FROM expected_resource
                           OR existing."ProviderOperationReceipt" IS DISTINCT FROM expected_receipt THEN
                            RETURN QUERY SELECT 'CorruptOrUnverifiable'::text,NULL::uuid,
                                NULL::bytea,NULL::bytea,NULL::bytea,NULL::text,NULL::integer,
                                NULL::text,NULL::text,NULL::bytea;
                            RETURN;
                        END IF;
                        RETURN QUERY SELECT 'ExistingMatch'::text,existing."FixtureWrapId",
                            existing."WrappedDekCiphertext",existing."WrappedDekNonce",
                            existing."WrappedDekTag",existing."WrappingSuiteId"::text,
                            existing."WrappingSuiteVersion",existing."ProviderResourceReference"::text,
                            existing."ProviderOperationReceipt"::text,existing."WrappedDekMetadataDigest";
                        RETURN;
                    END IF;
                    IF creation_null THEN
                        RETURN QUERY SELECT 'Missing'::text,NULL::uuid,NULL::bytea,NULL::bytea,
                            NULL::bytea,NULL::text,NULL::integer,NULL::text,NULL::text,NULL::bytea;
                        RETURN;
                    END IF;

                    candidate_id:=pg_catalog.gen_random_uuid();
                    candidate_digest:=tagekyc.raw_export_c1_hash_canonical(
                        'tip-88c1-wrapped-dek-metadata-v1',
                        pg_catalog.encode(p_attempt_key_context_fingerprint,'hex'),
                        p_wrapping_suite_id,p_wrapping_suite_version::text,
                        pg_catalog.encode(p_wrapped_dek_nonce,'hex'),
                        pg_catalog.encode(p_wrapped_dek_ciphertext,'hex'),
                        pg_catalog.encode(p_wrapped_dek_tag,'hex'));
                    candidate_resource:='fixture-wrap:' ||
                        pg_catalog.replace(candidate_id::text,'-','');
                    candidate_receipt:='fixture-receipt:' || pg_catalog.encode(
                        tagekyc.raw_export_c1_hash_canonical(
                            'tip-88c1-fixture-kek-receipt-v1',candidate_resource,
                            pg_catalog.encode(p_attempt_key_context_fingerprint,'hex'),
                            pg_catalog.encode(candidate_digest,'hex')),'hex');
                    prior_context:=pg_catalog.current_setting(
                        'tagekyc.raw_export_fixture_kek_write_context',true);
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_fixture_kek_write_context','active',true);
                    BEGIN
                        INSERT INTO tagekyc.raw_export_fixture_kek_wrap_journal(
                            "FixtureWrapId","KeyProviderId","ProviderOperationToken",
                            "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion",
                            "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag",
                            "ProviderResourceReference","ProviderOperationReceipt",
                            "WrappedDekMetadataDigest","CreatedAtUtc")
                        VALUES(candidate_id,p_key_provider_id,p_provider_operation_token,
                            p_attempt_key_context_fingerprint,p_wrapping_suite_id,
                            p_wrapping_suite_version,p_wrapped_dek_ciphertext,
                            p_wrapped_dek_nonce,p_wrapped_dek_tag,candidate_resource,
                            candidate_receipt,candidate_digest,pg_catalog.statement_timestamp())
                        ON CONFLICT ("KeyProviderId","ProviderOperationToken") DO NOTHING;
                        GET DIAGNOSTICS inserted_count=ROW_COUNT;
                    EXCEPTION WHEN OTHERS THEN
                        PERFORM pg_catalog.set_config(
                            'tagekyc.raw_export_fixture_kek_write_context',
                            COALESCE(prior_context,''),true);
                        RAISE;
                    END;
                    PERFORM pg_catalog.set_config(
                        'tagekyc.raw_export_fixture_kek_write_context',
                        COALESCE(prior_context,''),true);

                    SELECT * INTO existing
                    FROM tagekyc.raw_export_fixture_kek_wrap_journal j
                    WHERE j."KeyProviderId"=p_key_provider_id
                      AND j."ProviderOperationToken"=p_provider_operation_token;
                    IF existing."AttemptKeyContextFingerprint"
                       IS DISTINCT FROM p_attempt_key_context_fingerprint THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH';
                    END IF;
                    RETURN QUERY SELECT
                        CASE WHEN inserted_count=1 THEN 'Created' ELSE 'ExistingMatch' END,
                        existing."FixtureWrapId",existing."WrappedDekCiphertext",
                        existing."WrappedDekNonce",existing."WrappedDekTag",
                        existing."WrappingSuiteId"::text,existing."WrappingSuiteVersion",
                        existing."ProviderResourceReference"::text,existing."ProviderOperationReceipt"::text,
                        existing."WrappedDekMetadataDigest";
                END $wrap$;

                CREATE FUNCTION tagekyc.raw_export_fixture_kek_lookup(
                    p_key_provider_id text,
                    p_provider_operation_token text,
                    p_attempt_key_context_fingerprint bytea)
                RETURNS TABLE(
                    outcome text,
                    fixture_wrap_id uuid,
                    wrapped_dek_ciphertext bytea,
                    wrapped_dek_nonce bytea,
                    wrapped_dek_tag bytea,
                    wrapping_suite_id text,
                    wrapping_suite_version integer,
                    provider_resource_reference text,
                    provider_operation_receipt text,
                    wrapped_dek_metadata_digest bytea)
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $lookup$
                DECLARE
                    existing tagekyc.raw_export_fixture_kek_wrap_journal%ROWTYPE;
                    expected_digest bytea;
                    expected_resource text;
                    expected_receipt text;
                BEGIN
                    IF p_key_provider_id IS DISTINCT FROM 'fixture-kek-provider-v1'
                       OR p_provider_operation_token IS NULL
                       OR p_provider_operation_token !~ '^[A-Za-z0-9_-]{43}$'
                       OR p_attempt_key_context_fingerprint IS NULL
                       OR pg_catalog.octet_length(p_attempt_key_context_fingerprint)<>32 THEN
                        RAISE EXCEPTION USING ERRCODE='P0001',
                            MESSAGE='RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID';
                    END IF;
                    SELECT * INTO existing
                    FROM tagekyc.raw_export_fixture_kek_wrap_journal j
                    WHERE j."KeyProviderId"=p_key_provider_id
                      AND j."ProviderOperationToken"=p_provider_operation_token;
                    IF NOT FOUND THEN
                        RETURN QUERY SELECT 'Unknown'::text,NULL::uuid,NULL::bytea,
                            NULL::bytea,NULL::bytea,NULL::text,NULL::integer,
                            NULL::text,NULL::text,NULL::bytea;
                        RETURN;
                    END IF;
                    IF existing."AttemptKeyContextFingerprint"
                       IS DISTINCT FROM p_attempt_key_context_fingerprint THEN
                        RETURN QUERY SELECT 'CorruptOrUnverifiable'::text,NULL::uuid,
                            NULL::bytea,NULL::bytea,NULL::bytea,NULL::text,NULL::integer,
                            NULL::text,NULL::text,NULL::bytea;
                        RETURN;
                    END IF;
                    expected_digest:=tagekyc.raw_export_c1_hash_canonical(
                        'tip-88c1-wrapped-dek-metadata-v1',
                        pg_catalog.encode(existing."AttemptKeyContextFingerprint",'hex'),
                        existing."WrappingSuiteId",existing."WrappingSuiteVersion"::text,
                        pg_catalog.encode(existing."WrappedDekNonce",'hex'),
                        pg_catalog.encode(existing."WrappedDekCiphertext",'hex'),
                        pg_catalog.encode(existing."WrappedDekTag",'hex'));
                    expected_resource:='fixture-wrap:' ||
                        pg_catalog.replace(existing."FixtureWrapId"::text,'-','');
                    expected_receipt:='fixture-receipt:' || pg_catalog.encode(
                        tagekyc.raw_export_c1_hash_canonical(
                            'tip-88c1-fixture-kek-receipt-v1',expected_resource,
                            pg_catalog.encode(existing."AttemptKeyContextFingerprint",'hex'),
                            pg_catalog.encode(expected_digest,'hex')),'hex');
                    IF existing."WrappedDekMetadataDigest" IS DISTINCT FROM expected_digest
                       OR existing."ProviderResourceReference" IS DISTINCT FROM expected_resource
                       OR existing."ProviderOperationReceipt" IS DISTINCT FROM expected_receipt THEN
                        RETURN QUERY SELECT 'CorruptOrUnverifiable'::text,NULL::uuid,
                            NULL::bytea,NULL::bytea,NULL::bytea,NULL::text,NULL::integer,
                            NULL::text,NULL::text,NULL::bytea;
                        RETURN;
                    END IF;
                    RETURN QUERY SELECT 'Found'::text,existing."FixtureWrapId",
                        existing."WrappedDekCiphertext",existing."WrappedDekNonce",
                        existing."WrappedDekTag",existing."WrappingSuiteId"::text,
                        existing."WrappingSuiteVersion",existing."ProviderResourceReference"::text,
                        existing."ProviderOperationReceipt"::text,existing."WrappedDekMetadataDigest";
                END $lookup$;

                ALTER FUNCTION tagekyc.enforce_raw_export_fixture_kek_wrap_guard()
                  OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_fixture_kek_wrap(
                  text,text,bytea,bytea,bytea,bytea,text,integer)
                  OWNER TO tagekyc_raw_export_deployer;
                ALTER FUNCTION tagekyc.raw_export_fixture_kek_lookup(text,text,bytea)
                  OWNER TO tagekyc_raw_export_deployer;

                REVOKE ALL ON TABLE tagekyc.raw_export_fixture_kek_wrap_journal
                  FROM PUBLIC,tagekyc_runtime,
                    tagekyc_raw_export_custody_encryptor,
                    tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,
                    tagekyc_raw_export_fixture_kek_wrap_executor,
                    tagekyc_raw_export_fixture_kek_lookup_executor;
                REVOKE ALL ON FUNCTION tagekyc.enforce_raw_export_fixture_kek_wrap_guard()
                  FROM PUBLIC,tagekyc_runtime,
                    tagekyc_raw_export_custody_encryptor,
                    tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,
                    tagekyc_raw_export_fixture_kek_wrap_executor,
                    tagekyc_raw_export_fixture_kek_lookup_executor;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_fixture_kek_wrap(
                  text,text,bytea,bytea,bytea,bytea,text,integer)
                  FROM PUBLIC,tagekyc_runtime,
                    tagekyc_raw_export_custody_encryptor,
                    tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,
                    tagekyc_raw_export_fixture_kek_wrap_executor,
                    tagekyc_raw_export_fixture_kek_lookup_executor;
                REVOKE ALL ON FUNCTION tagekyc.raw_export_fixture_kek_lookup(text,text,bytea)
                  FROM PUBLIC,tagekyc_runtime,
                    tagekyc_raw_export_custody_encryptor,
                    tagekyc_raw_export_reconciler,tagekyc_raw_export_lifecycle,
                    tagekyc_raw_export_fixture_kek_wrap_executor,
                    tagekyc_raw_export_fixture_kek_lookup_executor;
                GRANT USAGE ON SCHEMA tagekyc
                  TO tagekyc_raw_export_fixture_kek_wrap_executor,
                     tagekyc_raw_export_fixture_kek_lookup_executor;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_fixture_kek_wrap(
                  text,text,bytea,bytea,bytea,bytea,text,integer)
                  TO tagekyc_raw_export_fixture_kek_wrap_executor;
                GRANT EXECUTE ON FUNCTION tagekyc.raw_export_fixture_kek_lookup(text,text,bytea)
                  TO tagekyc_raw_export_fixture_kek_lookup_executor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_fixture_kek_wrap(
                  text,text,bytea,bytea,bytea,bytea,text,integer)
                  FROM tagekyc_raw_export_fixture_kek_wrap_executor;
                REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_fixture_kek_lookup(text,text,bytea)
                  FROM tagekyc_raw_export_fixture_kek_lookup_executor;
                REVOKE USAGE ON SCHEMA tagekyc
                  FROM tagekyc_raw_export_fixture_kek_wrap_executor,
                       tagekyc_raw_export_fixture_kek_lookup_executor;
                DROP FUNCTION tagekyc.raw_export_fixture_kek_lookup(text,text,bytea);
                DROP FUNCTION tagekyc.raw_export_fixture_kek_wrap(
                  text,text,bytea,bytea,bytea,bytea,text,integer);
                DROP TRIGGER trg_raw_export_fixture_kek_wrap_guard
                  ON tagekyc.raw_export_fixture_kek_wrap_journal;
                DROP FUNCTION tagekyc.enforce_raw_export_fixture_kek_wrap_guard();
                """);

            migrationBuilder.DropTable(
                name: "raw_export_fixture_kek_wrap_journal",
                schema: "tagekyc");

            migrationBuilder.Sql(
                """
                DROP ROLE IF EXISTS tagekyc_raw_export_fixture_kek_wrap_executor;
                DROP ROLE IF EXISTS tagekyc_raw_export_fixture_kek_lookup_executor;
                """);
        }
    }
}
