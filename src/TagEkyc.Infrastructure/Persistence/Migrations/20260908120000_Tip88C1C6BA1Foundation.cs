using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagEkyc.Infrastructure.Persistence.Migrations;

public partial class Tip88C1C6BA1Foundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.Sql("""
DO $preflight$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_authid AS r
        WHERE r.rolname = 'tagekyc_raw_export_deployer'
          AND NOT r.rolcanlogin AND NOT r.rolsuper AND NOT r.rolcreatedb
          AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls
          AND r.rolinherit AND r.rolpassword IS NULL
          AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members AS m WHERE m.member = r.oid)
    ) THEN
        RAISE EXCEPTION 'TIP88C1C6BA_DEPLOYER_ROLE_INVALID' USING ERRCODE = 'P0001';
    END IF;
END
$preflight$;

DO $capability_roles$
DECLARE
    role_name text;
BEGIN
    FOREACH role_name IN ARRAY ARRAY[
        'tagekyc_capture_runtime_operator',
        'tagekyc_capture_runtime_authenticator',
        'tagekyc_capture_runtime_application'
    ]::text[] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles AS r WHERE r.rolname = role_name) THEN
            EXECUTE pg_catalog.format(
                'CREATE ROLE %I NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT',
                role_name);
        END IF;
        IF NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_authid AS r
            WHERE r.rolname = role_name
              AND NOT r.rolcanlogin AND NOT r.rolsuper AND NOT r.rolcreatedb
              AND NOT r.rolcreaterole AND NOT r.rolreplication AND NOT r.rolbypassrls
              AND r.rolinherit AND r.rolpassword IS NULL
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_auth_members AS m WHERE m.member = r.oid OR m.roleid = r.oid)
        ) THEN
            RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_ROLE_INVALID: %', role_name USING ERRCODE = 'P0001';
        END IF;
    END LOOP;
END
$capability_roles$;

GRANT USAGE ON SCHEMA tagekyc TO tagekyc_capture_runtime_operator,
    tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.c6ba_roles_are_canonical(p_roles text[])
RETURNS boolean
LANGUAGE sql
IMMUTABLE
STRICT
SET search_path = pg_catalog
AS $function$
    SELECT p_roles = ARRAY(
        SELECT DISTINCT role_name
        FROM pg_catalog.unnest(p_roles) AS role_name
        ORDER BY role_name
    )
    AND p_roles <@ ARRAY[
        'Bind',
        'CaptureObservation',
        'Configuration',
        'CredentialRotation',
        'RawIngress',
        'TrustedEvidence'
    ]::text[]
    AND pg_catalog.cardinality(p_roles) > 0;
$function$;

ALTER FUNCTION tagekyc.c6ba_roles_are_canonical(text[])
    OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_roles_are_canonical(text[]) FROM PUBLIC;

CREATE TABLE tagekyc.platform_operator_credentials (
    "CredentialId" uuid NOT NULL,
    "KeyLookupPrefix" varchar(12) COLLATE "C" NOT NULL,
    "SecretDigest" bytea NOT NULL,
    "VerifierPepperVersion" integer NOT NULL,
    "PrincipalId" uuid NOT NULL,
    "Scopes" text[] NOT NULL,
    "State" varchar(16) COLLATE "C" NOT NULL,
    "Revision" bigint NOT NULL,
    "IssuedAtUtc" timestamptz NOT NULL,
    "ExpiresAtUtc" timestamptz NOT NULL,
    "RevokedAtUtc" timestamptz NULL,
    "RevocationReason" varchar(64) COLLATE "C" NULL,
    CONSTRAINT "PK_platform_operator_credentials" PRIMARY KEY ("CredentialId"),
    CONSTRAINT "UQ_platform_operator_credentials_lookup" UNIQUE ("KeyLookupPrefix"),
    CONSTRAINT "CK_platform_operator_credentials_lookup"
        CHECK ("KeyLookupPrefix" ~ '^[A-Za-z0-9_-]{12}$'),
    CONSTRAINT "CK_platform_operator_credentials_digest"
        CHECK (pg_catalog.octet_length("SecretDigest") = 32),
    CONSTRAINT "CK_platform_operator_credentials_pepper"
        CHECK ("VerifierPepperVersion" > 0),
    CONSTRAINT "CK_platform_operator_credentials_principal"
        CHECK ("PrincipalId" <> '00000000-0000-0000-0000-000000000000'::uuid),
    CONSTRAINT "CK_platform_operator_credentials_scopes"
        CHECK ("Scopes" = ARRAY['operator.capture-runtime.manage']::text[]),
    CONSTRAINT "CK_platform_operator_credentials_revision"
        CHECK ("Revision" > 0),
    CONSTRAINT "CK_platform_operator_credentials_interval"
        CHECK ("ExpiresAtUtc" > "IssuedAtUtc"),
    CONSTRAINT "CK_platform_operator_credentials_state"
        CHECK (
            ("State" = 'Active'
                AND "RevokedAtUtc" IS NULL
                AND "RevocationReason" IS NULL)
            OR
            ("State" = 'Revoked'
                AND "RevokedAtUtc" IS NOT NULL
                AND "RevocationReason" IS NOT NULL)
        )
);

CREATE INDEX "IX_platform_operator_credentials_expiry"
    ON tagekyc.platform_operator_credentials ("ExpiresAtUtc", "CredentialId");

CREATE TABLE tagekyc.capture_runtime_role_policy_revisions (
    "CatalogId" uuid NOT NULL,
    "Revision" bigint NOT NULL,
    "Roles" text[] NOT NULL,
    "EffectiveAtUtc" timestamptz NOT NULL,
    "PublishedByCredentialId" uuid NOT NULL,
    "PublishedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_role_policy_revisions"
        PRIMARY KEY ("CatalogId", "Revision"),
    CONSTRAINT "FK_role_policy_publisher"
        FOREIGN KEY ("PublishedByCredentialId")
        REFERENCES tagekyc.platform_operator_credentials ("CredentialId")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_role_policy_revision" CHECK ("Revision" > 0),
    CONSTRAINT "CK_role_policy_roles"
        CHECK (tagekyc.c6ba_roles_are_canonical("Roles"))
);

CREATE TABLE tagekyc.capture_runtime_role_policy_heads (
    "CatalogId" uuid NOT NULL,
    "CurrentRevision" bigint NOT NULL,
    "HeadRevision" bigint NOT NULL,
    "UpdatedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_role_policy_heads" PRIMARY KEY ("CatalogId"),
    CONSTRAINT "FK_role_policy_head_revision"
        FOREIGN KEY ("CatalogId", "CurrentRevision")
        REFERENCES tagekyc.capture_runtime_role_policy_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_role_policy_head"
        CHECK ("CurrentRevision" = "HeadRevision" AND "HeadRevision" > 0)
);

CREATE TABLE tagekyc.capture_runtime_trust_profile_revisions (
    "CatalogId" uuid NOT NULL,
    "Revision" bigint NOT NULL,
    "RuntimeType" varchar(16) COLLATE "C" NOT NULL,
    "RetainedRawEnabled" boolean NOT NULL,
    "AllowTrustedEvidence" boolean NOT NULL,
    "RequireHandoffAttestation" boolean NOT NULL,
    "EffectiveAtUtc" timestamptz NOT NULL,
    "ExpiresAtUtc" timestamptz NOT NULL,
    "PublishedByCredentialId" uuid NOT NULL,
    "PublishedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_trust_profile_revisions"
        PRIMARY KEY ("CatalogId", "Revision"),
    CONSTRAINT "FK_trust_profile_publisher"
        FOREIGN KEY ("PublishedByCredentialId")
        REFERENCES tagekyc.platform_operator_credentials ("CredentialId")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_trust_profile_revision" CHECK ("Revision" > 0),
    CONSTRAINT "CK_trust_profile_runtime" CHECK ("RuntimeType" = 'Managed'),
    CONSTRAINT "CK_trust_profile_interval"
        CHECK ("ExpiresAtUtc" > "EffectiveAtUtc")
);

CREATE TABLE tagekyc.capture_runtime_trust_profile_heads (
    "CatalogId" uuid NOT NULL,
    "CurrentRevision" bigint NOT NULL,
    "HeadRevision" bigint NOT NULL,
    "UpdatedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_trust_profile_heads" PRIMARY KEY ("CatalogId"),
    CONSTRAINT "FK_trust_profile_head_revision"
        FOREIGN KEY ("CatalogId", "CurrentRevision")
        REFERENCES tagekyc.capture_runtime_trust_profile_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_trust_profile_head"
        CHECK ("CurrentRevision" = "HeadRevision" AND "HeadRevision" > 0)
);

CREATE TABLE tagekyc.capture_runtime_configuration_revisions (
    "CatalogId" uuid NOT NULL,
    "Revision" bigint NOT NULL,
    "EffectiveAtUtc" timestamptz NOT NULL,
    "ExpiresAtUtc" timestamptz NOT NULL,
    "RawExportEnabled" boolean NOT NULL,
    "PlaintextBudgetSeconds" integer NOT NULL,
    "RawExportSourceClaimSafetyMarginMilliseconds" integer NOT NULL,
    "CaptureAgentConfigurationPollingIntervalSeconds" integer NOT NULL,
    "RawExportSourceMaximumChipDg2PortraitBytes" integer NOT NULL,
    "RawExportSourceMaximumLiveSelfieImageBytes" integer NOT NULL,
    "RawExportCaptureMaximumAggregatePlaintextBytesPerHost" bigint NOT NULL,
    "RawExportCustodyMaximumPlaintextWindowBytesPerStream" integer NOT NULL,
    "RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment" bigint NOT NULL,
    "RawExportIngressMaximumPreAdmissionBufferedBytes" integer NOT NULL,
    "PublishedByCredentialId" uuid NOT NULL,
    "PublishedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_configuration_revisions"
        PRIMARY KEY ("CatalogId", "Revision"),
    CONSTRAINT "FK_configuration_publisher"
        FOREIGN KEY ("PublishedByCredentialId")
        REFERENCES tagekyc.platform_operator_credentials ("CredentialId")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_configuration_revision" CHECK ("Revision" > 0),
    CONSTRAINT "CK_configuration_interval"
        CHECK ("ExpiresAtUtc" > "EffectiveAtUtc"),
    CONSTRAINT "CK_configuration_budget" CHECK ("PlaintextBudgetSeconds" > 0),
    CONSTRAINT "CK_configuration_margin"
        CHECK ("RawExportSourceClaimSafetyMarginMilliseconds" >= 0),
    CONSTRAINT "CK_configuration_poll"
        CHECK ("CaptureAgentConfigurationPollingIntervalSeconds" > 0),
    CONSTRAINT "CK_configuration_dg2"
        CHECK ("RawExportSourceMaximumChipDg2PortraitBytes" BETWEEN 1 AND 67108864),
    CONSTRAINT "CK_configuration_selfie"
        CHECK ("RawExportSourceMaximumLiveSelfieImageBytes" BETWEEN 1 AND 67108864),
    CONSTRAINT "CK_configuration_capture_host"
        CHECK ("RawExportCaptureMaximumAggregatePlaintextBytesPerHost" BETWEEN 1 AND 2147483647),
    CONSTRAINT "CK_configuration_custody_stream"
        CHECK ("RawExportCustodyMaximumPlaintextWindowBytesPerStream" BETWEEN 1 AND 16777216),
    CONSTRAINT "CK_configuration_custody_deployment"
        CHECK ("RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment" BETWEEN 1 AND 2147483647),
    CONSTRAINT "CK_configuration_pre_admission"
        CHECK ("RawExportIngressMaximumPreAdmissionBufferedBytes" BETWEEN 1 AND 65536),
    CONSTRAINT "CK_configuration_freshness"
        CHECK (
            "EffectiveAtUtc"
            + pg_catalog.make_interval(secs => "CaptureAgentConfigurationPollingIntervalSeconds"::double precision)
            + pg_catalog.make_interval(secs => "RawExportSourceClaimSafetyMarginMilliseconds"::double precision / 1000.0)
            < "ExpiresAtUtc"
        )
);

CREATE TABLE tagekyc.capture_runtime_configuration_heads (
    "CatalogId" uuid NOT NULL,
    "CurrentRevision" bigint NOT NULL,
    "HeadRevision" bigint NOT NULL,
    "UpdatedAtUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_configuration_heads" PRIMARY KEY ("CatalogId"),
    CONSTRAINT "FK_configuration_head_revision"
        FOREIGN KEY ("CatalogId", "CurrentRevision")
        REFERENCES tagekyc.capture_runtime_configuration_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_configuration_head"
        CHECK ("CurrentRevision" = "HeadRevision" AND "HeadRevision" > 0)
);
""");
            migrationBuilder.Sql("""
CREATE TABLE tagekyc.capture_runtime_registrations (
    "CaptureAgentId" uuid NOT NULL,
    "RuntimeType" varchar(16) COLLATE "C" NOT NULL,
    "TrustProfileId" uuid NOT NULL,
    "TrustProfileRevision" bigint NOT NULL,
    "ConfigurationId" uuid NOT NULL,
    "ConfigurationRevision" bigint NOT NULL,
    "ConfigurationOverrideId" uuid NULL,
    "NextRolePolicyId" uuid NULL,
    "NextRolePolicyRevision" bigint NULL,
    "LifecycleState" varchar(16) COLLATE "C" NOT NULL,
    "Revision" bigint NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL,
    "SuspendedAtUtc" timestamptz NULL,
    "RevokedAtUtc" timestamptz NULL,
    "RetiredAtUtc" timestamptz NULL,
    "LifecycleReason" varchar(64) COLLATE "C" NULL,
    CONSTRAINT "PK_capture_runtime_registrations" PRIMARY KEY ("CaptureAgentId"),
    CONSTRAINT "FK_registration_trust"
        FOREIGN KEY ("TrustProfileId", "TrustProfileRevision")
        REFERENCES tagekyc.capture_runtime_trust_profile_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "FK_registration_configuration"
        FOREIGN KEY ("ConfigurationId", "ConfigurationRevision")
        REFERENCES tagekyc.capture_runtime_configuration_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "FK_registration_next_role"
        FOREIGN KEY ("NextRolePolicyId", "NextRolePolicyRevision")
        REFERENCES tagekyc.capture_runtime_role_policy_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_registration_runtime" CHECK ("RuntimeType" = 'Managed'),
    CONSTRAINT "CK_registration_revision" CHECK ("Revision" > 0),
    CONSTRAINT "CK_registration_next_role_pair"
        CHECK (("NextRolePolicyId" IS NULL) = ("NextRolePolicyRevision" IS NULL)),
    CONSTRAINT "CK_registration_state"
        CHECK (
            ("LifecycleState" = 'Active' AND "SuspendedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RetiredAtUtc" IS NULL)
            OR ("LifecycleState" = 'Suspended' AND "SuspendedAtUtc" IS NOT NULL AND "RevokedAtUtc" IS NULL AND "RetiredAtUtc" IS NULL AND "LifecycleReason" IS NOT NULL)
            OR ("LifecycleState" = 'Revoked' AND "RevokedAtUtc" IS NOT NULL AND "RetiredAtUtc" IS NULL AND "LifecycleReason" IS NOT NULL)
            OR ("LifecycleState" = 'Retired' AND "RetiredAtUtc" IS NOT NULL AND "LifecycleReason" IS NOT NULL)
        )
);

CREATE TABLE tagekyc.capture_runtime_installations (
    "DeviceInstallationId" uuid NOT NULL,
    "CaptureAgentId" uuid NOT NULL,
    "CurrentCredentialId" uuid NULL,
    "CurrentCredentialGeneration" bigint NULL,
    "LifecycleState" varchar(16) COLLATE "C" NOT NULL,
    "Revision" bigint NOT NULL,
    "EnrolledAtUtc" timestamptz NOT NULL,
    "SuspendedAtUtc" timestamptz NULL,
    "RevokedAtUtc" timestamptz NULL,
    "RetiredAtUtc" timestamptz NULL,
    "LifecycleReason" varchar(64) COLLATE "C" NULL,
    CONSTRAINT "PK_capture_runtime_installations" PRIMARY KEY ("DeviceInstallationId"),
    CONSTRAINT "FK_installation_registration"
        FOREIGN KEY ("CaptureAgentId")
        REFERENCES tagekyc.capture_runtime_registrations ("CaptureAgentId")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_installation_revision" CHECK ("Revision" > 0),
    CONSTRAINT "CK_installation_state_name"
        CHECK ("LifecycleState" IN ('Pending', 'Active', 'Suspended', 'Revoked', 'Retired')),
    CONSTRAINT "CK_installation_current_pair"
        CHECK (
            ("LifecycleState" = 'Pending' AND "CurrentCredentialId" IS NULL AND "CurrentCredentialGeneration" IS NULL)
            OR ("LifecycleState" <> 'Pending' AND "CurrentCredentialId" IS NOT NULL AND "CurrentCredentialGeneration" IS NOT NULL)
        )
);

CREATE UNIQUE INDEX "UX_installation_one_active_managed"
    ON tagekyc.capture_runtime_installations ("CaptureAgentId")
    WHERE "LifecycleState" = 'Active';

CREATE TABLE tagekyc.capture_runtime_credential_generations (
    "CredentialId" uuid NOT NULL,
    "Generation" bigint NOT NULL,
    "DeviceInstallationId" uuid NOT NULL,
    "CandidateKeyId" uuid NOT NULL,
    "PublicVerifierSpki" bytea NOT NULL,
    "Algorithm" varchar(64) COLLATE "C" NOT NULL,
    "PublicKeyThumbprint" bytea NOT NULL,
    "RolePolicyId" uuid NOT NULL,
    "RolePolicyRevision" bigint NOT NULL,
    "ValidFromUtc" timestamptz NOT NULL,
    "ValidUntilUtc" timestamptz NOT NULL,
    "State" varchar(16) COLLATE "C" NOT NULL,
    "Revision" bigint NOT NULL,
    "RotatedAtUtc" timestamptz NULL,
    "RevokedAtUtc" timestamptz NULL,
    "RetiredAtUtc" timestamptz NULL,
    "TerminalReason" varchar(64) COLLATE "C" NULL,
    CONSTRAINT "PK_capture_runtime_credential_generations"
        PRIMARY KEY ("CredentialId", "Generation"),
    CONSTRAINT "AK_generation_installation_lineage"
        UNIQUE ("DeviceInstallationId", "CredentialId", "Generation"),
    CONSTRAINT "UQ_generation_candidate_key" UNIQUE ("CandidateKeyId"),
    CONSTRAINT "UQ_generation_installation_thumbprint"
        UNIQUE ("DeviceInstallationId", "PublicKeyThumbprint"),
    CONSTRAINT "FK_generation_installation"
        FOREIGN KEY ("DeviceInstallationId")
        REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId")
        ON DELETE RESTRICT,
    CONSTRAINT "FK_generation_role_policy"
        FOREIGN KEY ("RolePolicyId", "RolePolicyRevision")
        REFERENCES tagekyc.capture_runtime_role_policy_revisions ("CatalogId", "Revision")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_generation_number" CHECK ("Generation" > 0),
    CONSTRAINT "CK_generation_spki"
        CHECK (pg_catalog.octet_length("PublicVerifierSpki") = 91),
    CONSTRAINT "CK_generation_algorithm"
        CHECK ("Algorithm" = 'TAG-EKYC-CRT1-ECDSA-P256-SHA256'),
    CONSTRAINT "CK_generation_thumbprint"
        CHECK (pg_catalog.octet_length("PublicKeyThumbprint") = 32),
    CONSTRAINT "CK_generation_revision" CHECK ("Revision" > 0),
    CONSTRAINT "CK_generation_interval" CHECK ("ValidUntilUtc" > "ValidFromUtc"),
    CONSTRAINT "CK_generation_state"
        CHECK (
            ("State" IN ('Pending', 'Active') AND "RotatedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "RetiredAtUtc" IS NULL)
            OR ("State" = 'Rotated' AND "RotatedAtUtc" IS NOT NULL AND "RevokedAtUtc" IS NULL AND "RetiredAtUtc" IS NULL AND "TerminalReason" IS NOT NULL)
            OR ("State" = 'Revoked' AND "RevokedAtUtc" IS NOT NULL AND "RetiredAtUtc" IS NULL AND "TerminalReason" IS NOT NULL)
            OR ("State" = 'Retired' AND "RetiredAtUtc" IS NOT NULL AND "TerminalReason" IS NOT NULL)
        )
);

CREATE UNIQUE INDEX "UX_generation_one_active_installation"
    ON tagekyc.capture_runtime_credential_generations ("DeviceInstallationId")
    WHERE "State" = 'Active';

ALTER TABLE tagekyc.capture_runtime_installations
    ADD CONSTRAINT "FK_installation_current_generation"
    FOREIGN KEY ("DeviceInstallationId", "CurrentCredentialId", "CurrentCredentialGeneration")
    REFERENCES tagekyc.capture_runtime_credential_generations
        ("DeviceInstallationId", "CredentialId", "Generation")
    ON DELETE RESTRICT
    DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE tagekyc.capture_runtime_request_nonces (
    "CredentialId" uuid NOT NULL,
    "CredentialGeneration" bigint NOT NULL,
    "Nonce" bytea NOT NULL,
    "SignedTimestampUtc" timestamptz NOT NULL,
    "AdmittedAtUtc" timestamptz NOT NULL,
    "PurgeAfterUtc" timestamptz NOT NULL,
    CONSTRAINT "PK_capture_runtime_request_nonces"
        PRIMARY KEY ("CredentialId", "CredentialGeneration", "Nonce"),
    CONSTRAINT "FK_runtime_nonce_generation"
        FOREIGN KEY ("CredentialId", "CredentialGeneration")
        REFERENCES tagekyc.capture_runtime_credential_generations ("CredentialId", "Generation")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_runtime_nonce_length"
        CHECK (pg_catalog.octet_length("Nonce") = 32),
    CONSTRAINT "CK_runtime_nonce_purge"
        CHECK ("PurgeAfterUtc" = "SignedTimestampUtc" + interval '150 seconds')
);

CREATE INDEX "IX_runtime_nonce_cleanup"
    ON tagekyc.capture_runtime_request_nonces
        ("PurgeAfterUtc", "CredentialId", "CredentialGeneration", "Nonce");
""");
            migrationBuilder.Sql("""
ALTER TABLE tagekyc.verification_sessions
 ADD CONSTRAINT "AK_verification_sessions_id_client"
 UNIQUE ("Id", "ClientApplicationId");

CREATE TABLE tagekyc.capture_runtime_bootstrap_issuances (
 "BootstrapIssuanceId" uuid NOT NULL, "KeyLookupPrefix" varchar(12) COLLATE "C" NOT NULL,
 "SecretDigest" bytea NOT NULL, "VerifierPepperVersion" integer NOT NULL,
 "RuntimeType" varchar(16) COLLATE "C" NOT NULL,
 "TrustProfileId" uuid NOT NULL, "TrustProfileRevision" bigint NOT NULL,
 "RolePolicyId" uuid NOT NULL, "RolePolicyRevision" bigint NOT NULL,
 "ConfigurationId" uuid NOT NULL, "ConfigurationRevision" bigint NOT NULL,
 "AttestationRequirementDigest" bytea NOT NULL, "RequestFingerprint" bytea NOT NULL,
 "IssueOperationId" uuid NOT NULL, "IssuedByCredentialId" uuid NOT NULL,
 "IssuedAtUtc" timestamptz NOT NULL, "ExpiresAtUtc" timestamptz NOT NULL,
 "State" varchar(16) COLLATE "C" NOT NULL, "Revision" bigint NOT NULL,
 "RedeemedAtUtc" timestamptz NULL, "CaptureAgentId" uuid NULL,
 "DeviceInstallationId" uuid NULL, "CredentialId" uuid NULL, "CredentialGeneration" bigint NULL,
 "RevokedAtUtc" timestamptz NULL, "ExpiredAtUtc" timestamptz NULL,
 "TerminalReason" varchar(64) COLLATE "C" NULL,
 CONSTRAINT "PK_capture_runtime_bootstrap_issuances" PRIMARY KEY ("BootstrapIssuanceId"),
 CONSTRAINT "UQ_bootstrap_lookup" UNIQUE ("KeyLookupPrefix"),
 CONSTRAINT "UQ_bootstrap_issue_operation" UNIQUE ("IssueOperationId"),
 CONSTRAINT "FK_bootstrap_operator" FOREIGN KEY ("IssuedByCredentialId") REFERENCES tagekyc.platform_operator_credentials ("CredentialId") ON DELETE RESTRICT,
 CONSTRAINT "FK_bootstrap_trust" FOREIGN KEY ("TrustProfileId","TrustProfileRevision") REFERENCES tagekyc.capture_runtime_trust_profile_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "FK_bootstrap_role" FOREIGN KEY ("RolePolicyId","RolePolicyRevision") REFERENCES tagekyc.capture_runtime_role_policy_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "FK_bootstrap_config" FOREIGN KEY ("ConfigurationId","ConfigurationRevision") REFERENCES tagekyc.capture_runtime_configuration_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "FK_bootstrap_result_registration" FOREIGN KEY ("CaptureAgentId") REFERENCES tagekyc.capture_runtime_registrations ("CaptureAgentId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "FK_bootstrap_result_installation" FOREIGN KEY ("DeviceInstallationId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "FK_bootstrap_result_generation" FOREIGN KEY ("DeviceInstallationId","CredentialId","CredentialGeneration") REFERENCES tagekyc.capture_runtime_credential_generations ("DeviceInstallationId","CredentialId","Generation") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "CK_bootstrap_lookup" CHECK ("KeyLookupPrefix" ~ '^[A-Za-z0-9_-]{12}$'),
 CONSTRAINT "CK_bootstrap_digest" CHECK (pg_catalog.octet_length("SecretDigest")=32 AND "VerifierPepperVersion">0),
 CONSTRAINT "CK_bootstrap_hashes" CHECK (pg_catalog.octet_length("AttestationRequirementDigest")=32 AND pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_bootstrap_runtime" CHECK ("RuntimeType"='Managed'),
 CONSTRAINT "CK_bootstrap_interval" CHECK ("ExpiresAtUtc">"IssuedAtUtc"),
 CONSTRAINT "CK_bootstrap_revision" CHECK ("Revision">0),
 CONSTRAINT "CK_bootstrap_state" CHECK (
  ("State"='Active' AND "RedeemedAtUtc" IS NULL AND "CaptureAgentId" IS NULL AND "DeviceInstallationId" IS NULL AND "CredentialId" IS NULL AND "CredentialGeneration" IS NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NULL)
  OR ("State"='Redeemed' AND "RedeemedAtUtc" IS NOT NULL AND "CaptureAgentId" IS NOT NULL AND "DeviceInstallationId" IS NOT NULL AND "CredentialId" IS NOT NULL AND "CredentialGeneration" IS NOT NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NULL)
  OR ("State"='Revoked' AND "RedeemedAtUtc" IS NULL AND "CaptureAgentId" IS NULL AND "DeviceInstallationId" IS NULL AND "CredentialId" IS NULL AND "CredentialGeneration" IS NULL AND "RevokedAtUtc" IS NOT NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NOT NULL)
  OR ("State"='Expired' AND "RedeemedAtUtc" IS NULL AND "CaptureAgentId" IS NULL AND "DeviceInstallationId" IS NULL AND "CredentialId" IS NULL AND "CredentialGeneration" IS NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NOT NULL AND "TerminalReason" IS NOT NULL)));
CREATE INDEX "IX_bootstrap_expiry" ON tagekyc.capture_runtime_bootstrap_issuances ("ExpiresAtUtc","BootstrapIssuanceId") WHERE "State"='Active';

CREATE TABLE tagekyc.capture_capabilities (
 "CaptureCapabilityId" uuid NOT NULL, "VerificationSessionId" uuid NOT NULL,
 "ClientApplicationId" uuid NOT NULL, "KeyLookupPrefix" varchar(12) COLLATE "C" NOT NULL,
 "SecretDigest" bytea NOT NULL, "VerifierPepperVersion" integer NOT NULL,
 "Audience" varchar(32) COLLATE "C" NOT NULL, "Challenge" varchar(128) COLLATE "C" NOT NULL,
 "IssuedAtUtc" timestamptz NOT NULL, "ExpiresAtUtc" timestamptz NOT NULL,
 "State" varchar(16) COLLATE "C" NOT NULL, "Revision" bigint NOT NULL,
 "PredecessorCapabilityId" uuid NULL, "SuccessorCapabilityId" uuid NULL,
 "BoundAtUtc" timestamptz NULL, "RevokedAtUtc" timestamptz NULL,
 "ExpiredAtUtc" timestamptz NULL, "TerminalReason" varchar(64) COLLATE "C" NULL,
 CONSTRAINT "PK_capture_capabilities" PRIMARY KEY ("CaptureCapabilityId"),
 CONSTRAINT "UQ_capability_lookup" UNIQUE ("KeyLookupPrefix"),
 CONSTRAINT "UQ_capability_predecessor" UNIQUE ("PredecessorCapabilityId"),
 CONSTRAINT "UQ_capability_successor" UNIQUE ("SuccessorCapabilityId"),
 CONSTRAINT "FK_capability_session_client" FOREIGN KEY ("VerificationSessionId","ClientApplicationId") REFERENCES tagekyc.verification_sessions ("Id","ClientApplicationId") ON DELETE RESTRICT,
 CONSTRAINT "FK_capability_predecessor" FOREIGN KEY ("PredecessorCapabilityId") REFERENCES tagekyc.capture_capabilities ("CaptureCapabilityId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "FK_capability_successor" FOREIGN KEY ("SuccessorCapabilityId") REFERENCES tagekyc.capture_capabilities ("CaptureCapabilityId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "CK_capability_lookup" CHECK ("KeyLookupPrefix" ~ '^[A-Za-z0-9_-]{12}$'),
 CONSTRAINT "CK_capability_digest" CHECK (pg_catalog.octet_length("SecretDigest")=32 AND "VerifierPepperVersion">0),
 CONSTRAINT "CK_capability_audience" CHECK ("Audience"='ManagedCaptureRuntime'),
 CONSTRAINT "CK_capability_interval" CHECK ("ExpiresAtUtc">"IssuedAtUtc"),
 CONSTRAINT "CK_capability_revision" CHECK ("Revision">0),
 CONSTRAINT "CK_capability_no_self_link" CHECK ("PredecessorCapabilityId" IS DISTINCT FROM "CaptureCapabilityId" AND "SuccessorCapabilityId" IS DISTINCT FROM "CaptureCapabilityId"),
 CONSTRAINT "CK_capability_state" CHECK (
  ("State"='ActiveUnbound' AND "BoundAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NULL)
  OR ("State"='Bound' AND "BoundAtUtc" IS NOT NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NULL)
  OR ("State"='Revoked' AND "RevokedAtUtc" IS NOT NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NOT NULL)
  OR ("State"='Expired' AND "ExpiredAtUtc" IS NOT NULL AND "RevokedAtUtc" IS NULL AND "TerminalReason" IS NOT NULL)));
CREATE UNIQUE INDEX "UX_capability_one_live_session" ON tagekyc.capture_capabilities ("VerificationSessionId") WHERE "State" IN ('ActiveUnbound','Bound');
CREATE INDEX "IX_capability_expiry" ON tagekyc.capture_capabilities ("ExpiresAtUtc","CaptureCapabilityId") WHERE "State"='ActiveUnbound';

CREATE TABLE tagekyc.capture_execution_bindings (
 "CaptureExecutionBindingId" uuid NOT NULL, "VerificationSessionId" uuid NOT NULL,
 "CaptureCapabilityId" uuid NOT NULL, "ClientApplicationId" uuid NOT NULL,
 "CaptureAgentId" uuid NOT NULL, "DeviceInstallationId" uuid NOT NULL,
 "CredentialId" uuid NOT NULL, "CredentialGeneration" bigint NOT NULL,
 "PublicKeyThumbprint" bytea NOT NULL, "RuntimeRevision" bigint NOT NULL,
 "InstallationRevision" bigint NOT NULL, "CredentialRevision" bigint NOT NULL,
 "TrustProfileId" uuid NOT NULL, "TrustProfileRevision" bigint NOT NULL,
 "RolePolicyId" uuid NOT NULL, "RolePolicyRevision" bigint NOT NULL,
 "ConfigurationId" uuid NOT NULL, "ConfigurationRevision" bigint NOT NULL,
 "Challenge" varchar(128) COLLATE "C" NOT NULL, "BindOperationId" uuid NOT NULL,
 "BoundAtUtc" timestamptz NOT NULL, "ExecutionExpiresAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_capture_execution_bindings" PRIMARY KEY ("CaptureExecutionBindingId"),
 CONSTRAINT "UQ_binding_session" UNIQUE ("VerificationSessionId"),
 CONSTRAINT "UQ_binding_capability" UNIQUE ("CaptureCapabilityId"),
 CONSTRAINT "UQ_binding_operation" UNIQUE ("BindOperationId"),
 CONSTRAINT "FK_binding_session_client" FOREIGN KEY ("VerificationSessionId","ClientApplicationId") REFERENCES tagekyc.verification_sessions ("Id","ClientApplicationId") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_capability" FOREIGN KEY ("CaptureCapabilityId") REFERENCES tagekyc.capture_capabilities ("CaptureCapabilityId") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_registration" FOREIGN KEY ("CaptureAgentId") REFERENCES tagekyc.capture_runtime_registrations ("CaptureAgentId") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_installation" FOREIGN KEY ("DeviceInstallationId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_generation" FOREIGN KEY ("DeviceInstallationId","CredentialId","CredentialGeneration") REFERENCES tagekyc.capture_runtime_credential_generations ("DeviceInstallationId","CredentialId","Generation") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_trust" FOREIGN KEY ("TrustProfileId","TrustProfileRevision") REFERENCES tagekyc.capture_runtime_trust_profile_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_role" FOREIGN KEY ("RolePolicyId","RolePolicyRevision") REFERENCES tagekyc.capture_runtime_role_policy_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "FK_binding_config" FOREIGN KEY ("ConfigurationId","ConfigurationRevision") REFERENCES tagekyc.capture_runtime_configuration_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "CK_binding_thumbprint" CHECK (pg_catalog.octet_length("PublicKeyThumbprint")=32),
 CONSTRAINT "CK_binding_revisions" CHECK ("RuntimeRevision">0 AND "InstallationRevision">0 AND "CredentialRevision">0),
 CONSTRAINT "CK_binding_interval" CHECK ("ExecutionExpiresAtUtc">"BoundAtUtc"));

ALTER TABLE tagekyc.capture_capabilities ADD CONSTRAINT "AK_capability_binding_lineage" UNIQUE ("CaptureCapabilityId","VerificationSessionId","ClientApplicationId");
ALTER TABLE tagekyc.capture_runtime_installations ADD CONSTRAINT "AK_installation_agent_lineage" UNIQUE ("DeviceInstallationId","CaptureAgentId");
ALTER TABLE tagekyc.capture_execution_bindings DROP CONSTRAINT "FK_binding_capability";
ALTER TABLE tagekyc.capture_execution_bindings ADD CONSTRAINT "FK_binding_capability_lineage" FOREIGN KEY ("CaptureCapabilityId","VerificationSessionId","ClientApplicationId") REFERENCES tagekyc.capture_capabilities ("CaptureCapabilityId","VerificationSessionId","ClientApplicationId") ON DELETE RESTRICT;
ALTER TABLE tagekyc.capture_execution_bindings DROP CONSTRAINT "FK_binding_installation";
ALTER TABLE tagekyc.capture_execution_bindings ADD CONSTRAINT "FK_binding_installation_agent" FOREIGN KEY ("DeviceInstallationId","CaptureAgentId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId","CaptureAgentId") ON DELETE RESTRICT;

CREATE TABLE tagekyc.capture_runtime_configuration_overrides (
 "ConfigurationOverrideId" uuid NOT NULL, "CaptureAgentId" uuid NOT NULL,
 "BaseConfigurationId" uuid NOT NULL, "BaseConfigurationRevision" bigint NOT NULL,
 "RawExportEnabled" boolean NULL, "PlaintextBudgetSeconds" integer NULL,
 "RawExportSourceClaimSafetyMarginMilliseconds" integer NULL,
 "CaptureAgentConfigurationPollingIntervalSeconds" integer NULL,
 "RawExportSourceMaximumChipDg2PortraitBytes" integer NULL,
 "RawExportSourceMaximumLiveSelfieImageBytes" integer NULL,
 "RawExportCaptureMaximumAggregatePlaintextBytesPerHost" bigint NULL,
 "RawExportCustodyMaximumPlaintextWindowBytesPerStream" integer NULL,
 "RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment" bigint NULL,
 "RawExportIngressMaximumPreAdmissionBufferedBytes" integer NULL,
 "Revision" bigint NOT NULL, "UpdatedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_capture_runtime_configuration_overrides" PRIMARY KEY ("ConfigurationOverrideId"),
 CONSTRAINT "UQ_override_agent" UNIQUE ("CaptureAgentId"),
 CONSTRAINT "FK_override_registration" FOREIGN KEY ("CaptureAgentId") REFERENCES tagekyc.capture_runtime_registrations ("CaptureAgentId") ON DELETE RESTRICT,
 CONSTRAINT "FK_override_base" FOREIGN KEY ("BaseConfigurationId","BaseConfigurationRevision") REFERENCES tagekyc.capture_runtime_configuration_revisions ("CatalogId","Revision") ON DELETE RESTRICT,
 CONSTRAINT "CK_override_enabled" CHECK ("RawExportEnabled" IS NULL OR "RawExportEnabled"=false),
 CONSTRAINT "CK_override_revision" CHECK ("Revision">0),
 CONSTRAINT "CK_override_ranges" CHECK (("PlaintextBudgetSeconds" IS NULL OR "PlaintextBudgetSeconds">0) AND ("RawExportSourceClaimSafetyMarginMilliseconds" IS NULL OR "RawExportSourceClaimSafetyMarginMilliseconds">=0) AND ("CaptureAgentConfigurationPollingIntervalSeconds" IS NULL OR "CaptureAgentConfigurationPollingIntervalSeconds">0) AND ("RawExportSourceMaximumChipDg2PortraitBytes" IS NULL OR "RawExportSourceMaximumChipDg2PortraitBytes" BETWEEN 1 AND 67108864) AND ("RawExportSourceMaximumLiveSelfieImageBytes" IS NULL OR "RawExportSourceMaximumLiveSelfieImageBytes" BETWEEN 1 AND 67108864) AND ("RawExportCaptureMaximumAggregatePlaintextBytesPerHost" IS NULL OR "RawExportCaptureMaximumAggregatePlaintextBytesPerHost" BETWEEN 1 AND 2147483647) AND ("RawExportCustodyMaximumPlaintextWindowBytesPerStream" IS NULL OR "RawExportCustodyMaximumPlaintextWindowBytesPerStream" BETWEEN 1 AND 16777216) AND ("RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment" IS NULL OR "RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment" BETWEEN 1 AND 2147483647) AND ("RawExportIngressMaximumPreAdmissionBufferedBytes" IS NULL OR "RawExportIngressMaximumPreAdmissionBufferedBytes" BETWEEN 1 AND 65536)));
ALTER TABLE tagekyc.capture_runtime_registrations ADD CONSTRAINT "FK_registration_override" FOREIGN KEY ("ConfigurationOverrideId") REFERENCES tagekyc.capture_runtime_configuration_overrides ("ConfigurationOverrideId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE tagekyc.capture_runtime_rotation_authorizations (
 "RotationAuthorizationId" uuid NOT NULL, "CaptureAgentId" uuid NOT NULL,
 "DeviceInstallationId" uuid NOT NULL, "CredentialId" uuid NOT NULL,
 "CurrentGeneration" bigint NOT NULL, "AuthorizeOperationId" uuid NOT NULL,
 "RequestFingerprint" bytea NOT NULL, "AuthorizedByCredentialId" uuid NOT NULL,
 "AuthorizedAtUtc" timestamptz NOT NULL, "ExpiresAtUtc" timestamptz NOT NULL,
 "State" varchar(16) COLLATE "C" NOT NULL, "Revision" bigint NOT NULL,
 "CandidateKeyId" uuid NULL, "SuccessorPublicVerifierSpki" bytea NULL,
 "SuccessorPublicKeyThumbprint" bytea NULL, "SuccessorGeneration" bigint NULL,
 "CompletedAtUtc" timestamptz NULL, "RevokedAtUtc" timestamptz NULL,
 "ExpiredAtUtc" timestamptz NULL, "TerminalReason" varchar(64) COLLATE "C" NULL,
 CONSTRAINT "PK_capture_runtime_rotation_authorizations" PRIMARY KEY ("RotationAuthorizationId"),
 CONSTRAINT "UQ_rotation_operation" UNIQUE ("AuthorizeOperationId"),
 CONSTRAINT "UQ_rotation_candidate" UNIQUE ("CandidateKeyId"),
 CONSTRAINT "FK_rotation_registration" FOREIGN KEY ("CaptureAgentId") REFERENCES tagekyc.capture_runtime_registrations ("CaptureAgentId") ON DELETE RESTRICT,
 CONSTRAINT "FK_rotation_generation" FOREIGN KEY ("DeviceInstallationId","CredentialId","CurrentGeneration") REFERENCES tagekyc.capture_runtime_credential_generations ("DeviceInstallationId","CredentialId","Generation") ON DELETE RESTRICT,
 CONSTRAINT "FK_rotation_operator" FOREIGN KEY ("AuthorizedByCredentialId") REFERENCES tagekyc.platform_operator_credentials ("CredentialId") ON DELETE RESTRICT,
 CONSTRAINT "CK_rotation_fingerprint" CHECK (pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_rotation_interval" CHECK ("ExpiresAtUtc">"AuthorizedAtUtc" AND "ExpiresAtUtc"<="AuthorizedAtUtc"+interval '10 minutes'),
 CONSTRAINT "CK_rotation_revision" CHECK ("Revision">0),
 CONSTRAINT "CK_rotation_state" CHECK (
  ("State"='Active' AND "CandidateKeyId" IS NULL AND "SuccessorGeneration" IS NULL AND "CompletedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NULL)
  OR ("State"='Completed' AND "CandidateKeyId" IS NOT NULL AND pg_catalog.octet_length("SuccessorPublicVerifierSpki")=91 AND pg_catalog.octet_length("SuccessorPublicKeyThumbprint")=32 AND "SuccessorGeneration"="CurrentGeneration"+1 AND "CompletedAtUtc" IS NOT NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NULL)
  OR ("State"='Revoked' AND "CandidateKeyId" IS NULL AND "SuccessorPublicVerifierSpki" IS NULL AND "SuccessorPublicKeyThumbprint" IS NULL AND "SuccessorGeneration" IS NULL AND "CompletedAtUtc" IS NULL AND "RevokedAtUtc" IS NOT NULL AND "ExpiredAtUtc" IS NULL AND "TerminalReason" IS NOT NULL)
  OR ("State"='Expired' AND "CandidateKeyId" IS NULL AND "SuccessorPublicVerifierSpki" IS NULL AND "SuccessorPublicKeyThumbprint" IS NULL AND "SuccessorGeneration" IS NULL AND "CompletedAtUtc" IS NULL AND "RevokedAtUtc" IS NULL AND "ExpiredAtUtc" IS NOT NULL AND "TerminalReason" IS NOT NULL)));
CREATE UNIQUE INDEX "UX_rotation_one_active_installation" ON tagekyc.capture_runtime_rotation_authorizations ("DeviceInstallationId") WHERE "State"='Active';
CREATE INDEX "IX_rotation_expiry" ON tagekyc.capture_runtime_rotation_authorizations ("ExpiresAtUtc","RotationAuthorizationId") WHERE "State"='Active';

CREATE TABLE tagekyc.capture_runtime_management_operations (
 "ActorCredentialId" uuid NOT NULL, "OperationKind" varchar(32) COLLATE "C" NOT NULL,
 "IdempotencyKey" uuid NOT NULL, "RequestFingerprint" bytea NOT NULL,
 "TargetKind" varchar(24) COLLATE "C" NOT NULL, "TargetId" uuid NOT NULL,
 "ResultCode" varchar(32) COLLATE "C" NOT NULL, "ResultRevision" bigint NULL,
 "ResultId" uuid NULL, "CreatedAtUtc" timestamptz NOT NULL, "CompletedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_capture_runtime_management_operations" PRIMARY KEY ("ActorCredentialId","OperationKind","IdempotencyKey"),
 CONSTRAINT "FK_management_actor" FOREIGN KEY ("ActorCredentialId") REFERENCES tagekyc.platform_operator_credentials ("CredentialId") ON DELETE RESTRICT,
 CONSTRAINT "CK_management_kind" CHECK ("OperationKind" IN ('BootstrapIssue','BootstrapRevoke','RuntimeSuspend','RuntimeReactivate','RuntimeRevoke','RuntimeRetire','CredentialRevoke','RotationAuthorize','RotationRevoke','RoleAssign','ConfigurationAssign','TrustPublish','RolePublish','ConfigurationPublish')),
 CONSTRAINT "CK_management_fingerprint" CHECK (pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_management_target" CHECK ("TargetKind" IN ('Bootstrap','Runtime','Credential','Rotation','RolePolicy','TrustProfile','Configuration')),
 CONSTRAINT "CK_management_result" CHECK ("ResultCode"='Applied' AND "ResultRevision">0),
 CONSTRAINT "CK_management_time" CHECK ("CompletedAtUtc">="CreatedAtUtc"));

CREATE TABLE tagekyc.capture_runtime_management_events (
 "EventId" uuid NOT NULL, "ActorCredentialId" uuid NOT NULL,
 "OperationKind" varchar(32) COLLATE "C" NOT NULL, "IdempotencyKey" uuid NOT NULL,
 "EventType" varchar(32) COLLATE "C" NOT NULL, "TargetKind" varchar(24) COLLATE "C" NOT NULL,
 "TargetId" uuid NOT NULL, "BeforeRevision" bigint NULL, "AfterRevision" bigint NULL,
 "Reason" varchar(64) COLLATE "C" NULL, "RecordedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_capture_runtime_management_events" PRIMARY KEY ("EventId"),
 CONSTRAINT "FK_management_event_operation" FOREIGN KEY ("ActorCredentialId","OperationKind","IdempotencyKey") REFERENCES tagekyc.capture_runtime_management_operations ("ActorCredentialId","OperationKind","IdempotencyKey") ON DELETE RESTRICT,
 CONSTRAINT "CK_management_event_type" CHECK ("EventType" IN ('Applied')),
 CONSTRAINT "CK_management_event_revision" CHECK (("BeforeRevision" IS NULL OR "BeforeRevision">0) AND ("AfterRevision" IS NULL OR "AfterRevision">0)));
CREATE INDEX "IX_management_event_operation" ON tagekyc.capture_runtime_management_events ("ActorCredentialId","OperationKind","IdempotencyKey","RecordedAtUtc","EventId");
ALTER TABLE tagekyc.capture_runtime_management_events ADD CONSTRAINT "UQ_management_event_operation" UNIQUE ("ActorCredentialId","OperationKind","IdempotencyKey") DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE tagekyc.capture_capability_operations (
 "ClientApplicationId" uuid NOT NULL, "VerificationSessionId" uuid NOT NULL,
 "OperationKind" varchar(24) COLLATE "C" NOT NULL, "IdempotencyKey" uuid NOT NULL,
 "RequestFingerprint" bytea NOT NULL, "RuntimeCaptureAgentId" uuid NULL,
 "RuntimeInstallationId" uuid NULL, "RuntimeCredentialId" uuid NULL,
 "RuntimeCredentialGeneration" bigint NULL, "ResultCode" varchar(32) COLLATE "C" NOT NULL,
 "ResultCapabilityId" uuid NULL, "ResultBindingId" uuid NULL, "ResultRevision" bigint NULL,
 "CreatedAtUtc" timestamptz NOT NULL, "CompletedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_capture_capability_operations" PRIMARY KEY ("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey"),
 CONSTRAINT "FK_capability_operation_session_client" FOREIGN KEY ("VerificationSessionId","ClientApplicationId") REFERENCES tagekyc.verification_sessions ("Id","ClientApplicationId") ON DELETE RESTRICT,
 CONSTRAINT "CK_capability_operation_kind" CHECK ("OperationKind" IN ('Issue','Replace','Bind','Cancel','Expire')),
 CONSTRAINT "CK_capability_operation_fingerprint" CHECK (pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_capability_operation_actor" CHECK (("RuntimeCaptureAgentId" IS NULL)=("RuntimeInstallationId" IS NULL) AND ("RuntimeInstallationId" IS NULL)=("RuntimeCredentialId" IS NULL) AND ("RuntimeCredentialId" IS NULL)=("RuntimeCredentialGeneration" IS NULL)),
 CONSTRAINT "CK_capability_operation_result" CHECK (("ResultCode"='Applied' AND "ResultRevision" IS NULL OR "ResultCode"='Applied' AND "ResultRevision">0) OR ("ResultCode"='Expired' AND "OperationKind" IN ('Replace','Bind') AND "ResultCapabilityId" IS NOT NULL AND "ResultBindingId" IS NULL AND "ResultRevision">0)),
 CONSTRAINT "CK_capability_operation_time" CHECK ("CompletedAtUtc">="CreatedAtUtc"));

CREATE TABLE tagekyc.capture_capability_events (
 "EventId" uuid NOT NULL, "ClientApplicationId" uuid NOT NULL,
 "VerificationSessionId" uuid NOT NULL, "OperationKind" varchar(24) COLLATE "C" NOT NULL,
 "IdempotencyKey" uuid NOT NULL, "CaptureCapabilityId" uuid NULL,
 "EventType" varchar(24) COLLATE "C" NOT NULL, "RuntimeCaptureAgentId" uuid NULL,
 "RuntimeInstallationId" uuid NULL, "BeforeRevision" bigint NULL,
 "AfterRevision" bigint NULL, "RecordedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_capture_capability_events" PRIMARY KEY ("EventId"),
 CONSTRAINT "FK_capability_event_operation" FOREIGN KEY ("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey") REFERENCES tagekyc.capture_capability_operations ("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey") ON DELETE RESTRICT,
 CONSTRAINT "FK_capability_event_capability" FOREIGN KEY ("CaptureCapabilityId") REFERENCES tagekyc.capture_capabilities ("CaptureCapabilityId") ON DELETE RESTRICT,
 CONSTRAINT "CK_capability_event_type" CHECK ("EventType" IN ('Issued','Replaced','Bound','Expired','Cancelled')),
 CONSTRAINT "CK_capability_event_revision" CHECK (("BeforeRevision" IS NULL OR "BeforeRevision">0) AND ("AfterRevision" IS NULL OR "AfterRevision">0)));
CREATE INDEX "IX_capability_event_operation" ON tagekyc.capture_capability_events ("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RecordedAtUtc","EventId");
ALTER TABLE tagekyc.capture_capability_events ADD CONSTRAINT "UQ_capability_event_operation" UNIQUE ("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey") DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE tagekyc.capture_runtime_bootstrap_redemption_operations (
 "BootstrapIssuanceId" uuid NOT NULL, "RedeemOperationId" uuid NOT NULL,
 "RequestFingerprint" bytea NOT NULL, "CandidateKeyId" uuid NOT NULL,
 "ResultCode" varchar(32) COLLATE "C" NOT NULL, "CaptureAgentId" uuid NULL,
 "DeviceInstallationId" uuid NULL, "CredentialId" uuid NULL, "Generation" bigint NULL,
 "CreatedAtUtc" timestamptz NOT NULL, "CompletedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_bootstrap_redemption_operations" PRIMARY KEY ("BootstrapIssuanceId","RedeemOperationId"),
 CONSTRAINT "FK_redemption_bootstrap" FOREIGN KEY ("BootstrapIssuanceId") REFERENCES tagekyc.capture_runtime_bootstrap_issuances ("BootstrapIssuanceId") ON DELETE RESTRICT,
 CONSTRAINT "FK_redemption_registration" FOREIGN KEY ("CaptureAgentId") REFERENCES tagekyc.capture_runtime_registrations ("CaptureAgentId") ON DELETE RESTRICT,
 CONSTRAINT "FK_redemption_installation" FOREIGN KEY ("DeviceInstallationId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId") ON DELETE RESTRICT,
 CONSTRAINT "FK_redemption_generation" FOREIGN KEY ("DeviceInstallationId","CredentialId","Generation") REFERENCES tagekyc.capture_runtime_credential_generations ("DeviceInstallationId","CredentialId","Generation") ON DELETE RESTRICT,
 CONSTRAINT "UQ_redemption_candidate" UNIQUE ("CandidateKeyId"),
 CONSTRAINT "CK_redemption_fingerprint" CHECK (pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_redemption_result" CHECK (("ResultCode"='Applied' AND "CaptureAgentId" IS NOT NULL AND "DeviceInstallationId" IS NOT NULL AND "CredentialId" IS NOT NULL AND "Generation">0) OR ("ResultCode"='Expired' AND "CaptureAgentId" IS NULL AND "DeviceInstallationId" IS NULL AND "CredentialId" IS NULL AND "Generation" IS NULL)),
 CONSTRAINT "CK_redemption_time" CHECK ("CompletedAtUtc">="CreatedAtUtc"));

CREATE TABLE tagekyc.capture_runtime_rotation_completion_operations (
 "RotationAuthorizationId" uuid NOT NULL, "BusinessIdempotencyKey" uuid NOT NULL,
 "RequestFingerprint" bytea NOT NULL, "DeviceInstallationId" uuid NOT NULL, "CredentialId" uuid NOT NULL,
 "PredecessorGeneration" bigint NOT NULL, "SuccessorGeneration" bigint NULL,
 "CandidateKeyId" uuid NOT NULL, "ResultCode" varchar(32) COLLATE "C" NOT NULL,
 "CredentialRevision" bigint NULL, "InstallationRevision" bigint NULL,
 "RotationRevision" bigint NULL, "CreatedAtUtc" timestamptz NOT NULL,
 "CompletedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_rotation_completion_operations" PRIMARY KEY ("RotationAuthorizationId","BusinessIdempotencyKey"),
 CONSTRAINT "FK_rotation_completion_authorization" FOREIGN KEY ("RotationAuthorizationId") REFERENCES tagekyc.capture_runtime_rotation_authorizations ("RotationAuthorizationId") ON DELETE RESTRICT,
 CONSTRAINT "FK_rotation_completion_predecessor" FOREIGN KEY ("DeviceInstallationId","CredentialId","PredecessorGeneration") REFERENCES tagekyc.capture_runtime_credential_generations ("DeviceInstallationId","CredentialId","Generation") ON DELETE RESTRICT,
 CONSTRAINT "FK_rotation_completion_successor" FOREIGN KEY ("DeviceInstallationId","CredentialId","SuccessorGeneration") REFERENCES tagekyc.capture_runtime_credential_generations ("DeviceInstallationId","CredentialId","Generation") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "UQ_rotation_completion_candidate" UNIQUE ("CandidateKeyId"),
 CONSTRAINT "CK_rotation_completion_fingerprint" CHECK (pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_rotation_completion_generations" CHECK ("PredecessorGeneration">0 AND ("SuccessorGeneration" IS NULL OR "SuccessorGeneration"="PredecessorGeneration"+1)),
 CONSTRAINT "CK_rotation_completion_result" CHECK (("ResultCode"='Applied' AND "SuccessorGeneration" IS NOT NULL AND "CredentialRevision">0 AND "InstallationRevision">0 AND "RotationRevision">0) OR ("ResultCode"='Expired' AND "SuccessorGeneration" IS NULL AND "CredentialRevision" IS NULL AND "InstallationRevision" IS NULL AND "RotationRevision" IS NULL)),
 CONSTRAINT "CK_rotation_completion_time" CHECK ("CompletedAtUtc">="CreatedAtUtc"));

ALTER TABLE tagekyc.capture_runtime_bootstrap_issuances ADD CONSTRAINT "FK_bootstrap_result_installation_agent" FOREIGN KEY ("DeviceInstallationId","CaptureAgentId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId","CaptureAgentId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED;
ALTER TABLE tagekyc.capture_runtime_bootstrap_redemption_operations ADD CONSTRAINT "FK_redemption_installation_agent" FOREIGN KEY ("DeviceInstallationId","CaptureAgentId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId","CaptureAgentId") ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED;
ALTER TABLE tagekyc.capture_runtime_rotation_authorizations ADD CONSTRAINT "AK_rotation_lineage" UNIQUE ("RotationAuthorizationId","DeviceInstallationId","CredentialId","CurrentGeneration");
ALTER TABLE tagekyc.capture_runtime_rotation_authorizations ADD CONSTRAINT "FK_rotation_installation_agent" FOREIGN KEY ("DeviceInstallationId","CaptureAgentId") REFERENCES tagekyc.capture_runtime_installations ("DeviceInstallationId","CaptureAgentId") ON DELETE RESTRICT;
ALTER TABLE tagekyc.capture_runtime_rotation_completion_operations DROP CONSTRAINT "FK_rotation_completion_authorization";
ALTER TABLE tagekyc.capture_runtime_rotation_completion_operations ADD CONSTRAINT "FK_rotation_completion_authorization_lineage" FOREIGN KEY ("RotationAuthorizationId","DeviceInstallationId","CredentialId","PredecessorGeneration") REFERENCES tagekyc.capture_runtime_rotation_authorizations ("RotationAuthorizationId","DeviceInstallationId","CredentialId","CurrentGeneration") ON DELETE RESTRICT;

CREATE TABLE tagekyc.capture_runtime_bootstrap_redemption_events (
 "EventId" uuid NOT NULL, "BootstrapIssuanceId" uuid NOT NULL,
 "RedeemOperationId" uuid NOT NULL, "EventType" varchar(24) COLLATE "C" NOT NULL,
 "CaptureAgentId" uuid NULL, "DeviceInstallationId" uuid NULL,
 "CredentialId" uuid NULL, "Generation" bigint NULL, "RecordedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_bootstrap_redemption_events" PRIMARY KEY ("EventId"),
 CONSTRAINT "UQ_bootstrap_redemption_event_operation" UNIQUE ("BootstrapIssuanceId","RedeemOperationId"),
 CONSTRAINT "FK_bootstrap_redemption_event_operation" FOREIGN KEY ("BootstrapIssuanceId","RedeemOperationId") REFERENCES tagekyc.capture_runtime_bootstrap_redemption_operations ("BootstrapIssuanceId","RedeemOperationId") ON DELETE RESTRICT,
 CONSTRAINT "CK_bootstrap_redemption_event_type" CHECK ("EventType" IN ('Redeemed','Expired')));

CREATE TABLE tagekyc.capture_runtime_rotation_completion_events (
 "EventId" uuid NOT NULL, "RotationAuthorizationId" uuid NOT NULL,
 "BusinessIdempotencyKey" uuid NOT NULL, "EventType" varchar(24) COLLATE "C" NOT NULL,
 "CredentialId" uuid NOT NULL, "PredecessorGeneration" bigint NOT NULL,
 "SuccessorGeneration" bigint NULL, "RecordedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_rotation_completion_events" PRIMARY KEY ("EventId"),
 CONSTRAINT "UQ_rotation_completion_event_operation" UNIQUE ("RotationAuthorizationId","BusinessIdempotencyKey"),
 CONSTRAINT "FK_rotation_completion_event_operation" FOREIGN KEY ("RotationAuthorizationId","BusinessIdempotencyKey") REFERENCES tagekyc.capture_runtime_rotation_completion_operations ("RotationAuthorizationId","BusinessIdempotencyKey") ON DELETE RESTRICT,
 CONSTRAINT "CK_rotation_completion_event_type" CHECK ("EventType" IN ('Completed','Expired')),
 CONSTRAINT "CK_rotation_completion_event_generation" CHECK ("PredecessorGeneration">0 AND ("SuccessorGeneration" IS NULL OR "SuccessorGeneration"="PredecessorGeneration"+1)));

CREATE TABLE tagekyc.platform_operator_root_operations (
 "OperationId" uuid NOT NULL, "OperationKind" varchar(16) COLLATE "C" NOT NULL,
 "RequestFingerprint" bytea NOT NULL, "OperatorPrincipalId" uuid NOT NULL,
 "TargetCredentialId" uuid NULL, "ResultCode" varchar(32) COLLATE "C" NOT NULL,
 "ResultRevision" bigint NULL, "CreatedAtUtc" timestamptz NOT NULL,
 "CompletedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_platform_operator_root_operations" PRIMARY KEY ("OperationId"),
 CONSTRAINT "CK_root_operation_kind" CHECK ("OperationKind" IN ('Provision','Revoke')),
 CONSTRAINT "CK_root_operation_fingerprint" CHECK (pg_catalog.octet_length("RequestFingerprint")=32),
 CONSTRAINT "CK_root_operation_principal" CHECK ("OperatorPrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid),
 CONSTRAINT "CK_root_operation_target" CHECK (("OperationKind"='Provision') OR ("OperationKind"='Revoke' AND "TargetCredentialId" IS NOT NULL)),
 CONSTRAINT "CK_root_operation_result" CHECK ("ResultCode"='Applied' AND "ResultRevision">0),
 CONSTRAINT "CK_root_operation_time" CHECK ("CompletedAtUtc">="CreatedAtUtc"));

CREATE TABLE tagekyc.platform_operator_root_events (
 "EventId" uuid NOT NULL, "OperationId" uuid NOT NULL,
 "EventType" varchar(24) COLLATE "C" NOT NULL, "OperatorPrincipalId" uuid NOT NULL,
 "TargetCredentialId" uuid NULL, "BeforeRevision" bigint NULL,
 "AfterRevision" bigint NULL, "RecordedAtUtc" timestamptz NOT NULL,
 CONSTRAINT "PK_platform_operator_root_events" PRIMARY KEY ("EventId"),
 CONSTRAINT "FK_root_event_operation" FOREIGN KEY ("OperationId") REFERENCES tagekyc.platform_operator_root_operations ("OperationId") ON DELETE RESTRICT,
 CONSTRAINT "UQ_root_event_operation" UNIQUE ("OperationId") DEFERRABLE INITIALLY DEFERRED,
 CONSTRAINT "CK_root_event_type" CHECK ("EventType" IN ('Provisioned','Revoked')),
 CONSTRAINT "CK_root_event_principal" CHECK ("OperatorPrincipalId"<>'00000000-0000-0000-0000-000000000000'::uuid),
 CONSTRAINT "CK_root_event_revision" CHECK (("BeforeRevision" IS NULL OR "BeforeRevision">0) AND ("AfterRevision" IS NULL OR "AfterRevision">0)));

CREATE TABLE tagekyc.capture_runtime_cutover_state (
 "Profile" varchar(16) COLLATE "C" NOT NULL, "State" varchar(16) COLLATE "C" NOT NULL,
 "Revision" bigint NOT NULL, "PreparedAtUtc" timestamptz NOT NULL,
 "ActivatedAtUtc" timestamptz NULL, "ActivatedByCredentialId" uuid NULL,
 CONSTRAINT "PK_capture_runtime_cutover_state" PRIMARY KEY ("Profile"),
 CONSTRAINT "FK_cutover_operator" FOREIGN KEY ("ActivatedByCredentialId") REFERENCES tagekyc.platform_operator_credentials ("CredentialId") ON DELETE RESTRICT,
 CONSTRAINT "CK_cutover_profile" CHECK ("Profile"='Managed'),
 CONSTRAINT "CK_cutover_revision" CHECK ("Revision">0),
 CONSTRAINT "CK_cutover_state" CHECK (("State"='Prepared' AND "ActivatedAtUtc" IS NULL AND "ActivatedByCredentialId" IS NULL) OR ("State"='Activated' AND "ActivatedAtUtc" IS NOT NULL AND "ActivatedByCredentialId" IS NOT NULL)));

INSERT INTO tagekyc.capture_runtime_cutover_state ("Profile","State","Revision","PreparedAtUtc","ActivatedAtUtc","ActivatedByCredentialId") VALUES ('Managed','Prepared',1,statement_timestamp(),NULL,NULL);

CREATE FUNCTION tagekyc.c6ba_reject_row_mutation() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $function$ BEGIN RAISE EXCEPTION 'TIP88C1C6BA_APPEND_ONLY'; END $function$;
ALTER FUNCTION tagekyc.c6ba_reject_row_mutation() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_reject_row_mutation() FROM PUBLIC;
CREATE TRIGGER capture_runtime_management_events_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_management_events FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER capture_capability_events_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_capability_events FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER platform_operator_root_events_immutable BEFORE UPDATE OR DELETE ON tagekyc.platform_operator_root_events FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER bootstrap_redemption_events_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_bootstrap_redemption_events FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER rotation_completion_events_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_rotation_completion_events FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER capture_runtime_management_operations_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_management_operations FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER capture_capability_operations_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_capability_operations FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER bootstrap_redemption_operations_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_bootstrap_redemption_operations FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER rotation_completion_operations_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_rotation_completion_operations FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER platform_operator_root_operations_immutable BEFORE UPDATE OR DELETE ON tagekyc.platform_operator_root_operations FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER role_policy_revisions_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_role_policy_revisions FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER trust_profile_revisions_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_trust_profile_revisions FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();
CREATE TRIGGER configuration_revisions_immutable BEFORE UPDATE OR DELETE ON tagekyc.capture_runtime_configuration_revisions FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_reject_row_mutation();

CREATE FUNCTION tagekyc.c6ba_validate_current_generation() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE v_bad boolean;
BEGIN
 SELECT EXISTS (
  SELECT 1 FROM tagekyc.capture_runtime_installations AS i
  LEFT JOIN tagekyc.capture_runtime_credential_generations AS g
    ON g."DeviceInstallationId"=i."DeviceInstallationId"
   AND g."CredentialId"=i."CurrentCredentialId"
   AND g."Generation"=i."CurrentCredentialGeneration"
  WHERE (i."LifecycleState"='Active' AND (g."State" IS DISTINCT FROM 'Active'))
     OR (g."State"='Active' AND (i."LifecycleState" IS DISTINCT FROM 'Active'
          OR i."CurrentCredentialId" IS DISTINCT FROM g."CredentialId"
          OR i."CurrentCredentialGeneration" IS DISTINCT FROM g."Generation"))
  UNION ALL
  SELECT 1 FROM tagekyc.capture_runtime_credential_generations AS g
  LEFT JOIN tagekyc.capture_runtime_installations AS i
    ON i."DeviceInstallationId"=g."DeviceInstallationId"
   AND i."CurrentCredentialId"=g."CredentialId"
   AND i."CurrentCredentialGeneration"=g."Generation"
  WHERE g."State"='Active' AND (i."LifecycleState" IS DISTINCT FROM 'Active')
 ) INTO v_bad;
 IF v_bad THEN RAISE EXCEPTION 'TIP88C1C6BA_CURRENT_GENERATION_INCONSISTENT'; END IF;
 RETURN NULL;
END $function$;
ALTER FUNCTION tagekyc.c6ba_validate_current_generation() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_validate_current_generation() FROM PUBLIC;
CREATE CONSTRAINT TRIGGER installation_current_generation_guard AFTER INSERT OR UPDATE OR DELETE ON tagekyc.capture_runtime_installations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_validate_current_generation();
CREATE CONSTRAINT TRIGGER generation_current_installation_guard AFTER INSERT OR UPDATE OR DELETE ON tagekyc.capture_runtime_credential_generations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_validate_current_generation();

CREATE FUNCTION tagekyc.c6ba_validate_capability_graph() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE v_bad boolean;
BEGIN
 SELECT EXISTS (
  SELECT 1 FROM tagekyc.capture_capabilities AS c
  LEFT JOIN tagekyc.capture_capabilities AS p ON p."CaptureCapabilityId"=c."PredecessorCapabilityId"
  LEFT JOIN tagekyc.capture_capabilities AS s ON s."CaptureCapabilityId"=c."SuccessorCapabilityId"
  WHERE (c."PredecessorCapabilityId" IS NOT NULL AND (p."SuccessorCapabilityId" IS DISTINCT FROM c."CaptureCapabilityId" OR p."VerificationSessionId" IS DISTINCT FROM c."VerificationSessionId"))
     OR (c."SuccessorCapabilityId" IS NOT NULL AND (s."PredecessorCapabilityId" IS DISTINCT FROM c."CaptureCapabilityId" OR s."VerificationSessionId" IS DISTINCT FROM c."VerificationSessionId"))
     OR (c."State"='ActiveUnbound' AND EXISTS (SELECT 1 FROM tagekyc.capture_capabilities AS b WHERE b."VerificationSessionId"=c."VerificationSessionId" AND b."State"='Bound'))
     -- Binding is immutable history: a formerly Bound capability retains it
     -- after the ratified Revoked/Expired transition. This is not live authority.
     OR (c."BoundAtUtc" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings AS b WHERE b."CaptureCapabilityId"=c."CaptureCapabilityId" AND b."VerificationSessionId"=c."VerificationSessionId" AND b."ClientApplicationId"=c."ClientApplicationId"))
     OR (c."BoundAtUtc" IS NULL AND EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings AS b WHERE b."CaptureCapabilityId"=c."CaptureCapabilityId"))
 ) INTO v_bad;
 IF v_bad THEN RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_GRAPH_INCONSISTENT'; END IF;
 RETURN NULL;
END $function$;
ALTER FUNCTION tagekyc.c6ba_validate_capability_graph() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_validate_capability_graph() FROM PUBLIC;
CREATE CONSTRAINT TRIGGER capability_graph_guard AFTER INSERT OR UPDATE OR DELETE ON tagekyc.capture_capabilities DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_validate_capability_graph();
CREATE CONSTRAINT TRIGGER binding_capability_graph_guard AFTER INSERT OR UPDATE OR DELETE ON tagekyc.capture_execution_bindings DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_validate_capability_graph();

CREATE FUNCTION tagekyc.c6ba_validate_configuration_override() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE b tagekyc.capture_runtime_configuration_revisions%ROWTYPE;
BEGIN
 SELECT * INTO STRICT b FROM tagekyc.capture_runtime_configuration_revisions WHERE "CatalogId"=NEW."BaseConfigurationId" AND "Revision"=NEW."BaseConfigurationRevision";
 IF (NEW."PlaintextBudgetSeconds" IS NOT NULL AND NEW."PlaintextBudgetSeconds">b."PlaintextBudgetSeconds")
 OR (NEW."RawExportSourceClaimSafetyMarginMilliseconds" IS NOT NULL AND NEW."RawExportSourceClaimSafetyMarginMilliseconds">b."RawExportSourceClaimSafetyMarginMilliseconds")
 OR (NEW."CaptureAgentConfigurationPollingIntervalSeconds" IS NOT NULL AND NEW."CaptureAgentConfigurationPollingIntervalSeconds">b."CaptureAgentConfigurationPollingIntervalSeconds")
 OR (NEW."RawExportSourceMaximumChipDg2PortraitBytes" IS NOT NULL AND NEW."RawExportSourceMaximumChipDg2PortraitBytes">b."RawExportSourceMaximumChipDg2PortraitBytes")
 OR (NEW."RawExportSourceMaximumLiveSelfieImageBytes" IS NOT NULL AND NEW."RawExportSourceMaximumLiveSelfieImageBytes">b."RawExportSourceMaximumLiveSelfieImageBytes")
 OR (NEW."RawExportCaptureMaximumAggregatePlaintextBytesPerHost" IS NOT NULL AND NEW."RawExportCaptureMaximumAggregatePlaintextBytesPerHost">b."RawExportCaptureMaximumAggregatePlaintextBytesPerHost")
 OR (NEW."RawExportCustodyMaximumPlaintextWindowBytesPerStream" IS NOT NULL AND NEW."RawExportCustodyMaximumPlaintextWindowBytesPerStream">b."RawExportCustodyMaximumPlaintextWindowBytesPerStream")
 OR (NEW."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment" IS NOT NULL AND NEW."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment">b."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment")
 OR (NEW."RawExportIngressMaximumPreAdmissionBufferedBytes" IS NOT NULL AND NEW."RawExportIngressMaximumPreAdmissionBufferedBytes">b."RawExportIngressMaximumPreAdmissionBufferedBytes") THEN
  RAISE EXCEPTION 'TIP88C1C6BA_CONFIGURATION_OVERRIDE_WIDENS_BASE';
 END IF;
 RETURN NEW;
END $function$;
ALTER FUNCTION tagekyc.c6ba_validate_configuration_override() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_validate_configuration_override() FROM PUBLIC;
CREATE TRIGGER configuration_override_narrowing_guard BEFORE INSERT OR UPDATE ON tagekyc.capture_runtime_configuration_overrides FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_validate_configuration_override();

CREATE FUNCTION tagekyc.c6ba_require_management_event() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$ BEGIN IF NEW."ResultCode"<>'Applied' OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_management_events e WHERE e."ActorCredentialId"=NEW."ActorCredentialId" AND e."OperationKind"=NEW."OperationKind" AND e."IdempotencyKey"=NEW."IdempotencyKey" AND e."TargetKind"=NEW."TargetKind" AND e."TargetId"=NEW."TargetId" AND e."AfterRevision" IS NOT DISTINCT FROM NEW."ResultRevision" AND e."EventType"='Applied') THEN RAISE EXCEPTION 'TIP88C1C6BA_MANAGEMENT_EVENT_MISMATCH'; END IF; RETURN NULL; END $function$;
CREATE CONSTRAINT TRIGGER management_operation_requires_event AFTER INSERT ON tagekyc.capture_runtime_management_operations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_require_management_event();
CREATE FUNCTION tagekyc.c6ba_require_capability_event() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$ BEGIN IF NOT EXISTS (SELECT 1 FROM tagekyc.capture_capability_events e WHERE e."ClientApplicationId"=NEW."ClientApplicationId" AND e."VerificationSessionId"=NEW."VerificationSessionId" AND e."OperationKind"=NEW."OperationKind" AND e."IdempotencyKey"=NEW."IdempotencyKey" AND e."CaptureCapabilityId" IS NOT DISTINCT FROM NEW."ResultCapabilityId" AND e."RuntimeCaptureAgentId" IS NOT DISTINCT FROM NEW."RuntimeCaptureAgentId" AND e."RuntimeInstallationId" IS NOT DISTINCT FROM NEW."RuntimeInstallationId" AND e."AfterRevision" IS NOT DISTINCT FROM NEW."ResultRevision" AND e."EventType"=CASE WHEN NEW."ResultCode"='Expired' THEN 'Expired' ELSE CASE NEW."OperationKind" WHEN 'Issue' THEN 'Issued' WHEN 'Replace' THEN 'Replaced' WHEN 'Bind' THEN 'Bound' WHEN 'Cancel' THEN 'Cancelled' WHEN 'Expire' THEN 'Expired' END END) THEN RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_EVENT_MISMATCH'; END IF; RETURN NULL; END $function$;
CREATE CONSTRAINT TRIGGER capability_operation_requires_event AFTER INSERT ON tagekyc.capture_capability_operations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_require_capability_event();
CREATE FUNCTION tagekyc.c6ba_require_redemption_event() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$ BEGIN IF NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_bootstrap_redemption_events e WHERE e."BootstrapIssuanceId"=NEW."BootstrapIssuanceId" AND e."RedeemOperationId"=NEW."RedeemOperationId" AND e."CaptureAgentId" IS NOT DISTINCT FROM NEW."CaptureAgentId" AND e."DeviceInstallationId" IS NOT DISTINCT FROM NEW."DeviceInstallationId" AND e."CredentialId" IS NOT DISTINCT FROM NEW."CredentialId" AND e."Generation" IS NOT DISTINCT FROM NEW."Generation" AND e."EventType"=CASE NEW."ResultCode" WHEN 'Applied' THEN 'Redeemed' WHEN 'Expired' THEN 'Expired' END) THEN RAISE EXCEPTION 'TIP88C1C6BA_REDEMPTION_EVENT_MISMATCH'; END IF; RETURN NULL; END $function$;
CREATE CONSTRAINT TRIGGER redemption_operation_requires_event AFTER INSERT ON tagekyc.capture_runtime_bootstrap_redemption_operations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_require_redemption_event();
CREATE FUNCTION tagekyc.c6ba_require_rotation_completion_event() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$ BEGIN IF NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_rotation_completion_events e WHERE e."RotationAuthorizationId"=NEW."RotationAuthorizationId" AND e."BusinessIdempotencyKey"=NEW."BusinessIdempotencyKey" AND e."CredentialId"=NEW."CredentialId" AND e."PredecessorGeneration"=NEW."PredecessorGeneration" AND e."SuccessorGeneration" IS NOT DISTINCT FROM NEW."SuccessorGeneration" AND e."EventType"=CASE NEW."ResultCode" WHEN 'Applied' THEN 'Completed' WHEN 'Expired' THEN 'Expired' END) THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_EVENT_MISMATCH'; END IF; RETURN NULL; END $function$;
CREATE CONSTRAINT TRIGGER rotation_operation_requires_event AFTER INSERT ON tagekyc.capture_runtime_rotation_completion_operations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_require_rotation_completion_event();
CREATE FUNCTION tagekyc.c6ba_require_root_event() RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$ BEGIN IF NEW."ResultCode"<>'Applied' OR NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_root_events e WHERE e."OperationId"=NEW."OperationId" AND e."OperatorPrincipalId"=NEW."OperatorPrincipalId" AND e."TargetCredentialId" IS NOT DISTINCT FROM NEW."TargetCredentialId" AND e."AfterRevision" IS NOT DISTINCT FROM NEW."ResultRevision" AND e."EventType"=CASE NEW."OperationKind" WHEN 'Provision' THEN 'Provisioned' WHEN 'Revoke' THEN 'Revoked' END) THEN RAISE EXCEPTION 'TIP88C1C6BA_ROOT_EVENT_MISMATCH'; END IF; RETURN NULL; END $function$;
CREATE CONSTRAINT TRIGGER root_operation_requires_event AFTER INSERT ON tagekyc.platform_operator_root_operations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION tagekyc.c6ba_require_root_event();

ALTER FUNCTION tagekyc.c6ba_require_management_event() OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_require_capability_event() OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_require_redemption_event() OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_require_rotation_completion_event() OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_require_root_event() OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_require_management_event() FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.c6ba_require_capability_event() FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.c6ba_require_redemption_event() FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.c6ba_require_rotation_completion_event() FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.c6ba_require_root_event() FROM PUBLIC;

ALTER TABLE tagekyc.platform_operator_credentials OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_role_policy_revisions OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_role_policy_heads OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_trust_profile_revisions OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_trust_profile_heads OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_configuration_revisions OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_configuration_heads OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_registrations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_installations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_credential_generations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_request_nonces OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_bootstrap_issuances OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_capabilities OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_execution_bindings OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_configuration_overrides OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_rotation_authorizations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_management_operations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_management_events OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_capability_operations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_capability_events OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_bootstrap_redemption_operations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_rotation_completion_operations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_bootstrap_redemption_events OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_rotation_completion_events OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.platform_operator_root_operations OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.platform_operator_root_events OWNER TO tagekyc_raw_export_deployer;
ALTER TABLE tagekyc.capture_runtime_cutover_state OWNER TO tagekyc_raw_export_deployer;

REVOKE ALL ON TABLE
 tagekyc.platform_operator_credentials,
 tagekyc.capture_runtime_role_policy_revisions,
 tagekyc.capture_runtime_role_policy_heads,
 tagekyc.capture_runtime_trust_profile_revisions,
 tagekyc.capture_runtime_trust_profile_heads,
 tagekyc.capture_runtime_configuration_revisions,
 tagekyc.capture_runtime_configuration_heads,
 tagekyc.capture_runtime_registrations,
 tagekyc.capture_runtime_installations,
 tagekyc.capture_runtime_credential_generations,
 tagekyc.capture_runtime_request_nonces,
 tagekyc.capture_runtime_bootstrap_issuances,
 tagekyc.capture_capabilities,
 tagekyc.capture_execution_bindings,
 tagekyc.capture_runtime_configuration_overrides,
 tagekyc.capture_runtime_rotation_authorizations,
 tagekyc.capture_runtime_management_operations,
 tagekyc.capture_runtime_management_events,
 tagekyc.capture_capability_operations,
 tagekyc.capture_capability_events,
 tagekyc.capture_runtime_bootstrap_redemption_operations,
 tagekyc.capture_runtime_rotation_completion_operations,
 tagekyc.capture_runtime_bootstrap_redemption_events,
 tagekyc.capture_runtime_rotation_completion_events,
 tagekyc.platform_operator_root_operations,
 tagekyc.platform_operator_root_events,
 tagekyc.capture_runtime_cutover_state
 FROM PUBLIC,
      tagekyc_runtime,
      tagekyc_capture_runtime_authenticator,
      tagekyc_capture_runtime_operator,
      tagekyc_capture_runtime_application;
""");
            migrationBuilder.Sql("""
CREATE FUNCTION tagekyc.c6ba_root_provision_platform_credential(
 p_operation_id uuid, p_principal_id uuid, p_expires_at_utc timestamptz,
 p_key_lookup_prefix text, p_secret_digest bytea, p_verifier_pepper_version integer,
 p_request_fingerprint bytea, p_now_utc timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,key_lookup_prefix text,
 secret_available boolean,expires_at_utc timestamptz,revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE o tagekyc.platform_operator_root_operations%ROWTYPE; c_id uuid; c tagekyc.platform_operator_credentials%ROWTYPE;
BEGIN
 IF p_operation_id IS NULL OR p_principal_id IS NULL OR p_principal_id='00000000-0000-0000-0000-000000000000'::uuid
 OR p_expires_at_utc<=p_now_utc OR p_key_lookup_prefix!~'^[A-Za-z0-9_-]{12}$'
 OR pg_catalog.octet_length(p_secret_digest)<>32 OR p_verifier_pepper_version<=0
 OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_key_lookup_prefix,1)));
 PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_key_lookup_prefix,1)));
 SELECT * INTO o FROM tagekyc.platform_operator_root_operations WHERE "OperationId"=p_operation_id;
 IF FOUND THEN
  IF o."OperationKind"<>'Provision' OR o."RequestFingerprint"<>p_request_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,false,NULL::timestamptz,NULL::bigint;
  ELSE SELECT * INTO c FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=o."TargetCredentialId"; RETURN QUERY SELECT 'ExistingMatchSecretUnavailable',o."TargetCredentialId",c."KeyLookupPrefix"::text,false,c."ExpiresAtUtc",o."ResultRevision"; END IF; RETURN;
 END IF;
 c_id:=pg_catalog.gen_random_uuid();
 INSERT INTO tagekyc.platform_operator_credentials VALUES(c_id,p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,p_principal_id,ARRAY['operator.capture-runtime.manage']::text[],'Active',1,p_now_utc,p_expires_at_utc,NULL,NULL);
 INSERT INTO tagekyc.platform_operator_root_operations VALUES(p_operation_id,'Provision',p_request_fingerprint,p_principal_id,c_id,'Applied',1,p_now_utc,p_now_utc);
 INSERT INTO tagekyc.platform_operator_root_events VALUES(pg_catalog.gen_random_uuid(),p_operation_id,'Provisioned',p_principal_id,c_id,NULL,1,p_now_utc);
 RETURN QUERY SELECT 'Created',c_id,p_key_lookup_prefix,true,p_expires_at_utc,1::bigint;
END $function$;

CREATE FUNCTION tagekyc.c6ba_root_revoke_platform_credential(
 p_operation_id uuid,p_credential_id uuid,p_expected_revision bigint,p_reason text,
 p_request_fingerprint bytea,p_now_utc timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,state text,revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE o tagekyc.platform_operator_root_operations%ROWTYPE; c tagekyc.platform_operator_credentials%ROWTYPE;
BEGIN
 IF p_expected_revision<=0 OR p_reason<>'DeploymentRevocation' OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_credential_id::text,1)));
 PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(pg_catalog.hashtextextended(p_operation_id::text,1),pg_catalog.hashtextextended(p_credential_id::text,1)));
 SELECT * INTO o FROM tagekyc.platform_operator_root_operations WHERE "OperationId"=p_operation_id;
 IF FOUND THEN IF o."OperationKind"='Revoke' AND o."RequestFingerprint"=p_request_fingerprint THEN RETURN QUERY SELECT 'Revoked',o."TargetCredentialId",'Revoked',o."ResultRevision",o."CompletedAtUtc"; ELSE RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; END IF; RETURN; END IF;
 SELECT * INTO c FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=p_credential_id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF;
 IF c."State"<>'Active' OR c."Revision"<>p_expected_revision THEN RETURN QUERY SELECT 'Conflict',c."CredentialId",c."State"::text,c."Revision",c."RevokedAtUtc"; RETURN; END IF;
 UPDATE tagekyc.platform_operator_credentials SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now_utc,"RevocationReason"=p_reason WHERE "CredentialId"=p_credential_id RETURNING * INTO c;
 INSERT INTO tagekyc.platform_operator_root_operations VALUES(p_operation_id,'Revoke',p_request_fingerprint,c."PrincipalId",p_credential_id,'Applied',c."Revision",p_now_utc,p_now_utc);
 INSERT INTO tagekyc.platform_operator_root_events VALUES(pg_catalog.gen_random_uuid(),p_operation_id,'Revoked',c."PrincipalId",p_credential_id,p_expected_revision,c."Revision",p_now_utc);
 RETURN QUERY SELECT 'Revoked',c."CredentialId",c."State"::text,c."Revision",c."RevokedAtUtc";
END $function$;

CREATE FUNCTION tagekyc.capture_runtime_issue_bootstrap(
 p_actor_credential_id uuid,p_idempotency_key uuid,p_runtime_type text,
 p_trust_profile_id uuid,p_trust_profile_revision bigint,p_role_policy_id uuid,
 p_role_policy_revision bigint,p_configuration_id uuid,p_configuration_revision bigint,
 p_expires_at_utc timestamptz,p_attestation_digest bytea,p_key_lookup_prefix text,
 p_secret_digest bytea,p_verifier_pepper_version integer,p_request_fingerprint bytea,p_now_utc timestamptz)
RETURNS TABLE(result_code text,bootstrap_issuance_id uuid,secret_available boolean,expires_at_utc timestamptz,revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $function$
DECLARE op tagekyc.capture_runtime_management_operations%ROWTYPE; b_id uuid; b tagekyc.capture_runtime_bootstrap_issuances%ROWTYPE;
BEGIN
 IF p_runtime_type<>'Managed' OR p_expires_at_utc<=p_now_utc OR p_key_lookup_prefix!~'^[A-Za-z0-9_-]{12}$' OR pg_catalog.octet_length(p_secret_digest)<>32 OR p_verifier_pepper_version<=0 OR pg_catalog.octet_length(p_attestation_digest)<>32 OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(pg_catalog.hashtextextended(p_idempotency_key::text,5),pg_catalog.hashtextextended(p_key_lookup_prefix,5)));
 PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(pg_catalog.hashtextextended(p_idempotency_key::text,5),pg_catalog.hashtextextended(p_key_lookup_prefix,5)));
 IF NOT EXISTS(SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=p_actor_credential_id AND "State"='Active' AND "ExpiresAtUtc">p_now_utc AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RETURN QUERY SELECT 'Denied',NULL::uuid,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor_credential_id AND "OperationKind"='BootstrapIssue' AND "IdempotencyKey"=p_idempotency_key;
 IF FOUND THEN IF op."RequestFingerprint"=p_request_fingerprint THEN SELECT * INTO STRICT b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=op."ResultId"; RETURN QUERY SELECT 'ExistingMatchSecretUnavailable',op."ResultId",false,b."ExpiresAtUtc",op."ResultRevision"; ELSE RETURN QUERY SELECT 'Conflict',NULL::uuid,false,NULL::timestamptz,NULL::bigint; END IF; RETURN; END IF;
 IF NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions WHERE "CatalogId"=p_trust_profile_id AND "Revision"=p_trust_profile_revision) OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions WHERE "CatalogId"=p_role_policy_id AND "Revision"=p_role_policy_revision) OR NOT EXISTS(SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions WHERE "CatalogId"=p_configuration_id AND "Revision"=p_configuration_revision) THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,false,NULL::timestamptz,NULL::bigint; RETURN; END IF;
 b_id:=pg_catalog.gen_random_uuid();
 INSERT INTO tagekyc.capture_runtime_bootstrap_issuances("BootstrapIssuanceId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion","RuntimeType","TrustProfileId","TrustProfileRevision","RolePolicyId","RolePolicyRevision","ConfigurationId","ConfigurationRevision","AttestationRequirementDigest","RequestFingerprint","IssueOperationId","IssuedByCredentialId","IssuedAtUtc","ExpiresAtUtc","State","Revision") VALUES(b_id,p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,p_runtime_type,p_trust_profile_id,p_trust_profile_revision,p_role_policy_id,p_role_policy_revision,p_configuration_id,p_configuration_revision,p_attestation_digest,p_request_fingerprint,p_idempotency_key,p_actor_credential_id,p_now_utc,p_expires_at_utc,'Active',1);
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor_credential_id,'BootstrapIssue',p_idempotency_key,p_request_fingerprint,'Bootstrap',b_id,'Applied',1,b_id,p_now_utc,p_now_utc);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor_credential_id,'BootstrapIssue',p_idempotency_key,'Applied','Bootstrap',b_id,NULL,1,NULL,p_now_utc);
 RETURN QUERY SELECT 'Created',b_id,true,p_expires_at_utc,1::bigint;
END $function$;

ALTER FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
""");
            migrationBuilder.Sql("""
CREATE FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz)
RETURNS TABLE(result_code text,bootstrap_issuance_id uuid,state text,revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a ALIAS FOR $1; k ALIAS FOR $2; id ALIAS FOR $3; er ALIAS FOR $4; why ALIAS FOR $5; fp ALIAS FOR $6; n ALIAS FOR $7; o tagekyc.capture_runtime_management_operations%ROWTYPE; b tagekyc.capture_runtime_bootstrap_issuances%ROWTYPE;
BEGIN
 IF er<=0 OR why<>'OperatorRevocation' OR octet_length(fp)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 PERFORM pg_advisory_xact_lock(LEAST(hashtextextended(id::text,5),hashtextextended(k::text,5)));
 PERFORM pg_advisory_xact_lock(GREATEST(hashtextextended(id::text,5),hashtextextended(k::text,5)));
 IF NOT EXISTS(SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=a AND "State"='Active' AND "ExpiresAtUtc">n AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 SELECT * INTO o FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=a AND "OperationKind"='BootstrapRevoke' AND "IdempotencyKey"=k;
 IF FOUND THEN IF o."RequestFingerprint"<>fp THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;ELSE SELECT * INTO b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=o."TargetId";RETURN QUERY SELECT 'Revoked',b."BootstrapIssuanceId",b."State"::text,b."Revision",b."RevokedAtUtc";END IF;RETURN;END IF;
 SELECT * INTO b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=id FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 IF b."State"<>'Active' OR b."Revision"<>er THEN RETURN QUERY SELECT 'Conflict',b."BootstrapIssuanceId",b."State"::text,b."Revision",b."RevokedAtUtc";RETURN;END IF;
 UPDATE tagekyc.capture_runtime_bootstrap_issuances SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=n,"TerminalReason"=why WHERE "BootstrapIssuanceId"=id RETURNING * INTO b;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(a,'BootstrapRevoke',k,fp,'Bootstrap',id,'Applied',b."Revision",id,n,n);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(gen_random_uuid(),a,'BootstrapRevoke',k,'Applied','Bootstrap',id,er,b."Revision",why,n);
 RETURN QUERY SELECT 'Revoked',id,b."State"::text,b."Revision",b."RevokedAtUtc";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,device_installation_id uuid,credential_id uuid,generation bigint,runtime_revision bigint,installation_revision bigint,credential_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE bid ALIAS FOR $1; opid ALIAS FOR $2; fp ALIAS FOR $3; kid ALIAS FOR $4; spki ALIAS FOR $5; thumb ALIAS FOR $6; sat ALIAS FOR $7; nonce ALIAS FOR $8; proof ALIAS FOR $9; digest ALIAS FOR $10; n ALIAS FOR $11; b tagekyc.capture_runtime_bootstrap_issuances%ROWTYPE;o tagekyc.capture_runtime_bootstrap_redemption_operations%ROWTYPE;a uuid;i uuid;c uuid;
BEGIN
 IF octet_length(fp)<>32 OR octet_length(spki)<>91 OR octet_length(thumb)<>32 OR octet_length(nonce)<>32 OR octet_length(proof)<>64 OR octet_length(digest)<>32 THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;RETURN;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(bid::text,5));
 PERFORM pg_advisory_xact_lock(hashtextextended(bid::text,10));
 PERFORM pg_advisory_xact_lock(hashtextextended(kid::text,20));
 PERFORM pg_advisory_xact_lock(hashtextextended(kid::text,30));
 SELECT * INTO o FROM tagekyc.capture_runtime_bootstrap_redemption_operations WHERE "BootstrapIssuanceId"=bid AND "RedeemOperationId"=opid;
 IF FOUND THEN IF o."RequestFingerprint"<>fp OR o."CandidateKeyId"<>kid THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; ELSIF o."ResultCode"='Expired' THEN RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; ELSE RETURN QUERY SELECT 'Replay',o."CaptureAgentId",o."DeviceInstallationId",o."CredentialId",o."Generation",1::bigint,2::bigint,2::bigint; END IF;RETURN;END IF;
 SELECT * INTO b FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=bid FOR UPDATE;
 IF FOUND AND b."State"='Active' AND b."ExpiresAtUtc"<=n THEN
  UPDATE tagekyc.capture_runtime_bootstrap_issuances SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=n,"TerminalReason"='BootstrapExpired' WHERE "BootstrapIssuanceId"=bid;
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(bid,opid,fp,kid,'Expired',NULL,NULL,NULL,NULL,n,n);
  INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),bid,opid,'Expired',NULL,NULL,NULL,NULL,n);
  RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF NOT FOUND OR b."State"<>'Active' OR b."SecretDigest"<>digest OR sat<n-interval '150 seconds' OR sat>n+interval '150 seconds' THEN RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;RETURN;END IF;
 a:=gen_random_uuid();i:=gen_random_uuid();c:=gen_random_uuid();
 INSERT INTO tagekyc.capture_runtime_registrations("CaptureAgentId","RuntimeType","TrustProfileId","TrustProfileRevision","ConfigurationId","ConfigurationRevision","LifecycleState","Revision","CreatedAtUtc") VALUES(a,'Managed',b."TrustProfileId",b."TrustProfileRevision",b."ConfigurationId",b."ConfigurationRevision",'Active',1,n);
 INSERT INTO tagekyc.capture_runtime_installations("DeviceInstallationId","CaptureAgentId","LifecycleState","Revision","EnrolledAtUtc") VALUES(i,a,'Pending',1,n);
 INSERT INTO tagekyc.capture_runtime_credential_generations("CredentialId","Generation","DeviceInstallationId","CandidateKeyId","PublicVerifierSpki","Algorithm","PublicKeyThumbprint","RolePolicyId","RolePolicyRevision","ValidFromUtc","ValidUntilUtc","State","Revision") VALUES(c,1,i,kid,spki,'TAG-EKYC-CRT1-ECDSA-P256-SHA256',thumb,b."RolePolicyId",b."RolePolicyRevision",n,b."ExpiresAtUtc",'Pending',1);
 UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"=c,"CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"=i;UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"=c;UPDATE tagekyc.capture_runtime_bootstrap_issuances SET "State"='Redeemed',"Revision"="Revision"+1,"RedeemedAtUtc"=n,"CaptureAgentId"=a,"DeviceInstallationId"=i,"CredentialId"=c,"CredentialGeneration"=1 WHERE "BootstrapIssuanceId"=bid;
 INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_operations VALUES(bid,opid,fp,kid,'Applied',a,i,c,1,n,n);INSERT INTO tagekyc.capture_runtime_bootstrap_redemption_events VALUES(gen_random_uuid(),bid,opid,'Redeemed',a,i,c,1,n);RETURN QUERY SELECT 'Created',a,i,c,1::bigint,1::bigint,2::bigint,2::bigint;
END $f$;
ALTER FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) TO tagekyc_capture_runtime_application;
""");
            migrationBuilder.Sql("""
CREATE FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean)
RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE actor ALIAS FOR $1; idem ALIAS FOR $2; aid ALIAS FOR $3; expected ALIAS FOR $4; reason ALIAS FOR $5; fp ALIAS FOR $6; n ALIAS FOR $7; kind ALIAS FOR $8; from_state ALIAS FOR $9; to_state ALIAS FOR $10; terminalize ALIAS FOR $11; op tagekyc.capture_runtime_management_operations%ROWTYPE; r tagekyc.capture_runtime_registrations%ROWTYPE; i tagekyc.capture_runtime_installations%ROWTYPE; g tagekyc.capture_runtime_credential_generations%ROWTYPE; lineage_count bigint;
BEGIN
 IF expected<=0 OR octet_length(fp)<>32 OR NOT ((kind='RuntimeSuspend' AND reason='OperatorSuspension') OR (kind='RuntimeReactivate' AND reason='OperatorReactivation') OR (kind='RuntimeRevoke' AND reason='OperatorRevocation') OR (kind='RuntimeRetire' AND reason='OperatorRetirement')) THEN RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(aid::text,10));
 IF NOT EXISTS(SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=actor AND "State"='Active' AND "ExpiresAtUtc">n AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=actor AND "OperationKind"=kind AND "IdempotencyKey"=idem;
 IF FOUND THEN IF op."RequestFingerprint"<>fp THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;ELSE RETURN QUERY SELECT 'Applied',op."TargetId",CASE op."OperationKind" WHEN 'RuntimeSuspend' THEN 'Suspended' WHEN 'RuntimeReactivate' THEN 'Active' WHEN 'RuntimeRevoke' THEN 'Revoked' WHEN 'RuntimeRetire' THEN 'Retired' END,op."ResultRevision",op."CompletedAtUtc";END IF;RETURN;END IF;
 SELECT * INTO r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=aid;
 IF NOT FOUND THEN RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz;RETURN;END IF;
 SELECT pg_catalog.count(*) INTO lineage_count FROM tagekyc.capture_runtime_installations
 WHERE "CaptureAgentId"=aid
   AND ((kind='RuntimeRetire' AND "LifecycleState"='Revoked' AND "RevokedAtUtc" IS NOT DISTINCT FROM r."RevokedAtUtc")
        OR (kind<>'RuntimeRetire' AND "LifecycleState"='Active'));
 IF lineage_count<>1 THEN RETURN QUERY SELECT 'NotReady',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 SELECT * INTO STRICT i FROM tagekyc.capture_runtime_installations
 WHERE "CaptureAgentId"=aid
   AND ((kind='RuntimeRetire' AND "LifecycleState"='Revoked' AND "RevokedAtUtc" IS NOT DISTINCT FROM r."RevokedAtUtc")
        OR (kind<>'RuntimeRetire' AND "LifecycleState"='Active'));
 PERFORM pg_advisory_xact_lock(hashtextextended(i."DeviceInstallationId"::text,20));
 PERFORM pg_advisory_xact_lock(hashtextextended(i."CurrentCredentialId"::text||':'||i."CurrentCredentialGeneration"::text,30));
 SELECT * INTO r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=aid FOR UPDATE;
 SELECT ix.* INTO i FROM tagekyc.capture_runtime_installations ix
 WHERE ix."DeviceInstallationId"=i."DeviceInstallationId"
   AND ((kind='RuntimeRetire' AND ix."LifecycleState"='Revoked' AND ix."RevokedAtUtc" IS NOT DISTINCT FROM r."RevokedAtUtc")
        OR (kind<>'RuntimeRetire' AND ix."LifecycleState"='Active')) FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotReady',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 SELECT * INTO g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=i."DeviceInstallationId" AND "CredentialId"=i."CurrentCredentialId" AND "Generation"=i."CurrentCredentialGeneration" AND ((kind='RuntimeRetire' AND "State"='Revoked') OR (kind<>'RuntimeRetire' AND "State"='Active')) FOR UPDATE;
 IF NOT FOUND THEN RETURN QUERY SELECT 'NotReady',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 IF r."Revision"<>expected OR (kind IN ('RuntimeSuspend','RuntimeReactivate','RuntimeRetire') AND r."LifecycleState"<>from_state) OR (kind='RuntimeRevoke' AND r."LifecycleState" NOT IN ('Active','Suspended')) THEN RETURN QUERY SELECT 'Conflict',r."CaptureAgentId",r."LifecycleState"::text,r."Revision",NULL::timestamptz;RETURN;END IF;
 UPDATE tagekyc.capture_runtime_registrations SET "LifecycleState"=to_state,"Revision"="Revision"+1,"SuspendedAtUtc"=CASE WHEN to_state='Suspended' THEN n WHEN to_state='Active' THEN NULL ELSE "SuspendedAtUtc" END,"RevokedAtUtc"=CASE WHEN to_state='Revoked' THEN n ELSE "RevokedAtUtc" END,"RetiredAtUtc"=CASE WHEN to_state='Retired' THEN n ELSE "RetiredAtUtc" END,"LifecycleReason"=CASE WHEN to_state='Active' THEN NULL ELSE reason END WHERE "CaptureAgentId"=aid RETURNING * INTO r;
 IF terminalize AND FOUND THEN UPDATE tagekyc.capture_runtime_installations SET "LifecycleState"=to_state,"Revision"="Revision"+1,"RevokedAtUtc"=CASE WHEN to_state='Revoked' THEN n ELSE "RevokedAtUtc" END,"RetiredAtUtc"=CASE WHEN to_state='Retired' THEN n ELSE "RetiredAtUtc" END,"LifecycleReason"=reason WHERE "DeviceInstallationId"=i."DeviceInstallationId"; IF g."CredentialId" IS NOT NULL THEN UPDATE tagekyc.capture_runtime_credential_generations SET "State"=to_state,"Revision"="Revision"+1,"RevokedAtUtc"=CASE WHEN to_state='Revoked' THEN n ELSE "RevokedAtUtc" END,"RetiredAtUtc"=CASE WHEN to_state='Retired' THEN n ELSE "RetiredAtUtc" END,"TerminalReason"=reason WHERE "CredentialId"=g."CredentialId" AND "Generation"=g."Generation";END IF;END IF;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(actor,kind,idem,fp,'Runtime',aid,'Applied',r."Revision",aid,n,n);INSERT INTO tagekyc.capture_runtime_management_events VALUES(gen_random_uuid(),actor,kind,idem,'Applied','Runtime',aid,expected,r."Revision",reason,n);RETURN QUERY SELECT 'Applied',aid,r."LifecycleState"::text,r."Revision",n;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeSuspend','Active','Suspended',false) $f$;
CREATE FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeReactivate','Suspended','Active',false) $f$;
CREATE FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeRevoke','Active','Revoked',true) $f$;
CREATE FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) RETURNS TABLE(result_code text,capture_agent_id uuid,state text,revision bigint,transitioned_at_utc timestamptz) LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog AS $f$ SELECT * FROM tagekyc.c6ba_transition_runtime_lifecycle($1,$2,$3,$4,$5,$6,$7,'RuntimeRetire','Revoked','Retired',true) $f$;
ALTER FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean) FROM PUBLIC;
ALTER FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;ALTER FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;ALTER FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;ALTER FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz),tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
""");
            migrationBuilder.Sql("""
CREATE FUNCTION tagekyc.c6ba_assert_current_operator(p_actor uuid,p_now timestamptz)
RETURNS void LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $f$
BEGIN
 IF NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_credentials WHERE "CredentialId"=p_actor AND "State"='Active' AND "IssuedAtUtc"<=p_now AND "ExpiresAtUtc">p_now AND "Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN RAISE EXCEPTION 'TIP88C1C6BA_OPERATOR_DENIED'; END IF;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_authorize_rotation(
 p_actor uuid,p_idem uuid,p_agent uuid,p_installation uuid,p_credential uuid,
 p_generation bigint,p_expected_revision bigint,p_expires timestamptz,
 p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,rotation_id uuid,capture_agent_id uuid,
 installation_id uuid,predecessor_credential_id uuid,current_generation bigint,
 state text,revision bigint,expires_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE; op record; new_rotation_id uuid;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_agent::text,10));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_installation::text,20));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential::text,30));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_idem::text,60));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations
  WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RotationAuthorize' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN
  IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::uuid,NULL::uuid,NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF;
  SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=op."ResultId";
  RETURN QUERY SELECT 'Replay',a."RotationAuthorizationId",a."CaptureAgentId",a."DeviceInstallationId",a."CredentialId",a."CurrentGeneration",'Active',1::bigint,a."ExpiresAtUtc"; RETURN;
 END IF;
 IF p_expires<=p_now OR p_expires>p_now+interval '10 minutes' OR octet_length(p_fingerprint)<>32 THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_INPUT_INVALID'; END IF;
 PERFORM 1 FROM tagekyc.capture_runtime_registrations r JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId" JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" WHERE r."CaptureAgentId"=p_agent AND i."DeviceInstallationId"=p_installation AND g."CredentialId"=p_credential AND g."Generation"=p_generation AND r."LifecycleState"='Active' AND i."LifecycleState"='Active' AND g."State"='Active' AND g."Revision"=p_expected_revision FOR UPDATE OF r,i,g;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_CONFLICT'; END IF;
 new_rotation_id:=pg_catalog.gen_random_uuid();
 a."RotationAuthorizationId":=new_rotation_id;a."CaptureAgentId":=p_agent;a."DeviceInstallationId":=p_installation;a."CredentialId":=p_credential;a."CurrentGeneration":=p_generation;a."AuthorizeOperationId":=p_idem;a."RequestFingerprint":=p_fingerprint;a."AuthorizedByCredentialId":=p_actor;a."AuthorizedAtUtc":=p_now;a."ExpiresAtUtc":=p_expires;a."State":='Active';a."Revision":=1;
 INSERT INTO tagekyc.capture_runtime_rotation_authorizations SELECT a.*;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RotationAuthorize',p_idem,p_fingerprint,'Rotation',new_rotation_id,'Applied',1,new_rotation_id,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RotationAuthorize',p_idem,'Applied','Rotation',new_rotation_id,NULL,1,NULL,p_now);
 RETURN QUERY SELECT 'Applied',new_rotation_id,p_agent,p_installation,p_credential,p_generation,'Active',1::bigint,p_expires;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_revoke_rotation(
 p_actor uuid,p_idem uuid,p_rotation uuid,p_expected bigint,p_reason text,
 p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,rotation_id uuid,state text,revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_rotation::text,60));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RotationRevoke' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::text,NULL::bigint,NULL::timestamptz; RETURN; END IF; SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation; RETURN QUERY SELECT 'Replay',p_rotation,a."State"::text,a."Revision",a."RevokedAtUtc"; RETURN; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation FOR UPDATE;
 IF NOT FOUND OR a."State"<>'Active' OR a."Revision"<>p_expected THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_CONFLICT'; END IF;
 UPDATE tagekyc.capture_runtime_rotation_authorizations SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"TerminalReason"=p_reason WHERE "RotationAuthorizationId"=p_rotation RETURNING * INTO a;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RotationRevoke',p_idem,p_fingerprint,'Rotation',p_rotation,'Applied',a."Revision",p_rotation,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RotationRevoke',p_idem,'Applied','Rotation',p_rotation,p_expected,a."Revision",p_reason,p_now);
 RETURN QUERY SELECT 'Applied',p_rotation,a."State"::text,a."Revision",a."RevokedAtUtc";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_complete_rotation(
 p_rotation uuid,p_credential uuid,p_predecessor_generation bigint,
 p_candidate_key uuid,p_business_idem uuid,p_spki bytea,p_thumbprint bytea,
 p_successor_proof bytea,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,generation bigint,
 candidate_key_id uuid,public_key_thumbprint bytea,credential_revision bigint,
 installation_revision bigint,rotation_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE; i tagekyc.capture_runtime_installations%ROWTYPE; op record; next_generation bigint;
BEGIN
 IF octet_length(p_spki)<>91 OR octet_length(p_thumbprint)<>32 OR octet_length(p_successor_proof)<>64 OR octet_length(p_fingerprint)<>32 THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_INPUT_INVALID'; END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_DENIED'; END IF;
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(a."CaptureAgentId"::text,10));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(a."DeviceInstallationId"::text,20));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential::text,30));
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_rotation::text,60));
 SELECT * INTO op FROM tagekyc.capture_runtime_rotation_completion_operations WHERE "RotationAuthorizationId"=p_rotation AND "BusinessIdempotencyKey"=p_business_idem;
 IF FOUND THEN
  IF op."RequestFingerprint"<>p_fingerprint OR op."CandidateKeyId"<>p_candidate_key THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::bigint,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::bigint; RETURN; END IF;
  SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation;
  IF op."ResultCode"='Expired' THEN RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::bigint,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::bigint; ELSE RETURN QUERY SELECT 'Replay',p_credential,op."SuccessorGeneration",p_candidate_key,a."SuccessorPublicKeyThumbprint",op."CredentialRevision",op."InstallationRevision",op."RotationRevision"; END IF; RETURN;
 END IF;
 SELECT * INTO a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation FOR UPDATE;
 IF FOUND AND a."State"='Active' AND a."ExpiresAtUtc"<=p_now THEN
  UPDATE tagekyc.capture_runtime_rotation_authorizations SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='RotationAuthorizationExpired' WHERE "RotationAuthorizationId"=p_rotation RETURNING * INTO a;
  INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES(p_rotation,p_business_idem,p_fingerprint,a."DeviceInstallationId",p_credential,p_predecessor_generation,NULL,p_candidate_key,'Expired',NULL,NULL,NULL,p_now,p_now);
  INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(pg_catalog.gen_random_uuid(),p_rotation,p_business_idem,'Expired',p_credential,p_predecessor_generation,NULL,p_now);
  RETURN QUERY SELECT 'TerminalizedExpiredAndDenied',NULL::uuid,NULL::bigint,NULL::uuid,NULL::bytea,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
 END IF;
 IF NOT FOUND OR a."State"<>'Active' OR a."CredentialId"<>p_credential OR a."CurrentGeneration"<>p_predecessor_generation THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_DENIED'; END IF;
 SELECT * INTO i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=a."DeviceInstallationId" AND "CurrentCredentialId"=p_credential AND "CurrentCredentialGeneration"=p_predecessor_generation AND "LifecycleState"='Active' FOR UPDATE;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROTATION_CONFLICT'; END IF;
 next_generation:=p_predecessor_generation+1;
 UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Rotated',"Revision"="Revision"+1,"RotatedAtUtc"=p_now,"TerminalReason"='RotationCompleted' WHERE "CredentialId"=p_credential AND "Generation"=p_predecessor_generation AND "State"='Active';
 INSERT INTO tagekyc.capture_runtime_credential_generations("CredentialId","Generation","DeviceInstallationId","CandidateKeyId","PublicVerifierSpki","Algorithm","PublicKeyThumbprint","RolePolicyId","RolePolicyRevision","ValidFromUtc","ValidUntilUtc","State","Revision") SELECT p_credential,next_generation,a."DeviceInstallationId",p_candidate_key,p_spki,'TAG-EKYC-CRT1-ECDSA-P256-SHA256',p_thumbprint,r."NextRolePolicyId",r."NextRolePolicyRevision",p_now,g."ValidUntilUtc",'Active',1 FROM tagekyc.capture_runtime_registrations r JOIN tagekyc.capture_runtime_credential_generations g ON g."CredentialId"=p_credential AND g."Generation"=p_predecessor_generation JOIN tagekyc.capture_runtime_role_policy_revisions rp ON rp."CatalogId"=r."NextRolePolicyId" AND rp."Revision"=r."NextRolePolicyRevision" WHERE r."CaptureAgentId"=a."CaptureAgentId" AND r."NextRolePolicyId" IS NOT NULL AND rp."EffectiveAtUtc"<=p_now;
 IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_NEXT_ROLE_POLICY_REQUIRED'; END IF;
 UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialGeneration"=next_generation,"Revision"="Revision"+1 WHERE "DeviceInstallationId"=a."DeviceInstallationId" RETURNING * INTO i;
 UPDATE tagekyc.capture_runtime_rotation_authorizations SET "State"='Completed',"Revision"="Revision"+1,"CandidateKeyId"=p_candidate_key,"SuccessorPublicVerifierSpki"=p_spki,"SuccessorPublicKeyThumbprint"=p_thumbprint,"SuccessorGeneration"=next_generation,"CompletedAtUtc"=p_now WHERE "RotationAuthorizationId"=p_rotation RETURNING * INTO a;
 INSERT INTO tagekyc.capture_runtime_rotation_completion_operations VALUES(p_rotation,p_business_idem,p_fingerprint,a."DeviceInstallationId",p_credential,p_predecessor_generation,next_generation,p_candidate_key,'Applied',1,i."Revision",a."Revision",p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_rotation_completion_events VALUES(pg_catalog.gen_random_uuid(),p_rotation,p_business_idem,'Completed',p_credential,p_predecessor_generation,next_generation,p_now);
RETURN QUERY SELECT 'Applied',p_credential,next_generation,p_candidate_key,p_thumbprint,1::bigint,i."Revision",a."Revision";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_replay_completed_rotation(
 p_rotation uuid,p_credential uuid,p_successor_generation bigint,p_candidate_key uuid,
 p_business_idem uuid,p_spki bytea,p_thumbprint bytea,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,generation bigint,candidate_key_id uuid,
 public_key_thumbprint bytea,credential_revision bigint,installation_revision bigint,rotation_revision bigint)
LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog AS $f$
BEGIN
 RETURN QUERY SELECT 'Replay',o."CredentialId",o."SuccessorGeneration",o."CandidateKeyId",a."SuccessorPublicKeyThumbprint",o."CredentialRevision",o."InstallationRevision",o."RotationRevision" FROM tagekyc.capture_runtime_rotation_completion_operations o JOIN tagekyc.capture_runtime_rotation_authorizations a ON a."RotationAuthorizationId"=o."RotationAuthorizationId" JOIN tagekyc.capture_runtime_credential_generations g ON g."CredentialId"=o."CredentialId" AND g."Generation"=o."SuccessorGeneration" WHERE o."RotationAuthorizationId"=p_rotation AND o."BusinessIdempotencyKey"=p_business_idem AND o."CredentialId"=p_credential AND o."SuccessorGeneration"=p_successor_generation AND o."CandidateKeyId"=p_candidate_key AND o."RequestFingerprint"=p_fingerprint AND a."SuccessorPublicVerifierSpki"=p_spki AND a."SuccessorPublicKeyThumbprint"=p_thumbprint AND a."State"='Completed' AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_publish_trust_profile(
 p_actor uuid,p_idem uuid,p_catalog uuid,p_expected bigint,p_effective timestamptz,
 p_expires timestamptz,p_runtime_type text,p_retained boolean,p_evidence boolean,
 p_handoff boolean,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE h bigint; op record; n bigint;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='TrustPublish' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_catalog,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_catalog,op."ResultRevision",op."ResultRevision"; RETURN; END IF;
 SELECT "HeadRevision" INTO h FROM tagekyc.capture_runtime_trust_profile_heads WHERE "CatalogId"=p_catalog FOR UPDATE; h:=COALESCE(h,0);
 IF h<>p_expected OR p_expires<=p_effective THEN RAISE EXCEPTION 'TIP88C1C6BA_TRUST_HEAD_CONFLICT'; END IF; n:=h+1;
 INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES(p_catalog,n,p_runtime_type,p_retained,p_evidence,p_handoff,p_effective,p_expires,p_actor,p_now);
 INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES(p_catalog,n,n,p_now) ON CONFLICT ("CatalogId") DO UPDATE SET "CurrentRevision"=n,"HeadRevision"=n,"UpdatedAtUtc"=p_now;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'TrustPublish',p_idem,p_fingerprint,'TrustProfile',p_catalog,'Applied',n,p_catalog,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'TrustPublish',p_idem,'Applied','TrustProfile',p_catalog,NULLIF(h,0),n,NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_catalog,n,n;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_publish_role_policy(
 p_actor uuid,p_idem uuid,p_catalog uuid,p_expected bigint,p_effective timestamptz,
 p_roles text[],p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE h bigint; op record; n bigint;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RolePublish' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_catalog,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_catalog,op."ResultRevision",op."ResultRevision"; RETURN; END IF;
 SELECT "HeadRevision" INTO h FROM tagekyc.capture_runtime_role_policy_heads WHERE "CatalogId"=p_catalog FOR UPDATE; h:=COALESCE(h,0); IF h<>p_expected THEN RAISE EXCEPTION 'TIP88C1C6BA_ROLE_HEAD_CONFLICT'; END IF; n:=h+1;
 INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES(p_catalog,n,p_roles,p_effective,p_actor,p_now);
 INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES(p_catalog,n,n,p_now) ON CONFLICT ("CatalogId") DO UPDATE SET "CurrentRevision"=n,"HeadRevision"=n,"UpdatedAtUtc"=p_now;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RolePublish',p_idem,p_fingerprint,'RolePolicy',p_catalog,'Applied',n,p_catalog,p_now,p_now);
 INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RolePublish',p_idem,'Applied','RolePolicy',p_catalog,NULLIF(h,0),n,NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_catalog,n,n;
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_assign_role_policy(p_actor uuid,p_idem uuid,p_agent uuid,p_catalog uuid,p_catalog_revision bigint,p_expected bigint,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,role_policy_id uuid,role_policy_revision bigint,runtime_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE r tagekyc.capture_runtime_registrations%ROWTYPE; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_agent::text,10)); PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='RoleAssign' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_agent,NULL::uuid,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_agent,p_catalog,p_catalog_revision,op."ResultRevision"; RETURN; END IF;
 PERFORM 1 FROM tagekyc.capture_runtime_role_policy_revisions WHERE "CatalogId"=p_catalog AND "Revision"=p_catalog_revision; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_ROLE_NOT_FOUND'; END IF;
 UPDATE tagekyc.capture_runtime_registrations SET "NextRolePolicyId"=p_catalog,"NextRolePolicyRevision"=p_catalog_revision,"Revision"="Revision"+1 WHERE "CaptureAgentId"=p_agent AND "Revision"=p_expected RETURNING * INTO r; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_RUNTIME_CONFLICT'; END IF;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'RoleAssign',p_idem,p_fingerprint,'Runtime',p_agent,'Applied',r."Revision",p_agent,p_now,p_now); INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'RoleAssign',p_idem,'Applied','Runtime',p_agent,p_expected,r."Revision",NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_agent,p_catalog,p_catalog_revision,r."Revision";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_assign_configuration(p_actor uuid,p_idem uuid,p_agent uuid,p_catalog uuid,p_catalog_revision bigint,p_expected bigint,p_override uuid,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,configuration_id uuid,configuration_revision bigint,override_id uuid,runtime_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE r tagekyc.capture_runtime_registrations%ROWTYPE; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_agent::text,10)); PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40));
 SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='ConfigurationAssign' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_agent,NULL::uuid,NULL::bigint,NULL::uuid,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_agent,p_catalog,p_catalog_revision,p_override,op."ResultRevision"; RETURN; END IF;
 PERFORM 1 FROM tagekyc.capture_runtime_configuration_revisions WHERE "CatalogId"=p_catalog AND "Revision"=p_catalog_revision; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_CONFIGURATION_NOT_FOUND'; END IF;
 IF p_override IS NOT NULL THEN PERFORM 1 FROM tagekyc.capture_runtime_configuration_overrides WHERE "ConfigurationOverrideId"=p_override AND "CaptureAgentId"=p_agent AND "BaseConfigurationId"=p_catalog AND "BaseConfigurationRevision"=p_catalog_revision; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_OVERRIDE_INVALID'; END IF; END IF;
 UPDATE tagekyc.capture_runtime_registrations SET "ConfigurationId"=p_catalog,"ConfigurationRevision"=p_catalog_revision,"ConfigurationOverrideId"=p_override,"Revision"="Revision"+1 WHERE "CaptureAgentId"=p_agent AND "Revision"=p_expected RETURNING * INTO r; IF NOT FOUND THEN RAISE EXCEPTION 'TIP88C1C6BA_RUNTIME_CONFLICT'; END IF;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'ConfigurationAssign',p_idem,p_fingerprint,'Runtime',p_agent,'Applied',r."Revision",p_agent,p_now,p_now); INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'ConfigurationAssign',p_idem,'Applied','Runtime',p_agent,p_expected,r."Revision",NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_agent,p_catalog,p_catalog_revision,p_override,r."Revision";
END $f$;

CREATE FUNCTION tagekyc.capture_runtime_publish_configuration(p_actor uuid,p_idem uuid,p_catalog uuid,p_expected bigint,p_effective timestamptz,p_expires timestamptz,p_enabled boolean,p_budget integer,p_margin integer,p_poll integer,p_dg2 integer,p_selfie integer,p_capture_host bigint,p_custody_stream integer,p_custody_deployment bigint,p_pre_admission integer,p_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,catalog_id uuid,revision bigint,head_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog AS $f$
DECLARE h bigint; n bigint; op record;
BEGIN
 PERFORM tagekyc.c6ba_assert_current_operator(p_actor,p_now);
 PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_catalog::text,40)); SELECT * INTO op FROM tagekyc.capture_runtime_management_operations WHERE "ActorCredentialId"=p_actor AND "OperationKind"='ConfigurationPublish' AND "IdempotencyKey"=p_idem;
 IF FOUND THEN IF op."RequestFingerprint"<>p_fingerprint THEN RETURN QUERY SELECT 'Conflict',p_catalog,NULL::bigint,NULL::bigint; RETURN; END IF; RETURN QUERY SELECT 'Replay',p_catalog,op."ResultRevision",op."ResultRevision"; RETURN; END IF;
 SELECT "HeadRevision" INTO h FROM tagekyc.capture_runtime_configuration_heads WHERE "CatalogId"=p_catalog FOR UPDATE; h:=COALESCE(h,0); IF h<>p_expected THEN RAISE EXCEPTION 'TIP88C1C6BA_CONFIGURATION_HEAD_CONFLICT'; END IF; n:=h+1;
 INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES(p_catalog,n,p_effective,p_expires,p_enabled,p_budget,p_margin,p_poll,p_dg2,p_selfie,p_capture_host,p_custody_stream,p_custody_deployment,p_pre_admission,p_actor,p_now);
 INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES(p_catalog,n,n,p_now) ON CONFLICT ("CatalogId") DO UPDATE SET "CurrentRevision"=n,"HeadRevision"=n,"UpdatedAtUtc"=p_now;
 INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'ConfigurationPublish',p_idem,p_fingerprint,'Configuration',p_catalog,'Applied',n,p_catalog,p_now,p_now); INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'ConfigurationPublish',p_idem,'Applied','Configuration',p_catalog,NULLIF(h,0),n,NULL,p_now);
 RETURN QUERY SELECT 'Applied',p_catalog,n,n;
END $f$;

ALTER FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.c6ba_assert_current_operator(uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.c6ba_assert_current_operator(uuid,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_operator,tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator,tagekyc_runtime;
ALTER FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
ALTER FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) FROM PUBLIC;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) TO tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) TO tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
""");
            migrationBuilder.Sql("""
CREATE FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(
    p_client_application_id uuid,
    p_verification_session_id uuid,
    p_action text,
    p_current_capability_id uuid,
    p_expected_revision bigint,
    p_idempotency_key uuid,
    p_new_capability_id uuid,
    p_key_lookup_prefix text,
    p_secret_digest bytea,
    p_verifier_pepper_version integer,
    p_request_fingerprint bytea,
    p_now timestamptz)
RETURNS TABLE(result_code text, capture_capability_id uuid,
    secret_available boolean, expires_at_utc timestamptz, state text,
    revision bigint)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog
AS $function$
DECLARE
    v_session tagekyc.verification_sessions%ROWTYPE;
    v_current tagekyc.capture_capabilities%ROWTYPE;
    v_operation tagekyc.capture_capability_operations%ROWTYPE;
    v_expiry timestamptz := p_now + interval '5 minutes';
    v_capability_lock_a bigint;
    v_capability_lock_b bigint;
    v_expiry_operation_id uuid;
BEGIN
    IF p_action NOT IN ('Issue','Replace')
       OR p_client_application_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_verification_session_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_idempotency_key = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_new_capability_id = '00000000-0000-0000-0000-000000000000'::uuid
       OR p_key_lookup_prefix !~ '^[A-Za-z0-9_-]{12}$'
       OR pg_catalog.octet_length(p_secret_digest) <> 32
       OR p_verifier_pepper_version <= 0
       OR pg_catalog.octet_length(p_request_fingerprint) <> 32
       OR (p_action='Issue' AND (p_current_capability_id IS NOT NULL OR p_expected_revision IS NOT NULL))
       OR (p_action='Replace' AND (p_current_capability_id IS NULL OR p_expected_revision IS NULL OR p_expected_revision <= 0)) THEN
        RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;

    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
    v_capability_lock_a:=pg_catalog.hashtextextended(COALESCE(p_current_capability_id,p_new_capability_id)::text,80);
    v_capability_lock_b:=pg_catalog.hashtextextended(p_new_capability_id::text,80);
    PERFORM pg_catalog.pg_advisory_xact_lock(LEAST(v_capability_lock_a,v_capability_lock_b));
    IF v_capability_lock_a<>v_capability_lock_b THEN
      PERFORM pg_catalog.pg_advisory_xact_lock(GREATEST(v_capability_lock_a,v_capability_lock_b));
    END IF;

    SELECT * INTO v_operation
    FROM tagekyc.capture_capability_operations
    WHERE "ClientApplicationId"=p_client_application_id
      AND "VerificationSessionId"=p_verification_session_id
      AND "OperationKind"=p_action
      AND "IdempotencyKey"=p_idempotency_key;
    IF FOUND THEN
        IF v_operation."RequestFingerprint" <> p_request_fingerprint THEN
            RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        ELSE
            RETURN QUERY
            SELECT CASE WHEN v_operation."ResultCode"='Expired' THEN 'TERMINALIZED_EXPIRED_AND_DENIED' ELSE 'EXISTING_MATCH_SECRET_UNAVAILABLE' END,c."CaptureCapabilityId",false,
                   c."ExpiresAtUtc",CASE WHEN v_operation."ResultCode"='Expired' THEN 'Expired' ELSE 'ActiveUnbound' END,v_operation."ResultRevision"
            FROM tagekyc.capture_capabilities c
            WHERE c."CaptureCapabilityId"=v_operation."ResultCapabilityId";
        END IF;
        RETURN;
    END IF;

    SELECT * INTO v_session FROM tagekyc.verification_sessions
    WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id
    FOR UPDATE;
    IF NOT FOUND THEN
        RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;
    IF v_session."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal')
       OR v_session."ExpiresAt" <= p_now
       OR v_session."BindingNonceHash" IS NULL THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;
    IF EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings b
               WHERE b."VerificationSessionId"=p_verification_session_id) THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;

    SELECT * INTO v_current FROM tagekyc.capture_capabilities
    WHERE "VerificationSessionId"=p_verification_session_id
      AND "ClientApplicationId"=p_client_application_id
      AND "State" IN ('ActiveUnbound','Bound')
    ORDER BY "CaptureCapabilityId" LIMIT 1 FOR UPDATE;
    IF FOUND AND v_current."ExpiresAtUtc"<=p_now THEN
      IF p_action='Replace' THEN
        IF v_current."CaptureCapabilityId"<>p_current_capability_id OR v_current."Revision"<>p_expected_revision THEN
          RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint; RETURN;
        END IF;
        UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_current."CaptureCapabilityId";
        INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Replace',p_idempotency_key,p_request_fingerprint,'Expired',v_current."CaptureCapabilityId",v_current."Revision"+1,p_now,p_now);
        INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Replace',p_idempotency_key,v_current."CaptureCapabilityId",'Expired',v_current."Revision",v_current."Revision"+1,p_now);
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',v_current."CaptureCapabilityId",false,v_current."ExpiresAtUtc",'Expired',v_current."Revision"+1; RETURN;
      END IF;
      v_expiry_operation_id:=pg_catalog.gen_random_uuid();
      UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_current."CaptureCapabilityId";
      INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,tagekyc_extensions.digest(pg_catalog.convert_to('tip-88c1-c6b-a1-capability-expiry-v1','UTF8')||pg_catalog.uuid_send(p_verification_session_id)||pg_catalog.uuid_send(v_current."CaptureCapabilityId")||pg_catalog.timestamptz_send(v_current."ExpiresAtUtc")||pg_catalog.uuid_send(v_expiry_operation_id),'sha256'),'Applied',v_current."CaptureCapabilityId",v_current."Revision"+1,p_now,p_now);
      INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,v_current."CaptureCapabilityId",'Expired',v_current."Revision",v_current."Revision"+1,p_now);
    END IF;

    IF p_action='Replace' THEN
        SELECT * INTO v_current FROM tagekyc.capture_capabilities
        WHERE "CaptureCapabilityId"=p_current_capability_id
          AND "VerificationSessionId"=p_verification_session_id
          AND "ClientApplicationId"=p_client_application_id
        FOR UPDATE;
        IF NOT FOUND OR v_current."State"<>'ActiveUnbound'
           OR v_current."Revision"<>p_expected_revision THEN
            RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
            RETURN;
        END IF;
        UPDATE tagekyc.capture_capabilities
        SET "State"='Revoked',"Revision"="Revision"+1,
            "SuccessorCapabilityId"=p_new_capability_id,"RevokedAtUtc"=p_now,
            "TerminalReason"='ClientReplacement'
        WHERE "CaptureCapabilityId"=p_current_capability_id;
    ELSIF EXISTS (SELECT 1 FROM tagekyc.capture_capabilities c
                  WHERE c."VerificationSessionId"=p_verification_session_id
                    AND c."State" IN ('ActiveUnbound','Bound')) THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,false,NULL::timestamptz,NULL::text,NULL::bigint;
        RETURN;
    END IF;

    INSERT INTO tagekyc.capture_capabilities(
        "CaptureCapabilityId","VerificationSessionId","ClientApplicationId",
        "KeyLookupPrefix","SecretDigest","VerifierPepperVersion","Audience",
        "Challenge","IssuedAtUtc","ExpiresAtUtc","State","Revision",
        "PredecessorCapabilityId")
    VALUES (p_new_capability_id,p_verification_session_id,p_client_application_id,
        p_key_lookup_prefix,p_secret_digest,p_verifier_pepper_version,
        'ManagedCaptureRuntime',v_session."BindingNonceHash",p_now,v_expiry,
        'ActiveUnbound',1,p_current_capability_id);

    INSERT INTO tagekyc.capture_capability_operations(
        "ClientApplicationId","VerificationSessionId","OperationKind",
        "IdempotencyKey","RequestFingerprint","ResultCode",
        "ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc")
    VALUES (p_client_application_id,p_verification_session_id,p_action,
        p_idempotency_key,p_request_fingerprint,'Applied',p_new_capability_id,1,p_now,p_now);
    INSERT INTO tagekyc.capture_capability_events(
        "EventId","ClientApplicationId","VerificationSessionId","OperationKind",
        "IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision",
        "AfterRevision","RecordedAtUtc")
    VALUES (pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,
        p_action,p_idempotency_key,p_new_capability_id,
        CASE WHEN p_action='Issue' THEN 'Issued' ELSE 'Replaced' END,
        CASE WHEN p_action='Issue' THEN NULL ELSE p_expected_revision END,1,p_now);
    RETURN QUERY SELECT 'CREATED',p_new_capability_id,true,v_expiry,'ActiveUnbound',1::bigint;
END
$function$;

ALTER FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) TO tagekyc_capture_runtime_application,tagekyc_runtime;

CREATE FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(p_capture_capability_id uuid)
RETURNS TABLE(secret_digest bytea,verifier_pepper_version integer)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT c."SecretDigest",c."VerifierPepperVersion"
    FROM tagekyc.capture_capabilities c
    WHERE c."CaptureCapabilityId"=p_capture_capability_id;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_bind_capability(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_credential_generation bigint,p_capture_capability_id uuid,
    p_capability_secret_verified boolean,p_bind_operation_id uuid,
    p_request_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,binding_id uuid,execution_expires_at_utc timestamptz,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,capability_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE
    v_r tagekyc.capture_runtime_registrations%ROWTYPE;
    v_i tagekyc.capture_runtime_installations%ROWTYPE;
    v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
    v_c tagekyc.capture_capabilities%ROWTYPE;
    v_s tagekyc.verification_sessions%ROWTYPE;
    v_o tagekyc.capture_capability_operations%ROWTYPE;
    v_binding_id uuid;
    v_horizon timestamptz;
BEGIN
    IF NOT COALESCE(p_capability_secret_verified,false)
       OR p_credential_generation<=0 OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN
        RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;
        RETURN;
    END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_device_installation_id::text,20));
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text||':'||p_credential_generation::text,30));
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id;
    IF NOT FOUND THEN RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN; END IF;
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_c."VerificationSessionId"::text,70));
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_capability_id::text,80));

    SELECT * INTO v_o FROM tagekyc.capture_capability_operations
    WHERE "ClientApplicationId"=v_c."ClientApplicationId" AND "VerificationSessionId"=v_c."VerificationSessionId"
      AND "OperationKind"='Bind' AND "IdempotencyKey"=p_bind_operation_id;
    IF FOUND THEN
      IF v_o."RequestFingerprint"<>p_request_fingerprint OR v_o."RuntimeCaptureAgentId"<>p_capture_agent_id
         OR v_o."RuntimeInstallationId"<>p_device_installation_id OR v_o."RuntimeCredentialId"<>p_credential_id
         OR v_o."RuntimeCredentialGeneration"<>p_credential_generation THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint;
      ELSIF v_o."ResultCode"='Expired' THEN
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,v_o."ResultRevision";
      ELSE
        RETURN QUERY SELECT 'AVAILABLE',b."CaptureExecutionBindingId",b."ExecutionExpiresAtUtc",b."RuntimeRevision",b."InstallationRevision",b."CredentialRevision",v_o."ResultRevision"
        FROM tagekyc.capture_execution_bindings b
        WHERE b."CaptureExecutionBindingId"=v_o."ResultBindingId";
      END IF;
      RETURN;
    END IF;

    SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
    SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
    SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_credential_generation FOR UPDATE;
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id FOR UPDATE;
    SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=v_c."VerificationSessionId" AND "ClientApplicationId"=v_c."ClientApplicationId" FOR UPDATE;
    IF v_c."State" IN ('ActiveUnbound','Bound') AND v_c."ExpiresAtUtc"<=p_now THEN
        UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=p_capture_capability_id;
        INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","RuntimeCaptureAgentId","RuntimeInstallationId","RuntimeCredentialId","RuntimeCredentialGeneration","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_request_fingerprint,p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,'Expired',p_capture_capability_id,v_c."Revision"+1,p_now,p_now);
        INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","RuntimeCaptureAgentId","RuntimeInstallationId","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_capture_capability_id,'Expired',p_capture_agent_id,p_device_installation_id,v_c."Revision",v_c."Revision"+1,p_now);
        RETURN QUERY SELECT 'TERMINALIZED_EXPIRED_AND_DENIED',NULL::uuid,NULL::timestamptz,v_r."Revision",v_i."Revision",v_g."Revision",v_c."Revision"+1; RETURN;
    END IF;
    IF v_r."LifecycleState" IS DISTINCT FROM 'Active' OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
       OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_credential_generation
       OR v_g."State" IS DISTINCT FROM 'Active' OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
       OR v_c."State" IS DISTINCT FROM 'ActiveUnbound'
       OR v_s."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal') OR v_s."ExpiresAt"<=p_now
       OR v_c."Challenge" IS DISTINCT FROM v_s."BindingNonceHash"
       OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision" AND rp."EffectiveAtUtc"<=p_now AND 'Bind'=ANY(rp."Roles"))
       OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp WHERE tp."CatalogId"=v_r."TrustProfileId" AND tp."Revision"=v_r."TrustProfileRevision" AND tp."EffectiveAtUtc"<=p_now AND tp."ExpiresAtUtc">p_now)
       OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions cp WHERE cp."CatalogId"=v_r."ConfigurationId" AND cp."Revision"=v_r."ConfigurationRevision" AND cp."EffectiveAtUtc"<=p_now AND cp."ExpiresAtUtc">p_now) THEN
        RETURN QUERY SELECT 'ACCESS_DENIED',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
    END IF;
    IF EXISTS (SELECT 1 FROM tagekyc.capture_execution_bindings b WHERE b."VerificationSessionId"=v_c."VerificationSessionId" OR b."CaptureCapabilityId"=p_capture_capability_id) THEN
        RETURN QUERY SELECT 'CONFLICT',NULL::uuid,NULL::timestamptz,NULL::bigint,NULL::bigint,NULL::bigint,NULL::bigint; RETURN;
    END IF;
    v_binding_id:=pg_catalog.gen_random_uuid();
    v_horizon:=LEAST(v_c."ExpiresAtUtc",v_s."ExpiresAt",p_now+interval '30 minutes');
    INSERT INTO tagekyc.capture_execution_bindings VALUES(v_binding_id,v_c."VerificationSessionId",p_capture_capability_id,v_c."ClientApplicationId",p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,v_g."PublicKeyThumbprint",v_r."Revision",v_i."Revision",v_g."Revision",v_r."TrustProfileId",v_r."TrustProfileRevision",v_g."RolePolicyId",v_g."RolePolicyRevision",v_r."ConfigurationId",v_r."ConfigurationRevision",v_c."Challenge",p_bind_operation_id,p_now,v_horizon);
    UPDATE tagekyc.capture_capabilities SET "State"='Bound',"Revision"="Revision"+1,"BoundAtUtc"=p_now WHERE "CaptureCapabilityId"=p_capture_capability_id;
    INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","RuntimeCaptureAgentId","RuntimeInstallationId","RuntimeCredentialId","RuntimeCredentialGeneration","ResultCode","ResultCapabilityId","ResultBindingId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_request_fingerprint,p_capture_agent_id,p_device_installation_id,p_credential_id,p_credential_generation,'Applied',p_capture_capability_id,v_binding_id,v_c."Revision"+1,p_now,p_now);
    INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","RuntimeCaptureAgentId","RuntimeInstallationId","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",v_c."VerificationSessionId",'Bind',p_bind_operation_id,p_capture_capability_id,'Bound',p_capture_agent_id,p_device_installation_id,v_c."Revision",v_c."Revision"+1,p_now);
    RETURN QUERY SELECT 'CREATED',v_binding_id,v_horizon,v_r."Revision",v_i."Revision",v_g."Revision",v_c."Revision"+1;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_reconcile_binding(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_credential_generation bigint,p_capture_capability_id uuid,
    p_bind_operation_id uuid,p_now timestamptz)
RETURNS TABLE(result_code text,binding_id uuid,execution_expires_at_utc timestamptz,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,capability_revision bigint)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path=pg_catalog
AS $function$
    SELECT CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN 'AVAILABLE' ELSE 'RESOURCE_NOT_AVAILABLE' END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."CaptureExecutionBindingId" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."ExecutionExpiresAtUtc" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."RuntimeRevision" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."InstallationRevision" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN b."CredentialRevision" END,
           CASE WHEN b."ExecutionExpiresAtUtc">p_now THEN c."Revision" END
    FROM tagekyc.capture_execution_bindings b
    JOIN tagekyc.capture_capabilities c ON c."CaptureCapabilityId"=b."CaptureCapabilityId"
    JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=b."CaptureAgentId"
    JOIN tagekyc.capture_runtime_installations i ON i."DeviceInstallationId"=b."DeviceInstallationId" AND i."CaptureAgentId"=b."CaptureAgentId"
    JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=b."DeviceInstallationId" AND g."CredentialId"=b."CredentialId" AND g."Generation"=b."CredentialGeneration"
    WHERE b."CaptureAgentId"=p_capture_agent_id
      AND b."DeviceInstallationId"=p_device_installation_id
      AND b."CredentialId"=p_credential_id
      AND b."CredentialGeneration"=p_credential_generation
      AND b."CaptureCapabilityId"=p_capture_capability_id
      AND b."BindOperationId"=p_bind_operation_id
      AND r."LifecycleState"='Active' AND r."Revision"=b."RuntimeRevision"
      AND i."LifecycleState"='Active' AND i."Revision"=b."InstallationRevision"
      AND i."CurrentCredentialId"=b."CredentialId" AND i."CurrentCredentialGeneration"=b."CredentialGeneration"
      AND g."State"='Active' AND g."Revision"=b."CredentialRevision"
      AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
      AND c."State"='Bound'
    UNION ALL
    SELECT 'RESOURCE_NOT_AVAILABLE',NULL,NULL,NULL,NULL,NULL,NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM tagekyc.capture_execution_bindings b
        JOIN tagekyc.capture_capabilities c ON c."CaptureCapabilityId"=b."CaptureCapabilityId"
        JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=b."CaptureAgentId"
        JOIN tagekyc.capture_runtime_installations i ON i."DeviceInstallationId"=b."DeviceInstallationId" AND i."CaptureAgentId"=b."CaptureAgentId"
        JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=b."DeviceInstallationId" AND g."CredentialId"=b."CredentialId" AND g."Generation"=b."CredentialGeneration"
        WHERE b."CaptureAgentId"=p_capture_agent_id
          AND b."DeviceInstallationId"=p_device_installation_id
          AND b."CredentialId"=p_credential_id
          AND b."CredentialGeneration"=p_credential_generation
          AND b."CaptureCapabilityId"=p_capture_capability_id
          AND b."BindOperationId"=p_bind_operation_id
          AND r."LifecycleState"='Active' AND r."Revision"=b."RuntimeRevision"
          AND i."LifecycleState"='Active' AND i."Revision"=b."InstallationRevision"
          AND i."CurrentCredentialId"=b."CredentialId" AND i."CurrentCredentialGeneration"=b."CredentialGeneration"
          AND g."State"='Active' AND g."Revision"=b."CredentialRevision"
          AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
          AND c."State"='Bound');
$function$;

ALTER FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(
    p_verification_session_id uuid,p_capture_capability_id uuid,
    p_now timestamptz,p_deterministic_operation_id uuid)
RETURNS TABLE(result_code text,state text,revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_c tagekyc.capture_capabilities%ROWTYPE; v_o tagekyc.capture_capability_operations%ROWTYPE; v_fingerprint bytea;
BEGIN
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_capability_id::text,80));
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=p_capture_capability_id AND "VerificationSessionId"=p_verification_session_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::text,NULL::bigint; RETURN; END IF;
  v_fingerprint:=tagekyc_extensions.digest(
    pg_catalog.convert_to('tip-88c1-c6b-a1-capability-expiry-v1','UTF8')
    ||pg_catalog.uuid_send(p_verification_session_id)
    ||pg_catalog.uuid_send(p_capture_capability_id)
    ||pg_catalog.timestamptz_send(v_c."ExpiresAtUtc")
    ||pg_catalog.uuid_send(p_deterministic_operation_id),'sha256');
  SELECT * INTO v_o FROM tagekyc.capture_capability_operations WHERE "ClientApplicationId"=v_c."ClientApplicationId" AND "VerificationSessionId"=p_verification_session_id AND "OperationKind"='Expire' AND "IdempotencyKey"=p_deterministic_operation_id;
  IF FOUND THEN
    IF v_o."RequestFingerprint" IS DISTINCT FROM v_fingerprint OR v_o."ResultCapabilityId" IS DISTINCT FROM p_capture_capability_id THEN
      RETURN QUERY SELECT 'CONFLICT',NULL::text,NULL::bigint;
    ELSE
      RETURN QUERY SELECT 'AVAILABLE',v_c."State"::text,v_c."Revision";
    END IF;
    RETURN;
  END IF;
  IF v_c."State"<>'ActiveUnbound' OR v_c."ExpiresAtUtc">p_now THEN RETURN QUERY SELECT 'CONFLICT',v_c."State"::text,v_c."Revision"; RETURN; END IF;
  UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=p_capture_capability_id;
  INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(v_c."ClientApplicationId",p_verification_session_id,'Expire',p_deterministic_operation_id,v_fingerprint,'Applied',p_capture_capability_id,v_c."Revision"+1,p_now,p_now);
  INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),v_c."ClientApplicationId",p_verification_session_id,'Expire',p_deterministic_operation_id,p_capture_capability_id,'Expired',v_c."Revision",v_c."Revision"+1,p_now);
  RETURN QUERY SELECT 'EXPIRED','Expired',v_c."Revision"+1;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_resolve_configuration(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_credential_generation bigint,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,configuration_id uuid,
    configuration_revision bigint,effective_at_utc timestamptz,
    expires_at_utc timestamptz,raw_export_enabled boolean,
    plaintext_budget_seconds integer,
    raw_export_source_claim_safety_margin_milliseconds integer,
    capture_agent_configuration_polling_interval_seconds integer,
    raw_export_source_maximum_chip_dg2_portrait_bytes integer,
    raw_export_source_maximum_live_selfie_image_bytes integer,
    raw_export_capture_maximum_aggregate_plaintext_bytes_per_host bigint,
    raw_export_custody_maximum_plaintext_window_bytes_per_stream integer,
    raw_export_custody_max_aggregate_bytes_per_deployment bigint,
    raw_export_ingress_maximum_pre_admission_buffered_bytes integer)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT 'AVAILABLE',r."CaptureAgentId",c."CatalogId",c."Revision",
           c."EffectiveAtUtc",c."ExpiresAtUtc",
           COALESCE(o."RawExportEnabled",c."RawExportEnabled"),
           LEAST(c."PlaintextBudgetSeconds",COALESCE(o."PlaintextBudgetSeconds",c."PlaintextBudgetSeconds")),
           GREATEST(c."RawExportSourceClaimSafetyMarginMilliseconds",COALESCE(o."RawExportSourceClaimSafetyMarginMilliseconds",c."RawExportSourceClaimSafetyMarginMilliseconds")),
           LEAST(c."CaptureAgentConfigurationPollingIntervalSeconds",COALESCE(o."CaptureAgentConfigurationPollingIntervalSeconds",c."CaptureAgentConfigurationPollingIntervalSeconds")),
           LEAST(c."RawExportSourceMaximumChipDg2PortraitBytes",COALESCE(o."RawExportSourceMaximumChipDg2PortraitBytes",c."RawExportSourceMaximumChipDg2PortraitBytes")),
           LEAST(c."RawExportSourceMaximumLiveSelfieImageBytes",COALESCE(o."RawExportSourceMaximumLiveSelfieImageBytes",c."RawExportSourceMaximumLiveSelfieImageBytes")),
           LEAST(c."RawExportCaptureMaximumAggregatePlaintextBytesPerHost",COALESCE(o."RawExportCaptureMaximumAggregatePlaintextBytesPerHost",c."RawExportCaptureMaximumAggregatePlaintextBytesPerHost")),
           LEAST(c."RawExportCustodyMaximumPlaintextWindowBytesPerStream",COALESCE(o."RawExportCustodyMaximumPlaintextWindowBytesPerStream",c."RawExportCustodyMaximumPlaintextWindowBytesPerStream")),
           LEAST(c."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment",COALESCE(o."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment",c."RawExportCustodyMaxAggregatePlaintextWindowBytesPerDeployment")),
           LEAST(c."RawExportIngressMaximumPreAdmissionBufferedBytes",COALESCE(o."RawExportIngressMaximumPreAdmissionBufferedBytes",c."RawExportIngressMaximumPreAdmissionBufferedBytes"))
    FROM tagekyc.capture_runtime_registrations r
    JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId"
    JOIN tagekyc.capture_runtime_credential_generations g
      ON g."DeviceInstallationId"=i."DeviceInstallationId"
     AND g."CredentialId"=i."CurrentCredentialId"
     AND g."Generation"=i."CurrentCredentialGeneration"
    JOIN tagekyc.capture_runtime_configuration_revisions c
      ON c."CatalogId"=r."ConfigurationId" AND c."Revision"=r."ConfigurationRevision"
    LEFT JOIN tagekyc.capture_runtime_configuration_overrides o
      ON o."ConfigurationOverrideId"=r."ConfigurationOverrideId"
     AND o."CaptureAgentId"=r."CaptureAgentId"
     AND o."BaseConfigurationId"=c."CatalogId"
     AND o."BaseConfigurationRevision"=c."Revision"
    WHERE r."CaptureAgentId"=p_capture_agent_id AND r."LifecycleState"='Active'
      AND i."DeviceInstallationId"=p_device_installation_id AND i."LifecycleState"='Active'
      AND g."CredentialId"=p_credential_id AND g."Generation"=p_credential_generation
      AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
      AND c."EffectiveAtUtc"<=p_now AND c."ExpiresAtUtc">p_now
    UNION ALL SELECT 'RESOURCE_NOT_AVAILABLE',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL
    WHERE NOT EXISTS (
      SELECT 1 FROM tagekyc.capture_runtime_registrations r
      JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId"
      JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" AND g."CredentialId"=i."CurrentCredentialId" AND g."Generation"=i."CurrentCredentialGeneration"
      JOIN tagekyc.capture_runtime_configuration_revisions c ON c."CatalogId"=r."ConfigurationId" AND c."Revision"=r."ConfigurationRevision"
      WHERE r."CaptureAgentId"=p_capture_agent_id AND r."LifecycleState"='Active'
        AND i."DeviceInstallationId"=p_device_installation_id AND i."LifecycleState"='Active'
        AND g."CredentialId"=p_credential_id AND g."Generation"=p_credential_generation
        AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
        AND c."EffectiveAtUtc"<=p_now AND c."ExpiresAtUtc">p_now);
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_read_readiness(p_capture_agent_id uuid,p_now timestamptz)
RETURNS TABLE(result_code text,capture_agent_id uuid,runtime_state text,
    runtime_revision bigint,installation_state text,installation_revision bigint,
    credential_state text,credential_generation bigint,credential_revision bigint,
    trust_profile_revision bigint,role_policy_revision bigint,
    configuration_revision bigint,required_pepper_versions integer[],
    nonce_store_ready boolean,cutover_sentinel_ready boolean,database_ready boolean)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT 'AVAILABLE',r."CaptureAgentId",r."LifecycleState",r."Revision",
           i."LifecycleState",i."Revision",g."State",g."Generation",g."Revision",
           r."TrustProfileRevision",g."RolePolicyRevision",r."ConfigurationRevision",
           ARRAY(SELECT DISTINCT x.v FROM (
             SELECT p."VerifierPepperVersion" v FROM tagekyc.platform_operator_credentials p WHERE p."State"='Active' AND p."ExpiresAtUtc">p_now
             UNION SELECT b."VerifierPepperVersion" FROM tagekyc.capture_runtime_bootstrap_issuances b WHERE b."State"='Active' AND b."ExpiresAtUtc">p_now
             UNION SELECT c."VerifierPepperVersion" FROM tagekyc.capture_capabilities c WHERE c."State" IN ('ActiveUnbound','Bound') AND c."ExpiresAtUtc">p_now
           ) x ORDER BY x.v),
           (SELECT pg_catalog.count(*)<=1000000 AND COALESCE(pg_catalog.max(p_now-n."PurgeAfterUtc") FILTER (WHERE n."PurgeAfterUtc"<p_now),interval '0')<=interval '900 seconds' FROM tagekyc.capture_runtime_request_nonces n),
           EXISTS (SELECT 1 FROM tagekyc.capture_runtime_cutover_state s WHERE s."Profile"='Managed' AND s."State" IN ('Prepared','Activated')),
           r."LifecycleState"='Active' AND i."LifecycleState"='Active' AND g."State"='Active'
             AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now
             AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp WHERE tp."CatalogId"=r."TrustProfileId" AND tp."Revision"=r."TrustProfileRevision" AND tp."EffectiveAtUtc"<=p_now AND tp."ExpiresAtUtc">p_now)
             AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp WHERE rp."CatalogId"=g."RolePolicyId" AND rp."Revision"=g."RolePolicyRevision" AND rp."EffectiveAtUtc"<=p_now)
             AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions cp WHERE cp."CatalogId"=r."ConfigurationId" AND cp."Revision"=r."ConfigurationRevision" AND cp."EffectiveAtUtc"<=p_now AND cp."ExpiresAtUtc">p_now)
    FROM tagekyc.capture_runtime_registrations r
    LEFT JOIN tagekyc.capture_runtime_installations i ON i."CaptureAgentId"=r."CaptureAgentId" AND i."LifecycleState"='Active' AND i."CurrentCredentialId" IS NOT NULL
    LEFT JOIN tagekyc.capture_runtime_credential_generations g ON g."DeviceInstallationId"=i."DeviceInstallationId" AND g."CredentialId"=i."CurrentCredentialId" AND g."Generation"=i."CurrentCredentialGeneration"
    WHERE r."CaptureAgentId"=p_capture_agent_id
    UNION ALL SELECT 'RESOURCE_NOT_AVAILABLE',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,ARRAY[]::integer[],false,false,false
    WHERE NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_registrations r WHERE r."CaptureAgentId"=p_capture_agent_id);
$function$;
ALTER FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) TO tagekyc_capture_runtime_operator;

-- R26 writes the existing audit in its SECURITY DEFINER transaction. Grant only
-- the inserted columns to the existing NOLOGIN owner, never to an online role.
GRANT INSERT ("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") ON tagekyc.audit_events TO tagekyc_raw_export_deployer;

CREATE FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(
    p_client_application_id uuid,p_verification_session_id uuid,p_reason text,
    p_request_id text,p_correlation_id text,p_now timestamptz,p_actor_key_prefix text,
    p_audit_event_id uuid)
RETURNS TABLE(result_code text,verification_session_id uuid,state text,
    request_id text,correlation_id text)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_s tagekyc.verification_sessions%ROWTYPE; v_c tagekyc.capture_capabilities%ROWTYPE; v_o tagekyc.capture_capability_operations%ROWTYPE; v_expiry_operation_id uuid;
  v_cancel_operation_id uuid:=p_verification_session_id;
  v_request_fingerprint bytea:=tagekyc_extensions.digest(pg_catalog.convert_to('TAG-EKYC-A1-R26-CANCEL-FINGERPRINT-v1','UTF8')||pg_catalog.uuid_send(p_client_application_id)||pg_catalog.uuid_send(p_verification_session_id),'sha256');
BEGIN
  IF p_client_application_id IS NULL OR p_verification_session_id IS NULL THEN
    RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_verification_session_id::text,70));
  SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id FOR UPDATE;
  IF NOT FOUND THEN RETURN QUERY SELECT 'RESOURCE_NOT_AVAILABLE',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN; END IF;
  SELECT * INTO v_o FROM tagekyc.capture_capability_operations WHERE "ClientApplicationId"=p_client_application_id AND "VerificationSessionId"=p_verification_session_id AND "OperationKind"='Cancel' AND "IdempotencyKey"=v_cancel_operation_id;
  IF FOUND THEN
    IF v_o."RequestFingerprint" IS DISTINCT FROM v_request_fingerprint THEN RETURN QUERY SELECT 'NOT_READY',NULL::uuid,NULL::text,NULL::text,NULL::text;
    ELSE RETURN QUERY SELECT 'AVAILABLE',v_s."Id",v_s."State"::text,v_s."RequestId"::text,v_s."CorrelationId"::text; END IF;
    RETURN;
  END IF;
  -- Candidate metadata belongs only to a first transition; exact replay ignores it.
  IF p_reason IS NULL OR p_reason='' OR pg_catalog.length(p_reason)>64 OR p_reason!~'^[A-Za-z0-9_.:-]+$'
     OR p_request_id IS NULL OR pg_catalog.length(p_request_id)>128 OR p_correlation_id IS NULL OR pg_catalog.length(p_correlation_id)>128
     OR p_actor_key_prefix IS NULL OR pg_catalog.length(p_actor_key_prefix)>128 THEN
    RETURN QUERY SELECT 'INVALID_INPUT',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN;
  END IF;
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "VerificationSessionId"=p_verification_session_id AND "State" IN ('ActiveUnbound','Bound') ORDER BY "CaptureCapabilityId" LIMIT 1;
  IF FOUND THEN
    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_c."CaptureCapabilityId"::text,80));
    SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=v_c."CaptureCapabilityId" FOR UPDATE;
    IF v_c."ExpiresAtUtc"<=p_now THEN
      v_expiry_operation_id:=pg_catalog.gen_random_uuid();
      UPDATE tagekyc.capture_capabilities SET "State"='Expired',"Revision"="Revision"+1,"ExpiredAtUtc"=p_now,"TerminalReason"='CapabilityExpiry' WHERE "CaptureCapabilityId"=v_c."CaptureCapabilityId";
      INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,tagekyc_extensions.digest(pg_catalog.convert_to('tip-88c1-c6b-a1-capability-expiry-v1','UTF8')||pg_catalog.uuid_send(p_verification_session_id)||pg_catalog.uuid_send(v_c."CaptureCapabilityId")||pg_catalog.timestamptz_send(v_c."ExpiresAtUtc")||pg_catalog.uuid_send(v_expiry_operation_id),'sha256'),'Applied',v_c."CaptureCapabilityId",v_c."Revision"+1,p_now,p_now);
      INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Expire',v_expiry_operation_id,v_c."CaptureCapabilityId",'Expired',v_c."Revision",v_c."Revision"+1,p_now);
      v_c:=NULL;
    END IF;
  END IF;
  IF v_s."State"='Cancelled' THEN RETURN QUERY SELECT 'AVAILABLE',v_s."Id",v_s."State"::text,v_s."RequestId"::text,v_s."CorrelationId"::text; RETURN; END IF;
  IF v_s."State" IN ('Completed','Expired','TechnicalTerminal') OR v_s."ExpiresAt"<=p_now THEN RETURN QUERY SELECT 'CONFLICT',NULL::uuid,NULL::text,NULL::text,NULL::text; RETURN; END IF;
  UPDATE tagekyc.verification_sessions SET "State"='Cancelled',"RequestId"=p_request_id,"CorrelationId"=p_correlation_id WHERE "Id"=p_verification_session_id AND "ClientApplicationId"=p_client_application_id;
  INSERT INTO tagekyc.audit_events("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") VALUES(p_audit_event_id,p_client_application_id,p_verification_session_id,'ClientApplication',p_actor_key_prefix,'SESSION_CANCELLED','sha256:localdev-session-cancelled',p_reason,p_request_id,p_correlation_id,p_now);
  IF v_c."CaptureCapabilityId" IS NOT NULL THEN
    UPDATE tagekyc.capture_capabilities SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"TerminalReason"='SessionCancelled' WHERE "CaptureCapabilityId"=v_c."CaptureCapabilityId";
  END IF;
  INSERT INTO tagekyc.capture_capability_operations("ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","RequestFingerprint","ResultCode","ResultCapabilityId","ResultRevision","CreatedAtUtc","CompletedAtUtc") VALUES(p_client_application_id,p_verification_session_id,'Cancel',v_cancel_operation_id,v_request_fingerprint,'Applied',v_c."CaptureCapabilityId",CASE WHEN v_c."CaptureCapabilityId" IS NULL THEN NULL ELSE v_c."Revision"+1 END,p_now,p_now);
  INSERT INTO tagekyc.capture_capability_events("EventId","ClientApplicationId","VerificationSessionId","OperationKind","IdempotencyKey","CaptureCapabilityId","EventType","BeforeRevision","AfterRevision","RecordedAtUtc") VALUES(pg_catalog.gen_random_uuid(),p_client_application_id,p_verification_session_id,'Cancel',v_cancel_operation_id,v_c."CaptureCapabilityId",'Cancelled',CASE WHEN v_c."CaptureCapabilityId" IS NULL THEN NULL ELSE v_c."Revision" END,CASE WHEN v_c."CaptureCapabilityId" IS NULL THEN NULL ELSE v_c."Revision"+1 END,p_now);
  RETURN QUERY SELECT 'APPLIED',p_verification_session_id,'Cancelled',p_request_id,p_correlation_id;
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) TO tagekyc_capture_runtime_application,tagekyc_runtime;

CREATE FUNCTION tagekyc.capture_runtime_validate_append_authority(
    p_capture_agent_id uuid,p_installation_id uuid,p_credential_id uuid,
    p_generation bigint,p_binding_id uuid,
    p_required_role text,p_now timestamptz)
RETURNS TABLE(role_policy_id uuid,role_policy_revision bigint,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,verification_session_id uuid,capability_id uuid,
    capability_revision bigint,binding_id uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_b tagekyc.capture_execution_bindings%ROWTYPE;
        v_c tagekyc.capture_capabilities%ROWTYPE;
        v_s tagekyc.verification_sessions%ROWTYPE;
        discovered_session_id uuid;
        discovered_capability_id uuid;
BEGIN
  IF p_generation<=0 OR p_required_role NOT IN ('CaptureObservation','TrustedEvidence') THEN RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  SELECT * INTO v_b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=p_binding_id;
  IF NOT FOUND THEN RETURN; END IF;
  discovered_session_id:=v_b."VerificationSessionId";
  discovered_capability_id:=v_b."CaptureCapabilityId";
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(discovered_session_id::text,70));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(discovered_capability_id::text,80));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_installation_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  SELECT * INTO v_b FROM tagekyc.capture_execution_bindings WHERE "CaptureExecutionBindingId"=p_binding_id FOR UPDATE;
  IF NOT FOUND OR v_b."VerificationSessionId" IS DISTINCT FROM discovered_session_id
     OR v_b."CaptureCapabilityId" IS DISTINCT FROM discovered_capability_id THEN RETURN; END IF;
  SELECT * INTO v_c FROM tagekyc.capture_capabilities WHERE "CaptureCapabilityId"=v_b."CaptureCapabilityId" FOR UPDATE;
  SELECT * INTO v_s FROM tagekyc.verification_sessions WHERE "Id"=v_b."VerificationSessionId" FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."DeviceInstallationId" IS DISTINCT FROM p_installation_id OR v_g."State" IS DISTINCT FROM 'Active'
     OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
     OR v_b."CaptureAgentId" IS DISTINCT FROM p_capture_agent_id OR v_b."DeviceInstallationId" IS DISTINCT FROM p_installation_id
     OR v_b."CredentialId" IS DISTINCT FROM p_credential_id OR v_b."CredentialGeneration" IS DISTINCT FROM p_generation
     OR v_b."ExecutionExpiresAtUtc"<=p_now
     OR v_b."RuntimeRevision" IS DISTINCT FROM v_r."Revision" OR v_b."InstallationRevision" IS DISTINCT FROM v_i."Revision"
     OR v_b."CredentialRevision" IS DISTINCT FROM v_g."Revision"
     OR v_b."TrustProfileId" IS DISTINCT FROM v_r."TrustProfileId" OR v_b."TrustProfileRevision" IS DISTINCT FROM v_r."TrustProfileRevision"
     OR v_b."ConfigurationId" IS DISTINCT FROM v_r."ConfigurationId" OR v_b."ConfigurationRevision" IS DISTINCT FROM v_r."ConfigurationRevision"
     OR v_b."RolePolicyId" IS DISTINCT FROM v_g."RolePolicyId" OR v_b."RolePolicyRevision" IS DISTINCT FROM v_g."RolePolicyRevision"
     OR v_c."VerificationSessionId" IS DISTINCT FROM v_b."VerificationSessionId" OR v_c."ClientApplicationId" IS DISTINCT FROM v_b."ClientApplicationId"
     OR v_c."State" IS DISTINCT FROM 'Bound' OR v_c."Revision"<=0 OR v_c."ExpiresAtUtc"<=p_now
     OR v_s."ClientApplicationId" IS DISTINCT FROM v_b."ClientApplicationId"
     OR v_b."Challenge" IS DISTINCT FROM v_s."BindingNonceHash"
     OR v_s."State" IN ('Completed','Expired','Cancelled','TechnicalTerminal') OR v_s."ExpiresAt"<=p_now
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_trust_profile_revisions tp
                    WHERE tp."CatalogId"=v_r."TrustProfileId" AND tp."Revision"=v_r."TrustProfileRevision"
                      AND tp."EffectiveAtUtc"<=p_now AND tp."ExpiresAtUtc">p_now
                      AND (p_required_role<>'TrustedEvidence' OR tp."AllowTrustedEvidence"))
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_configuration_revisions cp
                    WHERE cp."CatalogId"=v_r."ConfigurationId" AND cp."Revision"=v_r."ConfigurationRevision"
                      AND cp."EffectiveAtUtc"<=p_now AND cp."ExpiresAtUtc">p_now)
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
                    WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision"
                      AND rp."EffectiveAtUtc"<=p_now AND p_required_role=ANY(rp."Roles")) THEN RETURN;
  END IF;
  RETURN QUERY SELECT v_g."RolePolicyId",v_g."RolePolicyRevision",v_r."Revision",v_i."Revision",v_g."Revision",v_b."VerificationSessionId",v_c."CaptureCapabilityId",v_c."Revision",v_b."CaptureExecutionBindingId";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) FROM PUBLIC,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) TO tagekyc_runtime;

CREATE FUNCTION tagekyc.capture_runtime_read_cutover_state(p_profile text,p_now timestamptz)
RETURNS TABLE(profile text,state text,revision bigint,prepared_at_utc timestamptz,
    activated_at_utc timestamptz,activated_by_credential_id uuid,required_pepper_versions integer[])
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path=pg_catalog
AS $function$
    SELECT c."Profile",c."State",c."Revision",c."PreparedAtUtc",
           c."ActivatedAtUtc",c."ActivatedByCredentialId",
           ARRAY(SELECT DISTINCT x.v FROM (
             SELECT p."VerifierPepperVersion" v FROM tagekyc.platform_operator_credentials p WHERE p."State"='Active' AND p."ExpiresAtUtc">p_now
             UNION SELECT b."VerifierPepperVersion" FROM tagekyc.capture_runtime_bootstrap_issuances b WHERE b."State"='Active' AND b."ExpiresAtUtc">p_now
             UNION SELECT c."VerifierPepperVersion" FROM tagekyc.capture_capabilities c WHERE c."State" IN ('ActiveUnbound','Bound') AND c."ExpiresAtUtc">p_now
           ) x ORDER BY x.v)
    FROM tagekyc.capture_runtime_cutover_state c
    WHERE p_profile='Managed' AND c."Profile"=p_profile;
$function$;

ALTER FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) TO tagekyc_capture_runtime_application;
""");
            migrationBuilder.Sql("""
CREATE FUNCTION tagekyc.platform_operator_authenticate(
    p_key_lookup_prefix text,p_now timestamptz)
RETURNS TABLE(credential_id uuid,principal_id uuid,secret_digest bytea,
    verifier_pepper_version integer,scopes text[],revision bigint)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT c."CredentialId",c."PrincipalId",c."SecretDigest",
           c."VerifierPepperVersion",c."Scopes",c."Revision"
    FROM tagekyc.platform_operator_credentials c
    WHERE c."KeyLookupPrefix"=p_key_lookup_prefix
      AND c."State"='Active' AND c."ExpiresAtUtc">p_now;
$function$;
ALTER FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_resolve_verifier(
    p_credential_id uuid,p_generation bigint,p_now timestamptz)
RETURNS TABLE(capture_agent_id uuid,device_installation_id uuid,
    credential_id uuid,generation bigint,public_verifier_spki bytea,
    public_key_thumbprint bytea,role_policy_id uuid,role_policy_revision bigint,
    runtime_revision bigint,installation_revision bigint,credential_revision bigint)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT r."CaptureAgentId",i."DeviceInstallationId",g."CredentialId",g."Generation",
           g."PublicVerifierSpki",g."PublicKeyThumbprint",g."RolePolicyId",g."RolePolicyRevision",
           r."Revision",i."Revision",g."Revision"
    FROM tagekyc.capture_runtime_credential_generations g
    JOIN tagekyc.capture_runtime_installations i
      ON i."DeviceInstallationId"=g."DeviceInstallationId"
     AND i."CurrentCredentialId"=g."CredentialId"
     AND i."CurrentCredentialGeneration"=g."Generation"
    JOIN tagekyc.capture_runtime_registrations r ON r."CaptureAgentId"=i."CaptureAgentId"
    WHERE g."CredentialId"=p_credential_id AND g."Generation"=p_generation
      AND r."LifecycleState"='Active' AND i."LifecycleState"='Active'
      AND g."State"='Active' AND g."ValidFromUtc"<=p_now AND g."ValidUntilUtc">p_now;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(
    p_bootstrap_issuance_id uuid)
RETURNS TABLE(verifier_pepper_version integer)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
    SELECT b."VerifierPepperVersion"
    FROM tagekyc.capture_runtime_bootstrap_issuances b
    WHERE b."BootstrapIssuanceId"=p_bootstrap_issuance_id;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) TO tagekyc_capture_runtime_application;

CREATE FUNCTION tagekyc.capture_runtime_claim_nonce(
    p_capture_agent_id uuid,p_device_installation_id uuid,p_credential_id uuid,
    p_generation bigint,p_required_role text,p_nonce bytea,
    p_signed_timestamp timestamptz,p_admitted_at timestamptz)
RETURNS TABLE(result_code text,runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,role_policy_id uuid,role_policy_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
BEGIN
  IF p_required_role NOT IN ('Bind','CaptureObservation','Configuration','CredentialRotation','RawIngress','TrustedEvidence')
     OR pg_catalog.octet_length(p_nonce)<>32
     OR p_signed_timestamp<p_admitted_at-interval '150 seconds'
     OR p_signed_timestamp>p_admitted_at+interval '150 seconds' THEN
    RETURN QUERY SELECT 'INVALID_INPUT',NULL::bigint,NULL::bigint,NULL::bigint,NULL::uuid,NULL::bigint; RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('capture-runtime-nonce-capacity',1));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_device_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active' OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."State" IS DISTINCT FROM 'Active' OR v_g."ValidFromUtc">p_admitted_at OR v_g."ValidUntilUtc"<=p_admitted_at
     OR NOT EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
                    WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision"
                      AND rp."EffectiveAtUtc"<=p_admitted_at AND p_required_role=ANY(rp."Roles")) THEN
    RETURN QUERY SELECT 'ACCESS_DENIED',NULL::bigint,NULL::bigint,NULL::bigint,NULL::uuid,NULL::bigint; RETURN;
  END IF;
  BEGIN
    INSERT INTO tagekyc.capture_runtime_request_nonces VALUES(
      p_credential_id,p_generation,p_nonce,p_signed_timestamp,p_admitted_at,p_signed_timestamp+interval '150 seconds');
  EXCEPTION WHEN unique_violation THEN
    RETURN QUERY SELECT 'REPLAY',NULL::bigint,NULL::bigint,NULL::bigint,NULL::uuid,NULL::bigint; RETURN;
  END;
  RETURN QUERY SELECT 'ADMITTED',v_r."Revision",v_i."Revision",v_g."Revision",v_g."RolePolicyId",v_g."RolePolicyRevision";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(
    p_credential_id uuid,p_generation bigint,p_rotation_id uuid,
    p_candidate_key_id uuid,p_successor_thumbprint bytea,
    p_request_fingerprint_as_predecessor bytea,
    p_request_fingerprint_as_successor bytea,p_nonce bytea,
    p_signed_at timestamptz,p_now timestamptz)
RETURNS TABLE(branch text,selected_request_fingerprint bytea,
    runtime_revision bigint,installation_revision bigint,
    credential_revision bigint,role_policy_id uuid,role_policy_revision bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_r tagekyc.capture_runtime_registrations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_a tagekyc.capture_runtime_rotation_authorizations%ROWTYPE;
        v_o tagekyc.capture_runtime_rotation_completion_operations%ROWTYPE;
        v_branch text;
        v_selected bytea;
BEGIN
  IF p_generation<=0 OR pg_catalog.octet_length(p_successor_thumbprint)<>32
     OR (p_request_fingerprint_as_predecessor IS NOT NULL AND pg_catalog.octet_length(p_request_fingerprint_as_predecessor)<>32)
     OR (p_request_fingerprint_as_successor IS NOT NULL AND pg_catalog.octet_length(p_request_fingerprint_as_successor)<>32)
     OR (p_request_fingerprint_as_predecessor IS NULL AND p_request_fingerprint_as_successor IS NULL)
     OR pg_catalog.octet_length(p_nonce)<>32
     OR p_signed_at<p_now-interval '150 seconds' OR p_signed_at>p_now+interval '150 seconds' THEN
    RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('capture-runtime-nonce-capacity',1));
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations
   WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation;
  IF NOT FOUND THEN RETURN; END IF;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations
   WHERE "DeviceInstallationId"=v_g."DeviceInstallationId";
  IF NOT FOUND THEN RETURN; END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_i."CaptureAgentId"::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(v_i."DeviceInstallationId"::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_rotation_id::text,60));
  SELECT * INTO v_r FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"=v_i."CaptureAgentId" FOR UPDATE;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=v_i."DeviceInstallationId" FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  SELECT * INTO v_a FROM tagekyc.capture_runtime_rotation_authorizations WHERE "RotationAuthorizationId"=p_rotation_id FOR UPDATE;
  IF v_r."LifecycleState" IS DISTINCT FROM 'Active' OR v_i."LifecycleState" IS DISTINCT FROM 'Active'
     OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation
     OR v_g."State" IS DISTINCT FROM 'Active' OR v_g."ValidFromUtc">p_now OR v_g."ValidUntilUtc"<=p_now
     OR v_a."DeviceInstallationId" IS DISTINCT FROM v_i."DeviceInstallationId"
     OR v_a."CredentialId" IS DISTINCT FROM p_credential_id THEN RETURN;
  END IF;
  IF v_a."State"='Active' AND v_a."CurrentGeneration"=p_generation
     AND p_request_fingerprint_as_predecessor IS NOT NULL
     AND EXISTS (SELECT 1 FROM tagekyc.capture_runtime_role_policy_revisions rp
                 WHERE rp."CatalogId"=v_g."RolePolicyId" AND rp."Revision"=v_g."RolePolicyRevision"
                   AND rp."EffectiveAtUtc"<=p_now AND 'CredentialRotation'=ANY(rp."Roles")) THEN
    v_branch:='Predecessor'; v_selected:=p_request_fingerprint_as_predecessor;
  ELSIF v_a."State"='Completed' AND v_a."SuccessorGeneration"=p_generation
     AND v_a."CandidateKeyId" IS NOT DISTINCT FROM p_candidate_key_id
     AND v_a."SuccessorPublicKeyThumbprint" IS NOT DISTINCT FROM p_successor_thumbprint
     AND p_request_fingerprint_as_successor IS NOT NULL THEN
    SELECT * INTO v_o FROM tagekyc.capture_runtime_rotation_completion_operations
     WHERE "RotationAuthorizationId"=p_rotation_id AND "CredentialId"=p_credential_id
       AND "SuccessorGeneration"=p_generation AND "CandidateKeyId"=p_candidate_key_id
       AND "ResultCode"='Applied' AND "RequestFingerprint" IS NOT DISTINCT FROM p_request_fingerprint_as_successor;
    IF NOT FOUND THEN RETURN; END IF;
    v_branch:='Successor'; v_selected:=p_request_fingerprint_as_successor;
  ELSE RETURN;
  END IF;
  BEGIN
    INSERT INTO tagekyc.capture_runtime_request_nonces VALUES(
      p_credential_id,p_generation,p_nonce,p_signed_at,p_now,p_signed_at+interval '150 seconds');
  EXCEPTION WHEN unique_violation THEN RETURN;
  END;
  RETURN QUERY SELECT v_branch,v_selected,v_r."Revision",v_i."Revision",v_g."Revision",v_g."RolePolicyId",v_g."RolePolicyRevision";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_cleanup_nonces(p_now timestamptz,p_limit integer)
RETURNS TABLE(deleted_count bigint)
LANGUAGE sql VOLATILE SECURITY DEFINER SET search_path=pg_catalog
AS $function$
  WITH doomed AS (
    SELECT n."CredentialId",n."CredentialGeneration",n."Nonce"
    FROM tagekyc.capture_runtime_request_nonces n
    WHERE n."PurgeAfterUtc"<p_now
    ORDER BY n."PurgeAfterUtc",n."CredentialId",n."CredentialGeneration",n."Nonce"
    LIMIT CASE WHEN p_limit BETWEEN 1 AND 10000 THEN p_limit ELSE 0 END
    FOR UPDATE SKIP LOCKED
  ), deleted AS (
    DELETE FROM tagekyc.capture_runtime_request_nonces n USING doomed d
    WHERE n."CredentialId"=d."CredentialId" AND n."CredentialGeneration"=d."CredentialGeneration" AND n."Nonce"=d."Nonce"
    RETURNING 1)
  SELECT pg_catalog.count(*) FROM deleted;
$function$;
ALTER FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_application,tagekyc_capture_runtime_operator;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) TO tagekyc_capture_runtime_authenticator;

CREATE FUNCTION tagekyc.capture_runtime_revoke_credential(
    p_actor uuid,p_idempotency_key uuid,p_capture_agent_id uuid,
    p_device_installation_id uuid,p_credential_id uuid,p_generation bigint,
    p_expected_revision bigint,p_reason text,p_request_fingerprint bytea,p_now timestamptz)
RETURNS TABLE(result_code text,credential_id uuid,generation bigint,state text,
    revision bigint,revoked_at_utc timestamptz)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog
AS $function$
DECLARE v_o tagekyc.capture_runtime_management_operations%ROWTYPE;
        v_g tagekyc.capture_runtime_credential_generations%ROWTYPE;
        v_i tagekyc.capture_runtime_installations%ROWTYPE;
BEGIN
  IF p_generation<=0 OR p_expected_revision<=0 OR p_reason<>'CredentialCompromise'
     OR pg_catalog.octet_length(p_request_fingerprint)<>32 THEN
    RETURN QUERY SELECT 'InvalidInput',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM tagekyc.platform_operator_credentials c WHERE c."CredentialId"=p_actor AND c."State"='Active' AND c."ExpiresAtUtc">p_now AND c."Scopes"=ARRAY['operator.capture-runtime.manage']::text[]) THEN
    RETURN QUERY SELECT 'Denied',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_capture_agent_id::text,10));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_device_installation_id::text,20));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_credential_id::text,30));
  PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_idempotency_key::text,60));
  SELECT * INTO v_o FROM tagekyc.capture_runtime_management_operations
   WHERE "ActorCredentialId"=p_actor AND "OperationKind"='CredentialRevoke' AND "IdempotencyKey"=p_idempotency_key;
  IF FOUND THEN
    IF v_o."RequestFingerprint"<>p_request_fingerprint THEN RETURN QUERY SELECT 'Conflict',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz;
    ELSE RETURN QUERY SELECT 'Revoked',g."CredentialId",g."Generation",g."State"::text,g."Revision",g."RevokedAtUtc" FROM tagekyc.capture_runtime_credential_generations g WHERE g."CredentialId"=p_credential_id AND g."Generation"=p_generation; END IF;
    RETURN;
  END IF;
  SELECT * INTO v_i FROM tagekyc.capture_runtime_installations WHERE "DeviceInstallationId"=p_device_installation_id AND "CaptureAgentId"=p_capture_agent_id FOR UPDATE;
  SELECT * INTO v_g FROM tagekyc.capture_runtime_credential_generations WHERE "DeviceInstallationId"=p_device_installation_id AND "CredentialId"=p_credential_id AND "Generation"=p_generation FOR UPDATE;
  IF NOT FOUND OR v_i."CurrentCredentialId" IS DISTINCT FROM p_credential_id OR v_i."CurrentCredentialGeneration" IS DISTINCT FROM p_generation THEN
    RETURN QUERY SELECT 'ResourceNotAvailable',NULL::uuid,NULL::bigint,NULL::text,NULL::bigint,NULL::timestamptz; RETURN;
  END IF;
  IF v_g."State"<>'Active' OR v_g."Revision"<>p_expected_revision OR v_i."LifecycleState"<>'Active' THEN
    RETURN QUERY SELECT 'Conflict',v_g."CredentialId",v_g."Generation",v_g."State"::text,v_g."Revision",v_g."RevokedAtUtc"; RETURN;
  END IF;
  UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"TerminalReason"=p_reason WHERE "CredentialId"=p_credential_id AND "Generation"=p_generation RETURNING * INTO v_g;
  UPDATE tagekyc.capture_runtime_installations SET "LifecycleState"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=p_now,"LifecycleReason"=p_reason WHERE "DeviceInstallationId"=p_device_installation_id;
  INSERT INTO tagekyc.capture_runtime_management_operations VALUES(p_actor,'CredentialRevoke',p_idempotency_key,p_request_fingerprint,'Credential',p_credential_id,'Applied',v_g."Revision",p_credential_id,p_now,p_now);
  INSERT INTO tagekyc.capture_runtime_management_events VALUES(pg_catalog.gen_random_uuid(),p_actor,'CredentialRevoke',p_idempotency_key,'Applied','Credential',p_credential_id,p_expected_revision,v_g."Revision",p_reason,p_now);
  RETURN QUERY SELECT 'Revoked',v_g."CredentialId",v_g."Generation",v_g."State"::text,v_g."Revision",v_g."RevokedAtUtc";
END $function$;
ALTER FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) OWNER TO tagekyc_raw_export_deployer;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) FROM PUBLIC,tagekyc_runtime,tagekyc_capture_runtime_authenticator,tagekyc_capture_runtime_application;
GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) TO tagekyc_capture_runtime_operator;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.Sql("""
REVOKE ALL ON SCHEMA tagekyc FROM tagekyc_capture_runtime_operator,
    tagekyc_capture_runtime_authenticator, tagekyc_capture_runtime_application;

REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer) FROM tagekyc_capture_runtime_authenticator;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz) FROM tagekyc_capture_runtime_authenticator;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz) FROM tagekyc_capture_runtime_authenticator;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz) FROM tagekyc_capture_runtime_authenticator;
REVOKE EXECUTE ON FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz) FROM tagekyc_capture_runtime_authenticator;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz) FROM tagekyc_runtime;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid) FROM tagekyc_capture_runtime_application,tagekyc_runtime;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid) FROM tagekyc_capture_runtime_application;
REVOKE EXECUTE ON FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_application,tagekyc_runtime;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz) FROM tagekyc_capture_runtime_application;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;
REVOKE ALL ON FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz) FROM tagekyc_capture_runtime_operator;

