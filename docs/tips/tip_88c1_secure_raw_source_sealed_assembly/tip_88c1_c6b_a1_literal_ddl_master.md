# TIP-88C1-C6B-A1 — Literal DDL Master

**Status:** AS-BUILT REVIEW CANDIDATE — NOT STANDALONE AUTHORITY  
**Purpose:** Mechanical PostgreSQL catalogue reconciled with the tested A1 Foundation.  
**Implementation authority:** Consumed from the Homeowner-authorized parent; no new grant here.

This companion is reviewed together with every numbered block, writer/reader mapping,
Down operation and proof mapping; measured evidence is indexed by the parent.

## Block 1 — catalog and configuration authority

```sql
DO $preflight$
BEGIN
    IF current_user <> 'tagekyc_raw_export_deployer' THEN
        RAISE EXCEPTION 'TIP88C1C6BA_MIGRATION_OWNER_REQUIRED';
    END IF;
END
$preflight$;

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
```

Writers: `capture_runtime_publish_role_policy`,
`capture_runtime_publish_trust_profile`, and
`capture_runtime_publish_configuration`. Readers: assignment functions,
configuration resolution, bind, and readiness. Down drops the three head tables
before the three revision tables and drops
`tagekyc.c6ba_roles_are_canonical(text[])` after the role-policy revision table.
Proofs: `A1-09`, `A1-10`, `A1-16`, `A1-19`, `A1-20`.

## Block 2 — runtime identity and nonce authority

```sql
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
```

Writers: bootstrap redemption, rotation, lifecycle, nonce claim, and nonce
cleanup transition functions. Readers: authentication, bind, configuration,
capture/evidence, and readiness functions. Down drops the nonce table, removes
`FK_installation_current_generation`, drops credential generations, drops
installations, then drops registrations. Proofs: `A1-03`, `A1-04`, `A1-06`,
`A1-07`, `A1-08`, `A1-19`, `A1-20`.

## Block 3 — remaining authority aggregates

```sql
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
```

Writers: `c6ba_root_provision_platform_credential`,
`c6ba_root_revoke_platform_credential`, `capture_runtime_issue_bootstrap`,
`capture_runtime_revoke_bootstrap`, `capture_runtime_redeem_bootstrap`,
`capture_runtime_suspend`, `capture_runtime_reactivate`,
`capture_runtime_revoke`, `capture_runtime_retire`,
`capture_runtime_revoke_credential`, `capture_runtime_authorize_rotation`,
`capture_runtime_revoke_rotation`, `capture_runtime_complete_rotation`,
`capture_runtime_publish_trust_profile`, `capture_runtime_publish_role_policy`,
`capture_runtime_publish_configuration`, `capture_runtime_assign_role_policy`,
`capture_runtime_assign_configuration`,
`capture_runtime_issue_or_replace_capability`,
`capture_runtime_cancel_session_with_capability`,
`capture_runtime_bind_capability`, `capture_runtime_claim_nonce`,
`capture_runtime_claim_rotation_completion_nonce`,
`capture_runtime_cleanup_nonces` and
`capture_runtime_materialize_capability_expiry`.
Readers: `platform_operator_authenticate`, `capture_runtime_resolve_verifier`,
`capture_runtime_resolve_bootstrap_verifier`,
`capture_runtime_replay_completed_rotation`,
`capture_runtime_reconcile_binding`, `capture_runtime_resolve_configuration`,
`capture_runtime_read_readiness`, `capture_runtime_read_cutover_state` and
`capture_runtime_validate_append_authority`.
R06 `capture_runtime_suspend` and R07 `capture_runtime_reactivate` mutate only
the registration revision/state and append their management operation/event;
they do not terminalize installation or credential rows. R08
`capture_runtime_revoke` and R09 `capture_runtime_retire` terminalize the
registration, installation and current credential under the 10→20→30 lock
order. R24 capture-artifact and R25 evidence use their landed neutral cores
inside the runtime-authenticated application boundary; no SQL function named
`capture_runtime_apply_capture_artifact` or
`capture_runtime_apply_evidence_result` exists. Direct table DML is denied to PUBLIC,
`tagekyc_runtime`, `tagekyc_capture_runtime_authenticator`,
`tagekyc_capture_runtime_operator` and
`tagekyc_capture_runtime_application`. Down drops immutable triggers, event tables before operation
tables, cutover state, completion/redemption operations, bindings, capabilities,
rotation authorizations, the registration override FK, overrides and bootstrap
issuances; it then drops `tagekyc.c6ba_validate_configuration_override()`,
`tagekyc.c6ba_validate_capability_graph()`,
`tagekyc.c6ba_validate_current_generation()` and
`tagekyc.c6ba_reject_row_mutation()`.
Proofs: `A1-02`, `A1-05`, `A1-09`, `A1-11`, `A1-12`, `A1-13`, `A1-14`,
`A1-15`, `A1-16`, `A1-17`, `A1-18`, `A1-19`, `A1-20`, `A1-21`, `A1-22`,
`A1-23`, `A1-24`.

