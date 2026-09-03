using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientPackageServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRecipientPackage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = RecipientPackageOptions.Resolve(configuration);
        services.TryAddSingleton(options);
        services.TryAddScoped<RecipientPackageReadinessValidator>();
        if (!options.IsSyntacticallyValid || options.Topology != RecipientPackageTopology.S3CompatibleDurable)
            return services;
        services.TryAddScoped<RecipientPackageRepository>();
        services.TryAddScoped<RecipientPackageCryptoService>();
        services.TryAddScoped<RecipientPackageObjectClientFactory>();
        services.TryAddScoped<S3CompatibleRecipientPackageProvider>();
        services.TryAddScoped<IRecipientPackageObjectWriter>(p => p.GetRequiredService<S3CompatibleRecipientPackageProvider>());
        services.TryAddScoped<IRecipientPackageObjectReader>(p => p.GetRequiredService<S3CompatibleRecipientPackageProvider>());
        services.TryAddScoped<IRecipientPackageObjectLifecycle>(p => p.GetRequiredService<S3CompatibleRecipientPackageProvider>());
        services.TryAddScoped<IRecipientPackagePostureProbe>(p => p.GetRequiredService<S3CompatibleRecipientPackageProvider>());
        services.TryAddScoped<RecipientPackagePreparationProvider>();
        services.TryAddScoped<IC2AssemblyPreparationProvider>(p => p.GetRequiredService<RecipientPackagePreparationProvider>());
        return services;
    }
}

