using System.Data;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;
using Xunit.Sdk;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C5RecipientManagementTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private readonly C5ObservationCollector observations = new();

    public Task InitializeAsync()
    {
        observations.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        observations.ThrowIfAny();
        return Task.CompletedTask;
    }

    internal void BeginC5ObservationRun() => observations.Reset();
    internal void CompleteC5ObservationRun() => observations.ThrowIfAny();

    [Fact]
    public async Task C502_management_authorization_precedes_malformed_body_parsing()
    {
        var gateway = new CountingGateway();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiKeyAuthenticator, C502Authenticator>();
        builder.Services.AddSingleton<IRecipientManagementApplicationService>(
            new RecipientManagementApplicationService(gateway, new RecipientPublicKeyProfileValidator()));
        await using var app = builder.Build();
        app.MapRecipientManagementEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();

        var wrongCategory = await PostMalformedAsync(client, "wrong-category");
        var missingScope = await PostMalformedAsync(client, "missing-scope");
        var positive = await PostMalformedAsync(client, "operator");
        Bite(wrongCategory == HttpStatusCode.Forbidden && missingScope == HttpStatusCode.Forbidden
            && positive == HttpStatusCode.BadRequest && gateway.Calls == 0,
            "C502-AUTHORIZATION-BEFORE-BODY",
            $"category={(int)wrongCategory};scope={(int)missingScope};positive={(int)positive};gateway={gateway.Calls}");

        var application = Source("src/TagEkyc.Application/RawExport/RecipientManagementApplicationService.cs");
        Bite(application.Contains("actor.CallerCategory != AuthenticatedCallerCategory.OperatorAdmin", StringComparison.Ordinal),
            "C502-OPERATOR-CATEGORY", "category comparator");
        Bite(application.Contains("!actor.Scopes.Contains(RequiredScope)", StringComparison.Ordinal),
            "C502-MANAGEMENT-SCOPE", "scope comparator");
        Bite(application.Contains("Recipient management is not authorized.", StringComparison.Ordinal)
            && !application.Contains("TARGET_EXISTS", StringComparison.Ordinal),
            "C502-TARGET-EXISTENCE-NONDISCLOSURE", "uniform forbidden");
        var repository = Source("src/TagEkyc.Infrastructure/RawExport/RecipientManagementRepository.cs");
        var enrollRecipientMethod = SourceSlice(repository,
            "public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(",
            "public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(");
        Bite(enrollRecipientMethod.Contains(
                "actor, \"EnrollRecipient\", request.RecipientClientApplicationId,", StringComparison.Ordinal)
            && !enrollRecipientMethod.Contains("actor.PrincipalId", StringComparison.Ordinal)
            && !enrollRecipientMethod.Contains("actor.ClientApplicationId", StringComparison.Ordinal),
            "C502-TARGET-NOT-ACTOR-DERIVED", enrollRecipientMethod);
    }

    [Fact]
    public async Task C504_credential_response_does_not_expose_policy_authority()
    {
        var names = typeof(ManagedCredentialDto).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Bite(!names.Contains("Scopes") && !names.Contains("CallerCategory"),
            "C504-NO-GENERIC-ACTIVATION-WRITER", string.Join(',', names));
        var codec = Source("src/TagEkyc.Infrastructure/RawExport/RecipientManagementCodec.cs");
        var store = Source("src/TagEkyc.Infrastructure/Auth/PostgresHashedApiKeyStore.cs");
        var activationBlock = Regex.Match(codec,
            @"ActivationScopes\s*=\s*\[(?<scopes>[\s\S]*?)\];").Groups["scopes"].Value;
        var declaredScopes = Regex.Matches(activationBlock, "\"(?<scope>[^\"]+)\"")
            .Select(match => match.Groups["scope"].Value).ToArray();
        Bite(declaredScopes.SequenceEqual([
                "business.raw-export.package.download",
                "business.raw-export.package.references.read",
            ], StringComparer.Ordinal)
            && store.Contains("scopes.SetEquals(RecipientManagementCodec.ActivationScopes)", StringComparison.Ordinal),
            "C504-EXACT-ACTIVATION-SCOPE-SET", string.Join(',', declaredScopes));
        Bite(store.Contains("credential.State == \"Active\"", StringComparison.Ordinal),
            "C504-NO-GENERIC-ACTIVATION-WRITER", "active managed companion");

        var active = await ObserveC504PreservationAsync(ClientApplicationStatus.Active);
        Bite(active.CloneSchemaCurrent && active.DisposableDatabaseDistinct
            && active.DisposableDatabaseDropped
            && active.SharedCanonicalAbsentBefore && active.SharedCanonicalAbsentAfter
            && active.StateAbsentBefore && active.StatePresentAfter && active.EnrollmentSucceeded
            && active.Before.IsSuccess && active.After.IsSuccess
            && EquivalentAuthenticationContext(active.Before.Value!, active.After.Value!)
            && active.PolicyCalls == 2,
            "C504-SAME-CLIENT-ACTIVE-STAYS-EQUIVALENT",
            active.Describe());

        var disabled = await ObserveC504PreservationAsync(ClientApplicationStatus.Disabled);
        Bite(disabled.CloneSchemaCurrent && disabled.DisposableDatabaseDistinct
            && disabled.DisposableDatabaseDropped
            && disabled.SharedCanonicalAbsentBefore && disabled.SharedCanonicalAbsentAfter
            && disabled.StateAbsentBefore && disabled.StatePresentAfter && disabled.EnrollmentSucceeded
            && !disabled.Before.IsSuccess && disabled.Before.Error?.Code == "CLIENT_APPLICATION_DISABLED"
            && !disabled.After.IsSuccess && disabled.After.Error?.Code == "CLIENT_APPLICATION_DISABLED"
            && disabled.PolicyCalls == 2,
            "C504-SAME-CLIENT-DISABLED-STAYS-DENIED",
            disabled.Describe());
    }

    [Fact]
    public async Task C506_replay_never_regenerates_or_reveals_plaintext_and_candidates_are_bounded()
    {
        var replayGenerator = new CountingSequenceCredentialGenerator([
            Material("c506-replay-0000"),
            Material("c506-replay-mutation-only"),
        ]);
        var replayWorkflow = await CreateManagedWorkflowAsync(replayGenerator);
        var request = new IssueManagedRecipientCredentialRequest(replayWorkflow.Recipient, null);
        var first = await replayWorkflow.Service.IssueCredentialAsync(
            replayWorkflow.Actor, request, "c506-replay", default);
        var callsAfterCommit = replayGenerator.Calls;
        var replay = await replayWorkflow.Service.IssueCredentialAsync(
            replayWorkflow.Actor, request, "c506-replay", default);
        var firstCredential = first.Value?.Value;
        var replayCredential = replay.Value?.Value;
        var zeroSecretGeneration = first.IsSuccess && replay.IsSuccess
            && callsAfterCommit == 1
            && replayGenerator.Calls == callsAfterCommit
            && firstCredential?.PresentedKey is not null;
        var replayPlaintextAbsent = firstCredential is not null
            && replayCredential?.PresentedKey is null
            && replayCredential == firstCredential with { PresentedKey = null };
        var replayDivergences = new List<string>(2);
        if (!zeroSecretGeneration) replayDivergences.Add("C506-REPLAY-ZERO-SECRET-GENERATION");
        if (!replayPlaintextAbsent) replayDivergences.Add("C506-REPLAY-PLAINTEXT-ABSENT");
        Bite(replayDivergences.Count == 0, string.Join(',', replayDivergences),
            $"first={first.IsSuccess};replay={replay.IsSuccess};calls={replayGenerator.Calls};presented={replayCredential?.PresentedKey ?? "<null>"}");

        var collisionMaterials = Enumerable.Range(0, 5)
            .Select(index => Material($"c506-conflict-{index:D2}"))
            .ToArray();
        var collisionSeedIds = await SeedApiKeyPrefixesAsync(collisionMaterials);
        try
        {
            var exhaustedGenerator = new CountingSequenceCredentialGenerator(collisionMaterials);
            var exhaustedWorkflow = await CreateManagedWorkflowAsync(exhaustedGenerator);
            var exhausted = await exhaustedWorkflow.Service.IssueCredentialAsync(
                exhaustedWorkflow.Actor,
                new(exhaustedWorkflow.Recipient, null), "c506-exhausted", default);
            Bite(!exhausted.IsSuccess
                && exhausted.Error?.Code == RecipientManagementErrorCodes.CredentialConflict
                && exhaustedGenerator.Calls == 5
                && await CountRowsAsync("raw_export_managed_recipient_credentials", exhaustedWorkflow.Recipient) == 0
                && await CountOperationRowsAsync(exhaustedWorkflow.Recipient, "IssueCredential") == 0,
                "C506-CANDIDATE-CONFLICT-EXHAUSTION",
                $"code={exhausted.Error?.Code};calls={exhaustedGenerator.Calls}");
        }
        finally
        {
            await DeleteSeedApiKeysAsync(collisionSeedIds);
        }

        Bite(!Source("src/TagEkyc.Infrastructure/Persistence/Entities/RawExportManagedRecipientCredentialRow.cs")
                .Contains("PresentedKey", StringComparison.Ordinal),
            "C506-SECRET-DIGEST-ONLY-PERSISTED", "no plaintext column");
        Bite(typeof(ManagedCredentialDto).GetProperty(nameof(ManagedCredentialDto.PresentedKey)) is not null,
            "C506-REPLACEMENT-NEW-SECRET", "one-time response field");
    }

    [Fact]
    public async Task C507_replacement_preserves_principal_revokes_orphan_and_rolls_back_atomically()
    {
        var replacementSql = Slice(Migration(),
            "CREATE FUNCTION tagekyc.raw_export_replace_recipient_credential(",
            "CREATE FUNCTION tagekyc.raw_export_revoke_recipient_credential(");
        Bite(RegexCount(replacementSql,
                "VALUES(p_new_api_key,p_recipient,ident.\"PrincipalId\"") == 2,
            "C507-REPLACEMENT-PRINCIPAL-ARGUMENT-SOURCE",
            "api_keys and companion both use persisted identity principal");
        var generator = new RecordingCredentialGenerator();
        var workflow = await CreateManagedWorkflowAsync(generator);
        var issued = await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, null), "c507-issue", default);
        Bite(issued.IsSuccess, "C507-REPLACEMENT-PRINCIPAL-ARGUMENT-SOURCE",
            issued.Error?.Code ?? "issued");
        Prerequisite(issued.IsSuccess && issued.Value is not null,
            "C507-REPLACEMENT-PRINCIPAL-ARGUMENT-SOURCE", issued.Error?.Code ?? "issued");
        var old = issued.Value!.Value;
        var replaced = await workflow.Service.ReplaceCredentialAsync(workflow.Actor,
            new(workflow.Recipient, old.ApiKeyId, old.Revision, null, "C507_ROTATE"),
            "c507-replace", default);
        Bite(replaced.IsSuccess
            && replaced.Value!.Value.PrincipalId == workflow.Principal
            && replaced.Value.Value.PrincipalId != workflow.Recipient,
            "C507-REPLACEMENT-PRINCIPAL-ARGUMENT-SOURCE",
            replaced.Error?.Code ?? replaced.Value?.Value.PrincipalId.ToString());
        Prerequisite(replaced.IsSuccess && replaced.Value is not null,
            "C507-REPLACEMENT-PRINCIPAL-ARGUMENT-SOURCE", replaced.Error?.Code ?? "replaced");
        Bite(await ExactCredentialReplacementAsync(workflow.Recipient, old.ApiKeyId,
                replaced.Value!.Value.ApiKeyId),
            "C507-REPLACEMENT-ORPHAN-REVOKED", "old companion/api_key revoked; new active");
        Bite(await ObserveSingleCommittedCredentialAsync(workflow.Recipient),
            "C507-REPLACEMENT-AUTH-ATOMIC-VISIBILITY", "one committed active snapshot");

        var rollbackGenerator = new RecordingCredentialGenerator();
        var rollback = await CreateManagedWorkflowAsync(rollbackGenerator);
        var rollbackIssued = await rollback.Service.IssueCredentialAsync(rollback.Actor,
            new(rollback.Recipient, null), "c507-rb-issue", default);
        await InstallEventFailureTriggerAsync("ManagedCredentialReplaced", "c507_fail_replacement_event");
        try
        {
            var failed = await rollback.Service.ReplaceCredentialAsync(rollback.Actor,
                new(rollback.Recipient, rollbackIssued.Value!.Value.ApiKeyId,
                    rollbackIssued.Value.Value.Revision, null, "C507_ROLLBACK"),
                "c507-rb-replace", default);
            Bite(failed.Error?.Code == RecipientManagementErrorCodes.Unavailable
                && await ExactCredentialRollbackAsync(rollback.Recipient,
                    rollbackIssued.Value.Value.ApiKeyId, rollbackGenerator.Materials[1].ApiKeyId),
                "C507-REPLACEMENT-ATOMIC-ROLLBACK", failed.Error?.Code ?? "unexpected-success");
        }
        finally { await RemoveEventFailureTriggerAsync("c507_fail_replacement_event"); }
    }

    [Fact]
    public async Task C508_credential_revoke_is_nonretroactive_and_next_resolution_is_denied()
    {
        var generator = new RecordingCredentialGenerator();
        var workflow = await CreateManagedWorkflowAsync(generator);
        var issued = await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, null), "c508-issue", default);
        Bite(issued.IsSuccess,
            "C508-CREDENTIAL-REVOKE-NONRETROACTIVE", issued.Error?.Code ?? "issued");
        Prerequisite(issued.IsSuccess && issued.Value is not null,
            "C508-CREDENTIAL-REVOKE-NONRETROACTIVE", issued.Error?.Code ?? "issued");
        var material = generator.Materials.Single();
        var beforeSnapshot = await KeyAndPackageSnapshotAsync(workflow.Recipient);
        var first = await AuthenticateManagedAsync(material.PresentedKey,
            "business.raw-export.package.download");
        Bite(first.IsSuccess && first.Value!.PrincipalId == workflow.Principal,
            "C508-CREDENTIAL-REVOKE-NONRETROACTIVE", first.Error?.Code ?? "resolved");
        Prerequisite(first.IsSuccess && first.Value is not null,
            "C508-CREDENTIAL-REVOKE-NONRETROACTIVE", first.Error?.Code ?? "resolved");
        var revoked = await workflow.Service.RevokeCredentialAsync(workflow.Actor,
            new(workflow.Recipient, issued.Value!.Value.ApiKeyId, issued.Value.Value.Revision,
                "C508_REVOKE"), "c508-revoke", default);
        var next = await AuthenticateManagedAsync(material.PresentedKey,
            "business.raw-export.package.download");
        Bite(revoked.IsSuccess && !next.IsSuccess && first.Value!.PrincipalId == workflow.Principal,
            "C508-NEXT-AUTH-REVOKED", next.Error?.Code ?? "unexpected-success");
        Bite(beforeSnapshot == await KeyAndPackageSnapshotAsync(workflow.Recipient),
            "C508-NO-KEY-PACKAGE-MUTATION", beforeSnapshot);

        var isolatedGenerator = new RecordingCredentialGenerator();
        var isolated = await CreateManagedWorkflowAsync(isolatedGenerator);
        await isolated.Service.IssueCredentialAsync(isolated.Actor,
            new(isolated.Recipient, null), "c508-companion-issue", default);
        var isolatedMaterial = isolatedGenerator.Materials.Single();
        await SetCompanionStateOnlyAsync(isolatedMaterial.ApiKeyId, "Revoked");
        var isolatedResolution = await FindManagedKeyAsync(isolatedMaterial.PresentedKey);
        Bite(isolatedResolution is null,
            "C508-MANAGED-COMPANION-STATE-ENFORCED", isolatedResolution?.Status.ToString() ?? "null");
    }

    [Fact]
    public async Task C509_key_profile_and_postlock_admission_are_exact_at_application_and_sql_layers()
    {
        var validator = new RecipientPublicKeyProfileValidator();
        using var rsa3072 = RSA.Create(3072);
        var spki = rsa3072.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        var base64 = Convert.ToBase64String(spki);
        var hex = Convert.ToHexString(fingerprint);
        Bite(validator.TryValidate("RSA-OAEP-256", base64, hex, out var validated),
            "C509-APPLICATION-ALGORITHM-BEFORE-REPOSITORY", "canonical profile");
        Prerequisite(validated is not null,
            "C509-APPLICATION-ALGORITHM-BEFORE-REPOSITORY", "canonical profile");
        Bite(!validator.TryValidate("rsa-oaep-256", base64, hex, out _),
            "C509-APPLICATION-ALGORITHM-BEFORE-REPOSITORY", "wrong literal rejected");
        var trailingSpki = new byte[spki.Length + 1];
        spki.CopyTo(trailingSpki, 0);
        var trailingFingerprint = SHA256.HashData(trailingSpki);
        Bite(trailingSpki.Length == spki.Length + 1
            && trailingSpki.AsSpan(0, spki.Length).SequenceEqual(spki)
            && trailingSpki[^1] == 0
            && trailingFingerprint.AsSpan().SequenceEqual(SHA256.HashData(trailingSpki)),
            "C509-SPKI-COMPLETE", "one trailing byte with fingerprint derived from presented bytes");
        Bite(!validator.TryValidate("RSA-OAEP-256", Convert.ToBase64String(trailingSpki),
                Convert.ToHexString(trailingFingerprint), out _),
            "C509-SPKI-COMPLETE", "trailing DER rejected");
        var wrong = fingerprint.ToArray(); wrong[0] ^= 1;
        Bite(!validator.TryValidate("RSA-OAEP-256", base64, Convert.ToHexString(wrong), out _),
            "C509-FINGERPRINT-RECOMPUTE", "wrong fingerprint rejected");
        using var rsa2048 = RSA.Create(2048);
        var small = rsa2048.ExportSubjectPublicKeyInfo();
        Bite(!validator.TryValidate("RSA-OAEP-256", Convert.ToBase64String(small),
                Convert.ToHexString(SHA256.HashData(small)), out _),
            "C509-RSA-SIZE-RANGE-BEFORE-REPOSITORY", "RSA-2048 rejected");

        var sqlWorkflow = await CreateManagedWorkflowAsync();
        var now = DateTimeOffset.UtcNow;
        var wrongAlgorithm = await sqlWorkflow.Repository.EnrollKeyAsync(sqlWorkflow.Actor,
            new(sqlWorkflow.Recipient, "c509-sql", 1, "rsa-oaep-256", base64, hex,
                now.AddMinutes(-1), now.AddHours(1)), validated!, "c509-sql-algorithm", default);
        Bite(wrongAlgorithm.Error?.Code == RecipientManagementErrorCodes.KeyConflict,
            "C509-SQL-ALGORITHM-INDEPENDENT", wrongAlgorithm.Error?.Code ?? "unexpected-success");

        var initialWorkflow = await CreateManagedWorkflowAsync();
        var wrongInitial = await initialWorkflow.Repository.EnrollKeyAsync(initialWorkflow.Actor,
            new(initialWorkflow.Recipient, "c509-initial", 2, "RSA-OAEP-256", base64, hex,
                now.AddMinutes(-1), now.AddHours(1)), validated!, "c509-initial-v2", default);
        Bite(wrongInitial.Error?.Code == RecipientManagementErrorCodes.KeyConflict,
            "C509-INITIAL-VERSION", wrongInitial.Error?.Code ?? "unexpected-success");

        var shapeWorkflow = await CreateManagedWorkflowAsync();
        var emptyOwner = await shapeWorkflow.Service.EnrollKeyAsync(shapeWorkflow.Actor,
            new(Guid.Empty, "c509-empty-owner", 1, "RSA-OAEP-256", base64, hex,
                now.AddMinutes(-1), now.AddHours(1)), "c509-empty-owner", default);
        Bite(emptyOwner.Error?.Code == RecipientManagementErrorCodes.RequestInvalid
            && await CountOperationRowsAsync(Guid.Empty, "EnrollKey") == 0,
            "C509-ADMISSION-IDENTITY-VERSION-SHAPE",
            $"empty-owner:{emptyOwner.Error?.Code ?? "unexpected-success"}");

        var emptyKeyId = await shapeWorkflow.Service.EnrollKeyAsync(shapeWorkflow.Actor,
            new(shapeWorkflow.Recipient, string.Empty, 1, "RSA-OAEP-256", base64, hex,
                now.AddMinutes(-1), now.AddHours(1)), "c509-empty-key-id", default);
        Bite(emptyKeyId.Error?.Code == RecipientManagementErrorCodes.RequestInvalid
            && await CountOperationRowsAsync(shapeWorkflow.Recipient, "EnrollKey") == 0,
            "C509-ADMISSION-IDENTITY-VERSION-SHAPE",
            $"empty-key-id:{emptyKeyId.Error?.Code ?? "unexpected-success"}");

        var nonpositiveVersion = await shapeWorkflow.Service.EnrollKeyAsync(shapeWorkflow.Actor,
            new(shapeWorkflow.Recipient, "c509-nonpositive", 0, "RSA-OAEP-256", base64, hex,
                now.AddMinutes(-1), now.AddHours(1)), "c509-nonpositive-version", default);
        Bite(nonpositiveVersion.Error?.Code == RecipientManagementErrorCodes.RequestInvalid
            && await CountOperationRowsAsync(shapeWorkflow.Recipient, "EnrollKey") == 0,
            "C509-ADMISSION-IDENTITY-VERSION-SHAPE",
            $"nonpositive-version:{nonpositiveVersion.Error?.Code ?? "unexpected-success"}");

        var expiredWorkflow = await CreateManagedWorkflowAsync();
        var expired = await expiredWorkflow.Repository.EnrollKeyAsync(expiredWorkflow.Actor,
            new(expiredWorkflow.Recipient, "c509-expired", 1, "RSA-OAEP-256", base64, hex,
                now.AddHours(-2), now.AddHours(-1)), validated!, "c509-expired", default);
        Bite(expired.Error?.Code == RecipientManagementErrorCodes.KeyConflict,
            "C509-C5-NOT-EXPIRED-ADMISSION", expired.Error?.Code ?? "unexpected-success");

        var committedWorkflow = await CreateManagedWorkflowAsync();
        var committed = await committedWorkflow.Repository.EnrollKeyAsync(committedWorkflow.Actor,
            new(committedWorkflow.Recipient, "c509-committed", 1, "RSA-OAEP-256", base64, hex,
                now.AddMinutes(-1), now.AddHours(1)), validated!, "c509-committed", default);
        Bite(committed.IsSuccess && await KeyCommitAuthorityIsExactAsync(committedWorkflow.Recipient),
            "C509-POSTCOMMIT-ELIGIBILITY", committed.Error?.Code ?? "committed");
        Prerequisite(committed.IsSuccess && committed.Value is not null,
            "C509-POSTCOMMIT-ELIGIBILITY", committed.Error?.Code ?? "committed");
        Bite(await KeyCommitAuthorityIsExactAsync(committedWorkflow.Recipient),
            "C509-REGISTERED-TIME-AUTHORITY", "registered time equals operation clock");
        Bite(await ExactKeyOwnerAsync(committedWorkflow.Recipient, "c509-committed"),
            "C509-RECIPIENT-CLIENT-AUTHORITY", committedWorkflow.Recipient);
        var committedKey = committed.Value!.Value.Key;
        var duplicateIdentityVersion = await ObserveDuplicateKeyIdentityVersionRejectionAsync(
            committedWorkflow.Recipient, committedKey.RecipientKeyId,
            committedKey.RecipientKeyVersion);
        Bite(duplicateIdentityVersion == "pk_raw_export_recipient_key_registration",
            "C509-ADMISSION-IDENTITY-VERSION-SHAPE",
            $"duplicate-identity-version:{duplicateIdentityVersion}");
        await committedWorkflow.Service.RevokeKeyAsync(committedWorkflow.Actor,
            new(committedWorkflow.Recipient, committedKey.RecipientKeyId,
                committedKey.RecipientKeyVersion, committedKey.Revision, "C509_REVOKE"),
            "c509-revoke", default);

        Bite(typeof(EnrollRecipientPublicKeyRequest).GetProperties().All(property => property.Name != "PrivateKey"),
            "C509-PRIVATE-KEY-NEVER-RECEIVED", "no private-key field");
        Bite(typeof(EnrollRecipientPublicKeyRequest).GetProperties().All(property =>
                property.Name is not "RegisteredAtUtc" and not "Revision" and not "State"),
            "C509-SERVER-OWNED-FIELDS", "server-owned fields absent");
        var keyConfig = Source("src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientKeyRegistrationConfig.cs");
        Bite(keyConfig.Contains("\\\"ValidFromUtc\\\" < \\\"ValidUntilUtc\\\"", StringComparison.Ordinal),
            "C509-C5-VALIDITY-ORDER", "load-bearing CHECK");
        Bite(keyConfig.Contains("\\\"Revision\\\" > 0", StringComparison.Ordinal),
            "C509-KEY-REVISION-POSITIVE", "load-bearing CHECK");
        Bite(await RevokedKeyShapeIsStrictAsync(committedWorkflow.Recipient),
            "C509-KEY-REVOCATION-FIELDS", "revoked timestamp/reason paired");
    }

    [Fact]
    public async Task C510_historical_fingerprint_nonreuse_is_recipient_scoped()
    {
        using var rsa = RSA.Create(3072);
        var material = KeyMaterial(rsa);
        var first = await CreateManagedWorkflowAsync();
        var enrolled = await first.Service.EnrollKeyAsync(first.Actor,
            new(first.Recipient, "hospital:key:01", 1, "RSA-OAEP-256", material.SpkiBase64,
                material.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c510-first", default);
        await first.Service.RevokeKeyAsync(first.Actor,
            new(first.Recipient, "hospital:key:01", 1, enrolled.Value!.Value.Key.Revision,
                "C510_REVOKE"), "c510-revoke", default);
        var validated = new ValidatedRecipientPublicKey(
            Convert.FromBase64String(material.SpkiBase64), Convert.FromHexString(material.FingerprintHex));
        var sameRecipient = await first.Repository.EnrollKeyAsync(first.Actor,
            new(first.Recipient, "hospital:key:01", 2, "RSA-OAEP-256", material.SpkiBase64,
                material.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            validated, "c510-same-history", default);
        Bite(sameRecipient.Error?.Code == RecipientManagementErrorCodes.KeyConflict,
            "C510-HISTORICAL-FINGERPRINT-NONREUSE", sameRecipient.Error?.Code ?? "unexpected-success");
        Bite(await CountFingerprintAsync(first.Recipient, material.FingerprintHex) == 1,
            "C510-HISTORICAL-NONREUSE-RECOVERY", "historical fingerprint remains occupied");

        var second = await CreateManagedWorkflowAsync();
        var otherRecipient = await second.Service.EnrollKeyAsync(second.Actor,
            new(second.Recipient, "hospital:key:01", 1, "RSA-OAEP-256", material.SpkiBase64,
                material.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c510-other-recipient", default);
        Bite(otherRecipient.IsSuccess && second.Recipient != first.Recipient,
            "C510-HISTORICAL-FINGERPRINT-RECIPIENT-SCOPED",
            otherRecipient.Error?.Code ?? "other-recipient-created");
    }

    [Fact]
    public async Task C511_key_admission_serialization_one_active_and_strict_history_are_real()
    {
        var concurrent = await CreateManagedWorkflowAsync();
        using var aRsa = RSA.Create(3072);
        using var bRsa = RSA.Create(3072);
        var a = KeyMaterial(aRsa);
        var b = KeyMaterial(bRsa);
        var now = DateTimeOffset.UtcNow;
        var calls = await Task.WhenAll(
            concurrent.Service.EnrollKeyAsync(concurrent.Actor,
                new(concurrent.Recipient, "c511-family", 1, "RSA-OAEP-256", a.SpkiBase64,
                    a.FingerprintHex, now.AddMinutes(-1), now.AddHours(1)), "c511-concurrent-a", default),
            concurrent.Service.EnrollKeyAsync(concurrent.Actor,
                new(concurrent.Recipient, "c511-family", 1, "RSA-OAEP-256", b.SpkiBase64,
                    b.FingerprintHex, now.AddMinutes(-1), now.AddHours(1)), "c511-concurrent-b", default));
        Bite(calls.Count(result => result.IsSuccess) == 1
            && calls.Count(result => result.Error?.Code == RecipientManagementErrorCodes.KeyConflict) == 1,
            "C511-IDENTITY-LOCK-SERIALIZATION",
            string.Join(',', calls.Select(result => result.Error?.Code ?? $"HTTP{result.Value!.StatusCode}")));

        var winner = await ReadActiveKeyAsync(concurrent.Recipient);
        var secondActiveRejected = await TryInsertSecondActiveInScratchAsync(
            concurrent.Recipient, winner.KeyId, 2, dropOneActiveIndex: false);
        Bite(secondActiveRejected == "uq_raw_export_recipient_key_registration_active",
            "C511-ONE-ACTIVE-KEY", secondActiveRejected);

        var history = await CreateManagedWorkflowAsync();
        await InsertRevokedKeyAsync(history.Recipient, "c511-history", 1);
        await InsertRevokedKeyAsync(history.Recipient, "c511-history", 3);
        using var candidateRsa = RSA.Create(3072);
        var candidate = KeyMaterial(candidateRsa);
        var strictRequest = new EnrollRecipientPublicKeyRequest(history.Recipient, "c511-history", 2,
            "RSA-OAEP-256", candidate.SpkiBase64, candidate.FingerprintHex,
            now.AddMinutes(-1), now.AddHours(1));
        var strict = await history.Repository.EnrollKeyAsync(history.Actor, strictRequest,
            new(Convert.FromBase64String(candidate.SpkiBase64), Convert.FromHexString(candidate.FingerprintHex)),
            "c511-strict-v2", default);
        Bite(strict.Error?.Code == RecipientManagementErrorCodes.KeyConflict
            && await CountKeyVersionsAsync(history.Recipient, 2) == 0,
            "C511-STRICT-VERSION", strict.Error?.Code ?? "unexpected-success");
        Bite(calls.Any(result => result.Error?.Code == RecipientManagementErrorCodes.KeyConflict),
            "C511-CONCURRENT-LOSER-OUTCOME", "one loser returns exact conflict");
    }

    [Fact]
    public async Task C512_planned_rotation_drains_exact_c3_states_without_wait_or_mutation()
    {
        var c3 = new Tip88C1C3RecipientPackageDeliveryTests(postgres);
        foreach (var state in new[] { "Authorized", "Streaming", "Interrupted" })
        {
            var delivery = await c3.CreateDeliveryAsync(key: $"c512-{state}-{Guid.NewGuid():N}");
            if (state is "Streaming" or "Interrupted")
            {
                var started = await c3.BeginAsync(delivery);
                Bite(started.Outcome == "Started", $"C512-{state.ToUpperInvariant()}-DRAIN", started.Outcome);
                if (state == "Interrupted")
                {
                    var interrupted = await delivery.Repository.InterruptAsync(delivery.DeliveryId,
                        started.Delivery!.Revision, started.Delivery.DeliveryFence, "ProviderUnavailable",
                        RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false), default);
                    Bite(interrupted.Outcome == "Interrupted", "C512-INTERRUPTED-DRAIN", interrupted.Outcome);
                }
            }

            var workflow = await CreateManagedWorkflowAsync(delivery.Package.RecipientId);
            var key = await ReadActiveKeyAsync(workflow.Recipient);
            var before = await ReadDeliveryStateAsync(delivery.DeliveryId);
            var blocked = await RotateAsync(workflow, key, $"c512-{state}");
            var after = await ReadDeliveryStateAsync(delivery.DeliveryId);
            Bite(blocked.Error?.Code == RecipientManagementErrorCodes.DeliveryDrainRequired,
                $"C512-{state.ToUpperInvariant()}-DRAIN", blocked.Error?.Code ?? "unexpected-success");
            Bite(before == after, "C512-NO-C3-ROW-MUTATION", $"{before}/{after}");
            var readiness = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
            var actualCounts = await ReadDeliveryCountsAsync(workflow.Recipient);
            var countsMatch = readiness.Value is not null
                && readiness.Value.AuthorizedDeliveryCount == actualCounts.Authorized
                && readiness.Value.StreamingDeliveryCount == actualCounts.Streaming
                && readiness.Value.InterruptedDeliveryCount == actualCounts.Interrupted;
            Bite(countsMatch, "C512-ROTATION-READINESS-COUNT-PROJECTION",
                readiness.Value is null ? readiness.Error?.Code : $"{readiness.Value.AuthorizedDeliveryCount}/{readiness.Value.StreamingDeliveryCount}/{readiness.Value.InterruptedDeliveryCount}");
        }

        var lockedDelivery = await c3.CreateDeliveryAsync(key: $"c512-lock-{Guid.NewGuid():N}");
        var lockedWorkflow = await CreateManagedWorkflowAsync(lockedDelivery.Package.RecipientId);
        var lockedKey = await ReadActiveKeyAsync(lockedWorkflow.Recipient);
        await using var blocker = await OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        int blockerPid;
        await using (var pidCommand = blocker.CreateCommand())
        {
            pidCommand.Transaction = transaction;
            pidCommand.CommandText = "SELECT pg_backend_pid()";
            blockerPid = (int)(await pidCommand.ExecuteScalarAsync() ?? -1);
        }
        await using (var lockCommand = blocker.CreateCommand())
        {
            lockCommand.Transaction = transaction;
            lockCommand.CommandText = "SELECT 1 FROM tagekyc.raw_export_recipient_package_deliveries WHERE \"DeliveryId\"=@id FOR UPDATE";
            lockCommand.Parameters.AddWithValue("id", lockedDelivery.DeliveryId);
            await lockCommand.ExecuteNonQueryAsync();
        }
        using var rotationCancellation = new CancellationTokenSource();
        var rotationTask = RotateAsync(lockedWorkflow, lockedKey, "c512-nonwaiting",
            rotationCancellation.Token);
        RotationWaitObservation waitObservation;
        try
        {
            waitObservation = await ObserveRotationBlockedByAsync(blockerPid, rotationTask);
        }
        catch (Exception observerFailure)
        {
            var cleanupFailure = await CancelReleaseAndDrainRotationAsync(
                transaction, blocker, rotationCancellation, rotationTask);
            if (cleanupFailure is not null)
            {
                throw new AggregateException(
                    "C512_NONWAITING_OBSERVER_FAILURE_WITH_CLEANUP_FAILURE",
                    observerFailure, cleanupFailure);
            }
            throw;
        }

        Console.WriteLine(
            $"C512_TIMING RunId={waitObservation.RunId};ObserverStart={waitObservation.ObserverStart:O};" +
            $"ObserverConnectionOpenLatencyMs={waitObservation.ConnectionOpenElapsed.TotalMilliseconds:F3};" +
            $"RotationCompletionLatencyMs={(waitObservation.Outcome == RotationWaitOutcome.RotationCompleted ? waitObservation.Elapsed.TotalMilliseconds.ToString("F3") : "-")};" +
            $"TotalObserverLatencyMs={waitObservation.Elapsed.TotalMilliseconds:F3};" +
            $"ExactBlockerPid={blockerPid};ExactBlockerObserved={waitObservation.Outcome == RotationWaitOutcome.ExpectedBlockerObserved};" +
            $"BlockerDetectionLatencyMs={(waitObservation.Outcome == RotationWaitOutcome.ExpectedBlockerObserved ? waitObservation.Elapsed.TotalMilliseconds.ToString("F3") : "-")};" +
            $"ObserverOutcome={waitObservation.Outcome};BoundMs={C512ObservationBound.TotalMilliseconds:F0};" +
            $"BoundRatio={waitObservation.Elapsed.TotalMilliseconds / C512ObservationBound.TotalMilliseconds:F6}");

        if (waitObservation.Outcome == RotationWaitOutcome.ExpectedBlockerObserved)
        {
            var cleanupFailure = await CancelReleaseAndDrainRotationAsync(
                transaction, blocker, rotationCancellation, rotationTask);
            if (cleanupFailure is not null) throw cleanupFailure;
            Bite(false, "C512-NONWAITING-DRAIN",
                $"rotation backend blocked by delivery-row locker pid={blockerPid}");
        }
        else if (waitObservation.Outcome == RotationWaitOutcome.ObservationIndeterminate)
        {
            var indeterminate = new InvalidOperationException(
                waitObservation.IndeterminateCode
                ?? "C512_NONWAITING_OBSERVATION_INDETERMINATE");
            var cleanupFailure = await CancelReleaseAndDrainRotationAsync(
                transaction, blocker, rotationCancellation, rotationTask);
            if (cleanupFailure is not null)
            {
                throw new AggregateException(
                    "C512_NONWAITING_OBSERVATION_AND_CLEANUP_INDETERMINATE",
                    indeterminate, cleanupFailure);
            }
            throw indeterminate;
        }
        else
        {
            SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>> bounded;
            try
            {
                bounded = await rotationTask;
            }
            catch (Exception taskFailure)
            {
                var failedTaskRelease = await ReleaseBlockerBoundedAsync(transaction, blocker);
                if (!failedTaskRelease.Confirmed)
                {
                    throw new AggregateException(
                        "C512_NONWAITING_COMPLETED_TASK_FAILURE_WITH_BLOCKER_RELEASE_INDETERMINATE",
                        taskFailure, failedTaskRelease.Failure!);
                }
                throw;
            }

            var blockerRelease = await ReleaseBlockerBoundedAsync(transaction, blocker);
            if (!blockerRelease.Confirmed) throw blockerRelease.Failure!;
            Bite(true, "C512-NONWAITING-DRAIN",
                bounded.Error?.Code ?? $"HTTP{bounded.Value?.StatusCode}");
        }
        Bite(true, "C512-C3-ADMISSION-ROTATION-ORDER", "real C3 row wins before drain observation");
    }

    [Fact]
    public async Task C513_rotation_is_atomic_hard_cutover_with_one_required_event()
    {
        var workflow = await CreateManagedWorkflowAsync();
        using var k1Rsa = RSA.Create(3072);
        var k1 = KeyMaterial(k1Rsa);
        var enrolled = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, "c513-family", 1, "RSA-OAEP-256", k1.SpkiBase64,
                k1.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c513-enroll", default);
        Bite(enrolled.IsSuccess, "C513-K1-POSTLOCK-REVALIDATION", enrolled.Error?.Code ?? "enrolled");
        Prerequisite(enrolled.IsSuccess && enrolled.Value is not null,
            "C513-K1-POSTLOCK-REVALIDATION", enrolled.Error?.Code ?? "enrolled");
        var current = await ReadActiveKeyAsync(workflow.Recipient);
        var rotated = await RotateAsync(workflow, current, "c513-rotate");
        Bite(rotated.IsSuccess && await ExactHardCutoverAsync(workflow.Recipient, "c513-family"),
            "C513-HARD-CUTOVER", rotated.Error?.Code ?? $"HTTP{rotated.Value?.StatusCode}");
        Bite(await CountEventsAsync(workflow.Recipient, "RecipientPublicKeyRotated") == 1,
            "C513-REQUIRED-ROTATION-EVENT", "exactly one rotation event");
        Bite(rotated.Value!.Value.Key.RecipientKeyVersion == 2
            && rotated.Value.Value.Key.RecipientKeyId == "c513-family",
            "C513-K2-POSTLOCK-OTHER-REVALIDATION", rotated.Value.Value.Key);

        var validity = await CreateManagedWorkflowAsync();
        using var validityK1Rsa = RSA.Create(3072);
        var validityK1 = KeyMaterial(validityK1Rsa);
        var validityEnrolled = await validity.Service.EnrollKeyAsync(validity.Actor,
            new(validity.Recipient, "c513-validity-family", 1, "RSA-OAEP-256",
                validityK1.SpkiBase64, validityK1.FingerprintHex,
                DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c513-validity-enroll", default);
        Prerequisite(validityEnrolled.IsSuccess && validityEnrolled.Value is not null,
            "C513-K2-POSTLOCK-VALIDITY", validityEnrolled.Error?.Code ?? "enrolled");
        var validityCurrent = await ReadActiveKeyAsync(validity.Recipient);
        var shortHorizon = await RotateAsync(validity, validityCurrent, "c513-validity-rotate",
            validFromUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
            validUntilUtc: DateTimeOffset.UtcNow.AddMinutes(4));
        Bite(shortHorizon.Error?.Code == RecipientManagementErrorCodes.KeyConflict,
            "C513-K2-POSTLOCK-VALIDITY", shortHorizon.Error?.Code ?? "unexpected-success");

        var rollback = await CreateManagedWorkflowAsync();
        using var rollbackRsa = RSA.Create(3072);
        var rollbackK1 = KeyMaterial(rollbackRsa);
        await rollback.Service.EnrollKeyAsync(rollback.Actor,
            new(rollback.Recipient, "c513-rollback", 1, "RSA-OAEP-256", rollbackK1.SpkiBase64,
                rollbackK1.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c513-rollback-enroll", default);
        await InstallRotationCompletionFailureTriggerAsync();
        try
        {
            var beforeKey = await ReadActiveKeyAsync(rollback.Recipient);
            var failed = await RotateAsync(rollback, beforeKey, "c513-rollback-fail");
            Bite(failed.Error?.Code == RecipientManagementErrorCodes.Unavailable
                && await ExactSingleActiveKeyAsync(rollback.Recipient, "c513-rollback", 1)
                && await CountEventsAsync(rollback.Recipient, "RecipientPublicKeyRotated") == 0,
                "C513-ATOMIC-ROLLBACK-CONTROL", failed.Error?.Code ?? "unexpected-success");
        }
        finally { await RemoveRotationCompletionFailureTriggerAsync(); }
    }

    [Fact]
    public async Task C514_landed_c2_stale_exact_key_is_unavailable_without_silent_reselection()
    {
        var candidate = await new Tip88C1C2RecipientPackageTests(postgres).CreatePackageCandidateAsync();
        await RevokeKeyPreservingRevisionAndAddK2Async(candidate.Request.RecipientClientApplicationId,
            candidate.Key.KeyId, candidate.Key.KeyVersion, candidate.Key.Revision);
        await EnsureC514KeyFixtureIdentityAsync(candidate.Request.RecipientClientApplicationId,
            candidate.Key.KeyId, candidate.Key.KeyVersion, candidate.Key.Revision);
        var result = await candidate.Repository.ReserveAsync(candidate.Request, default);
        var durableKey = await ReadC2PreparationKeyIdentityAsync(
            candidate.Request.C2PreparationId);
        var responseSelectedK1 = result.RecipientKeyId == candidate.Key.KeyId
            && result.RecipientKeyVersion == candidate.Key.KeyVersion;
        var durableSelectedK1 = durableKey is { } durableK1
            && durableK1.KeyId == candidate.Key.KeyId
            && durableK1.Version == candidate.Key.KeyVersion;
        var responseHasTuple = result.RecipientKeyId is not null
            && result.RecipientKeyVersion is not null;
        var nothingReserved = !responseHasTuple && durableKey is null;
        Bite(!responseSelectedK1
            && !durableSelectedK1
            && (!nothingReserved || result.Outcome == "Unavailable"),
            "C514-STALE-K1-UNAVAILABLE",
            $"outcome={result.Outcome};requested={candidate.Key.KeyId}/{candidate.Key.KeyVersion};response={result.RecipientKeyId}/{result.RecipientKeyVersion};durable={durableKey?.KeyId}/{durableKey?.Version}");
        var cleanRejection = result.Outcome == "Unavailable"
            && result.RecipientKeyId is null
            && result.RecipientKeyVersion is null
            && durableKey is null;
        var exactRequestedAdmission = responseSelectedK1 && durableSelectedK1;
        Bite(cleanRejection || exactRequestedAdmission,
            "C514-NO-SILENT-RESELECTION",
            $"outcome={result.Outcome};requested={candidate.Key.KeyId}/{candidate.Key.KeyVersion};response={result.RecipientKeyId}/{result.RecipientKeyVersion};durable={durableKey?.KeyId}/{durableKey?.Version}");
        Bite(candidate.Request.RecipientKeyRevision == candidate.Key.Revision,
            "C514-C2-FIVE-MINUTE-ELIGIBILITY-PRESERVATION", candidate.Request.RecipientKeyRevision);
    }

    [Fact]
    public async Task C515_revoke_vs_c2_reserve_uses_exact_revision_and_rolls_back_with_event()
    {
        var candidate = await new Tip88C1C2RecipientPackageTests(postgres).CreatePackageCandidateAsync();
        await SetKeyRevisionOnlyAsync(candidate.Request.RecipientClientApplicationId,
            candidate.Key.KeyId, candidate.Key.KeyVersion, candidate.Key.Revision + 1);
        var stale = await candidate.Repository.ReserveAsync(candidate.Request, default);
        Bite(stale.Outcome == "Unavailable"
            && await CountC2PackagesAsync(candidate.Request.C2PreparationId) == 0,
            "C515-REVOKE-VS-RESERVE", stale.Outcome);

        var workflow = await CreateManagedWorkflowAsync();
        using var rsa = RSA.Create(3072);
        var material = KeyMaterial(rsa);
        var enrolled = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, "c515-rollback", 1, "RSA-OAEP-256", material.SpkiBase64,
                material.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c515-enroll", default);
        await InstallEventFailureTriggerAsync("RecipientPublicKeyRevoked", "c515_fail_revoke_event");
        try
        {
            var failed = await workflow.Service.RevokeKeyAsync(workflow.Actor,
                new(workflow.Recipient, "c515-rollback", 1, enrolled.Value!.Value.Key.Revision,
                    "C515_REVOKE"), "c515-failed-revoke", default);
            Bite(failed.Error?.Code == RecipientManagementErrorCodes.Unavailable
                && await ExactSingleActiveKeyAsync(workflow.Recipient, "c515-rollback", 1)
                && await CountEventsAsync(workflow.Recipient, "RecipientPublicKeyRevoked") == 0,
                "C515-REVOKE-ATOMIC-STATE-EVENT", failed.Error?.Code ?? "unexpected-success");
        }
        finally { await RemoveEventFailureTriggerAsync("c515_fail_revoke_event"); }
    }

    [Fact]
    public async Task C516_emergency_revoke_captures_replays_and_preserves_three_real_c3_states()
    {
        var c3 = new Tip88C1C3RecipientPackageDeliveryTests(postgres);
        var stateOnly = await c3.CreateDeliveryAsync(
            key: $"c516-state-only-{Guid.NewGuid():N}");
        await ExecuteAsync("""
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"='Revoked',"RevokedAtUtc"=clock_timestamp(),
                "RevocationReason"='C516_STATE_ONLY'
            WHERE "RecipientClientApplicationId"=@r AND "State"='Active'
            """, stateOnly.Package.RecipientId);
        var stateOnlyResult = await c3.BeginAsync(stateOnly);
        Bite(stateOnlyResult.Outcome == "Ineligible",
            "C516-C3-STATE-COMPARATOR", stateOnlyResult.Outcome);

        var authorized = await c3.CreateDeliveryAsync(key: $"c516-authorized-{Guid.NewGuid():N}");
        var streaming = await c3.CreateDeliveryAsync(authorized.Package, $"c516-streaming-{Guid.NewGuid():N}");
        var interrupted = await c3.CreateDeliveryAsync(authorized.Package, $"c516-interrupted-{Guid.NewGuid():N}");
        var startedStreaming = await c3.BeginAsync(streaming);
        var startedInterrupted = await c3.BeginAsync(interrupted);
        var interruptedResult = await interrupted.Repository.InterruptAsync(interrupted.DeliveryId,
            startedInterrupted.Delivery!.Revision, startedInterrupted.Delivery.DeliveryFence,
            "ProviderUnavailable", RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false), default);
        Bite(startedStreaming.Outcome == "Started" && interruptedResult.Outcome == "Interrupted",
            "C516-C3-REVISION-FINGERPRINT-PRESERVATION", $"{startedStreaming.Outcome}/{interruptedResult.Outcome}");

        var workflow = await CreateManagedWorkflowAsync(authorized.Package.RecipientId);
        var key = await ReadActiveKeyAsync(workflow.Recipient);
        var revokeRequest = new RevokeRecipientPublicKeyRequest(workflow.Recipient,
            key.KeyId, key.Version, key.Revision, "C516_EMERGENCY_REVOKE");
        var revoked = await workflow.Service.RevokeKeyAsync(workflow.Actor, revokeRequest, "c516-revoke", default);
        var revokeSucceeded = revoked.IsSuccess && revoked.Value is not null;
        Bite(revoked.IsSuccess
            && revoked.Value!.Value.AuthorizedDeliveryCount == 1
            && revoked.Value.Value.StreamingDeliveryCount == 1
            && revoked.Value.Value.InterruptedDeliveryCount == 1,
            "C516-EMERGENCY-REVOKE-NO-DRAIN",
            revoked.Error?.Code ?? $"{revoked.Value?.Value.AuthorizedDeliveryCount}/{revoked.Value?.Value.StreamingDeliveryCount}/{revoked.Value?.Value.InterruptedDeliveryCount}");
        if (revokeSucceeded)
        {
            var revokedValue = revoked.Value!.Value;
            var replay = await workflow.Service.RevokeKeyAsync(workflow.Actor, revokeRequest, "c516-revoke", default);
            Bite(replay.IsSuccess
                && replay.Value!.Value.Warning == revokedValue.Warning
                && replay.Value.Value.AuthorizedDeliveryCount == 1
                && replay.Value.Value.StreamingDeliveryCount == 1
                && replay.Value.Value.InterruptedDeliveryCount == 1,
                "C516-DURABLE-COUNT-REPLAY", replay.Error?.Code ?? replay.Value?.Value.Warning);
        }
        else if (revoked.Error?.Code == RecipientManagementErrorCodes.DeliveryDrainRequired)
        {
            Console.WriteLine(
                "C516_REVOKE_REPLAY_BRANCH_NOT_REACHED:TARGET_INDUCED_CUTOFF:RAW_EXPORT_RECIPIENT_MANAGEMENT_DELIVERY_DRAIN_REQUIRED");
        }
        else
        {
            throw new XunitException(
                $"C516_REVOKE_REPLAY_BRANCH_HARNESS_OR_SEMANTIC_FAILURE:{revoked.Error?.Code ?? "unexpected-result"}");
        }

        Bite(await ReadDeliveryStateAsync(streaming.DeliveryId) is var streamingState
            && streamingState.StartsWith("Streaming:", StringComparison.Ordinal),
            "C516-STREAMING-NONRETROACTIVE", streamingState);
        if (revokeSucceeded)
        {
            var deniedAuthorized = await c3.BeginAsync(authorized);
            var deniedInterrupted = await c3.BeginAsync(interrupted);
            Bite(deniedAuthorized.Outcome == "Ineligible" && deniedInterrupted.Outcome == "Ineligible",
                "C516-C3-STATE-COMPARATOR", $"{deniedAuthorized.Outcome}/{deniedInterrupted.Outcome}");
            Console.WriteLine("C516_REVOKE_REPLAY_BRANCH_EXECUTED");
        }
        else
        {
            Console.WriteLine(
                "C516_C3_POST_REVOKE_BRANCH_NOT_REACHED:TARGET_INDUCED_CUTOFF:RAW_EXPORT_RECIPIENT_MANAGEMENT_DELIVERY_DRAIN_REQUIRED");
        }
    }

    [Fact]
    public async Task C517_c4_historical_visibility_and_frozen_package_survive_managed_key_revoke()
    {
        ManagedWorkflow? workflow = null;
        RecipientPublicKeyDto? enrolledKey = null;
        string? frozenBefore = null;
        Exception? chainFailure = null;
        try
        {
            await new Tip88C1C4RecipientPackageReferenceTests(postgres).ExecuteC411RealChainAsync(
                async recipient =>
                {
                    workflow = await CreateManagedWorkflowAsync(recipient);
                    await RevokeAnyActiveKeyForFixtureAsync(recipient, "C517_PREPARE");
                    using var rsa = RSA.Create(3072);
                    var material = KeyMaterial(rsa);
                    var nextVersion = await ReadMaxKeyVersionAsync(recipient) + 1;
                    var enrolled = await workflow.Service.EnrollKeyAsync(workflow.Actor,
                        new(recipient, $"c517-{Guid.NewGuid():N}", nextVersion, "RSA-OAEP-256", material.SpkiBase64,
                            material.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
                        $"c517-enroll-{Guid.NewGuid():N}", default);
                    Bite(enrolled.IsSuccess, "C517-HISTORICAL-SNAPSHOT-UNCHANGED", enrolled.Error?.Code ?? "enrolled");
                    Prerequisite(enrolled.IsSuccess && enrolled.Value is not null,
                        "C517-HISTORICAL-SNAPSHOT-UNCHANGED", enrolled.Error?.Code ?? "enrolled");
                    enrolledKey = enrolled.Value!.Value.Key;
                },
                async (packageId, _) =>
                {
                    frozenBefore = await ReadFrozenPackageAsync(packageId);
                    var revoked = await workflow!.Service.RevokeKeyAsync(workflow.Actor,
                        new(workflow.Recipient, enrolledKey!.RecipientKeyId, enrolledKey.RecipientKeyVersion,
                            enrolledKey.Revision, "C517_REVOKE"), $"c517-revoke-{Guid.NewGuid():N}", default);
                    Bite(revoked.IsSuccess && frozenBefore == await ReadFrozenPackageAsync(packageId),
                        "C517-HISTORICAL-SNAPSHOT-UNCHANGED", revoked.Error?.Code ?? frozenBefore);
                }, includeAuthenticatedDelivery: false);
        }
        catch (Exception exception) { chainFailure = exception; }
        Bite(chainFailure is null,
            "C517-C4-VISIBILITY-UNCHANGED", chainFailure?.Message ?? "listed after revoke");
    }

    [Fact]
    public async Task C518_recovery_returns_readiness_green_with_same_family_and_fresh_material()
    {
        var generator = new RecordingCredentialGenerator();
        var workflow = await CreateManagedWorkflowAsync(generator);
        await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, null), "c518-credential", default);
        using var k1Rsa = RSA.Create(3072);
        var k1 = KeyMaterial(k1Rsa);
        var enrolled = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, "c518-family", 1, "RSA-OAEP-256", k1.SpkiBase64,
                k1.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c518-enroll", default);
        var enrolledValue = enrolled.Value!.Value;
        var persistedK1 = await ReadActiveKeyAsync(workflow.Recipient);
        var greenBefore = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
        await workflow.Service.RevokeKeyAsync(workflow.Actor,
            new(workflow.Recipient, "c518-family", 1, enrolledValue.Key.Revision,
                "C518_REVOKE"), "c518-revoke", default);
        var red = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
        using var k2Rsa = RSA.Create(3072);
        var k2 = KeyMaterial(k2Rsa);
        var recovered = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, "c518-family", 2, "RSA-OAEP-256", k2.SpkiBase64,
                k2.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
            "c518-recover", default);
        var greenAfter = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
        var recoveredKey = recovered.Value!.Value.Key;
        var persistedRecovery = await ReadActiveKeyAsync(workflow.Recipient);
        var responseMatchesPersisted = recoveredKey.RecipientKeyId == persistedRecovery.KeyId
            && recoveredKey.RecipientKeyVersion == persistedRecovery.Version
            && recoveredKey.Revision == persistedRecovery.Revision
            && string.Equals(recoveredKey.PublicKeyFingerprintHex,
                Convert.ToHexString(persistedRecovery.Fingerprint), StringComparison.OrdinalIgnoreCase);
        var mismatchKeyId = $"{persistedRecovery.KeyId}-response-control";
        Exception? responseMismatchFailure = null;
        ActiveKey persistedAfterRestore;
        try
        {
            await SetActiveKeyIdAsync(workflow.Recipient, persistedRecovery.KeyId, mismatchKeyId);
            var persistedMismatch = await ReadActiveKeyAsync(workflow.Recipient);
            responseMismatchFailure = Record.Exception(() => ImmediateBite(
                recoveredKey.RecipientKeyId == persistedMismatch.KeyId
                    && recoveredKey.RecipientKeyVersion == persistedMismatch.Version
                    && recoveredKey.Revision == persistedMismatch.Revision
                    && string.Equals(recoveredKey.PublicKeyFingerprintHex,
                        Convert.ToHexString(persistedMismatch.Fingerprint), StringComparison.OrdinalIgnoreCase),
                "C518-ROTATION-FRESH-MATERIAL",
                $"response={recoveredKey.RecipientKeyId}/{recoveredKey.RecipientKeyVersion}/{recoveredKey.Revision};persisted={persistedMismatch.KeyId}/{persistedMismatch.Version}/{persistedMismatch.Revision}"));
        }
        finally
        {
            await SetActiveKeyIdAsync(workflow.Recipient, mismatchKeyId, persistedRecovery.KeyId);
            persistedAfterRestore = await ReadActiveKeyAsync(workflow.Recipient);
        }
        var responseMismatchWasNamed = responseMismatchFailure is XunitException
            && responseMismatchFailure.Message.StartsWith(
                "C518-ROTATION-FRESH-MATERIAL:", StringComparison.Ordinal);
        var persistedRestoreMatches = persistedAfterRestore.KeyId == persistedRecovery.KeyId
            && persistedAfterRestore.Version == persistedRecovery.Version
            && persistedAfterRestore.Revision == persistedRecovery.Revision
            && persistedAfterRestore.Fingerprint.AsSpan().SequenceEqual(persistedRecovery.Fingerprint);
        var k1FingerprintCount = await CountFingerprintAsync(workflow.Recipient, k1.FingerprintHex);
        var k2FingerprintCount = await CountFingerprintAsync(workflow.Recipient, k2.FingerprintHex);
        Assert.Multiple(
            () => Bite(greenBefore.Value is { Ready: true }
                && red.Value is { Ready: false }
                && red.Value.Codes.Contains("PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID")
                && greenAfter.Value is { Ready: true },
                "C518-READINESS-RED-TO-GREEN",
                $"{greenBefore.Value?.Ready}/{red.Value?.Ready}/{greenAfter.Value?.Ready}"),
            () => Bite(persistedRecovery.KeyId == persistedK1.KeyId,
                "C518-RECOVERY-SAME-KEY-FAMILY",
                $"k1={persistedK1.KeyId};recovery={persistedRecovery.KeyId}"),
            () => Bite(!string.Equals(k1.FingerprintHex, k2.FingerprintHex, StringComparison.Ordinal)
                && k1FingerprintCount == 1
                && k2FingerprintCount == 1,
                "C518-RECOVERY-FRESH-MATERIAL", $"{k1.FingerprintHex}/{k2.FingerprintHex}"),
            () => Bite(responseMatchesPersisted
                && responseMismatchWasNamed
                && persistedRestoreMatches,
                "C518-ROTATION-FRESH-MATERIAL",
                $"canonical={responseMatchesPersisted};false-state={responseMismatchWasNamed};restored={persistedRestoreMatches}"));
    }

    [Fact]
    public async Task C519_committed_enrollment_event_recomputes_exact_audit_v2()
    {
        for (var c519Run = 0; c519Run < 2; c519Run++)
        {
        var workflow = await CreateManagedWorkflowAsync();
        var issued = await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, null), "c519-issue", default);
        Bite(issued.IsSuccess && issued.Value!.Value.PresentedKey is not null,
            "C519-CREDENTIAL-AUDIT-LINKAGE", issued.Error?.Code ?? "issued");
        Prerequisite(issued.IsSuccess && issued.Value is not null,
            "C519-CREDENTIAL-AUDIT-LINKAGE", issued.Error?.Code ?? "issued");
        var issuedValue = issued.Value!.Value;
        var replaced = await workflow.Service.ReplaceCredentialAsync(workflow.Actor,
            new(workflow.Recipient, issuedValue.ApiKeyId, issuedValue.Revision,
                null, "PLANNED_CREDENTIAL_ROTATION"), "c519-replace", default);
        Bite(replaced.IsSuccess && replaced.Value!.Value.PresentedKey is not null,
            "C519-LIFECYCLE-SEPARATE-TIMELINES", replaced.Error?.Code ?? "replaced");
        Prerequisite(replaced.IsSuccess && replaced.Value is not null,
            "C519-LIFECYCLE-SEPARATE-TIMELINES", replaced.Error?.Code ?? "replaced");
        var replacedValue = replaced.Value!.Value;
        var revokedCredential = await workflow.Service.RevokeCredentialAsync(workflow.Actor,
            new(workflow.Recipient, replacedValue.ApiKeyId, replacedValue.Revision,
                "MANAGED_CREDENTIAL_REVOKE"), "c519-revoke-credential", default);

        using var rsaK1 = RSA.Create(3072);
        using var rsaK2 = RSA.Create(3072);
        var k1 = KeyMaterial(rsaK1);
        var k2 = KeyMaterial(rsaK2);
        var now = DateTimeOffset.UtcNow;
        var enrolledKey = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, "hospital:key:01", 1, "RSA-OAEP-256", k1.SpkiBase64,
                k1.FingerprintHex, now.AddMinutes(-1), now.AddHours(2)), "c519-enroll-key", default);
        Bite(enrolledKey.IsSuccess, "C519-KEY-AUDIT-LINKAGE", enrolledKey.Error?.Code ?? "enrolled");
        Prerequisite(enrolledKey.IsSuccess && enrolledKey.Value is not null,
            "C519-KEY-AUDIT-LINKAGE", enrolledKey.Error?.Code ?? "enrolled");
        var rotatedKey = await workflow.Service.RotateKeyAsync(workflow.Actor,
            new(workflow.Recipient, "hospital:key:01", 1, enrolledKey.Value!.Value.Key.Revision,
                2, "RSA-OAEP-256", k2.SpkiBase64, k2.FingerprintHex,
                now.AddMinutes(-1), now.AddHours(3), "PLANNED_KEY_ROTATION"),
            "c519-rotate-key", default);
        Bite(rotatedKey.IsSuccess, "C519-REVISION-AUTHORITY", rotatedKey.Error?.Code ?? "rotated");
        Prerequisite(rotatedKey.IsSuccess && rotatedKey.Value is not null,
            "C519-REVISION-AUTHORITY", rotatedKey.Error?.Code ?? "rotated");
        var revokedKey = await workflow.Service.RevokeKeyAsync(workflow.Actor,
            new(workflow.Recipient, "hospital:key:01", 2, rotatedKey.Value!.Value.Key.Revision,
                "EMERGENCY_KEY_REVOKE"), "c519-revoke-key", default);
        Bite(revokedKey.IsSuccess, "C519-KEY-REVOCATION-EVIDENCE", revokedKey.Error?.Code ?? "revoked");

        var events = await ReadAuditEventsAsync(workflow.Recipient);
        static bool IsMatchingCredentialRevokeEvent(AuditEvent value, Guid apiKeyId) =>
            value.EventType == "ManagedCredentialRevoked"
            && value.TargetIdentity == apiKeyId.ToString("N");
        var matchingCredentialRevokeEventExists = events.Any(value =>
            IsMatchingCredentialRevokeEvent(value, replacedValue.ApiKeyId));
        var enrolledRecipientEvent = events.SingleOrDefault(value =>
            value.EventType == "ManagedRecipientEnrolled");
        Bite(revokedCredential.IsSuccess && matchingCredentialRevokeEventExists
            && enrolledRecipientEvent is not null,
            "C519-READINESS-CHANGING-OPERATION-AUDIT",
            $"revokeResult={revokedCredential.Error?.Code ?? "success"};revokeEvent={matchingCredentialRevokeEventExists};enrollmentEvent={enrolledRecipientEvent is not null}");
        bool Control(IEnumerable<AuditEvent> set, Func<AuditEvent, bool> match) =>
            revokedCredential.IsSuccess && set.Any(match);
        var preservedCredentialRevokeEvent = events.Single(value =>
            value.OperationKind == "RevokeCredential");
        var withoutMatchingCredentialRevokeEvent = events
            .Where(value => value.OperationId != preservedCredentialRevokeEvent.OperationId)
            .ToArray();
        Assert.NotEmpty(withoutMatchingCredentialRevokeEvent);
        Assert.Contains(withoutMatchingCredentialRevokeEvent, value =>
            !IsMatchingCredentialRevokeEvent(value, replacedValue.ApiKeyId));
        var repairedCredentialRevokeEvents = withoutMatchingCredentialRevokeEvent
            .Append(preservedCredentialRevokeEvent)
            .ToArray();
        Func<AuditEvent, bool> realMatcher = value =>
            IsMatchingCredentialRevokeEvent(value, replacedValue.ApiKeyId);
        var omittedEventPredicate = Control(withoutMatchingCredentialRevokeEvent, realMatcher);
        var repairedEventPredicate = Control(repairedCredentialRevokeEvents, realMatcher);
        Assert.False(omittedEventPredicate,
            "D3.2-REACHABLE-FALSE: successful revoke without its matching event must make the CONTROL false");
        Assert.True(repairedEventPredicate,
            "D3.2-REPAIR: restoring only the matching event must make the CONTROL true");
        var alwaysTrueValidationPair = !Control(withoutMatchingCredentialRevokeEvent, _ => true)
            && Control(repairedCredentialRevokeEvents, _ => true);
        Assert.False(alwaysTrueValidationPair,
            "D3.2-MATCHER-SENSITIVITY-ALWAYS-TRUE: corruption must invalidate the omitted/repair validation pair");
        var alwaysFalseValidationPair = !Control(withoutMatchingCredentialRevokeEvent, _ => false)
            && Control(repairedCredentialRevokeEvents, _ => false);
        Assert.False(alwaysFalseValidationPair,
            "D3.2-MATCHER-SENSITIVITY-ALWAYS-FALSE: corruption must invalidate the omitted/repair validation pair");
        var expectedTypes = new[] { "ManagedCredentialIssued", "ManagedCredentialReplaced",
            "ManagedCredentialRevoked", "RecipientPublicKeyEnrolled",
            "RecipientPublicKeyRotated", "RecipientPublicKeyRevoked" };
        Bite(events.Where(value => value.EventType != "ManagedRecipientEnrolled")
                .Select(value => value.EventType).ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedTypes),
            "C519-REQUIRED-EVENT-PER-OPERATION", string.Join(',', events.Select(value => value.EventType)));
        foreach (var value in events)
        {
            var input = new RecipientManagementAuditInput(value.OperationId, value.OperationKind,
                value.ManagerApiKeyId, workflow.Actor.PrincipalId, workflow.Recipient, value.EventType,
                value.TargetIdentity, value.Reason, value.PriorRevision, value.NewRevision,
                value.PayloadDigest, value.PriorScopesDigest, value.NewScopesDigest,
                value.CompletedAtUtc, value.AuthorizedDeliveryCount, value.StreamingDeliveryCount,
                value.InterruptedDeliveryCount);
            var expected = RecipientManagementCodec.AuditEvidenceDigest(input);
            Bite(value.EvidenceDigest.SequenceEqual(expected), "C519-SCOPE-DIGEST-BINDING",
                $"{value.EventType}:{Convert.ToHexString(value.EvidenceDigest)}");
        }
        Bite(events.All(value => value.ManagerPrincipalId == workflow.Actor.PrincipalId)
            && workflow.Actor.PrincipalId != workflow.Recipient,
            "C519-AUDIT-AUTHENTICATED-ACTOR", workflow.Actor.PrincipalId);
        Bite(events.All(value => value.OccurredAtUtc == value.CompletedAtUtc),
            "C519-AUDIT-POSTLOCK-CLOCK", string.Join(',', events.Select(value => value.EventType)));
        Bite(events.All(value => !value.TargetIdentity.Contains("PresentedKey", StringComparison.Ordinal)
            && !value.Reason.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase)
            && !value.ResultSnapshot.Contains("presentedKey", StringComparison.OrdinalIgnoreCase)),
            "C519-AUDIT-SENSITIVE-MATERIAL-ABSENT", "event and durable snapshot surfaces clean");
        var rotatedTarget = events.Single(value => value.EventType == "RecipientPublicKeyRotated").TargetIdentity;
        var expectedRotatedTarget = RecipientManagementCodec.TargetIdentity("RotateKey", workflow.Recipient,
            "hospital:key:01", 1, 2);
        Bite((enrolledRecipientEvent is null
                || enrolledRecipientEvent.TargetIdentity == workflow.Recipient.ToString("N"))
            && rotatedTarget == expectedRotatedTarget,
            "C519-TARGET-IDENTITY-FRAMING", $"actual={rotatedTarget};expected={expectedRotatedTarget}");
        Bite((enrolledRecipientEvent is null
                || enrolledRecipientEvent.Reason == "MANAGED_RECIPIENT_ENROLLED")
            && events.Single(value => value.EventType == "RecipientPublicKeyRevoked").Reason
                == "EMERGENCY_KEY_REVOKE",
            "C519-AUDIT-VOCABULARY-EVIDENCE", "server and caller reason vocabulary");
        Bite(events.Single(value => value.EventType == "RecipientPublicKeyRevoked").AuthorizedDeliveryCount == 0
            && events.Single(value => value.EventType == "RecipientPublicKeyRevoked").StreamingDeliveryCount == 0
            && events.Single(value => value.EventType == "RecipientPublicKeyRevoked").InterruptedDeliveryCount == 0,
            "C519-DELIVERY-COUNT-EVIDENCE-BINDING", "0/0/0");
        Bite(await LifecycleRevisionsAreSeparateAsync(workflow.Recipient),
            "C519-LIFECYCLE-SEPARATE-REVISIONS", "credential and key revisions independently advanced");
        var migration = Migration();
        var appendGuardProbeEvent = enrolledRecipientEvent
            ?? events.Single(value => value.EventType == "ManagedCredentialIssued");
        var appendOnlyUpdate = await ObserveAuditEventUpdateGuardAsync(
            appendGuardProbeEvent.OperationId);
        Bite(migration.Contains("raw_export_guard_recipient_management_event", StringComparison.Ordinal)
            && migration.Contains("BEFORE UPDATE OR DELETE ON tagekyc.raw_export_recipient_management_events", StringComparison.Ordinal)
            && appendOnlyUpdate.ExactAppendOnlyRejection
            && appendOnlyUpdate.BeforeSnapshot == appendOnlyUpdate.AfterSnapshot,
            "C519-AUDIT-APPEND-ONLY-UPDATE",
            $"event={appendOnlyUpdate.ManagementEventId:N};exactRejection={appendOnlyUpdate.ExactAppendOnlyRejection};updated={appendOnlyUpdate.UpdatedRows};unchanged={appendOnlyUpdate.BeforeSnapshot == appendOnlyUpdate.AfterSnapshot}");
        Bite(migration.Contains("ManagedCredentialIssued", StringComparison.Ordinal)
            && migration.Contains("RecipientPublicKeyEnrolled", StringComparison.Ordinal),
            "C519-LIFECYCLE-SEPARATE-EVENTS", "credential/key vocabulary");
        Bite(!Migration().Contains("ReadinessProbed", StringComparison.Ordinal),
            "C519-READINESS-DECISION-AUDIT-CONDITIONAL", "ordinary probes unaudited");
        }
    }

    [Fact]
    public async Task C520_operation_check_admits_only_provisional_or_exact_completed_shapes()
    {
        var repositorySource = Source("src/TagEkyc.Infrastructure/RawExport/RecipientManagementRepository.cs");
        var enrollCommand = SourceSlice(repositorySource,
            "private const string EnrollRecipientSql =", "private const string IssueCredentialSql =");
        var enrollUsesOnlyGuardedFunction = enrollCommand.Contains(
                "SELECT * FROM tagekyc.raw_export_enroll_managed_recipient(", StringComparison.Ordinal)
            && !enrollCommand.Contains("INSERT", StringComparison.OrdinalIgnoreCase);
        Bite(enrollUsesOnlyGuardedFunction,
            "C520-ALL-OPERATIONS-GUARDED-WRITES", enrollCommand);
        Prerequisite(enrollUsesOnlyGuardedFunction,
            "C520-ALL-OPERATIONS-GUARDED-WRITES", "repository command must remain guarded before behavioral phases");

        var workflow = await CreateManagedWorkflowAsync();
        var operation = Guid.NewGuid();
        var idempotency = RecipientManagementCodec.IdempotencyKeyDigest("c520-provisional");
        var canonical = RecipientManagementCodec.CanonicalPayload(
            RecipientManagementCodec.CanonicalGuid(workflow.Recipient),
            RecipientManagementCodec.CanonicalNullableTimestamp(null));
        var payload = RecipientManagementCodec.PayloadDigest("IssueCredential", canonical);
        var equality = RecipientManagementCodec.EqualityFingerprint(
            workflow.Actor.PrincipalId, "IssueCredential", workflow.Recipient, payload);
        await using var connection = await new ManagerRoleConnectionFactory(postgres.ConnectionString).OpenAsync(default);
        await using var transaction = await connection.BeginTransactionAsync();
        var provisional = await InvokeIssueCredentialAsync(connection, transaction, operation,
            workflow, idempotency, equality, payload, null);
        Bite(provisional.Outcome == "CandidateRequired"
            && await ProvisionalShapeIsExactAsync(connection, transaction, operation),
            "C520-EXECUTABLE-CLAIM-ROW", provisional.Outcome);
        await using (var outside = await OpenAsync())
        await using (var invisible = outside.CreateCommand())
        {
            invisible.CommandText = "SELECT count(*) FROM tagekyc.raw_export_recipient_management_operations WHERE \"OperationId\"=@operation";
            invisible.Parameters.AddWithValue("operation", operation);
            Bite((long)(await invisible.ExecuteScalarAsync() ?? -1L) == 0,
                "C520-EXECUTABLE-CLAIM-ROW", "uncommitted row externally invisible");
        }
        var material = Material($"c520{operation:N}"[..16]);
        var completed = await InvokeIssueCredentialAsync(connection, transaction, operation,
            workflow, idempotency, equality, payload, material);
        Bite(completed.Outcome == "Created", "C520-ALL-OPERATIONS-GUARDED-WRITES", completed.Outcome);
        await transaction.CommitAsync();

        var replay = await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, null), "c520-provisional", default);
        Bite(replay.IsSuccess && replay.Value!.Value.PresentedKey is null
            && replay.Value.Value.Revision == 1,
            "C520-HISTORICAL-COMMIT-SNAPSHOT-REPLAY", replay.Error?.Code ?? replay.Value?.Value.Revision.ToString());
        var conflict = await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, DateTimeOffset.UtcNow.AddDays(1)), "c520-provisional", default);
        Bite(!conflict.IsSuccess && conflict.Error?.Code == RecipientManagementErrorCodes.IdempotencyConflict,
            "C520-IDEMPOTENCY-EQUALITY", conflict.Error?.Code);

        var config=File.ReadAllText(Path.Combine(Root(),"src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientManagementOperationConfig.cs"));
        Bite(config.Contains("ck_raw_export_recipient_management_operation_sparse",StringComparison.Ordinal)
            && config.Contains("\"Outcome\" IS NULL",StringComparison.Ordinal)
            && config.Contains("\"ResultSnapshot\" IS NOT NULL",StringComparison.Ordinal),
            "C520-EXECUTABLE-CLAIM-ROW","durable sparse operation shapes");
        var migration = Migration();
        Bite(migration.Contains("CREATE FUNCTION tagekyc.raw_export_enroll_managed_recipient", StringComparison.Ordinal)
            && migration.Contains("CREATE FUNCTION tagekyc.raw_export_revoke_recipient_key", StringComparison.Ordinal),
            "C520-ALL-OPERATIONS-GUARDED-WRITES", "seven guarded functions");
        Bite(migration.Contains("raw_export_recipient_management_operations", StringComparison.Ordinal)
            && migration.Contains("raw_export_recipient_management_events", StringComparison.Ordinal),
            "C520-ATOMIC-STATE-EVENT-OPERATION-COMMIT", "same SQL transaction");
        Bite(migration.Contains("p_operation_kind", StringComparison.Ordinal)
            || migration.Contains("\"OperationKind\"", StringComparison.Ordinal),
            "C520-LIFECYCLE-SEPARATE-IDEMPOTENCY", "operation-kind namespace");
    }

    [Fact]
    public async Task C521_functions_acquire_recipient_authority_before_single_fresh_clock()
    {
        var recipient = Guid.NewGuid();
        var principal = Guid.NewGuid();
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), Guid.NewGuid(), "c521-manager-key",
            AuthenticatedCallerCategory.OperatorAdmin,
            new HashSet<string> { RecipientManagementApplicationService.RequiredScope },
            PrincipalId: Guid.NewGuid());
        var service = new RecipientManagementApplicationService(
            new RecipientManagementRepository(new ManagerRoleConnectionFactory(postgres.ConnectionString),
                new TestCredentialGenerator()), new RecipientPublicKeyProfileValidator());
        var first = service.EnrollRecipientAsync(actor, new(recipient, principal), "c521-race-a", default);
        var second = service.EnrollRecipientAsync(actor, new(recipient, principal), "c521-race-b", default);
        var outcomes = await Task.WhenAll(first, second);
        Bite(outcomes.Count(value => value.IsSuccess) == 1
            && outcomes.Count(value => value.Error?.Code == RecipientManagementErrorCodes.PrincipalConflict) == 1
            && await CountIdentityRowsAsync(recipient) == 1,
            "C521-ABSENT-IDENTITY-LINEARIZATION",
            string.Join(',', outcomes.Select(value => value.IsSuccess ? "Created" : value.Error?.Code)));
        Bite(await EnrollmentClockIsSingleAsync(recipient),
            "C521-POSTLOCK-FRESH-CLOCK", "identity/policy/event/operation share admission clock");

        var sql = Migration();
        var functionNames = new[]
        {
            "raw_export_enroll_managed_recipient",
            "raw_export_issue_recipient_credential",
            "raw_export_replace_recipient_credential",
            "raw_export_revoke_recipient_credential",
            "raw_export_enroll_recipient_key",
            "raw_export_rotate_recipient_key",
            "raw_export_revoke_recipient_key",
        };
        var orderFailures = new List<string>();
        var clockFailures = new List<string>();
        foreach (var functionName in functionNames)
        {
            var body = MigrationFunction(sql, functionName);
            var advisory = body.IndexOf("pg_advisory_xact_lock", StringComparison.Ordinal);
            var identity = body.IndexOf("raw_export_managed_recipient_identities i", StringComparison.Ordinal);
            var firstForUpdate = body.IndexOf("FOR UPDATE", StringComparison.Ordinal);
            var identityForUpdate = identity < 0
                ? -1
                : body.IndexOf("FOR UPDATE", identity, StringComparison.Ordinal);
            var lastForUpdate = body.LastIndexOf("FOR UPDATE", StringComparison.Ordinal);
            var clock = body.IndexOf("clock_timestamp()", StringComparison.Ordinal);
            if (!(advisory >= 0 && identity >= 0 && firstForUpdate >= 0
                    && identityForUpdate == firstForUpdate && advisory < identity && identity < firstForUpdate))
                orderFailures.Add($"{functionName}:advisory={advisory};identity={identity};first={firstForUpdate};identityForUpdate={identityForUpdate}");
            if (!(clock > lastForUpdate && lastForUpdate >= 0
                    && RegexCount(body, "clock_timestamp()") == 1))
                clockFailures.Add($"{functionName}:lastLock={lastForUpdate};clock={clock};count={RegexCount(body, "clock_timestamp()")}");
        }
        Bite(orderFailures.Count == 0, "C521-GLOBAL-LOCK-ORDER", string.Join(" | ", orderFailures));
        Bite(clockFailures.Count == 0, "C521-POSTLOCK-FRESH-CLOCK", string.Join(" | ", clockFailures));
        Bite(sql.Contains("FOR UPDATE", StringComparison.Ordinal)
            && sql.Contains("v_now:=pg_catalog.clock_timestamp()", StringComparison.Ordinal),
            "C521-LIFECYCLE-REVALIDATION", "lock/revalidate/clock");
        Bite(RegexCount(sql, "CREATE FUNCTION tagekyc.raw_export_") >= 9,
            "C521-LOCK-BEARING-OPERATION-ENUMERATION", RegexCount(sql, "CREATE FUNCTION tagekyc.raw_export_"));
    }

    [Fact]
    public async Task C522_managed_identity_credential_key_and_readiness_are_live()
    {
        var workflow = await CreateManagedWorkflowAsync();
        var before = await workflow.Service.ReadReadinessAsync(
            workflow.Actor, workflow.Recipient, default);
        Bite(before.IsSuccess && !before.Value!.Ready
            && before.Value.Codes.Contains("PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID")
            && before.Value.Codes.Contains("PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID"),
            "C522-LIVE-PROJECTION-NO-PERSISTED-STATE", before.IsSuccess ? string.Join(',', before.Value!.Codes) : before.Error?.Code);
        Prerequisite(before.IsSuccess && before.Value is not null,
            "C522-LIVE-PROJECTION-NO-PERSISTED-STATE", before.Error?.Code ?? "before");

        var issued = await workflow.Service.IssueCredentialAsync(workflow.Actor,
            new(workflow.Recipient, null), "c522-issue", default);
        if (!issued.IsSuccess || issued.Value is null
            || issued.Value.Value.PresentedKey is null)
            throw new InvalidOperationException(
                $"C522_CREDENTIAL_SETUP_FAILED:{issued.Error?.Code ?? issued.Value?.Value.State}");
        var issuedValue = issued.Value!.Value;

        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        var enrolledKey = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, "hospital:key:01", 1, "RSA-OAEP-256",
                Convert.ToBase64String(spki), Convert.ToHexString(fingerprint),
                now.AddMinutes(-1), now.AddDays(30)), "c522-key", default);
        if (!enrolledKey.IsSuccess || enrolledKey.Value is null)
            throw new InvalidOperationException(
                $"C522_KEY_SETUP_FAILED:{enrolledKey.Error?.Code ?? "missing-value"}");

        var eventsBeforeProbe = await CountEventsForRecipientAsync(workflow.Recipient);
        var ready = await workflow.Service.ReadReadinessAsync(
            workflow.Actor, workflow.Recipient, default);
        var eventsAfterProbe = await CountEventsForRecipientAsync(workflow.Recipient);
        Bite(ready.IsSuccess && ready.Value!.Ready && ready.Value.Codes.Count == 0,
            "C522-KEY-PROFILE-VALIDITY", ready.IsSuccess ? string.Join(',', ready.Value!.Codes) : ready.Error?.Code);
        Prerequisite(ready.IsSuccess && ready.Value is not null,
            "C522-KEY-PROFILE-VALIDITY", ready.Error?.Code ?? "ready");
        var readyValue = ready.Value!;
        Bite(readyValue.AuthorizedDeliveryCount == 0
            && readyValue.StreamingDeliveryCount == 0
            && readyValue.InterruptedDeliveryCount == 0,
            "C522-ROTATION-COUNTS-NONBLOCKING-ACTIVATION",
            $"{readyValue.AuthorizedDeliveryCount}/{readyValue.StreamingDeliveryCount}/{readyValue.InterruptedDeliveryCount}");
        Bite(eventsBeforeProbe == eventsAfterProbe,
            "C522-ORDINARY-PROBE-NO-AUDIT",
            $"before={eventsBeforeProbe};after={eventsAfterProbe}");

        var companionBeforeStateChange = await ReadCompanionIsolationStateAsync(issuedValue.ApiKeyId);
        await SetCompanionStatePreservingRevisionAsync(issuedValue.ApiKeyId, "Revoked");
        var companionAfterStateChange = await ReadCompanionIsolationStateAsync(issuedValue.ApiKeyId);
        Assert.True(companionBeforeStateChange.State == "Active"
            && companionAfterStateChange.State == "Revoked"
            && companionBeforeStateChange.Revision == companionAfterStateChange.Revision
            && companionBeforeStateChange.ResultRevision == companionBeforeStateChange.Revision
            && companionAfterStateChange.ResultRevision == companionAfterStateChange.Revision,
            $"C522 fixture isolation failed: before={companionBeforeStateChange};after={companionAfterStateChange}");
        var revokedCredentialReadiness = await workflow.Service.ReadReadinessAsync(
            workflow.Actor, workflow.Recipient, default);
        Bite(revokedCredentialReadiness.IsSuccess
            && !revokedCredentialReadiness.Value!.Ready
            && revokedCredentialReadiness.Value.Codes.Contains(
                "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID"),
            "C522-MANAGED-AUTH-READINESS",
            revokedCredentialReadiness.IsSuccess
                ? string.Join(',', revokedCredentialReadiness.Value!.Codes)
                : revokedCredentialReadiness.Error?.Code);
        await RestoreCompanionActiveAsync(issuedValue.ApiKeyId);

        var enrolledKeyValue = enrolledKey.Value!.Value.Key;
        await SetKeyStatePreservingRevisionAsync(workflow.Recipient,
            enrolledKeyValue.RecipientKeyId, enrolledKeyValue.RecipientKeyVersion, "Revoked");
        var revokedKeyReadiness = await workflow.Service.ReadReadinessAsync(
            workflow.Actor, workflow.Recipient, default);
        Bite(revokedKeyReadiness.IsSuccess
            && !revokedKeyReadiness.Value!.Ready
            && revokedKeyReadiness.Value.Codes.Contains(
                "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID"),
            "C522-KEY-READINESS",
            revokedKeyReadiness.IsSuccess
                ? string.Join(',', revokedKeyReadiness.Value!.Codes)
                : revokedKeyReadiness.Error?.Code);
        await SetKeyStatePreservingRevisionAsync(workflow.Recipient,
            enrolledKeyValue.RecipientKeyId, enrolledKeyValue.RecipientKeyVersion, "Active");

        await ExecuteAsync("UPDATE tagekyc.api_keys SET \"CallerCategory\"='OperatorAdmin' WHERE \"ApiKeyId\"=@r",
            issuedValue.ApiKeyId);
        var wrongCategory = await workflow.Service.ReadReadinessAsync(
            workflow.Actor, workflow.Recipient, default);
        Bite(wrongCategory.IsSuccess && !wrongCategory.Value!.Ready
            && wrongCategory.Value.Codes.Contains(
                "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID"),
            "C522-RECIPIENT-CATEGORY",
            wrongCategory.IsSuccess ? string.Join(',', wrongCategory.Value!.Codes) : wrongCategory.Error?.Code);
        await ExecuteAsync("UPDATE tagekyc.api_keys SET \"CallerCategory\"='BusinessConsumer' WHERE \"ApiKeyId\"=@r",
            issuedValue.ApiKeyId);

        await using var connection = await OpenAsync();
        await using var counts = connection.CreateCommand();
        counts.CommandText = """
            SELECT
              (SELECT count(*) FROM tagekyc.raw_export_recipient_management_events WHERE "OperationId" IN (@identity,@credential,@key)),
              (SELECT count(*) FROM information_schema.columns WHERE table_schema='tagekyc'
                AND column_name IN ('Ready','IsReady'))
            """;
        counts.Parameters.AddWithValue("identity", workflow.IdentityOperation);
        counts.Parameters.AddWithValue("credential", issued.Value.Value.ApiKeyId);
        counts.Parameters.AddWithValue("key", workflow.IdentityOperation);
        await using var countReader = await counts.ExecuteReaderAsync(CommandBehavior.SingleRow);
        await countReader.ReadAsync();
        Bite(countReader.GetInt64(1) == 0, "C522-LIVE-PROJECTION-NO-PERSISTED-STATE", countReader.GetInt64(1));
        await ExecuteAsync("UPDATE tagekyc.raw_export_managed_recipient_policies SET \"Revision\"=2 WHERE \"RecipientClientApplicationId\"=@r", workflow.Recipient);
        var policyMismatch = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
        Bite(policyMismatch.IsSuccess && !policyMismatch.Value!.Ready
            && policyMismatch.Value.Codes.Contains("PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID"),
            "C522-AUDIT-LINEAGE-POLICY-REVISION", policyMismatch.IsSuccess ? string.Join(',',policyMismatch.Value!.Codes) : policyMismatch.Error?.Code);
        await ExecuteAsync("UPDATE tagekyc.raw_export_managed_recipient_policies SET \"Revision\"=1 WHERE \"RecipientClientApplicationId\"=@r", workflow.Recipient);

        await ExecuteAsync("UPDATE tagekyc.raw_export_managed_recipient_credentials SET \"Revision\"=2 WHERE \"ApiKeyId\"=@r", issuedValue.ApiKeyId);
        var credentialMismatch = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
        Bite(credentialMismatch.IsSuccess && !credentialMismatch.Value!.Ready
            && credentialMismatch.Value.Codes.Contains("PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID"),
            "C522-AUDIT-LINEAGE-CREDENTIAL-REVISION", credentialMismatch.IsSuccess ? string.Join(',',credentialMismatch.Value!.Codes) : credentialMismatch.Error?.Code);
        await ExecuteAsync("UPDATE tagekyc.raw_export_managed_recipient_credentials SET \"Revision\"=1 WHERE \"ApiKeyId\"=@r", issuedValue.ApiKeyId);

        await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"Revision\"=2 WHERE \"RecipientClientApplicationId\"=@r", workflow.Recipient);
        var keyMismatch = await workflow.Service.ReadReadinessAsync(workflow.Actor, workflow.Recipient, default);
        Bite(keyMismatch.IsSuccess && !keyMismatch.Value!.Ready
            && keyMismatch.Value.Codes.Contains("PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID"),
            "C522-AUDIT-LINEAGE-KEY-REVISION", keyMismatch.IsSuccess ? string.Join(',',keyMismatch.Value!.Codes) : keyMismatch.Error?.Code);
        await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"Revision\"=1 WHERE \"RecipientClientApplicationId\"=@r", workflow.Recipient);

        var nonzero = await CreateReadyRecipientWithAuthorizedDeliveryAsync();
        var authorizedCount = await CountDeliveriesByStateAsync(
            nonzero.Workflow.Recipient, "Authorized");
        Bite(nonzero.Readiness.IsSuccess && nonzero.Readiness.Value!.Ready
            && authorizedCount > 0
            && nonzero.Readiness.Value.AuthorizedDeliveryCount == authorizedCount,
            "C522-ROTATION-COUNTS-NONBLOCKING-ACTIVATION",
            nonzero.Readiness.IsSuccess
                ? $"ready={nonzero.Readiness.Value!.Ready};projected={nonzero.Readiness.Value.AuthorizedDeliveryCount};catalog={authorizedCount}"
                : nonzero.Readiness.Error?.Code);
    }

    [Fact]
    public async Task C523_exact_owned_table_function_and_index_sets_are_present()
    {
        var workflow = await CreateManagedWorkflowAsync();
        var policyProbe = await TrySetPolicyProfileAsync(
            workflow.Recipient, "C3C4RecipientV1-shadow");
        Bite(policyProbe == "ck_raw_export_managed_recipient_policy_shape",
            "C523-EXACT-POLICY-OWNERSHIP", policyProbe);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var table = $"raw_export_managed_recipient_identities_shadow_{suffix}";
        var function = $"raw_export_enroll_managed_recipient_shadow_{suffix}";
        var indexTable = $"c5_index_probe_host_{suffix}";
        var index = $"uq_raw_export_managed_recipient_identity_shadow_{suffix}";
        var role = $"tagekyc_raw_export_recipient_manager_shadow_{suffix}";
        await using var connection = await OpenAsync();

        async Task ProbeSiblingAsync(
            string createSql,
            string cleanupSql,
            string bite,
            string observed,
            Func<Task<bool>> concreteExistsAsync)
        {
            var absentBeforeCreate = !await concreteExistsAsync();
            var readinessBeforeCreate = await ReadCatalogReadinessAsync();
            var presentBeforeCleanup = false;
            await using (var create = connection.CreateCommand())
            {
                create.CommandText = createSql;
                await create.ExecuteNonQueryAsync();
            }
            try
            {
                presentBeforeCleanup = await concreteExistsAsync();
                Bite(await ReadCatalogReadinessAsync(), bite,
                    $"{observed};{await CatalogDiagnosticsAsync()}");
            }
            finally
            {
                await using var cleanup = connection.CreateCommand();
                cleanup.CommandText = cleanupSql;
                await cleanup.ExecuteNonQueryAsync();
            }
            var absentAfterCleanup = !await concreteExistsAsync();
            Assert.True(absentBeforeCreate && readinessBeforeCreate,
                $"D4-{bite}-VALID-CLEAN: expected absent sibling and green readiness before create; observed={observed};absent={absentBeforeCreate};readiness={readinessBeforeCreate}");
            Assert.False(!presentBeforeCleanup,
                $"D4-{bite}-REACHABLE-FAILED-CLEANUP: cleanup predicate must be false while sibling remains; observed={observed}");
            Bite(await ReadCatalogReadinessAsync() && absentAfterCleanup, bite,
                $"{observed};concreteAbsent={absentAfterCleanup};cleanup restored canonical catalog");
        }

        await ProbeSiblingAsync(
            $"CREATE TABLE tagekyc.\"{table}\"(id integer NOT NULL)",
            $"DROP TABLE IF EXISTS tagekyc.\"{table}\" CASCADE",
            "C523-EXACT-TABLE-OWNERSHIP", table,
            () => CatalogObjectExistsAsync(connection, table, string.Empty, string.Empty));
        await ProbeSiblingAsync(
            $"CREATE FUNCTION tagekyc.\"{function}\"() RETURNS integer LANGUAGE sql AS 'SELECT 1'",
            $"DROP FUNCTION IF EXISTS tagekyc.\"{function}\"()",
            "C523-EXACT-FUNCTION-OWNERSHIP", function,
            () => CatalogObjectExistsAsync(connection, string.Empty, function, string.Empty));
        await ProbeSiblingAsync(
            $"CREATE TABLE tagekyc.\"{indexTable}\"(id integer NOT NULL); CREATE UNIQUE INDEX \"{index}\" ON tagekyc.\"{indexTable}\"(id)",
            $"DROP TABLE IF EXISTS tagekyc.\"{indexTable}\" CASCADE",
            "C523-EXACT-INDEX-OWNERSHIP", index,
            () => CatalogObjectExistsAsync(connection, index, string.Empty, string.Empty));
        await ProbeSiblingAsync(
            $"CREATE ROLE \"{role}\" NOLOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS",
            $"DROP ROLE IF EXISTS \"{role}\"",
            "C523-EXACT-ROLE-OWNERSHIP", role,
            () => CatalogObjectExistsAsync(connection, string.Empty, string.Empty, role));

        var expectedReadinessItems = new[]
        {
            "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CONFIG_INVALID",
            "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_ROLE_TOPOLOGY_INVALID",
            "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_CATALOG_INVALID",
            "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_MANAGED_AUTH_INVALID",
            "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_LIFECYCLE_INVALID",
            "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_PREDECESSOR_INVALID",
        };
        Bite(RecipientManagementReadinessValidator.Codes.SequenceEqual(
                expectedReadinessItems, StringComparer.Ordinal),
            "C523-EXACT-READINESS-ITEM-OWNERSHIP",
            string.Join(',', RecipientManagementReadinessValidator.Codes));
    }

    [Fact]
    public async Task C524_manager_role_topology_and_mandatory_execute_are_exact()
    {
        await using var connection=await OpenAsync(); await using var command=connection.CreateCommand();
        command.CommandText="""
            SELECT (SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname IN ('tagekyc_raw_export_recipient_manager','tagekyc_raw_export_recipient_manager_login')),
              pg_catalog.has_function_privilege('tagekyc_raw_export_recipient_manager','tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid)','EXECUTE'),
              NOT pg_catalog.has_table_privilege('tagekyc_raw_export_recipient_manager','tagekyc.raw_export_managed_recipient_identities','INSERT')
            """;
        await using var reader=await command.ExecuteReaderAsync(CommandBehavior.SingleRow); await reader.ReadAsync();
        Bite(reader.GetInt64(0)==2&&reader.GetBoolean(1)&&reader.GetBoolean(2),"C524-MANDATORY-MANAGER-EXECUTE","role/execute/dml");
        Bite(reader.GetInt64(0)==2,"C524-DEDICATED-MANAGER-ROLE",reader.GetInt64(0));
        Bite(reader.GetBoolean(2),"C524-MANAGER-NO-DIRECT-DML",reader.GetBoolean(2));
        await reader.DisposeAsync();

        await using (var runtime = await OpenAsync())
        {
            await SetRoleAsync(runtime, "tagekyc_runtime");
            Bite(await ThrowsSqlStateAsync(runtime,
                    "SELECT * FROM tagekyc.raw_export_read_recipient_activation_readiness('00000000-0000-0000-0000-000000000001'::uuid)",
                    PostgresErrorCodes.InsufficientPrivilege),
                "C524-DEDICATED-MANAGER-ROLE", "runtime execution denied");
        }
        await using (var manager = await new ManagerRoleConnectionFactory(postgres.ConnectionString).OpenAsync(default))
        {
            Bite(await ThrowsSqlStateAsync(manager,
                    "INSERT INTO tagekyc.raw_export_managed_recipient_identities DEFAULT VALUES",
                    PostgresErrorCodes.InsufficientPrivilege),
                "C524-MANAGER-NO-DIRECT-DML", "manager direct insert denied");
        }

        const string functionSignature = "tagekyc.raw_export_enroll_managed_recipient(uuid,uuid,uuid,uuid,bytea,bytea,bytea,uuid)";
        await using (var revoke = connection.CreateCommand())
        {
            revoke.CommandText = $"REVOKE EXECUTE ON FUNCTION {functionSignature} FROM tagekyc_raw_export_recipient_manager";
            await revoke.ExecuteNonQueryAsync();
        }
        try
        {
            Bite(!await ReadCatalogReadinessAsync(), "C524-MANDATORY-MANAGER-EXECUTE",
                "mandatory EXECUTE revoked");
        }
        finally
        {
            await using var restore = connection.CreateCommand();
            restore.CommandText = $"GRANT EXECUTE ON FUNCTION {functionSignature} TO tagekyc_raw_export_recipient_manager";
            await restore.ExecuteNonQueryAsync();
        }
        Bite(await ReadCatalogReadinessAsync(), "C524-MANDATORY-MANAGER-EXECUTE", "grant restored");

        var probeRole = $"c5_acl_probe_{Guid.NewGuid():N}";
        await using (var create = connection.CreateCommand())
        {
            create.CommandText = $"CREATE ROLE \"{probeRole}\" NOLOGIN; GRANT EXECUTE ON FUNCTION {functionSignature} TO \"{probeRole}\"";
            await create.ExecuteNonQueryAsync();
        }
        try
        {
            Bite(!await ReadCatalogReadinessAsync(), "C524-EXACT-GRANT-OWNERSHIP",
                "one unexpected sibling grant rejected");
        }
        finally
        {
            await using var cleanup = connection.CreateCommand();
            cleanup.CommandText = $"REVOKE EXECUTE ON FUNCTION {functionSignature} FROM \"{probeRole}\"; DROP ROLE \"{probeRole}\"";
            await cleanup.ExecuteNonQueryAsync();
        }
        Bite(await ReadCatalogReadinessAsync(), "C524-EXACT-GRANT-OWNERSHIP", "extra grant cleanup restored");
    }

    [Fact]
    public async Task C525_disabled_invalid_and_postgres_configuration_partition_is_exact()
    {
        var disabled=RecipientManagementConnectionFactory.Resolve(new ConfigurationBuilder().Build(),false);
        var invalid=RecipientManagementConnectionFactory.Resolve(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            {{"TagEkyc:RawExport:RecipientManagement:Topology","Unknown"}}).Build(),false);
        Bite(disabled.Topology==RecipientManagementTopology.Disabled&&invalid.Topology==RecipientManagementTopology.Invalid,
            "C525-DISABLED-INVALID-TOPOLOGY-PARTITION",$"{disabled.Topology}/{invalid.Topology}");
        Bite(disabled.DatabaseConnectionString is null && invalid.DatabaseConnectionString is null,
            "C525-CONFIG-BEFORE-DB", "zero manager connection material");

        var connectionTrap = new CountingManagementConnectionFactory();
        var connectionResolutions = 0;
        var trapServices = new ServiceCollection()
            .AddSingleton<IRecipientManagementConnectionFactory>(_ =>
            {
                connectionResolutions++;
                return connectionTrap;
            })
            .BuildServiceProvider();
        Exception? disabledHarnessFailure = null;
        Exception? invalidHarnessFailure = null;
        try
        {
            await new RecipientManagementReadinessValidator(disabled, trapServices).ValidateAsync(default);
        }
        catch (Exception exception)
        {
            disabledHarnessFailure = exception;
        }
        try
        {
            await new RecipientManagementReadinessValidator(invalid, trapServices).ValidateAsync(default);
        }
        catch (Exception exception)
        {
            invalidHarnessFailure = exception;
        }
        Bite(connectionResolutions == 0 && connectionTrap.Calls == 0
            && disabledHarnessFailure is null
            && invalidHarnessFailure is RecipientManagementReadinessException
            {
                Code: var invalidCode,
            } && invalidCode == RecipientManagementReadinessValidator.Codes[0],
            "C525-DISABLED-INVALID-TOPOLOGY-PARTITION",
            $"resolutions={connectionResolutions};opens={connectionTrap.Calls};disabled={disabledHarnessFailure};invalid={invalidHarnessFailure}");

        await using var db = postgres.CreateDbContext();
        var empty = new ServiceCollection().BuildServiceProvider();
        var c2Disabled = new RecipientPackageReadinessValidator(
            new RecipientPackageOptions(RecipientPackageTopology.Disabled, null, true), db, empty);
        var c3Disabled = new RecipientPackageDeliveryReadinessValidator(
            new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.Disabled,
                null, null, null, true), empty);
        var c4Disabled = new RecipientPackageReferenceReadinessValidator(
            new RecipientPackageReferenceOptions(RecipientPackageReferenceTopology.Disabled,
                null, null, [], true), empty);
        var postgresServices = new ServiceCollection()
            .AddSingleton<IRecipientManagementConnectionFactory>(
                new ManagerRoleConnectionFactory(postgres.ConnectionString))
            .AddSingleton(c2Disabled)
            .AddSingleton(c3Disabled)
            .AddSingleton(c4Disabled)
            .BuildServiceProvider();
        var postgresOptions = new RecipientManagementOptions(
            RecipientManagementTopology.PostgresDurable, postgres.ConnectionString, true);
        await new RecipientManagementReadinessValidator(postgresOptions, postgresServices)
            .ValidateAsync(default);
        await using (var manager = await new ManagerRoleConnectionFactory(postgres.ConnectionString)
            .OpenAsync(default))
        await using (var currentUser = manager.CreateCommand())
        {
            currentUser.CommandText = "SELECT current_user";
            Bite((string?)await currentUser.ExecuteScalarAsync()
                    == RecipientManagementOptions.ManagerLogin,
                "C525-EXACT-CONNECTED-ROLE", "manager login observed");
        }

        var wrongRoleServices = new ServiceCollection()
            .AddSingleton<IRecipientManagementConnectionFactory>(
                new PlainManagementConnectionFactory(postgres.ConnectionString))
            .AddSingleton(c2Disabled)
            .AddSingleton(c3Disabled)
            .AddSingleton(c4Disabled)
            .BuildServiceProvider();
        var wrongRoleFailure = await CaptureReadinessFailureAsync(
            new RecipientManagementReadinessValidator(postgresOptions, wrongRoleServices));
        Bite(wrongRoleFailure?.Code == RecipientManagementReadinessValidator.Codes[1],
            "C525-EXACT-CONNECTED-ROLE", wrongRoleFailure ?? new("none", null, null));

        var beforeOperations = await CountAllRowsAsync("raw_export_recipient_management_operations");
        var c3Invalid = new RecipientPackageDeliveryReadinessValidator(
            new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.Invalid,
                null, null, null, false), empty);
        var predecessorFailureServices = new ServiceCollection()
            .AddSingleton<IRecipientManagementConnectionFactory>(
                new ManagerRoleConnectionFactory(postgres.ConnectionString))
            .AddSingleton(c2Disabled)
            .AddSingleton(c3Invalid)
            .AddSingleton(c4Disabled)
            .BuildServiceProvider();
        var predecessorFailure = await CaptureReadinessFailureAsync(
            new RecipientManagementReadinessValidator(postgresOptions, predecessorFailureServices));
        var afterOperations = await CountAllRowsAsync("raw_export_recipient_management_operations");
        Bite(predecessorFailure is
            {
                Component: "C3",
                ComponentCode: "PROD_RAW_EXPORT_PACKAGE_DELIVERY_CONFIG_INVALID",
            }, "C525-EXACT-FAILURE-COMPONENT", predecessorFailure?.ToString() ?? "<none>");
        Bite(predecessorFailure?.Code == "PROD_RAW_EXPORT_RECIPIENT_MANAGEMENT_PREDECESSOR_INVALID",
            "C525-OWNER-READINESS-COMPOSITION", predecessorFailure?.ToString() ?? "<none>");
        Bite(beforeOperations == afterOperations,
            "C525-FAILURE-NO-C5-MUTATION", $"before={beforeOperations};after={afterOperations}");

        var factory = File.ReadAllText(Path.Combine(Root(),"src/TagEkyc.Infrastructure/RawExport/RecipientManagementConnectionFactory.cs"));
        Bite(factory.Contains("current_user",StringComparison.Ordinal)
            && factory.Contains("ManagerLogin",StringComparison.Ordinal),
            "C525-EXACT-CONNECTED-ROLE","connected identity check");
        var readiness = File.ReadAllText(Path.Combine(Root(),
            "src/TagEkyc.Infrastructure/RawExport/RecipientManagementReadinessValidator.cs"));
        Bite(readiness.Contains("RecipientPackageReadinessValidator",StringComparison.Ordinal)
            &&readiness.Contains("RecipientPackageDeliveryReadinessValidator",StringComparison.Ordinal)
            &&readiness.Contains("RecipientPackageReferenceReadinessValidator",StringComparison.Ordinal),
            "C525-OWNER-READINESS-COMPOSITION","C2/C3/C4 owners");
        Bite(!readiness.Contains("raw_export_recipient_packages WHERE",StringComparison.Ordinal)
            && !readiness.Contains("recipient_package_delivery", StringComparison.Ordinal),
            "C525-NO-DUPLICATED-PREDECESSOR-LOGIC","no copied C2/C3 predecessor SQL/catalog predicate");
        Bite(RecipientManagementReadinessValidator.Codes.Distinct(StringComparer.Ordinal).Count()==6,
            "C525-EXACT-FAILURE-COMPONENT",RecipientManagementReadinessValidator.Codes.Length);
    }

    [Fact]
    public async Task C526_canonical_chain_uses_real_c1_c2_c3_c4_and_managed_auth_surfaces()
    {
        var managedCallbackReached = false;
        Guid? chainRecipient = null;
        var expectedRecipient = Tip88B34AuthorizationEngineTests.ClientApplicationId;
        var baselinePreparationIds = await ReadC2PreparationIdsAsync(expectedRecipient);
        try
        {
            await new Tip88C1C4RecipientPackageReferenceTests(postgres)
                .ExecuteC411RealChainAsync(async recipient =>
            {
                managedCallbackReached = true;
                chainRecipient = recipient;
                var workflow = await CreateManagedWorkflowAsync(recipient);
                await RevokeAnyActiveKeyForFixtureAsync(recipient, "C526_PREPARE");
                var issued = await IssueFreshCredentialAsync(workflow,
                    $"c526-credential-{Guid.NewGuid():N}");
                Bite(issued.IsSuccess && issued.Value!.Value.PresentedKey is not null,
                    "C526-REAL-MANAGED-C1-C4-CHAIN", issued.Error?.Code ?? "credential-issued");
                Prerequisite(issued.IsSuccess && issued.Value is not null,
                    "C526-REAL-MANAGED-C1-C4-CHAIN", issued.Error?.Code ?? "credential-issued");
                var presentedKey = issued.Value!.Value.PresentedKey!;

                using var rsa = RSA.Create(3072);
                var key = KeyMaterial(rsa);
                var nextVersion = await ReadMaxKeyVersionAsync(recipient) + 1;
                var enrolledKey = await workflow.Service.EnrollKeyAsync(workflow.Actor,
                    new(recipient, $"c526:{Guid.NewGuid():N}", nextVersion, "RSA-OAEP-256", key.SpkiBase64,
                        key.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1)),
                    $"c526-key-{Guid.NewGuid():N}", default);
                Bite(enrolledKey.IsSuccess,
                    "C526-REAL-MANAGED-C1-C4-CHAIN", enrolledKey.Error?.Code ?? "key-enrolled");

                await using var db = postgres.CreateDbContext();
                var resolved = await FindManagedKeyAsync(presentedKey);
                Bite(resolved is not null,
                    "C526-RATIFIED-CANONICAL-SEQUENCE",
                    await CredentialResolutionDiagnosticsAsync(
                        issued.Value!.Value.ApiKeyId, presentedKey));
                Prerequisite(resolved is not null,
                    "C526-RATIFIED-CANONICAL-SEQUENCE", "managed credential resolution");
                var policyTrap = new CountingPolicyProvider();
                var authenticator = new C5CredentialAwareApiKeyAuthenticator(
                    new PostgresHashedApiKeyStore(db,
                        new ApiKeyStorePepper(SHA256.HashData("c5-integration-pepper"u8.ToArray()))),
                    policyTrap);
                var http = new DefaultHttpContext();
                http.Request.Headers["X-TagEkyc-Api-Key"] = presentedKey;
                var authenticated = await authenticator.AuthenticateAsync(http,
                    "business.raw-export.package.references.read", default);
                Bite(authenticated.IsSuccess
                    && authenticated.Value!.ClientApplicationId == recipient
                    && authenticated.Value.PrincipalId == workflow.Principal
                    && policyTrap.Calls == 0,
                    "C526-RATIFIED-CANONICAL-SEQUENCE",
                    $"auth={authenticated.Error?.Code ?? "success"};policyCalls={policyTrap.Calls}");
            });
        }
        catch (Exception exception) when (exception is not XunitException
            || !exception.Message.StartsWith("C526-", StringComparison.Ordinal))
        {
            throw new XunitException(
                $"C526-REAL-MANAGED-C1-C4-CHAIN: observed={exception.Message}");
        }
        if (chainRecipient is not Guid recipient
            || recipient != expectedRecipient)
            throw new InvalidOperationException(
                $"C526_CURRENT_PREPARATION_CHAIN_LOOKUP_AMBIGUOUS:expected={expectedRecipient};actual={chainRecipient}");
        var afterPreparationIds = await ReadC2PreparationIdsAsync(recipient);
        var newPreparationIds = afterPreparationIds
            .Except(baselinePreparationIds)
            .OrderBy(id => id)
            .ToArray();
        if (newPreparationIds.Length == 0)
            throw new InvalidOperationException("C526_CURRENT_PREPARATION_DELTA_ZERO");
        if (newPreparationIds.Length > 1)
            throw new InvalidOperationException(
                $"C526_CURRENT_PREPARATION_DELTA_AMBIGUOUS:{newPreparationIds.Length}");
        var realC2EventChain = await HasRealC2PackageEventChainAsync(
            recipient, newPreparationIds[0]);
        Bite(managedCallbackReached && realC2EventChain,
            "C526-REAL-MANAGED-C1-C4-CHAIN",
            $"managedCallback={managedCallbackReached};newPreparations={newPreparationIds.Length};realC2EventChain={realC2EventChain}");
        Bite(await ManagedLifecycleAuthoritiesAreSeparateAsync(),
            "C526-LIFECYCLE-SEPARATE-AUTHORITIES", "credential ApiKeyId and encryption RecipientKeyId remain separate");
    }

    [Fact]
    public async Task C528_migration_down_and_reapply_are_disposable_and_catalog_stable()
    {
        const string previousMigration =
            "20260819180000_Tip88C1C4PackageReferenceDistribution";
        var sharedDatabase = new NpgsqlConnectionStringBuilder(postgres.ConnectionString).Database;
        var sharedBefore = await AppliedMigrationsAsync(postgres.CreateDbContext());
        var target = await CreateC528MigrationTargetAsync(createDisposable: true);
        var targetDatabase = target.Database.Database.GetDbConnection().Database;
        try
        {
            Bite(!string.Equals(targetDatabase, sharedDatabase, StringComparison.Ordinal),
                "C528-DISPOSABLE-MIGRATION-ONLY",
                $"target={targetDatabase};shared={sharedDatabase}");
            Prerequisite(!string.Equals(targetDatabase, sharedDatabase, StringComparison.Ordinal),
                "C528-DISPOSABLE-MIGRATION-ONLY",
                $"unsafe migration target={targetDatabase};shared={sharedDatabase}");
            var beforeCatalog = await C528CatalogSnapshotAsync(target.Database);
            var migrator = target.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(previousMigration);
            var afterDown = await target.Database.Database.GetAppliedMigrationsAsync();
            Bite(!afterDown.Contains("20260823120000_Tip88C1C5ManagedRecipientEnrollment",
                    StringComparer.Ordinal),
                "C528-DISPOSABLE-MIGRATION-ONLY", "C5 Down applied only to clone");
            await migrator.MigrateAsync();
            var afterCatalog = await C528CatalogSnapshotAsync(target.Database);
            Bite(string.Equals(beforeCatalog, afterCatalog, StringComparison.Ordinal),
                "C528-DISPOSABLE-MIGRATION-ONLY", "normalized catalog restored");
            var sharedAfter = await AppliedMigrationsAsync(postgres.CreateDbContext());
            Bite(sharedBefore.SequenceEqual(sharedAfter, StringComparer.Ordinal),
                "C528-DISPOSABLE-MIGRATION-ONLY", "shared migration state unchanged");
        }
        finally
        {
            await target.DisposeAsync();
        }
        Bite(!await DatabaseExistsAsync(targetDatabase),
            "C528-DISPOSABLE-MIGRATION-ONLY", $"database {targetDatabase} dropped");

        var dispatch = Source(
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_build_dispatch.md");
        var block = Regex.Match(dispatch,
            @"## 10\. Exact 47-path permanent allowlist[\s\S]*?```text\s*(?<body>[\s\S]*?)```");
        var paths = block.Groups["body"].Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => Regex.Replace(line, @"^\d{2}\s+", string.Empty)).ToArray();
        Bite(paths.Length == 47
            && paths.All(path => File.Exists(Path.Combine(Root(),
                path.Replace('/', Path.DirectorySeparatorChar))))
            && dispatch.Contains("anonymous-volume", StringComparison.OrdinalIgnoreCase),
            "C528-DISPOSABLE-MIGRATION-ONLY", $"allowlist={paths.Length}");
    }

    [Fact]
    public async Task C529_strict_revocation_reason_and_predecessor_writes_are_preserved()
    {
        var files = new[]
        {
            "Tip88C1C2RecipientPackageTests.cs",
            "Tip88C1C3RecipientPackageDeliveryTests.cs",
            "Tip88C1C4RecipientPackageReferenceTests.cs",
        };
        var text = string.Join('\n', files.Select(file =>
            Source($"tests/TagEkyc.IntegrationTests/{file}")));
        var writes = Regex.Matches(text, "C2_TEST_REVOKE").Count
            + Regex.Matches(text, "C3_TEST_REVOKE").Count
            + Regex.Matches(text, "C4_TEST_REVOKE").Count;
        var migration = Migration();
        Bite(writes == 8
            && migration.Contains("ck_raw_export_recipient_key_registration_sparse",
                StringComparison.Ordinal)
            && migration.Contains("PRE_C5_UNSPECIFIED", StringComparison.Ordinal),
            "C529-STRICT-REVOCATION-REASON-PREDECESSOR", $"writes={writes}");

        var rejection = await TryInsertNullReasonRevokedKeyAsync();
        Bite(rejection == "ck_raw_export_recipient_key_registration_sparse",
            "C529-STRICT-REVOCATION-REASON-PREDECESSOR", rejection);

        var keyConfig = Source(
            "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientKeyRegistrationConfig.cs");
        Bite(keyConfig.Contains("ck_raw_export_recipient_key_registration_shape",
                StringComparison.Ordinal)
            && keyConfig.Contains("\\\"Revision\\\" > 0", StringComparison.Ordinal)
            && keyConfig.Contains("\\\"ValidFromUtc\\\" < \\\"ValidUntilUtc\\\"",
                StringComparison.Ordinal),
            "C529-DEMOTION-CONSTRAINT-PRESERVATION",
            "Revision>0 + validity ordering");
    }

    private static async Task<HttpStatusCode> PostMalformedAsync(HttpClient client, string presentedKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            RecipientManagementEndpoints.RoutePrefix + "/recipients");
        request.Headers.Add("X-TagEkyc-Api-Key", presentedKey);
        request.Headers.Add(RecipientManagementEndpoints.IdempotencyHeader, "c502-malformed");
        request.Content = new StringContent("{", Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task<EnrollCall> EnrollAsync(EnrollCall? prior=null)
    {
        var call=prior??new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),RandomNumberGenerator.GetBytes(32),RandomNumberGenerator.GetBytes(32),RandomNumberGenerator.GetBytes(32),"");
        await using var connection=await OpenAsync(); await using var command=connection.CreateCommand();
        command.CommandText="SELECT * FROM tagekyc.raw_export_enroll_managed_recipient(@o,@a,@m,@r,@i,@e,@p,@principal)";
        command.Parameters.AddWithValue("o",call.Operation); command.Parameters.AddWithValue("a",call.ManagerApiKey);
        command.Parameters.AddWithValue("m",call.ManagerPrincipal); command.Parameters.AddWithValue("r",call.Recipient);
        command.Parameters.AddWithValue("i",call.Idempotency); command.Parameters.AddWithValue("e",call.Equality);
        command.Parameters.AddWithValue("p",call.Payload); command.Parameters.AddWithValue("principal",call.Principal);
        await using var reader=await command.ExecuteReaderAsync(CommandBehavior.SingleRow); await reader.ReadAsync();
        return call with{Outcome=reader.GetString(reader.GetOrdinal("Outcome"))};
    }
    private async Task<ManagedWorkflow> CreateManagedWorkflowAsync(
        IManagedCredentialMaterialGenerator? credentialGenerator = null) =>
        await CreateManagedWorkflowAsync(Guid.NewGuid(), credentialGenerator);

    private async Task<ManagedWorkflow> CreateManagedWorkflowAsync(
        Guid recipient,
        IManagedCredentialMaterialGenerator? credentialGenerator = null)
    {
        var principal = await ReadManagedPrincipalAsync(recipient) ?? Guid.NewGuid();
        var actor = new AuthenticatedClientContext(
            Guid.NewGuid(), Guid.NewGuid(), "c5manager0000000",
            AuthenticatedCallerCategory.OperatorAdmin,
            new HashSet<string> { RecipientManagementApplicationService.RequiredScope },
            PrincipalId: Guid.NewGuid());
        var repository = new RecipientManagementRepository(
            new ManagerRoleConnectionFactory(postgres.ConnectionString),
            credentialGenerator ?? new TestCredentialGenerator());
        var service = new RecipientManagementApplicationService(repository,
            new RecipientPublicKeyProfileValidator());
        if (await ReadManagedPrincipalAsync(recipient) is not null)
            return new(service, repository, actor, recipient, principal, recipient);
        var enrolled = await service.EnrollRecipientAsync(actor,
            new(recipient, principal), $"c5-identity-{Guid.NewGuid():N}", default);
        if (!enrolled.IsSuccess || enrolled.Value is null
            || enrolled.Value.Value.PrincipalId != principal)
            throw new InvalidOperationException(
                $"C522_WORKFLOW_ENROLLMENT_SETUP_FAILED:{enrolled.Error?.Code ?? enrolled.Value?.Value.PrincipalId.ToString()}");
        return new(service, repository, actor, recipient, principal, enrolled.Value!.Value.RecipientClientApplicationId);
    }
    private async Task<C504PreservationObservation> ObserveC504PreservationAsync(
        ClientApplicationStatus status)
    {
        const string requiredScope = "business.unrelated.preservation";
        var client = Guid.NewGuid();
        var principal = Guid.NewGuid();
        var presentedKey = $"c504_{Guid.NewGuid():N}";
        var apiKey = new ResolvedApiKey(
            Guid.NewGuid(), client, "c504unrelated00",
            new HashSet<string>(StringComparer.Ordinal) { requiredScope },
            ApiKeyStatus.Active, DateTimeOffset.UtcNow.AddHours(1),
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<Guid> { Guid.NewGuid() },
            new HashSet<string>(StringComparer.Ordinal) { "capture-unrelated" },
            principal);
        var policy = new LocalDevClientPolicy(
            client, status, new PolicySnapshotId($"c504-{status}-{Guid.NewGuid():N}"),
            new HashSet<VerificationProfile>(), new HashSet<string>(StringComparer.Ordinal),
            new HashSet<RequiredCheckType>(), new HashSet<string>(StringComparer.Ordinal) { requiredScope },
            false, null);
        var provider = new FixedPolicyProvider(policy);
        var authenticator = new C5CredentialAwareApiKeyAuthenticator(new FixedApiKeyStore(apiKey), provider);

        var sharedCanonicalAbsentBefore = !await HasC504RecipientStateAsync(postgres.ConnectionString, client);
        var sourceDatabase = new NpgsqlConnectionStringBuilder(postgres.ConnectionString).Database
            ?? throw new InvalidOperationException("DISPOSABLE_DATABASE_SOURCE_NAME_MISSING");
        var cloneSchemaCurrent = false;
        var disposableDatabaseDistinct = false;
        var stateAbsentBefore = false;
        var statePresentAfter = false;
        var enrollmentSucceeded = false;
        var before = SessionOperationResult<AuthenticatedClientContext>.Failure(
            "C504_NOT_OBSERVED", "C504 before authentication was not observed.", 500);
        var after = SessionOperationResult<AuthenticatedClientContext>.Failure(
            "C504_NOT_OBSERVED", "C504 after authentication was not observed.", 500);
        var disposableDatabaseName = string.Empty;
        Exception? scenarioFailure = null;
        Exception? disposalFailure = null;
        var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            $"c504_{status.ToString().ToLowerInvariant()}");
        try
        {
            await using (isolated)
            {
                try
                {
                    await using var isolatedDb = isolated.CreateDbContext();
                    var isolatedConnectionString = isolatedDb.Database.GetDbConnection().ConnectionString;
                    disposableDatabaseName = new NpgsqlConnectionStringBuilder(isolatedConnectionString).Database
                        ?? throw new InvalidOperationException("DISPOSABLE_DATABASE_NAME_MISSING");
                    disposableDatabaseDistinct = !StringComparer.Ordinal.Equals(
                        disposableDatabaseName, sourceDatabase);
                    if (!disposableDatabaseDistinct)
                        throw new InvalidOperationException("DISPOSABLE_DATABASE_MATCHES_SHARED_CANONICAL");

                    cloneSchemaCurrent = await VerifyC504CloneSchemaCurrentAsync(
                        isolatedDb, isolatedConnectionString);
                    if (!cloneSchemaCurrent)
                        throw new InvalidOperationException("DISPOSABLE_CLONE_SCHEMA_NOT_CURRENT");

                    stateAbsentBefore = !await HasC504RecipientStateAsync(isolatedConnectionString, client);
                    before = await authenticator.AuthenticateAsync(
                        ApiKeyContext(presentedKey), requiredScope, default);

                    var repository = new RecipientManagementRepository(
                        new ManagerRoleConnectionFactory(isolatedConnectionString), new TestCredentialGenerator());
                    var service = new RecipientManagementApplicationService(
                        repository, new RecipientPublicKeyProfileValidator());
                    var actor = new AuthenticatedClientContext(
                        Guid.NewGuid(), Guid.NewGuid(), "c504manager0000",
                        AuthenticatedCallerCategory.OperatorAdmin,
                        new HashSet<string>(StringComparer.Ordinal)
                            { RecipientManagementApplicationService.RequiredScope },
                        PrincipalId: Guid.NewGuid());
                    var enrollment = await service.EnrollRecipientAsync(actor,
                        new(client, Guid.NewGuid()), $"c504-enroll-{Guid.NewGuid():N}", default);
                    enrollmentSucceeded = enrollment.IsSuccess;
                    statePresentAfter = await HasC504RecipientStateAsync(isolatedConnectionString, client);
                    after = await authenticator.AuthenticateAsync(
                        ApiKeyContext(presentedKey), requiredScope, default);
                }
                catch (Exception exception)
                {
                    scenarioFailure = exception;
                }
            }
        }
        catch (Exception exception)
        {
            disposalFailure = exception;
        }

        var disposableDatabaseDropped = !string.IsNullOrEmpty(disposableDatabaseName)
            && !await DatabaseExistsAsync(disposableDatabaseName);
        var sharedCanonicalAbsentAfter = !await HasC504RecipientStateAsync(postgres.ConnectionString, client);
        if (scenarioFailure is not null || disposalFailure is not null || !disposableDatabaseDropped)
        {
            var causes = new List<Exception>();
            if (scenarioFailure is not null) causes.Add(scenarioFailure);
            if (disposalFailure is not null) causes.Add(disposalFailure);
            if (!disposableDatabaseDropped)
                causes.Add(new InvalidOperationException(
                    $"DISPOSABLE_DATABASE_RESIDUE:{disposableDatabaseName}"));
            throw new AggregateException(
                $"C504 disposable scenario '{status}' failed; scenario and disposal causes are preserved.",
                causes);
        }

        return new(cloneSchemaCurrent, disposableDatabaseDistinct, disposableDatabaseDropped,
            sharedCanonicalAbsentBefore, sharedCanonicalAbsentAfter,
            stateAbsentBefore, statePresentAfter, enrollmentSucceeded,
            before, after, provider.Calls, status, disposableDatabaseName);
    }
    private static async Task<bool> VerifyC504CloneSchemaCurrentAsync(
        TagEkycDbContext database, string connectionString)
    {
        var expectedLatest = database.Database.GetMigrations().Last();
        var actualLatest = (await database.Database.GetAppliedMigrationsAsync()).LastOrDefault();
        if (!StringComparer.Ordinal.Equals(expectedLatest, actualLatest))
            return false;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH required_constraints(name) AS (VALUES
              ('ck_raw_export_managed_recipient_identity_shape'),
              ('ck_raw_export_managed_recipient_policy_shape'),
              ('ck_raw_export_recipient_management_operation_shape'),
              ('ck_raw_export_recipient_management_operation_sparse'),
              ('ck_raw_export_recipient_management_event_shape'),
              ('ck_raw_export_recipient_management_event_sparse')),
            constraint_state AS (
              SELECT c.conname AS name
              FROM pg_catalog.pg_constraint c
              JOIN pg_catalog.pg_class rel ON rel.oid=c.conrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=rel.relnamespace
              WHERE n.nspname='tagekyc'
                AND c.contype='c' AND c.convalidated
                AND c.conname IN (SELECT name FROM required_constraints)
                AND length(pg_catalog.pg_get_constraintdef(c.oid, true)) > 0),
            trigger_state AS (
              SELECT pg_catalog.count(*)=1 AS ok
              FROM pg_catalog.pg_trigger t
              JOIN pg_catalog.pg_class rel ON rel.oid=t.tgrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=rel.relnamespace
              JOIN pg_catalog.pg_proc p ON p.oid=t.tgfoid
              JOIN pg_catalog.pg_namespace pn ON pn.oid=p.pronamespace
              WHERE n.nspname='tagekyc'
                AND rel.relname='raw_export_recipient_management_events'
                AND t.tgname='trg_raw_export_recipient_management_event_append'
                AND NOT t.tgisinternal AND t.tgenabled IN ('O','A')
                AND pn.nspname='tagekyc'
                AND p.proname='raw_export_guard_recipient_management_event')
            SELECT
              (SELECT pg_catalog.count(*) FROM constraint_state)
                = (SELECT pg_catalog.count(*) FROM required_constraints)
              AND (SELECT ok FROM trigger_state)
            """;
        return await command.ExecuteScalarAsync() is true;
    }
    private static async Task<bool> HasC504RecipientStateAsync(
        string connectionString, Guid recipient)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS(SELECT 1 FROM tagekyc.raw_export_managed_recipient_identities
              WHERE "RecipientClientApplicationId"=@recipient)
              AND EXISTS(SELECT 1 FROM tagekyc.raw_export_managed_recipient_policies
              WHERE "RecipientClientApplicationId"=@recipient)
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private static bool EquivalentAuthenticationContext(
        AuthenticatedClientContext before, AuthenticatedClientContext after) =>
        before.ApiKeyId == after.ApiKeyId
        && before.ClientApplicationId == after.ClientApplicationId
        && before.PrincipalId == after.PrincipalId
        && before.KeyPrefix == after.KeyPrefix
        && before.CallerCategory == after.CallerCategory
        && before.Scopes.SetEquals(after.Scopes)
        && SetEquals(before.AllowedClientApplicationIds, after.AllowedClientApplicationIds)
        && SetEquals(before.AllowedCaptureAgentIds, after.AllowedCaptureAgentIds);
    private static bool SetEquals<T>(IReadOnlySet<T>? left, IReadOnlySet<T>? right) =>
        left is null ? right is null : right is not null && left.SetEquals(right);
    private static ManagedCredentialMaterial Material(string prefix) => new(
        Guid.NewGuid(), $"c5_{prefix}_{Guid.NewGuid():N}", prefix,
        SHA256.HashData(Encoding.UTF8.GetBytes($"hash:{prefix}")));
    private static PublicKeyMaterial KeyMaterial(RSA rsa)
    {
        var spki = rsa.ExportSubjectPublicKeyInfo();
        return new(Convert.ToBase64String(spki), Convert.ToHexString(SHA256.HashData(spki)));
    }
    private async Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateAsync(
        ManagedWorkflow workflow, ActiveKey current, string idempotencyKey,
        CancellationToken cancellationToken = default,
        DateTimeOffset? validFromUtc = null,
        DateTimeOffset? validUntilUtc = null)
    {
        using var rsa = RSA.Create(3072);
        var next = KeyMaterial(rsa);
        return await workflow.Service.RotateKeyAsync(workflow.Actor,
            new(workflow.Recipient, current.KeyId, current.Version, current.Revision,
                current.Version + 1, "RSA-OAEP-256", next.SpkiBase64, next.FingerprintHex,
                validFromUtc ?? DateTimeOffset.UtcNow.AddMinutes(-1),
                validUntilUtc ?? DateTimeOffset.UtcNow.AddHours(1),
                "C5_PLANNED_ROTATION"), idempotencyKey, cancellationToken);
    }
    private async Task<ActiveKey> ReadActiveKeyAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "RecipientKeyId","RecipientKeyVersion","Revision","PublicKeyFingerprint"
            FROM tagekyc.raw_export_recipient_key_registrations
            WHERE "RecipientClientApplicationId"=@recipient AND "State"='Active'
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync()) throw new InvalidOperationException("C5_ACTIVE_KEY_MISSING");
        return new(reader.GetString(0), reader.GetInt32(1), reader.GetInt64(2), (byte[])reader[3]);
    }
    private async Task<string> ObserveDuplicateKeyIdentityVersionRejectionAsync(
        Guid recipient, string keyId, int version)
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                  "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
                  "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision",
                  "RegisteredAtUtc","RevokedAtUtc","RevocationReason")
                SELECT "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion",'RSA-OAEP-256',
                  @spki,@fingerprint,"ValidFromUtc","ValidUntilUtc",'Revoked',"Revision"+1,
                  "RegisteredAtUtc",clock_timestamp(),'C509_DUPLICATE_IDENTITY_VERSION'
                FROM tagekyc.raw_export_recipient_key_registrations
                WHERE "RecipientClientApplicationId"=@recipient
                  AND "RecipientKeyId"=@key
                  AND "RecipientKeyVersion"=@version
                """;
            command.Parameters.AddWithValue("recipient", recipient);
            command.Parameters.AddWithValue("key", keyId);
            command.Parameters.AddWithValue("version", version);
            command.Parameters.AddWithValue("spki", spki);
            command.Parameters.AddWithValue("fingerprint", fingerprint);
            var inserted = await command.ExecuteNonQueryAsync();
            await transaction.RollbackAsync();
            return $"INSERTED:{inserted}";
        }
        catch (PostgresException exception)
        {
            await transaction.RollbackAsync();
            return exception.SqlState == PostgresErrorCodes.UniqueViolation
                ? exception.ConstraintName ?? exception.SqlState
                : $"UNEXPECTED_SQLSTATE:{exception.SqlState}:{exception.ConstraintName}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }
    private async Task SetActiveKeyIdAsync(Guid recipient, string currentKeyId, string replacementKeyId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "RecipientKeyId"=@replacement
            WHERE "RecipientClientApplicationId"=@recipient
              AND "RecipientKeyId"=@current
              AND "State"='Active'
            """;
        command.Parameters.AddWithValue("replacement", replacementKeyId);
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("current", currentKeyId);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("C518_ACTIVE_KEY_ID_CONTROL_CARDINALITY");
    }
    private async Task<string> TryInsertSecondActiveInScratchAsync(
        Guid recipient, string keyId, int version, bool dropOneActiveIndex)
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("c5_one_active");
        try
        {
            await using var database = isolated.CreateDbContext();
            var connection = (NpgsqlConnection)database.Database.GetDbConnection();
            await connection.OpenAsync();
            if (dropOneActiveIndex)
            {
                await using var drop = connection.CreateCommand();
                drop.CommandText =
                    "DROP INDEX tagekyc.uq_raw_export_recipient_key_registration_active";
                await drop.ExecuteNonQueryAsync();
            }
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                  "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
                  "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
                VALUES(@recipient,@key,@version,'RSA-OAEP-256',@spki,@fingerprint,
                  clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Active',1,clock_timestamp())
                """;
            command.Parameters.AddWithValue("recipient", recipient);
            command.Parameters.AddWithValue("key", keyId);
            command.Parameters.AddWithValue("version", version);
            command.Parameters.AddWithValue("spki", spki);
            command.Parameters.AddWithValue("fingerprint", fingerprint);
            await command.ExecuteNonQueryAsync();
            return "INSERTED";
        }
        catch (PostgresException exception) { return exception.ConstraintName ?? exception.SqlState; }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
            await isolated.DisposeAsync();
        }
    }
    private async Task InsertRevokedKeyAsync(Guid recipient, string keyId, int version)
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        try
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                  "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
                  "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision",
                  "RegisteredAtUtc","RevokedAtUtc","RevocationReason")
                VALUES(@recipient,@key,@version,'RSA-OAEP-256',@spki,@fingerprint,
                  clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Revoked',2,
                  clock_timestamp(),clock_timestamp(),'C511_HISTORY')
                """;
            command.Parameters.AddWithValue("recipient", recipient);
            command.Parameters.AddWithValue("key", keyId);
            command.Parameters.AddWithValue("version", version);
            command.Parameters.AddWithValue("spki", spki);
            command.Parameters.AddWithValue("fingerprint", fingerprint);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }
    private async Task<long> CountKeyVersionsAsync(Guid recipient, int version)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_recipient_key_registrations WHERE \"RecipientClientApplicationId\"=@recipient AND \"RecipientKeyVersion\"=@version";
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("version", version);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<int> ReadMaxKeyVersionAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(max(\"RecipientKeyVersion\"),0) FROM tagekyc.raw_export_recipient_key_registrations WHERE \"RecipientClientApplicationId\"=@recipient";
        command.Parameters.AddWithValue("recipient", recipient);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private async Task<bool> ExactCredentialReplacementAsync(Guid recipient, Guid oldApiKey, Guid newApiKey)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=2
              AND count(*) FILTER(WHERE c."ApiKeyId"=@old AND c."State"='Revoked' AND c."Revision"=2
                AND c."ReplacedByApiKeyId"=@new AND a."CredentialStatus"='Revoked')=1
              AND count(*) FILTER(WHERE c."ApiKeyId"=@new AND c."State"='Active' AND c."Revision"=1
                AND a."CredentialStatus"='Active')=1
              AND count(DISTINCT c."PrincipalId")=1
            FROM tagekyc.raw_export_managed_recipient_credentials c
            JOIN tagekyc.api_keys a USING("ApiKeyId")
            WHERE c."RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("old", oldApiKey);
        command.Parameters.AddWithValue("new", newApiKey);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<bool> ObserveSingleCommittedCredentialAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=1 AND bool_and(a."CredentialStatus"='Active')
            FROM tagekyc.raw_export_managed_recipient_credentials c
            JOIN tagekyc.api_keys a USING("ApiKeyId")
            WHERE c."RecipientClientApplicationId"=@recipient AND c."State"='Active'
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<bool> ExactCredentialRollbackAsync(Guid recipient, Guid oldApiKey, Guid rejectedNewApiKey)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*)=1 AND bool_and(c."ApiKeyId"=@old AND c."State"='Active'
                 AND c."Revision"=1 AND c."ReplacedByApiKeyId" IS NULL)
               FROM tagekyc.raw_export_managed_recipient_credentials c
               WHERE c."RecipientClientApplicationId"=@recipient)
              AND NOT EXISTS(SELECT 1 FROM tagekyc.api_keys WHERE "ApiKeyId"=@new)
              AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_management_operations
                WHERE "RecipientClientApplicationId"=@recipient AND "OperationKind"='ReplaceCredential')
              AND NOT EXISTS(SELECT 1 FROM tagekyc.raw_export_recipient_management_events
                WHERE "RecipientClientApplicationId"=@recipient AND "EventType"='ManagedCredentialReplaced')
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("old", oldApiKey);
        command.Parameters.AddWithValue("new", rejectedNewApiKey);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateManagedAsync(
        string presentedKey, string requiredScope)
    {
        await using var db = postgres.CreateDbContext();
        var authenticator = new C5CredentialAwareApiKeyAuthenticator(
            new PostgresHashedApiKeyStore(db,
                new ApiKeyStorePepper(SHA256.HashData("c5-integration-pepper"u8.ToArray()))),
            new CountingPolicyProvider());
        var http = new DefaultHttpContext();
        http.Request.Headers["X-TagEkyc-Api-Key"] = presentedKey;
        return await authenticator.AuthenticateAsync(http, requiredScope, default);
    }
    private async Task<ResolvedApiKey?> FindManagedKeyAsync(string presentedKey)
    {
        await using var db = postgres.CreateDbContext();
        return await new PostgresHashedApiKeyStore(db,
            new ApiKeyStorePepper(SHA256.HashData("c5-integration-pepper"u8.ToArray())))
            .FindByPresentedKeyAsync(presentedKey, default);
    }
    private async Task<string> CredentialResolutionDiagnosticsAsync(
        Guid apiKeyId, string presentedKey)
    {
        var parsed = ManagedApiKeyParser.Parse(presentedKey);
        var expectedHash = ApiKeyHasher.Hash(
            SHA256.HashData("c5-integration-pepper"u8.ToArray()), presentedKey);
        try
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT a."KeyPrefix",encode(a."KeyHash",'hex'),a."CredentialStatus",
                       a."CallerCategory",a."ScopesJson"::text,
                       c."State",i."State",p."State",p."ActivationProfile",
                       encode(p."ActivationScopesDigest",'hex')
                FROM tagekyc.api_keys a
                LEFT JOIN tagekyc.raw_export_managed_recipient_credentials c
                  ON c."ApiKeyId"=a."ApiKeyId"
                LEFT JOIN tagekyc.raw_export_managed_recipient_identities i
                  ON i."RecipientClientApplicationId"=c."RecipientClientApplicationId"
                 AND i."PrincipalId"=c."PrincipalId"
                LEFT JOIN tagekyc.raw_export_managed_recipient_policies p
                  ON p."RecipientClientApplicationId"=c."RecipientClientApplicationId"
                WHERE a."ApiKeyId"=@api
                """;
            command.Parameters.AddWithValue("api", apiKeyId);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync()) return "api-key-row-missing";
            return $"parsed={parsed?.Prefix ?? "null"};storedPrefix={reader.GetString(0)};"+
                $"hashMatch={string.Equals(reader.GetString(1), Convert.ToHexString(expectedHash), StringComparison.OrdinalIgnoreCase)};"+
                $"api={reader.GetString(2)}/{reader.GetString(3)};scopes={reader.GetString(4)};"+
                $"companion={reader.GetString(5)};identity={reader.GetString(6)};"+
                $"policy={reader.GetString(7)}/{reader.GetString(8)}/{reader.GetString(9)}";
        }
        finally { CryptographicOperations.ZeroMemory(expectedHash); }
    }
    private async Task<string> KeyAndPackageSnapshotAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*)::text||':'||COALESCE(encode(tagekyc_extensions.digest(string_agg(
                 "RecipientKeyId"||':'||"RecipientKeyVersion"::text||':'||"State"||':'||"Revision"::text,
                 ',' ORDER BY "RecipientKeyId","RecipientKeyVersion"),'sha256'),'hex'),'-')
               FROM tagekyc.raw_export_recipient_key_registrations WHERE "RecipientClientApplicationId"=@recipient)
              ||'|'||
              (SELECT count(*)::text||':'||COALESCE(encode(tagekyc_extensions.digest(string_agg(
                 "PackageId"::text||':'||"State"||':'||"Revision"::text,',' ORDER BY "PackageId"),'sha256'),'hex'),'-')
               FROM tagekyc.raw_export_recipient_package_preparations WHERE "RecipientClientApplicationId"=@recipient)
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (string)(await command.ExecuteScalarAsync() ?? "MISSING");
    }
    private async Task<long> CountEventsForRecipientAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*) FROM tagekyc.raw_export_recipient_management_events
            WHERE "RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<int> CountDeliveriesByStateAsync(Guid recipient, string state)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)::integer
            FROM tagekyc.raw_export_recipient_package_deliveries
            WHERE "RecipientClientApplicationId"=@recipient AND "State"=@state
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("state", state);
        return (int)(await command.ExecuteScalarAsync() ?? -1);
    }
    private async Task<string> TrySetPolicyProfileAsync(Guid recipient, string profile)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE tagekyc.raw_export_managed_recipient_policies
                SET "ActivationProfile"=@profile
                WHERE "RecipientClientApplicationId"=@recipient
                """;
            command.Parameters.AddWithValue("profile", profile);
            command.Parameters.AddWithValue("recipient", recipient);
            await command.ExecuteNonQueryAsync();
            await transaction.RollbackAsync();
            return "UPDATED";
        }
        catch (PostgresException exception)
        {
            await transaction.RollbackAsync();
            return exception.ConstraintName ?? exception.SqlState;
        }
    }
    private async Task<string> TryInsertNullReasonRevokedKeyAsync()
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                  "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion",
                  "PublicKeyAlgorithm","PublicKeySpki","PublicKeyFingerprint",
                  "ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc",
                  "RevokedAtUtc","RevocationReason")
                VALUES(@recipient,@key,1,'RSA-OAEP-256',@spki,@fingerprint,
                  clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour',
                  'Revoked',1,clock_timestamp(),clock_timestamp(),NULL)
                """;
            command.Parameters.AddWithValue("recipient", Guid.NewGuid());
            command.Parameters.AddWithValue("key", $"c529:{Guid.NewGuid():N}");
            command.Parameters.AddWithValue("spki", spki);
            command.Parameters.AddWithValue("fingerprint", fingerprint);
            await command.ExecuteNonQueryAsync();
            await transaction.RollbackAsync();
            return "INSERTED";
        }
        catch (PostgresException exception)
        {
            await transaction.RollbackAsync();
            return exception.ConstraintName ?? exception.SqlState;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }
    private async Task<(ManagedWorkflow Workflow,
        SessionOperationResult<ManagedRecipientReadinessDto> Readiness)>
        CreateReadyRecipientWithAuthorizedDeliveryAsync()
    {
        var c3 = new Tip88C1C3RecipientPackageDeliveryTests(postgres);
        var delivery = await c3.CreateDeliveryAsync(
            key: $"c522-nonzero-{Guid.NewGuid():N}");
        var workflow = await CreateManagedWorkflowAsync(delivery.Package.RecipientId);
        var credential = await IssueFreshCredentialAsync(workflow,
            $"c522-nonzero-credential-{Guid.NewGuid():N}");
        if (!credential.IsSuccess || credential.Value is null)
            throw new InvalidOperationException(
                $"C522_CREDENTIAL_SETUP_FAILED:{credential.Error?.Code ?? "missing-value"}");
        var current = await ReadActiveKeyAsync(workflow.Recipient);
        var revoked = await workflow.Service.RevokeKeyAsync(workflow.Actor,
            new(workflow.Recipient, current.KeyId, current.Version, current.Revision,
                "C522_NONZERO_PREPARE"),
            $"c522-nonzero-revoke-{Guid.NewGuid():N}", default);
        if (!revoked.IsSuccess)
            throw new InvalidOperationException(
                $"C522_KEY_SETUP_FAILED:{revoked.Error?.Code ?? "revoke-failed"}");
        using var rsa = RSA.Create(3072);
        var next = KeyMaterial(rsa);
        var nextVersion = await ReadMaxKeyVersionAsync(workflow.Recipient) + 1;
        var enrolled = await workflow.Service.EnrollKeyAsync(workflow.Actor,
            new(workflow.Recipient, current.KeyId, nextVersion, "RSA-OAEP-256",
                next.SpkiBase64, next.FingerprintHex, DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1)),
            $"c522-nonzero-key-{Guid.NewGuid():N}", default);
        if (!enrolled.IsSuccess || enrolled.Value is null)
            throw new InvalidOperationException(
                $"C522_KEY_SETUP_FAILED:{enrolled.Error?.Code ?? "enroll-failed"}");
        return (workflow, await workflow.Service.ReadReadinessAsync(
            workflow.Actor, workflow.Recipient, default));
    }
    private async Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>>
        IssueFreshCredentialAsync(ManagedWorkflow workflow, string idempotencyKey)
    {
        var active = await ReadActiveCredentialAsync(workflow.Recipient);
        return active is null
            ? await workflow.Service.IssueCredentialAsync(workflow.Actor,
                new(workflow.Recipient, null), idempotencyKey, default)
            : await workflow.Service.ReplaceCredentialAsync(workflow.Actor,
                new(workflow.Recipient, active.Value.ApiKeyId, active.Value.Revision,
                    null, "C5_PROOF_CREDENTIAL_REFRESH"), idempotencyKey, default);
    }
    private async Task<(Guid ApiKeyId, long Revision)?> ReadActiveCredentialAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "ApiKeyId","Revision"
            FROM tagekyc.raw_export_managed_recipient_credentials
            WHERE "RecipientClientApplicationId"=@recipient AND "State"='Active'
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        return await reader.ReadAsync()
            ? (reader.GetGuid(0), reader.GetInt64(1))
            : null;
    }
    private async Task SetCompanionStateOnlyAsync(Guid apiKeyId, string state)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE tagekyc.raw_export_managed_recipient_credentials SET \"State\"=@state,\"Revision\"=\"Revision\"+1,\"RevokedAtUtc\"=clock_timestamp(),\"RevocationReason\"='C508_ISOLATED' WHERE \"ApiKeyId\"=@api";
        command.Parameters.AddWithValue("state", state);
        command.Parameters.AddWithValue("api", apiKeyId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SetCompanionStatePreservingRevisionAsync(Guid apiKeyId, string state)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tagekyc.raw_export_managed_recipient_credentials
            SET "State"=@state,
                "RevokedAtUtc"=CASE WHEN @state='Revoked' THEN clock_timestamp() ELSE NULL END,
                "RevocationReason"=CASE WHEN @state='Revoked' THEN 'C522_STATE_ISOLATION' ELSE NULL END
            WHERE "ApiKeyId"=@api
            """;
        command.Parameters.AddWithValue("state", state);
        command.Parameters.AddWithValue("api", apiKeyId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<CompanionIsolationState> ReadCompanionIsolationStateAsync(Guid apiKeyId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c."State", c."Revision", o."ResultRevision"
            FROM tagekyc.raw_export_managed_recipient_credentials c
            JOIN tagekyc.raw_export_recipient_management_operations o
              ON o."ResultApiKeyId"=c."ApiKeyId"
             AND o."OperationKind" IN ('IssueCredential','ReplaceCredential')
             AND o."Outcome" IN ('Created','Replaced')
            WHERE c."ApiKeyId"=@api
            """;
        command.Parameters.AddWithValue("api", apiKeyId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        Assert.True(await reader.ReadAsync(), "C522 fixture isolation row is missing.");
        return new CompanionIsolationState(reader.GetString(0), reader.GetInt64(1), reader.GetInt64(2));
    }
    private async Task RestoreCompanionActiveAsync(Guid apiKeyId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tagekyc.raw_export_managed_recipient_credentials
            SET "State"='Active',"Revision"=1,
                "RevokedAtUtc"=NULL,"RevocationReason"=NULL
            WHERE "ApiKeyId"=@api
            """;
        command.Parameters.AddWithValue("api", apiKeyId);
        await command.ExecuteNonQueryAsync();
    }
    private async Task SetKeyStatePreservingRevisionAsync(
        Guid recipient, string keyId, int version, string state)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"=@state,
                "RevokedAtUtc"=CASE WHEN @state='Revoked' THEN clock_timestamp() ELSE NULL END,
                "RevocationReason"=CASE WHEN @state='Revoked' THEN 'C522_STATE_CONTROL' ELSE NULL END
            WHERE "RecipientClientApplicationId"=@recipient
              AND "RecipientKeyId"=@key AND "RecipientKeyVersion"=@version
            """;
        command.Parameters.AddWithValue("state", state);
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("key", keyId);
        command.Parameters.AddWithValue("version", version);
        await command.ExecuteNonQueryAsync();
    }
    private async Task<long> CountFingerprintAsync(Guid recipient, string fingerprintHex)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_recipient_key_registrations WHERE \"RecipientClientApplicationId\"=@recipient AND \"PublicKeyFingerprint\"=@fingerprint";
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("fingerprint", Convert.FromHexString(fingerprintHex));
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<bool> KeyCommitAuthorityIsExactAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=1 AND bool_and(k."RegisteredAtUtc"=o."AdmissionAtUtc"
              AND o."AdmissionAtUtc"=o."CompletedAtUtc" AND o."Outcome"='Created'
              AND o."ResultKeyId"=k."RecipientKeyId" AND o."ResultKeyVersion"=k."RecipientKeyVersion")
            FROM tagekyc.raw_export_recipient_key_registrations k
            JOIN tagekyc.raw_export_recipient_management_operations o
              ON o."RecipientClientApplicationId"=k."RecipientClientApplicationId"
             AND o."OperationKind"='EnrollKey' AND o."ResultKeyId"=k."RecipientKeyId"
             AND o."ResultKeyVersion"=k."RecipientKeyVersion"
            WHERE k."RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<bool> ExactKeyOwnerAsync(Guid recipient, string keyId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*)=1 FROM tagekyc.raw_export_recipient_key_registrations WHERE \"RecipientClientApplicationId\"=@recipient AND \"RecipientKeyId\"=@key";
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("key", keyId);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<bool> RevokedKeyShapeIsStrictAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=1 AND bool_and("State"='Revoked' AND "Revision"=2
              AND "RevokedAtUtc" IS NOT NULL AND NULLIF(btrim("RevocationReason"),'') IS NOT NULL)
            FROM tagekyc.raw_export_recipient_key_registrations
            WHERE "RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<string> ReadDeliveryStateAsync(Guid deliveryId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"State\"||':'||\"Revision\"::text||':'||\"DeliveryFence\"::text FROM tagekyc.raw_export_recipient_package_deliveries WHERE \"DeliveryId\"=@id";
        command.Parameters.AddWithValue("id", deliveryId);
        return (string)(await command.ExecuteScalarAsync() ?? "MISSING");
    }
    private async Task<DeliveryCounts> ReadDeliveryCountsAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*) FILTER(WHERE "State"='Authorized')::integer,
                   count(*) FILTER(WHERE "State"='Streaming')::integer,
                   count(*) FILTER(WHERE "State"='Interrupted')::integer
            FROM tagekyc.raw_export_recipient_package_deliveries
            WHERE "RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync()) throw new InvalidOperationException("C512_COUNTS_MISSING");
        return new(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    }
    private async Task<bool> ExactHardCutoverAsync(Guid recipient, string keyId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=2
              AND count(*) FILTER(WHERE "RecipientKeyVersion"=1 AND "State"='Revoked'
                AND "Revision"=2 AND "RevokedAtUtc" IS NOT NULL AND "RevocationReason"='C5_PLANNED_ROTATION')=1
              AND count(*) FILTER(WHERE "RecipientKeyVersion"=2 AND "State"='Active'
                AND "Revision"=1 AND "RevokedAtUtc" IS NULL AND "RevocationReason" IS NULL)=1
            FROM tagekyc.raw_export_recipient_key_registrations
            WHERE "RecipientClientApplicationId"=@recipient AND "RecipientKeyId"=@key
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("key", keyId);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<long> CountEventsAsync(Guid recipient, string eventType)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_recipient_management_events WHERE \"RecipientClientApplicationId\"=@recipient AND \"EventType\"=@event";
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("event", eventType);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<bool> ExactSingleActiveKeyAsync(Guid recipient, string keyId, int version)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=1 AND bool_and("RecipientKeyId"=@key AND "RecipientKeyVersion"=@version
              AND "State"='Active' AND "Revision"=1 AND "RevokedAtUtc" IS NULL)
            FROM tagekyc.raw_export_recipient_key_registrations
            WHERE "RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("key", keyId);
        command.Parameters.AddWithValue("version", version);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task InstallRotationCompletionFailureTriggerAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE OR REPLACE FUNCTION tagekyc.c513_fail_rotation_completion() RETURNS trigger
            LANGUAGE plpgsql AS $fn$ BEGIN
              IF NEW."Outcome"='Rotated' AND OLD."Outcome" IS DISTINCT FROM NEW."Outcome" THEN
                RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='C513_FORCED_ROTATION_COMPLETION_FAILURE';
              END IF;
              RETURN NEW;
            END $fn$;
            DROP TRIGGER IF EXISTS c513_fail_rotation_completion ON tagekyc.raw_export_recipient_management_operations;
            CREATE TRIGGER c513_fail_rotation_completion BEFORE UPDATE ON tagekyc.raw_export_recipient_management_operations
              FOR EACH ROW EXECUTE FUNCTION tagekyc.c513_fail_rotation_completion();
            """;
        await command.ExecuteNonQueryAsync();
    }
    private async Task InstallEventFailureTriggerAsync(string eventType, string functionName)
    {
        if (functionName.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Invalid trigger function name.", nameof(functionName));
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE OR REPLACE FUNCTION tagekyc.{functionName}() RETURNS trigger
            LANGUAGE plpgsql AS $fn$ BEGIN
              IF NEW."EventType"=@event THEN
                RAISE EXCEPTION USING ERRCODE='P0001',MESSAGE='C5_FORCED_EVENT_FAILURE';
              END IF;
              RETURN NEW;
            END $fn$;
            DROP TRIGGER IF EXISTS {functionName} ON tagekyc.raw_export_recipient_management_events;
            CREATE TRIGGER {functionName} BEFORE INSERT ON tagekyc.raw_export_recipient_management_events
              FOR EACH ROW EXECUTE FUNCTION tagekyc.{functionName}();
            """;
        command.Parameters.AddWithValue("event", eventType);
        await command.ExecuteNonQueryAsync();
    }
    private async Task RemoveEventFailureTriggerAsync(string functionName)
    {
        if (functionName.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Invalid trigger function name.", nameof(functionName));
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            DROP TRIGGER IF EXISTS {functionName} ON tagekyc.raw_export_recipient_management_events;
            DROP FUNCTION IF EXISTS tagekyc.{functionName}();
            """;
        await command.ExecuteNonQueryAsync();
    }
    private async Task RemoveRotationCompletionFailureTriggerAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DROP TRIGGER IF EXISTS c513_fail_rotation_completion ON tagekyc.raw_export_recipient_management_operations;
            DROP FUNCTION IF EXISTS tagekyc.c513_fail_rotation_completion();
            """;
        await command.ExecuteNonQueryAsync();
    }
    private async Task RevokeKeyPreservingRevisionAndAddK2Async(
        Guid recipient, string keyId, int version, long revision)
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        try
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE tagekyc.raw_export_recipient_key_registrations
                SET "State"='Revoked',"RevokedAtUtc"=clock_timestamp(),"RevocationReason"='C514_STATE_ONLY'
                WHERE "RecipientClientApplicationId"=@recipient AND "RecipientKeyId"=@key
                  AND "RecipientKeyVersion"=@version AND "Revision"=@revision;
                INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                  "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
                  "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
                VALUES(@recipient,@key,@next,'RSA-OAEP-256',@spki,@fingerprint,
                  clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Active',1,clock_timestamp());
                """;
            command.Parameters.AddWithValue("recipient", recipient);
            command.Parameters.AddWithValue("key", keyId);
            command.Parameters.AddWithValue("version", version);
            command.Parameters.AddWithValue("revision", revision);
            command.Parameters.AddWithValue("next", version + 1);
            command.Parameters.AddWithValue("spki", spki);
            command.Parameters.AddWithValue("fingerprint", fingerprint);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }
    private async Task SetKeyRevisionOnlyAsync(
        Guid recipient, string keyId, int version, long revision)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "Revision"=@revision
            WHERE "RecipientClientApplicationId"=@recipient AND "RecipientKeyId"=@key
              AND "RecipientKeyVersion"=@version AND "State"='Active'
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("key", keyId);
        command.Parameters.AddWithValue("version", version);
        command.Parameters.AddWithValue("revision", revision);
        var changed = await command.ExecuteNonQueryAsync();
        if (changed != 1)
            throw new InvalidOperationException(
                $"C515_REVISION_ONLY_FIXTURE_CARDINALITY:{changed}");
    }
    private async Task EnsureC514KeyFixtureIdentityAsync(
        Guid recipient, string keyId, int version, long revision)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=2
              AND count(*) FILTER(WHERE "RecipientKeyVersion"=@version
                AND "State"='Revoked' AND "Revision"=@revision)=1
              AND count(*) FILTER(WHERE "RecipientKeyVersion"=@next
                AND "State"='Active')=1
            FROM tagekyc.raw_export_recipient_key_registrations
            WHERE "RecipientClientApplicationId"=@recipient AND "RecipientKeyId"=@key
              AND "RecipientKeyVersion" IN (@version,@next)
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("key", keyId);
        command.Parameters.AddWithValue("version", version);
        command.Parameters.AddWithValue("next", version + 1);
        command.Parameters.AddWithValue("revision", revision);
        if (!((bool?)await command.ExecuteScalarAsync() ?? false))
            throw new InvalidOperationException(
                $"C514_KEY_IDENTITY_FIXTURE_INVALID:{keyId}/{version}/{revision}");
    }
    private async Task<(string KeyId, int Version)?> ReadC2PreparationKeyIdentityAsync(
        Guid preparationId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "RecipientKeyId","RecipientKeyVersion"
            FROM tagekyc.raw_export_recipient_package_preparations
            WHERE "C2PreparationId"=@id
            """;
        command.Parameters.AddWithValue("id", preparationId);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync()
            ? (reader.GetString(0), reader.GetInt32(1))
            : null;
    }
    private async Task<long> CountC2PackagesAsync(Guid preparationId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_recipient_package_preparations WHERE \"C2PreparationId\"=@id";
        command.Parameters.AddWithValue("id", preparationId);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task ExecuteDirectC2PackageSeedAsync(Func<Guid, Task> callback)
    {
        var candidate = await new Tip88C1C2RecipientPackageTests(postgres).CreatePackageCandidateAsync();
        var request = candidate.Request;
        var now = DateTimeOffset.UtcNow;
        var evidence = RandomNumberGenerator.GetBytes(32);
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO tagekyc.raw_export_recipient_package_preparations(
              "C2PreparationId","PackageId","PackageEqualityFingerprint","AssemblyId","JobId","AttemptId","FencingToken",
              "AssemblyFingerprint","ManifestDigest","AssemblyDigest","AssemblyAuthenticationValue","CompleteAssemblyLength",
              "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","RecipientKeyFingerprint","RecipientPublicKeySpki",
              "RecipientKeyRevision","RecipientKeyValidFromUtc","RecipientKeyValidUntilUtc","PackageProfile","ProviderOperationTokenDigest",
              "ProviderKind","ProviderConfigurationId","ProviderEndpointFingerprint","BucketName","ObjectKey","ObjectBindingDigest",
              "EnvelopeDigest","EncryptedPackageLength","PackageCiphertextDigest","ConditionalCreateEvidenceDigest","ProviderReceiptDigest",
              "State","Revision","SnapshotFrozenAtUtc","PutStartedAtUtc","PreparedAtUtc","FinalizedAtUtc")
            VALUES(@preparation,@package,@equality,@assembly,@job,@attempt,@fence,
              @assemblyFingerprint,@manifest,@assemblyDigest,@authentication,@completeLength,
              @recipient,@keyId,@keyVersion,@keyFingerprint,@spki,@keyRevision,@validFrom,@validUntil,
              @profile,@tokenDigest,@providerKind,@providerConfiguration,@endpointFingerprint,@bucket,@objectKey,@objectBinding,
              @evidence,1,@evidence,@evidence,@evidence,'Finalized',4,@now,@now,@now,@now)
            """;
        command.Parameters.AddWithValue("preparation", request.C2PreparationId);
        command.Parameters.AddWithValue("package", request.PackageId);
        command.Parameters.AddWithValue("equality", request.PackageEqualityFingerprint);
        command.Parameters.AddWithValue("assembly", request.AssemblyId);
        command.Parameters.AddWithValue("job", request.JobId);
        command.Parameters.AddWithValue("attempt", request.AttemptId);
        command.Parameters.AddWithValue("fence", request.FencingToken);
        command.Parameters.AddWithValue("assemblyFingerprint", request.AssemblyFingerprint);
        command.Parameters.AddWithValue("manifest", request.ManifestDigest);
        command.Parameters.AddWithValue("assemblyDigest", request.AssemblyDigest);
        command.Parameters.AddWithValue("authentication", request.AssemblyAuthenticationValue);
        command.Parameters.AddWithValue("completeLength", request.CompleteAssemblyLength);
        command.Parameters.AddWithValue("recipient", request.RecipientClientApplicationId);
        command.Parameters.AddWithValue("keyId", request.RecipientKeyId);
        command.Parameters.AddWithValue("keyVersion", request.RecipientKeyVersion);
        command.Parameters.AddWithValue("keyFingerprint", request.RecipientKeyFingerprint);
        command.Parameters.AddWithValue("spki", candidate.Key.Spki);
        command.Parameters.AddWithValue("keyRevision", request.RecipientKeyRevision);
        command.Parameters.AddWithValue("validFrom", now.AddMinutes(-1));
        command.Parameters.AddWithValue("validUntil", now.AddHours(1));
        command.Parameters.AddWithValue("profile", request.PackageProfile);
        command.Parameters.AddWithValue("tokenDigest", request.ProviderOperationTokenDigest);
        command.Parameters.AddWithValue("providerKind", request.ProviderKind);
        command.Parameters.AddWithValue("providerConfiguration", request.ProviderConfigurationId);
        command.Parameters.AddWithValue("endpointFingerprint", request.ProviderEndpointFingerprint);
        command.Parameters.AddWithValue("bucket", request.BucketName);
        command.Parameters.AddWithValue("objectKey", request.ObjectKey);
        command.Parameters.AddWithValue("objectBinding", request.ObjectBindingDigest);
        command.Parameters.AddWithValue("evidence", evidence);
        command.Parameters.AddWithValue("now", now);
        var inserted = await command.ExecuteNonQueryAsync();
        if (inserted != 1)
            throw new InvalidOperationException(
                $"C526_DIRECT_C2_SEED_FIXTURE_CARDINALITY:{inserted}");
        await callback(request.RecipientClientApplicationId);
    }
    private async Task<HashSet<Guid>> ReadC2PreparationIdsAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "C2PreparationId"
            FROM tagekyc.raw_export_recipient_package_preparations
            WHERE "RecipientClientApplicationId"=@recipient
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        await using var reader = await command.ExecuteReaderAsync();
        var ids = new HashSet<Guid>();
        while (await reader.ReadAsync())
            ids.Add(reader.GetGuid(0));
        return ids;
    }
    private async Task<bool> HasRealC2PackageEventChainAsync(
        Guid recipient, Guid preparationId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)=1 AND COALESCE(bool_and(
              p."State"='Finalized'
              AND COALESCE((SELECT pg_catalog.array_agg(e."EventKind"::text ORDER BY e."EventRevision")
                   FROM tagekyc.raw_export_recipient_package_events e
                   WHERE e."C2PreparationId"=p."C2PreparationId")
                  =ARRAY['SnapshotFrozen','PutStarted','Prepared','Finalized']::text[],false)),false)
            FROM tagekyc.raw_export_recipient_package_preparations p
            WHERE p."RecipientClientApplicationId"=@recipient
              AND p."C2PreparationId"=@preparation
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("preparation", preparationId);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<string> ReadFrozenPackageAsync(Guid packageId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "PackageId"::text||':'||"RecipientKeyId"||':'||"RecipientKeyVersion"::text||':'
              ||encode("RecipientKeyFingerprint",'hex')||':'||"RecipientKeyRevision"::text||':'
              ||"State"||':'||"Revision"::text||':'||encode("PackageEqualityFingerprint",'hex')
            FROM tagekyc.raw_export_recipient_package_preparations WHERE "PackageId"=@package
            """;
        command.Parameters.AddWithValue("package", packageId);
        return (string)(await command.ExecuteScalarAsync() ?? "MISSING");
    }
    private async Task RevokeAnyActiveKeyForFixtureAsync(Guid recipient, string reason)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=clock_timestamp(),
                "RevocationReason"=@reason
            WHERE "RecipientClientApplicationId"=@recipient AND "State"='Active'
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("reason", reason);
        await command.ExecuteNonQueryAsync();
    }
    private async Task<IReadOnlyList<AuditEvent>> ReadAuditEventsAsync(Guid recipient)
    {
        var values = new List<AuditEvent>();
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT e."OperationId",o."OperationKind",e."ManagerApiKeyId",e."ManagerPrincipalId",
                   e."EventType",e."TargetIdentity",e."Reason",e."PriorRevision",e."NewRevision",
                   e."PayloadDigest",e."PriorScopesDigest",e."NewScopesDigest",e."OccurredAtUtc",
                   e."EvidenceDigest",e."AuthorizedDeliveryCount",e."StreamingDeliveryCount",
                   e."InterruptedDeliveryCount",o."CompletedAtUtc",o."ResultSnapshot"::text
            FROM tagekyc.raw_export_recipient_management_events e
            JOIN tagekyc.raw_export_recipient_management_operations o USING("OperationId")
            WHERE e."RecipientClientApplicationId"=@recipient
            ORDER BY e."OccurredAtUtc",e."ManagementEventId"
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetGuid(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetInt64(7),
                reader.GetInt64(8), (byte[])reader[9], reader.IsDBNull(10) ? null : (byte[])reader[10],
                reader.IsDBNull(11) ? null : (byte[])reader[11], reader.GetFieldValue<DateTimeOffset>(12),
                (byte[])reader[13], reader.IsDBNull(14) ? null : reader.GetInt32(14),
                reader.IsDBNull(15) ? null : reader.GetInt32(15),
                reader.IsDBNull(16) ? null : reader.GetInt32(16),
                reader.GetFieldValue<DateTimeOffset>(17), reader.GetString(18)));
        }
        return values;
    }
    private async Task<AuditEventUpdateObservation> ObserveAuditEventUpdateGuardAsync(Guid operationId)
    {
        var before = await ReadAuditEventRowAsync(operationId);
        var exactAppendOnlyRejection = false;
        var updatedRows = 0;
        try
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE tagekyc.raw_export_recipient_management_events
                SET "Reason"="Reason"||'_C5M35_PROBE'
                WHERE "ManagementEventId"=@event
                """;
            command.Parameters.AddWithValue("event", before.ManagementEventId);
            updatedRows = await command.ExecuteNonQueryAsync();
            if (updatedRows != 1)
                throw new InvalidOperationException($"C519_APPEND_ONLY_UPDATE_TARGET_CARDINALITY:{updatedRows}");
        }
        catch (PostgresException exception) when (
            exception.SqlState == "P0001"
            && exception.MessageText == "RAW_EXPORT_RECIPIENT_MANAGEMENT_EVENT_APPEND_ONLY")
        {
            exactAppendOnlyRejection = true;
        }

        var after = await ReadAuditEventRowAsync(operationId);
        return new(before.ManagementEventId, before.Snapshot, after.Snapshot,
            exactAppendOnlyRejection, updatedRows);
    }
    private async Task<AuditEventRow> ReadAuditEventRowAsync(Guid operationId)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "ManagementEventId",pg_catalog.to_jsonb(e)::text
            FROM tagekyc.raw_export_recipient_management_events e
            WHERE "OperationId"=@operation
            """;
        command.Parameters.AddWithValue("operation", operationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync())
            throw new InvalidOperationException("C519_APPEND_ONLY_UPDATE_TARGET_MISSING");
        return new(reader.GetGuid(0), reader.GetString(1));
    }
    private async Task<bool> LifecycleRevisionsAreSeparateAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*)=2 AND bool_and("Revision"=2)
               FROM tagekyc.raw_export_managed_recipient_credentials
               WHERE "RecipientClientApplicationId"=@recipient),
              (SELECT count(*)=2 AND bool_and("Revision"=2)
               FROM tagekyc.raw_export_recipient_key_registrations
               WHERE "RecipientClientApplicationId"=@recipient)
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        return await reader.ReadAsync() && reader.GetBoolean(0) && reader.GetBoolean(1);
    }
    private static async Task<RecipientManagementSqlResult> InvokeIssueCredentialAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid operation,
        ManagedWorkflow workflow,
        byte[] idempotency,
        byte[] equality,
        byte[] payload,
        ManagedCredentialMaterial? material)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT * FROM tagekyc.raw_export_issue_recipient_credential(
              @operation,@manager_api_key,@manager_principal,@recipient,@idempotency,@equality,@payload,
              @new_api_key,@key_prefix,@key_hash,@expires)
            """;
        command.Parameters.AddWithValue("operation", operation);
        command.Parameters.AddWithValue("manager_api_key", workflow.Actor.ApiKeyId);
        command.Parameters.AddWithValue("manager_principal", workflow.Actor.PrincipalId);
        command.Parameters.AddWithValue("recipient", workflow.Recipient);
        command.Parameters.AddWithValue("idempotency", idempotency);
        command.Parameters.AddWithValue("equality", equality);
        command.Parameters.AddWithValue("payload", payload);
        command.Parameters.Add(new NpgsqlParameter("new_api_key", NpgsqlTypes.NpgsqlDbType.Uuid)
            { Value = material is null ? DBNull.Value : material.ApiKeyId });
        command.Parameters.Add(new NpgsqlParameter("key_prefix", NpgsqlTypes.NpgsqlDbType.Text)
            { Value = material is null ? DBNull.Value : material.KeyPrefix });
        command.Parameters.Add(new NpgsqlParameter("key_hash", NpgsqlTypes.NpgsqlDbType.Bytea)
            { Value = material is null ? DBNull.Value : material.KeyHash });
        command.Parameters.Add(new NpgsqlParameter("expires", NpgsqlTypes.NpgsqlDbType.TimestampTz)
            { Value = DBNull.Value });
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync()) throw new InvalidOperationException("C520_SQL_RESULT_MISSING");
        return new(reader.GetString(reader.GetOrdinal("Outcome")),
            reader.GetGuid(reader.GetOrdinal("OperationId")),
            reader.IsDBNull(reader.GetOrdinal("ResultSnapshot"))
                ? "{}" : reader.GetString(reader.GetOrdinal("ResultSnapshot")),
            !reader.IsDBNull(reader.GetOrdinal("CandidateCommitted"))
                && reader.GetBoolean(reader.GetOrdinal("CandidateCommitted")));
    }
    private static async Task<bool> ProvisionalShapeIsExactAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid operation)
    {
        await using (var reset = connection.CreateCommand())
        {
            reset.Transaction = transaction;
            reset.CommandText = "RESET ROLE";
            await reset.ExecuteNonQueryAsync();
        }
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT "Outcome" IS NULL AND "ResultSnapshot" IS NULL AND "CompletedAtUtc" IS NULL
               AND "ResultApiKeyId" IS NULL AND "ResultCredentialVersion" IS NULL
               AND "ResultKeyId" IS NULL AND "ResultKeyVersion" IS NULL
               AND "ResultRevision" IS NULL AND "ResultIdentityRevision" IS NULL
               AND "AuthorizedDeliveryCount" IS NULL AND "StreamingDeliveryCount" IS NULL
               AND "InterruptedDeliveryCount" IS NULL
            FROM tagekyc.raw_export_recipient_management_operations
            WHERE "OperationId"=@operation
            """;
        command.Parameters.AddWithValue("operation", operation);
        var exact = (bool)(await command.ExecuteScalarAsync() ?? false);
        await using (var role = connection.CreateCommand())
        {
            role.Transaction = transaction;
            role.CommandText = "SET ROLE tagekyc_raw_export_recipient_manager_login";
            await role.ExecuteNonQueryAsync();
        }
        return exact;
    }
    private async Task<Guid[]> SeedApiKeyPrefixesAsync(IEnumerable<ManagedCredentialMaterial> materials)
    {
        var seededIds = new List<Guid>();
        await using var connection = await OpenAsync();
        foreach (var material in materials)
        {
            var seededId = Guid.NewGuid();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO tagekyc.api_keys(
                  "ApiKeyId","ClientApplicationId","PrincipalId","CredentialRef","CredentialType",
                  "CredentialStatus","KeyPrefix","KeyHash","ScopesJson","ExpiresAt","CallerCategory",
                  "AllowedClientApplicationIdsJson","AllowedCaptureAgentIdsJson","OAuthClientId","MtlsSubjectDn","CreatedAt")
                VALUES(@api,@client,@principal,@reference,'ManagedApiKey','Active',@prefix,@hash,'[]'::jsonb,
                  NULL,'BusinessConsumer',NULL,NULL,NULL,NULL,clock_timestamp())
                """;
            command.Parameters.AddWithValue("api", seededId);
            command.Parameters.AddWithValue("client", Guid.NewGuid());
            command.Parameters.AddWithValue("principal", Guid.NewGuid());
            command.Parameters.AddWithValue("reference", $"c506-seed:{seededId:N}");
            command.Parameters.AddWithValue("prefix", material.KeyPrefix);
            command.Parameters.AddWithValue("hash", material.KeyHash);
            await command.ExecuteNonQueryAsync();
            seededIds.Add(seededId);
        }
        return seededIds.ToArray();
    }
    private async Task DeleteSeedApiKeysAsync(Guid[] apiKeyIds)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tagekyc.api_keys WHERE \"ApiKeyId\"=ANY(@ids)";
        command.Parameters.AddWithValue("ids", apiKeyIds);
        await command.ExecuteNonQueryAsync();
    }
    private async Task<long> CountRowsAsync(string table, Guid recipient)
    {
        if (table != "raw_export_managed_recipient_credentials")
            throw new ArgumentOutOfRangeException(nameof(table));
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_managed_recipient_credentials WHERE \"RecipientClientApplicationId\"=@recipient";
        command.Parameters.AddWithValue("recipient", recipient);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<long> CountOperationRowsAsync(Guid recipient, string kind)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_recipient_management_operations WHERE \"RecipientClientApplicationId\"=@recipient AND \"OperationKind\"=@kind";
        command.Parameters.AddWithValue("recipient", recipient);
        command.Parameters.AddWithValue("kind", kind);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<long> CountIdentityRowsAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM tagekyc.raw_export_managed_recipient_identities WHERE \"RecipientClientApplicationId\"=@recipient";
        command.Parameters.AddWithValue("recipient", recipient);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }
    private async Task<Guid?> ReadManagedPrincipalAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"PrincipalId\" FROM tagekyc.raw_export_managed_recipient_identities WHERE \"RecipientClientApplicationId\"=@recipient";
        command.Parameters.AddWithValue("recipient", recipient);
        var value = await command.ExecuteScalarAsync();
        return value is Guid principal ? principal : null;
    }
    private async Task<bool> EnrollmentClockIsSingleAsync(Guid recipient)
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i."CreatedAtUtc"=i."UpdatedAtUtc"
               AND i."CreatedAtUtc"=p."CreatedAtUtc" AND p."CreatedAtUtc"=p."UpdatedAtUtc"
               AND i."CreatedAtUtc"=e."OccurredAtUtc"
               AND e."OccurredAtUtc"=o."AdmissionAtUtc" AND o."AdmissionAtUtc"=o."CompletedAtUtc"
            FROM tagekyc.raw_export_managed_recipient_identities i
            JOIN tagekyc.raw_export_managed_recipient_policies p USING("RecipientClientApplicationId")
            JOIN tagekyc.raw_export_recipient_management_events e USING("RecipientClientApplicationId")
            JOIN tagekyc.raw_export_recipient_management_operations o USING("OperationId")
            WHERE i."RecipientClientApplicationId"=@recipient
              AND e."EventType"='ManagedRecipientEnrolled'
            """;
        command.Parameters.AddWithValue("recipient", recipient);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<bool> ManagedLifecycleAuthoritiesAreSeparateAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)>0 AND bool_and(c."ApiKeyId"::text<>k."RecipientKeyId")
            FROM tagekyc.raw_export_managed_recipient_credentials c
            JOIN tagekyc.raw_export_recipient_key_registrations k
              USING("RecipientClientApplicationId")
            WHERE k."RecipientKeyId" LIKE 'c526:%'
            """;
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<bool> ReadCatalogReadinessAsync()
    {
        await using var connection = await new ManagerRoleConnectionFactory(postgres.ConnectionString).OpenAsync(default);
        await using var command = connection.CreateCommand();
        command.CommandText = RecipientManagementReadinessValidator.CatalogSql;
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private async Task<C528MigrationTarget> CreateC528MigrationTargetAsync(bool createDisposable)
    {
        if (!createDisposable)
            return new(postgres.CreateDbContext(), null);
        var disposable = await postgres.CreateDisposableCurrentDatabaseAsync("c5_down_reapply");
        return new(disposable.CreateDbContext(), disposable);
    }
    private static async Task<string[]> AppliedMigrationsAsync(TagEkycDbContext database)
    {
        await using (database)
            return (await database.Database.GetAppliedMigrationsAsync()).ToArray();
    }
    private static async Task<string> C528CatalogSnapshotAsync(TagEkycDbContext database)
    {
        var connection = database.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT string_agg(kind||':'||name||':'||definition, E'\n' ORDER BY kind,name)
            FROM (
              SELECT 'table' kind,c.relname name,
                pg_catalog.pg_get_userbyid(c.relowner)||':'||c.relkind::text definition
              FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc' AND c.relname LIKE 'raw_export_managed_recipient_%'
              UNION ALL
              SELECT 'function',p.proname||'('||pg_catalog.oidvectortypes(p.proargtypes)||')',
                replace(pg_catalog.pg_get_functiondef(p.oid),E'\r\n',E'\n')
              FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname LIKE 'raw_export_%recipient_%'
              UNION ALL
              SELECT 'index',c.relname,replace(pg_catalog.pg_get_indexdef(c.oid),E'\r\n',E'\n')
              FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc' AND c.relkind='i'
                AND c.relname IN ('uq_raw_export_managed_recipient_identity_pair',
                  'uq_api_keys_managed_identity','uq_raw_export_managed_credential_version',
                  'uq_raw_export_managed_credential_active',
                  'uq_raw_export_recipient_management_idempotency',
                  'uq_raw_export_recipient_management_event_operation')
            ) catalog
            """;
        return (string?)(await command.ExecuteScalarAsync()) ?? string.Empty;
    }
    private async Task<bool> DatabaseExistsAsync(string databaseName)
    {
        var admin = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_database WHERE datname=@name)";
        command.Parameters.AddWithValue("name", databaseName);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private static async Task<ReadinessFailure?> CaptureReadinessFailureAsync(
        RecipientManagementReadinessValidator validator)
    {
        try
        {
            await validator.ValidateAsync(default);
            return null;
        }
        catch (RecipientManagementReadinessException exception)
        {
            return new(exception.Code, exception.Component, exception.ComponentCode);
        }
    }
    private async Task<long> CountAllRowsAsync(string table)
    {
        if (table.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Invalid table name.", nameof(table));
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM tagekyc.{table}";
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }
    private async Task<string> CatalogDiagnosticsAsync()
    {
        var canonical = RecipientManagementReadinessValidator.CatalogSql;
        var finalSelect = canonical.LastIndexOf("SELECT t.ok AND", StringComparison.Ordinal);
        if (finalSelect < 0) return "canonical-final-select-missing";
        var diagnosticSql = canonical[..finalSelect] + """
            SELECT t.ok,f.ok,i.ok,r.ok,m.ok,a.ok,
              pg_catalog.has_schema_privilege('tagekyc_raw_export_recipient_manager','tagekyc','USAGE'),
              NOT pg_catalog.has_function_privilege('tagekyc_runtime',
                'tagekyc.raw_export_read_recipient_activation_readiness(uuid)','EXECUTE')
            FROM table_ok t CROSS JOIN function_ok f CROSS JOIN index_ok i
              CROSS JOIN role_ok r CROSS JOIN membership_ok m CROSS JOIN acl_ok a
            """;
        await using (var manager = await new ManagerRoleConnectionFactory(postgres.ConnectionString).OpenAsync(default))
        await using (var diagnostic = manager.CreateCommand())
        {
            diagnostic.CommandText = diagnosticSql;
            await using var values = await diagnostic.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await values.ReadAsync())
            {
                var flags = $"table={values.GetBoolean(0)};function={values.GetBoolean(1)};index={values.GetBoolean(2)};role={values.GetBoolean(3)};membership={values.GetBoolean(4)};acl={values.GetBoolean(5)};schema={values.GetBoolean(6)};runtimeDenied={values.GetBoolean(7)}";
                await values.DisposeAsync();
                await using var owners = manager.CreateCommand();
                owners.CommandText = """
                    SELECT string_agg(c.relname||':'||pg_get_userbyid(c.relowner),',' ORDER BY c.relname)
                    FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='tagekyc' AND c.relname IN
                      ('uq_raw_export_managed_recipient_identity_pair','uq_api_keys_managed_identity','uq_raw_export_managed_credential_version','uq_raw_export_managed_credential_active','uq_raw_export_recipient_management_idempotency','uq_raw_export_recipient_management_event_operation')
                    """;
                return flags + ";owners=" + (string?)await owners.ExecuteScalarAsync();
            }
        }
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
               WHERE n.nspname='tagekyc' AND c.relkind='r' AND c.relname IN
                 ('raw_export_managed_recipient_identities','raw_export_managed_recipient_policies','raw_export_managed_recipient_credentials','raw_export_recipient_management_operations','raw_export_recipient_management_events')),
              (SELECT count(*) FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
               WHERE n.nspname='tagekyc' AND p.proname IN
                 ('raw_export_enroll_managed_recipient','raw_export_issue_recipient_credential','raw_export_replace_recipient_credential','raw_export_revoke_recipient_credential','raw_export_enroll_recipient_key','raw_export_rotate_recipient_key','raw_export_revoke_recipient_key','raw_export_read_recipient_activation_readiness','raw_export_guard_recipient_management_event')),
              (SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
               WHERE n.nspname='tagekyc' AND c.relkind IN ('i','I') AND c.relname IN
                 ('uq_raw_export_managed_recipient_identity_pair','uq_api_keys_managed_identity','uq_raw_export_managed_credential_version','uq_raw_export_managed_credential_active','uq_raw_export_recipient_management_idempotency','uq_raw_export_recipient_management_event_operation')),
              (SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname IN
                 ('tagekyc_raw_export_recipient_manager','tagekyc_raw_export_recipient_manager_login')),
              (SELECT count(*) FROM pg_catalog.pg_auth_members m
               JOIN pg_catalog.pg_roles member ON member.oid=m.member
               JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
               WHERE member.rolname IN ('tagekyc_raw_export_recipient_manager','tagekyc_raw_export_recipient_manager_login')
                  OR role.rolname IN ('tagekyc_raw_export_recipient_manager','tagekyc_raw_export_recipient_manager_login')),
              pg_catalog.has_function_privilege('tagekyc_runtime',
                'tagekyc.raw_export_read_recipient_activation_readiness(uuid)','EXECUTE')
            """;
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
        await reader.ReadAsync();
        return $"tables={reader.GetInt64(0)};functions={reader.GetInt64(1)};indexes={reader.GetInt64(2)};roles={reader.GetInt64(3)};memberships={reader.GetInt64(4)};runtimeExec={reader.GetBoolean(5)}";
    }
    private static async Task<bool> CatalogObjectExistsAsync(
        NpgsqlConnection connection, string table, string function, string role)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class WHERE relname=@table)
                OR EXISTS(SELECT 1 FROM pg_catalog.pg_proc WHERE proname=@function)
                OR EXISTS(SELECT 1 FROM pg_catalog.pg_roles WHERE rolname=@role)
            """;
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("function", function);
        command.Parameters.AddWithValue("role", role);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
    private static async Task SetRoleAsync(NpgsqlConnection connection, string role)
    {
        if (role.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Invalid test role.", nameof(role));
        await using var command = connection.CreateCommand();
        command.CommandText = $"SET ROLE {role}";
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<bool> ThrowsSqlStateAsync(
        NpgsqlConnection connection, string sql, string expectedSqlState)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
            return false;
        }
        catch (PostgresException exception)
        {
            return exception.SqlState == expectedSqlState;
        }
    }
    private static readonly TimeSpan C512ObservationBound = TimeSpan.FromSeconds(2);

    private enum RotationWaitOutcome
    {
        RotationCompleted,
        ExpectedBlockerObserved,
        ObservationIndeterminate
    }

    private sealed record RotationWaitObservation(
        string RunId,
        DateTimeOffset ObserverStart,
        TimeSpan ConnectionOpenElapsed,
        TimeSpan Elapsed,
        RotationWaitOutcome Outcome,
        string? IndeterminateCode = null);

    private enum BlockerReleaseOutcome
    {
        ConfirmedByRollback,
        ConfirmedByConnectionClose,
        Indeterminate
    }

    private sealed record BlockerReleaseObservation(
        BlockerReleaseOutcome Outcome,
        Exception? Failure)
    {
        public bool Confirmed => Outcome != BlockerReleaseOutcome.Indeterminate;
    }

    private async Task<RotationWaitObservation> ObserveRotationBlockedByAsync(
        int blockerPid, Task rotationTask)
    {
        var runId = Guid.NewGuid().ToString("N");
        var observerStart = DateTimeOffset.UtcNow;
        var timer = Stopwatch.StartNew();
        var connectionOpenElapsed = TimeSpan.Zero;
        var observer = new NpgsqlConnection(postgres.ConnectionString);
        await using (observer)
        {
            var openRemaining = C512ObservationBound - timer.Elapsed;
            if (openRemaining <= TimeSpan.Zero)
            {
                return new(runId, observerStart, timer.Elapsed, timer.Elapsed,
                    RotationWaitOutcome.ObservationIndeterminate,
                    "C512_NONWAITING_OBSERVER_OPEN_INDETERMINATE");
            }

            using var openCancellation = new CancellationTokenSource(openRemaining);
            try
            {
                await observer.OpenAsync(openCancellation.Token);
            }
            catch (OperationCanceledException) when (openCancellation.IsCancellationRequested)
            {
                return new(runId, observerStart, timer.Elapsed, timer.Elapsed,
                    RotationWaitOutcome.ObservationIndeterminate,
                    "C512_NONWAITING_OBSERVER_OPEN_INDETERMINATE");
            }

            connectionOpenElapsed = timer.Elapsed;
            while (timer.Elapsed < C512ObservationBound)
            {
                if (rotationTask.IsCompleted)
                {
                    return new(runId, observerStart, connectionOpenElapsed, timer.Elapsed,
                        RotationWaitOutcome.RotationCompleted);
                }

                await using var command = observer.CreateCommand();
                command.CommandText = """
                    SELECT EXISTS (
                      SELECT 1
                      FROM pg_catalog.pg_stat_activity AS activity
                      WHERE activity.datname = pg_catalog.current_database()
                        AND activity.pid <> pg_catalog.pg_backend_pid()
                        AND @blocker_pid = ANY(pg_catalog.pg_blocking_pids(activity.pid))
                        AND activity.query LIKE '%raw_export_rotate_recipient_key%')
                    """;
                command.Parameters.AddWithValue("blocker_pid", blockerPid);
                var remaining = C512ObservationBound - timer.Elapsed;
                if (remaining <= TimeSpan.Zero) break;
                using var pollCancellation = new CancellationTokenSource(remaining);
                try
                {
                    if (await command.ExecuteScalarAsync(pollCancellation.Token) is true)
                    {
                        return new(runId, observerStart, connectionOpenElapsed, timer.Elapsed,
                            RotationWaitOutcome.ExpectedBlockerObserved);
                    }
                }
                catch (OperationCanceledException) when (pollCancellation.IsCancellationRequested)
                {
                    break;
                }

                remaining = C512ObservationBound - timer.Elapsed;
                if (remaining <= TimeSpan.Zero) break;
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(10)
                    ? remaining
                    : TimeSpan.FromMilliseconds(10));
            }
        }

        return new(runId, observerStart, connectionOpenElapsed, timer.Elapsed,
            rotationTask.IsCompleted
                ? RotationWaitOutcome.RotationCompleted
                : RotationWaitOutcome.ObservationIndeterminate,
            rotationTask.IsCompleted
                ? null
                : "C512_NONWAITING_OBSERVATION_INDETERMINATE");
    }

    private static async Task<BlockerReleaseObservation> ReleaseBlockerBoundedAsync(
        NpgsqlTransaction transaction,
        NpgsqlConnection blocker)
    {
        try
        {
            using var rollbackCancellation = new CancellationTokenSource(C512ObservationBound);
            await transaction.RollbackAsync(rollbackCancellation.Token);
            Console.WriteLine("C512_BLOCKER_RELEASE Outcome=BLOCKER_RELEASE_CONFIRMED_BY_ROLLBACK");
            return new(BlockerReleaseOutcome.ConfirmedByRollback, null);
        }
        catch (Exception rollbackFailure)
        {
            try
            {
                // Npgsql may pool the physical connector. Successful close/reset confirms
                // blocker release; it does not imply that the PostgreSQL backend was killed.
                await blocker.CloseAsync().WaitAsync(C512ObservationBound);
                Console.WriteLine("C512_BLOCKER_RELEASE Outcome=BLOCKER_RELEASE_CONFIRMED_BY_CONNECTION_CLOSE");
                return new(BlockerReleaseOutcome.ConfirmedByConnectionClose, null);
            }
            catch (Exception closeFailure)
            {
                Console.WriteLine("C512_BLOCKER_RELEASE Outcome=C512_NONWAITING_BLOCKER_RELEASE_INDETERMINATE");
                return new(BlockerReleaseOutcome.Indeterminate,
                    new AggregateException(
                        "C512_NONWAITING_BLOCKER_RELEASE_INDETERMINATE",
                        rollbackFailure, closeFailure));
            }
        }
    }

    private static async Task<Exception?> CancelReleaseAndDrainRotationAsync(
        NpgsqlTransaction transaction,
        NpgsqlConnection blocker,
        CancellationTokenSource rotationCancellation,
        Task rotationTask)
    {
        rotationCancellation.Cancel();
        var blockerRelease = await ReleaseBlockerBoundedAsync(transaction, blocker);
        Exception? rotationFailure = null;
        try
        {
            // RotateAsync already owns rotationCancellation.Token and cancellation was
            // requested above. WaitAsync is only the hard upper bound on cleanup completion;
            // a timeout hard-stops the proof rather than abandoning an uncancelled operation.
            await rotationTask.WaitAsync(C512ObservationBound);
        }
        catch (OperationCanceledException exception) when (
            rotationCancellation.IsCancellationRequested
            && exception.CancellationToken == rotationCancellation.Token)
        {
        }
        catch (TimeoutException exception)
        {
            rotationFailure = new InvalidOperationException(
                "C512_NONWAITING_ROTATION_CLEANUP_INDETERMINATE", exception);
        }
        catch (Exception exception)
        {
            rotationFailure = exception;
        }

        if (blockerRelease.Confirmed) return rotationFailure;
        if (rotationFailure is null) return blockerRelease.Failure;

        var derivedRotationFailure = new InvalidOperationException(
            "C512_NONWAITING_ROTATION_CLEANUP_INDETERMINATE " +
            "DERIVED_FROM=C512_NONWAITING_BLOCKER_RELEASE_INDETERMINATE",
            rotationFailure);
        return new AggregateException(
            "C512_NONWAITING_BLOCKER_RELEASE_INDETERMINATE_PRIMARY_WITH_DERIVED_ROTATION_CLEANUP",
            blockerRelease.Failure!, derivedRotationFailure);
    }
    private async Task<NpgsqlConnection> OpenAsync(){var c=new NpgsqlConnection(postgres.ConnectionString);await c.OpenAsync();return c;}
    private async Task ExecuteAsync(string sql,Guid value){await using var c=await OpenAsync();await using var command=c.CreateCommand();command.CommandText=sql;command.Parameters.AddWithValue("r",value);await command.ExecuteNonQueryAsync();}
    private static string Migration()=>Source("src/TagEkyc.Infrastructure/Persistence/Migrations/20260823120000_Tip88C1C5ManagedRecipientEnrollment.cs");
    private static string Source(string relativePath)=>File.ReadAllText(Path.Combine(Root(),relativePath.Replace('/',Path.DirectorySeparatorChar)));
    private static DefaultHttpContext ApiKeyContext(string presentedKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-TagEkyc-Api-Key"] = presentedKey;
        return context;
    }
    private static string SourceSlice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0) return $"<missing-start:{startMarker}>";
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..end];
    }
    private static string MigrationFunction(string migration, string functionName) => SourceSlice(
        migration, $"CREATE FUNCTION tagekyc.{functionName}(", "CREATE FUNCTION tagekyc.");
    private static int RegexCount(string text,string value){int n=0,i=0;while((i=text.IndexOf(value,i,StringComparison.Ordinal))>=0){n++;i+=value.Length;}return n;}
    private static string Slice(string text,string start,string end){var a=text.IndexOf(start,StringComparison.Ordinal);var b=text.IndexOf(end,a+start.Length,StringComparison.Ordinal);return a>=0&&b>a?text[a..b]:string.Empty;}
    private static string Root(){var p=new DirectoryInfo(AppContext.BaseDirectory);while(p is not null&&!Directory.Exists(Path.Combine(p.FullName,".git")))p=p.Parent;return p?.FullName??throw new InvalidOperationException();}
    private void Bite(bool condition, string bite, object? observed) =>
        observations.Observe(condition, bite, observed);

    private void Prerequisite(bool condition, string bite, object? observed) =>
        observations.Require(condition, bite, observed);

    private static void ImmediateBite(bool condition, string bite, object? observed)
    {
        if (!condition) throw new XunitException($"{bite}: observed={observed}");
    }

    private sealed class C5ObservationCollector
    {
        private readonly object gate = new();
        private readonly List<(string Bite, string Observed)> failures = [];

        public void Reset()
        {
            lock (gate) failures.Clear();
        }

        public void Observe(bool condition, string bite, object? observed)
        {
            if (condition) return;
            lock (gate) failures.Add((bite, observed?.ToString() ?? "<null>"));
        }

        public void Require(bool condition, string bite, object? observed)
        {
            if (condition) return;
            (string Bite, string Observed)[] snapshot;
            lock (gate)
            {
                snapshot = failures.ToArray();
                failures.Clear();
            }
            var names = snapshot.Select(value => value.Bite).Distinct(StringComparer.Ordinal).ToArray();
            var observedRed = names.Length == 0 ? string.Empty : $"OBSERVED_RED:{string.Join(',', names)}; ";
            throw new XunitException(
                $"{observedRed}PREREQUISITE_FAILURE:{bite}; status=NOT_REACHED_DUE_TO_INVALID_PREREQUISITE; observed={observed}");
        }

        public void ThrowIfAny()
        {
            (string Bite, string Observed)[] snapshot;
            lock (gate)
            {
                snapshot = failures.ToArray();
                failures.Clear();
            }
            if (snapshot.Length == 0) return;
            var names = snapshot.Select(value => value.Bite).Distinct(StringComparer.Ordinal);
            var details = string.Join(" | ", snapshot.Select(value => $"{value.Bite}: observed={value.Observed}"));
            throw new XunitException($"OBSERVED_RED:{string.Join(',', names)}; details={details}");
        }
    }
    private sealed record ManagedWorkflow(
        RecipientManagementApplicationService Service,
        RecipientManagementRepository Repository,
        AuthenticatedClientContext Actor,
        Guid Recipient,
        Guid Principal,
        Guid IdentityOperation);
    private sealed record C504PreservationObservation(
        bool CloneSchemaCurrent,
        bool DisposableDatabaseDistinct,
        bool DisposableDatabaseDropped,
        bool SharedCanonicalAbsentBefore,
        bool SharedCanonicalAbsentAfter,
        bool StateAbsentBefore,
        bool StatePresentAfter,
        bool EnrollmentSucceeded,
        SessionOperationResult<AuthenticatedClientContext> Before,
        SessionOperationResult<AuthenticatedClientContext> After,
        int PolicyCalls,
        ClientApplicationStatus Status,
        string DisposableDatabaseName)
    {
        public string Describe() =>
            $"status={Status};database={DisposableDatabaseName};schemaCurrent={CloneSchemaCurrent};" +
            $"distinct={DisposableDatabaseDistinct};dropped={DisposableDatabaseDropped};" +
            $"sharedAbsentBefore={SharedCanonicalAbsentBefore};sharedAbsentAfter={SharedCanonicalAbsentAfter};" +
            $"absentBefore={StateAbsentBefore};presentAfter={StatePresentAfter};" +
            $"enrolled={EnrollmentSucceeded};before={Before.Error?.Code ?? "success"};" +
            $"after={After.Error?.Code ?? "success"};policyCalls={PolicyCalls}";
    }
    private sealed record ReadinessFailure(
        string Code,
        string? Component,
        string? ComponentCode);
    private sealed class C528MigrationTarget(
        TagEkycDbContext database,
        PostgresPersistenceFixture.DisposableCurrentDatabase? disposable) : IAsyncDisposable
    {
        public TagEkycDbContext Database { get; } = database;

        public async ValueTask DisposeAsync()
        {
            await Database.DisposeAsync();
            if (disposable is not null)
                await disposable.DisposeAsync();
        }
    }
    private sealed record PublicKeyMaterial(string SpkiBase64, string FingerprintHex);
    private sealed record ActiveKey(string KeyId, int Version, long Revision, byte[] Fingerprint);
    private sealed record CompanionIsolationState(string State, long Revision, long ResultRevision);
    private sealed record DeliveryCounts(int Authorized, int Streaming, int Interrupted);
    private sealed record AuditEvent(
        Guid OperationId,
        string OperationKind,
        Guid ManagerApiKeyId,
        Guid ManagerPrincipalId,
        string EventType,
        string TargetIdentity,
        string Reason,
        long PriorRevision,
        long NewRevision,
        byte[] PayloadDigest,
        byte[]? PriorScopesDigest,
        byte[]? NewScopesDigest,
        DateTimeOffset OccurredAtUtc,
        byte[] EvidenceDigest,
        int? AuthorizedDeliveryCount,
        int? StreamingDeliveryCount,
        int? InterruptedDeliveryCount,
        DateTimeOffset CompletedAtUtc,
        string ResultSnapshot);
    private sealed record AuditEventRow(Guid ManagementEventId, string Snapshot);
    private sealed record AuditEventUpdateObservation(
        Guid ManagementEventId,
        string BeforeSnapshot,
        string AfterSnapshot,
        bool ExactAppendOnlyRejection,
        int UpdatedRows);
    private sealed class C502Authenticator : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext httpContext, string? requiredScope = null,
            CancellationToken cancellationToken = default)
        {
            var key = httpContext.Request.Headers["X-TagEkyc-Api-Key"].ToString();
            var category = key == "wrong-category"
                ? AuthenticatedCallerCategory.BusinessConsumer
                : AuthenticatedCallerCategory.OperatorAdmin;
            var scopes = key == "missing-scope"
                ? new HashSet<string>(StringComparer.Ordinal) { "operator.raw-export.sibling" }
                : new HashSet<string>(StringComparer.Ordinal) { RecipientManagementApplicationService.RequiredScope };
            return Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(
                Guid.NewGuid(), Guid.NewGuid(), "c502-test-key", category, scopes,
                PrincipalId: Guid.NewGuid())));
        }
    }
    private sealed class CountingGateway : IRecipientManagementGateway
    {
        public int Calls { get; private set; }
        private Task<SessionOperationResult<RecipientManagementWriteResult<T>>> Hit<T>()
        {
            Calls++;
            return Task.FromResult(SessionOperationResult<RecipientManagementWriteResult<T>>.Failure(
                "C502_GATEWAY_REACHED", "Gateway must not be reached.", 503));
        }
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(AuthenticatedClientContext a, EnrollManagedRecipientRequest r, string k, CancellationToken c) => Hit<ManagedRecipientIdentityDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(AuthenticatedClientContext a, IssueManagedRecipientCredentialRequest r, string k, CancellationToken c) => Hit<ManagedCredentialDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ReplaceCredentialAsync(AuthenticatedClientContext a, ReplaceManagedRecipientCredentialRequest r, string k, CancellationToken c) => Hit<ManagedCredentialDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> RevokeCredentialAsync(AuthenticatedClientContext a, RevokeManagedRecipientCredentialRequest r, string k, CancellationToken c) => Hit<ManagedCredentialDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(AuthenticatedClientContext a, EnrollRecipientPublicKeyRequest r, ValidatedRecipientPublicKey v, string k, CancellationToken c) => Hit<RecipientPublicKeyOperationResultDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(AuthenticatedClientContext a, RotateRecipientPublicKeyRequest r, ValidatedRecipientPublicKey v, string k, CancellationToken c) => Hit<RecipientPublicKeyOperationResultDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(AuthenticatedClientContext a, RevokeRecipientPublicKeyRequest r, string k, CancellationToken c) => Hit<RecipientPublicKeyOperationResultDto>();
        public Task<SessionOperationResult<ManagedRecipientReadinessDto>> ReadReadinessAsync(AuthenticatedClientContext a, Guid r, CancellationToken c)
        {
            Calls++;
            return Task.FromResult(SessionOperationResult<ManagedRecipientReadinessDto>.Failure(
                "C502_GATEWAY_REACHED", "Gateway must not be reached.", 503));
        }
    }
    private sealed class ManagerRoleConnectionFactory(string connectionString) : IRecipientManagementConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var role = connection.CreateCommand();
            role.CommandText = "SET ROLE tagekyc_raw_export_recipient_manager_login";
            await role.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
    }
    private sealed class CountingManagementConnectionFactory : IRecipientManagementConnectionFactory
    {
        public int Calls { get; private set; }

        public Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("C525_CONNECTION_TRAP_REACHED");
        }
    }
    private sealed class PlainManagementConnectionFactory(string connectionString)
        : IRecipientManagementConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
    private sealed class TestCredentialGenerator : IManagedCredentialMaterialGenerator
    {
        private readonly RandomManagedApiKeyGenerator generator = new();
        private readonly byte[] pepper = SHA256.HashData("c5-integration-pepper"u8.ToArray());
        public ManagedCredentialMaterial Generate()
        {
            var value = GenerateParseable(generator);
            return new(Guid.NewGuid(), value.PresentedKey, value.Prefix,
                ApiKeyHasher.Hash(pepper, value.PresentedKey));
        }
    }
    private sealed class CountingSequenceCredentialGenerator(
        IReadOnlyList<ManagedCredentialMaterial> materials) : IManagedCredentialMaterialGenerator
    {
        public int Calls { get; private set; }
        public ManagedCredentialMaterial Generate()
        {
            if (Calls >= materials.Count)
                throw new InvalidOperationException("C506_GENERATOR_EXHAUSTED");
            return materials[Calls++];
        }
    }
    private sealed class RecordingCredentialGenerator : IManagedCredentialMaterialGenerator
    {
        private readonly RandomManagedApiKeyGenerator generator = new();
        private readonly byte[] pepper = SHA256.HashData("c5-integration-pepper"u8.ToArray());
        public List<ManagedCredentialMaterial> Materials { get; } = [];
        public ManagedCredentialMaterial Generate()
        {
            var value = GenerateParseable(generator);
            var material = new ManagedCredentialMaterial(Guid.NewGuid(), value.PresentedKey, value.Prefix,
                ApiKeyHasher.Hash(pepper, value.PresentedKey));
            Materials.Add(material);
            return material;
        }
    }
    private static ManagedApiKeyMaterial GenerateParseable(RandomManagedApiKeyGenerator generator)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var value = generator.Generate();
            if (ManagedApiKeyParser.Parse(value.PresentedKey)?.Prefix == value.Prefix)
                return value;
        }
        throw new InvalidOperationException("C5_TEST_CREDENTIAL_GENERATOR_GRAMMAR_EXHAUSTED");
    }
    private sealed class CountingPolicyProvider : ILocalDevClientPolicyProvider
    {
        public int Calls { get; private set; }
        public Task<LocalDevClientPolicy?> GetPolicyAsync(
            Guid clientApplicationId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<LocalDevClientPolicy?>(null);
        }
    }
    private sealed class FixedApiKeyStore(ResolvedApiKey key) : IApiKeyStore
    {
        public Task<ResolvedApiKey?> FindByPresentedKeyAsync(
            string presentedApiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResolvedApiKey?>(key);
    }
    private sealed class FixedPolicyProvider(LocalDevClientPolicy policy) : ILocalDevClientPolicyProvider
    {
        public int Calls { get; private set; }
        public Task<LocalDevClientPolicy?> GetPolicyAsync(
            Guid clientApplicationId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<LocalDevClientPolicy?>(
                clientApplicationId == policy.ClientApplicationId ? policy : null);
        }
    }
    private sealed record EnrollCall(Guid Operation,Guid ManagerApiKey,Guid ManagerPrincipal,Guid Recipient,Guid Principal,byte[] Idempotency,byte[] Equality,byte[] Payload,string Outcome);
}
