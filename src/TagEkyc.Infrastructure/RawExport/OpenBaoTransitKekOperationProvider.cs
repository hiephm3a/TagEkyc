using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class OpenBaoTransitKekOperationProvider(
    OpenBaoKekOptions options,
    OpenBaoHttpTransport transport,
    OpenBaoTokenSession tokenSession,
    PostgresOpenBaoKekJournal journal)
    : IKekOperationProvider, IKekProvisioningRecoveryOperation, IDurableKekProviderCapabilitySource,
      IOpenBaoKekProviderReadiness
{
    private const int MaximumOpaqueBytes = 4096;
    public DurableKekProviderCapabilities Capabilities { get; } = new(true,true,true,true);

    public async Task<KekWrapResult> WrapDekAsync(KekReference reference, ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, IAttemptDekCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (!Matches(reference) || attemptKeyContextFingerprint.Length != 32 || candidate.Material.Length != 32)
            return new KekWrapResult.CorruptOrUnverifiable();
        var issued = await journal.IssueAsync(providerOperationToken, attemptKeyContextFingerprint, reference,
            OpenBaoKekWrappedMaterialProfileSource.Profile, cancellationToken).ConfigureAwait(false);
        if (issued is null)
            return new KekWrapResult.OutcomeUnknown();
        if (issued.JournalState == "Wrapped")
            return TryMaterial(issued, out var existing) ? new KekWrapResult.Wrapped(existing) : new KekWrapResult.CorruptOrUnverifiable();
        if (issued.Outcome is not ("Issued" or "ExistingMatch") || issued.JournalState != "Issued")
            return new KekWrapResult.CorruptOrUnverifiable();
        try
        {
            var aad = ComputeAad(reference, attemptKeyContextFingerprint.Span);
            using var response = await PostTransitAsync(
                $"/v1/{options.TransitMount}/encrypt/{Uri.EscapeDataString(options.KeyName)}",
                new
                {
                    plaintext = Convert.ToBase64String(candidate.Material.Span),
                    associated_data = Convert.ToBase64String(aad),
                    key_version = options.KeyVersion,
                }, cancellationToken).ConfigureAwait(false);
            string? ciphertext;
            try
            {
                ciphertext = response.RootElement.GetProperty("data").GetProperty("ciphertext").GetString();
            }
            catch (InvalidOperationException error)
            {
                throw new JsonException("OPENBAO_TRANSIT_RESPONSE_SHAPE_INVALID", error);
            }
            if (!IsValidTransitCiphertext(ciphertext))
                return new KekWrapResult.CorruptOrUnverifiable();
            var opaque = Encoding.UTF8.GetBytes(ciphertext!);
            var recorded = await journal.RecordWrappedAsync(providerOperationToken, attemptKeyContextFingerprint,
                issued.RowRevision, opaque, ResourceReference(reference), cancellationToken).ConfigureAwait(false);
            if (recorded is null || recorded.Outcome is not ("Wrapped" or "ExistingMatch"))
                return new KekWrapResult.OutcomeUnknown();
            return TryMaterial(recorded, out var material)
                ? new KekWrapResult.Wrapped(material)
                : new KekWrapResult.CorruptOrUnverifiable();
        }
        catch (OpenBaoTransportException exception)
        {
            tokenSession.Invalidate();
            return ClassifyWrapFailure(exception);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new KekWrapResult.OutcomeUnknown();
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or FormatException)
        {
            return ClassifyWrapFailure(exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ClassifyWrapFailure(exception);
        }
    }

    public async Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken)
    {
        var row = await journal.ReadAsync(providerOperationToken, attemptKeyContextFingerprint, cancellationToken).ConfigureAwait(false);
        if (row is null) return new KekOperationLookup.Unknown();
        if (row.JournalState == "Wrapped" && TryMaterial(row,out var material)) return new KekOperationLookup.Found(material);
        if (row.JournalState == "AbsenceProven" && DurableKeyText.IsValid(row.ProviderAbsenceProofReceipt!)) return new KekOperationLookup.PositivelyAbsent(row.ProviderAbsenceProofReceipt!);
        return row.JournalState == "Issued" ? new KekOperationLookup.Unknown() : new KekOperationLookup.CorruptOrUnverifiable();
    }

    public async Task<AttemptDekLease> UnwrapDekAsync(KekReference reference, KekWrappedMaterial wrapped,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken)
    {
        if (!Matches(reference) || wrapped is not OpaqueProviderWrappedMaterial opaque
            || attemptKeyContextFingerprint.Length != 32 || opaque.OpaquePayload.Length is < 1 or > MaximumOpaqueBytes
            || !string.Equals(opaque.SuiteId,OpenBaoKekOptions.WrappingSchemeId,StringComparison.Ordinal)
            || opaque.SuiteVersion != OpenBaoKekOptions.WrappingSchemeVersion)
            throw new CryptographicException("OPENBAO_WRAPPED_MATERIAL_INVALID");
        var ciphertext = Encoding.UTF8.GetString(opaque.OpaquePayload);
        if (!IsValidTransitCiphertext(ciphertext)) throw new CryptographicException("OPENBAO_CIPHERTEXT_INVALID");
        var aad = ComputeAad(reference,attemptKeyContextFingerprint.Span);
        using var response = await PostTransitAsync(
            $"/v1/{options.TransitMount}/decrypt/{Uri.EscapeDataString(options.KeyName)}",
            new { ciphertext, associated_data=Convert.ToBase64String(aad) }, cancellationToken).ConfigureAwait(false);
        var plaintextText=response.RootElement.GetProperty("data").GetProperty("plaintext").GetString();
        byte[] plaintext;
        try { plaintext=Convert.FromBase64String(plaintextText ?? string.Empty); }
        catch (FormatException) { throw new CryptographicException("OPENBAO_PLAINTEXT_INVALID"); }
        if(plaintext.Length!=32){CryptographicOperations.ZeroMemory(plaintext);throw new CryptographicException("OPENBAO_PLAINTEXT_INVALID");}
        return AttemptDekLease.CreateOwned(plaintext);
    }

    public async Task<KekProvisioningResolution> ResolveProvisioningOperationAsync(ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,CancellationToken cancellationToken)
    {
        var row=await journal.ReadAsync(providerOperationToken,attemptKeyContextFingerprint,cancellationToken).ConfigureAwait(false);
        if(row is null) return new KekProvisioningResolution.ProviderOutcomeUnknown();
        if(row.JournalState=="Wrapped")
        {
            if(!TryMaterial(row,out _)) return new KekProvisioningResolution.CorruptOrUnverifiable();
            var cleanup=await journal.RequireCleanupAsync(providerOperationToken,attemptKeyContextFingerprint,
                row.RowRevision,cancellationToken).ConfigureAwait(false);
            return cleanup?.JournalState=="CleanupRequired"
                && DurableKeyText.IsValid(cleanup.ProviderCleanupReference!)
                ? new KekProvisioningResolution.ProviderResourceCleanupRequired(cleanup.ProviderCleanupReference!)
                : new KekProvisioningResolution.ProviderOutcomeUnknown();
        }
        if(row.JournalState=="AbsenceProven" && DurableKeyText.IsValid(row.ProviderAbsenceProofReceipt!)) return new KekProvisioningResolution.NoProviderResult(row.ProviderAbsenceProofReceipt!);
        if(row.JournalState=="Issued")
        {
            var absent=await journal.ProveAbsenceAsync(providerOperationToken,attemptKeyContextFingerprint,row.RowRevision,cancellationToken).ConfigureAwait(false);
            return absent?.JournalState=="AbsenceProven" && DurableKeyText.IsValid(absent.ProviderAbsenceProofReceipt!)
                ? new KekProvisioningResolution.NoProviderResult(absent.ProviderAbsenceProofReceipt!)
                : new KekProvisioningResolution.ProviderOutcomeUnknown();
        }
        if(row.JournalState=="CleanupRequired" && DurableKeyText.IsValid(row.ProviderCleanupReference!)) return new KekProvisioningResolution.ProviderResourceCleanupRequired(row.ProviderCleanupReference!);
        return new KekProvisioningResolution.CorruptOrUnverifiable();
    }

    public async Task<KekProvisioningCleanupResult> CleanupProvisioningOperationAsync(string providerCleanupReference,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,CancellationToken cancellationToken)
    {
        if(!DurableKeyText.IsValid(providerCleanupReference) || attemptKeyContextFingerprint.Length!=32)
            return new KekProvisioningCleanupResult.CleanupFailed();
        var row=await journal.CompleteCleanupAsync(providerCleanupReference,attemptKeyContextFingerprint,cancellationToken).ConfigureAwait(false);
        if(row?.JournalState=="CleanedUp" && DurableKeyText.IsValid(row.ProviderCleanupReceipt!))
            return row.Outcome=="AlreadyAbsent" ? new KekProvisioningCleanupResult.AlreadyAbsent(row.ProviderCleanupReceipt!) : new KekProvisioningCleanupResult.Cleaned(row.ProviderCleanupReceipt!);
        return row is null ? new KekProvisioningCleanupResult.CleanupOutcomeUnknown() : new KekProvisioningCleanupResult.CleanupFailed();
    }

    internal static byte[] ComputeAad(KekReference reference,ReadOnlySpan<byte> context) =>
        C1HashCanonical.Compute("tip-88c1-openbao-transit-wrap-aad-v1",
            new C1HashCanonical.Scalar(Convert.ToHexString(context).ToLowerInvariant()),
            new C1HashCanonical.Scalar(reference.KeyProviderId),new C1HashCanonical.Scalar(reference.KekId),
            new C1HashCanonical.Scalar(reference.KekVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(reference.KekFingerprint),new C1HashCanonical.Scalar(OpenBaoKekOptions.WrappingSchemeId),
            new C1HashCanonical.Scalar(OpenBaoKekOptions.WrappingSchemeVersion.ToString(CultureInfo.InvariantCulture)));

    private bool Matches(KekReference reference) => reference==options.Reference;

    internal static KekWrapResult ClassifyWrapFailure(Exception exception) => exception switch
    {
        OpenBaoTransportException or HttpRequestException or IOException or System.Net.Sockets.SocketException =>
            new KekWrapResult.Unavailable(),
        JsonException or KeyNotFoundException or FormatException =>
            new KekWrapResult.CorruptOrUnverifiable(),
        _ => new KekWrapResult.OutcomeUnknown(),
    };

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        using var response = await GetTransitAsync(
            $"/v1/{options.TransitMount}/keys/{Uri.EscapeDataString(options.KeyName)}", cancellationToken)
            .ConfigureAwait(false);
        var data = response.RootElement.GetProperty("data");
        if (!string.Equals(data.GetProperty("name").GetString(), options.KeyName, StringComparison.Ordinal)
            || !string.Equals(data.GetProperty("type").GetString(), "aes256-gcm96", StringComparison.Ordinal)
            || (data.TryGetProperty("derived", out var derived) && derived.GetBoolean())
            || (data.TryGetProperty("exportable", out var exportable) && exportable.GetBoolean())
            || (data.TryGetProperty("allow_plaintext_backup", out var backup) && backup.GetBoolean())
            || !data.TryGetProperty("keys", out var keys)
            || !keys.TryGetProperty(options.KeyVersion.ToString(CultureInfo.InvariantCulture), out _)
            || (data.TryGetProperty("min_decryption_version", out var minimum)
                && minimum.GetInt32() > options.KeyVersion)
            || !string.Equals(ComputeKeyFingerprint(data, options.KeyVersion), options.KeyFingerprint,
                StringComparison.Ordinal))
            throw new InvalidOperationException("OPENBAO_TRANSIT_KEY_REFERENCE_INVALID");
    }

    internal static string ComputeKeyFingerprint(System.Text.Json.JsonElement keyData, int keyVersion)
    {
        var version = keyVersion.ToString(CultureInfo.InvariantCulture);
        if (!keyData.TryGetProperty("name", out var name)
            || !keyData.TryGetProperty("type", out var type)
            || !keyData.TryGetProperty("keys", out var keys)
            || !keys.TryGetProperty(version, out var key)
            || string.IsNullOrWhiteSpace(name.GetString())
            || string.IsNullOrWhiteSpace(type.GetString()))
            throw new InvalidOperationException("OPENBAO_TRANSIT_KEY_REFERENCE_INVALID");
        var versionIdentity = key.ValueKind == System.Text.Json.JsonValueKind.Object
                              && key.TryGetProperty("creation_time", out var creationTime)
            ? creationTime.GetString()
            : key.GetRawText();
        if (string.IsNullOrWhiteSpace(versionIdentity))
            throw new InvalidOperationException("OPENBAO_TRANSIT_KEY_REFERENCE_INVALID");
        return Convert.ToHexString(C1HashCanonical.Compute(
                "tip-88c1-openbao-transit-key-reference-v1",
                new C1HashCanonical.Scalar(name.GetString()!),
                new C1HashCanonical.Scalar(type.GetString()!),
                new C1HashCanonical.Scalar(version),
                new C1HashCanonical.Scalar(versionIdentity)))
            .ToLowerInvariant();
    }

    private async Task<System.Text.Json.JsonDocument> PostTransitAsync(
        string path, object payload, CancellationToken cancellationToken)
    {
        var token = await tokenSession.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try { return await transport.PostAsync(path, payload, token, cancellationToken).ConfigureAwait(false); }
        catch (OpenBaoTransportException exception) when (exception.StatusCode is 401 or 403)
        {
            tokenSession.Invalidate();
            token = await tokenSession.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            return await transport.PostAsync(path, payload, token, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<System.Text.Json.JsonDocument> GetTransitAsync(
        string path, CancellationToken cancellationToken)
    {
        var token = await tokenSession.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try { return await transport.GetAsync(path, token, cancellationToken).ConfigureAwait(false); }
        catch (OpenBaoTransportException exception) when (exception.StatusCode is 401 or 403)
        {
            tokenSession.Invalidate();
            token = await tokenSession.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            return await transport.GetAsync(path, token, cancellationToken).ConfigureAwait(false);
        }
    }
    private static string ResourceReference(KekReference reference) => $"openbao-transit:{reference.KekId}:{reference.KekVersion}";
    private static bool IsValidTransitCiphertext(string? value) => value is { Length: >= 10 and <= MaximumOpaqueBytes } && value.StartsWith("vault:v",StringComparison.Ordinal) && value.All(ch=>ch is >= '!' and <= '~');
    private static bool TryMaterial(OpenBaoJournalSnapshot row,out OpaqueProviderWrappedMaterial material)
    {
        material=null!;
        if(row.OpaquePayload is not {Length:>=1 and <=MaximumOpaqueBytes} payload || !DurableKeyText.IsValid(row.ProviderResourceReference!) || !DurableKeyText.IsValid(row.ProviderOperationReceipt!) || row.MaterialRepresentationId!=KekWrappedMaterialRepresentations.OpaqueProviderCiphertext || row.MaterialRepresentationVersion!=1 || row.WrappingSchemeId!=OpenBaoKekOptions.WrappingSchemeId || row.WrappingSchemeVersion!=1)
            return false;
        material=new(payload,row.WrappingSchemeId,row.WrappingSchemeVersion,row.ProviderResourceReference!,row.ProviderOperationReceipt!);
        return true;
    }
}

internal interface IOpenBaoKekProviderReadiness
{
    Task ValidateAsync(CancellationToken cancellationToken);
}
