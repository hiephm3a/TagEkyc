using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal static class FixtureDurableKekCatalog
{
    internal const string KeyProviderId = "fixture-kek-provider-v1";
    internal const string KekId = "fixture-kek-v1";
    internal const int KekVersion = 1;
    internal const string KekFingerprint =
        "f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2";
    internal const string WrappingSuiteId = "AES-256-GCM";
    internal const int WrappingSuiteVersion = 1;
    internal const string MaterialPurpose =
        "tagekyc-tip88c1-fixture-attempt-kek-material-v1";

    internal static bool Matches(KekReference reference) =>
        string.Equals(reference.KeyProviderId, KeyProviderId, StringComparison.Ordinal)
        && string.Equals(reference.KekId, KekId, StringComparison.Ordinal)
        && reference.KekVersion == KekVersion
        && string.Equals(reference.KekFingerprint, KekFingerprint, StringComparison.Ordinal);
}

internal sealed record FixtureKekJournalResult(
    string Outcome,
    Guid? FixtureWrapId,
    byte[]? Ciphertext,
    byte[]? Nonce,
    byte[]? Tag,
    string? SuiteId,
    int? SuiteVersion,
    string? ProviderResourceReference,
    string? ProviderOperationReceipt,
    byte[]? WrappedDekMetadataDigest)
{
    public override string ToString() => "FixtureKekJournalResult:<redacted>";
}

internal interface IFixtureKekWrapJournal
{
    Task<FixtureKekJournalResult> ProbeOrCreateAsync(
        ProviderOperationToken token,
        ReadOnlyMemory<byte> contextFingerprint,
        KekWrappedMaterial? candidate,
        CancellationToken cancellationToken);
}

internal interface IFixtureKekLookupJournal
{
    Task<FixtureKekJournalResult> LookupAsync(
        ProviderOperationToken token,
        ReadOnlyMemory<byte> contextFingerprint,
        CancellationToken cancellationToken);
}

internal sealed class PostgresFixtureKekJournal(TagEkycDbContext db)
    : IFixtureKekWrapJournal, IFixtureKekLookupJournal
{
    public Task<FixtureKekJournalResult> ProbeOrCreateAsync(
        ProviderOperationToken token,
        ReadOnlyMemory<byte> contextFingerprint,
        KekWrappedMaterial? candidate,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            "SELECT * FROM tagekyc.raw_export_fixture_kek_wrap(@provider,@token,@context,@ciphertext,@nonce,@tag,@suite,@version)",
            token,
            contextFingerprint,
            candidate,
            cancellationToken);

    public Task<FixtureKekJournalResult> LookupAsync(
        ProviderOperationToken token,
        ReadOnlyMemory<byte> contextFingerprint,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            "SELECT * FROM tagekyc.raw_export_fixture_kek_lookup(@provider,@token,@context)",
            token,
            contextFingerprint,
            null,
            cancellationToken);

    private async Task<FixtureKekJournalResult> ExecuteAsync(
        string sql,
        ProviderOperationToken token,
        ReadOnlyMemory<byte> contextFingerprint,
        KekWrappedMaterial? candidate,
        CancellationToken cancellationToken)
    {
        var connectionWasOpen = db.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
        if (!connectionWasOpen)
            await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("provider", FixtureDurableKekCatalog.KeyProviderId);
            command.Parameters.AddWithValue("token", token.Value);
            command.Parameters.AddWithValue("context", contextFingerprint.ToArray());
            if (sql.Contains("_wrap(", StringComparison.Ordinal))
            {
                AddNullable(command, "ciphertext", NpgsqlDbType.Bytea, candidate?.Ciphertext);
                AddNullable(command, "nonce", NpgsqlDbType.Bytea, candidate?.Nonce);
                AddNullable(command, "tag", NpgsqlDbType.Bytea, candidate?.Tag);
                AddNullable(command, "suite", NpgsqlDbType.Text, candidate?.SuiteId);
                AddNullable(command, "version", NpgsqlDbType.Integer, candidate?.SuiteVersion);
            }
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Fixture KEK journal returned no outcome row.");
            var result = new FixtureKekJournalResult(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetGuid(1),
                reader.IsDBNull(2) ? null : (byte[])reader[2],
                reader.IsDBNull(3) ? null : (byte[])reader[3],
                reader.IsDBNull(4) ? null : (byte[])reader[4],
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : (byte[])reader[9]);
            if (await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("Fixture KEK journal returned multiple outcome rows.");
            return result;
        }
        finally
        {
            if (!connectionWasOpen)
                await db.Database.CloseConnectionAsync();
        }
    }

    private static void AddNullable(
        NpgsqlCommand command,
        string name,
        NpgsqlDbType type,
        object? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type)
        {
            Value = value ?? DBNull.Value,
        });
}

