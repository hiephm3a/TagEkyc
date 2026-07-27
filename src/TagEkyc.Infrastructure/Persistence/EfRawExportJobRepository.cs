using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportJobRepository : IRawExportJobRepository
{
    private const string IsolationInvalid = "RAW_EXPORT_JOB_TRANSACTION_ISOLATION_INVALID";
    private const string RequestInvalid = "RAW_EXPORT_JOB_REQUEST_VALIDATION_FAILED";
    private const string GraphInvalid = "RAW_EXPORT_JOB_GRAPH_INVARIANT_FAILURE";
    private const string AuthorityInvalid = "RAW_EXPORT_JOB_AUTHORITY_NOT_EFFECTIVE";
    private static readonly Regex IdempotencyPattern =
        new("^[A-Za-z0-9._:-]{1,256}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly TagEkycDbContext db;
    private readonly IRawExportControlPlaneRepository controlPlane;
    private readonly IRawExportAuthorizationProjectionReader projections;
    private readonly IRawExportSubjectConsentRepository consent;
    private readonly RawExportJobLeaseState lease;

    public EfRawExportJobRepository(
        TagEkycDbContext db,
        IRawExportControlPlaneRepository controlPlane,
        IRawExportAuthorizationProjectionReader projections,
        IRawExportSubjectConsentRepository consent,
        RawExportJobLeaseState lease)
    {
        this.db = db;
        this.controlPlane = controlPlane;
        this.projections = projections;
        this.consent = consent;
        this.lease = lease;
    }

    public Task<RawExportJobBindResult> BindAsync(
        BindRawExportJobCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(command.Actor);
        Require(command.PermitId);
        if (command.IdempotencyKey is null || !IdempotencyPattern.IsMatch(command.IdempotencyKey))
        {
            throw Error(RequestInvalid);
        }

        return ExecuteAsync(
            command.Actor.PrincipalId,
            (connection, transaction, token) => BindCoreAsync(command, connection, transaction, token),
            cancellationToken);
    }

    public Task<RawExportJobReadResult> ReadAsync(
        ReadRawExportJobCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(command.Actor);
        Require(command.JobId);
        return ExecuteAsync(
            command.Actor.PrincipalId,
            (connection, transaction, token) => ReadCoreAsync(command.Actor, command.JobId, connection, transaction, token),
            cancellationToken);
    }

    public Task<RawExportJobLeaseResult> AcquireOrReclaimLeaseAsync(
        AcquireOrReclaimRawExportJobLeaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateLeaseCommand(command.Actor, command.JobId, command.ExpectedRevision, command.ExpectedFencingToken, command.LeaseOwnerId);
        ValidateLeaseState();
        return ExecuteAsync(
            command.Actor.PrincipalId,
            (connection, transaction, token) => AcquireCoreAsync(command, connection, transaction, token),
            cancellationToken);
    }

    public Task<RawExportJobRenewResult> RenewLeaseAsync(
        RenewRawExportJobLeaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateLeaseCommand(command.Actor, command.JobId, command.ExpectedRevision, command.ExpectedFencingToken, command.LeaseOwnerId);
        Require(command.AttemptId);
        ValidateLeaseState();
        return ExecuteAsync(
            command.Actor.PrincipalId,
            async (connection, transaction, token) =>
            {
                var row = await MutationAsync(
                    connection,
                    transaction,
                    "SELECT * FROM tagekyc.raw_export_renew_job_lease(@job,@principal,@client,@revision,@fence,@attempt,@owner,@seconds);",
                    token,
                    P("job", command.JobId), P("principal", command.Actor.PrincipalId), P("client", command.Actor.ClientApplicationId),
                    P("revision", command.ExpectedRevision), P("fence", command.ExpectedFencingToken), P("attempt", command.AttemptId),
                    P("owner", command.LeaseOwnerId), P("seconds", lease.LeaseSeconds));
                ThrowMutation(row.Outcome);
                if (row.Outcome != "Renewed" || row.Revision is null || row.Fence is null || row.ExpiresAt is null)
                {
                    throw Error(GraphInvalid);
                }

                return new RawExportJobRenewResult(RawExportJobRenewStatus.Renewed, row.Revision.Value, row.Fence.Value, row.ExpiresAt.Value);
            },
            cancellationToken);
    }

    public Task<RawExportJobAttemptFailureResult> RecordAttemptFailureAsync(
        RecordRawExportJobAttemptFailureCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateLeaseCommand(command.Actor, command.JobId, command.ExpectedRevision, command.ExpectedFencingToken, command.LeaseOwnerId);
        Require(command.AttemptId);
        if (!Enum.IsDefined(command.FailureCode))
        {
            throw Error(RequestInvalid);
        }

        return ExecuteAsync<RawExportJobAttemptFailureResult>(
            command.Actor.PrincipalId,
            async (connection, transaction, token) =>
            {
                var row = await MutationAsync(
                    connection,
                    transaction,
                    "SELECT * FROM tagekyc.raw_export_record_job_attempt_failure(@job,@principal,@client,@revision,@fence,@attempt,@owner,@code);",
                    token,
                    P("job", command.JobId), P("principal", command.Actor.PrincipalId), P("client", command.Actor.ClientApplicationId),
                    P("revision", command.ExpectedRevision), P("fence", command.ExpectedFencingToken), P("attempt", command.AttemptId),
                    P("owner", command.LeaseOwnerId), P("code", command.FailureCode.ToString()));
                ThrowMutation(row.Outcome);
                if (row.Revision is null || row.Fence is null)
                {
                    throw Error(GraphInvalid);
                }

                return row.Outcome switch
                {
                    "Recorded" => new(RawExportJobAttemptFailureStatus.Recorded, RawExportJobState.Assembling, row.Revision.Value, row.Fence.Value, null, null),
                    "TerminalizedModeRetryForbidden" => new(RawExportJobAttemptFailureStatus.TerminalFailed, RawExportJobState.TerminalFailed, row.Revision.Value, row.Fence.Value, RawExportJobTerminalReasonCode.MODE_RETRY_NOT_AUTHORIZED, "RAW_EXPORT_JOB_MODE_RETRY_NOT_AUTHORIZED"),
                    _ => throw Error(GraphInvalid),
                };
            },
            cancellationToken);
    }

    public Task<RawExportJobTerminalizeResult> TerminalizeAsync(
        TerminalizeRawExportJobCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(command.Actor);
        Require(command.JobId);
        if (command.ExpectedRevision < 0 || command.ExpectedFencingToken < 0 ||
            !Enum.IsDefined(command.TerminalState) || !Enum.IsDefined(command.ReasonCode) ||
            command.LeaseOwnerId is not null && command.AttemptId is null ||
            !ValidTerminalPair(command.TerminalState, command.ReasonCode))
        {
            throw Error(RequestInvalid);
        }

        return ExecuteAsync(
            command.Actor.PrincipalId,
            (connection, transaction, token) => TerminalizeCoreAsync(command, connection, transaction, token),
            cancellationToken);
    }

    private async Task<RawExportJobBindResult> BindCoreAsync(
        BindRawExportJobCommand command,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var projection = await ReadBindingProjectionAsync(command, connection, transaction, cancellationToken);
        var frozen = await ParseBindingProjectionAsync(command, projection, connection, transaction, cancellationToken);
        if (frozen.TerminalResult is not null)
        {
            return frozen.TerminalResult;
        }

        if (frozen.ExistingResult is not null)
        {
            return frozen.ExistingResult;
        }

        var lockedSession = await LockSessionAsync(
            frozen.VerificationSessionId,
            connection,
            transaction,
            cancellationToken);
        if (lockedSession is null ||
            lockedSession.ClientApplicationId != command.Actor.ClientApplicationId ||
            !string.Equals(lockedSession.SubjectRef, frozen.SubjectRef, StringComparison.Ordinal) ||
            !string.Equals(lockedSession.State, VerificationSessionState.Completed.ToString(), StringComparison.Ordinal))
        {
            throw Error(AuthorityInvalid);
        }

        projection = await ReadBindingProjectionAsync(command, connection, transaction, cancellationToken);
        frozen = await ParseBindingProjectionAsync(command, projection, connection, transaction, cancellationToken);
        if (frozen.TerminalResult is not null)
        {
            return frozen.TerminalResult;
        }

        if (frozen.ExistingResult is not null)
        {
            return frozen.ExistingResult;
        }

        var authority = await ResolveAuthorityAsync(frozen, cancellationToken);
        var preClaimNow = await ReadDatabaseTimeAsync(connection, transaction, cancellationToken);
        if (!AuthorityIsEffectiveAt(authority, preClaimNow) || preClaimNow >= frozen.PermitExpiresAt)
        {
            throw Error(AuthorityInvalid);
        }

        await using var sql = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.raw_export_claim_or_read_job(
              @job,@permit,@decision,@principal,@client,@apiKey,@session,@subject,@policy,@version,@purpose,@recipient,@mode,@permitExpiry,@jobExpiry,@key,@hash,@classes);
            """,
            connection,
            transaction);
        sql.Parameters.AddRange(new NpgsqlParameter[]
        {
            P("job", Guid.NewGuid()), P("permit", command.PermitId), P("decision", frozen.AuthorizationDecisionId),
            P("principal", command.Actor.PrincipalId), P("client", command.Actor.ClientApplicationId), P("apiKey", command.Actor.ApiKeyId),
            P("session", frozen.VerificationSessionId), P("subject", frozen.SubjectRef), P("policy", frozen.PolicyId),
            P("version", frozen.PolicyVersion), P("purpose", frozen.PurposeCode), P("recipient", frozen.RecipientClientApplicationId),
            P("mode", frozen.Mode.ToString()), P("permitExpiry", frozen.PermitExpiresAt), P("jobExpiry", frozen.PermitExpiresAt),
            P("key", command.IdempotencyKey), P("hash", frozen.Fingerprint), new NpgsqlParameter("classes", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = frozen.Classes.Select(value => value.ToString()).ToArray() },
        });
        await using var reader = await ExecuteReaderAsync(sql, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw Error(GraphInvalid);
        }

        var outcome = reader.GetString(0);
        Guid? jobId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
        if (await reader.ReadAsync(cancellationToken))
        {
            throw Error(GraphInvalid);
        }
        await reader.DisposeAsync();

        if (outcome == "NewJob" && jobId is not null)
        {
            var postClaimNow = await ReadDatabaseTimeAsync(connection, transaction, cancellationToken);
            if (!AuthorityIsEffectiveAt(authority, postClaimNow))
            {
                throw Error(AuthorityInvalid);
            }

            return new(RawExportJobBindStatus.NewJob, jobId.Value, null);
        }

        return outcome switch
        {
            "ExistingMatch" when jobId is not null => new(RawExportJobBindStatus.ExistingMatch, jobId.Value, null),
            "FingerprintConflict" when jobId is null => throw Error("RAW_EXPORT_JOB_IDEMPOTENCY_CONFLICT"),
            "AuthorityNotEffective" when jobId is null => throw Error(AuthorityInvalid),
            _ => throw Error(GraphInvalid),
        };
    }

    private async Task<RawExportJobLeaseResult> AcquireCoreAsync(
        AcquireOrReclaimRawExportJobLeaseCommand command,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        string lockOutcome;
        await using (var lockCommand = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_lock_job_for_attempt(@job,@principal,@client,@revision,@fence);",
            connection,
            transaction))
        {
            lockCommand.Parameters.AddRange(new NpgsqlParameter[]
            {
                P("job", command.JobId),
                P("principal", command.Actor.PrincipalId),
                P("client", command.Actor.ClientApplicationId),
                P("revision", command.ExpectedRevision),
                P("fence", command.ExpectedFencingToken),
            });
            await using var reader = await ExecuteReaderAsync(lockCommand, cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw Error(GraphInvalid);
            }
            lockOutcome = reader.GetString(0);
        }

        if (lockOutcome == "NotFound") return new(RawExportJobLeaseStatus.NotFound, null, null, null, null, null, null, null);
        if (lockOutcome == "AlreadyTerminal")
        {
            var read = await ReadCoreAsync(command.Actor, command.JobId, connection, transaction, cancellationToken);
            return TerminalLeaseResult(read);
        }
        if (lockOutcome != "Locked") ThrowMutation(lockOutcome);

        var current = await ReadCoreAsync(command.Actor, command.JobId, connection, transaction, cancellationToken);
        if (current.Status != RawExportJobReadStatus.Found || current.Job is null)
        {
            throw Error(GraphInvalid);
        }

        var identity = current.Job.Identity;
        var head = current.Job.Head;
        var databaseNow = await ReadDatabaseTimeAsync(connection, transaction, cancellationToken);
        if (databaseNow >= identity.JobExpiresAt || databaseNow >= identity.PermitExpiresAt)
        {
            var terminal = await TerminalizeCoreAsync(
                new(command.Actor, command.JobId, head.Revision, head.FencingToken, head.CurrentAttemptId, head.LeaseOwnerId,
                    RawExportJobState.Expired, RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED),
                connection,
                transaction,
                cancellationToken);
            return TerminalLeaseResult(terminal);
        }

        try
        {
            var lockedSession = await LockSessionAsync(
                identity.VerificationSessionId,
                connection,
                transaction,
                cancellationToken);
            if (lockedSession is null ||
                lockedSession.ClientApplicationId != identity.ClientApplicationId ||
                !string.Equals(lockedSession.SubjectRef, identity.SubjectRef, StringComparison.Ordinal) ||
                !string.Equals(lockedSession.State, VerificationSessionState.Completed.ToString(), StringComparison.Ordinal))
            {
                throw Error(AuthorityInvalid);
            }

            var frozen = FrozenBinding.FromJob(current.Job);
            var authority = await ResolveAuthorityAsync(frozen, cancellationToken);
            databaseNow = await ReadDatabaseTimeAsync(connection, transaction, cancellationToken);
            if (databaseNow >= identity.JobExpiresAt || databaseNow >= identity.PermitExpiresAt)
            {
                var expired = await TerminalizeCoreAsync(
                    new(command.Actor, command.JobId, head.Revision, head.FencingToken, head.CurrentAttemptId, head.LeaseOwnerId,
                        RawExportJobState.Expired, RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED),
                    connection,
                    transaction,
                    cancellationToken);
                return TerminalLeaseResult(expired);
            }
            if (!AuthorityIsEffectiveAt(authority, databaseNow))
            {
                throw Error(AuthorityInvalid);
            }
        }
        catch (RawExportJobException exception) when (exception.Code is AuthorityInvalid or GraphInvalid)
        {
            var reason = exception.Code == GraphInvalid
                ? RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE
                : RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED;
            var terminal = await TerminalizeCoreAsync(
                new(command.Actor, command.JobId, head.Revision, head.FencingToken, head.CurrentAttemptId, head.LeaseOwnerId,
                    RawExportJobState.TerminalFailed, reason),
                connection,
                transaction,
                cancellationToken);
            return TerminalLeaseResult(terminal);
        }

        var attempt = Guid.NewGuid();
        var row = await MutationAsync(
            connection,
            transaction,
            "SELECT * FROM tagekyc.raw_export_acquire_or_reclaim_job_lease(@job,@principal,@client,@revision,@fence,@attempt,@owner,@seconds);",
            cancellationToken,
            P("job", command.JobId), P("principal", command.Actor.PrincipalId), P("client", command.Actor.ClientApplicationId),
            P("revision", command.ExpectedRevision), P("fence", command.ExpectedFencingToken), P("attempt", attempt),
            P("owner", command.LeaseOwnerId), P("seconds", lease.LeaseSeconds));
        ThrowMutation(row.Outcome);
        if (row.Revision is null || row.Fence is null)
        {
            throw Error(GraphInvalid);
        }

        return row.Outcome switch
        {
            "Acquired" => LeaseSuccess(RawExportJobLeaseStatus.Acquired, attempt, row),
            "AcquiredAfterRetryableFailure" => LeaseSuccess(RawExportJobLeaseStatus.AcquiredAfterRetryableFailure, attempt, row),
            "Reclaimed" => LeaseSuccess(RawExportJobLeaseStatus.Reclaimed, attempt, row),
            "TerminalizedModeRetryForbidden" => new(RawExportJobLeaseStatus.TerminalFailed, null, RawExportJobState.TerminalFailed, row.Revision, row.Fence, null, RawExportJobTerminalReasonCode.MODE_RETRY_NOT_AUTHORIZED, "RAW_EXPORT_JOB_MODE_RETRY_NOT_AUTHORIZED"),
            "Expired" => new(RawExportJobLeaseStatus.Expired, null, RawExportJobState.Expired, row.Revision, row.Fence, null, RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED, null),
            _ => throw Error(GraphInvalid),
        };
    }

    private async Task<RawExportJobTerminalizeResult> TerminalizeCoreAsync(
        TerminalizeRawExportJobCommand command,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var row = await MutationAsync(
            connection,
            transaction,
            "SELECT * FROM tagekyc.raw_export_terminalize_job(@job,@principal,@client,@revision,@fence,@attempt,@owner,@state,@reason);",
            cancellationToken,
            P("job", command.JobId), P("principal", command.Actor.PrincipalId), P("client", command.Actor.ClientApplicationId),
            P("revision", command.ExpectedRevision), P("fence", command.ExpectedFencingToken),
            NullableGuid("attempt", command.AttemptId), NullableGuid("owner", command.LeaseOwnerId),
            P("state", command.TerminalState.ToString()), P("reason", command.ReasonCode.ToString()));
        ThrowMutation(row.Outcome);
        if (row.Outcome == "AlreadyTerminal")
        {
            var read = await ReadCoreAsync(command.Actor, command.JobId, connection, transaction, cancellationToken);
            if (read.Job is null)
            {
                throw Error(GraphInvalid);
            }
            var terminal = ParseTerminal(read.Job);
            return terminal with { Status = RawExportJobTerminalizeStatus.AlreadyTerminal };
        }
        if (row.Revision is null || row.Fence is null)
        {
            throw Error(GraphInvalid);
        }
        if (row.Outcome == "Expired")
        {
            return new(RawExportJobTerminalizeStatus.Expired, RawExportJobState.Expired, row.Revision.Value, row.Fence.Value, RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED, null);
        }
        if (row.Outcome != "Terminalized")
        {
            throw Error(GraphInvalid);
        }
        return new(RawExportJobTerminalizeStatus.Terminalized, command.TerminalState, row.Revision.Value, row.Fence.Value, command.ReasonCode, StableTerminalCode(command.ReasonCode));
    }

    private async Task<RawExportJobReadResult> ReadCoreAsync(
        AuthenticatedRawExportActor actor,
        Guid jobId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_job(@principal,@client,@job);",
            connection,
            transaction);
        command.Parameters.AddRange(new NpgsqlParameter[]
        {
            P("principal", actor.PrincipalId),
            P("client", actor.ClientApplicationId),
            P("job", jobId),
        });
        await using var reader = await ExecuteReaderAsync(command, cancellationToken);
        RawExportJobIdentityView? identity = null;
        RawExportJobOperationalHeadView? head = null;
        RawExportJobTransitionSummary? latest = null;
        var classes = new List<RawExportJobClassView>();
        while (await reader.ReadAsync(cancellationToken))
        {
            identity ??= new(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5),
                reader.GetGuid(6), reader.GetString(7), reader.GetGuid(8), reader.GetInt32(9), reader.GetString(10), reader.GetGuid(11),
                Enum.Parse<RawExportMode>(reader.GetString(12), false), ReadTime(reader, 13), ReadTime(reader, 14), reader.GetInt32(15), ReadTime(reader, 16));
            classes.Add(new(reader.GetInt32(17), Enum.Parse<RawExportRawClass>(reader.GetString(18), false)));
            head ??= new(Enum.Parse<RawExportJobState>(reader.GetString(19), false), reader.GetInt64(20), reader.IsDBNull(21) ? null : reader.GetGuid(21), reader.IsDBNull(22) ? null : reader.GetGuid(22), reader.IsDBNull(23) ? null : ReadTime(reader, 23), reader.GetInt64(24));
            latest ??= new(Enum.Parse<RawExportJobEventType>(reader.GetString(25), false), reader.IsDBNull(26) ? null : reader.GetString(26), ReadTime(reader, 27));
        }
        if (identity is null) return new(RawExportJobReadStatus.NotFound, null);
        return new(RawExportJobReadStatus.Found, new(identity, classes, head!, latest!));
    }

    private async Task<FrozenBinding> ParseBindingProjectionAsync(
        BindRawExportJobCommand command,
        IReadOnlyList<BindingRow> rows,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            throw Error("RAW_EXPORT_JOB_PERMIT_UNAVAILABLE");
        }

        var first = rows[0];
        if (first.Outcome == "GraphInvalid")
        {
            if (rows.Count != 1 || first.ExistingJobId is null)
            {
                throw Error(GraphInvalid);
            }

            var read = await ReadCoreAsync(
                command.Actor,
                first.ExistingJobId.Value,
                connection,
                transaction,
                cancellationToken);
            if (read.Status != RawExportJobReadStatus.Found || read.Job is null)
            {
                throw Error(GraphInvalid);
            }

            var head = read.Job.Head;
            var terminal = await TerminalizeCoreAsync(
                new(
                    command.Actor,
                    first.ExistingJobId.Value,
                    head.Revision,
                    head.FencingToken,
                    head.CurrentAttemptId,
                    head.LeaseOwnerId,
                    RawExportJobState.TerminalFailed,
                    RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE),
                connection,
                transaction,
                cancellationToken);
            return FrozenBinding.Terminal(
                new(RawExportJobBindStatus.Terminal, first.ExistingJobId.Value, terminal));
        }

        if (rows.Any(row => row.Outcome != "Valid") ||
            first.AuthorizationDecisionId is null ||
            first.DecisionPrincipalId != command.Actor.PrincipalId ||
            first.DecisionClientApplicationId != command.Actor.ClientApplicationId ||
            first.VerificationSessionId is null ||
            first.SubjectRef is null ||
            first.PolicyId is null ||
            first.PolicyVersion is null or < 1 ||
            first.PurposeCode is null ||
            first.DecisionRecipientClientApplicationId is null ||
            first.PermitRecipientClientApplicationId is null ||
            first.DecisionRecipientClientApplicationId != first.PermitRecipientClientApplicationId ||
            first.PermitRecipientClientApplicationId != command.Actor.ClientApplicationId ||
            first.PermitExpiresAt is null ||
            first.PermitSchemaVersion != 1 ||
            !Enum.TryParse<RawExportMode>(first.ExportMode, false, out var mode) ||
            !Enum.IsDefined(mode) ||
            !string.Equals(first.ClosureType, RawExportPolicyClosureType.CatalogApproved.ToString(), StringComparison.Ordinal) ||
            first.EvaluatedAtUtc is null)
        {
            throw Error(GraphInvalid);
        }

        var classes = new List<RawExportRawClass>(rows.Count);
        for (var ordinal = 0; ordinal < rows.Count; ordinal++)
        {
            var row = rows[ordinal];
            if (row.AuthorizationDecisionId != first.AuthorizationDecisionId ||
                row.DecisionPrincipalId != first.DecisionPrincipalId ||
                row.DecisionClientApplicationId != first.DecisionClientApplicationId ||
                row.VerificationSessionId != first.VerificationSessionId ||
                !string.Equals(row.SubjectRef, first.SubjectRef, StringComparison.Ordinal) ||
                row.PolicyId != first.PolicyId ||
                row.PolicyVersion != first.PolicyVersion ||
                !string.Equals(row.PurposeCode, first.PurposeCode, StringComparison.Ordinal) ||
                row.DecisionRecipientClientApplicationId != first.DecisionRecipientClientApplicationId ||
                row.PermitRecipientClientApplicationId != first.PermitRecipientClientApplicationId ||
                row.PermitExpiresAt != first.PermitExpiresAt ||
                row.PermitSchemaVersion != first.PermitSchemaVersion ||
                !string.Equals(row.ExportMode, first.ExportMode, StringComparison.Ordinal) ||
                !string.Equals(row.ClosureType, first.ClosureType, StringComparison.Ordinal) ||
                row.EvaluatedAtUtc != first.EvaluatedAtUtc ||
                row.ExistingJobId != first.ExistingJobId ||
                !BytesEqual(row.ExistingFingerprint, first.ExistingFingerprint) ||
                row.Ordinal != ordinal ||
                !Enum.TryParse<RawExportRawClass>(row.RawClass, false, out var rawClass) ||
                !Enum.IsDefined(rawClass))
            {
                throw Error(GraphInvalid);
            }

            classes.Add(rawClass);
        }

        if (classes.Count == 0 || classes.Distinct().Count() != classes.Count ||
            (first.ExistingJobId is null) != (first.ExistingFingerprint is null))
        {
            throw Error(GraphInvalid);
        }

        var fingerprint = RawExportJobFingerprintCodec.Compute(
            command.Actor.PrincipalId,
            command.Actor.ClientApplicationId,
            command.PermitId,
            first.AuthorizationDecisionId.Value,
            first.VerificationSessionId.Value,
            first.SubjectRef,
            first.PolicyId.Value,
            first.PolicyVersion.Value,
            first.PurposeCode,
            first.PermitRecipientClientApplicationId.Value,
            mode,
            first.PermitExpiresAt.Value,
            first.PermitExpiresAt.Value,
            command.IdempotencyKey,
            classes);

        if (first.ExistingJobId is not null)
        {
            if (first.ExistingFingerprint!.SequenceEqual(fingerprint))
            {
                return FrozenBinding.Existing(
                    new(RawExportJobBindStatus.ExistingMatch, first.ExistingJobId.Value, null));
            }

            throw Error("RAW_EXPORT_JOB_IDEMPOTENCY_CONFLICT");
        }

        return new(
            command.Actor.PrincipalId,
            first.AuthorizationDecisionId.Value,
            first.VerificationSessionId.Value,
            first.SubjectRef,
            first.PolicyId.Value,
            first.PolicyVersion.Value,
            first.PurposeCode,
            first.PermitRecipientClientApplicationId.Value,
            mode,
            first.PermitExpiresAt.Value,
            classes,
            fingerprint,
            null,
            null);
    }

    private async Task<AuthoritySnapshot> ResolveAuthorityAsync(
        FrozenBinding frozen,
        CancellationToken cancellationToken)
    {
        var eligibility = await controlPlane.ResolveExportEligibilityForAuthorizationAsync(
            frozen.PrincipalId,
            frozen.PolicyId,
            frozen.PolicyVersion,
            cancellationToken);
        var policy = await projections.ReadPolicyInputsAsync(
            frozen.PrincipalId,
            frozen.PolicyId,
            frozen.PolicyVersion,
            cancellationToken);
        var subjectConsent = await consent.ResolveSubjectExportConsentForAuthorizationAsync(
            frozen.VerificationSessionId,
            frozen.PolicyId,
            frozen.PolicyVersion,
            cancellationToken);

        if (eligibility.State == RawExportEligibilityState.Active &&
            (eligibility.PrimaryCause is not null || eligibility.Causes.Count != 0 ||
             eligibility.GrantRef is null || eligibility.LifecycleRef is null ||
             eligibility.BoundRuleSetVersion != eligibility.CurrentRuleSetVersion))
        {
            throw Error(GraphInvalid);
        }

        if (policy.PolicyId != frozen.PolicyId ||
            policy.PolicyVersion != frozen.PolicyVersion ||
            policy.EvaluatedAtUtc != eligibility.EvaluatedAtUtc ||
            subjectConsent.EvaluatedAtUtc != eligibility.EvaluatedAtUtc)
        {
            throw Error(GraphInvalid);
        }

        if (eligibility.State != RawExportEligibilityState.Active ||
            !policy.PolicyExists ||
            policy.ClosureType != RawExportPolicyClosureType.CatalogApproved ||
            subjectConsent.State != RawExportSubjectConsentState.Effective ||
            subjectConsent.Cause is not null ||
            subjectConsent.ConsentRef is null ||
            subjectConsent.VerificationSessionId != frozen.VerificationSessionId ||
            !string.Equals(subjectConsent.SubjectRef, frozen.SubjectRef, StringComparison.Ordinal) ||
            subjectConsent.PolicyId != frozen.PolicyId ||
            subjectConsent.PolicyVersion != frozen.PolicyVersion ||
            !string.Equals(subjectConsent.PurposeCode, frozen.PurposeCode, StringComparison.Ordinal) ||
            subjectConsent.RecipientClientApplicationId != frozen.RecipientClientApplicationId ||
            frozen.Classes.Any(rawClass =>
                !policy.AllowedClasses.Contains(rawClass) ||
                !subjectConsent.ConsentedRawClasses.Contains(rawClass)))
        {
            throw Error(AuthorityInvalid);
        }

        return new(eligibility.FulfillmentRefs, subjectConsent.ValidFromUtc, subjectConsent.ValidUntilUtc);
    }

    private static bool AuthorityIsEffectiveAt(AuthoritySnapshot authority, DateTimeOffset databaseNow) =>
        (authority.ConsentValidFromUtc is null || authority.ConsentValidFromUtc <= databaseNow) &&
        (authority.ConsentValidUntilUtc is null || databaseNow < authority.ConsentValidUntilUtc) &&
        authority.Fulfillments.All(item => item.ValidUntilUtc is null || databaseNow < item.ValidUntilUtc);

    private static async Task<LockedSession?> LockSessionAsync(
        Guid verificationSessionId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT "ClientApplicationId","SubjectRef","State"
            FROM tagekyc.raw_export_lock_verification_session_for_authorization(@sessionId);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", verificationSessionId);
        await using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var result = new LockedSession(reader.GetGuid(0), reader.GetString(1), reader.GetString(2));
        if (await reader.ReadAsync(cancellationToken))
        {
            throw Error(GraphInvalid);
        }

        return result;
    }

    private static async Task<DateTimeOffset> ReadDatabaseTimeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT clock_timestamp();", connection, transaction);
        return (await command.ExecuteScalarAsync(cancellationToken)) switch
        {
            DateTimeOffset value => value,
            DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            _ => throw Error(GraphInvalid),
        };
    }

    private static bool BytesEqual(byte[]? left, byte[]? right) =>
        left is null ? right is null : right is not null && left.SequenceEqual(right);

    private async Task<List<BindingRow>> ReadBindingProjectionAsync(
        BindRawExportJobCommand command,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var sql = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_job_binding_inputs(@principal,@client,@permit);",
            connection,
            transaction);
        sql.Parameters.AddRange(new NpgsqlParameter[]
        {
            P("principal", command.Actor.PrincipalId),
            P("client", command.Actor.ClientApplicationId),
            P("permit", command.PermitId),
        });
        await using var reader = await ExecuteReaderAsync(sql, cancellationToken);
        var rows = new List<BindingRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetGuid(1),
                reader.IsDBNull(2) ? null : (byte[])reader.GetValue(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetGuid(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetGuid(11),
                reader.IsDBNull(12) ? null : reader.GetGuid(12),
                reader.IsDBNull(13) ? null : ReadTime(reader, 13),
                reader.IsDBNull(14) ? null : reader.GetInt32(14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? null : reader.GetInt32(17),
                reader.IsDBNull(18) ? null : reader.GetString(18),
                reader.IsDBNull(19) ? null : ReadTime(reader, 19)));
        }
        return rows;
    }

    private async Task<T> ExecuteAsync<T>(
        Guid actorPrincipalId,
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> body,
        CancellationToken cancellationToken)
    {
        if (db.Database.GetDbConnection() is not NpgsqlConnection connection)
        {
            throw Error(IsolationInvalid);
        }

        EnsureTransactionAdmission(connection);
        Exception? cleanupFailure = null;
        try
        {
            EnsureTransactionAdmission(connection);
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
            if (transaction.GetDbTransaction() is not NpgsqlTransaction npgsql ||
                npgsql.IsolationLevel != System.Data.IsolationLevel.ReadCommitted)
            {
                throw Error(IsolationInvalid);
            }
            await SetActorAsync(connection, npgsql, actorPrincipalId, cancellationToken);
            try
            {
                var result = await body(connection, npgsql, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.RaiseException)
        {
            throw Error(MapDatabaseCode(exception.MessageText));
        }
        finally
        {
            try
            {
                if (connection.State != ConnectionState.Closed)
                {
                    await db.Database.CloseConnectionAsync();
                }
                if (db.Database.CurrentTransaction is not null || connection.State != ConnectionState.Closed)
                {
                    throw Error(IsolationInvalid);
                }
            }
            catch (Exception exception)
            {
                cleanupFailure = exception;
            }
            if (cleanupFailure is not null)
            {
                throw Error(IsolationInvalid);
            }
        }
    }

    private void EnsureTransactionAdmission(NpgsqlConnection connection)
    {
        if (Transaction.Current is not null ||
            db.Database.CurrentTransaction is not null ||
            connection.State != ConnectionState.Closed)
        {
            throw Error(IsolationInvalid);
        }
    }

    private static async Task SetActorAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid actor, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("SELECT set_config('tagekyc.actor_principal_id',@actor,true);", connection, transaction);
        command.Parameters.AddWithValue("actor", actor.ToString("D"));
        await command.ExecuteScalarAsync(token);
    }

    private static async Task<MutationRow> MutationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken token,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters);
        await using var reader = await ExecuteReaderAsync(command, token);
        if (!await reader.ReadAsync(token)) throw Error(GraphInvalid);
        return new(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetInt64(1), reader.IsDBNull(2) ? null : reader.GetInt64(2), reader.IsDBNull(3) ? null : ReadTime(reader, 3));
    }

    private static async Task<NpgsqlDataReader> ExecuteReaderAsync(NpgsqlCommand command, CancellationToken token) =>
        await command.ExecuteReaderAsync(token);

    private static RawExportJobLeaseResult LeaseSuccess(RawExportJobLeaseStatus status, Guid attempt, MutationRow row)
    {
        if (row.ExpiresAt is null) throw Error(GraphInvalid);
        return new(status, attempt, RawExportJobState.Assembling, row.Revision, row.Fence, row.ExpiresAt, null, null);
    }

    private static RawExportJobLeaseResult TerminalLeaseResult(RawExportJobReadResult read)
    {
        if (read.Job is null) throw Error(GraphInvalid);
        var terminal = ParseTerminal(read.Job);
        var status = terminal.State == RawExportJobState.Expired ? RawExportJobLeaseStatus.Expired : terminal.State == RawExportJobState.TerminalFailed ? RawExportJobLeaseStatus.TerminalFailed : RawExportJobLeaseStatus.AlreadyTerminal;
        return new(status, null, terminal.State, terminal.Revision, terminal.FencingToken, null, terminal.TerminalReason, terminal.StableCode);
    }

    private static RawExportJobLeaseResult TerminalLeaseResult(RawExportJobTerminalizeResult terminal)
    {
        var status = terminal.State == RawExportJobState.Expired
            ? RawExportJobLeaseStatus.Expired
            : terminal.State == RawExportJobState.TerminalFailed
                ? RawExportJobLeaseStatus.TerminalFailed
                : RawExportJobLeaseStatus.AlreadyTerminal;
        return new(
            status,
            null,
            terminal.State,
            terminal.Revision,
            terminal.FencingToken,
            null,
            terminal.TerminalReason,
            terminal.StableCode);
    }

    private static RawExportJobTerminalizeResult ParseTerminal(RawExportJobView job)
    {
        if (job.Head.CurrentState is not (RawExportJobState.TerminalFailed or RawExportJobState.Cancelled or RawExportJobState.Expired) ||
            job.LatestTransition.LatestFailureCode is null ||
            !Enum.TryParse<RawExportJobTerminalReasonCode>(job.LatestTransition.LatestFailureCode, false, out var reason))
        {
            throw Error(GraphInvalid);
        }
        return new(RawExportJobTerminalizeStatus.AlreadyTerminal, job.Head.CurrentState, job.Head.Revision, job.Head.FencingToken, reason, StableTerminalCode(reason));
    }

    private static string? StableTerminalCode(RawExportJobTerminalReasonCode reason) => reason switch
    {
        RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED => AuthorityInvalid,
        RawExportJobTerminalReasonCode.ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE => "RAW_EXPORT_JOB_TRANSITION_INVALID",
        RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE => GraphInvalid,
        RawExportJobTerminalReasonCode.MODE_RETRY_NOT_AUTHORIZED => "RAW_EXPORT_JOB_MODE_RETRY_NOT_AUTHORIZED",
        RawExportJobTerminalReasonCode.REQUEST_CANCELLED => null,
        RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED => null,
        _ => throw Error(GraphInvalid),
    };

    private static void ThrowMutation(string outcome)
    {
        var code = outcome switch
        {
            "ConcurrencyConflict" => "RAW_EXPORT_JOB_CONCURRENCY_CONFLICT",
            "LeaseHeld" => "RAW_EXPORT_JOB_LEASE_HELD",
            "LeaseNotHeld" => "RAW_EXPORT_JOB_LEASE_NOT_HELD",
            "FenceStale" => "RAW_EXPORT_JOB_FENCE_STALE",
            "TransitionInvalid" => "RAW_EXPORT_JOB_TRANSITION_INVALID",
            _ => null,
        };
        if (code is not null) throw Error(code);
    }

    private static string MapDatabaseCode(string message) => message switch
    {
        "RAW_EXPORT_JOB_LEASE_CONFIG_INVALID" => RawExportJobLeaseOptions.InvalidCode,
        "RAW_EXPORT_ACTOR_CONTEXT_MISSING" or "RAW_EXPORT_ACTOR_CONTEXT_INVALID" or
        "RAW_EXPORT_JOB_ACTOR_MISMATCH" or IsolationInvalid or RequestInvalid or GraphInvalid => message,
        _ => GraphInvalid,
    };

    private static void ValidateActor(AuthenticatedRawExportActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        Require(actor.PrincipalId);
        Require(actor.ClientApplicationId);
        Require(actor.ApiKeyId);
    }
    private static void ValidateLeaseCommand(AuthenticatedRawExportActor actor, Guid job, long revision, long fence, Guid owner)
    {
        ValidateActor(actor); Require(job); Require(owner);
        if (revision < 0 || fence < 0) throw Error(RequestInvalid);
    }
    private void ValidateLeaseState()
    {
        if (!lease.IsValid || lease.LeaseSeconds is < 10 or > 300) throw Error(RawExportJobLeaseOptions.InvalidCode);
    }
    private static bool ValidTerminalPair(RawExportJobState state, RawExportJobTerminalReasonCode reason) =>
        state switch
        {
            RawExportJobState.TerminalFailed => reason is RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED or RawExportJobTerminalReasonCode.ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE or RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE or RawExportJobTerminalReasonCode.MODE_RETRY_NOT_AUTHORIZED,
            RawExportJobState.Cancelled => reason == RawExportJobTerminalReasonCode.REQUEST_CANCELLED,
            RawExportJobState.Expired => reason == RawExportJobTerminalReasonCode.PERMIT_OR_JOB_EXPIRED,
            _ => false,
        };
    private static void Require(Guid value)
    {
        if (value == Guid.Empty) throw Error(RequestInvalid);
    }
    private static RawExportJobException Error(string code) => new(code);
    private static NpgsqlParameter P<T>(string name, T value) => new(name, value!);
    private static NpgsqlParameter NullableGuid(string name, Guid? value) => new(name, NpgsqlDbType.Uuid) { Value = value is null ? DBNull.Value : value.Value };
    private static DateTimeOffset ReadTime(DbDataReader reader, int ordinal)
    {
        var value = reader.GetFieldValue<DateTime>(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private sealed record FrozenBinding(
        Guid PrincipalId,
        Guid AuthorizationDecisionId,
        Guid VerificationSessionId,
        string SubjectRef,
        Guid PolicyId,
        int PolicyVersion,
        string PurposeCode,
        Guid RecipientClientApplicationId,
        RawExportMode Mode,
        DateTimeOffset PermitExpiresAt,
        IReadOnlyList<RawExportRawClass> Classes,
        byte[] Fingerprint,
        RawExportJobBindResult? ExistingResult,
        RawExportJobBindResult? TerminalResult)
    {
        public static FrozenBinding Existing(RawExportJobBindResult result) =>
            new(Guid.Empty, Guid.Empty, Guid.Empty, string.Empty, Guid.Empty, 0, string.Empty, Guid.Empty,
                default, default, [], [], result, null);

        public static FrozenBinding Terminal(RawExportJobBindResult result) =>
            new(Guid.Empty, Guid.Empty, Guid.Empty, string.Empty, Guid.Empty, 0, string.Empty, Guid.Empty,
                default, default, [], [], null, result);

        public static FrozenBinding FromJob(RawExportJobView job) =>
            new(
                job.Identity.PrincipalId,
                job.Identity.AuthorizationDecisionId,
                job.Identity.VerificationSessionId,
                job.Identity.SubjectRef,
                job.Identity.PolicyId,
                job.Identity.PolicyVersion,
                job.Identity.PurposeCode,
                job.Identity.RecipientClientApplicationId,
                job.Identity.ExportMode,
                job.Identity.PermitExpiresAt,
                job.Classes.OrderBy(item => item.Ordinal).Select(item => item.RawClass).ToArray(),
                [],
                null,
                null);
    }

    private sealed record AuthoritySnapshot(
        IReadOnlyList<RawExportFulfillmentRef> Fulfillments,
        DateTimeOffset? ConsentValidFromUtc,
        DateTimeOffset? ConsentValidUntilUtc);

    private sealed record LockedSession(Guid ClientApplicationId, string SubjectRef, string State);

    private sealed record BindingRow(
        string Outcome,
        Guid? ExistingJobId,
        byte[]? ExistingFingerprint,
        Guid? AuthorizationDecisionId,
        Guid? DecisionPrincipalId,
        Guid? DecisionClientApplicationId,
        Guid? VerificationSessionId,
        string? SubjectRef,
        Guid? PolicyId,
        int? PolicyVersion,
        string? PurposeCode,
        Guid? DecisionRecipientClientApplicationId,
        Guid? PermitRecipientClientApplicationId,
        DateTimeOffset? PermitExpiresAt,
        int? PermitSchemaVersion,
        string? ExportMode,
        string? ClosureType,
        int? Ordinal,
        string? RawClass,
        DateTimeOffset? EvaluatedAtUtc);
    private sealed record MutationRow(string Outcome, long? Revision, long? Fence, DateTimeOffset? ExpiresAt);
}
