using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.CaptureRuntime;

// The default scoped context owns B. Never use the separate authenticator/application connection here.
public sealed class CaptureRuntimeAppendAuthority(TagEkycDbContext db) : ICaptureRuntimeAppendAuthority
{
    public Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateCaptureAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId, DateTimeOffset now, CancellationToken ct) =>
        ValidateAsync(actor, bindingId, "CaptureObservation", now, ct);
    public Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateEvidenceAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId, DateTimeOffset now, CancellationToken ct) =>
        ValidateAsync(actor, bindingId, "TrustedEvidence", now, ct);

    private async Task<SessionOperationResult<VerifiedRuntimeAppendAuthority>> ValidateAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId, string requiredRole,
        DateTimeOffset now, CancellationToken ct)
    {
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Append authority requires the owning business transaction.");
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.capture_runtime_validate_append_authority(@agent,@installation,@credential,@generation,@binding,@role,@now)",
            (NpgsqlConnection)db.Database.GetDbConnection(),
            (NpgsqlTransaction)transaction.GetDbTransaction());
        command.Parameters.AddWithValue("agent", NpgsqlDbType.Uuid, actor.CaptureAgentId);
        command.Parameters.AddWithValue("installation", NpgsqlDbType.Uuid, actor.DeviceInstallationId);
        command.Parameters.AddWithValue("credential", NpgsqlDbType.Uuid, actor.CredentialId);
        command.Parameters.AddWithValue("generation", NpgsqlDbType.Bigint, actor.CredentialGeneration);
        command.Parameters.AddWithValue("binding", NpgsqlDbType.Uuid, bindingId);
        command.Parameters.AddWithValue("role", NpgsqlDbType.Text, requiredRole);
        command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return Denied();
        for (var i = 0; i < 9; i++) if (reader.IsDBNull(i)) return Denied();
        var row = new VerifiedRuntimeAppendAuthority(reader.GetGuid(5), reader.GetGuid(0),
            reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4),
            reader.GetGuid(6), reader.GetInt64(7), reader.GetGuid(8));
        return await reader.ReadAsync(ct) ? Denied() :
            SessionOperationResult<VerifiedRuntimeAppendAuthority>.Success(row);
    }

    private static SessionOperationResult<VerifiedRuntimeAppendAuthority> Denied() =>
        SessionOperationResult<VerifiedRuntimeAppendAuthority>.Failure("ACCESS_DENIED", "Access denied.", 403);
}