internal sealed class FixtureDurableKekOperationProvider(
    IFixtureKekWrapJournal wrapJournal,
    IFixtureKekLookupJournal lookupJournal)
    : IKekOperationProvider, IKekProvisioningRecoveryOperation,
        IDurableKekProviderCapabilitySource
{
    private static readonly byte[] FixtureKek = SHA256.HashData(
        Encoding.UTF8.GetBytes(FixtureDurableKekCatalog.MaterialPurpose));

    public DurableKekProviderCapabilities Capabilities { get; } =
        new(true, false, true, true);

    public async Task<KekWrapResult> WrapDekAsync(
        KekReference reference,
        ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,
        IAttemptDekCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (!FixtureDurableKekCatalog.Matches(reference)
            || attemptKeyContextFingerprint.Length != 32
            || candidate.Material.Length != 32)
            return new KekWrapResult.CorruptOrUnverifiable();

        FixtureKekJournalResult probe;
        try
        {
            probe = await wrapJournal.ProbeOrCreateAsync(
                providerOperationToken,
                attemptKeyContextFingerprint,
                null,
                cancellationToken);
        }
        catch (PostgresException exception) when (IsPinnedFixtureInputFailure(exception))
        {
            return new KekWrapResult.CorruptOrUnverifiable();
        }
        if (probe.Outcome == "ExistingMatch")
            return ToWrapResult(probe, attemptKeyContextFingerprint);
        if (probe.Outcome == "CorruptOrUnverifiable")
            return new KekWrapResult.CorruptOrUnverifiable();
        if (probe.Outcome != "Missing")
            return new KekWrapResult.OutcomeUnknown();

        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[32];
        var tag = new byte[16];
        byte[]? aad = null;
        try
        {
            aad = ComputeAad(reference, attemptKeyContextFingerprint.Span);
            using (var aes = new AesGcm(FixtureKek, 16))
                aes.Encrypt(nonce, candidate.Material.Span, ciphertext, tag, aad);
            var provisional = new KekWrappedMaterial(
                ciphertext,
                nonce,
                tag,
                FixtureDurableKekCatalog.WrappingSuiteId,
                FixtureDurableKekCatalog.WrappingSuiteVersion,
                "pending",
                "pending");
            var stored = await wrapJournal.ProbeOrCreateAsync(
                providerOperationToken,
                attemptKeyContextFingerprint,
                provisional,
                cancellationToken);
            var result = ToWrapResult(stored, attemptKeyContextFingerprint);
            return result;
        }
        catch (OperationCanceledException)
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            throw;
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            return new KekWrapResult.CorruptOrUnverifiable();
        }
        catch (PostgresException exception) when (IsPinnedFixtureInputFailure(exception))
        {
            return new KekWrapResult.CorruptOrUnverifiable();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            if (aad is not null) CryptographicOperations.ZeroMemory(aad);
        }
    }

    public async Task<KekOperationLookup> LookupByOperationTokenAsync(
        ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,
        CancellationToken cancellationToken)
    {
        if (attemptKeyContextFingerprint.Length != 32)
            return new KekOperationLookup.CorruptOrUnverifiable();
        var result = await lookupJournal.LookupAsync(
            providerOperationToken,
            attemptKeyContextFingerprint,
            cancellationToken);
        return result.Outcome switch
        {
            "Found" when TryMaterial(result, attemptKeyContextFingerprint, out var material) =>
                new KekOperationLookup.Found(material),
            "Unknown" => new KekOperationLookup.Unknown(),
            _ => new KekOperationLookup.CorruptOrUnverifiable(),
        };
    }

    public Task<AttemptDekLease> UnwrapDekAsync(
        KekReference reference,
        KekWrappedMaterial wrapped,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!FixtureDurableKekCatalog.Matches(reference)
            || attemptKeyContextFingerprint.Length != 32
            || wrapped.Ciphertext.Length != 32 || wrapped.Nonce.Length != 12
            || wrapped.Tag.Length != 16
            || !string.Equals(wrapped.SuiteId, FixtureDurableKekCatalog.WrappingSuiteId, StringComparison.Ordinal)
            || wrapped.SuiteVersion != FixtureDurableKekCatalog.WrappingSuiteVersion)
            throw new CryptographicException("Fixture wrapped DEK shape is invalid.");
        var plaintext = new byte[32];
        var aad = ComputeAad(reference, attemptKeyContextFingerprint.Span);
        try
        {
            using var aes = new AesGcm(FixtureKek, 16);
            aes.Decrypt(wrapped.Nonce, wrapped.Ciphertext, wrapped.Tag, plaintext, aad);
            return Task.FromResult(AttemptDekLease.CreateOwned(plaintext));
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aad);
        }
    }

    public async Task<KekProvisioningResolution> ResolveProvisioningOperationAsync(
        ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,
        CancellationToken cancellationToken)
    {
        var lookup = await LookupByOperationTokenAsync(
            providerOperationToken,
            attemptKeyContextFingerprint,
            cancellationToken);
        return lookup switch
        {
            KekOperationLookup.Found found =>
                new KekProvisioningResolution.WrappedResultRecovered(found.Material),
            KekOperationLookup.PositivelyAbsent absent =>
                new KekProvisioningResolution.NoProviderResult(absent.AbsenceProofReceipt),
            KekOperationLookup.Unknown => new KekProvisioningResolution.ProviderOutcomeUnknown(),
            _ => new KekProvisioningResolution.CorruptOrUnverifiable(),
        };
    }

    public Task<KekProvisioningCleanupResult> CleanupProvisioningOperationAsync(
        string providerCleanupReference,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsFixtureReference(providerCleanupReference)
            || attemptKeyContextFingerprint.Length != 32)
            return Task.FromResult<KekProvisioningCleanupResult>(
                new KekProvisioningCleanupResult.CleanupFailed());
        var digest = C1HashCanonical.Compute(
            "tip-88c1-fixture-kek-cleanup-v1",
            new C1HashCanonical.Scalar(providerCleanupReference),
            new C1HashCanonical.Scalar(Hex(attemptKeyContextFingerprint.Span)));
        return Task.FromResult<KekProvisioningCleanupResult>(
            new KekProvisioningCleanupResult.Cleaned(
                "fixture-cleaned:" + Hex(digest)));
    }

    public override string ToString() => "FixtureDurableKekOperationProvider:<redacted>";

    private static KekWrapResult ToWrapResult(
        FixtureKekJournalResult result,
        ReadOnlyMemory<byte> contextFingerprint) =>
        result.Outcome switch
        {
            "Created" or "ExistingMatch"
                when TryMaterial(result, contextFingerprint, out var material) =>
                    new KekWrapResult.Wrapped(material),
            "CorruptOrUnverifiable" => new KekWrapResult.CorruptOrUnverifiable(),
            _ => new KekWrapResult.OutcomeUnknown(),
        };

    private static bool TryMaterial(
        FixtureKekJournalResult result,
        ReadOnlyMemory<byte> contextFingerprint,
        out KekWrappedMaterial material)
    {
        material = null!;
        if (result.FixtureWrapId is not { } id || id == Guid.Empty
            || result.Ciphertext is not { Length: 32 } ciphertext
            || result.Nonce is not { Length: 12 } nonce
            || result.Tag is not { Length: 16 } tag
            || result.WrappedDekMetadataDigest is not { Length: 32 } digest
            || result.SuiteId is not { } suiteId
            || !string.Equals(suiteId, FixtureDurableKekCatalog.WrappingSuiteId, StringComparison.Ordinal)
            || result.SuiteVersion != FixtureDurableKekCatalog.WrappingSuiteVersion)
            return false;
        var expectedDigest = ComputeMetadataDigest(
            contextFingerprint.Span,
            suiteId,
            result.SuiteVersion.Value,
            nonce,
            ciphertext,
            tag);
        var expectedResource = "fixture-wrap:" + id.ToString("N");
        var expectedReceipt = "fixture-receipt:" + Hex(C1HashCanonical.Compute(
            "tip-88c1-fixture-kek-receipt-v1",
            new C1HashCanonical.Scalar(expectedResource),
            new C1HashCanonical.Scalar(Hex(contextFingerprint.Span)),
            new C1HashCanonical.Scalar(Hex(expectedDigest))));
        if (!CryptographicOperations.FixedTimeEquals(digest, expectedDigest)
            || !string.Equals(result.ProviderResourceReference, expectedResource, StringComparison.Ordinal)
            || !string.Equals(result.ProviderOperationReceipt, expectedReceipt, StringComparison.Ordinal))
            return false;
        material = new KekWrappedMaterial(
            ciphertext,
            nonce,
            tag,
            suiteId,
            result.SuiteVersion.Value,
            expectedResource,
            expectedReceipt);
        return true;
    }

    internal static byte[] ComputeMetadataDigest(
        ReadOnlySpan<byte> contextFingerprint,
        string suiteId,
        int suiteVersion,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag) =>
        C1HashCanonical.Compute(
            "tip-88c1-wrapped-dek-metadata-v1",
            new C1HashCanonical.Scalar(Hex(contextFingerprint)),
            new C1HashCanonical.Scalar(suiteId),
            new C1HashCanonical.Scalar(suiteVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(Hex(nonce)),
            new C1HashCanonical.Scalar(Hex(ciphertext)),
            new C1HashCanonical.Scalar(Hex(tag)));

    private static byte[] ComputeAad(
        KekReference reference,
        ReadOnlySpan<byte> contextFingerprint) =>
        C1HashCanonical.Compute(
            "tip-88c1-fixture-kek-wrap-aad-v1",
            new C1HashCanonical.Scalar(Hex(contextFingerprint)),
            new C1HashCanonical.Scalar(reference.KeyProviderId),
            new C1HashCanonical.Scalar(reference.KekId),
            new C1HashCanonical.Scalar(reference.KekVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(reference.KekFingerprint),
            new C1HashCanonical.Scalar(FixtureDurableKekCatalog.WrappingSuiteId),
            new C1HashCanonical.Scalar(FixtureDurableKekCatalog.WrappingSuiteVersion.ToString(CultureInfo.InvariantCulture)));

    private static bool IsFixtureReference(string value) =>
        value is { Length: 45 }
        && value.StartsWith("fixture-wrap:", StringComparison.Ordinal)
        && value.AsSpan(13).ToString().All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string Hex(ReadOnlySpan<byte> value) =>
        Convert.ToHexString(value).ToLowerInvariant();

    private static bool IsPinnedFixtureInputFailure(PostgresException exception) =>
        exception.SqlState == "P0001"
        && exception.MessageText is
            "RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH" or
            "RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID";
}