DROP FUNCTION tagekyc.capture_runtime_revoke_credential(uuid,uuid,uuid,uuid,uuid,bigint,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_cleanup_nonces(timestamptz,integer);
DROP FUNCTION tagekyc.capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_claim_nonce(uuid,uuid,uuid,bigint,text,bytea,timestamptz,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_resolve_bootstrap_verifier(uuid);
DROP FUNCTION tagekyc.capture_runtime_resolve_verifier(uuid,bigint,timestamptz);
DROP FUNCTION tagekyc.platform_operator_authenticate(text,timestamptz);
""");
            migrationBuilder.Sql("""
DROP FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz);

DROP FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz);

DROP FUNCTION tagekyc.capture_runtime_cancel_session_with_capability(uuid,uuid,text,text,text,timestamptz,text,uuid);
REVOKE INSERT ("Id","ClientApplicationId","VerificationSessionId","ActorType","ActorId","EventType","EventPayloadHash","EventPayloadRef","RequestId","CorrelationId","OccurredAt") ON tagekyc.audit_events FROM tagekyc_raw_export_deployer;

DROP FUNCTION tagekyc.capture_runtime_read_readiness(uuid,timestamptz);

DROP FUNCTION tagekyc.capture_runtime_resolve_configuration(uuid,uuid,uuid,bigint,timestamptz);

