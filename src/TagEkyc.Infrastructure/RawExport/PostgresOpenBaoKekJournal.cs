using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record OpenBaoJournalSnapshot(
    string Outcome,
    Guid ProviderOperationId,
    long RowRevision,
    string JournalState,
    byte[]? OpaquePayload,
    string? ProviderResourceReference,
    string? ProviderOperationReceipt,
    string? ProviderAbsenceProofReceipt,
    string? ProviderCleanupReference,
    string? ProviderCleanupReceipt,
    string MaterialRepresentationId,
    int MaterialRepresentationVersion,
    string WrappingSchemeId,
    int WrappingSchemeVersion);

internal sealed class PostgresOpenBaoKekJournal(TagEkyc.Infrastructure.Persistence.TagEkycDbContext db)
{
    internal Task<OpenBaoJournalSnapshot?> IssueAsync(
        ProviderOperationToken token,
        ReadOnlyMemory<byte> context,
        KekReference reference,
        KekWrappedMaterialProfile profile,
        CancellationToken cancellationToken) =>
        ReadCommandAsync(
            "SELECT * FROM tagekyc.raw_export_openbao_issue_kek_operation(@token,@context,@provider,@kek,@kv,@kf,@rid,@rv,@sid,@sv)",
            command =>
            {
                Add(command,"token",token.Value); Add(command,"context",context.ToArray()); Add(command,"provider",reference.KeyProviderId);
                Add(command,"kek",reference.KekId); Add(command,"kv",reference.KekVersion); Add(command,"kf",reference.KekFingerprint);
                Add(command,"rid",profile.RepresentationId); Add(command,"rv",profile.RepresentationVersion);
                Add(command,"sid",profile.WrappingSchemeId); Add(command,"sv",profile.WrappingSchemeVersion);
            }, cancellationToken);

    internal Task<OpenBaoJournalSnapshot?> RecordWrappedAsync(
        ProviderOperationToken token,
        ReadOnlyMemory<byte> context,
        long expectedRevision,
        byte[] opaquePayload,
        string resourceReference,
        CancellationToken cancellationToken) =>
        ReadCommandAsync(
            "SELECT * FROM tagekyc.raw_export_openbao_record_wrapped(@token,@context,@revision,@payload,@resource)",
            command =>
            {
                Add(command,"token",token.Value); Add(command,"context",context.ToArray()); Add(command,"revision",expectedRevision);
                Add(command,"payload",opaquePayload); Add(command,"resource",resourceReference);
            }, cancellationToken);

    internal Task<OpenBaoJournalSnapshot?> ReadAsync(ProviderOperationToken token, ReadOnlyMemory<byte> context,
        CancellationToken cancellationToken) =>
        ReadCommandAsync(
            "SELECT * FROM tagekyc.raw_export_openbao_read_kek_operation(@token,@context)",
            command => { Add(command,"token",token.Value); Add(command,"context",context.ToArray()); }, cancellationToken);

    internal Task<OpenBaoJournalSnapshot?> ProveAbsenceAsync(ProviderOperationToken token, ReadOnlyMemory<byte> context,
        long expectedRevision, CancellationToken cancellationToken) =>
        ReadCommandAsync(
            "SELECT * FROM tagekyc.raw_export_openbao_prove_absence(@token,@context,@revision)",
            command => { Add(command,"token",token.Value); Add(command,"context",context.ToArray()); Add(command,"revision",expectedRevision); }, cancellationToken);

    internal Task<OpenBaoJournalSnapshot?> RequireCleanupAsync(ProviderOperationToken token, ReadOnlyMemory<byte> context,
        long expectedRevision, CancellationToken cancellationToken) =>
        ReadCommandAsync(
            "SELECT * FROM tagekyc.raw_export_openbao_require_cleanup(@token,@context,@revision)",
            command => { Add(command,"token",token.Value); Add(command,"context",context.ToArray()); Add(command,"revision",expectedRevision); }, cancellationToken);

    internal Task<OpenBaoJournalSnapshot?> CompleteCleanupAsync(string cleanupReference, ReadOnlyMemory<byte> context,
        CancellationToken cancellationToken) =>
        ReadCommandAsync(
            "SELECT * FROM tagekyc.raw_export_openbao_complete_cleanup(@cleanup,@context)",
            command => { Add(command,"cleanup",cleanupReference); Add(command,"context",context.ToArray()); }, cancellationToken);

    private async Task<OpenBaoJournalSnapshot?> ReadCommandAsync(string sql, Action<NpgsqlCommand> bind,
        CancellationToken cancellationToken)
    {
        var wasOpen = db.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
        if (!wasOpen)
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command=(NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.CommandText=sql;
            bind(command);
            await using var reader=await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return null;
            return new(
                reader.GetString(0), reader.GetGuid(1), reader.GetInt64(2), reader.GetString(3),
                reader.IsDBNull(4)?null:(byte[])reader[4], reader.IsDBNull(5)?null:reader.GetString(5),
                reader.IsDBNull(6)?null:reader.GetString(6), reader.IsDBNull(7)?null:reader.GetString(7),
                reader.IsDBNull(8)?null:reader.GetString(8), reader.IsDBNull(9)?null:reader.GetString(9),
                reader.GetString(10),reader.GetInt32(11),reader.GetString(12),reader.GetInt32(13));
        }
        finally
        {
            if (!wasOpen)
                await db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private static void Add(NpgsqlCommand command,string name,object value) => command.Parameters.AddWithValue(name,value);
}
