using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeRotationPersistenceBoundary(ICaptureRuntimeDbContextFactory contexts)
    : ICaptureRuntimeRotationGateway
{
    public async Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(
        CaptureRuntimeRotationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var db=await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection=(NpgsqlConnection)db.Database.GetDbConnection();
            await using var transaction=await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var command=connection.CreateCommand();
            command.Transaction=transaction;
            command.CommandText="""
                SELECT * FROM tagekyc.capture_runtime_complete_rotation(
                  @rotation,@credential,@generation,@candidate,@operation,@spki,@thumbprint,@proof,@fingerprint,@now)
                """;
            Parameters(command,request);
            command.Parameters.AddWithValue("proof",NpgsqlDbType.Bytea,request.SuccessorProof);
            var result=await ReadAsync(command,false,cancellationToken).ConfigureAwait(false);
            if(result.Error?.StatusCode==503) return result;
            // Includes TerminalizedExpiredAndDenied: expiry/event/result commit before 403.
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch(OperationCanceledException) { throw; }
        catch(PostgresException error) { return MapException(error); }
        catch(NpgsqlException) { return Failure(CaptureRuntimeErrorCodes.NotReady,503); }
    }

    public async Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> ReplayCompletedRotationAsync(
        CaptureRuntimeRotationCommand request,CancellationToken cancellationToken)
    {
        try
        {
            await using var db=await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection=(NpgsqlConnection)db.Database.GetDbConnection();
            await using var command=connection.CreateCommand();
            command.CommandText="""
                SELECT * FROM tagekyc.capture_runtime_replay_completed_rotation(
                  @rotation,@credential,@generation,@candidate,@operation,@spki,@thumbprint,@fingerprint,@now)
                """;
            Parameters(command,request);
            return await ReadAsync(command,true,cancellationToken).ConfigureAwait(false);
        }
        catch(OperationCanceledException) { throw; }
        catch(NpgsqlException) { return Failure(CaptureRuntimeErrorCodes.NotReady,503); }
    }

    private static void Parameters(NpgsqlCommand command,CaptureRuntimeRotationCommand request)
    {
        command.Parameters.AddWithValue("rotation",NpgsqlDbType.Uuid,request.RotationId);
        command.Parameters.AddWithValue("credential",NpgsqlDbType.Uuid,request.CredentialId);
        command.Parameters.AddWithValue("generation",NpgsqlDbType.Bigint,request.PresentedGeneration);
        command.Parameters.AddWithValue("candidate",NpgsqlDbType.Uuid,request.CandidateKeyId);
        command.Parameters.AddWithValue("operation",NpgsqlDbType.Uuid,request.IdempotencyKey);
        command.Parameters.AddWithValue("spki",NpgsqlDbType.Bytea,request.PublicVerifierSpki);
        command.Parameters.AddWithValue("thumbprint",NpgsqlDbType.Bytea,request.PublicKeyThumbprint);
        command.Parameters.AddWithValue("fingerprint",NpgsqlDbType.Bytea,request.RequestFingerprint);
        command.Parameters.AddWithValue("now",NpgsqlDbType.TimestampTz,request.NowUtc);
    }
    private static async Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> ReadAsync(
        NpgsqlCommand command,bool replayOnly,CancellationToken cancellationToken)
    {
        await using var reader=await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if(!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return Failure(replayOnly?CaptureRuntimeErrorCodes.AccessDenied:CaptureRuntimeErrorCodes.NotReady,replayOnly?403:503);
        if(reader.IsDBNull(0)) return Failure(CaptureRuntimeErrorCodes.NotReady,503);
        var code=reader.GetString(0);
        SessionOperationResult<CaptureRuntimeRotationCompletionResponse> result;
        if(code=="Replay" || (!replayOnly && code=="Applied"))
        {
            for(var i=1;i<8;i++)
                if(reader.IsDBNull(i)) return Failure(CaptureRuntimeErrorCodes.NotReady,503);
            var thumb=reader.GetFieldValue<byte[]>(4);
            var response=new CaptureRuntimeRotationCompletionResponse(reader.GetGuid(1),reader.GetInt64(2),
                reader.GetGuid(3),Convert.ToHexString(thumb).ToLowerInvariant(),reader.GetInt64(5),reader.GetInt64(6),reader.GetInt64(7));
            if(response.CredentialId==Guid.Empty || response.Generation<=1 || response.CandidateKeyId==Guid.Empty ||
                thumb.Length!=32 || response.CredentialRevision<=0 || response.InstallationRevision<=0 || response.RotationRevision<=0)
                return Failure(CaptureRuntimeErrorCodes.NotReady,503);
            result=SessionOperationResult<CaptureRuntimeRotationCompletionResponse>.Success(response,isReplay:code=="Replay");
        }
        else
        {
            for(var i=1;i<8;i++)
                if(!reader.IsDBNull(i)) return Failure(CaptureRuntimeErrorCodes.NotReady,503);
            result=code switch
            {
                "TerminalizedExpiredAndDenied" when !replayOnly=>Failure(CaptureRuntimeErrorCodes.AccessDenied,403),
                "Conflict" when !replayOnly=>Failure(CaptureRuntimeErrorCodes.Conflict,409),
                _=>Failure(CaptureRuntimeErrorCodes.NotReady,503)
            };
        }
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Failure(CaptureRuntimeErrorCodes.NotReady,503):result;
    }
    private static SessionOperationResult<CaptureRuntimeRotationCompletionResponse> MapException(PostgresException error) =>
        error.SqlState=="P0001"?error.MessageText switch
        {
            "TIP88C1C6BA_ROTATION_INPUT_INVALID"=>Failure(CaptureRuntimeErrorCodes.RequestInvalid,400),
            "TIP88C1C6BA_ROTATION_DENIED"=>Failure(CaptureRuntimeErrorCodes.AccessDenied,403),
            "TIP88C1C6BA_ROTATION_CONFLICT" or "TIP88C1C6BA_NEXT_ROLE_POLICY_REQUIRED"=>Failure(CaptureRuntimeErrorCodes.Conflict,409),
            _=>Failure(CaptureRuntimeErrorCodes.NotReady,503)
        }:Failure(CaptureRuntimeErrorCodes.NotReady,503);
    private static SessionOperationResult<CaptureRuntimeRotationCompletionResponse> Failure(string code,int status)=>
        SessionOperationResult<CaptureRuntimeRotationCompletionResponse>.Failure(code,
            status==503?"Capture Runtime is not ready.":"Rotation request was denied.",status);
}