### Stage-1 internal helper authority

| Helper signature | Owner / security | Sole EXECUTE grantee | Down |
| --- | --- | --- | --- |
| `capture_runtime_resolve_bootstrap_verifier(uuid)` | `tagekyc_raw_export_deployer`; SQL STABLE SECURITY DEFINER; `search_path=pg_catalog` | `tagekyc_capture_runtime_application` | revoke Application, then drop before bootstrap tables |
| `capture_runtime_claim_rotation_completion_nonce(uuid,bigint,uuid,uuid,bytea,bytea,bytea,bytea,timestamptz,timestamptz)` | `tagekyc_raw_export_deployer`; PL/pgSQL SECURITY DEFINER; `search_path=pg_catalog` | `tagekyc_capture_runtime_authenticator` | revoke Authenticator, then drop before nonce/rotation dependencies |
| `capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz)` | `tagekyc_raw_export_deployer`; PL/pgSQL SECURITY DEFINER; `search_path=pg_catalog` | `tagekyc_runtime` | revoke Runtime, then drop before binding/capability dependencies |

All three revoke PUBLIC and every unrelated A1 role. The bootstrap resolver
returns only the persisted positive pepper-version metadata without lifecycle
filtering. The rotation helper accepts nullable predecessor/successor candidate
fingerprints and uses `IS NULL`, `IS NOT NULL` and `IS NOT DISTINCT FROM`
semantics before exactly one nonce insert. The append validator accepts only
`CaptureObservation` or `TrustedEvidence`, locks 10→20→30→70→80 and returns
only the frozen authority/revision tuple. Its seven inputs contain no session
selector; it derives the session from the locked exact binding before the
ordinary-context neutral append writer runs in the same transaction B.

### Executable Down order

```sql
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
DROP FUNCTION tagekyc.c6ba_require_root_event();
DROP FUNCTION tagekyc.c6ba_require_rotation_completion_event();
DROP FUNCTION tagekyc.c6ba_require_redemption_event();
DROP FUNCTION tagekyc.c6ba_require_capability_event();
DROP FUNCTION tagekyc.c6ba_require_management_event();
DROP FUNCTION tagekyc.c6ba_validate_configuration_override();
DROP FUNCTION tagekyc.c6ba_validate_capability_graph();
DROP FUNCTION tagekyc.c6ba_validate_current_generation();
DROP FUNCTION tagekyc.c6ba_reject_row_mutation();
DROP FUNCTION tagekyc.c6ba_roles_are_canonical(text[]);
ALTER TABLE tagekyc.verification_sessions DROP CONSTRAINT "AK_verification_sessions_id_client";
```

### Transition-definition binding

This DDL companion does not authorize placeholder or stub transition bodies.
Every SECURITY DEFINER transition must use the complete signature, result shape,
lock/CAS body and sole grantee recorded by the sibling
`tip_88c1_c6b_a1_operation_master.md`. The parent A1 dispatch binds exact SHA-256
for both companions; companions do not hash-bind each other because mutual
content hashes would be circular. A migration is incomplete unless those
definitions are rendered literally and its generated SQL passes the same
PostgreSQL execution check as the tables above.
