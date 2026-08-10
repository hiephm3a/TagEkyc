using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class AttemptAeadEncryptionOperationService(
    TagEkycDbContext db,
    IKekOperationProvider provider,
    DurableKeyCustodyOptions options) : IAttemptAeadEncryptionOperation
{
    public async Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(AttemptAeadChunkRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.BoundedAeadOperationDuration);
        var envelope = await ReadEnvelopeAsync(request.AttemptKeyReservationId, timeout.Token) ?? throw new InvalidOperationException("RAW_EXPORT_KEY_NOT_ACTIVE");
        using var lease = await provider.UnwrapDekAsync(envelope.Reference,envelope.Wrapped,envelope.ContextFingerprint,timeout.Token);
        var output=new byte[request.Input.Length]; var tag=new byte[16];
        using var aes=new AesGcm(lease.Material.Span,16);
        aes.Encrypt(request.Nonce.Span,request.Input.Span,output,tag,request.AssociatedData.Span);
        return new(output,tag);
    }

    private static void Validate(AttemptAeadChunkRequest request)
    {
        if(request.AttemptKeyReservationId==Guid.Empty || request.Nonce.Length!=12) throw new ArgumentException("Invalid bounded AEAD request.",nameof(request));
    }

    private async Task<ActiveEnvelope?> ReadEnvelopeAsync(Guid id,CancellationToken token)
    {
        await db.Database.OpenConnectionAsync(token);
        await using var command=(NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT * FROM tagekyc.raw_export_read_active_attempt_key_envelope(@r)"; command.Parameters.AddWithValue("r",id);
        await using var reader=await command.ExecuteReaderAsync(token); if(!await reader.ReadAsync(token)) return null;
        var context=(byte[])reader[1]; var reference=new KekReference(reader.GetString(2),reader.GetString(3),reader.GetInt32(4),reader.GetString(5));
        var wrapped=new KekWrappedMaterial((byte[])reader[8],(byte[])reader[9],(byte[])reader[10],reader.GetString(6),reader.GetInt32(7),"active-envelope",Convert.ToHexString((byte[])reader[11]));
        return new(reference,context,wrapped);
    }
    private sealed record ActiveEnvelope(KekReference Reference,byte[] ContextFingerprint,KekWrappedMaterial Wrapped);
}

internal sealed class AttemptAeadVerificationOperationService(
    TagEkycDbContext db,
    IKekOperationProvider provider,
    DurableKeyCustodyOptions options) : IAttemptAeadVerificationOperation
{
    public async Task<AttemptAeadVerificationResult> DecryptAndVerifyBoundedChunkAsync(
        AttemptAeadChunkRequest request,
        ReadOnlyMemory<byte> authenticationTag,
        CancellationToken cancellationToken)
    {
        if (request.AttemptKeyReservationId == Guid.Empty || request.Nonce.Length != 12)
            throw new ArgumentException("Invalid bounded AEAD request.", nameof(request));
        if (authenticationTag.Length != 16)
            throw new ArgumentException("Authentication tag must contain 16 bytes.", nameof(authenticationTag));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.BoundedAeadOperationDuration);
        ActiveEnvelope? envelope;
        try
        {
            envelope = await ReadEnvelopeAsync(
                request.AttemptKeyReservationId,
                timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return AttemptAeadVerificationResult.KeyAccessIndeterminate();
        }

        if (envelope is null)
            return AttemptAeadVerificationResult.KeyAccessIndeterminate();

        AttemptDekLease lease;
        try
        {
            lease = await provider.UnwrapDekAsync(
                envelope.Reference,
                envelope.Wrapped,
                envelope.ContextFingerprint,
                timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return AttemptAeadVerificationResult.KeyAccessIndeterminate();
        }

        using (lease)
        {
        var output = new byte[request.Input.Length];
        try
        {
            using var aes = new AesGcm(lease.Material.Span, 16);
            try
            {
                aes.Decrypt(
                    request.Nonce.Span,
                    request.Input.Span,
                    authenticationTag.Span,
                    output,
                    request.AssociatedData.Span);
            }
            catch (CryptographicException)
            {
                CryptographicOperations.ZeroMemory(output);
                return AttemptAeadVerificationResult.AuthenticationFailed();
            }

            return AttemptAeadVerificationResult.Verified(output);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(output);
            throw;
        }
        }
    }

    private async Task<ActiveEnvelope?> ReadEnvelopeAsync(Guid id, CancellationToken token)
    {
        await db.Database.OpenConnectionAsync(token);
        await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_active_attempt_key_envelope(@r)";
        command.Parameters.AddWithValue("r", id);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        var context = (byte[])reader[1];
        var reference = new KekReference(
            reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.GetString(5));
        var wrapped = new KekWrappedMaterial(
            (byte[])reader[8], (byte[])reader[9], (byte[])reader[10],
            reader.GetString(6), reader.GetInt32(7), "active-envelope",
            Convert.ToHexString((byte[])reader[11]));
        return new(reference, context, wrapped);
    }

    private sealed record ActiveEnvelope(
        KekReference Reference,
        byte[] ContextFingerprint,
        KekWrappedMaterial Wrapped);
}
