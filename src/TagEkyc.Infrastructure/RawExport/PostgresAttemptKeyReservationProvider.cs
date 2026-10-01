using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class PostgresAttemptKeyReservationProvider(
    TagEkycDbContext db,
    PostgresKeyProviderOperationMap operationMap,
    IKekOperationProvider provider,
    IKekWrappedMaterialProfileSource materialProfileSource) : IAttemptKeyReservationProvisioningOperation
{
    internal PostgresAttemptKeyReservationProvider(
        TagEkycDbContext db,
        PostgresKeyProviderOperationMap operationMap,
        IKekOperationProvider provider)
        : this(db, operationMap, provider, new LegacyKekWrappedMaterialProfileSource())
    {
    }
    public async Task<AttemptKeyProvisioningResult> ProvisionAsync(AttemptKeyProvisioningRequest request, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(request, materialProfileSource.Current, cancellationToken);
        if (prepared.Outcome is not ("PreparingLive" or "ExistingMatch" or "InProgress"))
            return new(Enum.TryParse<AttemptKeyProvisioningOutcome>(prepared.Outcome, out var result) ? result : AttemptKeyProvisioningOutcome.StateConflict, request.AttemptKeyReservationId, null);

        if (prepared.Outcome == "ExistingMatch")
        {
            var existing = await provider.LookupByOperationTokenAsync(
                prepared.ProviderOperationToken!.Value,
                prepared.AttemptKeyContextFingerprint!,
                cancellationToken);
            return existing switch
            {
                KekOperationLookup.Found found => await CompleteAsync(request, prepared, found.Material, cancellationToken),
                KekOperationLookup.CorruptOrUnverifiable or KekOperationLookup.PositivelyAbsent =>
                    new(AttemptKeyProvisioningOutcome.ProviderCorruptOrUnverifiable,
                        request.AttemptKeyReservationId, null),
                _ => new(AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
                    request.AttemptKeyReservationId, null),
            };
        }

        using var candidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        var wrapped = await provider.WrapDekAsync(new(prepared.KeyProviderId!,prepared.KekId!,prepared.KekVersion!.Value,prepared.KekFingerprint!),
            prepared.ProviderOperationToken!.Value,prepared.AttemptKeyContextFingerprint!,candidate,cancellationToken);
        if (wrapped is not KekWrapResult.Wrapped success)
            return new(wrapped switch
            {
                KekWrapResult.OutcomeUnknown => AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown,
                KekWrapResult.CorruptOrUnverifiable => AttemptKeyProvisioningOutcome.ProviderCorruptOrUnverifiable,
                _ => AttemptKeyProvisioningOutcome.ProviderUnavailable,
            }, request.AttemptKeyReservationId, null);
        return await CompleteAsync(request, prepared, success.Material, cancellationToken);
    }

    private async Task<AttemptKeyProvisioningResult> CompleteAsync(
        AttemptKeyProvisioningRequest request,
        PrepareResult prepared,
        KekWrappedMaterial material,
        CancellationToken cancellationToken)
    {
        var recorded = await operationMap.RecordWrappedAsync(prepared.ProviderOperationId!.Value,request.AttemptKeyReservationId,
            prepared.PreparationId!.Value,prepared.PreparationFence!.Value,prepared.ProviderOperationToken!.Value,material,cancellationToken);
        if (recorded is not ("ResultObserved" or "ExistingMatch"))
            return new(AttemptKeyProvisioningOutcome.StateConflict,request.AttemptKeyReservationId,null);
        var activated = await operationMap.ActivateAsync(request.AttemptKeyReservationId,prepared.PreparationId.Value,prepared.PreparationFence.Value,cancellationToken);
        return new(activated is "Activated" or "AlreadyActivated" ? AttemptKeyProvisioningOutcome.Activated : AttemptKeyProvisioningOutcome.StateConflict,
            request.AttemptKeyReservationId,null);
    }

    private async Task<PrepareResult> PrepareAsync(AttemptKeyProvisioningRequest request, KekWrappedMaterialProfile profile, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var command=(NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT * FROM tagekyc.raw_export_prepare_attempt_key_reservation(@r,@a,@s,@rid,@rv,@sid,@sv)";
        PostgresKeyProviderOperationMap.Add(command,"r",request.AttemptKeyReservationId); PostgresKeyProviderOperationMap.Add(command,"a",request.AttemptId); PostgresKeyProviderOperationMap.Add(command,"s",request.SourceArtifactId);
        PostgresKeyProviderOperationMap.Add(command,"rid",profile.RepresentationId); PostgresKeyProviderOperationMap.Add(command,"rv",profile.RepresentationVersion);
        PostgresKeyProviderOperationMap.Add(command,"sid",profile.WrappingSchemeId); PostgresKeyProviderOperationMap.Add(command,"sv",profile.WrappingSchemeVersion);
        await using var reader=await command.ExecuteReaderAsync(cancellationToken);
        if(!await reader.ReadAsync(cancellationToken)) return new("StateConflict",null,null,null,null,null,null,null,null,null);
        return new(reader.GetString(0),reader.IsDBNull(1)?null:reader.GetGuid(1),reader.IsDBNull(2)?null:reader.GetGuid(2),reader.IsDBNull(3)?null:reader.GetInt64(3),reader.IsDBNull(4)?null:new ProviderOperationToken(reader.GetString(4)),reader.IsDBNull(6)?null:(byte[])reader[6],reader.IsDBNull(7)?null:reader.GetString(7),reader.IsDBNull(8)?null:reader.GetString(8),reader.IsDBNull(9)?null:reader.GetInt32(9),reader.IsDBNull(10)?null:reader.GetString(10));
    }

    private sealed record PrepareResult(string Outcome,Guid? ProviderOperationId,Guid? PreparationId,long? PreparationFence,ProviderOperationToken? ProviderOperationToken,byte[]? AttemptKeyContextFingerprint,string? KeyProviderId,string? KekId,int? KekVersion,string? KekFingerprint);
}
