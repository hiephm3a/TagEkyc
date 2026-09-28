using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class PostgresSiteRawIngressQualificationRunStore(TagEkycDbContext db)
    : ISiteRawIngressQualificationRunStore
{
    public Task<bool> EnrollSyntheticCredentialAsync(
        SiteRawIngressQualificationSyntheticCredentialEnrollment enrollment,
        DateTimeOffset now, CancellationToken cancellationToken) => BooleanAsync(
            "SELECT tagekyc.site_qualification_enroll_synthetic_credential($1,$2,$3,$4,$5,$6,$7)",
            cancellationToken, enrollment.SiteId, enrollment.EndpointOrigin,
            enrollment.DeploymentRevision, enrollment.CredentialId,
            enrollment.CredentialGeneration, enrollment.ExpiresAtUtc,
            enrollment.EnrolledByApiKeyId);

    public async Task<SiteRawIngressQualificationRunHandle?> RegisterAsync(
        SiteRawIngressQualificationRunRegistration registration, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = await CommandAsync("""
            SELECT tagekyc.site_qualification_register_run(
              $1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14)
            """, cancellationToken);
        Add(command, registration.SiteId, registration.EndpointOrigin, registration.DeploymentRevision,
            registration.QualificationSuiteId,
            registration.Binding.CredentialId, registration.Binding.CredentialGeneration,
            registration.Binding.IngressIdempotencyKey, registration.Binding.IngressMetadataSha256,
            registration.Binding.MediaType, registration.Binding.ContentLength,
            registration.Binding.PlaintextSha256, registration.Mode.ToString(),
            registration.ExpiresAtUtc, registration.RegisteredByApiKeyId);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is Guid id && id != Guid.Empty
            ? new(id, registration.QualificationSuiteId, registration.SiteId,
                registration.EndpointOrigin, registration.DeploymentRevision,
                registration.ExpiresAtUtc) : null;
    }

    public async Task<SiteRawIngressQualificationRawPost?> ObserveRawPostAsync(
        CaptureRuntimeSiteTransportQualificationSettings settings,
        SiteRawIngressQualificationRunBinding binding, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = await CommandAsync("""
            SELECT * FROM tagekyc.site_qualification_observe_raw_post($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
            """, cancellationToken);
        Add(command, settings.SiteId, settings.EndpointOrigin, settings.DeploymentRevision,
            binding.CredentialId, binding.CredentialGeneration, binding.IngressIdempotencyKey,
            binding.IngressMetadataSha256, binding.MediaType, binding.ContentLength,
            binding.PlaintextSha256);
        await using var row = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await row.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        if (row.FieldCount != 3 || row.IsDBNull(0) || row.IsDBNull(1) || row.IsDBNull(2)) throw Invalid();
        if (!Enum.TryParse<SiteRawIngressQualificationRunMode>(row.GetString(1), false, out var mode))
            throw Invalid();
        var result = new SiteRawIngressQualificationRawPost(row.GetGuid(0), mode, row.GetBoolean(2));
        if (await row.ReadAsync(cancellationToken).ConfigureAwait(false)) throw Invalid();
        return result;
    }

    public Task<bool> ConsumeAuthenticatedAsync(Guid qualificationRunId,
        SiteRawIngressQualificationRunBinding binding, DateTimeOffset now,
        CancellationToken cancellationToken) => BooleanAsync(
            "SELECT tagekyc.site_qualification_consume_authenticated($1,$2,$3,$4,$5,$6,$7,$8)",
            cancellationToken, qualificationRunId, binding.CredentialId,
            binding.CredentialGeneration, binding.IngressIdempotencyKey,
            binding.IngressMetadataSha256, binding.MediaType, binding.ContentLength,
            binding.PlaintextSha256);

    public Task<bool> ReleaseBrokerAsync(Guid qualificationRunId, DateTimeOffset now,
        SiteRawIngressQualificationRunAccess access, CancellationToken cancellationToken) => BooleanAsync(
            "SELECT tagekyc.site_qualification_release_broker($1,$2,$3,$4,$5)",
            cancellationToken, qualificationRunId, access.ApiKeyId, access.SiteId,
            access.EndpointOrigin, access.DeploymentRevision);

    public Task<bool> AcknowledgeBrokerCommitAsync(Guid qualificationRunId, DateTimeOffset now,
        SiteRawIngressQualificationRunAccess access, CancellationToken cancellationToken) => BooleanAsync(
            "SELECT tagekyc.site_qualification_acknowledge_broker_commit($1,$2,$3,$4,$5)",
            cancellationToken, qualificationRunId, access.ApiKeyId, access.SiteId,
            access.EndpointOrigin, access.DeploymentRevision);

    public Task<bool> RecordAgentObservationAsync(Guid qualificationRunId,
        SiteRawIngressQualificationAgentObservation observation, DateTimeOffset now,
        SiteRawIngressQualificationRunAccess access, CancellationToken cancellationToken) => BooleanAsync(
            "SELECT tagekyc.site_qualification_record_agent_observation($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)",
            cancellationToken, qualificationRunId, access.ApiKeyId, access.SiteId,
            access.EndpointOrigin, access.DeploymentRevision, observation.TransportEntryCount,
            observation.ContentBytesCopied, observation.BodyBytesSentWhileBrokerHeld,
            observation.ObservedContinue, observation.ContinueObservedBeforeBrokerCommit,
            observation.ApplicationPrebufferObserved, observation.FinalResponseObserved);

    public Task<bool> RecordServerBodyReadsAsync(Guid qualificationRunId, int readsWhileBrokerHeld,
        DateTimeOffset now, CancellationToken cancellationToken) => BooleanAsync(
            "SELECT tagekyc.site_qualification_record_server_body_read($1,$2)",
            cancellationToken, qualificationRunId, readsWhileBrokerHeld);

    public async Task<SiteRawIngressQualificationRunReport?> ReadAsync(Guid qualificationRunId,
        DateTimeOffset now, SiteRawIngressQualificationRunAccess access,
        CancellationToken cancellationToken)
    {
        await using var command = await CommandAsync(
            "SELECT * FROM tagekyc.site_qualification_read_run($1,$2,$3,$4,$5)", cancellationToken);
        Add(command, qualificationRunId, access.ApiKeyId, access.SiteId,
            access.EndpointOrigin, access.DeploymentRevision);
        await using var row = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await row.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        if (row.FieldCount != 24 || Enumerable.Range(0, 24)
                .Where(index => index is not (9 or 10 or 11)).Any(row.IsDBNull))
            throw Invalid();
        if (!Enum.TryParse<SiteRawIngressQualificationRunMode>(row.GetString(6), false, out var mode))
            throw Invalid();
        var result = new SiteRawIngressQualificationRunReport(
            row.GetGuid(0), row.GetGuid(1), row.GetString(2), row.GetString(3), row.GetString(4),
            row.GetString(5), mode, row.GetFieldValue<DateTimeOffset>(7),
            row.GetFieldValue<DateTimeOffset>(8), NullableUtc(row, 9), NullableUtc(row, 10),
            NullableUtc(row, 11), row.GetInt32(12), row.GetInt32(13), row.GetInt32(14),
            row.GetInt64(15), row.GetInt64(16), row.GetBoolean(17), row.GetBoolean(18),
            row.GetBoolean(19), row.GetBoolean(20), row.GetBoolean(21), row.GetBoolean(22),
            row.GetBoolean(23));
        if (await row.ReadAsync(cancellationToken).ConfigureAwait(false)) throw Invalid();
        return result;
    }

    private async Task<bool> BooleanAsync(string sql, CancellationToken cancellationToken,
        params object[] values)
    {
        await using var command = await CommandAsync(sql, cancellationToken);
        Add(command, values);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is bool result && result;
    }

    private async Task<NpgsqlCommand> CommandAsync(string sql, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return new NpgsqlCommand(sql, connection);
    }

    private static DateTimeOffset? NullableUtc(NpgsqlDataReader row, int index) =>
        row.IsDBNull(index) ? null : row.GetFieldValue<DateTimeOffset>(index);

    private static void Add(NpgsqlCommand command, params object[] values)
    {
        foreach (var value in values) command.Parameters.Add(new NpgsqlParameter { Value = value });
    }

    private static InvalidOperationException Invalid() =>
        new("SITE_QUALIFICATION_MEASUREMENT_STATE_INVALID");
}
