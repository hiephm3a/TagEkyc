using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientManagementRepository(
    IRecipientManagementConnectionFactory connections,
    IManagedCredentialMaterialGenerator credentialGenerator) : IRecipientManagementGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(
        AuthenticatedClientContext actor, EnrollManagedRecipientRequest request,
        string idempotencyKey, CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalGuid(request.PrincipalId));
        return ExecuteSingleAsync(
            actor, "EnrollRecipient", request.RecipientClientApplicationId,
            idempotencyKey, payload, EnrollRecipientSql,
            parameters => parameters.AddWithValue("principal", request.PrincipalId),
            static snapshot => Deserialize<ManagedRecipientIdentityDto>(snapshot),
            cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(
        AuthenticatedClientContext actor, IssueManagedRecipientCredentialRequest request,
        string idempotencyKey, CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalNullableTimestamp(request.ExpiresAtUtc));
        return ExecuteCredentialAsync(actor, "IssueCredential", request.RecipientClientApplicationId,
            idempotencyKey, payload, IssueCredentialSql,
            (parameters, material) =>
            {
                AddNullableCandidate(parameters, material);
                parameters.Add(new NpgsqlParameter("expires", NpgsqlDbType.TimestampTz)
                    { Value = request.ExpiresAtUtc is null ? DBNull.Value : request.ExpiresAtUtc.Value });
            }, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ReplaceCredentialAsync(
        AuthenticatedClientContext actor, ReplaceManagedRecipientCredentialRequest request,
        string idempotencyKey, CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalGuid(request.CurrentApiKeyId),
            RecipientManagementCodec.CanonicalInteger(request.CurrentCredentialRevision),
            RecipientManagementCodec.CanonicalNullableTimestamp(request.ExpiresAtUtc),
            RecipientManagementCodec.CanonicalString(request.Reason));
        return ExecuteCredentialAsync(actor, "ReplaceCredential", request.RecipientClientApplicationId,
            idempotencyKey, payload, ReplaceCredentialSql,
            (parameters, material) =>
            {
                parameters.AddWithValue("current_api_key", request.CurrentApiKeyId);
                parameters.AddWithValue("current_revision", request.CurrentCredentialRevision);
                AddNullableCandidate(parameters, material);
                parameters.Add(new NpgsqlParameter("expires", NpgsqlDbType.TimestampTz)
                    { Value = request.ExpiresAtUtc is null ? DBNull.Value : request.ExpiresAtUtc.Value });
                parameters.AddWithValue("reason", request.Reason);
            }, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> RevokeCredentialAsync(
        AuthenticatedClientContext actor, RevokeManagedRecipientCredentialRequest request,
        string idempotencyKey, CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalGuid(request.ApiKeyId),
            RecipientManagementCodec.CanonicalInteger(request.ExpectedRevision),
            RecipientManagementCodec.CanonicalString(request.Reason));
        return ExecuteSingleAsync(
            actor, "RevokeCredential", request.RecipientClientApplicationId,
            idempotencyKey, payload, RevokeCredentialSql,
            parameters =>
            {
                parameters.AddWithValue("api_key", request.ApiKeyId);
                parameters.AddWithValue("expected_revision", request.ExpectedRevision);
                parameters.AddWithValue("reason", request.Reason);
            },
            static snapshot => Deserialize<ManagedCredentialDto>(snapshot),
            cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(
        AuthenticatedClientContext actor, EnrollRecipientPublicKeyRequest request,
        ValidatedRecipientPublicKey key, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalString(request.RecipientKeyId),
            RecipientManagementCodec.CanonicalInteger(request.RecipientKeyVersion),
            RecipientManagementCodec.CanonicalString(request.PublicKeyAlgorithm),
            key.SubjectPublicKeyInfo, key.Fingerprint,
            RecipientManagementCodec.CanonicalTimestamp(request.ValidFromUtc),
            RecipientManagementCodec.CanonicalTimestamp(request.ValidUntilUtc));
        return ExecuteSingleAsync(
            actor, "EnrollKey", request.RecipientClientApplicationId,
            idempotencyKey, payload, EnrollKeySql,
            parameters => AddKey(parameters, request.RecipientKeyId,
                request.RecipientKeyVersion, request.PublicKeyAlgorithm, key,
                request.ValidFromUtc, request.ValidUntilUtc),
            static snapshot => Deserialize<RecipientPublicKeyOperationResultDto>(snapshot),
            cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(
        AuthenticatedClientContext actor, RotateRecipientPublicKeyRequest request,
        ValidatedRecipientPublicKey key, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalString(request.RecipientKeyId),
            RecipientManagementCodec.CanonicalInteger(request.CurrentKeyVersion),
            RecipientManagementCodec.CanonicalInteger(request.CurrentKeyRevision),
            RecipientManagementCodec.CanonicalInteger(request.NewKeyVersion),
            RecipientManagementCodec.CanonicalString(request.PublicKeyAlgorithm),
            key.SubjectPublicKeyInfo, key.Fingerprint,
            RecipientManagementCodec.CanonicalTimestamp(request.ValidFromUtc),
            RecipientManagementCodec.CanonicalTimestamp(request.ValidUntilUtc),
            RecipientManagementCodec.CanonicalString(request.Reason));
        return ExecuteSingleAsync(
            actor, "RotateKey", request.RecipientClientApplicationId,
            idempotencyKey, payload, RotateKeySql,
            parameters =>
            {
                parameters.AddWithValue("key_id", request.RecipientKeyId);
                parameters.AddWithValue("current_version", request.CurrentKeyVersion);
                parameters.AddWithValue("current_revision", request.CurrentKeyRevision);
                parameters.AddWithValue("new_version", request.NewKeyVersion);
                parameters.AddWithValue("algorithm", request.PublicKeyAlgorithm);
                parameters.AddWithValue("spki", key.SubjectPublicKeyInfo);
                parameters.AddWithValue("fingerprint", key.Fingerprint);
                parameters.AddWithValue("valid_from", request.ValidFromUtc);
                parameters.AddWithValue("valid_until", request.ValidUntilUtc);
                parameters.AddWithValue("reason", request.Reason);
            },
            static snapshot => Deserialize<RecipientPublicKeyOperationResultDto>(snapshot),
            cancellationToken);
    }

    public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(
        AuthenticatedClientContext actor, RevokeRecipientPublicKeyRequest request,
        string idempotencyKey, CancellationToken cancellationToken)
    {
        var payload = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(request.RecipientClientApplicationId),
            RecipientManagementCodec.CanonicalString(request.RecipientKeyId),
            RecipientManagementCodec.CanonicalInteger(request.RecipientKeyVersion),
            RecipientManagementCodec.CanonicalInteger(request.ExpectedRevision),
            RecipientManagementCodec.CanonicalString(request.Reason));
        return ExecuteSingleAsync(
            actor, "RevokeKey", request.RecipientClientApplicationId,
            idempotencyKey, payload, RevokeKeySql,
            parameters =>
            {
                parameters.AddWithValue("key_id", request.RecipientKeyId);
                parameters.AddWithValue("key_version", request.RecipientKeyVersion);
                parameters.AddWithValue("expected_revision", request.ExpectedRevision);
                parameters.AddWithValue("reason", request.Reason);
            },
            static snapshot => Deserialize<RecipientPublicKeyOperationResultDto>(snapshot),
            cancellationToken);
    }

    public async Task<SessionOperationResult<ManagedRecipientReadinessDto>> ReadReadinessAsync(
        AuthenticatedClientContext actor, Guid recipientClientApplicationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connections.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = ReadinessSql;
            command.Parameters.AddWithValue("recipient", recipientClientApplicationId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return NotFound<ManagedRecipientReadinessDto>();
            var codes = reader.GetFieldValue<string[]>(reader.GetOrdinal("Codes"));
            return SessionOperationResult<ManagedRecipientReadinessDto>.Success(new(
                recipientClientApplicationId,
                reader.GetBoolean(reader.GetOrdinal("Ready")),
                codes,
                reader.GetInt32(reader.GetOrdinal("AuthorizedDeliveryCount")),
                reader.GetInt32(reader.GetOrdinal("StreamingDeliveryCount")),
                reader.GetInt32(reader.GetOrdinal("InterruptedDeliveryCount"))));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return Unavailable<ManagedRecipientReadinessDto>(); }
    }

    private async Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ExecuteCredentialAsync(
        AuthenticatedClientContext actor,
        string operationKind,
        Guid recipient,
        string idempotencyKey,
        byte[] canonicalPayload,
        string commandText,
        Action<NpgsqlParameterCollection, ManagedCredentialMaterial?> addOperationParameters,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid();
        var idempotencyDigest = RecipientManagementCodec.IdempotencyKeyDigest(idempotencyKey);
        var payloadDigest = RecipientManagementCodec.PayloadDigest(operationKind, canonicalPayload);
        var equality = RecipientManagementCodec.EqualityFingerprint(
            actor.PrincipalId, operationKind, recipient, payloadDigest);
        try
        {
            await using var connection = await connections.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var initial = await InvokeAsync(connection, transaction, commandText, operationId,
                actor, recipient, idempotencyDigest, equality, payloadDigest,
                parameters => addOperationParameters(parameters, null), cancellationToken).ConfigureAwait(false);
            if (initial.Outcome != "CandidateRequired")
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return MapCredentialSnapshot(initial, credential: null);
            }

            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var material = credentialGenerator.Generate();
                try
                {
                    var result = await InvokeAsync(connection, transaction, commandText, operationId,
                        actor, recipient, idempotencyDigest, equality, payloadDigest,
                        parameters => addOperationParameters(parameters, material), cancellationToken).ConfigureAwait(false);
                    if (result.Outcome == "CandidateConflict")
                    {
                        if (attempt == 5)
                        {
                            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                            return Failure<RecipientManagementWriteResult<ManagedCredentialDto>>(RecipientManagementErrorCodes.CredentialConflict,
                                "Managed credential candidate space is exhausted.", 409);
                        }
                        continue;
                    }
                    if (!SuccessOutcome(result.Outcome))
                    {
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        return MapCredentialSnapshot(result, null);
                    }
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return MapCredentialSnapshot(result,
                        result.CandidateCommitted ? material.PresentedKey : null);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(material.KeyHash);
                }
            }
            throw new InvalidOperationException("C5_CANDIDATE_LOOP_UNREACHABLE");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return Unavailable<RecipientManagementWriteResult<ManagedCredentialDto>>(); }
        finally
        {
            CryptographicOperations.ZeroMemory(idempotencyDigest);
            CryptographicOperations.ZeroMemory(payloadDigest);
            CryptographicOperations.ZeroMemory(equality);
            CryptographicOperations.ZeroMemory(canonicalPayload);
        }
    }

    private async Task<SessionOperationResult<RecipientManagementWriteResult<T>>> ExecuteSingleAsync<T>(
        AuthenticatedClientContext actor,
        string operationKind,
        Guid recipient,
        string idempotencyKey,
        byte[] canonicalPayload,
        string commandText,
        Action<NpgsqlParameterCollection> addOperationParameters,
        Func<string, T> deserialize,
        CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid();
        var idempotencyDigest = RecipientManagementCodec.IdempotencyKeyDigest(idempotencyKey);
        var payloadDigest = RecipientManagementCodec.PayloadDigest(operationKind, canonicalPayload);
        var equality = RecipientManagementCodec.EqualityFingerprint(actor.PrincipalId, operationKind, recipient, payloadDigest);
        try
        {
            await using var connection = await connections.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var result = await InvokeAsync(connection, transaction, commandText, operationId,
                actor, recipient, idempotencyDigest, equality, payloadDigest,
                addOperationParameters, cancellationToken).ConfigureAwait(false);
            if (!SuccessOutcome(result.Outcome))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return MapFailure<RecipientManagementWriteResult<T>>(result.Outcome);
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return SessionOperationResult<RecipientManagementWriteResult<T>>.Success(new(
                deserialize(result.ResultSnapshot), SuccessStatus(operationKind, result.Outcome)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return Unavailable<RecipientManagementWriteResult<T>>(); }
        finally
        {
            CryptographicOperations.ZeroMemory(idempotencyDigest);
            CryptographicOperations.ZeroMemory(payloadDigest);
            CryptographicOperations.ZeroMemory(equality);
            CryptographicOperations.ZeroMemory(canonicalPayload);
        }
    }

    private static async Task<RecipientManagementSqlResult> InvokeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string commandText,
        Guid operationId,
        AuthenticatedClientContext actor,
        Guid recipient,
        byte[] idempotencyDigest,
        byte[] equality,
        byte[] payloadDigest,
        Action<NpgsqlParameterCollection> addOperationParameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.Parameters.AddWithValue("operation", operationId);
        command.Parameters.AddWithValue("manager_api_key", actor.ApiKeyId);
        command.Parameters.AddWithValue("manager_principal", actor.PrincipalId);
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("idempotency", idempotencyDigest);
        command.Parameters.AddWithValue("equality", equality);
        command.Parameters.AddWithValue("payload", payloadDigest);
        addOperationParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("C5_SQL_RESULT_MISSING");
        return new(
            reader.GetString(reader.GetOrdinal("Outcome")),
            reader.GetGuid(reader.GetOrdinal("OperationId")),
            reader.IsDBNull(reader.GetOrdinal("ResultSnapshot"))
                ? "{}" : reader.GetString(reader.GetOrdinal("ResultSnapshot")),
            !reader.IsDBNull(reader.GetOrdinal("CandidateCommitted"))
                && reader.GetBoolean(reader.GetOrdinal("CandidateCommitted")));
    }

    private static void AddNullableCandidate(
        NpgsqlParameterCollection parameters,
        ManagedCredentialMaterial? material)
    {
        parameters.Add(new NpgsqlParameter("new_api_key", NpgsqlDbType.Uuid)
            { Value = material is null ? DBNull.Value : material.ApiKeyId });
        parameters.Add(new NpgsqlParameter("key_prefix", NpgsqlDbType.Text)
            { Value = material is null ? DBNull.Value : material.KeyPrefix });
        parameters.Add(new NpgsqlParameter("key_hash", NpgsqlDbType.Bytea)
            { Value = material is null ? DBNull.Value : material.KeyHash });
    }

    private static void AddKey(
        NpgsqlParameterCollection parameters,
        string keyId,
        int version,
        string algorithm,
        ValidatedRecipientPublicKey key,
        DateTimeOffset validFrom,
        DateTimeOffset validUntil)
    {
        parameters.AddWithValue("key_id", keyId);
        parameters.AddWithValue("key_version", version);
        parameters.AddWithValue("algorithm", algorithm);
        parameters.AddWithValue("spki", key.SubjectPublicKeyInfo);
        parameters.AddWithValue("fingerprint", key.Fingerprint);
        parameters.AddWithValue("valid_from", validFrom);
        parameters.AddWithValue("valid_until", validUntil);
    }

    private static SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>> MapCredentialSnapshot(
        RecipientManagementSqlResult result,
        string? credential)
    {
        if (!SuccessOutcome(result.Outcome)) return MapFailure<RecipientManagementWriteResult<ManagedCredentialDto>>(result.Outcome);
        var dto = Deserialize<ManagedCredentialDto>(result.ResultSnapshot);
        var replay = result.Outcome == "ExistingMatchSecretUnavailable" || !result.CandidateCommitted;
        return SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>.Success(new(
            dto with { PresentedKey = replay ? null : credential },
            SuccessStatus(dto.State == "Revoked" ? "RevokeCredential" : "IssueCredential", result.Outcome)));
    }

    private static bool SuccessOutcome(string outcome) => outcome is
        "Created" or "Replaced" or "Revoked" or "Rotated" or "ExistingMatch" or "ExistingMatchSecretUnavailable";

    private static int SuccessStatus(string operationKind, string outcome) =>
        outcome is "ExistingMatch" or "ExistingMatchSecretUnavailable" ? 200
        : operationKind is "EnrollRecipient" or "IssueCredential" or "ReplaceCredential" or "EnrollKey" ? 201
        : 200;

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException("C5_RESULT_SNAPSHOT_INVALID");

    private static SessionOperationResult<T> MapFailure<T>(string outcome) => outcome switch
    {
        "IdempotencyConflict" => Failure<T>(RecipientManagementErrorCodes.IdempotencyConflict, "Idempotency key conflicts with another request.", 409),
        "NotFound" => NotFound<T>(),
        "PrincipalConflict" => Failure<T>(RecipientManagementErrorCodes.PrincipalConflict, "Managed recipient principal conflicts with persisted authority.", 409),
        "CredentialConflict" => Failure<T>(RecipientManagementErrorCodes.CredentialConflict, "Managed credential conflicts with persisted authority.", 409),
        "KeyConflict" => Failure<T>(RecipientManagementErrorCodes.KeyConflict, "Recipient public key conflicts with persisted authority.", 409),
        "DeliveryDrainRequired" => Failure<T>(RecipientManagementErrorCodes.DeliveryDrainRequired, "Outstanding deliveries must drain before rotation.", 409),
        _ => Unavailable<T>(),
    };

    private static SessionOperationResult<T> NotFound<T>() =>
        Failure<T>(RecipientManagementErrorCodes.TargetNotFound, "Managed recipient target was not found.", 404);
    private static SessionOperationResult<T> Unavailable<T>() =>
        Failure<T>(RecipientManagementErrorCodes.Unavailable, "Recipient management is temporarily unavailable.", 503);
    private static SessionOperationResult<T> Failure<T>(string code, string message, int status) =>
        SessionOperationResult<T>.Failure(code, message, status);

    private const string EnrollRecipientSql = """
        SELECT * FROM tagekyc.raw_export_enroll_managed_recipient(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,@principal)
        """;
    private const string IssueCredentialSql = """
        SELECT * FROM tagekyc.raw_export_issue_recipient_credential(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
          @new_api_key,@key_prefix,@key_hash,@expires)
        """;
    private const string ReplaceCredentialSql = """
        SELECT * FROM tagekyc.raw_export_replace_recipient_credential(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
          @current_api_key,@current_revision,@new_api_key,@key_prefix,@key_hash,@expires,@reason)
        """;
    private const string RevokeCredentialSql = """
        SELECT * FROM tagekyc.raw_export_revoke_recipient_credential(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
          @api_key,@expected_revision,@reason)
        """;
    private const string EnrollKeySql = """
        SELECT * FROM tagekyc.raw_export_enroll_recipient_key(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
          @key_id,@key_version,@algorithm,@spki,@fingerprint,@valid_from,@valid_until)
        """;
    private const string RotateKeySql = """
        SELECT * FROM tagekyc.raw_export_rotate_recipient_key(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
          @key_id,@current_version,@current_revision,@new_version,@algorithm,@spki,@fingerprint,
          @valid_from,@valid_until,@reason)
        """;
    private const string RevokeKeySql = """
        SELECT * FROM tagekyc.raw_export_revoke_recipient_key(
          @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
          @key_id,@key_version,@expected_revision,@reason)
        """;
    private const string ReadinessSql = """
        SELECT * FROM tagekyc.raw_export_read_recipient_activation_readiness(@recipient)
        """;
}
