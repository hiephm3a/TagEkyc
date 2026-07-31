using System.Security.Cryptography;
using System.Text;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class FixtureAttemptKekCatalog
{
    internal const string KeyProviderId = "fixture-kek-provider-v1";
    internal const string KekId = "fixture-kek-v1";
    internal const int KekVersion = 1;
    internal const string KekFingerprint =
        "f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2";
    internal const string MaterialPurpose =
        "tagekyc-tip88c1-fixture-attempt-kek-material-v1";

    internal bool Matches(AttemptKeyReference reference) =>
        string.Equals(
            reference.KeyProviderId,
            KeyProviderId,
            StringComparison.Ordinal)
        && string.Equals(reference.KekId, KekId, StringComparison.Ordinal)
        && reference.KekVersion == KekVersion
        && string.Equals(
            reference.KekFingerprint,
            KekFingerprint,
            StringComparison.Ordinal);

    internal FixtureWrappedAttemptKey WrapDek(
        AttemptKeyReference reference,
        byte[] dek)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[dek.Length];
        var tag = new byte[16];
        var kek = SHA256.HashData(
            Encoding.UTF8.GetBytes(MaterialPurpose));
        try
        {
            using var aes = new AesGcm(kek, tag.Length);
            aes.Encrypt(
                nonce,
                dek,
                ciphertext,
                tag,
                AssociatedData(reference));
            return new(reference, nonce, ciphertext, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(kek);
        }
    }

    internal byte[] UnwrapDek(FixtureWrappedAttemptKey entry)
    {
        var dek = new byte[32];
        var kek = SHA256.HashData(
            Encoding.UTF8.GetBytes(MaterialPurpose));
        try
        {
            using var aes = new AesGcm(kek, entry.Tag.Length);
            aes.Decrypt(
                entry.Nonce,
                entry.Ciphertext,
                entry.Tag,
                dek,
                AssociatedData(entry.Reference));
            return dek;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(dek);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(kek);
        }
    }

    private static byte[] AssociatedData(AttemptKeyReference reference) =>
        Encoding.UTF8.GetBytes(
            string.Join(
                "|",
                "tagekyc-tip88c1-attempt-dek-wrap-v1",
                reference.AttemptKeyReservationId.ToString("N"),
                reference.KeyProviderId,
                reference.KekId,
                reference.KekVersion,
                reference.KekFingerprint));
}

internal sealed record FixtureWrappedAttemptKey(
    AttemptKeyReference Reference,
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] Tag)
{
    internal byte[] Metadata =>
        [.. Nonce, .. Tag, .. Ciphertext];
}

internal sealed class FixtureWrappedAttemptKeyStore
{
    private readonly object sync = new();
    private readonly Dictionary<Guid, FixtureWrappedAttemptKey> entries = [];

    internal int Count
    {
        get
        {
            lock (sync)
            {
                return entries.Count;
            }
        }
    }

    internal StoreResult CreateOrGet(
        AttemptKeyReference reference,
        bool allowCreate,
        Func<FixtureWrappedAttemptKey> create)
    {
        lock (sync)
        {
            if (entries.TryGetValue(
                    reference.AttemptKeyReservationId,
                    out var existing))
            {
                return existing.Reference == reference
                    ? new(existing, false, false, false)
                    : new(null, false, true, false);
            }

            if (!allowCreate)
            {
                return new(null, false, false, true);
            }

            var created = create();
            entries.Add(reference.AttemptKeyReservationId, created);
            return new(created, true, false, false);
        }
    }

    internal sealed record StoreResult(
        FixtureWrappedAttemptKey? Entry,
        bool Created,
        bool Conflict,
        bool Missing);
}

internal sealed class FixtureAttemptKeyProvider(
    FixtureAttemptKekCatalog catalog,
    FixtureWrappedAttemptKeyStore store) : IAttemptKeyProvider
{
    public Task<AttemptKeyResult> CreateOrGetAttemptKeyAsync(
        AttemptKeyReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();

        if (reference.AttemptKeyReservationId == Guid.Empty
            || string.IsNullOrWhiteSpace(reference.KeyProviderId)
            || string.IsNullOrWhiteSpace(reference.KekId)
            || reference.KekVersion < 1
            || string.IsNullOrWhiteSpace(reference.KekFingerprint))
        {
            return Task.FromResult(
                AttemptKeyResult.Failed(
                    AttemptKeyFailure.InvalidReference));
        }

        byte[]? generatedDek = null;
        try
        {
            var stored = store.CreateOrGet(
                reference,
                catalog.Matches(reference),
                () =>
                {
                    generatedDek = RandomNumberGenerator.GetBytes(32);
                    return catalog.WrapDek(reference, generatedDek);
                });
            if (stored.Conflict)
            {
                return Task.FromResult(
                    AttemptKeyResult.Failed(
                        AttemptKeyFailure.ReservationConflict));
            }

            if (stored.Missing)
            {
                return Task.FromResult(
                    AttemptKeyResult.Failed(
                        AttemptKeyFailure.ReferenceMismatch));
            }

            var entry = stored.Entry
                ?? throw new CryptographicException(
                    "The fixture wrapped-DEK store returned no entry.");
            return Task.FromResult(Recover(entry));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(
                AttemptKeyResult.Failed(
                    AttemptKeyFailure.ProviderFailure));
        }
        finally
        {
            if (generatedDek is not null)
            {
                CryptographicOperations.ZeroMemory(generatedDek);
            }
        }
    }

    private AttemptKeyResult Recover(FixtureWrappedAttemptKey entry)
    {
        try
        {
            var dek = catalog.UnwrapDek(entry);
            return AttemptKeyResult.Succeeded(
                AttemptDekLease.CreateOwned(dek),
                entry.Metadata);
        }
        catch (CryptographicException)
        {
            return AttemptKeyResult.Failed(
                AttemptKeyFailure.ProviderFailure);
        }
    }

}
