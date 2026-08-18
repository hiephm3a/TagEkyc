using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2SourceFinalizationTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string R3Migration="20260810120000_Tip88C1B2R3VerifiedCiphertextStaging";
    private const string R4R6Migration="20260812120000_Tip88C1B2R4R6SourceFinalization";
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task F401_exact_Staged_winner_with_fresh_authority_commits_once_without_provider_operation()
    {
        AssertAbsoluteEvidenceVectors();
        var (source, staged)=await CreateStagedAsync("f401"u8.ToArray());
        await using var beforeDb=postgres.CreateDbContext();
        var headBefore=await beforeDb.RawExportSourceHeads.AsNoTracking().SingleAsync(x=>x.SourceArtifactId==source.SourceArtifactId);
        var objectBefore=await beforeDb.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==source.ObjectCustodyId);
        var keyBefore=await beforeDb.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==source.AttemptKeyReservationId);
        var committed=await CommitAsync(source,staged);
        Assert.Equal(SourceCommitDisposition.Committed, committed.Disposition);
        Assert.Equal(source.SourceArtifactId, committed.SourceArtifactId);
        Assert.Equal(source.ObjectCustodyId, committed.ObjectCustodyId);
        Assert.Equal(staged.ReservationRevision, committed.ReservationRevision);
        Assert.Equal(32, committed.CommitEvidenceDigest!.Length);
        await using var db=postgres.CreateDbContext();
        var row=await db.RawExportSourcePublications.AsNoTracking().SingleAsync(x=>x.SourcePublicationId==committed.SourcePublicationId);
        Assert.Equal("Committed",row.PublicationState); Assert.Equal("NotPlanned",row.CleanupDisposition); Assert.Equal(1,row.PublicationRevision);
        Assert.Equal(row.CommitEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeCommit(new(
            row.SourceArtifactId,row.AttemptId,row.ObjectCustodyId,row.AttemptKeyReservationId,row.StagedCiphertextFingerprintSchemaVersion,
            row.StagedCiphertextFingerprint,row.CommittedAuthoritySnapshotSchemaVersion,row.CommittedAuthoritySnapshotId,
            row.CommittedAuthorityRevision,row.CommittedConsentPolicyId,row.CommittedConsentPolicyVersion,row.CommittedAtUtc)));
        var headAfter=await db.RawExportSourceHeads.AsNoTracking().SingleAsync(x=>x.SourceArtifactId==source.SourceArtifactId);
        var objectAfter=await db.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==source.ObjectCustodyId);
        var keyAfter=await db.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==source.AttemptKeyReservationId);
        Assert.Equal(ScalarSnapshot(headBefore),ScalarSnapshot(headAfter));
        Assert.Equal(ScalarSnapshot(objectBefore),ScalarSnapshot(objectAfter));
        Assert.Equal(ScalarSnapshot(keyBefore),ScalarSnapshot(keyAfter));
    }

    [Fact]
    public async Task F402_R4_identity_revision_fence_fingerprint_and_fresh_authority_barriers_are_exact()
    {
        var (source,staged)=await CreateStagedAsync("f402"u8.ToArray());
        Assert.Equal(SourceCommitDisposition.StateConflict,(await CommitAsync(source,staged,expectedReservationRevision:staged.ReservationRevision+1)).Disposition);
        Assert.Equal(SourceCommitDisposition.StateConflict,(await CommitAsync(source,staged,expectedAttemptRevision:source.EncryptionAttemptRevision+1)).Disposition);
        Assert.Equal(SourceCommitDisposition.StateConflict,(await CommitAsync(source,staged,expectedFence:source.Fence+1)).Disposition);
        Assert.Equal(SourceCommitDisposition.StateConflict,(await CommitAsync(source,staged,expectedObjectRevision:source.ObjectStateRevision+1)).Disposition);
        await using(var missingDb=postgres.CreateDbContext()) Assert.Equal(SourceCommitDisposition.NotFound,(await new RawExportSourceFinalizationService(missingDb).CommitAsync(
            new(source.ActorPrincipalId,Guid.NewGuid(),staged.ReservationRevision!.Value,source.EncryptionAttemptRevision,source.Fence,source.ObjectStateRevision))).Disposition);
        await WithdrawAuthorityAsync(source);
        Assert.Equal(SourceCommitDisposition.SourceRetentionNotAuthorized,(await CommitAsync(source,staged)).Disposition);
        await using var db=postgres.CreateDbContext(); Assert.Empty(await db.RawExportSourcePublications.ToListAsync());
        var definition=NormalizeSql(await FunctionDefinitionAsync("tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)"));
        var requiredBarriers=new[]
        {
            "h.\"CurrentEncryptionAttemptId\"<>a.\"AttemptId\" OR h.\"Fence\"<>a.\"Fence\"",
            "k.\"AttemptId\"<>a.\"AttemptId\" OR k.\"AttemptKeyReservationId\"<>a.\"AttemptKeyReservationId\"",
            "k.\"EncryptionAttemptFingerprint\" IS DISTINCT FROM a.\"EncryptionAttemptFingerprint\"",
            "o.\"AttemptId\"<>a.\"AttemptId\" OR o.\"AttemptKeyReservationId\"<>a.\"AttemptKeyReservationId\"",
            "o.\"SourceArtifactId\"<>a.\"SourceArtifactId\" OR o.\"ProvisionalObjectIdentity\"<>a.\"ProvisionalObjectIdentity\"",
            "o.\"EncryptionAttemptRevision\"<>a.\"EncryptionAttemptRevision\" OR o.\"AttemptFence\"<>a.\"Fence\"",
            "o.\"EncryptionAttemptFingerprint\" IS DISTINCT FROM a.\"EncryptionAttemptFingerprint\"",
            "o.\"State\"<>'VerifiedCompleted' OR o.\"StateRevision\"<>a.\"StagedObjectStateRevision\"",
            "a.\"VerifiedPlaintextLength\"<>r.\"ClaimedPlaintextLength\"",
            "a.\"StagedCiphertextLength\"<>o.\"CiphertextLength\"",
            "a.\"StagedCiphertextDigest\" IS DISTINCT FROM o.\"CiphertextDigest\"",
            "a.\"StagedProviderReceiptDigest\" IS DISTINCT FROM o.\"ProviderReceiptDigest\"",
            "a.\"StagedVerificationEvidenceDigest\" IS DISTINCT FROM o.\"VerificationEvidenceDigest\"",
            "authority.\"AuthoritySnapshotSchemaVersion\"<>r.\"AuthoritySnapshotSchemaVersion\" OR authority.\"AuthoritySnapshotId\"<>r.\"AuthoritySnapshotId\"",
            "authority.\"ControllerIdentity\"<>r.\"ControllerIdentity\" OR authority.\"StableDataScopeId\"<>r.\"StableDataScopeId\"",
            "authority.\"ConsentPolicyId\"<>r.\"ConsentPolicyId\" OR authority.\"ConsentPolicyVersion\"<>r.\"ConsentPolicyVersion\"",
            "consent.\"State\"<>'Effective' OR consent.\"VerificationSessionId\"<>c.\"VerificationSessionId\"",
            "consent.\"RecipientClientApplicationId\"<>c.\"ClientApplicationId\" OR consent.\"RawClass\"<>c.\"RawClass\"",
            "consent.\"ValidFromUtc\" IS NULL OR consent.\"ValidFromUtc\">now_ OR (consent.\"ValidUntilUtc\" IS NOT NULL AND now_>=consent.\"ValidUntilUtc\")",
            "now_>=r.\"ReservationExpiresAtUtc\" OR now_>=r.\"AbsoluteSourceExpiresAtUtc\""
        };
        Assert.All(requiredBarriers,barrier=>Assert.Contains(NormalizeSql(barrier),definition,StringComparison.Ordinal));
        var onConflictBranch=MigrationText().Split("ON CONFLICT DO NOTHING;",StringSplitOptions.None)[1].Split("RETURN QUERY SELECT 'Committed'",StringSplitOptions.None)[0];
        const string restore="set_config('tagekyc.raw_export_source_finalization_write_context',COALESCE(prior,''),true)";
        Assert.Equal(3,onConflictBranch.Split(restore,StringSplitOptions.None).Length-1);
        await AssertCommitExpiresWhileBlockedAsync(true);
        await AssertCommitExpiresWhileBlockedAsync(false);
    }

    [Fact]
    public void F403_R4_R5_have_no_S3_key_provider_AEAD_plaintext_or_external_service_edge()
    {
        var fields=typeof(RawExportSourceFinalizationService).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        Assert.All(fields,f=>Assert.Equal(typeof(TagEkycDbContext),f.FieldType));
    }

    [Fact]
    public async Task F404_fresh_authority_consent_and_deadlines_publish_exact_Available_shape()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f404"u8.ToArray());
        var published=await PublishAsync(source,staged,committed);
        Assert.Equal(SourcePublishDisposition.Available,published.Disposition); Assert.NotEqual(Guid.Empty,published.OpaqueCommittedLocatorId); Assert.Equal(32,published.AvailableEvidenceDigest!.Length);
        await using var db=postgres.CreateDbContext(); var row=await db.RawExportSourcePublications.AsNoTracking().SingleAsync(); var head=await db.RawExportSourceHeads.AsNoTracking().SingleAsync(x=>x.SourceArtifactId==source.SourceArtifactId);
        var reservation=await db.RawExportSourceReservations.AsNoTracking().SingleAsync(x=>x.SourceArtifactId==source.SourceArtifactId);
        var attempt=await db.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync(x=>x.AttemptId==source.AttemptId);
        Assert.Equal("Available",row.PublicationState); Assert.Equal("NoObsoleteResidue",row.CleanupDisposition); Assert.Equal("Available",head.CustodyState);
        Assert.Equal((source.SourceArtifactId,source.AttemptId,source.ObjectCustodyId,source.AttemptKeyReservationId),
            (row.SourceArtifactId,row.AttemptId,row.ObjectCustodyId,row.AttemptKeyReservationId));
        Assert.Equal(2,row.StagedCiphertextFingerprintSchemaVersion);Assert.Equal(attempt.StagedCiphertextFingerprint,row.StagedCiphertextFingerprint);
        Assert.Equal((reservation.AuthoritySnapshotSchemaVersion,reservation.AuthoritySnapshotId,source.AuthorityRevision,reservation.ConsentPolicyId,reservation.ConsentPolicyVersion),
            (row.PublishedAuthoritySnapshotSchemaVersion,row.PublishedAuthoritySnapshotId,row.PublishedAuthorityRevision,row.PublishedConsentPolicyId,row.PublishedConsentPolicyVersion));
        Assert.Equal(2,row.PublicationRevision);Assert.NotNull(row.FinalizedAtUtc);Assert.Empty(await db.RawExportSourceCleanupItems.ToListAsync());
        Assert.Equal(row.AvailableEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeAvailable(new(
            row.SourcePublicationId,row.SourceArtifactId,row.AttemptId,row.ObjectCustodyId,row.AttemptKeyReservationId,row.OpaqueCommittedLocatorId!.Value,
            row.CommitEvidenceDigest,row.PublishedAuthoritySnapshotSchemaVersion!.Value,row.PublishedAuthoritySnapshotId!.Value,
             row.PublishedAuthorityRevision!.Value,row.PublishedConsentPolicyId!.Value,row.PublishedConsentPolicyVersion!.Value,row.AvailableAtUtc!.Value)));
        Assert.Equal(row.CleanupEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeCleanup(new(
            row.SourcePublicationId,row.SourceArtifactId,"NoObsoleteResidue",0,row.FinalizedAtUtc!.Value)));
    }

    [Fact]
    public async Task F405_R5_post_lock_admission_time_publication_shape_and_precedence_are_exact()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f405"u8.ToArray());
        await WithdrawAuthorityAsync(source);
        Assert.Equal(SourcePublishDisposition.StateConflict,(await PublishAsync(source,staged,committed,staged.ReservationRevision!.Value+1)).Disposition);
        Assert.Equal(SourcePublishDisposition.SourceRetentionNotAuthorized,(await PublishAsync(source,staged,committed)).Disposition);
        await using var db=postgres.CreateDbContext(); var row=await db.RawExportSourcePublications.AsNoTracking().SingleAsync();
        Assert.Equal("Committed",row.PublicationState); Assert.Null(row.OpaqueCommittedLocatorId); Assert.Empty(await db.RawExportSourceCleanupItems.ToListAsync());
        await AssertPublishExpiresWhileBlockedAsync(true);
        await AssertPublishExpiresWhileBlockedAsync(false);
        var (shapeSource,shapeStaged,shapeCommitted)=await CreateCommittedAsync("f405-shape"u8.ToArray());
        var shapePublished=await PublishAsync(shapeSource,shapeStaged,shapeCommitted);
        await AssertShapeCheckAsync("raw_export_source_publications","tr_raw_export_source_publication_guard","UPDATE tagekyc.raw_export_source_publications SET \"CleanupDisposition\"='NotPlanned' WHERE \"SourcePublicationId\"=@id",shapePublished.SourcePublicationId!.Value,"ck_raw_export_source_publication_shape");
    }

    [Fact]
    public async Task F406_Committed_is_non_readable_and_only_Available_is_descriptor_eligible()
    {
        var (_,_,committed)=await CreateCommittedAsync("f406"u8.ToArray());
        await using var db=postgres.CreateDbContext(); var row=await db.RawExportSourcePublications.AsNoTracking().SingleAsync(x=>x.SourcePublicationId==committed.SourcePublicationId);
        Assert.Equal("Committed",row.PublicationState); Assert.Null(row.OpaqueCommittedLocatorId); Assert.Null(row.AvailableEvidenceDigest);
    }

    [Fact]
    public async Task F407_opaque_locator_and_results_expose_no_bucket_key_provider_or_secret()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f407"u8.ToArray()); var result=await PublishAsync(source,staged,committed);
        var text=result.ToString(); Assert.Contains("<redacted>",text); Assert.DoesNotContain(result.OpaqueCommittedLocatorId!.Value.ToString("N"),text);
        Assert.Contains("<redacted>",committed.ToString());Assert.DoesNotContain(Convert.ToHexString(committed.CommitEvidenceDigest!),committed.ToString(),StringComparison.OrdinalIgnoreCase);
        var secret=RandomNumberGenerator.GetBytes(32);var id=Guid.NewGuid();
        var values=new object[]{
            new SourceCleanupReadResult(SourceCleanupReadDisposition.ItemAvailable,id,2,id,"ProvisionalObject",id,id,1,1),
            new SourceCleanupCompleteResult(SourceCleanupCompleteDisposition.Completed,id,2,id,"ProvisionalObject",id,"Deleted",secret,DateTimeOffset.UtcNow,2),
            new SourceCleanupFinalizeResult(SourceCleanupFinalizeDisposition.Completed,id,3,"Completed",secret,DateTimeOffset.UtcNow)};
        Assert.All(values,value=>{var rendered=value.ToString()!;Assert.Contains("<redacted>",rendered);Assert.DoesNotContain(id.ToString("N"),rendered);Assert.DoesNotContain(Convert.ToHexString(secret),rendered,StringComparison.OrdinalIgnoreCase);});
    }

    [Fact]
    public async Task F408_R3_R4_R5_response_loss_replays_are_exact_zero_write_and_deadlock_free()
    {
        var (source,staged)=await CreateStagedAsync("f408"u8.ToArray());
        var r3Replay=StageReplayAsync(source); var r4Commit=CommitAsync(source,staged);
        await Task.WhenAll(r3Replay,r4Commit).WaitAsync(TimeSpan.FromSeconds(20));
        var r3ReplayResult=await r3Replay; var committed=await r4Commit;
        Assert.Equal(RawExportR3StageDisposition.ExistingMatch,r3ReplayResult.Disposition);
        Assert.Equal(SourceCommitDisposition.Committed,committed.Disposition);
        var r3AfterCommit=StageReplayAsync(source);var r4AfterCommit=CommitAsync(source,staged);
        await Task.WhenAll(r3AfterCommit,r4AfterCommit).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(RawExportR3StageDisposition.ExistingMatch,(await r3AfterCommit).Disposition);
        var exactR4Replay=await r4AfterCommit;
        Assert.Equal(SourceCommitDisposition.ExistingMatch,exactR4Replay.Disposition);
        Assert.Equal(staged.ReservationRevision,exactR4Replay.ReservationRevision);

        var obsoleteForAttemptOrder=await AddObsoleteVerifiedAttemptAsync(source);
        var headBlocker=await BeginRowBlockAsync("f408-head-blocker","raw_export_source_head","SourceArtifactId",source.SourceArtifactId);
        await using(headBlocker.Connection) await using(headBlocker.Transaction)
        {
            var r3ReplayAtPublish=StageReplayAsync(source,"f408-r3-replay");
            await WaitForBlockedByAsync("f408-r3-replay","f408-head-blocker");
            var r5Publish=PublishAsync(source,staged,committed,applicationName:"f408-r5-publish");
            await WaitForBlockedByAsync("f408-r5-publish","f408-r3-replay");
            var attemptWaiter=await BeginRowBlockAsync("f408-attempt-waiter","raw_export_source_encryption_attempts","AttemptId",obsoleteForAttemptOrder.AttemptId,waitForLock:false);
            await using(attemptWaiter.Connection) await using(attemptWaiter.Transaction)
            {
                await WaitForBlockedByAsync("f408-attempt-waiter","f408-r5-publish");
                await headBlocker.Transaction.CommitAsync();
                await Task.WhenAll(r3ReplayAtPublish,r5Publish).WaitAsync(TimeSpan.FromSeconds(20));
                var r3AtPublishResult=await r3ReplayAtPublish; var published=await r5Publish;
                Assert.Equal(RawExportR3StageDisposition.ExistingMatch,r3AtPublishResult.Disposition);
                Assert.Equal(SourcePublishDisposition.Available,published.Disposition);
                await attemptWaiter.PendingLock!.WaitAsync(TimeSpan.FromSeconds(20));
                await attemptWaiter.Transaction.CommitAsync();

                var r4Replay=CommitAsync(source,staged); var r5Replay=PublishAsync(source,staged,committed);
                await Task.WhenAll(r4Replay,r5Replay).WaitAsync(TimeSpan.FromSeconds(20));
                var r4ReplayResult=await r4Replay; var r5ReplayResult=await r5Replay;
                Assert.Equal(SourceCommitDisposition.ExistingMatch,r4ReplayResult.Disposition);
                Assert.Equal(staged.ReservationRevision,r4ReplayResult.ReservationRevision);
                Assert.Equal(SourceCommitDisposition.StateConflict,(await CommitAsync(source,staged,staged.ReservationRevision!.Value+1)).Disposition);
                Assert.Equal(SourcePublishDisposition.ExistingMatch,r5ReplayResult.Disposition);
                Assert.Equal(published.OpaqueCommittedLocatorId,r5ReplayResult.OpaqueCommittedLocatorId);
            }
        }
        await using var db=postgres.CreateDbContext(); Assert.Equal(1,await db.RawExportSourcePublications.CountAsync());
        var (raceSource,raceStaged)=await CreateStagedAsync("f408-first-commit"u8.ToArray());
        var race=await Task.WhenAll(CommitAsync(raceSource,raceStaged),CommitAsync(raceSource,raceStaged)).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Contains(race,x=>x.Disposition==SourceCommitDisposition.Committed);
        Assert.Contains(race,x=>x.Disposition==SourceCommitDisposition.ExistingMatch);
        Assert.Equal(1,race.Count(x=>x.Disposition==SourceCommitDisposition.Committed));

        var (publishRaceSource,publishRaceStaged,publishRaceCommitted)=await CreateCommittedAsync("f408-r4-r5"u8.ToArray());
        var r4DuringPublish=CommitAsync(publishRaceSource,publishRaceStaged);
        var r5DuringReplay=PublishAsync(publishRaceSource,publishRaceStaged,publishRaceCommitted);
        await Task.WhenAll(r4DuringPublish,r5DuringReplay).WaitAsync(TimeSpan.FromSeconds(20));
        var r4DuringPublishResult=await r4DuringPublish;
        Assert.Equal(SourceCommitDisposition.ExistingMatch,r4DuringPublishResult.Disposition);
        Assert.Equal(publishRaceStaged.ReservationRevision,r4DuringPublishResult.ReservationRevision);
        Assert.Equal(SourcePublishDisposition.Available,(await r5DuringReplay).Disposition);

        var (orderSource,orderStaged,orderCommitted)=await CreateCommittedAsync("f408-key-object-order"u8.ToArray());
        var objectBlocker=await BeginRowBlockAsync("f408-object-blocker","raw_export_provisional_objects","ObjectCustodyId",orderSource.ObjectCustodyId);
        await using(objectBlocker.Connection) await using(objectBlocker.Transaction)
        {
            var orderedPublish=PublishAsync(orderSource,orderStaged,orderCommitted,applicationName:"f408-order-r5");
            await WaitForBlockedByAsync("f408-order-r5","f408-object-blocker");
            var keyWaiter=await BeginRowBlockAsync("f408-key-waiter","raw_export_attempt_key_reservations","AttemptKeyReservationId",orderSource.AttemptKeyReservationId,waitForLock:false);
            await using(keyWaiter.Connection) await using(keyWaiter.Transaction)
            {
                await WaitForBlockedByAsync("f408-key-waiter","f408-order-r5");
                await objectBlocker.Transaction.CommitAsync();
                Assert.Equal(SourcePublishDisposition.Available,(await orderedPublish.WaitAsync(TimeSpan.FromSeconds(20))).Disposition);
                await keyWaiter.PendingLock!.WaitAsync(TimeSpan.FromSeconds(20));
                await keyWaiter.Transaction.CommitAsync();
            }
        }
    }

    [Fact]
    public async Task F409_R6_cleanup_plan_excludes_winning_object_and_key_under_every_census()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f409"u8.ToArray());
        var obsolete=await AddObsoleteVerifiedAttemptAsync(source);
        var published=await PublishAsync(source,staged,committed);
        Assert.Equal("Pending",published.CleanupDisposition);
        await using var db=postgres.CreateDbContext(); var items=await db.RawExportSourceCleanupItems.AsNoTracking().OrderBy(x=>x.ResourceKind).ToListAsync();
        Assert.Equal(2,items.Count);
        Assert.Contains(items,x=>x.ResourceKind=="ProvisionalObject"&&x.ResourceId==obsolete.ObjectCustodyId&&x.ResourceAttemptId==obsolete.AttemptId);
        Assert.Contains(items,x=>x.ResourceKind=="AttemptKeyReservation"&&x.ResourceId==obsolete.AttemptKeyReservationId&&x.ResourceAttemptId==obsolete.AttemptId);
        Assert.Equal(obsolete.ObjectCustodyId,obsolete.AttemptKeyReservationId);
        Assert.Equal(2,items.Select(x=>x.CleanupItemId).Distinct().Count());
        Assert.All(items,x=>Assert.NotEqual(x.ResourceId,x.CleanupItemId));
        Assert.DoesNotContain(items,x=>x.ResourceId==source.ObjectCustodyId||x.ResourceId==source.AttemptKeyReservationId);
        var publishDefinition=NormalizeSql(await FunctionDefinitionAsync("tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)"));
        Assert.Contains("x.\"State\" IN ('PutInFlight','PutOutcomeUnknown','ObjectPresentPendingVerification','VerifiedCompleted','ObjectConflict','CleanupPending','Deleted','Quarantined')",publishDefinition,StringComparison.Ordinal);
        Assert.DoesNotContain("'Initiated'",publishDefinition,StringComparison.Ordinal);
        Assert.DoesNotContain("'NoObjectEstablished'",publishDefinition,StringComparison.Ordinal);
        Assert.Contains("x.\"AttemptKeyReservationId\"<>p.\"AttemptKeyReservationId\" AND NOT (x.\"PreparationDisposition\"='ReadyForFreshPreparation' AND x.\"WrappedDekCiphertext\" IS NULL AND x.\"CurrentProviderOperationToken\" IS NULL)",publishDefinition,StringComparison.Ordinal);

        var winnerObject=await db.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==source.ObjectCustodyId);
        var injectedObject=Guid.ParseExact("00000000000000000000000000000001","N");
        await InjectCleanupItemAsync(injectedObject,published.SourcePublicationId!.Value,"ProvisionalObject",source.ObjectCustodyId,source.AttemptId,winnerObject.StateRevision);
        var objectLifecycle=new FixedLifecycle(ExactDeleteOutcome.DeletedAcknowledged,204);
        await using(var objectGuardDb=postgres.CreateDbContext())
            Assert.Equal(SourceCleanupCompleteDisposition.StateConflict,(await new RawExportSourceCleanupService(objectGuardDb).SettleNextAsync(
                new(source.ActorPrincipalId,published.SourcePublicationId.Value,2),objectLifecycle)).Disposition);
        Assert.False(objectLifecycle.Called);
        await RemoveInjectedCleanupItemAsync(injectedObject);

        var winnerKey=await db.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==source.AttemptKeyReservationId);
        var injectedKey=Guid.ParseExact("00000000000000000000000000000001","N");
        await InjectCleanupItemAsync(injectedKey,published.SourcePublicationId.Value,"AttemptKeyReservation",source.AttemptKeyReservationId,source.AttemptId,winnerKey.RowRevision);
        await using(var keyGuardDb=postgres.CreateDbContext())
            Assert.Equal(SourceCleanupCompleteDisposition.StateConflict,(await new RawExportSourceCleanupService(keyGuardDb).SettleNextAsync(
                new(source.ActorPrincipalId,published.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.DeletedAcknowledged,204))).Disposition);
        await RemoveInjectedCleanupItemAsync(injectedKey);

        await using var verify=postgres.CreateDbContext();
        Assert.Equal("VerifiedCompleted",(await verify.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==source.ObjectCustodyId)).State);
        Assert.Equal("Active",(await verify.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==source.AttemptKeyReservationId)).PreparationDisposition);
    }

    [Fact]
    public async Task F410_Available_remains_stable_while_cleanup_is_pending_or_retried()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f410"u8.ToArray()); var obsolete=await AddObsoleteVerifiedAttemptAsync(source); await PublishAsync(source,staged,committed);
        if(await NextPendingKindAsync(committed.SourcePublicationId!.Value)=="AttemptKeyReservation")
        {
            await using var keyFirst=postgres.CreateDbContext();
            Assert.Equal(SourceCleanupCompleteDisposition.Completed,(await new RawExportSourceCleanupService(keyFirst).SettleNextAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.ProviderUnavailable,null))).Disposition);
        }
        await using(var read=postgres.CreateDbContext())
        {
            var unavailable=new FixedLifecycle(ExactDeleteOutcome.ProviderUnavailable,null);
            var result=await new RawExportSourceCleanupService(read).SettleNextAsync(new(source.ActorPrincipalId,committed.SourcePublicationId!.Value,2),unavailable);
            Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,result.Disposition); Assert.True(unavailable.Called);
        }
        await using var db=postgres.CreateDbContext(); var row=await db.RawExportSourcePublications.AsNoTracking().SingleAsync(); var pending=await db.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(x=>x.ResourceKind=="ProvisionalObject");
        Assert.Equal("Available",row.PublicationState); Assert.Equal("Pending",row.CleanupDisposition); Assert.Equal("Pending",pending.CleanupState);
        await using(var retryDb=postgres.CreateDbContext())
        {
            var unknown=new FixedLifecycle(ExactDeleteOutcome.OutcomeUnknown,null);
            var retry=await new RawExportSourceCleanupService(retryDb).SettleNextAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId!.Value,2),unknown);
            Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,retry.Disposition);
            Assert.True(unknown.Called);
        }
        await using var afterDb=postgres.CreateDbContext();
        Assert.Equal("CleanupPending",(await afterDb.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==obsolete.ObjectCustodyId)).State);
        Assert.Equal("Pending",(await afterDb.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(x=>x.ResourceKind=="ProvisionalObject")).CleanupState);
        Assert.Equal("Pending",(await afterDb.RawExportSourcePublications.AsNoTracking().SingleAsync()).CleanupDisposition);
    }

    [Fact]
    public async Task F411_R6_completes_only_from_durable_exact_object_and_key_terminal_evidence()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f411"u8.ToArray()); var obsolete=await AddObsoleteVerifiedAttemptAsync(source); var published=await PublishAsync(source,staged,committed);
        Assert.Equal("Pending",published.CleanupDisposition);
        SourceCleanupCompleteResult? objectCompleted=null;SourceCleanupCompleteResult? keyCompleted=null;
        while(objectCompleted is null||keyCompleted is null)
        {
            var kind=await NextPendingKindAsync(committed.SourcePublicationId!.Value);
            await using var settleDb=postgres.CreateDbContext();
            var completed=await new RawExportSourceCleanupService(settleDb).SettleNextAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.DeletedAcknowledged,204));
            Assert.Equal(SourceCleanupCompleteDisposition.Completed,completed.Disposition);Assert.Equal(32,completed.CleanupEvidenceDigest!.Length);
            if(kind=="ProvisionalObject")
            {
                objectCompleted=completed;Assert.Equal("Deleted",completed.CompletionDisposition);
                await using var evidenceDb=postgres.CreateDbContext();var deleted=await evidenceDb.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==obsolete.ObjectCustodyId);
                Assert.Equal(completed.CleanupEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeObjectItem(new(
                    committed.SourcePublicationId.Value,obsolete.AttemptId,obsolete.ObjectCustodyId,"Deleted",deleted.DeletionEvidenceDigest!,completed.CompletedAtUtc!.Value)));
            }
            else
            {
                Assert.Equal("AttemptKeyReservation",kind);keyCompleted=completed;Assert.Equal("Revoked",completed.CompletionDisposition);
                await using var evidenceDb=postgres.CreateDbContext();var revokedEvent=await evidenceDb.RawExportAttemptKeyPreparationEvents.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==obsolete.AttemptKeyReservationId&&x.EventKind=="Revoked");
                Assert.Equal(completed.CleanupEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeKeyItem(new(
                    committed.SourcePublicationId.Value,obsolete.AttemptId,obsolete.AttemptKeyReservationId,"Revoked",revokedEvent.RevocationEvidenceDigest!,completed.CompletedAtUtc!.Value)));
            }
            await using var replayDb=postgres.CreateDbContext();var replayService=new RawExportSourceCleanupService(replayDb);
            var itemReplay=await replayService.CompleteAsync(new(source.ActorPrincipalId,completed.CleanupItemId!.Value,1));
            Assert.Equal(SourceCleanupCompleteDisposition.ExistingMatch,itemReplay.Disposition);Assert.Equal(completed.CleanupEvidenceDigest,itemReplay.CleanupEvidenceDigest);
            Assert.Equal(SourceCleanupCompleteDisposition.StateConflict,(await replayService.CompleteAsync(new(source.ActorPrincipalId,completed.CleanupItemId.Value,2))).Disposition);
        }
        await using var db=postgres.CreateDbContext(); var service=new RawExportSourceCleanupService(db); var result=await service.FinalizeAsync(new(source.ActorPrincipalId,committed.SourcePublicationId!.Value,2));
        Assert.Equal(SourceCleanupFinalizeDisposition.Completed,result.Disposition); Assert.Equal(32,result.CleanupEvidenceDigest!.Length);
        var finalPublication=await db.RawExportSourcePublications.AsNoTracking().SingleAsync();
        Assert.Equal(result.CleanupEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeCleanup(new(
            finalPublication.SourcePublicationId,finalPublication.SourceArtifactId,"Completed",2,result.FinalizedAtUtc!.Value)));
        var replay=await service.FinalizeAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2)); Assert.Equal(SourceCleanupFinalizeDisposition.ExistingMatch,replay.Disposition); Assert.Equal(result.CleanupEvidenceDigest,replay.CleanupEvidenceDigest);
        var winnerObject=await db.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==source.ObjectCustodyId); var winnerKey=await db.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==source.AttemptKeyReservationId);
        Assert.Equal("VerifiedCompleted",winnerObject.State); Assert.Equal("Active",winnerKey.PreparationDisposition);
        var revoked=await db.RawExportAttemptKeyPreparationEvents.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==obsolete.AttemptKeyReservationId&&x.EventKind=="Revoked");
        Assert.Equal("SourceFinalizationSupersededAttempt",revoked.RevocationReasonCode); Assert.Equal(32,revoked.RevocationEvidenceDigest!.Length);
    }

    [Fact]
    public async Task F412_function_catalog_owner_ACL_roles_memberships_and_table_privileges_are_exact()
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            SELECT count(*) FROM pg_catalog.pg_proc p WHERE p.oid=ANY(ARRAY[
              'tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_finalize_source_cleanup(uuid,bigint)'::pg_catalog.regprocedure])
              AND pg_catalog.pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer'
              AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
            """,connection);
        Assert.Equal(5L,(long)(await command.ExecuteScalarAsync())!);
        var expectedResults=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["raw_export_commit_staged_source"]="Outcome:text|SourcePublicationId:uuid|SourceArtifactId:uuid|AttemptId:uuid|ObjectCustodyId:uuid|CommitEvidenceDigest:bytea|CommittedAtUtc:timestamp with time zone|ReservationRevision:bigint|Fence:bigint",
            ["raw_export_publish_available_source"]="Outcome:text|SourcePublicationId:uuid|SourceArtifactId:uuid|OpaqueCommittedLocatorId:uuid|AvailableEvidenceDigest:bytea|ReservationRevision:bigint|Fence:bigint|AvailableAtUtc:timestamp with time zone|CleanupDisposition:text",
            ["raw_export_read_next_source_cleanup_item"]="Outcome:text|SourcePublicationId:uuid|PublicationRevision:bigint|CleanupItemId:uuid|ResourceKind:text|ResourceId:uuid|ResourceAttemptId:uuid|CleanupItemRevision:bigint|PlannedResourceRevision:bigint",
            ["raw_export_complete_source_cleanup_item"]="Outcome:text|SourcePublicationId:uuid|PublicationRevision:bigint|CleanupItemId:uuid|ResourceKind:text|ResourceId:uuid|CompletionDisposition:text|CleanupEvidenceDigest:bytea|CompletedAtUtc:timestamp with time zone|CleanupItemRevision:bigint",
            ["raw_export_finalize_source_cleanup"]="Outcome:text|SourcePublicationId:uuid|PublicationRevision:bigint|CleanupDisposition:text|CleanupEvidenceDigest:bytea|FinalizedAtUtc:timestamp with time zone"
        };
        await using(var resultShape=new NpgsqlCommand("""
            SELECT p.proname,pg_catalog.string_agg(a.name||':'||pg_catalog.format_type(a.type,NULL),'|' ORDER BY a.ordinality)
            FROM pg_catalog.pg_proc p
            CROSS JOIN LATERAL ROWS FROM (
              pg_catalog.unnest(p.proargnames),pg_catalog.unnest(p.proallargtypes),pg_catalog.unnest(p.proargmodes))
              WITH ORDINALITY AS a(name,type,mode,ordinality)
            WHERE p.oid=ANY(ARRAY[
              'tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_finalize_source_cleanup(uuid,bigint)'::pg_catalog.regprocedure]) AND a.mode='t'
            GROUP BY p.proname
            """,connection))
        await using(var reader=await resultShape.ExecuteReaderAsync())
        {
            var observed=new Dictionary<string,string>(StringComparer.Ordinal);
            while(await reader.ReadAsync()) observed.Add(reader.GetString(0),reader.GetString(1));
            Assert.Equal(expectedResults.OrderBy(x=>x.Key),observed.OrderBy(x=>x.Key));
        }
        var expectedInputs=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["raw_export_commit_staged_source"]="p_attempt_id:uuid|p_expected_reservation_revision:bigint|p_expected_attempt_revision:bigint|p_expected_fence:bigint|p_expected_object_state_revision:bigint",
            ["raw_export_publish_available_source"]="p_source_publication_id:uuid|p_expected_reservation_revision:bigint|p_expected_fence:bigint",
            ["raw_export_read_next_source_cleanup_item"]="p_source_publication_id:uuid|p_expected_publication_revision:bigint",
            ["raw_export_complete_source_cleanup_item"]="p_cleanup_item_id:uuid|p_expected_cleanup_item_revision:bigint",
            ["raw_export_finalize_source_cleanup"]="p_source_publication_id:uuid|p_expected_publication_revision:bigint"
        };
        await using(var inputShape=new NpgsqlCommand("""
            SELECT p.proname,pg_catalog.string_agg(a.name||':'||pg_catalog.format_type(a.type,NULL),'|' ORDER BY a.ordinality)
            FROM pg_catalog.pg_proc p
            CROSS JOIN LATERAL ROWS FROM (
              pg_catalog.unnest(p.proargnames),pg_catalog.unnest(p.proallargtypes),pg_catalog.unnest(p.proargmodes))
              WITH ORDINALITY AS a(name,type,mode,ordinality)
            WHERE p.oid=ANY(ARRAY[
              'tagekyc.raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_publish_available_source(uuid,bigint,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_read_next_source_cleanup_item(uuid,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_complete_source_cleanup_item(uuid,bigint)'::pg_catalog.regprocedure,
              'tagekyc.raw_export_finalize_source_cleanup(uuid,bigint)'::pg_catalog.regprocedure]) AND a.mode IN ('i','b')
            GROUP BY p.proname
            """,connection))
        await using(var reader=await inputShape.ExecuteReaderAsync())
        {
            var observed=new Dictionary<string,string>(StringComparer.Ordinal);
            while(await reader.ReadAsync()) observed.Add(reader.GetString(0),reader.GetString(1));
            Assert.Equal(expectedInputs.OrderBy(x=>x.Key),observed.OrderBy(x=>x.Key));
        }
        await using var acl=new NpgsqlCommand("""
            WITH target AS (
              SELECT p.oid,p.proowner,p.proname,COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner)) acl
              FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname IN ('raw_export_commit_staged_source','raw_export_publish_available_source','raw_export_read_next_source_cleanup_item','raw_export_complete_source_cleanup_item','raw_export_finalize_source_cleanup')
            ), grants AS (
              SELECT t.proname,CASE WHEN x.grantee=0 THEN 'PUBLIC' ELSE pg_catalog.pg_get_userbyid(x.grantee) END grantee
              FROM target t CROSS JOIN LATERAL pg_catalog.aclexplode(t.acl) x WHERE x.privilege_type='EXECUTE'
            ), expected(proname,grantee) AS (VALUES
              ('raw_export_commit_staged_source','tagekyc_raw_export_deployer'),('raw_export_commit_staged_source','tagekyc_raw_export_reconciler'),
              ('raw_export_publish_available_source','tagekyc_raw_export_deployer'),('raw_export_publish_available_source','tagekyc_raw_export_reconciler'),
              ('raw_export_read_next_source_cleanup_item','tagekyc_raw_export_deployer'),('raw_export_read_next_source_cleanup_item','tagekyc_raw_export_reconciler'),('raw_export_read_next_source_cleanup_item','tagekyc_raw_export_lifecycle'),
              ('raw_export_complete_source_cleanup_item','tagekyc_raw_export_deployer'),('raw_export_complete_source_cleanup_item','tagekyc_raw_export_lifecycle'),
              ('raw_export_finalize_source_cleanup','tagekyc_raw_export_deployer'),('raw_export_finalize_source_cleanup','tagekyc_raw_export_lifecycle'))
            SELECT (SELECT count(*) FROM ((SELECT * FROM grants EXCEPT SELECT * FROM expected) UNION ALL (SELECT * FROM expected EXCEPT SELECT * FROM grants)) d)
              +(SELECT count(*) FROM (VALUES('tagekyc_runtime'),('tagekyc_raw_export_custody_encryptor'),('tagekyc_raw_export_reconciler'),('tagekyc_raw_export_lifecycle')) r(role)
                CROSS JOIN (VALUES('raw_export_source_publications'),('raw_export_source_cleanup_items')) t(table_name)
                WHERE pg_catalog.has_table_privilege(r.role,'tagekyc.'||t.table_name,'SELECT,INSERT,UPDATE,DELETE'))
            """,connection);
        Assert.Equal(0L,(long)(await acl.ExecuteScalarAsync())!);
        await using var memberships=new NpgsqlCommand("""
            WITH expected(member,role) AS (VALUES
              ('tagekyc_raw_export_encryptor_login','tagekyc_raw_export_custody_encryptor'),
              ('tagekyc_raw_export_reconciler_login','tagekyc_raw_export_reconciler'),
              ('tagekyc_raw_export_lifecycle_login','tagekyc_raw_export_lifecycle')),
            actual AS (
              SELECT member.rolname,role.rolname
              FROM pg_catalog.pg_auth_members m JOIN pg_catalog.pg_roles member ON member.oid=m.member
              JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
               WHERE role.rolname IN ('tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle')
                 AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option)
            SELECT (SELECT count(*) FROM ((SELECT * FROM actual EXCEPT SELECT * FROM expected) UNION ALL (SELECT * FROM expected EXCEPT SELECT * FROM actual)) d)
              +(SELECT count(*) FROM (VALUES
                ('tagekyc_raw_export_encryptor_login','raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)',false),
                ('tagekyc_raw_export_reconciler_login','raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)',true),
                ('tagekyc_raw_export_lifecycle_login','raw_export_commit_staged_source(uuid,bigint,bigint,bigint,bigint)',false),
                ('tagekyc_raw_export_encryptor_login','raw_export_publish_available_source(uuid,bigint,bigint)',false),
                ('tagekyc_raw_export_reconciler_login','raw_export_publish_available_source(uuid,bigint,bigint)',true),
                ('tagekyc_raw_export_lifecycle_login','raw_export_publish_available_source(uuid,bigint,bigint)',false),
                ('tagekyc_raw_export_encryptor_login','raw_export_read_next_source_cleanup_item(uuid,bigint)',false),
                ('tagekyc_raw_export_reconciler_login','raw_export_read_next_source_cleanup_item(uuid,bigint)',true),
                ('tagekyc_raw_export_lifecycle_login','raw_export_read_next_source_cleanup_item(uuid,bigint)',true),
                ('tagekyc_raw_export_encryptor_login','raw_export_complete_source_cleanup_item(uuid,bigint)',false),
                ('tagekyc_raw_export_reconciler_login','raw_export_complete_source_cleanup_item(uuid,bigint)',false),
                ('tagekyc_raw_export_lifecycle_login','raw_export_complete_source_cleanup_item(uuid,bigint)',true),
                ('tagekyc_raw_export_encryptor_login','raw_export_finalize_source_cleanup(uuid,bigint)',false),
                ('tagekyc_raw_export_reconciler_login','raw_export_finalize_source_cleanup(uuid,bigint)',false),
                ('tagekyc_raw_export_lifecycle_login','raw_export_finalize_source_cleanup(uuid,bigint)',true)) e(role_name,function_name,allowed)
                WHERE pg_catalog.has_function_privilege(e.role_name,'tagekyc.'||e.function_name,'EXECUTE') IS DISTINCT FROM e.allowed)
            """,connection);
        Assert.Equal(0L,(long)(await memberships.ExecuteScalarAsync())!);
        Assert.Contains(typeof(IProvisionalObjectReconciler),typeof(RawExportSourceCleanupReconciliationService).GetMethod("ReconcileObjectAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.GetParameters().Select(x=>x.ParameterType));
        Assert.Contains(typeof(IProvisionalObjectLifecycle),typeof(RawExportSourceCleanupService).GetMethod("SettleNextAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.GetParameters().Select(x=>x.ParameterType));
        var actor=Guid.NewGuid();
        await using(var reconcilerDb=CreateCapabilityDb("tagekyc_raw_export_reconciler"))
        {
            await AssertEffectiveRoleAsync(reconcilerDb,"tagekyc_raw_export_reconciler");
            Assert.Equal(SourceCleanupReadDisposition.NotFound,(await new RawExportSourceCleanupReconciliationService(reconcilerDb).ReadNextAsync(new(actor,Guid.NewGuid(),1))).Disposition);
            var complete=await Assert.ThrowsAsync<PostgresException>(()=>new RawExportSourceCleanupService(reconcilerDb).CompleteAsync(new(actor,Guid.NewGuid(),1)));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,complete.SqlState);Assert.Contains("raw_export_complete_source_cleanup_item",complete.MessageText,StringComparison.Ordinal);
            var finalize=await Assert.ThrowsAsync<PostgresException>(()=>new RawExportSourceCleanupService(reconcilerDb).FinalizeAsync(new(actor,Guid.NewGuid(),1)));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,finalize.SqlState);Assert.Contains("raw_export_finalize_source_cleanup",finalize.MessageText,StringComparison.Ordinal);
        }
        await using(var lifecycleDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
        {
            await AssertEffectiveRoleAsync(lifecycleDb,"tagekyc_raw_export_lifecycle");
            var lifecycle=new RawExportSourceCleanupService(lifecycleDb);
            Assert.Equal(SourceCleanupCompleteDisposition.NotFound,(await lifecycle.CompleteAsync(new(actor,Guid.NewGuid(),1))).Disposition);
            Assert.Equal(SourceCleanupFinalizeDisposition.NotFound,(await lifecycle.FinalizeAsync(new(actor,Guid.NewGuid(),1))).Disposition);
        }
    }

    [Fact]
    public async Task F413_publication_cleanup_shapes_and_write_guards_are_total_one_way()
    {
        await using(var connection=new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command=new NpgsqlCommand("SET ROLE tagekyc_raw_export_deployer; SELECT pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r4-commit-v1',false); INSERT INTO tagekyc.raw_export_source_publications(\"SourcePublicationId\") VALUES(pg_catalog.gen_random_uuid())",connection);
            var error=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.NotNullViolation,error.SqlState);
            Assert.Equal("SourceArtifactId",error.ColumnName);
        }

        await using(var connection=new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command=new NpgsqlCommand("SET ROLE tagekyc_raw_export_deployer; SELECT pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r5-publish-v1',false); INSERT INTO tagekyc.raw_export_source_cleanup_items(\"SourcePublicationId\") VALUES(pg_catalog.gen_random_uuid())",connection);
            var error=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.NotNullViolation,error.SqlState);
            Assert.Equal("CleanupItemId",error.ColumnName);
        }

        var (source,staged,committed)=await CreateCommittedAsync("f413"u8.ToArray());
        await AssertR5HeadGuardComparatorsAsync(source.SourceArtifactId);
        await AddObsoleteVerifiedAttemptAsync(source);
        var published=await PublishAsync(source,staged,committed);
        Assert.Equal("Pending",published.CleanupDisposition);
        await AssertRequiredColumnsAsync("raw_export_source_publications","tr_raw_export_source_publication_guard","SourcePublicationId",published.SourcePublicationId!.Value,
            "SourcePublicationId","SourceArtifactId","AttemptId","ObjectCustodyId","AttemptKeyReservationId","StagedCiphertextFingerprintSchemaVersion",
            "StagedCiphertextFingerprint","CommittedAuthoritySnapshotSchemaVersion","CommittedAuthoritySnapshotId","CommittedAuthorityRevision",
            "CommittedConsentPolicyId","CommittedConsentPolicyVersion","CommitEvidenceDigest","CommittedAtUtc","PublicationState","CleanupDisposition",
            "PublicationRevision","SchemaVersion");
        await using(var requiredDb=postgres.CreateDbContext())
        {
            var requiredItem=await requiredDb.RawExportSourceCleanupItems.AsNoTracking().OrderBy(x=>x.CleanupItemId).FirstAsync();
            await AssertRequiredColumnsAsync("raw_export_source_cleanup_items","tr_raw_export_source_cleanup_item_guard","CleanupItemId",requiredItem.CleanupItemId,
                "CleanupItemId","SourcePublicationId","ResourceKind","ResourceId","ResourceAttemptId","PlannedResourceRevision","CleanupState","RowRevision","SchemaVersion");
        }

        foreach(var column in new[]{"OpaqueCommittedLocatorId","PublishedAuthoritySnapshotSchemaVersion","PublishedAuthoritySnapshotId","PublishedAuthorityRevision","PublishedConsentPolicyId","PublishedConsentPolicyVersion","AvailableEvidenceDigest","AvailableAtUtc"})
            await AssertShapeCheckAsync("raw_export_source_publications","tr_raw_export_source_publication_guard",$"UPDATE tagekyc.raw_export_source_publications SET \"{column}\"=NULL WHERE \"SourcePublicationId\"=@id",published.SourcePublicationId!.Value,"ck_raw_export_source_publication_shape");

        await AssertShapeCheckAsync("raw_export_source_publications","tr_raw_export_source_publication_guard","UPDATE tagekyc.raw_export_source_publications SET \"CleanupDisposition\"='NotPlanned' WHERE \"SourcePublicationId\"=@id",published.SourcePublicationId!.Value,"ck_raw_export_source_publication_shape");

        await using(var db=postgres.CreateDbContext())
        {
            var objectItem=await db.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(x=>x.ResourceKind=="ProvisionalObject");
            foreach(var column in new[]{"CompletionDisposition","CleanupEvidenceDigest","CompletedAtUtc"})
            {
                var disposition=column=="CompletionDisposition"?"NULL":"'Deleted'";
                var evidence=column=="CleanupEvidenceDigest"?"NULL":"pg_catalog.decode(pg_catalog.repeat('00',32),'hex')";
                var completedAt=column=="CompletedAtUtc"?"NULL":"pg_catalog.clock_timestamp()";
                await AssertShapeCheckAsync("raw_export_source_cleanup_items","tr_raw_export_source_cleanup_item_guard",$"UPDATE tagekyc.raw_export_source_cleanup_items SET \"CleanupState\"='Completed',\"RowRevision\"=2,\"CompletionDisposition\"={disposition},\"CleanupEvidenceDigest\"={evidence},\"CompletedAtUtc\"={completedAt} WHERE \"CleanupItemId\"=@id",objectItem.CleanupItemId,"ck_raw_export_source_cleanup_item_shape");
            }
            await AssertShapeCheckAsync("raw_export_source_cleanup_items","tr_raw_export_source_cleanup_item_guard","UPDATE tagekyc.raw_export_source_cleanup_items SET \"CleanupState\"='Completed',\"RowRevision\"=2,\"CompletionDisposition\"='Revoked',\"CleanupEvidenceDigest\"=pg_catalog.decode(pg_catalog.repeat('00',32),'hex'),\"CompletedAtUtc\"=pg_catalog.clock_timestamp() WHERE \"CleanupItemId\"=@id",objectItem.CleanupItemId,"ck_raw_export_source_cleanup_item_shape");
        }

        await using(var connection=new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command=new NpgsqlCommand("SET ROLE tagekyc_raw_export_deployer; UPDATE tagekyc.raw_export_source_publications SET \"CommittedAtUtc\"=\"CommittedAtUtc\" WHERE \"SourcePublicationId\"=@id",connection);
            command.Parameters.AddWithValue("id",published.SourcePublicationId!.Value);
            var error=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Equal("P0001",error.SqlState);
            Assert.Equal("RAW_EXPORT_SOURCE_PUBLICATION_WRITE_FORBIDDEN",error.MessageText);
        }

        await using(var connection=new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var duplicate=new NpgsqlCommand("""
                SET ROLE tagekyc_raw_export_deployer;
                SELECT pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r4-commit-v1',false);
                INSERT INTO tagekyc.raw_export_source_publications SELECT * FROM tagekyc.raw_export_source_publications WHERE "SourcePublicationId"=@id;
                """,connection);
            duplicate.Parameters.AddWithValue("id",published.SourcePublicationId!.Value);
            var duplicateError=await Assert.ThrowsAsync<PostgresException>(()=>duplicate.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation,duplicateError.SqlState);Assert.Equal("pk_raw_export_source_publications",duplicateError.ConstraintName);
        }

        await using(var connection=new NpgsqlConnection(postgres.ConnectionString))
        {
            await connection.OpenAsync();
            await using var badForeignKey=new NpgsqlCommand("""
                SET ROLE tagekyc_raw_export_deployer;
                SELECT pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r5-publish-v1',false);
                INSERT INTO tagekyc.raw_export_source_cleanup_items
                  ("CleanupItemId","SourcePublicationId","ResourceKind","ResourceId","ResourceAttemptId","PlannedResourceRevision","CleanupState","RowRevision","SchemaVersion")
                VALUES(pg_catalog.gen_random_uuid(),pg_catalog.gen_random_uuid(),'ProvisionalObject',pg_catalog.gen_random_uuid(),pg_catalog.gen_random_uuid(),1,'Pending',1,1);
                """,connection);
            var foreignKeyError=await Assert.ThrowsAsync<PostgresException>(()=>badForeignKey.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation,foreignKeyError.SqlState);Assert.Equal("fk_raw_export_source_cleanup_item_publication",foreignKeyError.ConstraintName);
        }
    }

    [Fact]
    public void F414_schema_contracts_services_results_and_logs_contain_no_raw_plaintext_digest_or_key_material()
    {
        var types=new[]{typeof(CommitStagedSourceCommand),typeof(PublishAvailableSourceCommand),typeof(ReadNextSourceCleanupItemCommand),
            typeof(CompleteSourceCleanupItemCommand),typeof(FinalizeSourceCleanupCommand),typeof(SourceCommitResult),typeof(SourcePublishResult),
            typeof(SourceCleanupReadResult),typeof(SourceCleanupCompleteResult),typeof(SourceCleanupFinalizeResult),
            typeof(TagEkyc.Infrastructure.Persistence.Entities.RawExportSourcePublicationRow),typeof(TagEkyc.Infrastructure.Persistence.Entities.RawExportSourceCleanupItemRow)};
        var forbidden=new[]{"Plaintext","ObjectKey","Bucket","WrappedDek","Kek","Credential","Secret","RawBio"};
        foreach(var type in types)
        {
            Assert.DoesNotContain(forbidden,token=>type.Name.Contains(token,StringComparison.OrdinalIgnoreCase));
            foreach(var property in type.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                Assert.DoesNotContain(forbidden,token=>property.Name.Contains(token,StringComparison.OrdinalIgnoreCase)||property.PropertyType.Name.Contains(token,StringComparison.OrdinalIgnoreCase));
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                Assert.DoesNotContain(forbidden,token=>field.Name.Contains(token,StringComparison.OrdinalIgnoreCase)||field.FieldType.Name.Contains(token,StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task F415_apply_occupied_Down_rejection_teardown_Down_reapply_restores_R3()
    {
        await CreateCommittedAsync("f415-occupied"u8.ToArray());
        await using(var occupied=postgres.CreateDbContext())
        {
            var error=await Assert.ThrowsAsync<PostgresException>(()=>occupied.Database.GetService<IMigrator>().MigrateAsync(R3Migration));
            Assert.Equal("RAW_EXPORT_SOURCE_FINALIZATION_DOWN_OCCUPIED",error.MessageText);
        }
        await using(var reset=postgres.CreateDbContext()){await reset.Database.EnsureDeletedAsync();await reset.Database.GetService<IMigrator>().MigrateAsync(R3Migration);}
        var before=await HeadGuardDefinitionAsync();
        var coreBefore=await CoreGuardDefinitionAsync();
        try
        {
            await MigrateAsync(R4R6Migration); await MigrateAsync(R3Migration);
            Assert.Equal(before.ReplaceLineEndings("\n"),(await HeadGuardDefinitionAsync()).ReplaceLineEndings("\n"));
            Assert.Equal(coreBefore.ReplaceLineEndings("\n"),(await CoreGuardDefinitionAsync()).ReplaceLineEndings("\n"));
            await AssertR4R6CatalogAbsentAsync();
        }
        finally
        {
            await using var restore=postgres.CreateDbContext();
            await restore.Database.GetService<IMigrator>().MigrateAsync();
        }
    }

    [Fact]
    public async Task F416_model_snapshot_catalog_and_E3_tripwire_are_synchronized()
    {
        await using var db=postgres.CreateDbContext();
        Assert.False(db.Database.HasPendingModelChanges());
        var model=db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var publication=model.FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportSourcePublicationRow");
        var cleanup=model.FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportSourceCleanupItemRow");
        Assert.NotNull(publication); Assert.NotNull(cleanup);
        Assert.Equal(28,publication!.GetProperties().Count()); Assert.Equal(12,cleanup!.GetProperties().Count());
        Assert.Equal("ck_raw_export_source_publication_shape",Assert.Single(publication.GetCheckConstraints()).Name);
        Assert.Equal("ck_raw_export_source_cleanup_item_shape",Assert.Single(cleanup.GetCheckConstraints()).Name);
        Assert.Equal(4,publication.GetKeys().Count()); Assert.Equal(3,publication.GetIndexes().Count()); Assert.Equal(5,publication.GetForeignKeys().Count());
        Assert.Single(cleanup.GetKeys()); Assert.Equal(2,cleanup.GetIndexes().Count()); Assert.Single(cleanup.GetForeignKeys());
    }

    [Fact]
    public async Task F417_R3_Staged_to_R4_Committed_to_R5_Available_preserves_exact_fingerprint()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f417"u8.ToArray()); await PublishAsync(source,staged,committed);
        await using var db=postgres.CreateDbContext(); var publication=await db.RawExportSourcePublications.AsNoTracking().SingleAsync(); var attempt=await db.RawExportSourceEncryptionAttempts.AsNoTracking().SingleAsync(x=>x.AttemptId==source.AttemptId);
        Assert.Equal(attempt.StagedCiphertextFingerprint,publication.StagedCiphertextFingerprint);
    }

    [Fact]
    public async Task F418_restart_after_each_R4_R5_R6_commit_recovers_only_from_durable_state()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f418"u8.ToArray());
        var obsolete=await AddObsoleteVerifiedAttemptAsync(source);
        await MakeKeyProviderOutcomeUnknownAsync(obsolete.AttemptKeyReservationId);
        await using(var fresh=postgres.CreateDbContext()) Assert.Equal(SourcePublishDisposition.Available,(await new RawExportSourceFinalizationService(fresh).PublishAsync(new(source.ActorPrincipalId,committed.SourcePublicationId!.Value,staged.ReservationRevision!.Value,source.Fence))).Disposition);
        var recovery=new FixedRecovery();
        var objectClosed=false;var keyClosed=false;
        while(!objectClosed||!keyClosed)
        {
            var kind=await NextPendingKindAsync(committed.SourcePublicationId.Value);
            if(kind=="ProvisionalObject")
            {
                await using(var unavailableDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
                {
                    await AssertEffectiveRoleAsync(unavailableDb,"tagekyc_raw_export_lifecycle");
                    Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupService(unavailableDb).SettleNextAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.OutcomeUnknown,null))).Disposition);
                }
                await using(var reconcilerDb=CreateCapabilityDb("tagekyc_raw_export_reconciler"))
                {
                    await AssertEffectiveRoleAsync(reconcilerDb,"tagekyc_raw_export_reconciler");
                    Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(reconcilerDb).ReconcileObjectAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedReconciler(ExactObjectInspectionOutcome.PositivelyAbsent,ExactObjectInspectionOutcome.PositivelyAbsent))).Disposition);
                }
                await AssertPendingDurableHandoffAsync(committed.SourcePublicationId.Value,obsolete.ObjectCustodyId,"ProvisionalObject","Deleted");
                await using(var replayDb=CreateCapabilityDb("tagekyc_raw_export_reconciler"))
                    Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(replayDb).ReconcileObjectAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedReconciler())).Disposition);
                await using(var lifecycleDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
                {
                    await AssertEffectiveRoleAsync(lifecycleDb,"tagekyc_raw_export_lifecycle");
                    Assert.Equal(SourceCleanupCompleteDisposition.Completed,(await new RawExportSourceCleanupService(lifecycleDb).SettleNextAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.DeletedAcknowledged,204))).Disposition);
                }
                objectClosed=true;
            }
            else
            {
                Assert.Equal("AttemptKeyReservation",kind);
                await using(var keyDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
                {
                    await AssertEffectiveRoleAsync(keyDb,"tagekyc_raw_export_lifecycle");
                    Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupService(keyDb).SettleNextAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.ProviderUnavailable,null))).Disposition);
                }
                await using(var recoveryDb=CreateCapabilityDb("tagekyc_raw_export_reconciler")) Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(recoveryDb).ReconcileKeyAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),recovery)).Disposition);
                Assert.Equal(1,recovery.ResolveCallCount); Assert.Equal(0,recovery.CleanupCallCount);
                await using(var cleanupDb=CreateCapabilityDb("tagekyc_raw_export_reconciler")) Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(cleanupDb).ReconcileKeyAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),recovery)).Disposition);
                Assert.Equal(1,recovery.ResolveCallCount); Assert.Equal(1,recovery.CleanupCallCount);
                await AssertPendingDurableHandoffAsync(committed.SourcePublicationId.Value,obsolete.AttemptKeyReservationId,"AttemptKeyReservation","AbandonRequested","CleanedUp");
                await using(var replayDb=CreateCapabilityDb("tagekyc_raw_export_reconciler")) Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(replayDb).ReconcileKeyAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),recovery)).Disposition);
                Assert.Equal(1,recovery.ResolveCallCount); Assert.Equal(1,recovery.CleanupCallCount);
                await using(var lifecycleDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
                {
                    await AssertEffectiveRoleAsync(lifecycleDb,"tagekyc_raw_export_lifecycle");
                    Assert.Equal(SourceCleanupCompleteDisposition.Completed,(await new RawExportSourceCleanupService(lifecycleDb).SettleNextAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.ProviderUnavailable,null))).Disposition);
                }
                keyClosed=true;
            }
        }
        await using(var freshCleanup=postgres.CreateDbContext()) Assert.Equal(SourceCleanupFinalizeDisposition.Completed,(await new RawExportSourceCleanupService(freshCleanup).FinalizeAsync(new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2))).Disposition);
        await using var verify=postgres.CreateDbContext(); var durable=await verify.RawExportSourcePublications.AsNoTracking().SingleAsync();
        Assert.Equal("Available",durable.PublicationState); Assert.Equal("Completed",durable.CleanupDisposition); Assert.Equal(32,durable.CleanupEvidenceDigest!.Length);
        Assert.Equal("Deleted",(await verify.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==obsolete.ObjectCustodyId)).State);
        Assert.Equal("ReservationAbandoned",(await verify.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==obsolete.AttemptKeyReservationId)).PreparationDisposition);
        var objectItem=await verify.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(x=>x.SourcePublicationId==committed.SourcePublicationId&&x.ResourceKind=="ProvisionalObject");
        var deletedObject=await verify.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==obsolete.ObjectCustodyId);
        Assert.Equal(objectItem.CleanupEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeObjectItem(new(
            committed.SourcePublicationId.Value,obsolete.AttemptId,obsolete.ObjectCustodyId,"Deleted",deletedObject.DeletionEvidenceDigest!,objectItem.CompletedAtUtc!.Value)));
        var keyItem=await verify.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(x=>x.SourcePublicationId==committed.SourcePublicationId&&x.ResourceKind=="AttemptKeyReservation");
        var abandonedEvent=await verify.RawExportAttemptKeyPreparationEvents.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==obsolete.AttemptKeyReservationId&&x.EventKind=="ProviderOperationAbandoned");
        Assert.Equal(keyItem.CleanupEvidenceDigest,RawExportSourceFinalizationEvidenceCodec.ComputeKeyItem(new(
            committed.SourcePublicationId.Value,obsolete.AttemptId,obsolete.AttemptKeyReservationId,"ReservationAbandoned",abandonedEvent.AbandonmentEvidenceDigest!,keyItem.CompletedAtUtc!.Value)));

        await postgres.ResetDatabaseAsync();
        await ProveF418AbsenceProvenReplayAsync();
    }

    private async Task ProveF418AbsenceProvenReplayAsync()
    {
        var (source,staged,committed)=await CreateCommittedAsync("f418-absence"u8.ToArray());
        var obsolete=await AddObsoleteVerifiedAttemptAsync(source);
        await MakeKeyProviderOutcomeUnknownAsync(obsolete.AttemptKeyReservationId,expirePreparationLease:true);
        await PublishAsync(source,staged,committed);
        while(await NextPendingKindAsync(committed.SourcePublicationId!.Value)=="ProvisionalObject")
        {
            await using var objectLifecycleDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle");
            Assert.Equal(SourceCleanupCompleteDisposition.Completed,(await new RawExportSourceCleanupService(objectLifecycleDb).SettleNextAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.DeletedAcknowledged,204))).Disposition);
        }
        Assert.Equal("AttemptKeyReservation",await NextPendingKindAsync(committed.SourcePublicationId.Value));
        await using(var initialLifecycleDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
            Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupService(initialLifecycleDb).SettleNextAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.ProviderUnavailable,null))).Disposition);

        var recovery=new FixedRecovery(new KekProvisioningResolution.NoProviderResult("fixture-absence-proof"));
        await using(var reconciliationDb=CreateCapabilityDb("tagekyc_raw_export_reconciler"))
            Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(reconciliationDb).ReconcileKeyAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),recovery)).Disposition);
        Assert.Equal(1,recovery.ResolveCallCount); Assert.Equal(0,recovery.CleanupCallCount);
        await AssertPendingDurableHandoffAsync(committed.SourcePublicationId.Value,obsolete.AttemptKeyReservationId,"AttemptKeyReservation","AbandonRequested","AbsenceProven");

        await using(var replayDb=CreateCapabilityDb("tagekyc_raw_export_reconciler"))
            Assert.Equal(SourceCleanupCompleteDisposition.ResourceNotTerminal,(await new RawExportSourceCleanupReconciliationService(replayDb).ReconcileKeyAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),recovery)).Disposition);
        Assert.Equal(1,recovery.ResolveCallCount); Assert.Equal(0,recovery.CleanupCallCount);

        await using(var lifecycleDb=CreateCapabilityDb("tagekyc_raw_export_lifecycle"))
            Assert.Equal(SourceCleanupCompleteDisposition.Completed,(await new RawExportSourceCleanupService(lifecycleDb).SettleNextAsync(
                new(source.ActorPrincipalId,committed.SourcePublicationId.Value,2),new FixedLifecycle(ExactDeleteOutcome.ProviderUnavailable,null))).Disposition);
        await using var verify=postgres.CreateDbContext();
        Assert.Equal("ReservationAbandoned",(await verify.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(
            x=>x.AttemptKeyReservationId==obsolete.AttemptKeyReservationId)).PreparationDisposition);
        Assert.Equal("Completed",(await verify.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(
            x=>x.SourcePublicationId==committed.SourcePublicationId&&x.ResourceKind=="AttemptKeyReservation")).CleanupState);
    }

    private async Task<(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture Source,RawExportR3StageResult Staged)> CreateStagedAsync(byte[] plaintext,TimeSpan? sourceLifetime=null,TimeSpan? consentLifetime=null)
    {
        await using var minio=await DurableObjectMinioFixture.StartAsync();
        var source=await new Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(postgres).CreateR3VerifiedSourceAsync(plaintext,minio,sourceLifetime:sourceLifetime,consentLifetime:consentLifetime);
        await using var db=postgres.CreateDbContext(); var staged=await new RawExportR3StagingService(db).StageAsync(new(source.ActorPrincipalId,source.AttemptId,source.ObjectCustodyId,source.ReservationRevision,source.EncryptionAttemptRevision,source.Fence,source.ObjectStateRevision));
        return(source,staged);
    }
    private async Task<(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture Source,RawExportR3StageResult Staged,SourceCommitResult Committed)> CreateCommittedAsync(byte[] plaintext,TimeSpan? sourceLifetime=null,TimeSpan? consentLifetime=null)
    { var pair=await CreateStagedAsync(plaintext,sourceLifetime,consentLifetime); return(pair.Source,pair.Staged,await CommitAsync(pair.Source,pair.Staged)); }
    private async Task<SourceCommitResult> CommitAsync(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture s,RawExportR3StageResult st,long? expectedReservationRevision=null,long? expectedAttemptRevision=null,long? expectedFence=null,long? expectedObjectRevision=null)
    { await using var db=postgres.CreateDbContext(); return await new RawExportSourceFinalizationService(db).CommitAsync(new(s.ActorPrincipalId,s.AttemptId,expectedReservationRevision??st.ReservationRevision!.Value,expectedAttemptRevision??s.EncryptionAttemptRevision,expectedFence??s.Fence,expectedObjectRevision??s.ObjectStateRevision)); }
    private async Task<SourcePublishResult> PublishAsync(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture s,RawExportR3StageResult st,SourceCommitResult c,long? revision=null,string? applicationName=null)
    { await using var db=applicationName is null?postgres.CreateDbContext():CreateNamedDb(applicationName); return await new RawExportSourceFinalizationService(db).PublishAsync(new(s.ActorPrincipalId,c.SourcePublicationId!.Value,revision??st.ReservationRevision!.Value,s.Fence)); }
    private async Task<RawExportR3StageResult> StageReplayAsync(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture s,string? applicationName=null)
    { await using var db=applicationName is null?postgres.CreateDbContext():CreateNamedDb(applicationName); return await new RawExportR3StagingService(db).StageAsync(new(s.ActorPrincipalId,s.AttemptId,s.ObjectCustodyId,s.ReservationRevision,s.EncryptionAttemptRevision,s.Fence,s.ObjectStateRevision)); }
    private async Task WithdrawAuthorityAsync(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); await using var tx=await connection.BeginTransactionAsync();
        await using(var actor=new NpgsqlCommand("SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)",connection,tx)){actor.Parameters.AddWithValue("actor",source.ActorPrincipalId.ToString("D"));await actor.ExecuteNonQueryAsync();}
        await using(var command=new NpgsqlCommand("SELECT tagekyc.raw_export_withdraw_authority_snapshot(@client,@session,@acceptance,@class,@revision,@actor)",connection,tx))
        {command.Parameters.AddWithValue("client",source.ClientApplicationId);command.Parameters.AddWithValue("session",source.VerificationSessionId);command.Parameters.AddWithValue("acceptance",source.CaptureAcceptanceId);command.Parameters.AddWithValue("class",source.RawClass);command.Parameters.AddWithValue("revision",source.AuthorityRevision);command.Parameters.AddWithValue("actor",source.ActorPrincipalId);await command.ExecuteNonQueryAsync();}
        await tx.CommitAsync();
    }
    private async Task<ObsoleteResidueFixture> AddObsoleteVerifiedAttemptAsync(Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        var attemptId=Guid.ParseExact("00000000000000000000000000000002","N"); var sharedResourceId=Guid.ParseExact("00000000000000000000000000000010","N"); var keyId=sharedResourceId; var objectId=sharedResourceId; var objectIdentity=Guid.NewGuid(); var preparationId=Guid.NewGuid();
        var operationId=Guid.NewGuid(); var fingerprint=RandomNumberGenerator.GetBytes(32); var contextFingerprint=RandomNumberGenerator.GetBytes(32); var bindingDigest=RandomNumberGenerator.GetBytes(32);
        var token=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        await using var connection=new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); await using var tx=await connection.BeginTransactionAsync();
        await using var command=new NpgsqlCommand("""
            SET LOCAL ROLE tagekyc_raw_export_deployer;
            SELECT pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','complete-r1',true);
            INSERT INTO tagekyc.raw_export_source_encryption_attempts(
              "AttemptId","SourceArtifactId","EncryptionAttemptRevision","Fence","ProvisionalObjectIdentity","AttemptKeyReservationId",
              "KeyProviderId","KekId","KekVersion","KekFingerprint","EncryptionSuiteId","EncryptionFramingVersion","NonceStrategyId",
              "NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize","FramingParametersDigest",
              "EncryptionAttemptFingerprint","OwnershipLeaseExpiresAtUtc","R2TerminationDisposition","R2TerminatedAtUtc",
              "StagedCiphertextFingerprintSchemaVersion","StagedCiphertextFingerprint","StagedObjectCustodyId","StagedObjectStateRevision",
              "StagedFromReservationRevision","VerifiedPlaintextLength","StagedCiphertextLength","StagedCiphertextDigest",
              "StagedProviderReceiptDigest","StagedVerificationEvidenceDigest","StagedAtUtc","CreatedAtUtc","SchemaVersion")
            SELECT @attempt,"SourceArtifactId","EncryptionAttemptRevision"+1,"Fence"+1,@identity,@key,
              "KeyProviderId","KekId","KekVersion","KekFingerprint","EncryptionSuiteId","EncryptionFramingVersion","NonceStrategyId",
              "NonceDerivationSeedReferenceOrWrappedSeed","NonceDerivationSeedCommitment","ChunkSize","FramingParametersDigest",
              @fingerprint,pg_catalog.clock_timestamp()+interval '1 hour',NULL,NULL,
              NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,pg_catalog.clock_timestamp(),"SchemaVersion"
            FROM tagekyc.raw_export_source_encryption_attempts WHERE "AttemptId"=@winner;

            SELECT pg_catalog.set_config('tagekyc.raw_export_attempt_key_write_context','active',true);
            INSERT INTO tagekyc.raw_export_attempt_key_reservations(
              "AttemptKeyReservationId","AttemptId","EncryptionAttemptFingerprint","KeyProviderId","KekId","KekVersion","KekFingerprint",
              "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion","PreparationDisposition","CurrentPreparationId",
              "CurrentPreparationFence","CurrentPreparationLeaseExpiresAtUtc","CurrentProviderOperationToken","ResolutionAttemptCount",
              "NextResolutionAttemptNotBeforeUtc","ResolutionDeadlineUtc","CleanupAttemptCount","NextCleanupAttemptNotBeforeUtc",
              "CleanupDeadlineUtc","CleanupOperatorInterventionRequired","WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag",
              "WrappedDekMetadataDigest","RowRevision","PreparedAtUtc","RevokedAtUtc","RevocationReasonCode","CreatedAtUtc","UpdatedAtUtc")
            SELECT @key,@attempt,@fingerprint,"KeyProviderId","KekId","KekVersion","KekFingerprint",
              @context,"WrappingSuiteId","WrappingSuiteVersion",'Active',@preparation,"CurrentPreparationFence",
              "CurrentPreparationLeaseExpiresAtUtc",@token,"ResolutionAttemptCount","NextResolutionAttemptNotBeforeUtc","ResolutionDeadlineUtc",
              "CleanupAttemptCount","NextCleanupAttemptNotBeforeUtc","CleanupDeadlineUtc","CleanupOperatorInterventionRequired",
              "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag","WrappedDekMetadataDigest",1,"PreparedAtUtc",NULL,NULL,
              pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp()
            FROM tagekyc.raw_export_attempt_key_reservations WHERE "AttemptKeyReservationId"=@winnerKey;

            INSERT INTO tagekyc.raw_export_key_provider_operations(
              "ProviderOperationId","KeyProviderId","ProviderOperationToken","AttemptKeyReservationId","PreparationId","PreparationFence",
              "AttemptKeyContextFingerprint","ProviderOperationState","WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag",
              "WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion","ProviderResourceReference","ProviderOperationReceipt",
              "ResultObservedAtUtc","ProviderCleanupReference","ProviderCleanupReceipt","ProviderAbsenceProofReceipt","IssuedAtUtc","UpdatedAtUtc")
            SELECT @operation,"KeyProviderId",@token,@key,@preparation,"PreparationFence",@context,'ResultObserved',
              "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag","WrappedDekMetadataDigest","WrappingSuiteId","WrappingSuiteVersion",
              "ProviderResourceReference","ProviderOperationReceipt","ResultObservedAtUtc",NULL,NULL,NULL,pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp()
            FROM tagekyc.raw_export_key_provider_operations WHERE "AttemptKeyReservationId"=@winnerKey ORDER BY "UpdatedAtUtc" DESC LIMIT 1;

            SELECT pg_catalog.set_config('tagekyc.raw_export_provisional_object_write_context','tip88c1-object-head-write-v1',true);
            INSERT INTO tagekyc.raw_export_provisional_objects(
              "ObjectCustodyId","AttemptId","AttemptKeyReservationId","SourceArtifactId","ProvisionalObjectIdentity","EncryptionAttemptRevision",
              "AttemptFence","EncryptionAttemptFingerprint","ObjectKey","ObjectBindingDigest","State","StateRevision","PutOperationId",
              "PutArmedAtUtc","PutOutcomeKind","OutcomeObservedAtUtc","CiphertextLength","CiphertextDigest","ProviderReceiptDigest",
              "VerificationEvidenceDigest","VerifiedAtUtc","CleanupReasonCode","CleanupEvidenceDigest","CleanupRequestedAtUtc",
              "DeletionEvidenceDigest","DeletionEvidenceKind","DeletedAtUtc","QuarantineReasonCode","QuarantineEvidenceDigest",
              "QuarantinedAtUtc","CreatedAtUtc","UpdatedAtUtc","SchemaVersion")
            SELECT @object,@attempt,@key,"SourceArtifactId",@identity,"EncryptionAttemptRevision"+1,"AttemptFence"+1,@fingerprint,
              @objectKey,@binding,'VerifiedCompleted',"StateRevision",@operation,"PutArmedAtUtc","PutOutcomeKind","OutcomeObservedAtUtc",
              "CiphertextLength","CiphertextDigest","ProviderReceiptDigest","VerificationEvidenceDigest","VerifiedAtUtc",
              NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp(),"SchemaVersion"
            FROM tagekyc.raw_export_provisional_objects WHERE "ObjectCustodyId"=@winnerObject;
            """,connection,tx);
        command.Parameters.AddWithValue("attempt",attemptId); command.Parameters.AddWithValue("key",keyId); command.Parameters.AddWithValue("object",objectId);
        command.Parameters.AddWithValue("identity",objectIdentity); command.Parameters.AddWithValue("preparation",preparationId); command.Parameters.AddWithValue("operation",operationId);
        command.Parameters.AddWithValue("fingerprint",fingerprint); command.Parameters.AddWithValue("context",contextFingerprint); command.Parameters.AddWithValue("binding",bindingDigest);
        command.Parameters.AddWithValue("token",token); command.Parameters.AddWithValue("objectKey","raw-export/c1/v1/"+objectIdentity.ToString("N"));
        command.Parameters.AddWithValue("winner",source.AttemptId); command.Parameters.AddWithValue("winnerKey",source.AttemptKeyReservationId); command.Parameters.AddWithValue("winnerObject",source.ObjectCustodyId);
        await command.ExecuteNonQueryAsync(); await tx.CommitAsync();
        return new(attemptId,keyId,objectId);
    }
    private static string MigrationText()=>File.ReadAllText(Path.Combine(RepoRoot(),"src/TagEkyc.Infrastructure/Persistence/Migrations/20260812120000_Tip88C1B2R4R6SourceFinalization.cs"));
    private async Task MigrateAsync(string target){await using var db=postgres.CreateDbContext();await db.Database.GetService<IMigrator>().MigrateAsync(target);}
    private async Task AssertShapeCheckAsync(string table,string trigger,string statement,Guid id,string constraint)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync();
        await using var transaction=await connection.BeginTransactionAsync();
        await using(var disable=new NpgsqlCommand($"ALTER TABLE tagekyc.{table} DISABLE TRIGGER {trigger}",connection,transaction)) await disable.ExecuteNonQueryAsync();
        await using var command=new NpgsqlCommand(statement,connection,transaction); command.Parameters.AddWithValue("id",id);
        var error=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation,error.SqlState); Assert.Equal(constraint,error.ConstraintName);
        await transaction.RollbackAsync();
    }
    private async Task<string> HeadGuardDefinitionAsync(){await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();await using var command=new NpgsqlCommand("SELECT pg_catalog.pg_get_functiondef('tagekyc.enforce_raw_export_source_head_write()'::pg_catalog.regprocedure)",connection);return (string)(await command.ExecuteScalarAsync())!;}
    private async Task<string> CoreGuardDefinitionAsync(){await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();await using var command=new NpgsqlCommand("SELECT pg_catalog.pg_get_functiondef('tagekyc.enforce_raw_export_source_core_write()'::pg_catalog.regprocedure)",connection);return (string)(await command.ExecuteScalarAsync())!;}

    private async Task AssertR4R6CatalogAbsentAsync()
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            SELECT
              (SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='tagekyc' AND c.relname IN ('raw_export_source_publications','raw_export_source_cleanup_items'))
              +(SELECT count(*) FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname IN ('raw_export_commit_staged_source','raw_export_publish_available_source','raw_export_read_next_source_cleanup_item','raw_export_complete_source_cleanup_item','raw_export_finalize_source_cleanup','enforce_raw_export_source_publication_write','enforce_raw_export_source_cleanup_item_write'))
            """,connection);
        Assert.Equal(0L,(long)(await command.ExecuteScalarAsync())!);
    }

    private async Task<string?> NextPendingKindAsync(Guid publicationId)
    {
        await using var db=postgres.CreateDbContext();
        return await db.RawExportSourceCleanupItems.AsNoTracking().Where(x=>x.SourcePublicationId==publicationId&&x.CleanupState=="Pending")
            .OrderBy(x=>x.CleanupItemId).Select(x=>x.ResourceKind).FirstOrDefaultAsync();
    }

    private TagEkycDbContext CreateCapabilityDb(string role)
    {
        if(role is not ("tagekyc_raw_export_reconciler" or "tagekyc_raw_export_lifecycle")) throw new ArgumentOutOfRangeException(nameof(role));
        var builder=new NpgsqlConnectionStringBuilder(postgres.ConnectionString);
        builder["Options"]=$"-c role={role}";
        return new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(builder.ConnectionString).Options);
    }

    private static async Task AssertEffectiveRoleAsync(TagEkycDbContext db,string expected)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command=new NpgsqlCommand("SELECT current_user",(NpgsqlConnection)db.Database.GetDbConnection());
            Assert.Equal(expected,(string)(await command.ExecuteScalarAsync())!);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private async Task AssertPendingDurableHandoffAsync(Guid publicationId,Guid resourceId,string resourceKind,string expectedState,string? expectedProviderOperationState=null)
    {
        await using var db=postgres.CreateDbContext();
        var item=await db.RawExportSourceCleanupItems.AsNoTracking().SingleAsync(x=>x.SourcePublicationId==publicationId&&x.ResourceKind==resourceKind);
        Assert.Equal(resourceId,item.ResourceId);Assert.Equal("Pending",item.CleanupState);Assert.Equal(1,item.RowRevision);
        if(resourceKind=="ProvisionalObject")
            Assert.Equal(expectedState,(await db.RawExportProvisionalObjects.AsNoTracking().SingleAsync(x=>x.ObjectCustodyId==resourceId)).State);
        else
        {
            Assert.Equal(expectedState,(await db.RawExportAttemptKeyReservations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==resourceId)).PreparationDisposition);
            if(expectedProviderOperationState is not null)
                Assert.Equal(expectedProviderOperationState,(await db.RawExportKeyProviderOperations.AsNoTracking().SingleAsync(x=>x.AttemptKeyReservationId==resourceId)).ProviderOperationState);
        }
    }

    private async Task AssertR5HeadGuardComparatorsAsync(Guid sourceArtifactId)
    {
        var cases=new (string Name,string? Setup,string Update)[]
        {
            ("old-state","UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Reserved' WHERE \"SourceArtifactId\"=@id","UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Available',\"ReservationRevision\"=\"ReservationRevision\"+1 WHERE \"SourceArtifactId\"=@id"),
            ("new-state",null,"UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Committed',\"ReservationRevision\"=\"ReservationRevision\"+1 WHERE \"SourceArtifactId\"=@id"),
            ("revision",null,"UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Available',\"ReservationRevision\"=\"ReservationRevision\"+2 WHERE \"SourceArtifactId\"=@id"),
            ("source",null,"UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Available',\"ReservationRevision\"=\"ReservationRevision\"+1,\"SourceArtifactId\"=pg_catalog.gen_random_uuid() WHERE \"SourceArtifactId\"=@id"),
            ("attempt",null,"UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Available',\"ReservationRevision\"=\"ReservationRevision\"+1,\"CurrentEncryptionAttemptId\"=pg_catalog.gen_random_uuid() WHERE \"SourceArtifactId\"=@id"),
            ("fence",null,"UPDATE tagekyc.raw_export_source_head SET \"CustodyState\"='Available',\"ReservationRevision\"=\"ReservationRevision\"+1,\"Fence\"=\"Fence\"+1 WHERE \"SourceArtifactId\"=@id")
        };
        foreach(var scenario in cases)
        {
            await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
            await using var transaction=await connection.BeginTransactionAsync();
            if(scenario.Setup is not null)
            {
                await using var disable=new NpgsqlCommand("ALTER TABLE tagekyc.raw_export_source_head DISABLE TRIGGER USER",connection,transaction);await disable.ExecuteNonQueryAsync();
                await using var setup=new NpgsqlCommand(scenario.Setup,connection,transaction);setup.Parameters.AddWithValue("id",sourceArtifactId);await setup.ExecuteNonQueryAsync();
                await using var enable=new NpgsqlCommand("ALTER TABLE tagekyc.raw_export_source_head ENABLE TRIGGER USER",connection,transaction);await enable.ExecuteNonQueryAsync();
            }
            await using var command=new NpgsqlCommand("SET LOCAL ROLE tagekyc_raw_export_deployer; SELECT pg_catalog.set_config('tagekyc.raw_export_source_core_write_context','tip88c1-r5-publish-v1',true); "+scenario.Update,connection,transaction);
            command.Parameters.AddWithValue("id",sourceArtifactId);
            var error=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Equal("P0001",error.SqlState);Assert.Equal("RAW_EXPORT_SOURCE_HEAD_WRITE_FORBIDDEN",error.MessageText);
            await transaction.RollbackAsync();
        }
    }

    private async Task AssertRequiredColumnsAsync(string table,string trigger,string idColumn,Guid id,params string[] columns)
    {
        foreach(var column in columns)
        {
            await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
            await using var transaction=await connection.BeginTransactionAsync();
            await using(var disable=new NpgsqlCommand($"ALTER TABLE tagekyc.{table} DISABLE TRIGGER {trigger}",connection,transaction))
                await disable.ExecuteNonQueryAsync();
            await using var command=new NpgsqlCommand($"UPDATE tagekyc.{table} SET \"{column}\"=NULL WHERE \"{idColumn}\"=@id",connection,transaction);
            command.Parameters.AddWithValue("id",id);
            var error=await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.NotNullViolation,error.SqlState);
            Assert.Equal(column,error.ColumnName);
            await transaction.RollbackAsync();
        }
    }

    private async Task<string> FunctionDefinitionAsync(string signature)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("SELECT pg_catalog.pg_get_functiondef(@signature::pg_catalog.regprocedure)",connection);
        command.Parameters.AddWithValue("signature",signature);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static string NormalizeSql(string sql)=>Regex.Replace(sql,@"\s+"," ").Trim();

    private static string ScalarSnapshot(object value) => string.Join("|",value.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public)
        .Where(x=>x.CanRead).OrderBy(x=>x.Name,StringComparer.Ordinal).Select(x=>$"{x.Name}={FormatScalar(x.GetValue(value))}"));
    private static string FormatScalar(object? value) => value switch
    {
        null=>"<null>", byte[] bytes=>Convert.ToHexString(bytes), DateTimeOffset instant=>instant.ToUniversalTime().ToString("O",System.Globalization.CultureInfo.InvariantCulture),
        _=>Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture)??"<null>"
    };

    private async Task InjectCleanupItemAsync(Guid cleanupItemId,Guid publicationId,string kind,Guid resourceId,Guid attemptId,long revision)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            SET ROLE tagekyc_raw_export_deployer;
            SELECT pg_catalog.set_config('tagekyc.raw_export_source_finalization_write_context','tip88c1-r5-publish-v1',false);
            INSERT INTO tagekyc.raw_export_source_cleanup_items
              ("CleanupItemId","SourcePublicationId","ResourceKind","ResourceId","ResourceAttemptId","PlannedResourceRevision","CleanupState","CompletionDisposition","CleanupEvidenceDigest","CompletedAtUtc","RowRevision","SchemaVersion")
            VALUES(@item,@publication,@kind,@resource,@attempt,@revision,'Pending',NULL,NULL,NULL,1,1);
            """,connection);
        command.Parameters.AddWithValue("item",cleanupItemId);command.Parameters.AddWithValue("publication",publicationId);
        command.Parameters.AddWithValue("kind",kind);command.Parameters.AddWithValue("resource",resourceId);
        command.Parameters.AddWithValue("attempt",attemptId);command.Parameters.AddWithValue("revision",revision);
        await command.ExecuteNonQueryAsync();
    }

    private async Task RemoveInjectedCleanupItemAsync(Guid cleanupItemId)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            ALTER TABLE tagekyc.raw_export_source_cleanup_items DISABLE TRIGGER USER;
            DELETE FROM tagekyc.raw_export_source_cleanup_items WHERE "CleanupItemId"=@item;
            ALTER TABLE tagekyc.raw_export_source_cleanup_items ENABLE TRIGGER USER;
            """,connection);
        command.Parameters.AddWithValue("item",cleanupItemId);await command.ExecuteNonQueryAsync();
    }

    private TagEkycDbContext CreateNamedDb(string applicationName)
    {
        var builder=new NpgsqlConnectionStringBuilder(postgres.ConnectionString){ApplicationName=applicationName};
        return new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(builder.ConnectionString).Options);
    }

    private async Task<RowBlock> BeginRowBlockAsync(string applicationName,string table,string idColumn,Guid id,bool waitForLock=true)
    {
        if(table is not ("raw_export_source_head" or "raw_export_source_encryption_attempts" or "raw_export_provisional_objects" or "raw_export_attempt_key_reservations")) throw new ArgumentOutOfRangeException(nameof(table));
        var connectionString=new NpgsqlConnectionStringBuilder(postgres.ConnectionString){ApplicationName=applicationName}.ConnectionString;
        var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();
        var transaction=await connection.BeginTransactionAsync();
        var command=new NpgsqlCommand($"SELECT 1 FROM tagekyc.{table} WHERE \"{idColumn}\"=@id FOR UPDATE",connection,transaction);
        command.Parameters.AddWithValue("id",id);var pending=command.ExecuteNonQueryAsync();
        if(waitForLock) await pending;
        return new(connection,transaction,command,pending);
    }

    private async Task WaitForBlockedByAsync(string waitingApplication,string blockerApplication)
    {
        for(var attempt=0;attempt<200;attempt++)
        {
            await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
            await using var command=new NpgsqlCommand("""
                SELECT EXISTS(
                  SELECT 1 FROM pg_catalog.pg_stat_activity waiting
                  JOIN pg_catalog.pg_stat_activity blocker ON blocker.application_name=@blocker
                  WHERE waiting.application_name=@waiting AND waiting.wait_event_type='Lock'
                    AND blocker.pid=ANY(pg_catalog.pg_blocking_pids(waiting.pid)))
                """,connection);
            command.Parameters.AddWithValue("waiting",waitingApplication);command.Parameters.AddWithValue("blocker",blockerApplication);
            if((bool)(await command.ExecuteScalarAsync())!) return;
            await Task.Delay(50);
        }
        throw new TimeoutException($"{waitingApplication} was not blocked by {blockerApplication}.");
    }

    private async Task AssertPublishExpiresWhileBlockedAsync(bool authorityLock)
    {
        var marker=authorityLock?"authority":"consent";
        var lifetime=TimeSpan.FromSeconds(10);
        var (source,staged,committed)=await CreateCommittedAsync(System.Text.Encoding.UTF8.GetBytes("f405-"+marker),lifetime,lifetime);
        var blocker=await BeginAdmissionBlockAsync(source,authorityLock,"f405-"+marker+"-blocker");
        await using(blocker.Connection) await using(blocker.Transaction)
        {
            var publish=PublishAsync(source,staged,committed,applicationName:"f405-"+marker+"-publish");
            await WaitForBlockedByAsync("f405-"+marker+"-publish","f405-"+marker+"-blocker");
            var wait=source.AbsoluteSourceExpiresAtUtc-DateTimeOffset.UtcNow+TimeSpan.FromMilliseconds(150);
            if(wait>TimeSpan.Zero) await Task.Delay(wait);
            await blocker.Transaction.CommitAsync();
            Assert.Equal(SourcePublishDisposition.SourceRetentionNotAuthorized,(await publish.WaitAsync(TimeSpan.FromSeconds(20))).Disposition);
        }
        await using var verify=postgres.CreateDbContext();
        var publication=await verify.RawExportSourcePublications.AsNoTracking().SingleAsync(x=>x.SourcePublicationId==committed.SourcePublicationId);
        Assert.Equal("Committed",publication.PublicationState);Assert.Null(publication.AvailableAtUtc);
        Assert.Empty(await verify.RawExportSourceCleanupItems.Where(x=>x.SourcePublicationId==committed.SourcePublicationId).ToListAsync());
    }

    private async Task AssertCommitExpiresWhileBlockedAsync(bool authorityLock)
    {
        var marker=authorityLock?"authority":"consent";
        var lifetime=TimeSpan.FromSeconds(10);
        var (source,staged)=await CreateStagedAsync(System.Text.Encoding.UTF8.GetBytes("f402-"+marker),lifetime,lifetime);
        var blocker=await BeginAdmissionBlockAsync(source,authorityLock,"f402-"+marker+"-blocker");
        await using(blocker.Connection) await using(blocker.Transaction)
        {
            var commit=Task.Run(async()=>
            {
                await using var db=CreateNamedDb("f402-"+marker+"-commit");
                return await new RawExportSourceFinalizationService(db).CommitAsync(new(source.ActorPrincipalId,source.AttemptId,
                    staged.ReservationRevision!.Value,source.EncryptionAttemptRevision,source.Fence,source.ObjectStateRevision));
            });
            await WaitForBlockedByAsync("f402-"+marker+"-commit","f402-"+marker+"-blocker");
            var wait=source.AbsoluteSourceExpiresAtUtc-DateTimeOffset.UtcNow+TimeSpan.FromMilliseconds(150);
            if(wait>TimeSpan.Zero) await Task.Delay(wait);
            await blocker.Transaction.CommitAsync();
            Assert.Equal(SourceCommitDisposition.SourceRetentionNotAuthorized,(await commit.WaitAsync(TimeSpan.FromSeconds(20))).Disposition);
        }
        await using var verify=postgres.CreateDbContext();
        Assert.Empty(await verify.RawExportSourcePublications.Where(x=>x.AttemptId==source.AttemptId).ToListAsync());
    }

    private async Task<RowBlock> BeginAdmissionBlockAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source,bool authorityLock,string applicationName)
    {
        var connectionString=new NpgsqlConnectionStringBuilder(postgres.ConnectionString){ApplicationName=applicationName}.ConnectionString;
        var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();var transaction=await connection.BeginTransactionAsync();
        var sql=authorityLock
            ? "SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtext('tip88c1:b2-authority:'||@client::text||':'||@session::text||':'||@acceptance::text||':'||@class))"
            : "SELECT pg_catalog.pg_advisory_xact_lock(tagekyc.raw_export_consent_lock_key(tagekyc.raw_export_consent_scope_hash(@session,@subject,@policy,@version,'SubjectRawBiometricExport',@client)))";
        var command=new NpgsqlCommand(sql,connection,transaction);command.Parameters.AddWithValue("client",source.ClientApplicationId);
        command.Parameters.AddWithValue("session",source.VerificationSessionId);command.Parameters.AddWithValue("acceptance",source.CaptureAcceptanceId);
        command.Parameters.AddWithValue("class",source.RawClass);command.Parameters.AddWithValue("subject",source.SubjectRef);
        command.Parameters.AddWithValue("policy",source.ConsentPolicyId);command.Parameters.AddWithValue("version",source.ConsentPolicyVersion);
        var pending=command.ExecuteNonQueryAsync();await pending;return new(connection,transaction,command,pending);
    }

    private async Task MakeKeyProviderOutcomeUnknownAsync(Guid reservationId,bool expirePreparationLease=false)
    {
        await using var connection=new NpgsqlConnection(postgres.ConnectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            ALTER TABLE tagekyc.raw_export_attempt_key_reservations DISABLE TRIGGER USER;
            ALTER TABLE tagekyc.raw_export_key_provider_operations DISABLE TRIGGER USER;
            UPDATE tagekyc.raw_export_key_provider_operations SET
              "ProviderOperationState"='Issued',"WrappedDekCiphertext"=NULL,"WrappedDekNonce"=NULL,"WrappedDekTag"=NULL,
              "WrappedDekMetadataDigest"=NULL,"WrappingSuiteId"=NULL,"WrappingSuiteVersion"=NULL,
              "ProviderResourceReference"=NULL,"ProviderOperationReceipt"=NULL,"ResultObservedAtUtc"=NULL,
              "ProviderCleanupReference"=NULL,"ProviderCleanupReceipt"=NULL,"ProviderAbsenceProofReceipt"=NULL,
              "UpdatedAtUtc"=pg_catalog.clock_timestamp()
            WHERE "AttemptKeyReservationId"=@reservation;
            UPDATE tagekyc.raw_export_attempt_key_reservations SET
              "PreparationDisposition"='ProviderOutcomeUnknown',"ResolutionAttemptCount"=1,
              "CurrentPreparationLeaseExpiresAtUtc"=CASE WHEN @expire_lease THEN pg_catalog.clock_timestamp()-interval '1 hour' ELSE "CurrentPreparationLeaseExpiresAtUtc" END,
              "NextResolutionAttemptNotBeforeUtc"=pg_catalog.clock_timestamp()-interval '1 minute',
              "ResolutionDeadlineUtc"=pg_catalog.clock_timestamp()+interval '24 hours',
              "CleanupAttemptCount"=0,"NextCleanupAttemptNotBeforeUtc"=NULL,"CleanupDeadlineUtc"=NULL,
              "CleanupOperatorInterventionRequired"=false,"WrappedDekCiphertext"=NULL,"WrappedDekNonce"=NULL,
              "WrappedDekTag"=NULL,"WrappedDekMetadataDigest"=NULL,"PreparedAtUtc"=NULL,"RevokedAtUtc"=NULL,
              "RevocationReasonCode"=NULL,"RowRevision"="RowRevision"+1,"UpdatedAtUtc"=pg_catalog.clock_timestamp()
            WHERE "AttemptKeyReservationId"=@reservation;
            ALTER TABLE tagekyc.raw_export_key_provider_operations ENABLE TRIGGER USER;
            ALTER TABLE tagekyc.raw_export_attempt_key_reservations ENABLE TRIGGER USER;
            """,connection);
        command.Parameters.AddWithValue("reservation",reservationId);command.Parameters.AddWithValue("expire_lease",expirePreparationLease);await command.ExecuteNonQueryAsync();
    }
    private static void AssertAbsoluteEvidenceVectors()
    {
        static Guid G(char c)=>Guid.ParseExact(new string(c,32),"N");
        var timestamp=DateTimeOffset.Parse("2026-08-12T00:00:00.123456Z",System.Globalization.CultureInfo.InvariantCulture);
        var staged=Enumerable.Range(0,32).Select(i=>(byte)i).ToArray();
        var objectDigest=Enumerable.Range(0xA0,32).Select(i=>(byte)i).ToArray();
        var keyDigest=Enumerable.Range(0xC0,32).Select(i=>(byte)i).ToArray();
        var commit=RawExportSourceFinalizationEvidenceCodec.ComputeCommit(new(G('1'),G('2'),G('3'),G('4'),2,staged,1,G('7'),9,G('8'),1,timestamp));
        Assert.Equal("2df9964cfb6d0148248d9c893022607737c06d687075f9037ac262aa37ef3dba",Convert.ToHexString(commit).ToLowerInvariant());
        var available=RawExportSourceFinalizationEvidenceCodec.ComputeAvailable(new(G('5'),G('1'),G('2'),G('3'),G('4'),G('6'),commit,1,G('7'),9,G('8'),1,timestamp));
        Assert.Equal("46186c52cf2101d6cb12aab53a946aba1148e25dda4e49d81b82b6514309e2de",Convert.ToHexString(available).ToLowerInvariant());
        var cleanup=RawExportSourceFinalizationEvidenceCodec.ComputeCleanup(new(G('5'),G('1'),"NoObsoleteResidue",0,timestamp));
        Assert.Equal("5b3899bc0678e04876dabf2647dc64281bbf6f053e8e828891fed38e77116c4f",Convert.ToHexString(cleanup).ToLowerInvariant());
        var objectItem=RawExportSourceFinalizationEvidenceCodec.ComputeObjectItem(new(G('5'),G('9'),G('a'),"Deleted",objectDigest,timestamp));
        Assert.Equal("7cbdb5fe1a882fc150399f612005e6786854c1ae587238110f9894af362e8d92",Convert.ToHexString(objectItem).ToLowerInvariant());
        var keyItem=RawExportSourceFinalizationEvidenceCodec.ComputeKeyItem(new(G('5'),G('9'),G('b'),"Revoked",keyDigest,timestamp));
        Assert.Equal("a1c47ba47a3d8c02d928df4c2fb737ad399b5971698c0fe666296b95836505ed",Convert.ToHexString(keyItem).ToLowerInvariant());
    }
    private static string RepoRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"TagEkyc.sln")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException();}
    private sealed record ObsoleteResidueFixture(Guid AttemptId,Guid AttemptKeyReservationId,Guid ObjectCustodyId);
    private sealed class FixedLifecycle(ExactDeleteOutcome outcome,int? statusCode):IProvisionalObjectLifecycle
    {
        internal bool Called { get; private set; }
        public Task<ExactDeleteResult> DeleteExactAsync(ExactObjectLocator locator,CancellationToken cancellationToken)
        { Called=true; Assert.NotEqual(Guid.Empty,locator.ProvisionalObjectIdentity); Assert.Equal(32,locator.ObjectBindingDigest.Length); return Task.FromResult(new ExactDeleteResult(outcome,statusCode)); }
    }
    private sealed class FixedReconciler(params ExactObjectInspectionOutcome[] outcomes):IProvisionalObjectReconciler
    {
        private readonly Queue<ExactObjectInspectionOutcome> remaining=new(outcomes);
        public Task<ExactObjectInspection> InspectExactAsync(ExactObjectLocator locator,CancellationToken cancellationToken)
        { Assert.NotEqual(Guid.Empty,locator.ProvisionalObjectIdentity);var outcome=remaining.Count>0?remaining.Dequeue():ExactObjectInspectionOutcome.Indeterminate;return Task.FromResult(new ExactObjectInspection(outcome,null,null,null,outcome==ExactObjectInspectionOutcome.PositivelyAbsent?404:null)); }
        public Task<ExactObjectRead> OpenExactReadAsync(ExactObjectLocator locator,CancellationToken cancellationToken)=>throw new NotSupportedException();
    }

    private sealed class FixedRecovery(KekProvisioningResolution? resolution=null,KekProvisioningCleanupResult? cleanup=null):IKekProvisioningRecoveryOperation
    {
        internal int ResolveCallCount { get; private set; }
        internal int CleanupCallCount { get; private set; }
        public Task<KekProvisioningResolution> ResolveProvisioningOperationAsync(ProviderOperationToken providerOperationToken,ReadOnlyMemory<byte> attemptKeyContextFingerprint,CancellationToken cancellationToken)
        { ResolveCallCount++; return Task.FromResult(resolution??new KekProvisioningResolution.ProviderResourceCleanupRequired("fixture-cleanup-reference")); }
        public Task<KekProvisioningCleanupResult> CleanupProvisioningOperationAsync(string providerCleanupReference,ReadOnlyMemory<byte> attemptKeyContextFingerprint,CancellationToken cancellationToken)
        { CleanupCallCount++; return Task.FromResult(cleanup??new KekProvisioningCleanupResult.Cleaned("fixture-cleanup-receipt")); }
    }

    private sealed record RowBlock(NpgsqlConnection Connection,NpgsqlTransaction Transaction,NpgsqlCommand Command,Task<int> PendingLock);
}
