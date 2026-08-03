using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class PostgresAttemptKeyReservationProvider(
    TagEkycDbContext db,
    PostgresKeyProviderOperationMap operationMap,
    IKekOperationProvider provider) : IAttemptKeyReservationProvisioningOperation
{
    public async Task<AttemptKeyProvisioningResult> ProvisionAsync(AttemptKeyProvisioningRequest request, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(request, cancellationToken);
        if (prepared.Outcome is not ("PreparingLive" or "ExistingMatch" or "InProgress"))
            return new(Enum.TryParse<AttemptKeyProvisioningOutcome>(prepared.Outcome, out var result) ? result : AttemptKeyProvisioningOutcome.StateConflict, request.AttemptKeyReservationId, null);

        using var candidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        var wrapped = await provider.WrapDekAsync(new(prepared.KeyProviderId!,prepared.KekId!,prepared.KekVersion!.Value,prepared.KekFingerprint!),
            prepared.ProviderOperationToken!.Value,prepared.AttemptKeyContextFingerprint!,candidate,cancellationToken);
        if (wrapped is not KekWrapResult.Wrapped success)
            return new(wrapped is KekWrapResult.OutcomeUnknown ? AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown : AttemptKeyProvisioningOutcome.ProviderUnavailable, request.AttemptKeyReservationId, null);
        var recorded = await operationMap.RecordWrappedAsync(prepared.ProviderOperationId!.Value,request.AttemptKeyReservationId,
            prepared.PreparationId!.Value,prepared.PreparationFence!.Value,prepared.ProviderOperationToken.Value,success.Material,cancellationToken);
        if (recorded is not ("ResultObserved" or "ExistingMatch"))
            return new(AttemptKeyProvisioningOutcome.StateConflict,request.AttemptKeyReservationId,null);
        var activated = await operationMap.ActivateAsync(request.AttemptKeyReservationId,prepared.PreparationId.Value,prepared.PreparationFence.Value,cancellationToken);
        return new(activated is "Activated" or "AlreadyActivated" ? AttemptKeyProvisioningOutcome.Activated : AttemptKeyProvisioningOutcome.StateConflict,
            request.AttemptKeyReservationId,null);
    }

    private async Task<PrepareResult> PrepareAsync(AttemptKeyProvisioningRequest request, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var command=(NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT * FROM tagekyc.raw_export_prepare_attempt_key_reservation(@r,@a,@s)";
        PostgresKeyProviderOperationMap.Add(command,"r",request.AttemptKeyReservationId); PostgresKeyProviderOperationMap.Add(command,"a",request.AttemptId); PostgresKeyProviderOperationMap.Add(command,"s",request.SourceArtifactId);
        await using var reader=await command.ExecuteReaderAsync(cancellationToken);
        if(!await reader.ReadAsync(cancellationToken)) return new("StateConflict",null,null,null,null,null,null,null,null,null);
        return new(reader.GetString(0),reader.IsDBNull(1)?null:reader.GetGuid(1),reader.IsDBNull(2)?null:reader.GetGuid(2),reader.IsDBNull(3)?null:reader.GetInt64(3),reader.IsDBNull(4)?null:new ProviderOperationToken(reader.GetString(4)),reader.IsDBNull(6)?null:(byte[])reader[6],reader.IsDBNull(7)?null:reader.GetString(7),reader.IsDBNull(8)?null:reader.GetString(8),reader.IsDBNull(9)?null:reader.GetInt32(9),reader.IsDBNull(10)?null:reader.GetString(10));
    }

    private sealed record PrepareResult(string Outcome,Guid? ProviderOperationId,Guid? PreparationId,long? PreparationFence,ProviderOperationToken? ProviderOperationToken,byte[]? AttemptKeyContextFingerprint,string? KeyProviderId,string? KekId,int? KekVersion,string? KekFingerprint);
}