DROP FUNCTION tagekyc.capture_runtime_materialize_capability_expiry(uuid,uuid,timestamptz,uuid);

DROP FUNCTION tagekyc.capture_runtime_reconcile_binding(uuid,uuid,uuid,bigint,uuid,uuid,timestamptz);

DROP FUNCTION tagekyc.capture_runtime_bind_capability(uuid,uuid,uuid,bigint,uuid,boolean,uuid,bytea,timestamptz);

DROP FUNCTION tagekyc.capture_runtime_resolve_capability_verifier(uuid);

DROP FUNCTION tagekyc.capture_runtime_issue_or_replace_capability(uuid,uuid,text,uuid,bigint,uuid,uuid,text,bytea,integer,bytea,timestamptz);
""");
            migrationBuilder.Sql("""
DROP FUNCTION tagekyc.capture_runtime_revoke_rotation(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_replay_completed_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_complete_rotation(uuid,uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_publish_role_policy(uuid,uuid,uuid,bigint,timestamptz,text[],bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_publish_trust_profile(uuid,uuid,uuid,bigint,timestamptz,timestamptz,text,boolean,boolean,boolean,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_publish_configuration(uuid,uuid,uuid,bigint,timestamptz,timestamptz,boolean,integer,integer,integer,integer,integer,bigint,integer,bigint,integer,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_assign_configuration(uuid,uuid,uuid,uuid,bigint,bigint,uuid,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_assign_role_policy(uuid,uuid,uuid,uuid,bigint,bigint,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_authorize_rotation(uuid,uuid,uuid,uuid,uuid,bigint,bigint,timestamptz,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_assert_current_operator(uuid,timestamptz);
""");
            migrationBuilder.Sql("""
DROP FUNCTION tagekyc.capture_runtime_retire(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_revoke(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_reactivate(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_suspend(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_transition_runtime_lifecycle(uuid,uuid,uuid,bigint,text,bytea,timestamptz,text,text,text,boolean);
DROP FUNCTION tagekyc.capture_runtime_redeem_bootstrap(uuid,uuid,bytea,uuid,bytea,bytea,timestamptz,bytea,bytea,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_revoke_bootstrap(uuid,uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.capture_runtime_issue_bootstrap(uuid,uuid,text,uuid,bigint,uuid,bigint,uuid,bigint,timestamptz,bytea,text,bytea,integer,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_root_revoke_platform_credential(uuid,uuid,bigint,text,bytea,timestamptz);
DROP FUNCTION tagekyc.c6ba_root_provision_platform_credential(uuid,uuid,timestamptz,text,bytea,integer,bytea,timestamptz);
""");
            migrationBuilder.Sql("""
DROP TRIGGER IF EXISTS root_operation_requires_event ON tagekyc.platform_operator_root_operations;
DROP TRIGGER IF EXISTS rotation_operation_requires_event ON tagekyc.capture_runtime_rotation_completion_operations;
DROP TRIGGER IF EXISTS redemption_operation_requires_event ON tagekyc.capture_runtime_bootstrap_redemption_operations;
DROP TRIGGER IF EXISTS capability_operation_requires_event ON tagekyc.capture_capability_operations;
DROP TRIGGER IF EXISTS management_operation_requires_event ON tagekyc.capture_runtime_management_operations;
DROP TRIGGER IF EXISTS configuration_override_narrowing_guard ON tagekyc.capture_runtime_configuration_overrides;
DROP TRIGGER IF EXISTS capability_graph_guard ON tagekyc.capture_capabilities;
DROP TRIGGER IF EXISTS binding_capability_graph_guard ON tagekyc.capture_execution_bindings;
DROP TRIGGER IF EXISTS generation_current_installation_guard ON tagekyc.capture_runtime_credential_generations;
DROP TRIGGER IF EXISTS installation_current_generation_guard ON tagekyc.capture_runtime_installations;
DROP TRIGGER IF EXISTS configuration_revisions_immutable ON tagekyc.capture_runtime_configuration_revisions;
DROP TRIGGER IF EXISTS trust_profile_revisions_immutable ON tagekyc.capture_runtime_trust_profile_revisions;
DROP TRIGGER IF EXISTS role_policy_revisions_immutable ON tagekyc.capture_runtime_role_policy_revisions;
DROP TRIGGER IF EXISTS platform_operator_root_operations_immutable ON tagekyc.platform_operator_root_operations;
DROP TRIGGER IF EXISTS rotation_completion_operations_immutable ON tagekyc.capture_runtime_rotation_completion_operations;
DROP TRIGGER IF EXISTS bootstrap_redemption_operations_immutable ON tagekyc.capture_runtime_bootstrap_redemption_operations;
DROP TRIGGER IF EXISTS capture_capability_operations_immutable ON tagekyc.capture_capability_operations;
DROP TRIGGER IF EXISTS capture_runtime_management_operations_immutable ON tagekyc.capture_runtime_management_operations;
DROP TRIGGER IF EXISTS rotation_completion_events_immutable ON tagekyc.capture_runtime_rotation_completion_events;
DROP TRIGGER IF EXISTS bootstrap_redemption_events_immutable ON tagekyc.capture_runtime_bootstrap_redemption_events;
DROP TRIGGER IF EXISTS platform_operator_root_events_immutable ON tagekyc.platform_operator_root_events;
DROP TRIGGER IF EXISTS capture_capability_events_immutable ON tagekyc.capture_capability_events;
DROP TRIGGER IF EXISTS capture_runtime_management_events_immutable ON tagekyc.capture_runtime_management_events;
DROP FUNCTION tagekyc.c6ba_require_root_event();
DROP FUNCTION tagekyc.c6ba_require_rotation_completion_event();
DROP FUNCTION tagekyc.c6ba_require_redemption_event();
DROP FUNCTION tagekyc.c6ba_require_capability_event();
DROP FUNCTION tagekyc.c6ba_require_management_event();
DROP FUNCTION tagekyc.c6ba_validate_configuration_override();
DROP FUNCTION tagekyc.c6ba_validate_capability_graph();
DROP FUNCTION tagekyc.c6ba_validate_current_generation();
DROP FUNCTION tagekyc.c6ba_reject_row_mutation();
DROP TABLE tagekyc.capture_runtime_rotation_completion_events;
DROP TABLE tagekyc.capture_runtime_bootstrap_redemption_events;
DROP TABLE tagekyc.capture_capability_events;
DROP TABLE tagekyc.capture_runtime_management_events;
DROP TABLE tagekyc.platform_operator_root_events;
DROP TABLE tagekyc.platform_operator_root_operations;
DROP TABLE tagekyc.capture_runtime_cutover_state;
DROP TABLE tagekyc.capture_runtime_rotation_completion_operations;
DROP TABLE tagekyc.capture_runtime_bootstrap_redemption_operations;
DROP TABLE tagekyc.capture_capability_operations;
DROP TABLE tagekyc.capture_runtime_management_operations;
DROP TABLE tagekyc.capture_execution_bindings;
DROP TABLE tagekyc.capture_capabilities;
DROP TABLE tagekyc.capture_runtime_rotation_authorizations;
ALTER TABLE tagekyc.capture_runtime_registrations DROP CONSTRAINT "FK_registration_override";
DROP TABLE tagekyc.capture_runtime_configuration_overrides;
DROP TABLE tagekyc.capture_runtime_bootstrap_issuances;
DROP TABLE tagekyc.capture_runtime_request_nonces;
ALTER TABLE tagekyc.capture_runtime_installations DROP CONSTRAINT "FK_installation_current_generation";
DROP TABLE tagekyc.capture_runtime_credential_generations;
DROP TABLE tagekyc.capture_runtime_installations;
DROP TABLE tagekyc.capture_runtime_registrations;
DROP TABLE tagekyc.capture_runtime_configuration_heads;
DROP TABLE tagekyc.capture_runtime_trust_profile_heads;
DROP TABLE tagekyc.capture_runtime_role_policy_heads;
DROP TABLE tagekyc.capture_runtime_configuration_revisions;
DROP TABLE tagekyc.capture_runtime_trust_profile_revisions;
DROP TABLE tagekyc.capture_runtime_role_policy_revisions;
DROP TABLE tagekyc.platform_operator_credentials;
DROP FUNCTION tagekyc.c6ba_roles_are_canonical(text[]);
ALTER TABLE tagekyc.verification_sessions DROP CONSTRAINT "AK_verification_sessions_id_client";

DO $drop_capability_roles$
DECLARE
    role_name text;
    role_oid oid;
BEGIN
    FOREACH role_name IN ARRAY ARRAY[
        'tagekyc_capture_runtime_operator',
        'tagekyc_capture_runtime_authenticator',
        'tagekyc_capture_runtime_application'
    ]::text[] LOOP
        SELECT r.oid INTO role_oid FROM pg_catalog.pg_roles AS r WHERE r.rolname = role_name;
        IF role_oid IS NULL THEN
            RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_ROLE_MISSING: %', role_name USING ERRCODE = 'P0001';
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_auth_members AS m
            WHERE m.roleid = role_oid OR m.member = role_oid
        ) THEN
            RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_ROLE_MEMBERSHIP: %', role_name USING ERRCODE = 'P0001';
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_shdepend AS d
            WHERE d.refclassid = 'pg_catalog.pg_authid'::regclass AND d.refobjid = role_oid
        ) THEN
            RAISE EXCEPTION 'TIP88C1C6BA_CAPABILITY_ROLE_DEPENDENCY: %', role_name USING ERRCODE = 'P0001';
        END IF;
        EXECUTE pg_catalog.format('DROP ROLE %I', role_name);
    END LOOP;
END
$drop_capability_roles$;
""");
    }
}