internal sealed class RecipientPackagePreparationProvider(
    RecipientPackageOptions options,
    RecipientPackageRepository repository,
    RawExportAssemblyRepository assemblies,
    RecipientPackageCryptoService crypto,
    IRecipientPackageObjectWriter writer,
    IRecipientPackageObjectReader reader,
    IRecipientPackageObjectLifecycle lifecycle) : IC2AssemblyPreparationProvider
{
    public async Task<C2AssemblyPrepareResult> PrepareAsync(
        C2AssemblyPreparationRequest request,
        Func<Stream, CancellationToken, Task> boundedAssemblyWriter,
        CancellationToken cancellationToken)
    {
        if (!Valid(request) || !Ready(out var provider)) return new(C2AssemblyPrepareOutcome.Conflict, null);
        var lineage = await assemblies.ReadRecoveryContextAsync(request.C2PreparationId, cancellationToken).ConfigureAwait(false);
        if (!LineageMatches(lineage, request))
            return new(C2AssemblyPrepareOutcome.Conflict, null);
        var trustedLineage = lineage!;

        var current = await repository.ReadAsync(RecipientPackageDatabaseCapability.Preparer, request.C2PreparationId, cancellationToken)
            .ConfigureAwait(false);
        if (current is not null)
        {
            if (!Matches(current, request)) return new(C2AssemblyPrepareOutcome.Conflict, null);
            if (current.State is "Prepared" or "Finalized" && current.ProviderReceiptDigest is { Length: 32 })
                return new(C2AssemblyPrepareOutcome.ExistingMatch, current.ProviderReceiptDigest.ToArray());
            if (current.State is "PutInFlight" or "PutOutcomeUnknown")
                return await RecoverPreparedAsync(request, current, provider, cancellationToken).ConfigureAwait(false);
            if (current.State is "Aborted" or "Quarantined" or "AbortAuthorized" or "CleanupPending")
                return new(C2AssemblyPrepareOutcome.Conflict, null);
        }

        var candidate = current is null
            ? await repository.SelectActiveKeyAsync(request.RecipientClientApplicationId, cancellationToken).ConfigureAwait(false)
            : Candidate(current);
        if (candidate.Outcome != "Selected" || candidate.RecipientKeyId is null || candidate.RecipientKeyVersion is null
            || candidate.RecipientKeyFingerprint is not { Length: 32 } || candidate.RecipientPublicKeySpki is null
            || candidate.RecipientKeyRevision is null || !KeyMaterialMatches(candidate.RecipientPublicKeySpki, candidate.RecipientKeyFingerprint))
            return new(C2AssemblyPrepareOutcome.Unavailable, null);

        byte[]? token = null;
        byte[]? equality = null;
        byte[]? tokenDigest = null;
        byte[]? endpoint = null;
        byte[]? binding = null;
        try
        {
            equality = RecipientPackageCodec.PackageEqualityFingerprint(
                request.C2PreparationId, request.AssemblyId, request.AssemblyFingerprint, request.ManifestDigest,
                request.AssemblyDigest, request.AssemblyAuthenticationValue, request.RecipientClientApplicationId,
                candidate.RecipientKeyId, candidate.RecipientKeyVersion.Value, candidate.RecipientKeyFingerprint,
                request.CompleteAssemblyLength);
            var packageId = RecipientPackageCodec.PackageId(request.C2PreparationId, equality);
            var objectKey = RecipientPackageCodec.ObjectKey(packageId);
            endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
            binding = RecipientPackageCodec.ObjectBindingDigest(provider.ProviderConfigurationId, endpoint, provider.BucketName, objectKey);
            if (current is null)
            {
                token = RandomNumberGenerator.GetBytes(32);
                tokenDigest = RecipientPackageCodec.ProviderOperationTokenDigest(token);
            }
            else
            {
                tokenDigest = current.ProviderOperationTokenDigest.ToArray();
            }
            var reserve = await repository.ReserveAsync(new(
                request.C2PreparationId, packageId, request.AssemblyId, trustedLineage.JobId, trustedLineage.AttemptId, trustedLineage.FencingToken,
                request.AssemblyFingerprint, request.ManifestDigest, request.AssemblyDigest, request.AssemblyAuthenticationValue,
                request.CompleteAssemblyLength, request.RecipientClientApplicationId, candidate.RecipientKeyId,
                candidate.RecipientKeyVersion.Value, candidate.RecipientKeyFingerprint, candidate.RecipientKeyRevision.Value,
                equality, tokenDigest, RecipientPackageProviderConfiguration.ProviderKind, provider.ProviderConfigurationId,
                endpoint, provider.BucketName, objectKey, binding, RecipientPackageOptions.PackageProfile), cancellationToken).ConfigureAwait(false);
            if (reserve.Outcome == "ExistingMatch" && reserve.State is "Prepared" or "Finalized"
                && reserve.ProviderReceiptDigest is { Length: 32 })
                return new(C2AssemblyPrepareOutcome.ExistingMatch, reserve.ProviderReceiptDigest.ToArray());
            if (reserve.Outcome is not ("Reserved" or "ExistingMatch") || reserve.RowRevision is null)
                return new(reserve.Outcome == "Unavailable" ? C2AssemblyPrepareOutcome.Unavailable : C2AssemblyPrepareOutcome.Conflict, null);

            var refreshedLineage = await assemblies.ReadRecoveryContextAsync(request.C2PreparationId, cancellationToken).ConfigureAwait(false);
            if (!LineageMatches(refreshedLineage, request)
                || refreshedLineage!.JobId != trustedLineage.JobId
                || refreshedLineage.AttemptId != trustedLineage.AttemptId
                || refreshedLineage.FencingToken != trustedLineage.FencingToken)
                return new(C2AssemblyPrepareOutcome.Conflict, null);

            var encrypted = await crypto.EncryptAsync(request, reserve, boundedAssemblyWriter, cancellationToken).ConfigureAwait(false);
            await using var encryptedSpool = encrypted.Spool;
            var armed = await repository.BeginPutAsync(request.C2PreparationId, reserve.RowRevision.Value,
                encrypted.EnvelopeDigest, encrypted.EncryptedPackageLength, encrypted.PackageCiphertextDigest, cancellationToken).ConfigureAwait(false);
            if (armed.Outcome is not ("PutInFlight" or "ExistingMatch") || armed.RowRevision is null)
                return new(C2AssemblyPrepareOutcome.Conflict, null);
            var locator = Locator(provider, reserve.PackageId!.Value, reserve.ObjectBindingDigest!, endpoint);
            var put = await writer.PutIfAbsentAsync(locator, encrypted.Spool, cancellationToken).ConfigureAwait(false);
            if (put.Outcome == RecipientPackagePutOutcome.Created
                && put.ContentLength == encrypted.EncryptedPackageLength)
                return await CompletePreparedAsync(request, reserve, armed.RowRevision.Value, encrypted, put.EntityTag, cancellationToken).ConfigureAwait(false);
            if (put.Outcome == RecipientPackagePutOutcome.Created)
                return await QuarantineAsync(request, reserve, armed.RowRevision.Value, encrypted,
                    "MetadataConflict", put.ContentLength, put.EntityTag, cancellationToken).ConfigureAwait(false);
            if (put.Outcome == RecipientPackagePutOutcome.Unavailable)
                return new(C2AssemblyPrepareOutcome.Unavailable, null);
            var observation = RecipientPackageCodec.ProviderObservationEvidenceDigest(
                reserve.PackageId.Value, reserve.ObjectBindingDigest!, reserve.ProviderOperationTokenDigest!,
                put.Outcome.ToString(), put.ContentLength, null, null);
            try
            {
                var unknown = await repository.RecordUnknownAsync(request.C2PreparationId, armed.RowRevision.Value, observation, cancellationToken).ConfigureAwait(false);
                if (unknown.RowRevision is null) return new(C2AssemblyPrepareOutcome.OutcomeUnknown, null);
                var recovery = await repository.ReadAsync(RecipientPackageDatabaseCapability.Reconciler, request.C2PreparationId, cancellationToken).ConfigureAwait(false);
                return recovery is null ? new(C2AssemblyPrepareOutcome.OutcomeUnknown, null)
                    : await RecoverPreparedAsync(request, recovery, provider, cancellationToken).ConfigureAwait(false);
            }
            finally { CryptographicOperations.ZeroMemory(observation); }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(C2AssemblyPrepareOutcome.OutcomeUnknown, null);
        }
        catch (Exception exception) when (exception is IOException or CryptographicException or HttpRequestException)
        {
            return new(C2AssemblyPrepareOutcome.Unavailable, null);
        }
        finally
        {
            Zero(token); Zero(equality); Zero(tokenDigest); Zero(endpoint); Zero(binding);
        }
    }

    public async Task<C2AssemblyInspection> GetPreparationAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return new(C2AssemblyInspectionOutcome.Missing, null, null);
        var row = await repository.ReadAsync(RecipientPackageDatabaseCapability.Preparer, id, cancellationToken).ConfigureAwait(false);
        return row is null
            ? new(C2AssemblyInspectionOutcome.Missing, null, null)
            : new(RecipientPackageOutcomeMapper.Inspection(row.State), row.AssemblyFingerprint.ToArray(), row.ProviderReceiptDigest?.ToArray());
    }

    public async Task<C2AssemblyFinalizeResult> FinalizeAsync(Guid id, byte[] assemblyFingerprint, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || assemblyFingerprint is not { Length: 32 }) return new(C2AssemblyFinalizeOutcome.Conflict);
        var row = await repository.ReadAsync(RecipientPackageDatabaseCapability.Preparer, id, cancellationToken).ConfigureAwait(false);
        if (row is null) return new(C2AssemblyFinalizeOutcome.Conflict);
        var result = await repository.FinalizeAsync(id, row.RowRevision, assemblyFingerprint, cancellationToken).ConfigureAwait(false);
        return new(result.Outcome switch
        {
            "Finalized" => C2AssemblyFinalizeOutcome.Finalized,
            "ExistingMatch" => C2AssemblyFinalizeOutcome.ExistingMatch,
            "Unavailable" => C2AssemblyFinalizeOutcome.Unavailable,
            "OutcomeUnknown" => C2AssemblyFinalizeOutcome.OutcomeUnknown,
            _ => C2AssemblyFinalizeOutcome.Conflict,
        });
    }

    public async Task<C2AssemblyAbortResult> AbortAsync(Guid id, byte[] authorization, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || authorization is not { Length: 32 }) return new(C2AssemblyAbortOutcome.Conflict);
        var row = await repository.ReadAsync(RecipientPackageDatabaseCapability.Lifecycle, id, cancellationToken).ConfigureAwait(false);
        if (row is null) return new(C2AssemblyAbortOutcome.Conflict);
        if (row.State == "Aborted") return new(C2AssemblyAbortOutcome.ExistingMatch);
        var authorized = await repository.AuthorizeAbortAsync(id, row.RowRevision, authorization, cancellationToken).ConfigureAwait(false);
        if (authorized.Outcome is not ("AbortAuthorized" or "ExistingMatch") || authorized.RowRevision is null)
            return new(C2AssemblyAbortOutcome.Conflict);
        row = await repository.ReadAsync(RecipientPackageDatabaseCapability.Lifecycle, id, cancellationToken).ConfigureAwait(false);
        if (row is null) return new(C2AssemblyAbortOutcome.OutcomeUnknown);
        if (!Ready(out var provider)) return new(C2AssemblyAbortOutcome.Unavailable);
        if (!ProviderMatches(provider, row))
        {
            await QuarantineProviderIdentityAsync(
                RecipientPackageDatabaseCapability.Lifecycle,
                row,
                cancellationToken).ConfigureAwait(false);
            return new(C2AssemblyAbortOutcome.Conflict);
        }
        var locator = Locator(row);
        var before = await lifecycle.InspectAsync(locator, cancellationToken).ConfigureAwait(false);
        DateTimeOffset? firstAbsenceObservedAtUtc = null;
        if (before.Outcome == RecipientPackageInspectionOutcome.Present)
        {
            if (!Exact(row, before)) return new(C2AssemblyAbortOutcome.Conflict);
            var deleted = await lifecycle.DeleteAsync(locator, cancellationToken).ConfigureAwait(false);
            if (deleted is RecipientPackageDeleteOutcome.Unavailable)
            {
                await RecordCleanupPendingAsync(row, authorized.RowRevision.Value, "ProviderUnavailable", cancellationToken).ConfigureAwait(false);
                return new(C2AssemblyAbortOutcome.Unavailable);
            }
            if (deleted is RecipientPackageDeleteOutcome.OutcomeUnknown)
            {
                await RecordCleanupPendingAsync(row, authorized.RowRevision.Value, "DeleteOutcomeUnknown", cancellationToken).ConfigureAwait(false);
                return new(C2AssemblyAbortOutcome.OutcomeUnknown);
            }
        }
        else if (before.Outcome is RecipientPackageInspectionOutcome.Unavailable) return new(C2AssemblyAbortOutcome.Unavailable);
        else if (before.Outcome is RecipientPackageInspectionOutcome.OutcomeUnknown) return new(C2AssemblyAbortOutcome.OutcomeUnknown);
        else if (before.Outcome == RecipientPackageInspectionOutcome.PositivelyAbsent) firstAbsenceObservedAtUtc = DateTimeOffset.UtcNow;

        if (firstAbsenceObservedAtUtc is null)
        {
            var firstAbsence = await lifecycle.InspectAsync(locator, cancellationToken).ConfigureAwait(false);
            if (firstAbsence.Outcome != RecipientPackageInspectionOutcome.PositivelyAbsent)
            {
                await RecordCleanupPendingAsync(row, authorized.RowRevision.Value,
                    firstAbsence.Outcome == RecipientPackageInspectionOutcome.Unavailable ? "ProviderUnavailable" : "DeleteOutcomeUnknown",
                    cancellationToken).ConfigureAwait(false);
                return firstAbsence.Outcome == RecipientPackageInspectionOutcome.Unavailable
                    ? new(C2AssemblyAbortOutcome.Unavailable)
                    : new(C2AssemblyAbortOutcome.OutcomeUnknown);
            }
            firstAbsenceObservedAtUtc = DateTimeOffset.UtcNow;
        }
        var secondAbsence = await lifecycle.InspectAsync(locator, cancellationToken).ConfigureAwait(false);
        if (secondAbsence.Outcome != RecipientPackageInspectionOutcome.PositivelyAbsent)
        {
            await RecordCleanupPendingAsync(row, authorized.RowRevision.Value,
                secondAbsence.Outcome == RecipientPackageInspectionOutcome.Unavailable ? "ProviderUnavailable" : "DeleteOutcomeUnknown",
                cancellationToken).ConfigureAwait(false);
            return secondAbsence.Outcome == RecipientPackageInspectionOutcome.Unavailable
                ? new(C2AssemblyAbortOutcome.Unavailable)
                : new(C2AssemblyAbortOutcome.OutcomeUnknown);
        }
        var absence = RecipientPackageCodec.PositiveAbsenceEvidenceDigest(
            row.PackageId, row.ObjectBindingDigest, row.ProviderOperationTokenDigest,
            firstAbsenceObservedAtUtc.Value, DateTimeOffset.UtcNow);
        try
        {
            var result = await repository.RecordAbortResultAsync(id, authorized.RowRevision.Value, "PositiveAbsenceConfirmed", absence, cancellationToken).ConfigureAwait(false);
            return new(result.Outcome switch
            {
                "Aborted" => C2AssemblyAbortOutcome.Aborted,
                "ExistingMatch" => C2AssemblyAbortOutcome.ExistingMatch,
                "Unavailable" => C2AssemblyAbortOutcome.Unavailable,
                "OutcomeUnknown" or "CleanupPending" => C2AssemblyAbortOutcome.OutcomeUnknown,
                _ => C2AssemblyAbortOutcome.Conflict,
            });
        }
        finally { Zero(absence); }
    }

    private async Task<C2AssemblyPrepareResult> RecoverPreparedAsync(
        C2AssemblyPreparationRequest request,
        RecipientPackageRecoveryContext row,
        RecipientPackageProviderConfiguration provider,
        CancellationToken cancellationToken)
    {
        if (!ProviderMatches(provider, row))
        {
            await QuarantineProviderIdentityAsync(
                RecipientPackageDatabaseCapability.Reconciler,
                row,
                cancellationToken).ConfigureAwait(false);
            return new(C2AssemblyPrepareOutcome.Conflict, null);
        }
        var inspection = await reader.InspectAsync(Locator(row), cancellationToken).ConfigureAwait(false);
        if (inspection.Outcome == RecipientPackageInspectionOutcome.Present && Exact(row, inspection))
        {
            var entityTagDigest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(inspection.EntityTag ?? string.Empty));
            var conditional = RecipientPackageCodec.ConditionalCreateEvidenceDigest(
                row.ObjectBindingDigest, row.EncryptedPackageLength!.Value, row.PackageCiphertextDigest!, entityTagDigest);
            var receipt = RecipientPackageCodec.ProviderReceiptDigest(request, row.PackageId, row.PackageEqualityFingerprint,
                row.RecipientKeyId, row.RecipientKeyVersion, row.RecipientKeyFingerprint, row.ObjectBindingDigest,
                row.EncryptedPackageLength.Value, row.PackageCiphertextDigest!, row.EnvelopeDigest!, row.ProviderOperationTokenDigest, conditional);
            try
            {
                var result = await repository.RecordPreparedAsync(RecipientPackageDatabaseCapability.Reconciler, row.C2PreparationId,
                    row.RowRevision, entityTagDigest, row.EncryptedPackageLength.Value, row.PackageCiphertextDigest!, conditional, receipt,
                    cancellationToken).ConfigureAwait(false);
                return new(result.Outcome is "Prepared" or "ExistingMatch" ? C2AssemblyPrepareOutcome.Prepared : C2AssemblyPrepareOutcome.Conflict,
                    result.ProviderReceiptDigest ?? receipt.ToArray());
            }
            finally { Zero(entityTagDigest); Zero(conditional); Zero(receipt); }
        }
        if (inspection.Outcome == RecipientPackageInspectionOutcome.Present)
            return await QuarantineAsync(request, row, "ExistingObjectMismatch", inspection, cancellationToken).ConfigureAwait(false);
        if (inspection.Outcome is RecipientPackageInspectionOutcome.PositivelyAbsent or RecipientPackageInspectionOutcome.OutcomeUnknown
            && row.State == "PutInFlight")
        {
            var observation = RecipientPackageCodec.ProviderObservationEvidenceDigest(
                row.PackageId, row.ObjectBindingDigest, row.ProviderOperationTokenDigest,
                inspection.Outcome.ToString(), inspection.ContentLength, inspection.PackageCiphertextDigest, inspection.EnvelopeDigest);
            try
            {
                await repository.RecordUnknownAsync(row.C2PreparationId, row.RowRevision, observation, cancellationToken).ConfigureAwait(false);
            }
            finally { Zero(observation); }
        }
        return inspection.Outcome switch
        {
            RecipientPackageInspectionOutcome.Unavailable => new(C2AssemblyPrepareOutcome.Unavailable, null),
            _ => new(C2AssemblyPrepareOutcome.OutcomeUnknown, null),
        };
    }

    private async Task<C2AssemblyPrepareResult> QuarantineAsync(
        C2AssemblyPreparationRequest request,
        RecipientPackageReserveResult reserve,
        long revision,
        RecipientPackageEncryptionResult encrypted,
        string reason,
        long? observedLength,
        string? entityTag,
        CancellationToken cancellationToken)
    {
        var observation = RecipientPackageCodec.ProviderObservationEvidenceDigest(
            reserve.PackageId!.Value, reserve.ObjectBindingDigest!, reserve.ProviderOperationTokenDigest!,
            "PresentMismatch", observedLength, encrypted.PackageCiphertextDigest, encrypted.EnvelopeDigest);
        var quarantine = RecipientPackageCodec.QuarantineEvidenceDigest(
            reserve.PackageId.Value, reserve.PackageEqualityFingerprint!, reserve.ObjectBindingDigest!,
            reserve.ProviderOperationTokenDigest!, reason, observation);
        try
        {
            await repository.RecordQuarantinedAsync(RecipientPackageDatabaseCapability.Reconciler,
                request.C2PreparationId, revision, quarantine, reason, cancellationToken).ConfigureAwait(false);
            return new(C2AssemblyPrepareOutcome.Conflict, null);
        }
        finally { Zero(observation); Zero(quarantine); }
    }

    private async Task<C2AssemblyPrepareResult> QuarantineAsync(
        C2AssemblyPreparationRequest request,
        RecipientPackageRecoveryContext row,
        string reason,
        RecipientPackageInspection inspection,
        CancellationToken cancellationToken)
    {
        var observation = RecipientPackageCodec.ProviderObservationEvidenceDigest(
            row.PackageId, row.ObjectBindingDigest, row.ProviderOperationTokenDigest, "PresentMismatch",
            inspection.ContentLength, inspection.PackageCiphertextDigest, inspection.EnvelopeDigest);
        var quarantine = RecipientPackageCodec.QuarantineEvidenceDigest(
            row.PackageId, row.PackageEqualityFingerprint, row.ObjectBindingDigest,
            row.ProviderOperationTokenDigest, reason, observation);
        try
        {
            await repository.RecordQuarantinedAsync(RecipientPackageDatabaseCapability.Reconciler,
                request.C2PreparationId, row.RowRevision, quarantine, reason, cancellationToken).ConfigureAwait(false);
            return new(C2AssemblyPrepareOutcome.Conflict, null);
        }
        finally { Zero(observation); Zero(quarantine); }
    }

    private async Task RecordCleanupPendingAsync(
        RecipientPackageRecoveryContext row,
        long revision,
        string resultKind,
        CancellationToken cancellationToken)
    {
        var observation = RecipientPackageCodec.ProviderObservationEvidenceDigest(
            row.PackageId, row.ObjectBindingDigest, row.ProviderOperationTokenDigest,
            resultKind == "ProviderUnavailable" ? "ProviderUnavailable" : "PutOutcomeUnknown",
            null, null, null);
        var pending = RecipientPackageCodec.CleanupProgressEvidenceDigest(
            row.PackageId, row.ObjectBindingDigest, row.ProviderOperationTokenDigest, resultKind, observation);
        try
        {
            await repository.RecordAbortResultAsync(row.C2PreparationId, revision, resultKind, pending, cancellationToken).ConfigureAwait(false);
        }
        finally { Zero(observation); Zero(pending); }
    }

    private async Task QuarantineProviderIdentityAsync(
        RecipientPackageDatabaseCapability capability,
        RecipientPackageRecoveryContext row,
        CancellationToken cancellationToken)
    {
        var observation = RecipientPackageCodec.ProviderObservationEvidenceDigest(
            row.PackageId,
            row.ObjectBindingDigest,
            row.ProviderOperationTokenDigest,
            "ProviderIdentityConflict",
            null,
            null,
            null);
        var quarantine = RecipientPackageCodec.QuarantineEvidenceDigest(
            row.PackageId,
            row.PackageEqualityFingerprint,
            row.ObjectBindingDigest,
            row.ProviderOperationTokenDigest,
            "ProviderIdentityConflict",
            observation);
        try
        {
            await repository.RecordQuarantinedAsync(
                capability,
                row.C2PreparationId,
                row.RowRevision,
                quarantine,
                "ProviderIdentityConflict",
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Zero(observation);
            Zero(quarantine);
        }
    }

    private async Task<C2AssemblyPrepareResult> CompletePreparedAsync(
        C2AssemblyPreparationRequest request, RecipientPackageReserveResult reserve, long revision,
        RecipientPackageEncryptionResult encrypted, string? entityTag, CancellationToken cancellationToken)
    {
        var entityTagDigest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(entityTag ?? string.Empty));
        var conditional = RecipientPackageCodec.ConditionalCreateEvidenceDigest(
            reserve.ObjectBindingDigest!, encrypted.EncryptedPackageLength, encrypted.PackageCiphertextDigest, entityTagDigest);
        var receipt = RecipientPackageCodec.ProviderReceiptDigest(request, reserve.PackageId!.Value,
            reserve.PackageEqualityFingerprint!, reserve.RecipientKeyId!, reserve.RecipientKeyVersion!.Value,
            reserve.RecipientKeyFingerprint!, reserve.ObjectBindingDigest!, encrypted.EncryptedPackageLength,
            encrypted.PackageCiphertextDigest, encrypted.EnvelopeDigest, reserve.ProviderOperationTokenDigest!, conditional);
        try
        {
            var result = await repository.RecordPreparedAsync(RecipientPackageDatabaseCapability.Preparer,
                request.C2PreparationId, revision, entityTagDigest, encrypted.EncryptedPackageLength,
                encrypted.PackageCiphertextDigest, conditional, receipt, cancellationToken).ConfigureAwait(false);
            return new(result.Outcome is "Prepared" or "ExistingMatch" ? C2AssemblyPrepareOutcome.Prepared : C2AssemblyPrepareOutcome.Conflict,
                result.ProviderReceiptDigest ?? receipt.ToArray());
        }
        finally { Zero(entityTagDigest); Zero(conditional); Zero(receipt); }
    }

    private bool Ready(out RecipientPackageProviderConfiguration provider)
    {
        provider = options.Provider!;
        return options.IsSyntacticallyValid && options.Topology == RecipientPackageTopology.S3CompatibleDurable
            && options.Provider is not null;
    }
    private static bool Valid(C2AssemblyPreparationRequest r) => r.C2PreparationId != Guid.Empty && r.AssemblyId != Guid.Empty
        && r.RecipientClientApplicationId != Guid.Empty && r.AssemblyFingerprint is { Length: 32 }
        && r.ManifestDigest is { Length: 32 } && r.AssemblyDigest is { Length: 32 }
        && r.AssemblyAuthenticationValue is { Length: 32 }
        && r.CompleteAssemblyLength is >= 1 and <= RecipientPackageOptions.MaximumCompleteAssemblyLength;
    private static bool Matches(RecipientPackageRecoveryContext row, C2AssemblyPreparationRequest r) =>
        row.AssemblyId == r.AssemblyId && row.RecipientClientApplicationId == r.RecipientClientApplicationId
        && row.CompleteAssemblyLength == r.CompleteAssemblyLength && Fixed(row.AssemblyFingerprint, r.AssemblyFingerprint)
        && Fixed(row.ManifestDigest, r.ManifestDigest) && Fixed(row.AssemblyDigest, r.AssemblyDigest)
        && Fixed(row.AssemblyAuthenticationValue, r.AssemblyAuthenticationValue);
    private static bool LineageMatches(RawExportAssemblyRecoveryContext? lineage, C2AssemblyPreparationRequest request) =>
        lineage is not null && lineage.C2PreparationId == request.C2PreparationId
        && lineage.AssemblyId == request.AssemblyId
        && Fixed(lineage.AssemblyFingerprint, request.AssemblyFingerprint);
    private static bool Exact(RecipientPackageRecoveryContext row, RecipientPackageInspection value) =>
        value.ContentLength == row.EncryptedPackageLength && value.PackageCiphertextDigest is not null
        && value.EnvelopeDigest is not null && row.PackageCiphertextDigest is not null && row.EnvelopeDigest is not null
        && Fixed(value.PackageCiphertextDigest, row.PackageCiphertextDigest) && Fixed(value.EnvelopeDigest, row.EnvelopeDigest);
    private static bool KeyMaterialMatches(ReadOnlySpan<byte> spki, ReadOnlySpan<byte> fingerprint)
    {
        var computed = SHA256.HashData(spki);
        try { return Fixed(computed, fingerprint); }
        finally { Zero(computed); }
    }
    private static bool ProviderMatches(
        RecipientPackageProviderConfiguration provider,
        RecipientPackageRecoveryContext row)
    {
        byte[]? endpoint = null;
        byte[]? binding = null;
        try
        {
            endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
            binding = RecipientPackageCodec.ObjectBindingDigest(
                provider.ProviderConfigurationId,
                endpoint,
                provider.BucketName,
                row.ObjectKey);
            return string.Equals(row.ProviderKind, RecipientPackageProviderConfiguration.ProviderKind, StringComparison.Ordinal)
                && string.Equals(row.ProviderConfigurationId, provider.ProviderConfigurationId, StringComparison.Ordinal)
                && string.Equals(row.BucketName, provider.BucketName, StringComparison.Ordinal)
                && string.Equals(row.ObjectKey, RecipientPackageCodec.ObjectKey(row.PackageId), StringComparison.Ordinal)
                && Fixed(row.ProviderEndpointFingerprint, endpoint)
                && Fixed(row.ObjectBindingDigest, binding);
        }
        finally
        {
            Zero(endpoint);
            Zero(binding);
        }
    }
    private static bool Fixed(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
    private static RecipientKeyCandidate Candidate(RecipientPackageRecoveryContext r) => new("Selected", r.RecipientClientApplicationId,
        r.RecipientKeyId, r.RecipientKeyVersion, r.RecipientKeyFingerprint, r.RecipientPublicKeySpki,
        r.RecipientKeyRevision, r.RecipientKeyValidFromUtc, r.RecipientKeyValidUntilUtc);
    private static RecipientPackageLocator Locator(RecipientPackageRecoveryContext r) =>
        new(r.ProviderKind, r.ProviderConfigurationId, r.ProviderEndpointFingerprint, r.BucketName, r.ObjectKey, r.ObjectBindingDigest);
    private static RecipientPackageLocator Locator(RecipientPackageProviderConfiguration p, Guid packageId, byte[] binding, byte[] endpoint) =>
        new(RecipientPackageProviderConfiguration.ProviderKind, p.ProviderConfigurationId, endpoint.ToArray(), p.BucketName,
            RecipientPackageCodec.ObjectKey(packageId), binding.ToArray());
    private static void Zero(byte[]? value) { if (value is not null) CryptographicOperations.ZeroMemory(value); }
}
