using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportAuthorizationRepository : IRawExportAuthorizationRepository
{
    private const string InvariantFailureCode = "RAW_EXPORT_AUTHORIZATION_INVARIANT_FAILURE";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new();
    private static readonly Regex IdempotencyKeyPattern =
        new("^[A-Za-z0-9._:-]{1,256}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex LowerHex64Pattern =
        new("^[0-9a-f]{64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly TagEkycDbContext db;
    private readonly IRawExportControlPlaneRepository controlPlane;
    private readonly IRawExportSubjectConsentRepository subjectConsent;
    private readonly IRawExportPolicyRepository policies;
    private readonly RawExportPermitTtlBoundsState permitTtlBounds;

    public EfRawExportAuthorizationRepository(TagEkycDbContext db)
        : this(
            db,
            new EfRawExportControlPlaneRepository(db),
            new EfRawExportSubjectConsentRepository(db),
            new EfRawExportPolicyRepository(db),
            RawExportPermitTtlBoundsState.Valid(
                RawExportPermitTtlOptions.DefaultMinSeconds,
                RawExportPermitTtlOptions.DefaultMaxSeconds))
    {
    }

    public EfRawExportAuthorizationRepository(
        TagEkycDbContext db,
        IRawExportControlPlaneRepository controlPlane,
        IRawExportSubjectConsentRepository subjectConsent,
        IRawExportPolicyRepository policies,
        RawExportPermitTtlBoundsState permitTtlBounds)
    {
        this.db = db;
        this.controlPlane = controlPlane;
        this.subjectConsent = subjectConsent;
        this.policies = policies;
        this.permitTtlBounds = permitTtlBounds;
    }

    public async Task<RawExportAuthorizationResult> AuthorizeExportAsync(
        AuthorizeRawExportCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = PrepareRequest(command);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await SetActorContextAsync(request.Command.Actor.PrincipalId, cancellationToken);
        var prospectiveDecisionId = Guid.NewGuid();
        var claim = await ClaimOrReadAsync(request, prospectiveDecisionId, cancellationToken);
        if (claim.Outcome == "ExistingMatch")
        {
            if (claim.ExportDecisionId is null)
            {
                await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
            }

            var existing = await LoadGraphAsync(claim.ExportDecisionId!.Value, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }

        if (claim.Outcome == "FingerprintConflict")
        {
            if (claim.ExportDecisionId is not null)
            {
                await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
            }

            await RollbackAndThrowAsync(
                transaction,
                "RAW_EXPORT_AUTHORIZATION_IDEMPOTENCY_CONFLICT",
                cancellationToken);
        }

        if (claim.Outcome != "NewClaim" || claim.ExportDecisionId != prospectiveDecisionId)
        {
            await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
        }

        var evidence = new AuthorizationEvidence();
        var lockedSession = await LockSessionAsync(
            request.Command.RequestedVerificationSessionId,
            cancellationToken);
        if (lockedSession is null)
        {
            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.SESSION_NOT_FOUND,
                    evidence),
                cancellationToken);
        }

        evidence.Session = lockedSession;
        if (lockedSession.ClientApplicationId != request.Command.Actor.ClientApplicationId)
        {
            evidence.SuppressSessionSubject = true;
            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.SESSION_NOT_OWNED,
                    evidence),
                cancellationToken);
        }

        if (!string.Equals(
                lockedSession.State,
                VerificationSessionState.Completed.ToString(),
                StringComparison.Ordinal))
        {
            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.SESSION_NOT_COMPLETED,
                    evidence),
                cancellationToken);
        }

        if (!permitTtlBounds.IsValid)
        {
            await RollbackAndThrowAsync(
                transaction,
                RawExportPermitTtlBoundsState.InvalidCode,
                cancellationToken);
        }

        evidence.Eligibility = await controlPlane.ResolveExportEligibilityForAuthorizationAsync(
            request.Command.Actor.PrincipalId,
            request.Command.PolicyId,
            request.Command.PolicyVersion,
            cancellationToken);
        await AssertTransactionTimestampAsync(
            transaction,
            evidence.Eligibility.EvaluatedAtUtc,
            cancellationToken);

        if (evidence.Eligibility.State != RawExportEligibilityState.Active)
        {
            if (evidence.Eligibility.PrimaryCause is null || evidence.Eligibility.Causes.Count == 0)
            {
                await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
            }

            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.EXPORT_ELIGIBILITY_INACTIVE,
                    evidence),
                cancellationToken);
        }

        if (evidence.Eligibility.PrimaryCause is not null || evidence.Eligibility.Causes.Count != 0)
        {
            await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
        }

        evidence.Policy = await policies.GetVersionAsync(
            request.Command.PolicyId,
            request.Command.PolicyVersion,
            cancellationToken);
        if (evidence.Policy is null || evidence.Policy.Status != RawExportPolicyStatus.CatalogApproved)
        {
            await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
        }

        evidence.PolicyAllowedClasses = CanonicalizeClasses(evidence.Policy!.AllowedClasses);
        if (evidence.Policy.PermitTtlSeconds is null ||
            !permitTtlBounds.Allows(evidence.Policy.PermitTtlSeconds.Value))
        {
            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.POLICY_PERMIT_TTL_INVALID,
                    evidence),
                cancellationToken);
        }

        evidence.EffectiveRequestedClasses =
            request.SelectionMode == RawExportRawClassSelectionMode.DefaultPolicySet
                ? evidence.PolicyAllowedClasses
                : request.RequestedRawClasses;
        if (!evidence.EffectiveRequestedClasses.All(evidence.Policy.AllowedClasses.Contains))
        {
            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.REQUESTED_RAW_CLASSES_NOT_ALLOWED,
                    evidence),
                cancellationToken);
        }

        evidence.Consent = await subjectConsent.ResolveSubjectExportConsentForAuthorizationAsync(
            request.Command.RequestedVerificationSessionId,
            request.Command.PolicyId,
            request.Command.PolicyVersion,
            cancellationToken);
        await AssertConsentIntegrityAsync(transaction, request, evidence, cancellationToken);
        evidence.ConsentedClasses = CanonicalizeClasses(evidence.Consent.ConsentedRawClasses);

        if (evidence.Consent.State != RawExportSubjectConsentState.Effective)
        {
            if (evidence.Consent.Cause is null)
            {
                await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
            }

            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.SUBJECT_CONSENT_NOT_EFFECTIVE,
                    evidence),
                cancellationToken);
        }

        if (evidence.Consent.Cause is not null)
        {
            await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
        }

        if (!evidence.EffectiveRequestedClasses.All(evidence.Consent.ConsentedRawClasses.Contains))
        {
            return await PersistLoadAndCommitAsync(
                transaction,
                BuildPayload(
                    request,
                    prospectiveDecisionId,
                    RawExportAuthorizationOutcome.Denied,
                    RawExportAuthorizationPrimaryCause.SUBJECT_CONSENT_CLASS_COVERAGE_INSUFFICIENT,
                    evidence),
                cancellationToken);
        }

        evidence.DecisionExpiresAtUtc = ComputeExpiry(evidence);
        return await PersistLoadAndCommitAsync(
            transaction,
            BuildPayload(
                request,
                prospectiveDecisionId,
                RawExportAuthorizationOutcome.Authorized,
                primaryCause: null,
                evidence),
            cancellationToken);
    }

    public async Task<Guid> PersistAuthorizationDecisionAsync(
        RawExportAuthorizationPersistencePayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_REQUIRES_AMBIENT_TRANSACTION");
        }

        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_persist_authorization_decision(@payload);",
            connection,
            (NpgsqlTransaction)db.Database.CurrentTransaction.GetDbTransaction());
        command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value =
            JsonSerializer.Serialize(payload, PayloadJsonOptions);

        var persisted = (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_PERSIST_RETURN_MISSING"));
        if (persisted != payload.ExportDecisionId)
        {
            throw new InvalidOperationException("RAW_EXPORT_AUTHORIZATION_PERSIST_ID_MISMATCH");
        }

        return persisted;
    }

    private static PreparedRequest PrepareRequest(AuthorizeRawExportCommand command)
    {
        if (command is null ||
            command.Actor is null ||
            command.Actor.PrincipalId == Guid.Empty ||
            command.Actor.ClientApplicationId == Guid.Empty ||
            command.Actor.ApiKeyId == Guid.Empty ||
            command.RequestedVerificationSessionId == Guid.Empty ||
            command.PolicyId == Guid.Empty ||
            command.PolicyVersion < 1 ||
            command.IdempotencyKey is null ||
            !IdempotencyKeyPattern.IsMatch(command.IdempotencyKey))
        {
            throw new RawExportAuthorizationInputException();
        }

        var selectionMode = command.RequestedRawClasses is null
            ? RawExportRawClassSelectionMode.DefaultPolicySet
            : RawExportRawClassSelectionMode.ExplicitSubset;
        IReadOnlyList<RawExportRawClass> requestedClasses = [];
        if (command.RequestedRawClasses is not null)
        {
            var supplied = command.RequestedRawClasses.ToArray();
            if (supplied.Length == 0 ||
                supplied.Any(rawClass => !Enum.IsDefined(rawClass)) ||
                supplied.Distinct().Count() != supplied.Length)
            {
                throw new RawExportAuthorizationInputException();
            }

            requestedClasses = CanonicalizeClasses(supplied);
        }

        var fingerprint = RawExportAuthorizationFingerprintCodec.ComputeHash(
            command.Actor.PrincipalId,
            command.Actor.ClientApplicationId,
            command.RequestedVerificationSessionId,
            command.PolicyId,
            command.PolicyVersion,
            RawExportSubjectConsentConstants.PurposeCode,
            selectionMode,
            selectionMode == RawExportRawClassSelectionMode.DefaultPolicySet ? null : requestedClasses);
        return new PreparedRequest(
            command,
            selectionMode,
            requestedClasses,
            fingerprint,
            Convert.ToHexString(fingerprint).ToLowerInvariant());
    }

    private async Task SetActorContextAsync(Guid actorPrincipalId, CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id', @actor, true);",
            connection,
            CurrentTransaction());
        command.Parameters.AddWithValue("actor", actorPrincipalId.ToString("D"));
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<ClaimResult> ClaimOrReadAsync(
        PreparedRequest request,
        Guid prospectiveDecisionId,
        CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT outcome, export_decision_id
            FROM tagekyc.raw_export_claim_or_read_authorization_idempotency(
                @principalId,@clientApplicationId,@requestedSessionId,@idempotencyKey,
                @fingerprintHash,@prospectiveDecisionId);
            """,
            connection,
            CurrentTransaction());
        command.Parameters.AddWithValue("principalId", request.Command.Actor.PrincipalId);
        command.Parameters.AddWithValue("clientApplicationId", request.Command.Actor.ClientApplicationId);
        command.Parameters.AddWithValue("requestedSessionId", request.Command.RequestedVerificationSessionId);
        command.Parameters.AddWithValue("idempotencyKey", request.Command.IdempotencyKey);
        command.Parameters.Add("fingerprintHash", NpgsqlDbType.Bytea).Value = request.FingerprintHash;
        command.Parameters.AddWithValue("prospectiveDecisionId", prospectiveDecisionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new RawExportAuthorizationException(InvariantFailureCode);
        }

        var result = new ClaimResult(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetGuid(1));
        if (await reader.ReadAsync(cancellationToken))
        {
            throw new RawExportAuthorizationException(InvariantFailureCode);
        }

        return result;
    }

    private async Task<LockedSession?> LockSessionAsync(
        Guid verificationSessionId,
        CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT "ClientApplicationId","SubjectRef","State"
            FROM tagekyc.raw_export_lock_verification_session_for_authorization(@sessionId);
            """,
            connection,
            CurrentTransaction());
        command.Parameters.AddWithValue("sessionId", verificationSessionId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var result = new LockedSession(reader.GetGuid(0), reader.GetString(1), reader.GetString(2));
        if (await reader.ReadAsync(cancellationToken))
        {
            throw new RawExportAuthorizationException(InvariantFailureCode);
        }

        return result;
    }

    private async Task AssertConsentIntegrityAsync(
        IDbContextTransaction transaction,
        PreparedRequest request,
        AuthorizationEvidence evidence,
        CancellationToken cancellationToken)
    {
        var consent = evidence.Consent!;
        var session = evidence.Session!;
        var validHash = consent.ConsentRef is null ||
            LowerHex64Pattern.IsMatch(consent.ConsentRef.ConsentScopeHash);
        if (consent.VerificationSessionId != request.Command.RequestedVerificationSessionId ||
            !string.Equals(consent.SubjectRef, session.SubjectRef, StringComparison.Ordinal) ||
            consent.PolicyId != request.Command.PolicyId ||
            consent.PolicyVersion != request.Command.PolicyVersion ||
            !string.Equals(
                consent.PurposeCode,
                RawExportSubjectConsentConstants.PurposeCode,
                StringComparison.Ordinal) ||
            consent.RecipientClientApplicationId != session.ClientApplicationId ||
            consent.RecipientClientApplicationId != request.Command.Actor.ClientApplicationId ||
            consent.EvaluatedAtUtc != evidence.Eligibility!.EvaluatedAtUtc ||
            !validHash)
        {
            await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
        }
    }

    private async Task AssertTransactionTimestampAsync(
        IDbContextTransaction transaction,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT transaction_timestamp();",
            connection,
            CurrentTransaction());
        var value = await command.ExecuteScalarAsync(cancellationToken);
        var transactionTimestamp = value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => throw new RawExportAuthorizationException(InvariantFailureCode),
        };
        if (transactionTimestamp != evaluatedAtUtc)
        {
            await RollbackAndThrowAsync(transaction, InvariantFailureCode, cancellationToken);
        }
    }

    private static DateTimeOffset ComputeExpiry(AuthorizationEvidence evidence)
    {
        var policyExpiry = evidence.Eligibility!.EvaluatedAtUtc.AddSeconds(
            evidence.Policy!.PermitTtlSeconds!.Value);
        var candidates = new List<DateTimeOffset> { policyExpiry };
        if (evidence.Consent!.ValidUntilUtc is not null)
        {
            candidates.Add(evidence.Consent.ValidUntilUtc.Value);
        }

        var fulfillmentExpiry = evidence.Eligibility.FulfillmentRefs
            .Where(reference => reference.ValidUntilUtc is not null)
            .Select(reference => reference.ValidUntilUtc!.Value)
            .DefaultIfEmpty()
            .Min();
        if (fulfillmentExpiry != default)
        {
            candidates.Add(fulfillmentExpiry);
        }

        return candidates.Min();
    }

    private static RawExportAuthorizationPersistencePayload BuildPayload(
        PreparedRequest request,
        Guid exportDecisionId,
        RawExportAuthorizationOutcome outcome,
        RawExportAuthorizationPrimaryCause? primaryCause,
        AuthorizationEvidence evidence)
    {
        var eligibility = evidence.Eligibility;
        var consent = evidence.Consent;
        var session = evidence.Session;
        var policy = evidence.Policy;
        var classes = new List<RawExportAuthorizationClassPayload>();
        if (request.SelectionMode == RawExportRawClassSelectionMode.ExplicitSubset)
        {
            AddClassSet(classes, RawExportAuthorizationClassKind.Requested, request.RequestedRawClasses);
        }

        if (evidence.PolicyAllowedClasses is not null)
        {
            AddClassSet(classes, RawExportAuthorizationClassKind.PolicyAllowed, evidence.PolicyAllowedClasses);
        }

        if (evidence.EffectiveRequestedClasses is not null)
        {
            AddClassSet(classes, RawExportAuthorizationClassKind.Effective, evidence.EffectiveRequestedClasses);
        }

        if (evidence.ConsentedClasses is not null)
        {
            AddClassSet(classes, RawExportAuthorizationClassKind.Consented, evidence.ConsentedClasses);
        }

        if (outcome == RawExportAuthorizationOutcome.Authorized)
        {
            AddClassSet(classes, RawExportAuthorizationClassKind.Authorized, evidence.EffectiveRequestedClasses!);
        }

        var eligibilityCauses = eligibility?.Causes
            .Select((cause, ordinal) =>
                new RawExportAuthorizationEligibilityCausePayload(ordinal, cause.ToString()))
            .ToArray() ?? [];
        var fulfillmentRefs = eligibility?.FulfillmentRefs
            .Select((reference, ordinal) => new RawExportAuthorizationFulfillmentRefPayload(
                ordinal,
                reference.RequirementType.ToString(),
                reference.FulfillmentEventId,
                reference.Revision,
                reference.ArtifactRef,
                reference.ArtifactVersion,
                reference.ValidUntilUtc))
            .ToArray() ?? [];

        var decision = new RawExportAuthorizationDecisionPayload(
            request.Command.Actor.PrincipalId,
            request.Command.Actor.ClientApplicationId,
            request.Command.Actor.ApiKeyId,
            request.Command.RequestedVerificationSessionId,
            request.Command.PolicyId,
            request.Command.PolicyVersion,
            request.FingerprintHex,
            request.SelectionMode.ToString(),
            outcome.ToString(),
            primaryCause?.ToString(),
            session is null ? null : request.Command.RequestedVerificationSessionId,
            session?.ClientApplicationId,
            evidence.SuppressSessionSubject ? null : session?.SubjectRef,
            session?.State,
            eligibility?.BoundRuleSetVersion,
            eligibility?.CurrentRuleSetVersion,
            eligibility?.PrimaryCause?.ToString(),
            eligibility?.EvaluatedAtUtc,
            eligibility?.GrantRef?.PrincipalId,
            eligibility?.GrantRef?.PolicyId,
            eligibility?.GrantRef?.PolicyVersion,
            eligibility?.GrantRef?.Revision,
            eligibility?.LifecycleRef?.PolicyId,
            eligibility?.LifecycleRef?.PolicyVersion,
            eligibility?.LifecycleRef?.Revision,
            consent?.PurposeCode,
            consent?.RecipientClientApplicationId,
            consent?.Cause?.ToString(),
            consent?.ConsentRef?.ConsentScopeHash,
            consent?.ConsentRef?.SubjectConsentRecordId,
            consent?.ConsentRef?.Revision,
            consent?.ValidFromUtc,
            consent?.ValidUntilUtc,
            consent?.EvaluatedAtUtc,
            policy?.PermitTtlSeconds,
            evidence.DecisionExpiresAtUtc);

        RawExportAuthorizationPermitPayload? permit = null;
        IReadOnlyList<RawExportAuthorizationPermitClassPayload>? permitClasses = null;
        if (outcome == RawExportAuthorizationOutcome.Authorized)
        {
            permit = new RawExportAuthorizationPermitPayload(
                Guid.NewGuid(),
                request.Command.RequestedVerificationSessionId,
                session!.SubjectRef,
                request.Command.PolicyId,
                request.Command.PolicyVersion,
                RawExportSubjectConsentConstants.PurposeCode,
                request.Command.Actor.ClientApplicationId,
                evidence.DecisionExpiresAtUtc!.Value,
                SchemaVersion: 1);
            permitClasses = evidence.EffectiveRequestedClasses!
                .Select((rawClass, ordinal) =>
                    new RawExportAuthorizationPermitClassPayload(rawClass.ToString(), ordinal))
                .ToArray();
        }

        return new RawExportAuthorizationPersistencePayload(
            PayloadSchemaVersion: 1,
            exportDecisionId,
            new RawExportAuthorizationIdempotencyIdentityPayload(
                request.Command.Actor.PrincipalId,
                request.Command.Actor.ClientApplicationId,
                request.Command.RequestedVerificationSessionId,
                request.Command.IdempotencyKey),
            decision,
            eligibilityCauses,
            fulfillmentRefs,
            classes,
            permit,
            permitClasses);
    }

    private static void AddClassSet(
        ICollection<RawExportAuthorizationClassPayload> target,
        RawExportAuthorizationClassKind kind,
        IReadOnlyList<RawExportRawClass> classes)
    {
        foreach (var item in classes.Select((rawClass, ordinal) => (rawClass, ordinal)))
        {
            target.Add(new RawExportAuthorizationClassPayload(
                kind.ToString(),
                item.rawClass.ToString(),
                item.ordinal));
        }
    }

    private async Task<RawExportAuthorizationResult> PersistLoadAndCommitAsync(
        IDbContextTransaction transaction,
        RawExportAuthorizationPersistencePayload payload,
        CancellationToken cancellationToken)
    {
        await PersistAuthorizationDecisionAsync(payload, cancellationToken);
        var result = await LoadGraphAsync(payload.ExportDecisionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<RawExportAuthorizationResult> LoadGraphAsync(
        Guid exportDecisionId,
        CancellationToken cancellationToken)
    {
        var row = await db.RawExportAuthorizationDecisions.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ExportDecisionId == exportDecisionId,
                cancellationToken)
            ?? throw new RawExportAuthorizationException(InvariantFailureCode);
        var eligibilityCauses = await db.RawExportDecisionEligibilityCauses.AsNoTracking()
            .Where(item => item.ExportDecisionId == exportDecisionId)
            .OrderBy(item => item.Ordinal)
            .Select(item => new RawExportAuthorizationEligibilityCause(
                item.Ordinal,
                Enum.Parse<RawExportEligibilityCause>(item.Cause)))
            .ToListAsync(cancellationToken);
        var fulfillmentRefs = await db.RawExportDecisionFulfillmentRefs.AsNoTracking()
            .Where(item => item.ExportDecisionId == exportDecisionId)
            .OrderBy(item => item.Ordinal)
            .Select(item => new RawExportAuthorizationFulfillmentRef(
                item.Ordinal,
                Enum.Parse<RawExportRequirementType>(item.RequirementType),
                item.FulfillmentEventId,
                item.Revision,
                item.ArtifactRef,
                item.ArtifactVersion,
                item.ValidUntilUtc))
            .ToListAsync(cancellationToken);
        var classes = await db.RawExportDecisionClasses.AsNoTracking()
            .Where(item => item.ExportDecisionId == exportDecisionId)
            .OrderBy(item => item.ClassKind)
            .ThenBy(item => item.Ordinal)
            .Select(item => new RawExportAuthorizationClassSnapshot(
                Enum.Parse<RawExportAuthorizationClassKind>(item.ClassKind),
                Enum.Parse<RawExportRawClass>(item.RawClass),
                item.Ordinal))
            .ToListAsync(cancellationToken);
        var permitRow = await db.RawExportAuthorizationPermits.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.AuthorizationDecisionId == exportDecisionId,
                cancellationToken);
        var permitClasses = permitRow is null
            ? []
            : await db.RawExportPermitClasses.AsNoTracking()
                .Where(item => item.PermitId == permitRow.PermitId)
                .OrderBy(item => item.Ordinal)
                .Select(item => new RawExportAuthorizationPermitClass(
                    Enum.Parse<RawExportRawClass>(item.RawClass),
                    item.Ordinal))
                .ToListAsync(cancellationToken);

        var grantRef = row.GrantPrincipalId is not null &&
                       row.GrantPolicyId is not null &&
                       row.GrantPolicyVersion is not null &&
                       row.GrantRevision is not null
            ? new RawExportGrantRef(
                row.GrantPrincipalId.Value,
                row.GrantPolicyId.Value,
                row.GrantPolicyVersion.Value,
                row.GrantRevision.Value)
            : null;
        var lifecycleRef = row.LifecyclePolicyId is not null &&
                           row.LifecyclePolicyVersion is not null &&
                           row.LifecycleRevision is not null
            ? new RawExportLifecycleRef(
                row.LifecyclePolicyId.Value,
                row.LifecyclePolicyVersion.Value,
                row.LifecycleRevision.Value)
            : null;
        var consentRef = row.SubjectConsentRecordId is not null &&
                         row.ConsentScopeHash is not null &&
                         row.ConsentRevision is not null
            ? new RawExportSubjectConsentRef(
                row.SubjectConsentRecordId.Value,
                Convert.ToHexString(row.ConsentScopeHash).ToLowerInvariant(),
                row.ConsentRevision.Value)
            : null;
        var decision = new RawExportAuthorizationDecision(
            row.ExportDecisionId,
            row.PrincipalId,
            row.ClientApplicationId,
            row.ApiKeyId,
            row.RequestedVerificationSessionId,
            row.PolicyId,
            row.PolicyVersion,
            row.FingerprintHash,
            Enum.Parse<RawExportRawClassSelectionMode>(row.RawClassSelectionMode),
            Enum.Parse<RawExportAuthorizationOutcome>(row.Outcome),
            row.PrimaryCause is null
                ? null
                : Enum.Parse<RawExportAuthorizationPrimaryCause>(row.PrimaryCause),
            row.ResolvedVerificationSessionId,
            row.SessionOwnerClientApplicationId,
            row.SessionSubjectRef,
            row.SessionState is null ? null : Enum.Parse<VerificationSessionState>(row.SessionState),
            row.BoundRuleSetVersion,
            row.CurrentRuleSetVersion,
            row.EligibilityPrimaryCause is null
                ? null
                : Enum.Parse<RawExportEligibilityCause>(row.EligibilityPrimaryCause),
            row.EligibilityEvaluatedAtUtc,
            grantRef,
            lifecycleRef,
            row.PurposeCode,
            row.RecipientClientApplicationId,
            row.SubjectConsentCause is null
                ? null
                : Enum.Parse<RawExportSubjectConsentCause>(row.SubjectConsentCause),
            row.ConsentScopeHash is null
                ? null
                : Convert.ToHexString(row.ConsentScopeHash).ToLowerInvariant(),
            consentRef,
            row.ConsentValidFromUtc,
            row.ConsentValidUntilUtc,
            row.ConsentEvaluatedAtUtc,
            row.PolicyPermitTtlSeconds,
            row.DecisionExpiresAtUtc,
            row.DecidedAtUtc);
        var permit = permitRow is null
            ? null
            : new RawExportAuthorizationPermit(
                permitRow.PermitId,
                permitRow.AuthorizationDecisionId,
                permitRow.ResolvedVerificationSessionId,
                permitRow.SubjectRef,
                permitRow.PolicyId,
                permitRow.PolicyVersion,
                permitRow.PurposeCode,
                permitRow.RecipientClientApplicationId,
                permitRow.DecisionExpiresAtUtc,
                permitRow.SchemaVersion,
                permitRow.CreatedAt);
        return new RawExportAuthorizationResult(
            decision,
            eligibilityCauses,
            fulfillmentRefs,
            classes,
            permit,
            permitClasses);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private NpgsqlTransaction CurrentTransaction() =>
        (NpgsqlTransaction)(db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new RawExportAuthorizationException(InvariantFailureCode));

    private static IReadOnlyList<RawExportRawClass> CanonicalizeClasses(
        IEnumerable<RawExportRawClass> classes) =>
        classes.Distinct().OrderBy(rawClass => (int)rawClass).ToArray();

    private static async Task RollbackAndThrowAsync(
        IDbContextTransaction transaction,
        string code,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        throw new RawExportAuthorizationException(code);
    }

    private sealed record PreparedRequest(
        AuthorizeRawExportCommand Command,
        RawExportRawClassSelectionMode SelectionMode,
        IReadOnlyList<RawExportRawClass> RequestedRawClasses,
        byte[] FingerprintHash,
        string FingerprintHex);

    private sealed record ClaimResult(string Outcome, Guid? ExportDecisionId);

    private sealed record LockedSession(
        Guid ClientApplicationId,
        string SubjectRef,
        string State);

    private sealed class AuthorizationEvidence
    {
        public LockedSession? Session { get; set; }
        public bool SuppressSessionSubject { get; set; }
        public RawExportEligibilitySnapshot? Eligibility { get; set; }
        public RawExportPolicyVersion? Policy { get; set; }
        public IReadOnlyList<RawExportRawClass>? PolicyAllowedClasses { get; set; }
        public IReadOnlyList<RawExportRawClass>? EffectiveRequestedClasses { get; set; }
        public RawExportSubjectConsentSnapshot? Consent { get; set; }
        public IReadOnlyList<RawExportRawClass>? ConsentedClasses { get; set; }
        public DateTimeOffset? DecisionExpiresAtUtc { get; set; }
    }
}
