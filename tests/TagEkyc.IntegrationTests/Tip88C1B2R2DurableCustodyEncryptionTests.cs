using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

public sealed class Tip88C1B2R2DurableCustodyEncryptionTests
{
    [Fact]
    public void R202_frame_codec_golden_vectors_are_absolute_and_independently_recomputed()
    {
        var attempt = Guid.Parse("00010203-0405-0607-0809-0a0b0c0d0e0f");
        var keyReservation = Guid.Parse("10111213-1415-1617-1819-1a1b1c1d1e1f");
        var source = Guid.Parse("20212223-2425-2627-2829-2a2b2c2d2e2f");
        var objectIdentity = Guid.Parse("30313233-3435-3637-3839-3a3b3c3d3e3f");
        var fingerprint = Bytes(0x40, 32);
        var binding = Bytes(0x60, 32);
        var framing = Bytes(0x80, 32);
        var wrapped = Bytes(0x00, 32);
        var commitment = Bytes(0xe0, 32);
        var dek = Bytes(0xc0, 32);
        var dataNonce = Convert.FromHexString("00a1a2a3a4a5a6a7a8a9aaab");
        var finalNonce = Convert.FromHexString("01b1b2b3b4b5b6b7b8b9babb");
        var header = new RawExportR2Header(
            attempt, keyReservation, source, objectIdentity, fingerprint, binding,
            "AES-256-GCM", 1, 4, "Random96", framing);
        var headerBytes = RawExportR2FrameCodec.SerializeHeader(header);
        var headerDigest = SHA256.HashData(headerBytes);
        Assert.Equal(208, headerBytes.Length);
        Assert.Equal(
            "b5aa36aec08187300564247f29e6f04605021861f68fa5f935a5a43fe0729e10",
            Hex(headerDigest));

        var plaintext = "abc"u8.ToArray();
        var dataAad = RawExportR2FrameCodec.CreateDataAad(fingerprint, headerDigest, 0, 3);
        var dataCiphertext = new byte[3];
        var dataTag = new byte[16];
        using (var aes = new AesGcm(dek, 16))
            aes.Encrypt(dataNonce, plaintext, dataCiphertext, dataTag, dataAad);
        Assert.Equal("82b28a", Hex(dataCiphertext));
        Assert.Equal("31639cf9eb1c50489f9a2980da8044bf", Hex(dataTag));
        var dataFrame = RawExportR2FrameCodec.SerializeDataFrame(
            0, 3, dataNonce, dataCiphertext, dataTag);
        var dataDigest = SHA256.HashData(dataFrame);
        Assert.Equal(
            "1c5f69573a7e8599a420a949340ef52642bd2bf7fb3fbbeaeb462a2c28a95024",
            Hex(dataDigest));
        var chain = RawExportR2FrameCodec.NextAuthenticationChunkCommitment(
            RawExportR2FrameCodec.InitialAuthenticationChunkCommitment(headerDigest), 0, dataTag);
        Assert.Equal(
            "33210073ab93895bd564de97ad5ac6d1a78f6351a9ce86cd7a7e9684b1bdd4f6",
            Hex(chain));
        var envelope = RawExportR2FrameCodec.ComputeEnvelopeMetadataDigest(
            attempt, keyReservation, "fixture-provider", "fixture-kek", 1,
            "fixture-fingerprint", "AES-256-GCM", 1, 4, "Random96", framing, wrapped);
        Assert.Equal(
            "aac48eda26c784318ef49cd1a6ca9ee901eaf1a76e1d0d32c88e13a73854f338",
            Hex(envelope));

        var completion = RawExportR2FrameCodec.SerializeCompletion(new(
            1, 3, (ulong)dataFrame.Length, dataDigest, commitment, chain, envelope, wrapped));
        Assert.Equal(181, completion.Length);
        var finalAad = RawExportR2FrameCodec.CreateFinalAad(fingerprint, headerDigest, 1, 3);
        var finalCiphertext = new byte[completion.Length];
        var finalTag = new byte[16];
        using (var aes = new AesGcm(dek, 16))
            aes.Encrypt(finalNonce, completion, finalCiphertext, finalTag, finalAad);
        Assert.Equal(
            "71496650f485d2a4741e8fb02c6060a50a5b4b863fb16774061b7e6ff7e48deb83453ccacee26637e984b4352134935aa300d1e41af5bfc6ebef0d94cc9cc808cf153baadf01428dc56d9aec657f132cab0839793009b9bed3cea2363120fbab7c61ab11ecc84b04b38fc1da6827a0578b745b7e6f4eef00cc1e76f67cb52d5b0926a7f2cc9d6dfb42a099a170b96fdd662c9fa7847fbb3e2108d288f56ee2ef4db41cb5cf22ab63d4c400c5ca7cc123a2ca9a332b",
            Hex(finalCiphertext));
        Assert.Equal("6275f608220e9b58e6c881a56ed604b3", Hex(finalTag));

        var finalFrame = RawExportR2FrameCodec.SerializeFinalFrame(finalNonce, finalCiphertext, finalTag);
        var completeObject = headerBytes.Concat(dataFrame).Concat(finalFrame).ToArray();
        Assert.Equal(462, completeObject.Length);
        Assert.Equal(
            "e2bdff09933146098393a45537023e0e7460fe396a9d6fed078d8768c4fc9c06",
            Hex(SHA256.HashData(completeObject)));

        var independent = IndependentHeader(
            attempt, keyReservation, source, objectIdentity, fingerprint, binding,
            "AES-256-GCM", 1, 4, "Random96", framing);
        Assert.Equal(headerBytes, independent);
    }

    [Fact]
    public async Task R203_zero_one_and_multiple_chunk_shapes_have_exact_length_and_eof()
    {
        var header = CodecHeader(chunkSize: 4);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RawExportR2FrameCodec.CiphertextLength(0, header));
        foreach (var plaintext in new[] { "abc"u8.ToArray(), "abcdefghi"u8.ToArray() })
        {
            var context = Context(plaintext.Length, SHA256.HashData(plaintext)) with { ChunkSize = 4 };
            await using var stream = new RawExportR2FramedCiphertextStream(
                new MemoryStream(plaintext, writable: false), context, Bytes(0x60, 32),
                new FixtureAead(), new EchoCommitment(context.ContentCommitment));
            var encoded = await ReadAllAsync(stream);
            Assert.Equal(RawExportR2FrameCodec.CiphertextLength(plaintext.Length, stream.Header), encoded.Length);
            AssertExactFrameShape(encoded, stream.Header, plaintext.Length);
            Assert.True(stream.Completed);
        }
        Assert.Throws<RawExportR2DeterministicInvalidException>(() =>
            RawExportR2FrameCodec.ParseCompletion(new byte[180]));
    }

    [Fact]
    public async Task R204_data_and_final_nonce_namespaces_are_disjoint_and_duplicates_fail_closed()
    {
        var plaintext = "synthetic-r2"u8.ToArray();
        var context = Context(plaintext.Length, SHA256.HashData(plaintext));
        await using var stream = new RawExportR2FramedCiphertextStream(
            new MemoryStream(plaintext, writable: false), context, Bytes(0x60, 32),
            new FixtureAead(), new EchoCommitment(context.ContentCommitment));
        var bytes = await ReadAllAsync(stream);
        var headerLength = RawExportR2FrameCodec.HeaderLength(stream.Header);
        Assert.Equal(0x00, bytes[headerLength + 9]);
        var finalOffset = headerLength + 37 + plaintext.Length;
        Assert.Equal(RawExportR2FrameCodec.FinalFrameType, bytes[finalOffset]);
        Assert.Equal(0x01, bytes[finalOffset + 1]);
        Assert.NotEqual(
            bytes.AsSpan(headerLength + 10, 11).ToArray(),
            bytes.AsSpan(finalOffset + 2, 11).ToArray());

        var multiChunkPlaintext = "abcdefgh"u8.ToArray();
        var duplicateContext = Context(
            multiChunkPlaintext.Length,
            SHA256.HashData(multiChunkPlaintext)) with { ChunkSize = 4 };
        var callerOwnedSource = new MemoryStream(multiChunkPlaintext, writable: false);
        await using var duplicateStream = new RawExportR2FramedCiphertextStream(
            callerOwnedSource,
            duplicateContext,
            Bytes(0x60, 32),
            new FixtureAead(),
            new EchoCommitment(duplicateContext.ContentCommitment),
            count => Enumerable.Repeat((byte)0x5a, count).ToArray());
        await Assert.ThrowsAsync<RawExportR2DeterministicInvalidException>(async () =>
            await ReadAllAsync(duplicateStream));
        Assert.False(duplicateStream.Completed);
        Assert.True(callerOwnedSource.CanRead);
    }

    [Fact]
    public async Task R205_historic_commitment_must_match_before_final_frame_is_emitted()
    {
        var plaintext = "synthetic-r2"u8.ToArray();
        var context = Context(plaintext.Length, Bytes(0xaa, 32));
        await using var stream = new RawExportR2FramedCiphertextStream(
            new MemoryStream(plaintext, writable: false), context, Bytes(0x60, 32),
            new FixtureAead(), new EchoCommitment(Bytes(0xbb, 32)));
        await Assert.ThrowsAsync<RawExportR2DeterministicInvalidException>(async () =>
            await ReadAllAsync(stream));
        Assert.False(stream.Completed);
    }

    [Fact]
    public void R206_transient_plaintext_digest_is_not_returned_persisted_or_logged()
    {
        Assert.DoesNotContain(typeof(RawExportR2WriterResult).GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(RawExportR2VerifierResult).GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.Name.Contains("PlaintextDigest", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("digest", new RawExportR2WriterResult(
            Guid.NewGuid(), null, null, null,
            RawExportR2WriterDisposition.PreCustodyRejected).ToString(),
            StringComparison.OrdinalIgnoreCase);

        var helper = typeof(RawExportR2FrameCodec).GetMethod(
            "BuildHistoricCommitmentPayload",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(helper);
        var digestParameter = Assert.Single(
            helper.GetParameters(),
            parameter => parameter.Name == "plaintextDigest");
        Assert.Equal(typeof(ReadOnlySpan<byte>), digestParameter.ParameterType);

        var encryptMoveNext = R206AsyncMoveNext(
            typeof(RawExportR2FramedCiphertextStream),
            "BuildFinalFrameAsync");
        var verifyMoveNext = R206AsyncMoveNext(
            typeof(RawExportR2CompletionVerifier),
            "VerifyFramesAsync");
        var encryptCalls = R206ResolvedCalls(encryptMoveNext);
        var verifyCalls = R206ResolvedCalls(verifyMoveNext);
        var helperCalls = R206ResolvedCalls(helper);

        R206AssertDigestPath("ENCRYPT", encryptCalls, helper);
        R206AssertDigestPath("VERIFY", verifyCalls, helper);
        R206AssertNoManagedDigestText("ENCRYPT", encryptCalls);
        R206AssertNoManagedDigestText("VERIFY", verifyCalls);
        R206AssertNoManagedDigestText("HISTORIC_HELPER", helperCalls);
    }

    [Fact]
    public async Task R214_r2_owned_buffers_are_zeroized_and_source_stream_remains_caller_owned()
    {
        var sourceFailurePlaintext = "r214-source-failure"u8.ToArray();
        var sourceFailure = new R214ThrowingSource(sourceFailurePlaintext);
        var sourceFailureStream = new RawExportR2FramedCiphertextStream(
            sourceFailure,
            Context(sourceFailurePlaintext.Length, SHA256.HashData(sourceFailurePlaintext)),
            Bytes(0x60, 32),
            new CapturingAead(),
            new CapturingCommitment(Bytes(0x90, 32)),
            count => Enumerable.Range(1, count).Select(value => (byte)value).ToArray());
        var sourceException = await Assert.ThrowsAsync<R214SentinelException>(
            () => ReadAllAsync(sourceFailureStream));
        Assert.Equal(R214ThrowingSource.Sentinel, sourceException.Message);
        Assert.Equal(1, sourceFailure.CallCount);
        Assert.NotNull(sourceFailure.Destination);
        sourceFailure.Destination!.AssertWasNonZero();
        sourceFailure.Destination.AssertAllZero();
        var sourceFailureLongLived = R214RetainLongLivedScratch(sourceFailureStream);
        await sourceFailureStream.DisposeAsync();
        Assert.All(sourceFailureLongLived, retained => retained.AssertAllZero());
        R214AssertCallerOwnership(sourceFailure, sourceFailurePlaintext[0]);
        sourceFailure.Dispose();

        var aeadFailurePlaintext = "r214-aead-failure"u8.ToArray();
        var aeadFailureSource = new R214CallerOwnedStream(aeadFailurePlaintext);
        var throwingAead = new R214CapturingAead(throwOnCall: 1);
        var aeadFailureRandom = new R214RandomBytes();
        var aeadFailureStream = new RawExportR2FramedCiphertextStream(
            aeadFailureSource,
            Context(aeadFailurePlaintext.Length, SHA256.HashData(aeadFailurePlaintext)),
            Bytes(0x60, 32),
            throwingAead,
            new CapturingCommitment(Bytes(0x90, 32)),
            aeadFailureRandom.GetBytes);
        var aeadException = await Assert.ThrowsAsync<R214SentinelException>(
            () => ReadAllAsync(aeadFailureStream));
        Assert.Equal(R214CapturingAead.Sentinel, aeadException.Message);
        Assert.Equal(1, throwingAead.CallCount);
        Assert.Equal(
            new[] { "DATA plaintext", "DATA nonce", "DATA AAD" },
            throwingAead.Retained.Select(retained => retained.Family));
        Assert.All(throwingAead.Retained, retained => retained.AssertWasNonZero());
        Assert.All(throwingAead.Retained, retained => retained.AssertAllZero());
        Assert.All(aeadFailureRandom.Returned, retained => retained.AssertWasNonZero());
        Assert.All(aeadFailureRandom.Returned, retained => retained.AssertAllZero());
        var aeadFailureLongLived = R214RetainLongLivedScratch(aeadFailureStream);
        await aeadFailureStream.DisposeAsync();
        Assert.All(aeadFailureLongLived, retained => retained.AssertAllZero());
        R214AssertCallerOwnership(aeadFailureSource, aeadFailurePlaintext[0]);
        aeadFailureSource.Dispose();

        var commitmentFailurePlaintext = "r214-commitment-failure"u8.ToArray();
        var commitmentFailureSource = new R214CallerOwnedStream(commitmentFailurePlaintext);
        var commitmentFailureAead = new R214CapturingAead();
        var commitmentFailureRandom = new R214RandomBytes();
        var throwingCommitment = new R214CapturingCommitment(
            Bytes(0x90, 32),
            throwOnCall: true);
        var commitmentFailureStream = new RawExportR2FramedCiphertextStream(
            commitmentFailureSource,
            Context(
                commitmentFailurePlaintext.Length,
                SHA256.HashData(commitmentFailurePlaintext)),
            Bytes(0x60, 32),
            commitmentFailureAead,
            throwingCommitment,
            commitmentFailureRandom.GetBytes);
        var commitmentException = await Assert.ThrowsAsync<R214SentinelException>(
            () => ReadAllAsync(commitmentFailureStream));
        Assert.Equal(R214CapturingCommitment.Sentinel, commitmentException.Message);
        Assert.Equal(1, throwingCommitment.CallCount);
        Assert.NotNull(throwingCommitment.Payload);
        throwingCommitment.Payload!.AssertWasNonZero();
        throwingCommitment.Payload.AssertAllZero();
        Assert.All(commitmentFailureAead.Retained, retained => retained.AssertWasNonZero());
        Assert.All(commitmentFailureAead.Retained, retained => retained.AssertAllZero());
        Assert.All(commitmentFailureRandom.Returned, retained => retained.AssertWasNonZero());
        Assert.All(commitmentFailureRandom.Returned, retained => retained.AssertAllZero());
        var commitmentFailureLongLived = R214RetainLongLivedScratch(commitmentFailureStream);
        await commitmentFailureStream.DisposeAsync();
        Assert.All(commitmentFailureLongLived, retained => retained.AssertAllZero());
        R214AssertCallerOwnership(commitmentFailureSource, commitmentFailurePlaintext[0]);
        commitmentFailureSource.Dispose();

        var finalFailurePlaintext = "r214-final-aead-failure"u8.ToArray();
        var finalFailureSource = new R214CallerOwnedStream(finalFailurePlaintext);
        var finalFailureAead = new R214CapturingAead(throwOnCall: 2);
        var finalFailureRandom = new R214RandomBytes();
        var finalFailureCommitment = new R214CapturingCommitment(
            SHA256.HashData(finalFailurePlaintext));
        var finalFailureStream = new RawExportR2FramedCiphertextStream(
            finalFailureSource,
            Context(finalFailurePlaintext.Length, SHA256.HashData(finalFailurePlaintext)),
            Bytes(0x60, 32),
            finalFailureAead,
            finalFailureCommitment,
            finalFailureRandom.GetBytes);
        var finalException = await Assert.ThrowsAsync<R214SentinelException>(
            () => ReadAllAsync(finalFailureStream));
        Assert.Equal(R214CapturingAead.Sentinel, finalException.Message);
        Assert.Equal(2, finalFailureAead.CallCount);
        Assert.Contains(finalFailureAead.Retained, retained => retained.Family == "FINAL plaintext");
        Assert.Contains(finalFailureAead.Retained, retained => retained.Family == "FINAL nonce");
        Assert.Contains(finalFailureAead.Retained, retained => retained.Family == "FINAL AAD");
        Assert.All(finalFailureAead.Retained, retained => retained.AssertWasNonZero());
        Assert.All(finalFailureAead.Retained, retained => retained.AssertAllZero());
        Assert.NotNull(finalFailureCommitment.Payload);
        finalFailureCommitment.Payload!.AssertWasNonZero();
        finalFailureCommitment.Payload.AssertAllZero();
        Assert.All(finalFailureRandom.Returned, retained => retained.AssertWasNonZero());
        Assert.All(finalFailureRandom.Returned, retained => retained.AssertAllZero());
        var finalFailureLongLived = R214RetainLongLivedScratch(finalFailureStream);
        await finalFailureStream.DisposeAsync();
        Assert.All(finalFailureLongLived, retained => retained.AssertAllZero());
        R214AssertCallerOwnership(finalFailureSource, finalFailurePlaintext[0]);
        finalFailureSource.Dispose();

        var frameScratchPlaintext = "r214-frame-scratch"u8.ToArray();
        var frameScratchSource = new R214CallerOwnedStream(frameScratchPlaintext);
        var frameScratchContext = Context(
            frameScratchPlaintext.Length,
            SHA256.HashData(frameScratchPlaintext));
        var frameScratchStream = new RawExportR2FramedCiphertextStream(
            frameScratchSource,
            frameScratchContext,
            Bytes(0x60, 32),
            new R214CapturingAead(),
            new R214CapturingCommitment(frameScratchContext.ContentCommitment),
            new R214RandomBytes().GetBytes);
        var frameReadBuffer = new byte[checked((int)frameScratchStream.Length)];
        Assert.True(await frameScratchStream.ReadAsync(frameReadBuffer) > 0);
        var headerFrameScratch = R214RetainCurrentFrame(frameScratchStream, "header frame scratch");
        headerFrameScratch.AssertWasNonZero();
        Assert.True(await frameScratchStream.ReadAsync(frameReadBuffer) > 0);
        headerFrameScratch.AssertAllZero();
        var dataFrameScratch = R214RetainCurrentFrame(frameScratchStream, "DATA frame scratch");
        dataFrameScratch.AssertWasNonZero();
        Assert.True(await frameScratchStream.ReadAsync(frameReadBuffer) > 0);
        dataFrameScratch.AssertAllZero();
        var finalFrameScratch = R214RetainCurrentFrame(frameScratchStream, "FINAL frame scratch");
        finalFrameScratch.AssertWasNonZero();
        Assert.Equal(0, await frameScratchStream.ReadAsync(frameReadBuffer));
        finalFrameScratch.AssertAllZero();
        await frameScratchStream.DisposeAsync();
        R214AssertCallerOwnership(frameScratchSource, frameScratchPlaintext[0]);
        frameScratchSource.Dispose();

        var plaintext = "caller-owned-synthetic-r2"u8.ToArray();
        var original = plaintext.ToArray();
        var context = Context(plaintext.Length, SHA256.HashData(plaintext));
        var aead = new CapturingAead();
        var commitment = new CapturingCommitment(context.ContentCommitment);
        var source = new MemoryStream(plaintext, writable: false);
        var stream = new RawExportR2FramedCiphertextStream(
            source,
            context,
            Bytes(0x60, 32),
            aead,
            commitment,
            count => Enumerable.Range(1, count).Select(value => (byte)value).ToArray());
        _ = await ReadAllAsync(stream);

        var retainedNonceSuffixes = ((IEnumerable<byte[]>)typeof(RawExportR2FramedCiphertextStream)
            .GetField("dataNonceSuffixes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(stream)!).ToArray();
        var longLivedScratch = new[]
        {
            (byte[])typeof(RawExportR2FramedCiphertextStream)
                .GetField("headerDigest", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(stream)!,
            (byte[])typeof(RawExportR2FramedCiphertextStream)
                .GetField("envelopeMetadataDigest", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(stream)!,
            (byte[])typeof(RawExportR2FramedCiphertextStream)
                .GetField("authenticationChunkCommitment", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(stream)!,
        };
        await stream.DisposeAsync();

        Assert.Equal(original, plaintext);
        Assert.True(source.CanRead);
        Assert.All(aead.R2OwnedBuffers, AssertAllZero);
        Assert.All(commitment.R2OwnedBuffers, AssertAllZero);
        Assert.All(retainedNonceSuffixes, AssertAllZero);
        Assert.All(longLivedScratch, AssertAllZero);
        source.Dispose();

        R214AssertFinalFieldCleanup(
            "transient plaintext digest",
            "<plaintextDigest>5__3",
            typeof(IncrementalHash),
            nameof(IncrementalHash.GetHashAndReset));
        R214AssertFinalFieldCleanup(
            "DATA ciphertext digest",
            "<dataDigest>5__5",
            typeof(IncrementalHash),
            nameof(IncrementalHash.GetHashAndReset));
        R214AssertFinalLocalCleanup(
            "computed commitment comparison buffer",
            typeof(ReadOnlyMemory<byte>),
            nameof(ReadOnlyMemory<byte>.ToArray));

        Assert.Equal(
            new[]
            {
                "BEHAVIORALLY_PROVEN:plaintext chunk/read buffer",
                "BEHAVIORALLY_PROVEN:DATA nonce",
                "BEHAVIORALLY_PROVEN:DATA AAD",
                "STRUCTURALLY_PROVEN:transient plaintext digest",
                "STRUCTURALLY_PROVEN:DATA ciphertext digest",
                "BEHAVIORALLY_PROVEN:historic commitment payload",
                "BEHAVIORALLY_PROVEN:completion plaintext / FINAL plaintext scratch",
                "BEHAVIORALLY_PROVEN:FINAL nonce",
                "BEHAVIORALLY_PROVEN:FINAL AAD",
                "STRUCTURALLY_PROVEN:computed commitment comparison buffer",
                "BEHAVIORALLY_PROVEN:AEAD ciphertext output",
                "BEHAVIORALLY_PROVEN:AEAD authentication tag",
                "BEHAVIORALLY_PROVEN:nonce-generation scratch",
                "BEHAVIORALLY_PROVEN:retained DATA nonce suffix",
                "BEHAVIORALLY_PROVEN:header digest",
                "BEHAVIORALLY_PROVEN:envelope metadata digest",
                "BEHAVIORALLY_PROVEN:authentication chunk commitment",
                "BEHAVIORALLY_PROVEN:serialized frame/current scratch",
            },
            R214BufferFamilyCensus);
    }

    private static RawExportR2Header CodecHeader(int chunkSize) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new byte[32], new byte[32], "AES-256-GCM", 1, chunkSize, "Random96", new byte[32]);

    private static RawExportR2EncryptionContext Context(int length, byte[] commitment) => new(
        Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(), Guid.NewGuid(),
        Bytes(0x40, 32), "fixture-provider", "fixture-kek", 1, "fixture-fingerprint",
        RawExportR2FrameCodec.FixtureEncryptionSuiteId, RawExportR2FrameCodec.FixtureFramingVersion,
        RawExportR2FrameCodec.FixtureChunkSize, RawExportR2FrameCodec.FixtureNonceStrategyId,
        "none", Bytes(0x20, 32), Bytes(0x80, 32), Bytes(0x00, 32),
        Guid.NewGuid(), Guid.NewGuid(), 1, "LiveSelfieImage", "scope:fixture", "controller:fixture",
        length, "image/jpeg", 1, "fixture-content-commitment", 1, commitment,
        DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow.AddMinutes(10),
        DateTimeOffset.UtcNow.AddMinutes(10));

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        return output.ToArray();
    }

    private static void AssertExactFrameShape(
        byte[] encoded,
        RawExportR2Header header,
        int plaintextLength)
    {
        var offset = RawExportR2FrameCodec.HeaderLength(header);
        var remaining = plaintextLength;
        uint expectedOrdinal = 0;
        while (remaining > 0)
        {
            Assert.Equal(RawExportR2FrameCodec.DataFrameType, encoded[offset++]);
            Assert.Equal(expectedOrdinal++, System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(encoded.AsSpan(offset, 4)));
            offset += 4;
            var chunkLength = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(encoded.AsSpan(offset, 4)));
            offset += 4;
            Assert.Equal(Math.Min(header.ChunkSize, remaining), chunkLength);
            offset += 12 + chunkLength + 16;
            remaining -= chunkLength;
        }

        Assert.Equal(RawExportR2FrameCodec.FinalFrameType, encoded[offset]);
        offset += RawExportR2FrameCodec.FinalFrameLength;
        Assert.Equal(encoded.Length, offset);
    }

    private static byte[] Bytes(int first, int count) =>
        Enumerable.Range(first, count).Select(value => checked((byte)value)).ToArray();

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static byte[] IndependentHeader(
        Guid attempt, Guid key, Guid source, Guid objectIdentity, byte[] fingerprint,
        byte[] binding, string suite, ushort version, uint chunk, string nonce, byte[] framing)
    {
        using var output = new MemoryStream();
        output.Write("TAGEKYC-C1-R2"u8);
        WriteU16(output, 1);
        output.Write(attempt.ToByteArray(bigEndian: true));
        output.Write(key.ToByteArray(bigEndian: true));
        output.Write(source.ToByteArray(bigEndian: true));
        output.Write(objectIdentity.ToByteArray(bigEndian: true));
        output.Write(fingerprint);
        output.Write(binding);
        WriteText(output, suite);
        WriteU16(output, version);
        WriteU32(output, chunk);
        WriteText(output, nonce);
        output.Write(framing);
        return output.ToArray();
    }

    private static void WriteText(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteU32(stream, checked((uint)bytes.Length));
        stream.Write(bytes);
    }

    private static void WriteU16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteU32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private sealed class FixtureAead : IAttemptAeadEncryptionOperation
    {
        public Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            var output = request.Input.ToArray();
            var tag = SHA256.HashData(request.AssociatedData.Span).AsSpan(0, 16).ToArray();
            return Task.FromResult(new AttemptAeadChunkResult(output, tag));
        }
    }

    private sealed class EchoCommitment(byte[] value) : IContentCommitmentService
    {
        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(ContentCommitmentResult.Success(value));
    }

    private sealed class CapturingAead : IAttemptAeadEncryptionOperation
    {
        internal List<byte[]> R2OwnedBuffers { get; } = [];

        public Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            Capture(request.Input);
            Capture(request.Nonce);
            Capture(request.AssociatedData);
            var output = request.Input.ToArray();
            var tag = Enumerable.Repeat((byte)0x7f, 16).ToArray();
            R2OwnedBuffers.Add(output);
            R2OwnedBuffers.Add(tag);
            return Task.FromResult(new AttemptAeadChunkResult(output, tag));
        }

        private void Capture(ReadOnlyMemory<byte> memory)
        {
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment));
            Assert.NotNull(segment.Array);
            R2OwnedBuffers.Add(segment.Array!);
        }
    }

    private sealed class CapturingCommitment(byte[] value) : IContentCommitmentService
    {
        internal List<byte[]> R2OwnedBuffers { get; } = [];

        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            Assert.True(MemoryMarshal.TryGetArray(lpPayload, out ArraySegment<byte> segment));
            Assert.NotNull(segment.Array);
            R2OwnedBuffers.Add(segment.Array!);
            return ValueTask.FromResult(ContentCommitmentResult.Success(value));
        }
    }

    private sealed class R214SentinelException(string message) : Exception(message);

    private sealed class R214RetainedBuffer(
        string family,
        byte[] storage,
        int offset,
        int count,
        int preFailureNonZeroCount)
    {
        internal string Family { get; } = family;

        internal static R214RetainedBuffer Capture(
            string family,
            ReadOnlyMemory<byte> memory)
        {
            Assert.True(MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment));
            Assert.NotNull(segment.Array);
            return new(
                family,
                segment.Array!,
                segment.Offset,
                segment.Count,
                CountNonZero(segment.AsSpan()));
        }

        internal static R214RetainedBuffer Capture(string family, byte[] storage) =>
            new(family, storage, 0, storage.Length, CountNonZero(storage));

        internal void AssertWasNonZero() =>
            Assert.True(
                preFailureNonZeroCount > 0,
                $"R214_BUFFER_WAS_NOT_POPULATED:{Family}");

        internal void AssertAllZero()
        {
            foreach (var value in storage.AsSpan(offset, count))
                Assert.Equal(0, value);
        }

        private static int CountNonZero(ReadOnlySpan<byte> value)
        {
            var count = 0;
            foreach (var item in value)
            {
                if (item != 0)
                    count++;
            }
            return count;
        }
    }

    private sealed class R214ThrowingSource(byte[] callerOwnedBytes) : Stream
    {
        internal const string Sentinel = "R214_SOURCE_READ_SENTINEL";
        private readonly MemoryStream inner = new(callerOwnedBytes, writable: false);
        private bool disposed;

        internal int CallCount { get; private set; }
        internal R214RetainedBuffer? Destination { get; private set; }
        internal bool Disposed => disposed;

        public override bool CanRead => !disposed;
        public override bool CanSeek => !disposed;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var count = Math.Min(buffer.Length, callerOwnedBytes.Length);
            callerOwnedBytes.AsMemory(0, count).CopyTo(buffer);
            Destination = R214RetainedBuffer.Capture(
                "plaintext chunk/read buffer",
                buffer[..count]);
            Destination.AssertWasNonZero();
            throw new R214SentinelException(Sentinel);
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            if (disposing)
                inner.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class R214CallerOwnedStream(byte[] callerOwnedBytes)
        : MemoryStream(callerOwnedBytes, writable: false)
    {
        internal bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class R214CapturingAead(int throwOnCall = 0)
        : IAttemptAeadEncryptionOperation
    {
        internal const string Sentinel = "R214_AEAD_SENTINEL";
        internal List<R214RetainedBuffer> Retained { get; } = [];
        internal int CallCount { get; private set; }

        public Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var frame = CallCount == 1 ? "DATA" : "FINAL";
            Retained.Add(R214RetainedBuffer.Capture($"{frame} plaintext", request.Input));
            Retained.Add(R214RetainedBuffer.Capture($"{frame} nonce", request.Nonce));
            Retained.Add(R214RetainedBuffer.Capture($"{frame} AAD", request.AssociatedData));
            Assert.All(Retained.TakeLast(3), retained => retained.AssertWasNonZero());
            if (CallCount == throwOnCall)
                throw new R214SentinelException(Sentinel);

            var output = request.Input.ToArray();
            var tag = Enumerable.Repeat((byte)0x7f, 16).ToArray();
            Retained.Add(R214RetainedBuffer.Capture($"{frame} ciphertext output", output));
            Retained.Add(R214RetainedBuffer.Capture($"{frame} authentication tag", tag));
            return Task.FromResult(new AttemptAeadChunkResult(output, tag));
        }
    }

    private sealed class R214CapturingCommitment(byte[] value, bool throwOnCall = false)
        : IContentCommitmentService
    {
        internal const string Sentinel = "R214_COMMITMENT_SENTINEL";
        internal R214RetainedBuffer? Payload { get; private set; }
        internal int CallCount { get; private set; }

        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Payload = R214RetainedBuffer.Capture("historic commitment payload", lpPayload);
            Payload.AssertWasNonZero();
            if (throwOnCall)
                throw new R214SentinelException(Sentinel);
            return ValueTask.FromResult(ContentCommitmentResult.Success(value));
        }
    }

    private sealed class R214RandomBytes
    {
        internal List<R214RetainedBuffer> Returned { get; } = [];

        internal byte[] GetBytes(int count)
        {
            var value = Enumerable.Range(1, count).Select(item => (byte)item).ToArray();
            Returned.Add(R214RetainedBuffer.Capture("nonce-generation scratch", value));
            return value;
        }
    }

    private static readonly IReadOnlyList<string> R214BufferFamilyCensus =
    [
        "BEHAVIORALLY_PROVEN:plaintext chunk/read buffer",
        "BEHAVIORALLY_PROVEN:DATA nonce",
        "BEHAVIORALLY_PROVEN:DATA AAD",
        "STRUCTURALLY_PROVEN:transient plaintext digest",
        "STRUCTURALLY_PROVEN:DATA ciphertext digest",
        "BEHAVIORALLY_PROVEN:historic commitment payload",
        "BEHAVIORALLY_PROVEN:completion plaintext / FINAL plaintext scratch",
        "BEHAVIORALLY_PROVEN:FINAL nonce",
        "BEHAVIORALLY_PROVEN:FINAL AAD",
        "STRUCTURALLY_PROVEN:computed commitment comparison buffer",
        "BEHAVIORALLY_PROVEN:AEAD ciphertext output",
        "BEHAVIORALLY_PROVEN:AEAD authentication tag",
        "BEHAVIORALLY_PROVEN:nonce-generation scratch",
        "BEHAVIORALLY_PROVEN:retained DATA nonce suffix",
        "BEHAVIORALLY_PROVEN:header digest",
        "BEHAVIORALLY_PROVEN:envelope metadata digest",
        "BEHAVIORALLY_PROVEN:authentication chunk commitment",
        "BEHAVIORALLY_PROVEN:serialized frame/current scratch",
    ];

    private static IReadOnlyList<R214RetainedBuffer> R214RetainLongLivedScratch(
        RawExportR2FramedCiphertextStream stream)
    {
        var retained = new List<R214RetainedBuffer>
        {
            R214RetainedBuffer.Capture(
                "header digest",
                (byte[])typeof(RawExportR2FramedCiphertextStream)
                    .GetField("headerDigest", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(stream)!),
            R214RetainedBuffer.Capture(
                "envelope metadata digest",
                (byte[])typeof(RawExportR2FramedCiphertextStream)
                    .GetField("envelopeMetadataDigest", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(stream)!),
            R214RetainedBuffer.Capture(
                "authentication chunk commitment",
                (byte[])typeof(RawExportR2FramedCiphertextStream)
                    .GetField("authenticationChunkCommitment", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(stream)!),
        };
        var nonceSuffixes = (IEnumerable<byte[]>)typeof(RawExportR2FramedCiphertextStream)
            .GetField("dataNonceSuffixes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(stream)!;
        retained.AddRange(nonceSuffixes.Select(
            suffix => R214RetainedBuffer.Capture("retained DATA nonce suffix", suffix)));
        Assert.All(retained, item => item.AssertWasNonZero());
        return retained;
    }

    private static R214RetainedBuffer R214RetainCurrentFrame(
        RawExportR2FramedCiphertextStream stream,
        string family) =>
        R214RetainedBuffer.Capture(
            family,
            (byte[])typeof(RawExportR2FramedCiphertextStream)
                .GetField("current", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(stream)!);

    private static void R214AssertCallerOwnership(R214ThrowingSource source, byte expected)
    {
        Assert.False(source.Disposed);
        Assert.True(source.CanRead);
        Assert.True(source.CanSeek);
        source.Position = 0;
        Assert.Equal(expected, source.ReadByte());
    }

    private static void R214AssertCallerOwnership(R214CallerOwnedStream source, byte expected)
    {
        Assert.False(source.Disposed);
        Assert.True(source.CanRead);
        Assert.True(source.CanSeek);
        source.Position = 0;
        Assert.Equal(expected, source.ReadByte());
    }

    private static void AssertAllZero(byte[] value) =>
        Assert.All(value, item => Assert.Equal(0, item));

    private sealed record R214IlInstruction(
        int Offset,
        System.Reflection.Emit.OpCode OpCode,
        object? Operand);

    private static void R214AssertFinalFieldCleanup(
        string family,
        string fieldName,
        Type producerOwner,
        string producerName)
    {
        var moveNext = R206AsyncMoveNext(
            typeof(RawExportR2FramedCiphertextStream),
            "BuildFinalFrameAsync");
        var instructions = R214ReadIl(moveNext);
        var field = moveNext.DeclaringType!.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.True(field is not null, $"R214_FIELD_NOT_RESOLVED:{family}");

        var producerIndex = -1;
        var producerStoreOffset = -1;
        for (var index = 0; index < instructions.Count; index++)
        {
            if (instructions[index].Operand is not MethodBase method
                || method.DeclaringType != producerOwner
                || method.Name != producerName)
                continue;
            var store = instructions.Skip(index + 1).Take(3).FirstOrDefault(
                instruction => instruction.OpCode == System.Reflection.Emit.OpCodes.Stfld
                    && instruction.Operand is FieldInfo stored
                    && R214SameField(stored, field!));
            if (store is not null)
            {
                producerIndex = index;
                producerStoreOffset = store.Offset;
                break;
            }
        }
        Assert.True(producerIndex >= 0, $"R214_PRODUCER_NOT_RESOLVED:{family}");

        var zeroIndex = R214FindZeroMemoryIndex(
            instructions,
            index => index >= 2
                && instructions[index - 2].OpCode == System.Reflection.Emit.OpCodes.Ldfld
                && instructions[index - 2].Operand is FieldInfo loaded
                && R214SameField(loaded, field!));
        Assert.True(zeroIndex >= 0, $"R214_ZERO_NOT_BOUND_TO_FIELD:{family}");
        R214AssertFinallyOwnsCleanup(
            family,
            moveNext,
            instructions,
            producerStoreOffset,
            instructions[zeroIndex].Offset);
    }

    private static void R214AssertFinalLocalCleanup(
        string family,
        Type producerOwner,
        string producerName)
    {
        var moveNext = R206AsyncMoveNext(
            typeof(RawExportR2FramedCiphertextStream),
            "BuildFinalFrameAsync");
        var instructions = R214ReadIl(moveNext);
        var producerIndex = R214FindIndex(instructions, instruction =>
            instruction.Operand is MethodBase method
            && method.DeclaringType == producerOwner
            && method.Name == producerName);
        Assert.True(producerIndex >= 0, $"R214_LOCAL_PRODUCER_NOT_RESOLVED:{family}");
        Assert.True(producerIndex + 1 < instructions.Count);
        var local = R214LocalIndex(instructions[producerIndex + 1], store: true);
        Assert.True(local >= 0, $"R214_LOCAL_STORE_NOT_RESOLVED:{family}");

        var zeroIndex = R214FindZeroMemoryIndex(
            instructions,
            index => index >= 2
                && R214LocalIndex(instructions[index - 2], store: false) == local);
        Assert.True(zeroIndex >= 0, $"R214_ZERO_NOT_BOUND_TO_LOCAL:{family}");
        R214AssertFinallyOwnsCleanup(
            family,
            moveNext,
            instructions,
            instructions[producerIndex + 1].Offset,
            instructions[zeroIndex].Offset);
    }

    private static int R214FindZeroMemoryIndex(
        IReadOnlyList<R214IlInstruction> instructions,
        Func<int, bool> exactBufferLoad)
    {
        for (var index = 0; index < instructions.Count; index++)
        {
            if (instructions[index].Operand is MethodBase method
                && method.DeclaringType == typeof(CryptographicOperations)
                && method.Name == nameof(CryptographicOperations.ZeroMemory)
                && index >= 1
                && instructions[index - 1].Operand is MethodBase conversion
                && conversion.Name == "op_Implicit"
                && conversion.DeclaringType == typeof(Span<byte>)
                && exactBufferLoad(index))
                return index;
        }
        return -1;
    }

    private static void R214AssertFinallyOwnsCleanup(
        string family,
        MethodInfo moveNext,
        IReadOnlyList<R214IlInstruction> instructions,
        int producerStoreOffset,
        int zeroOffset)
    {
        var body = moveNext.GetMethodBody();
        Assert.NotNull(body);
        var clause = body.ExceptionHandlingClauses.SingleOrDefault(candidate =>
            candidate.Flags == ExceptionHandlingClauseOptions.Finally
            && zeroOffset >= candidate.HandlerOffset
            && zeroOffset < candidate.HandlerOffset + candidate.HandlerLength);
        Assert.NotNull(clause);
        var storeIndex = R214FindIndex(
            instructions,
            instruction => instruction.Offset == producerStoreOffset);
        Assert.True(storeIndex >= 0, $"R214_PRODUCER_STORE_NOT_RESOLVED:{family}");
        var producerInsideProtectedTry =
            producerStoreOffset >= clause.TryOffset
            && producerStoreOffset < clause.TryOffset + clause.TryLength;
        var producerImmediatelyBeforeProtectedTry =
            storeIndex + 1 < instructions.Count
            && instructions[storeIndex + 1].Offset == clause.TryOffset;
        Assert.True(
            producerInsideProtectedTry || producerImmediatelyBeforeProtectedTry,
            $"R214_PRODUCER_NOT_PROTECTED_BY_FINALLY:{family}");
        Assert.True(
            zeroOffset < clause.HandlerOffset + clause.HandlerLength,
            $"R214_ZERO_OUTSIDE_FINALLY:{family}");
    }

    private static int R214LocalIndex(R214IlInstruction instruction, bool store)
    {
        var name = instruction.OpCode.Name;
        var prefix = store ? "stloc." : "ldloc.";
        if (name is not null && name.StartsWith(prefix, StringComparison.Ordinal))
        {
            var suffix = name[prefix.Length..];
            if (int.TryParse(suffix, CultureInfo.InvariantCulture, out var direct))
                return direct;
            if (suffix == "s" && instruction.Operand is byte shortIndex)
                return shortIndex;
        }
        if (instruction.OpCode == (store
                ? System.Reflection.Emit.OpCodes.Stloc
                : System.Reflection.Emit.OpCodes.Ldloc)
            && instruction.Operand is ushort index)
            return index;
        return -1;
    }

    private static bool R214SameField(FieldInfo left, FieldInfo right) =>
        left.Module == right.Module && left.MetadataToken == right.MetadataToken;

    private static int R214FindIndex(
        IReadOnlyList<R214IlInstruction> instructions,
        Func<R214IlInstruction, bool> predicate)
    {
        for (var index = 0; index < instructions.Count; index++)
        {
            if (predicate(instructions[index]))
                return index;
        }
        return -1;
    }

    private static IReadOnlyList<R214IlInstruction> R214ReadIl(MethodBase method)
    {
        var body = method.GetMethodBody();
        Assert.NotNull(body);
        var il = body.GetILAsByteArray();
        Assert.NotNull(il);
        Assert.NotEmpty(il);
        var instructions = new List<R214IlInstruction>();
        var offset = 0;
        while (offset < il.Length)
        {
            var instructionOffset = offset;
            var first = il[offset++];
            var value = first == 0xfe
                ? unchecked((short)(0xfe00 | il[offset++]))
                : (short)first;
            var opcode = R206OpCodes[value];
            object? operand = null;
            switch (opcode.OperandType)
            {
                case System.Reflection.Emit.OperandType.InlineNone:
                    break;
                case System.Reflection.Emit.OperandType.ShortInlineBrTarget:
                case System.Reflection.Emit.OperandType.ShortInlineI:
                case System.Reflection.Emit.OperandType.ShortInlineVar:
                    operand = il[offset];
                    offset += 1;
                    break;
                case System.Reflection.Emit.OperandType.InlineVar:
                    operand = BitConverter.ToUInt16(il, offset);
                    offset += 2;
                    break;
                case System.Reflection.Emit.OperandType.InlineI:
                case System.Reflection.Emit.OperandType.InlineBrTarget:
                    operand = BitConverter.ToInt32(il, offset);
                    offset += 4;
                    break;
                case System.Reflection.Emit.OperandType.InlineField:
                {
                    var token = BitConverter.ToInt32(il, offset);
                    operand = method.Module.ResolveField(
                        token,
                        method.DeclaringType?.GetGenericArguments(),
                        method.IsGenericMethod ? method.GetGenericArguments() : null);
                    offset += 4;
                    break;
                }
                case System.Reflection.Emit.OperandType.InlineMethod:
                {
                    var token = BitConverter.ToInt32(il, offset);
                    operand = method.Module.ResolveMethod(
                        token,
                        method.DeclaringType?.GetGenericArguments(),
                        method.IsGenericMethod ? method.GetGenericArguments() : null);
                    offset += 4;
                    break;
                }
                case System.Reflection.Emit.OperandType.InlineString:
                    operand = method.Module.ResolveString(BitConverter.ToInt32(il, offset));
                    offset += 4;
                    break;
                case System.Reflection.Emit.OperandType.InlineType:
                case System.Reflection.Emit.OperandType.InlineTok:
                case System.Reflection.Emit.OperandType.InlineSig:
                    operand = BitConverter.ToInt32(il, offset);
                    offset += 4;
                    break;
                case System.Reflection.Emit.OperandType.ShortInlineR:
                    operand = BitConverter.ToSingle(il, offset);
                    offset += 4;
                    break;
                case System.Reflection.Emit.OperandType.InlineI8:
                    operand = BitConverter.ToInt64(il, offset);
                    offset += 8;
                    break;
                case System.Reflection.Emit.OperandType.InlineR:
                    operand = BitConverter.ToDouble(il, offset);
                    offset += 8;
                    break;
                case System.Reflection.Emit.OperandType.InlineSwitch:
                {
                    var count = BitConverter.ToInt32(il, offset);
                    operand = count;
                    offset += checked(4 + count * 4);
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"R214_UNSUPPORTED_IL_OPERAND:{opcode.OperandType}");
            }
            instructions.Add(new(instructionOffset, opcode, operand));
        }
        return instructions;
    }

    private static MethodInfo R206AsyncMoveNext(Type owner, string methodName)
    {
        var method = owner.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var stateMachine = method.GetCustomAttribute<
            System.Runtime.CompilerServices.AsyncStateMachineAttribute>();
        Assert.True(stateMachine is not null,
            $"R206_ASYNC_STATE_MACHINE_MISSING:{owner.FullName}.{methodName}");
        var moveNext = stateMachine.StateMachineType.GetMethod(
            "MoveNext",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(moveNext);
        Assert.NotEmpty(moveNext.GetMethodBody()?.GetILAsByteArray() ?? []);
        return moveNext;
    }

    private static void R206AssertDigestPath(
        string scope,
        IReadOnlyList<MethodBase> calls,
        MethodInfo historicHelper)
    {
        Assert.Contains(calls, method =>
            method.DeclaringType == typeof(IncrementalHash)
            && method.Name == nameof(IncrementalHash.GetHashAndReset)
            && method.GetParameters().Length == 0);
        Assert.Contains(calls, method =>
            method.Module == historicHelper.Module
            && method.MetadataToken == historicHelper.MetadataToken);
        Assert.True(calls.Count > 0, $"R206_DIGEST_PATH_NOT_RESOLVED:{scope}");
    }

    private static void R206AssertNoManagedDigestText(
        string scope,
        IReadOnlyList<MethodBase> calls)
    {
        var forbidden = calls.FirstOrDefault(R206IsForbiddenManagedTextCall);
        Assert.True(forbidden is null,
            $"R206_FORBIDDEN_MANAGED_STRING_CONVERSION:{scope}:"
            + $"{forbidden?.DeclaringType?.FullName}.{forbidden?.Name}");
    }

    private static bool R206IsForbiddenManagedTextCall(MethodBase method)
    {
        if (!R206ConsumesByteOrientedInput(method))
            return false;
        if (method.DeclaringType == typeof(Convert))
            return method.Name is "ToHexString" or "ToHexStringLower" or "ToBase64String";
        if (method.DeclaringType == typeof(BitConverter))
            return method.Name == nameof(BitConverter.ToString);
        return method.Name == "GetString"
            && method.DeclaringType is not null
            && typeof(Encoding).IsAssignableFrom(method.DeclaringType);
    }

    private static bool R206ConsumesByteOrientedInput(MethodBase method) =>
        method.GetParameters().Any(parameter =>
        {
            var type = parameter.ParameterType;
            if (type.IsByRef)
                type = type.GetElementType()!;
            return type == typeof(byte[])
                || type == typeof(ReadOnlySpan<byte>)
                || type == typeof(Span<byte>)
                || type == typeof(ReadOnlyMemory<byte>)
                || type == typeof(Memory<byte>)
                || type == typeof(byte).MakePointerType();
        });

    private static IReadOnlyList<MethodBase> R206ResolvedCalls(MethodBase method)
    {
        var body = method.GetMethodBody();
        Assert.NotNull(body);
        var il = body.GetILAsByteArray();
        Assert.NotNull(il);
        Assert.NotEmpty(il);
        var calls = new List<MethodBase>();
        var offset = 0;
        while (offset < il.Length)
        {
            var first = il[offset++];
            var value = first == 0xfe
                ? unchecked((short)(0xfe00 | il[offset++]))
                : (short)first;
            var opcode = R206OpCodes[value];
            var operandStart = offset;
            var operandSize = opcode.OperandType switch
            {
                System.Reflection.Emit.OperandType.InlineNone => 0,
                System.Reflection.Emit.OperandType.ShortInlineBrTarget
                    or System.Reflection.Emit.OperandType.ShortInlineI
                    or System.Reflection.Emit.OperandType.ShortInlineVar => 1,
                System.Reflection.Emit.OperandType.InlineVar => 2,
                System.Reflection.Emit.OperandType.InlineI
                    or System.Reflection.Emit.OperandType.InlineBrTarget
                    or System.Reflection.Emit.OperandType.InlineField
                    or System.Reflection.Emit.OperandType.InlineMethod
                    or System.Reflection.Emit.OperandType.InlineSig
                    or System.Reflection.Emit.OperandType.InlineString
                    or System.Reflection.Emit.OperandType.InlineType
                    or System.Reflection.Emit.OperandType.InlineTok
                    or System.Reflection.Emit.OperandType.ShortInlineR => 4,
                System.Reflection.Emit.OperandType.InlineI8
                    or System.Reflection.Emit.OperandType.InlineR => 8,
                System.Reflection.Emit.OperandType.InlineSwitch =>
                    checked(4 + BitConverter.ToInt32(il, offset) * 4),
                _ => throw new InvalidOperationException(
                    $"R206_UNSUPPORTED_IL_OPERAND:{opcode.OperandType}"),
            };
            offset = checked(offset + operandSize);
            if (opcode.OperandType != System.Reflection.Emit.OperandType.InlineMethod)
                continue;
            var token = BitConverter.ToInt32(il, operandStart);
            var resolved = method.Module.ResolveMethod(
                token,
                method.DeclaringType?.GetGenericArguments(),
                method.IsGenericMethod ? method.GetGenericArguments() : null);
            Assert.NotNull(resolved);
            calls.Add(resolved);
        }
        return calls;
    }

    private static readonly IReadOnlyDictionary<short, System.Reflection.Emit.OpCode>
        R206OpCodes = typeof(System.Reflection.Emit.OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(System.Reflection.Emit.OpCode))
            .Select(field => (System.Reflection.Emit.OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => opcode.Value);
}

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(
    PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string ChallengeHash = "sha256:c1b2-r2-session-challenge";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task R201_identifiers_projections_acl_and_63_byte_round_trip_are_exact()
    {
        var encryptionSignature =
            "tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)";
        var verificationSignature =
            "tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint)";
        var terminationSignature =
            "tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)";
        var encryptionColumns = new (string, string)[]
        {
            ("AttemptId", "uuid"), ("SourceArtifactId", "uuid"),
            ("EncryptionAttemptRevision", "bigint"), ("Fence", "bigint"),
            ("AttemptKeyReservationId", "uuid"), ("ProvisionalObjectIdentity", "uuid"),
            ("EncryptionAttemptFingerprint", "bytea"), ("KeyProviderId", "text"),
            ("KekId", "text"), ("KekVersion", "integer"), ("KekFingerprint", "text"),
            ("EncryptionSuiteId", "text"), ("EncryptionFramingVersion", "integer"),
            ("ChunkSize", "integer"), ("NonceStrategyId", "text"),
            ("NonceDerivationSeedReferenceOrWrappedSeed", "text"),
            ("NonceDerivationSeedCommitment", "bytea"), ("FramingParametersDigest", "bytea"),
            ("WrappedDekMetadataDigest", "bytea"), ("VerificationSessionId", "uuid"),
            ("CaptureArtifactId", "uuid"), ("CaptureRevision", "integer"), ("RawClass", "text"),
            ("StableDataScopeId", "text"), ("ControllerIdentity", "text"),
            ("ClaimedPlaintextLength", "bigint"), ("MediaType", "text"),
            ("ContentCommitmentSchemaVersion", "integer"), ("ContentCommitmentKeyId", "text"),
            ("ContentCommitmentKeyVersion", "integer"), ("ContentCommitment", "bytea"),
            ("OwnershipLeaseExpiresAtUtc", "timestamp with time zone"),
            ("EffectivePlaintextRetentionExpiresAtUtc", "timestamp with time zone"),
            ("ReservationExpiresAtUtc", "timestamp with time zone"),
        };
        var verificationColumns = encryptionColumns
            .Where(column => column.Item1 is not
                ("NonceDerivationSeedReferenceOrWrappedSeed"
                or "NonceDerivationSeedCommitment"
                or "OwnershipLeaseExpiresAtUtc"
                or "EffectivePlaintextRetentionExpiresAtUtc"
                or "ReservationExpiresAtUtc"))
            .ToArray();

        Assert.Equal(encryptionColumns, await ReadProjectionColumnsAsync(encryptionSignature));
        Assert.Equal(verificationColumns, await ReadProjectionColumnsAsync(verificationSignature));
        Assert.Equal(
            new[] { ("tagekyc_raw_export_custody_encryptor", "tagekyc_raw_export_deployer") },
            await ReadNonOwnerExecuteGrantsAsync(encryptionSignature));
        Assert.Equal(
            new[] { ("tagekyc_raw_export_reconciler", "tagekyc_raw_export_deployer") },
            await ReadNonOwnerExecuteGrantsAsync(verificationSignature));
        Assert.Equal(
            new[] { ("tagekyc_raw_export_reconciler", "tagekyc_raw_export_deployer") },
            await ReadNonOwnerExecuteGrantsAsync(terminationSignature));

        foreach (var signature in new[] { encryptionSignature, verificationSignature, terminationSignature })
        {
            var posture = await ReadFunctionPostureAsync(signature);
            Assert.Equal("tagekyc_raw_export_deployer", posture.Owner);
            Assert.True(posture.SecurityDefiner);
            Assert.Equal("{search_path=pg_catalog}", posture.Config);
            Assert.Equal("f", posture.Kind);
        }

        await using (var connection = await OpenAsync())
        await using (var command = new NpgsqlCommand(
            """
            SELECT a.attnotnull=false,pg_catalog.format_type(a.atttypid,a.atttypmod),a.attacl IS NULL
            FROM pg_catalog.pg_attribute a
            WHERE a.attrelid='tagekyc.raw_export_source_encryption_attempts'::pg_catalog.regclass
              AND a.attname='R2TerminatedAtUtc' AND a.attnum>0 AND NOT a.attisdropped;
            """,
            connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.True(reader.GetBoolean(0));
            Assert.Equal("timestamp with time zone", reader.GetString(1));
            Assert.True(reader.GetBoolean(2));
        }

        await AssertNoDirectCapabilityTablePrivilegesAsync();
        var exact63 = new string('r', 63);
        await using (var connection = await OpenAsync())
        await using (var command = new NpgsqlCommand($"SELECT 1 AS \"{exact63}\";", connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal(exact63, reader.GetName(0));
            Assert.Equal(63, Encoding.UTF8.GetByteCount(reader.GetName(0)));
        }
        Assert.All(
            new[]
            {
                "ck_raw_export_source_attempt_values",
                "raw_export_read_source_encryption_context",
                "raw_export_read_source_verification_context",
                "raw_export_terminate_source_encryption_attempt",
            },
            identifier => Assert.InRange(Encoding.UTF8.GetByteCount(identifier), 1, 63));
    }

    [Fact]
    public async Task R207_fresh_key_provisioning_precedes_active_context_and_partitions_every_outcome()
    {
        var invalidProvisioner = new FixedProvisioner(
            AttemptKeyProvisioningOutcome.Activated,
            Guid.NewGuid());
        await using (var db = postgres.CreateDbContext())
        {
            var result = await new RawExportR2EncryptionOrchestrator(
                new RawExportR2Repository(db),
                invalidProvisioner,
                new CountingEncryptionOperation(),
                new UnavailableCommitment(),
                new CountingObjectWriter(ConditionalPutOutcome.Created))
                .ExecuteAsync(
                    new(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1, Stream.Null),
                    CancellationToken.None);
            Assert.Equal(RawExportR2WriterDisposition.PreCustodyRejected, result.Disposition);
            Assert.Equal(0, invalidProvisioner.CallCount);
        }

        var expected = new Dictionary<AttemptKeyProvisioningOutcome, RawExportR2WriterDisposition>
        {
            [AttemptKeyProvisioningOutcome.InProgress] = RawExportR2WriterDisposition.PreCustodyRetryable,
            [AttemptKeyProvisioningOutcome.ProviderUnavailable] = RawExportR2WriterDisposition.PreCustodyRetryable,
            [AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown] = RawExportR2WriterDisposition.PreCustodyRetryable,
            [AttemptKeyProvisioningOutcome.Conflict] = RawExportR2WriterDisposition.PreCustodyOperatorRequired,
            [AttemptKeyProvisioningOutcome.HeadNotReserved] = RawExportR2WriterDisposition.PreCustodyOperatorRequired,
            [AttemptKeyProvisioningOutcome.ProviderCorruptOrUnverifiable] = RawExportR2WriterDisposition.PreCustodyOperatorRequired,
            [AttemptKeyProvisioningOutcome.StateConflict] = RawExportR2WriterDisposition.PreCustodyOperatorRequired,
            [AttemptKeyProvisioningOutcome.Terminated] = RawExportR2WriterDisposition.PreCustodyOperatorRequired,
            [AttemptKeyProvisioningOutcome.Activated] = RawExportR2WriterDisposition.PreCustodyRejected,
            [AttemptKeyProvisioningOutcome.ExistingMatch] = RawExportR2WriterDisposition.PreCustodyRejected,
        };
        foreach (var pair in expected)
        {
            var keyReservationId = Guid.NewGuid();
            var provisioner = new FixedProvisioner(pair.Key, keyReservationId);
            var encryption = new CountingEncryptionOperation();
            var writer = new CountingObjectWriter(ConditionalPutOutcome.Created);
            var request = new RawExportR2EncryptionRequest(
                Guid.NewGuid(), keyReservationId, Guid.NewGuid(), Guid.NewGuid(), 1, 1,
                new MemoryStream("pre-custody"u8.ToArray(), writable: false));
            await using var db = postgres.CreateDbContext();
            var result = await new RawExportR2EncryptionOrchestrator(
                new RawExportR2Repository(db), provisioner, encryption,
                new UnavailableCommitment(), writer)
                .ExecuteAsync(request, CancellationToken.None);
            Assert.Equal(pair.Value, result.Disposition);
            Assert.Null(result.ObjectCustodyId);
            Assert.Equal(1, provisioner.CallCount);
            Assert.Equal(0, encryption.CallCount);
            Assert.Equal(0, writer.CallCount);
        }

        var source = await SeedReservedSourceAsync("terminal-key-routing"u8.ToArray());
        await ProvisionKeyAsync(source);
        RawExportR2EncryptionContext context;
        await using (var db = postgres.CreateDbContext())
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(source.ActorPrincipalId, CancellationToken.None);
            context = (await repository.ReadEncryptionContextAsync(
                source.AttemptId, source.Revision, source.Fence, CancellationToken.None))!;
        }
        var profileMethod = typeof(RawExportR2EncryptionOrchestrator).GetMethod(
            "ProfileIsAdmitted",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.True((bool)profileMethod.Invoke(null, [context])!);
        Assert.False((bool)profileMethod.Invoke(null, [context with { EncryptionSuiteId = "AES-256-GCM" }])!);
        Assert.False((bool)profileMethod.Invoke(null, [context with { EncryptionFramingVersion = 2 }])!);
        Assert.False((bool)profileMethod.Invoke(null, [context with { ChunkSize = 4 }])!);
        Assert.False((bool)profileMethod.Invoke(null, [context with { NonceStrategyId = "Random96" }])!);
        Assert.False((bool)profileMethod.Invoke(null, [context with { RawClass = "NfcReadArtifact" }])!);

        await using (var connection = await OpenAsync())
        await using (var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_revoke_attempt_key_reservation(@id,'r2-terminal-key-test')",
            connection))
        {
            command.Parameters.AddWithValue("id", source.AttemptKeyReservationId);
            Assert.Equal("Revoked", await command.ExecuteScalarAsync());
        }
        var terminalProvisioner = new FixedProvisioner(
            AttemptKeyProvisioningOutcome.Terminated,
            source.AttemptKeyReservationId);
        var terminalWriter = new CountingObjectWriter(ConditionalPutOutcome.Created);
        await using (var db = postgres.CreateDbContext())
        {
            var result = await new RawExportR2EncryptionOrchestrator(
                new RawExportR2Repository(db), terminalProvisioner,
                new CountingEncryptionOperation(), new UnavailableCommitment(), terminalWriter)
                .ExecuteAsync(
                    new(
                        source.ActorPrincipalId, source.AttemptKeyReservationId, source.AttemptId,
                        source.SourceArtifactId, source.Revision, source.Fence,
                        new MemoryStream("terminal-key-routing"u8.ToArray(), writable: false)),
                    CancellationToken.None);
            Assert.Equal(RawExportR2WriterDisposition.PreCustodyTerminalKey, result.Disposition);
            Assert.Null(result.ObjectCustodyId);
            Assert.Equal(0, terminalWriter.CallCount);
        }
        await using var assertionDb = postgres.CreateDbContext();
        Assert.Empty(await assertionDb.RawExportProvisionalObjects.ToListAsync());
    }

    [Fact]
    public async Task R208_created_write_hands_off_for_verification_without_writer_read()
    {
        var plaintext = "synthetic-r2-created-handoff"u8.ToArray();
        var source = await SeedReservedSourceAsync(plaintext);
        await using var brokerProvider = CreateBrokerProvider();
        await using var commitmentScope = brokerProvider.CreateAsyncScope();
        await using var db = postgres.CreateDbContext();
        await using var lookupDb = postgres.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(db),
            new PostgresFixtureKekJournal(lookupDb));
        var encryption = new CountingEncryptionOperation();
        var writer = new CountingObjectWriter(ConditionalPutOutcome.Created);
        var orchestrator = new RawExportR2EncryptionOrchestrator(
            new RawExportR2Repository(db),
            new PostgresAttemptKeyReservationProvider(
                db,
                new PostgresKeyProviderOperationMap(db),
                kek),
            encryption,
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>(),
            writer);
        var callerSource = new MemoryStream(plaintext, writable: false);
        var result = await orchestrator.ExecuteAsync(
            new(
                source.ActorPrincipalId, source.AttemptKeyReservationId, source.AttemptId,
                source.SourceArtifactId, source.Revision, source.Fence, callerSource),
            CancellationToken.None);

        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, result.Disposition);
        Assert.Equal("ObjectPresentPendingVerification", result.ObjectState);
        Assert.Equal(1, writer.CallCount);
        Assert.True(encryption.CallCount >= 2);
        Assert.True(callerSource.CanRead);
        var row = await db.RawExportProvisionalObjects.SingleAsync(
            value => value.ObjectCustodyId == result.ObjectCustodyId);
        Assert.Equal("ObjectPresentPendingVerification", row.State);
        Assert.Null(row.VerificationEvidenceDigest);
        callerSource.Dispose();
    }

    [Fact]
    public async Task R209_unknown_and_conditional_conflict_never_reencrypt_same_attempt()
    {
        foreach (var outcome in new[]
                 {
                     ConditionalPutOutcome.OutcomeUnknown,
                     ConditionalPutOutcome.ConditionalConflict,
                 })
        {
            var plaintext = Encoding.ASCII.GetBytes($"synthetic-r2-no-replay-{outcome}");
            var source = await SeedReservedSourceAsync(plaintext);
            await using var brokerProvider = CreateBrokerProvider();
            await using var commitmentScope = brokerProvider.CreateAsyncScope();
            await using var db = postgres.CreateDbContext();
            await using var lookupDb = postgres.CreateDbContext();
            var kek = new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(db),
                new PostgresFixtureKekJournal(lookupDb));
            var encryption = new CountingEncryptionOperation();
            var writer = new CountingObjectWriter(outcome);
            var orchestrator = new RawExportR2EncryptionOrchestrator(
                new RawExportR2Repository(db),
                new PostgresAttemptKeyReservationProvider(
                    db,
                    new PostgresKeyProviderOperationMap(db),
                    kek),
                encryption,
                commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>(),
                writer);

            await using var firstSource = new MemoryStream(plaintext, writable: false);
            var first = await orchestrator.ExecuteAsync(
                new(
                    source.ActorPrincipalId, source.AttemptKeyReservationId, source.AttemptId,
                    source.SourceArtifactId, source.Revision, source.Fence, firstSource),
                CancellationToken.None);
            Assert.Equal(RawExportR2WriterDisposition.ReconciliationRequired, first.Disposition);
            Assert.Equal("PutOutcomeUnknown", first.ObjectState);
            var encryptionCalls = encryption.CallCount;
            Assert.True(encryptionCalls >= 2);
            Assert.Equal(1, writer.CallCount);

            await using var replaySource = new MemoryStream(plaintext, writable: false);
            var replay = await orchestrator.ExecuteAsync(
                new(
                    source.ActorPrincipalId, source.AttemptKeyReservationId, source.AttemptId,
                    source.SourceArtifactId, source.Revision, source.Fence, replaySource),
                CancellationToken.None);
            Assert.Equal(RawExportR2WriterDisposition.ReconciliationRequired, replay.Disposition);
            Assert.Equal(first.ObjectCustodyId, replay.ObjectCustodyId);
            Assert.Equal(encryptionCalls, encryption.CallCount);
            Assert.Equal(1, writer.CallCount);
        }
    }

    [Fact]
    public async Task R210_complete_existing_object_is_verified_without_reencrypt()
    {
        var plaintext = "synthetic-r2-existing-object"u8.ToArray();
        var source = await SeedReservedSourceAsync(plaintext);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var brokerProvider = CreateBrokerProvider();
        await using var commitmentScope = brokerProvider.CreateAsyncScope();
        await using var writerDb = postgres.CreateDbContext();
        await using var writerLookupDb = postgres.CreateDbContext();
        var writerKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(writerDb),
            new PostgresFixtureKekJournal(writerLookupDb));
        var encryption = new CountingEncryptionDecorator(
            new AttemptAeadEncryptionOperationService(
                writerDb,
                writerKek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())));
        var orchestrator = new RawExportR2EncryptionOrchestrator(
            new RawExportR2Repository(writerDb),
            new PostgresAttemptKeyReservationProvider(
                writerDb,
                new PostgresKeyProviderOperationMap(writerDb),
                writerKek),
            encryption,
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>(),
            new S3CompatibleProvisionalObjectWriter(
                minio.Options(ProvisionalObjectCapability.Writer)));
        await using var plaintextSource = new MemoryStream(plaintext, writable: false);
        var written = await orchestrator.ExecuteAsync(
            new(
                source.ActorPrincipalId, source.AttemptKeyReservationId, source.AttemptId,
                source.SourceArtifactId, source.Revision, source.Fence, plaintextSource),
            CancellationToken.None);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        var encryptionCallsAtHandoff = encryption.CallCount;
        Assert.True(encryptionCallsAtHandoff >= 2);

        await minio.RestartAsync();
        await using var verifyDb = postgres.CreateDbContext();
        await using var verifyLookupDb = postgres.CreateDbContext();
        var verifyKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(verifyLookupDb));
        var verificationProbe = new DualAeadProbe(
            new AttemptAeadVerificationOperationService(
                verifyDb,
                verifyKek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())));
        var verifier = new RawExportR2CompletionVerifier(
            new RawExportR2Repository(verifyDb),
            new S3CompatibleProvisionalObjectReconciler(
                minio.Options(ProvisionalObjectCapability.Reconciler)),
            verificationProbe,
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
        var verified = await verifier.ExecuteAsync(
            new(
                source.ActorPrincipalId, source.AttemptId, source.Revision, source.Fence,
                written.ObjectCustodyId!.Value),
            CancellationToken.None);
        Assert.Equal(RawExportR2VerificationDisposition.Verified, verified.Disposition);
        Assert.Equal(encryptionCallsAtHandoff, encryption.CallCount);
        Assert.Equal(0, verificationProbe.EncryptionCallCount);
    }

    [Fact]
    public async Task R211_typed_aead_stage_keeps_key_uncertainty_pending_and_auth_failure_requires_cleanup()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();

        var canonical = await WriteObjectAsync("synthetic-r2-r211-canonical"u8.ToArray(), minio);
        var canonicalProbe = new R211RecordingReconciler(
            new S3CompatibleProvisionalObjectReconciler(
                minio.Options(ProvisionalObjectCapability.Reconciler)));
        var canonicalResult = await VerifyR211Async(canonical, canonicalProbe, null, null);
        Assert.Equal(RawExportR2VerificationDisposition.Verified, canonicalResult.Disposition);
        Assert.NotNull(canonicalProbe.CanonicalBytes);
        Assert.Equal(canonicalProbe.CanonicalBytes!.Length, canonicalProbe.ReportedLength);
        Assert.Equal(SHA256.HashData(canonicalProbe.CanonicalBytes), canonicalResult.CiphertextDigest);
        Assert.Equal(1, R211DataChunkCount("synthetic-r2-r211-canonical"u8.Length));
        Assert.Equal("VerifiedCompleted", (await ReadObjectRowAsync(
            canonical.Written.ObjectCustodyId!.Value)).State);

        var keyUncertain = await WriteObjectAsync(
            Encoding.ASCII.GetBytes("synthetic-r2-key-uncertainty"),
            minio);
        await using (var db = postgres.CreateDbContext())
        await using (var scope = CreateBrokerProvider().CreateAsyncScope())
        {
            var verifier = new RawExportR2CompletionVerifier(
                new RawExportR2Repository(db),
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                new AttemptAeadVerificationOperationService(
                    db,
                    new UnwrapFailingKekOperationProvider(),
                    DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                scope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
            var result = await verifier.ExecuteAsync(
                new(
                    keyUncertain.Source.ActorPrincipalId,
                    keyUncertain.Source.AttemptId,
                    keyUncertain.Source.Revision,
                    keyUncertain.Source.Fence,
                    keyUncertain.Written.ObjectCustodyId!.Value),
                CancellationToken.None);
            Assert.Equal(
                RawExportR2VerificationDisposition.VerificationIndeterminateRetry,
                result.Disposition);
        }

        await using (var db = postgres.CreateDbContext())
        {
            var row = await db.RawExportProvisionalObjects.SingleAsync(
                value => value.ObjectCustodyId == keyUncertain.Written.ObjectCustodyId);
            AssertR211Pending(row);
        }

        await using (var db = postgres.CreateDbContext())
        await using (var scope = CreateBrokerProvider().CreateAsyncScope())
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var verifier = new RawExportR2CompletionVerifier(
                new RawExportR2Repository(db),
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                new FixedVerificationOperation(AttemptAeadVerificationOutcome.KeyAccessIndeterminate),
                scope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
            var result = await verifier.ExecuteAsync(
                new(
                    keyUncertain.Source.ActorPrincipalId,
                    keyUncertain.Source.AttemptId,
                    keyUncertain.Source.Revision,
                    keyUncertain.Source.Fence,
                    keyUncertain.Written.ObjectCustodyId!.Value),
                cancellation.Token);
            Assert.Equal(
                RawExportR2VerificationDisposition.VerificationIndeterminateRetry,
                result.Disposition);
        }

        AssertR211Pending(await ReadObjectRowAsync(keyUncertain.Written.ObjectCustodyId!.Value));

        await using (var db = postgres.CreateDbContext())
        await using (var scope = CreateBrokerProvider().CreateAsyncScope())
        using (var cancellation = new CancellationTokenSource())
        {
            var verifier = new RawExportR2CompletionVerifier(
                new RawExportR2Repository(db),
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                new CancelingVerificationOperation(cancellation),
                scope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
            var result = await verifier.ExecuteAsync(
                new(
                    keyUncertain.Source.ActorPrincipalId,
                    keyUncertain.Source.AttemptId,
                    keyUncertain.Source.Revision,
                    keyUncertain.Source.Fence,
                    keyUncertain.Written.ObjectCustodyId!.Value),
                cancellation.Token);
            Assert.Equal(
                RawExportR2VerificationDisposition.VerificationIndeterminateRetry,
                result.Disposition);
        }

        AssertR211Pending(await ReadObjectRowAsync(keyUncertain.Written.ObjectCustodyId!.Value));

        var authenticationFailed = await WriteObjectAsync(
            Encoding.ASCII.GetBytes("synthetic-r2-authentication-failure"),
            minio);
        await using (var db = postgres.CreateDbContext())
        await using (var lookupDb = postgres.CreateDbContext())
        await using (var scope = CreateBrokerProvider().CreateAsyncScope())
        {
            var verification = new AttemptAeadVerificationOperationService(
                db,
                new FixtureDurableKekOperationProvider(
                    new PostgresFixtureKekJournal(db),
                    new PostgresFixtureKekJournal(lookupDb)),
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager()));
            var verifier = new RawExportR2CompletionVerifier(
                new RawExportR2Repository(db),
                new LastByteTamperingReconciler(
                    new S3CompatibleProvisionalObjectReconciler(
                        minio.Options(ProvisionalObjectCapability.Reconciler))),
                verification,
                scope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
            var result = await verifier.ExecuteAsync(
                new(
                    authenticationFailed.Source.ActorPrincipalId,
                    authenticationFailed.Source.AttemptId,
                    authenticationFailed.Source.Revision,
                    authenticationFailed.Source.Fence,
                    authenticationFailed.Written.ObjectCustodyId!.Value),
                CancellationToken.None);
            Assert.Equal(
                RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
                result.Disposition);
            Assert.Null(result.VerifiedPlaintextLength);
            Assert.Null(result.ContentCommitment);
            Assert.Null(result.CiphertextDigest);
            Assert.Null(result.VerificationEvidenceDigest);
        }

        var lifecycle = await ReadLifecycleContextAsync(
            authenticationFailed.Source.ActorPrincipalId,
            authenticationFailed.Written.ObjectCustodyId!.Value);
        Assert.Equal(authenticationFailed.Written.ObjectCustodyId, lifecycle.ObjectCustodyId);
        Assert.Equal("CleanupPending", lifecycle.State);
        Assert.Equal("VerificationFailed", lifecycle.CleanupReasonCode);
        Assert.StartsWith("raw-export/", lifecycle.ObjectKey, StringComparison.Ordinal);

        foreach (var mutation in new[] { R211ByteMutation.DataType, R211ByteMutation.DataOrdinal })
        {
            var fixture = await WriteObjectAsync(
                Encoding.ASCII.GetBytes($"synthetic-r2-r211-{mutation}"), minio);
            await using var mutationDb = postgres.CreateDbContext();
            await using var mutationLookupDb = postgres.CreateDbContext();
            var kek = new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(mutationDb),
                new PostgresFixtureKekJournal(mutationLookupDb));
            var mutator = await R211DependencyRepairedDataReconciler.CreateAsync(
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                new RawExportR2Repository(mutationDb),
                new AttemptAeadVerificationOperationService(
                    mutationDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                new AttemptAeadEncryptionOperationService(
                    mutationDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                fixture,
                mutation);
            await MutateR211ExpectedCiphertextDigestAsync(
                fixture.Written.ObjectCustodyId!.Value,
                mutator.Manifest.CraftedObjectSha256);
            var result = await VerifyR211Async(fixture, mutator, null, null);
            Assert.True(
                result.Disposition == RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
                $"R211 dependency-repaired scenario {mutation} returned {result.Disposition}.");
            AssertR211DependencyManifest(mutator.Manifest, mutation);
            AssertR211Cleanup(await ReadObjectRowAsync(fixture.Written.ObjectCustodyId!.Value));
        }

        var deterministicMutations = new[]
        {
            R211ByteMutation.TrailingByte,
            R211ByteMutation.TruncatedFrame,
            R211ByteMutation.ReportedLength,
            R211ByteMutation.FinalType,
        };
        foreach (var mutation in deterministicMutations)
        {
            var fixture = await WriteObjectAsync(
                Encoding.ASCII.GetBytes($"synthetic-r2-r211-{mutation}"), minio);
            var mutator = new R211ByteMutatingReconciler(
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                mutation,
                fixture.Source);
            await mutator.PrepareAsync(await ReadR211LocatorAsync(fixture));
            if (mutation is R211ByteMutation.FinalType)
            {
                await MutateR211ExpectedCiphertextDigestAsync(
                    fixture.Written.ObjectCustodyId!.Value,
                    mutator.Evidence!.MutatedSha256);
            }
            var result = await VerifyR211Async(fixture, mutator, null, null);
            Assert.True(
                result.Disposition == RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
                $"R211 deterministic scenario {mutation} returned {result.Disposition}.");
            Assert.True(mutator.OpenCount >= 1);
            Assert.NotNull(mutator.Evidence);
            AssertR211MutationEvidence(mutator.Evidence!, mutation == R211ByteMutation.ReportedLength);
            AssertR211Cleanup(await ReadObjectRowAsync(fixture.Written.ObjectCustodyId!.Value));
        }

        var completionObservations = new List<(
            int Offset,
            RawExportR2VerificationDisposition Disposition,
            RawExportProvisionalObjectRow Row)>();
        foreach (var completionOffset in new[] { 0, 4, 12, 20, 52, 84, 148, 180 })
        {
            var fixture = await WriteObjectAsync(
                Encoding.ASCII.GetBytes($"synthetic-r2-r211-completion-{completionOffset}"), minio);
            await using var mutationDb = postgres.CreateDbContext();
            await using var mutationLookupDb = postgres.CreateDbContext();
            var kek = new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(mutationDb),
                new PostgresFixtureKekJournal(mutationLookupDb));
            var mutator = await R211ReencryptedCompletionReconciler.CreateAsync(
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                new RawExportR2Repository(mutationDb),
                new AttemptAeadVerificationOperationService(
                    mutationDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                new AttemptAeadEncryptionOperationService(
                    mutationDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                fixture,
                completionOffset);
            await MutateR211ExpectedCiphertextDigestAsync(
                fixture.Written.ObjectCustodyId!.Value,
                mutator.Evidence!.MutatedSha256);
            var result = await VerifyR211Async(fixture, mutator, null, null);
            AssertR211MutationEvidence(Assert.IsType<R211MutationEvidence>(mutator.Evidence), false);
            completionObservations.Add((
                completionOffset,
                result.Disposition,
                await ReadObjectRowAsync(fixture.Written.ObjectCustodyId!.Value)));
        }
        Assert.All(completionObservations, observation =>
        {
            Assert.True(
                observation.Disposition == RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
                $"R211 completion offset {observation.Offset} returned {observation.Disposition}.");
            AssertR211Cleanup(observation.Row);
        });

        var envelope = await WriteObjectAsync("synthetic-r2-r211-envelope"u8.ToArray(), minio);
        await using (var mutationDb = postgres.CreateDbContext())
        await using (var mutationLookupDb = postgres.CreateDbContext())
        {
            var kek = new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(mutationDb),
                new PostgresFixtureKekJournal(mutationLookupDb));
            var mutator = await R211ReencryptedCompletionReconciler.CreateAsync(
                new S3CompatibleProvisionalObjectReconciler(
                    minio.Options(ProvisionalObjectCapability.Reconciler)),
                new RawExportR2Repository(mutationDb),
                new AttemptAeadVerificationOperationService(
                    mutationDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                new AttemptAeadEncryptionOperationService(
                    mutationDb, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
                envelope,
                116);
            await MutateR211ExpectedCiphertextDigestAsync(
                envelope.Written.ObjectCustodyId!.Value,
                mutator.Evidence!.MutatedSha256);
            var result = await VerifyR211Async(envelope, mutator, null, null);
            Assert.Equal(RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup, result.Disposition);
            AssertR211MutationEvidence(Assert.IsType<R211MutationEvidence>(mutator.Evidence), false);
        }
        AssertR211Cleanup(await ReadObjectRowAsync(envelope.Written.ObjectCustodyId!.Value));

        var objectDigestMismatch = await WriteObjectAsync("synthetic-r2-r211-object-digest"u8.ToArray(), minio);
        var canonicalObjectBytes = await ReadR211ObjectBytesAsync(objectDigestMismatch, minio);
        await MutateR211ExpectedCiphertextDigestAsync(
            objectDigestMismatch.Written.ObjectCustodyId!.Value,
            Enumerable.Repeat((byte)0x7e, 32).ToArray());
        var digestResult = await VerifyR211Async(objectDigestMismatch,
            new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
            null, null);
        Assert.Equal(RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup, digestResult.Disposition);
        Assert.Equal(canonicalObjectBytes, await ReadR211ObjectBytesAsync(objectDigestMismatch, minio));
        AssertR211Cleanup(await ReadObjectRowAsync(objectDigestMismatch.Written.ObjectCustodyId!.Value));

        var commitmentMismatch = await WriteObjectAsync("synthetic-r2-r211-commitment-mismatch"u8.ToArray(), minio);
        var mismatchCommitment = new R211CommitmentProbe(
            ContentCommitmentResult.Success(Enumerable.Repeat((byte)0x7d, 32).ToArray()));
        var commitmentMismatchResult = await VerifyR211Async(
            commitmentMismatch,
            new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
            null, mismatchCommitment);
        Assert.Equal(1, mismatchCommitment.CallCount);
        Assert.True(mismatchCommitment.ReturnedSuccess);
        Assert.Equal(RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup, commitmentMismatchResult.Disposition);
        AssertR211Cleanup(await ReadObjectRowAsync(commitmentMismatch.Written.ObjectCustodyId!.Value));

        var exactReadUncertain = await WriteObjectAsync("synthetic-r2-r211-exact-read"u8.ToArray(), minio);
        var throwingRead = new R211ThrowingReadReconciler();
        var exactReadResult = await VerifyR211Async(exactReadUncertain, throwingRead, null, null);
        Assert.Equal(1, throwingRead.OpenCount);
        Assert.Equal(RawExportR2VerificationDisposition.VerificationIndeterminateRetry, exactReadResult.Disposition);
        AssertR211Pending(await ReadObjectRowAsync(exactReadUncertain.Written.ObjectCustodyId!.Value));

        var commitmentUnavailable = await WriteObjectAsync("synthetic-r2-r211-commitment-unavailable"u8.ToArray(), minio);
        var unavailable = new R211CommitmentProbe(
            ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure));
        var unavailableResult = await VerifyR211Async(commitmentUnavailable,
            new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
            null, unavailable);
        Assert.Equal(1, unavailable.CallCount);
        Assert.False(unavailable.ReturnedSuccess);
        Assert.Equal(RawExportR2VerificationDisposition.VerificationIndeterminateRetry, unavailableResult.Disposition);
        AssertR211Pending(await ReadObjectRowAsync(commitmentUnavailable.Written.ObjectCustodyId!.Value));

        var dbReadUncertain = await WriteObjectAsync("synthetic-r2-r211-db-read"u8.ToArray(), minio);
        await RenameR211FunctionAsync(
            "raw_export_read_provisional_object_reconcile_context(uuid)",
            "raw_export_read_provisional_object_reconcile_context_r211_unavailable");
        try
        {
            var dbReadResult = await VerifyR211Async(dbReadUncertain,
                new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
                null, null);
            Assert.Equal(RawExportR2VerificationDisposition.VerificationIndeterminateRetry, dbReadResult.Disposition);
        }
        finally
        {
            await RenameR211FunctionAsync(
                "raw_export_read_provisional_object_reconcile_context_r211_unavailable(uuid)",
                "raw_export_read_provisional_object_reconcile_context");
        }
        AssertR211Pending(await ReadObjectRowAsync(dbReadUncertain.Written.ObjectCustodyId!.Value));

        var transitionUncertain = await WriteObjectAsync("synthetic-r2-r211-db-transition"u8.ToArray(), minio);
        var transitionProbe = new R211TransitionFailureCommitment(
            CreateBrokerProvider,
            RenameR211FunctionAsync);
        try
        {
            var transitionResult = await VerifyR211Async(transitionUncertain,
                new S3CompatibleProvisionalObjectReconciler(minio.Options(ProvisionalObjectCapability.Reconciler)),
                null, transitionProbe);
            Assert.Equal(1, transitionProbe.CallCount);
            Assert.True(transitionProbe.TransitionDisabled);
            Assert.Equal(RawExportR2VerificationDisposition.VerificationIndeterminateRetry, transitionResult.Disposition);
        }
        finally
        {
            if (transitionProbe.TransitionDisabled)
                await RenameR211FunctionAsync(
                    "raw_export_mark_provisional_object_verified_r211_unavailable(uuid,bigint,bytea)",
                    "raw_export_mark_provisional_object_verified");
        }
        AssertR211Pending(await ReadObjectRowAsync(transitionUncertain.Written.ObjectCustodyId!.Value));
    }

    [Fact]
    public async Task R212_verified_transition_binds_exact_object_frame_and_content_evidence()
    {
        var plaintext = "synthetic-r2-verification-evidence"u8.ToArray();
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var written = await WriteObjectAsync(plaintext, minio);

        await using (var db = postgres.CreateDbContext())
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(written.Source.ActorPrincipalId, CancellationToken.None);
            var stale = await repository.MarkVerifiedAsync(
                written.Written.ObjectCustodyId!.Value,
                written.Written.StateRevision!.Value + 1,
                Enumerable.Repeat((byte)0xaa, 32).ToArray(),
                CancellationToken.None);
            Assert.Equal("StateConflict", stale.OutcomeCode);
            var row = await db.RawExportProvisionalObjects.SingleAsync(
                value => value.ObjectCustodyId == written.Written.ObjectCustodyId);
            Assert.Equal("ObjectPresentPendingVerification", row.State);
            Assert.Null(row.VerificationEvidenceDigest);
        }

        await using (var connection = await OpenAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await SetActorAsync(connection, transaction, written.Source.ActorPrincipalId);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.raw_export_mark_provisional_object_verified(@id,@revision,NULL)",
                connection,
                transaction);
            command.Parameters.AddWithValue("id", written.Written.ObjectCustodyId!.Value);
            command.Parameters.AddWithValue("revision", written.Written.StateRevision!.Value);
            var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
                await command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_PROVISIONAL_OBJECT_ARGUMENT_INVALID", exception.MessageText);
        }

        await using var brokerProvider = CreateBrokerProvider();
        await using var commitmentScope = brokerProvider.CreateAsyncScope();
        await using var verifyDb = postgres.CreateDbContext();
        await using var verifyLookupDb = postgres.CreateDbContext();
        var verifyKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(verifyLookupDb));
        var verified = await new RawExportR2CompletionVerifier(
            new RawExportR2Repository(verifyDb),
            new S3CompatibleProvisionalObjectReconciler(
                minio.Options(ProvisionalObjectCapability.Reconciler)),
            new AttemptAeadVerificationOperationService(
                verifyDb,
                verifyKek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>())
            .ExecuteAsync(
                new(
                    written.Source.ActorPrincipalId,
                    written.Source.AttemptId,
                    written.Source.Revision,
                    written.Source.Fence,
                    written.Written.ObjectCustodyId!.Value),
                CancellationToken.None);
        Assert.Equal(RawExportR2VerificationDisposition.Verified, verified.Disposition);
        Assert.NotNull(verified.VerificationEvidenceDigest);

        await using var assertionDb = postgres.CreateDbContext();
        var objectRow = await assertionDb.RawExportProvisionalObjects.SingleAsync(
            value => value.ObjectCustodyId == written.Written.ObjectCustodyId);
        Assert.Equal(verified.VerificationEvidenceDigest, objectRow.VerificationEvidenceDigest);
        var eventRow = await assertionDb.RawExportProvisionalObjectEvents.SingleAsync(
            value => value.ObjectCustodyId == written.Written.ObjectCustodyId
                && value.ToState == "VerifiedCompleted");
        Assert.Equal(verified.VerificationEvidenceDigest, eventRow.EvidenceDigest);

        var repositoryAfter = new RawExportR2Repository(assertionDb);
        await repositoryAfter.SetActorAsync(written.Source.ActorPrincipalId, CancellationToken.None);
        var exactReplay = await repositoryAfter.MarkVerifiedAsync(
            objectRow.ObjectCustodyId,
            objectRow.StateRevision,
            verified.VerificationEvidenceDigest!,
            CancellationToken.None);
        Assert.Equal("ExistingMatch", exactReplay.OutcomeCode);
        var differentReplay = await repositoryAfter.MarkVerifiedAsync(
            objectRow.ObjectCustodyId,
            objectRow.StateRevision,
            Enumerable.Repeat((byte)0x55, 32).ToArray(),
            CancellationToken.None);
        Assert.Equal("StateConflict", differentReplay.OutcomeCode);
    }

    [Fact]
    public async Task R213_no_database_transaction_spans_source_key_aead_or_object_io()
    {
        var plaintext = "synthetic-r2-no-db-transaction-over-io"u8.ToArray();
        var source = await SeedReservedSourceAsync(plaintext);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var brokerProvider = CreateBrokerProvider();
        await using var commitmentScope = brokerProvider.CreateAsyncScope();

        await using var writerDb = postgres.CreateDbContext();
        await using var writerLookupDb = postgres.CreateDbContext();
        var writerKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(writerDb),
            new PostgresFixtureKekJournal(writerLookupDb));
        var key = new TransactionAssertingProvisioner(
            writerDb,
            new PostgresAttemptKeyReservationProvider(
                writerDb,
                new PostgresKeyProviderOperationMap(writerDb),
                writerKek));
        var encryption = new TransactionAssertingEncryption(
            writerDb,
            new AttemptAeadEncryptionOperationService(
                writerDb,
                writerKek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())));
        var writerProvider = new S3CompatibleProvisionalObjectWriter(
            minio.Options(ProvisionalObjectCapability.Writer));
        var writer = new TransactionAssertingWriter(writerDb, writerProvider);
        var commitment = new TransactionAssertingCommitment(
            writerDb,
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
        var callerSource = new TransactionAssertingSourceStream(writerDb, plaintext);
        var written = await new RawExportR2EncryptionOrchestrator(
            new RawExportR2Repository(writerDb), key, encryption, commitment, writer)
            .ExecuteAsync(
                new(
                    source.ActorPrincipalId, source.AttemptKeyReservationId, source.AttemptId,
                    source.SourceArtifactId, source.Revision, source.Fence, callerSource),
                CancellationToken.None);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        Assert.True(key.CallCount > 0);
        Assert.True(encryption.CallCount > 0);
        Assert.True(commitment.CallCount > 0);
        Assert.Equal(1, writer.CallCount);
        Assert.True(callerSource.ReadCount > 0);

        await using var verifyDb = postgres.CreateDbContext();
        await using var verifyLookupDb = postgres.CreateDbContext();
        var verifyKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(verifyLookupDb));
        var reconcilerProvider = new S3CompatibleProvisionalObjectReconciler(
            minio.Options(ProvisionalObjectCapability.Reconciler));
        var reconciler = new TransactionAssertingReconciler(verifyDb, reconcilerProvider);
        var verification = new TransactionAssertingVerification(
            verifyDb,
            new AttemptAeadVerificationOperationService(
                verifyDb,
                verifyKek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())));
        var verifyCommitment = new TransactionAssertingCommitment(
            verifyDb,
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
        var verified = await new RawExportR2CompletionVerifier(
            new RawExportR2Repository(verifyDb), reconciler, verification, verifyCommitment)
            .ExecuteAsync(
                new(
                    source.ActorPrincipalId, source.AttemptId, source.Revision, source.Fence,
                    written.ObjectCustodyId!.Value),
                CancellationToken.None);
        Assert.Equal(RawExportR2VerificationDisposition.Verified, verified.Disposition);
        Assert.Equal(1, reconciler.OpenReadCount);
        Assert.True(verification.CallCount > 0);
        Assert.True(verifyCommitment.CallCount > 0);
    }

    [Fact]
    public async Task R215_termination_is_append_once_on_exact_revision_fence_object_or_terminal_key_evidence()
    {
        var active = await SeedReservedSourceAsync("r2-active-zero-object"u8.ToArray());
        await ProvisionKeyAsync(active);
        await AssertR215ZeroObjectTerminationAsync(active, "Active", "StateConflict");

        await using (var connection = await OpenAsync())
        await using (var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_revoke_attempt_key_reservation(@id,'r2-terminal-key')",
            connection))
        {
            command.Parameters.AddWithValue("id", active.AttemptKeyReservationId);
            Assert.Equal("Revoked", await command.ExecuteScalarAsync());
        }
        await using (var db = postgres.CreateDbContext())
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(active.ActorPrincipalId, CancellationToken.None);
            Assert.Equal(
                "StateConflict",
                await repository.TerminateAsync(
                    active.AttemptId, active.Revision + 1, active.Fence,
                    "TerminatedBeforeStart", CancellationToken.None));
            Assert.Equal(
                "StateConflict",
                await repository.TerminateAsync(
                    active.AttemptId, active.Revision, active.Fence + 1,
                    "TerminatedBeforeStart", CancellationToken.None));
            Assert.Equal(
                "Terminated",
                await repository.TerminateAsync(
                    active.AttemptId, active.Revision, active.Fence,
                    "TerminatedBeforeStart", CancellationToken.None));
            Assert.Equal(
                "ExistingMatch",
                await repository.TerminateAsync(
                    active.AttemptId, active.Revision, active.Fence,
                    "TerminatedBeforeStart", CancellationToken.None));
            Assert.Equal(
                "StateConflict",
                await repository.TerminateAsync(
                    active.AttemptId, active.Revision, active.Fence,
                    "Terminated", CancellationToken.None));
        }

        foreach (var rejectedDisposition in new[]
                 {
                     "PreparingLive",
                     "ProviderOutcomeUnknown",
                     "ProviderCorruptOrUnverifiable",
                     "ProviderCleanupRequired",
                     "ReadyForFreshPreparation",
                     "AbandonRequested",
                 })
        {
            var rejected = await CreateR215KeyDispositionAsync(rejectedDisposition);
            await AssertR215ZeroObjectTerminationAsync(
                rejected,
                rejectedDisposition,
                "StateConflict");
        }

        var abandoned = await CreateR215KeyDispositionAsync("ReservationAbandoned");
        await AssertR215ZeroObjectTerminationAsync(
            abandoned,
            "ReservationAbandoned",
            "Terminated");

        await using (var connection = await OpenAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using var command = new NpgsqlCommand(
                """
                SET LOCAL ROLE tagekyc_raw_export_deployer;
                SELECT pg_catalog.set_config(
                  'tagekyc.raw_export_source_core_write_context',
                  'tip88c1-r2-termination-v1',true);
                UPDATE tagekyc.raw_export_source_encryption_attempts
                SET "Fence"="Fence"+1
                WHERE "AttemptId"=@attempt;
                """,
                connection,
                transaction);
            command.Parameters.AddWithValue("attempt", active.AttemptId);
            var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
                await command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_SOURCE_CORE_APPEND_ONLY", exception.MessageText);
            await transaction.RollbackAsync();
        }

        foreach (var rejectedState in new[]
                 {
                     "Initiated",
                     "PutInFlight",
                     "PutOutcomeUnknown",
                     "NoObjectEstablished",
                     "ObjectConflict",
                 })
        {
            var rejected = await CreateR215ObjectStateAsync(rejectedState, null);
            await AssertR215ObjectStateTerminationAsync(
                rejected.Source,
                rejected.ObjectCustodyId,
                rejectedState,
                "StateConflict");
        }

        await using var minio = await DurableObjectMinioFixture.StartAsync();
        foreach (var rejectedState in new[]
                 {
                     "ObjectPresentPendingVerification",
                     "VerifiedCompleted",
                     "CleanupPending",
                 })
        {
            var rejected = await CreateR215ObjectStateAsync(rejectedState, minio);
            await AssertR215ObjectStateTerminationAsync(
                rejected.Source,
                rejected.ObjectCustodyId,
                rejectedState,
                "StateConflict");
        }

        var deleted = await WriteObjectAsync("r2-delete-terminal"u8.ToArray(), minio);
        Assert.Equal(
            RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
            (await MarkAuthenticationFailureAsync(deleted, minio)).Disposition);
        var deletedRow = await ReadObjectRowAsync(deleted.Written.ObjectCustodyId!.Value);
        using (var lifecycle = new S3CompatibleProvisionalObjectLifecycle(
                   minio.Options(ProvisionalObjectCapability.Lifecycle)))
        {
            var deletion = await lifecycle.DeleteExactAsync(
                new(
                    deletedRow.ProvisionalObjectIdentity,
                    deletedRow.ObjectKey,
                    deletedRow.ObjectBindingDigest),
                CancellationToken.None);
            Assert.Equal(ExactDeleteOutcome.DeletedAcknowledged, deletion.Outcome);
            Assert.Equal(204, deletion.StatusCode);
        }
        var deleteEvidence = C1HashCanonical.Compute(
            "tip-88c1-object-delete-ack-evidence-v1",
            new C1HashCanonical.Scalar(
                Convert.ToHexString(deletedRow.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar(deletedRow.StateRevision.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar("DeleteAcknowledged"),
            new C1HashCanonical.Scalar("204"));
        Assert.Equal(
            "Deleted",
            (await RecordDeleteAcknowledgedAsync(
                deleted.Source.ActorPrincipalId,
                deletedRow.ObjectCustodyId,
                deletedRow.StateRevision,
                deleteEvidence)).OutcomeCode);
        await AssertR215ObjectStateTerminationAsync(
            deleted.Source,
            deletedRow.ObjectCustodyId,
            "Deleted",
            "Terminated",
            proveObjectRowLock: true);

        var quarantined = await WriteObjectAsync("r2-quarantine-terminal"u8.ToArray(), minio);
        Assert.Equal(
            RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
            (await MarkAuthenticationFailureAsync(quarantined, minio)).Disposition);
        var quarantineRow = await ReadObjectRowAsync(quarantined.Written.ObjectCustodyId!.Value);
        var quarantineEvidence = C1HashCanonical.Compute(
            "tip-88c1-object-cleanup-quarantine-evidence-v1",
            new C1HashCanonical.Scalar(
                Convert.ToHexString(quarantineRow.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar("CleanupPending"),
            new C1HashCanonical.Scalar(quarantineRow.StateRevision.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar("DeleteOutcomeIndeterminateTerminal"),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(quarantineRow.CleanupEvidenceDigest!).ToLowerInvariant()));
        Assert.Equal(
            "Quarantined",
            (await RecordQuarantinedAsync(
                quarantined.Source.ActorPrincipalId,
                quarantineRow.ObjectCustodyId,
                quarantineRow.StateRevision,
                quarantineEvidence)).OutcomeCode);
        await AssertR215ObjectStateTerminationAsync(
            quarantined.Source,
            quarantineRow.ObjectCustodyId,
            "Quarantined",
            "Terminated");
    }

    [Fact]
    public async Task R216_reconciliation_can_terminate_but_cannot_allocate_replacement_attempt()
    {
        var source = await SeedReservedSourceAsync(
            Encoding.ASCII.GetBytes("synthetic-r2-not-armed-termination"));
        await using (var db = postgres.CreateDbContext())
        await using (var lookupDb = postgres.CreateDbContext())
        {
            var kek = new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(db),
                new PostgresFixtureKekJournal(lookupDb));
            var provisioning = new PostgresAttemptKeyReservationProvider(
                db,
                new PostgresKeyProviderOperationMap(db),
                kek);
            var provisioned = await provisioning.ProvisionAsync(
                new(source.AttemptKeyReservationId, source.AttemptId, source.SourceArtifactId),
                CancellationToken.None);
            Assert.Contains(
                provisioned.Outcome,
                new[] { AttemptKeyProvisioningOutcome.Activated, AttemptKeyProvisioningOutcome.ExistingMatch });
        }

        Guid objectCustodyId;
        await using (var db = postgres.CreateDbContext())
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(source.ActorPrincipalId, CancellationToken.None);
            var context = await repository.ReadEncryptionContextAsync(
                source.AttemptId,
                source.Revision,
                source.Fence,
                CancellationToken.None);
            Assert.NotNull(context);
            var began = await repository.BeginAsync(
                source.AttemptId,
                source.Revision,
                source.Fence,
                CancellationToken.None);
            Assert.Equal("Created", began.Mutation.OutcomeCode);
            objectCustodyId = began.Mutation.ObjectCustodyId;
            var notArmed = await repository.RecordNotArmedAsync(
                objectCustodyId,
                began.Mutation.StateRevision,
                began.ObjectBindingDigest,
                CancellationToken.None);
            Assert.Equal("NoObjectEstablished", notArmed.ObjectState);
            Assert.Equal(
                "Terminated",
                await repository.TerminateAsync(
                    source.AttemptId,
                    source.Revision,
                    source.Fence,
                    "TerminatedBeforeStart",
                    CancellationToken.None));
        }

        var lifecycle = await ReadLifecycleContextAsync(source.ActorPrincipalId, objectCustodyId);
        Assert.Equal(objectCustodyId, lifecycle.ObjectCustodyId);
        Assert.Equal("NoObjectEstablished", lifecycle.State);
        Assert.NotEmpty(lifecycle.ObjectKey);
        Assert.Null(lifecycle.CleanupReasonCode);

        const string completeSignature =
            "tagekyc.complete_raw_export_source_ingress_claim(" +
            "uuid,text,text,text,uuid,bigint,bigint,text,timestamp with time zone," +
            "text,bytea,integer,text,integer,bytea,integer,text,integer,bytea," +
            "bigint,text,timestamp with time zone,timestamp with time zone," +
            "timestamp with time zone,integer,text,text,integer,text,integer,text," +
            "bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)";
        await using (var connection = await OpenAsync())
        await using (var command = new NpgsqlCommand(
            """
            SELECT
              pg_catalog.has_function_privilege(
                'tagekyc_raw_export_reconciler',@signature,'EXECUTE'),
              pg_catalog.has_table_privilege(
                'tagekyc_raw_export_reconciler',
                'tagekyc.raw_export_source_encryption_attempts','INSERT');
            """,
            connection))
        {
            command.Parameters.AddWithValue("signature", completeSignature);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.False(reader.GetBoolean(0));
            Assert.False(reader.GetBoolean(1));
        }

        await using var assertionDb = postgres.CreateDbContext();
        var attempt = await assertionDb.RawExportSourceEncryptionAttempts
            .SingleAsync(value => value.AttemptId == source.AttemptId);
        Assert.Equal("TerminatedBeforeStart", attempt.R2TerminationDisposition);
        Assert.NotNull(attempt.R2TerminatedAtUtc);
        Assert.Equal(
            1,
            await assertionDb.RawExportSourceEncryptionAttempts.CountAsync(
                value => value.SourceArtifactId == source.SourceArtifactId));
        var head = await assertionDb.RawExportSourceHeads
            .SingleAsync(value => value.SourceArtifactId == source.SourceArtifactId);
        Assert.Equal(source.AttemptId, head.CurrentEncryptionAttemptId);
    }

    [Fact]
    public async Task R217_apply_down_reapply_restores_guard_constraint_functions_and_acl()
    {
        const string durableObjectMigration = "20260804120000_Tip88C1B2DurableObjectCustody";
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync(durableObjectMigration);
        var baselineReconcile = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)");
        var baselineLifecycle = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)");
        var baselineGuard = await ReadFunctionPostureAsync(
            "tagekyc.enforce_raw_export_source_core_write()");
        var baselineConstraint = await ReadConstraintDefinitionAsync(
            "ck_raw_export_source_attempt_values");
        Assert.DoesNotContain("\"ObjectKey\"::text", baselineReconcile.Definition, StringComparison.Ordinal);
        Assert.DoesNotContain("\"CleanupReasonCode\"::text", baselineLifecycle.Definition, StringComparison.Ordinal);
        Assert.False(await ColumnExistsAsync("R2TerminatedAtUtc"));

        await migrator.MigrateAsync();
        var upReconcile = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)");
        var upLifecycle = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)");
        AssertSameFrozenPosture(baselineReconcile, upReconcile);
        AssertSameFrozenPosture(baselineLifecycle, upLifecycle);
        Assert.Equal(2, CountOccurrences(upReconcile.Definition, "::text"));
        Assert.Equal(3, CountOccurrences(upLifecycle.Definition, "::text"));
        Assert.True(await ColumnExistsAsync("R2TerminatedAtUtc"));
        Assert.True(await FunctionExistsAsync(
            "tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)"));
        Assert.True(await FunctionExistsAsync(
            "tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint)"));
        Assert.True(await FunctionExistsAsync(
            "tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)"));

        await migrator.MigrateAsync(durableObjectMigration);
        var downReconcile = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)");
        var downLifecycle = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)");
        var downGuard = await ReadFunctionPostureAsync(
            "tagekyc.enforce_raw_export_source_core_write()");
        Assert.Equal(baselineReconcile, downReconcile);
        Assert.Equal(baselineLifecycle, downLifecycle);
        Assert.Equal(baselineGuard, downGuard);
        Assert.Equal(
            baselineConstraint,
            await ReadConstraintDefinitionAsync("ck_raw_export_source_attempt_values"));
        Assert.False(await ColumnExistsAsync("R2TerminatedAtUtc"));
        Assert.False(await FunctionExistsAsync(
            "tagekyc.raw_export_read_source_encryption_context(uuid,bigint,bigint)"));
        Assert.False(await FunctionExistsAsync(
            "tagekyc.raw_export_read_source_verification_context(uuid,bigint,bigint)"));
        Assert.False(await FunctionExistsAsync(
            "tagekyc.raw_export_terminate_source_encryption_attempt(uuid,bigint,bigint,text)"));

        await migrator.MigrateAsync();
        var reappliedReconcile = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_reconcile_context(uuid)");
        var reappliedLifecycle = await ReadFunctionPostureAsync(
            "tagekyc.raw_export_read_provisional_object_lifecycle_context(uuid)");
        Assert.Equal(upReconcile, reappliedReconcile);
        Assert.Equal(upLifecycle, reappliedLifecycle);
        Assert.True(await ColumnExistsAsync("R2TerminatedAtUtc"));
    }

    [Fact]
    public async Task R218_minio_restart_recovers_and_verifies_exact_single_part_object()
    {
        var plaintext = Encoding.ASCII.GetBytes("synthetic-r2-restart-evidence");
        var seeded = await SeedReservedSourceAsync(plaintext);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        await using var brokerProvider = CreateBrokerProvider();
        await using var commitmentScope = brokerProvider.CreateAsyncScope();
        var commitments = commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>();

        await using var writerDb = postgres.CreateDbContext();
        await using var writerLookupDb = postgres.CreateDbContext();
        var writerKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(writerDb),
            new PostgresFixtureKekJournal(writerLookupDb));
        var keyProvisioning = new PostgresAttemptKeyReservationProvider(
            writerDb,
            new PostgresKeyProviderOperationMap(writerDb),
            writerKek);
        var keyOptions = DurableKeyCustodyOptions.Resolve(new ConfigurationManager());
        var repository = new RawExportR2Repository(writerDb);
        var orchestrator = new RawExportR2EncryptionOrchestrator(
            repository,
            keyProvisioning,
            new AttemptAeadEncryptionOperationService(writerDb, writerKek, keyOptions),
            commitments,
            new S3CompatibleProvisionalObjectWriter(
                minio.Options(ProvisionalObjectCapability.Writer)));

        await using var source = new MemoryStream(plaintext, writable: false);
        var written = await orchestrator.ExecuteAsync(
            new(
                seeded.ActorPrincipalId,
                seeded.AttemptKeyReservationId,
                seeded.AttemptId,
                seeded.SourceArtifactId,
                seeded.Revision,
                seeded.Fence,
                source),
            CancellationToken.None);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        Assert.NotNull(written.ObjectCustodyId);
        Assert.Equal("ObjectPresentPendingVerification", written.ObjectState);
        Assert.True(source.CanRead);

        await minio.RestartAsync();

        var reconcileProjection = await ReadReconcileContextAsync(
            seeded.ActorPrincipalId,
            written.ObjectCustodyId!.Value);
        Assert.Equal(written.ObjectCustodyId, reconcileProjection.ObjectCustodyId);
        Assert.Equal("ObjectPresentPendingVerification", reconcileProjection.State);
        Assert.StartsWith("raw-export/", reconcileProjection.ObjectKey, StringComparison.Ordinal);

        using (var wrongCredentialClient = minio.CreateCapabilityClient(
                   ProvisionalObjectCapability.Writer))
        {
            var denied = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(() =>
                wrongCredentialClient.GetObjectAsync(minio.BucketName, reconcileProjection.ObjectKey));
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, denied.StatusCode);
        }

        var beforeWrongCredentialAttempt = await ReadR215AttemptSnapshotAsync(seeded.AttemptId);
        var beforeWrongCredentialObject = await ReadR215ObjectSnapshotAsync(
            seeded.AttemptId,
            written.ObjectCustodyId.Value);
        Assert.Equal("ObjectPresentPendingVerification", beforeWrongCredentialObject.State);
        Assert.Null(beforeWrongCredentialAttempt.TerminationDisposition);
        Assert.Null(beforeWrongCredentialAttempt.TerminatedAtUtc);

        await using (var wrongCredentialDb = postgres.CreateDbContext())
        await using (var wrongCredentialLookupDb = postgres.CreateDbContext())
        {
            var wrongCredentialKek = new FixtureDurableKekOperationProvider(
                new PostgresFixtureKekJournal(wrongCredentialDb),
                new PostgresFixtureKekJournal(wrongCredentialLookupDb));
            var wrongCredentialOptions = minio.Options(ProvisionalObjectCapability.Writer) with
            {
                Capability = ProvisionalObjectCapability.Reconciler,
            };
            var wrongCredentialVerifier = new RawExportR2CompletionVerifier(
                new RawExportR2Repository(wrongCredentialDb),
                new S3CompatibleProvisionalObjectReconciler(wrongCredentialOptions),
                new AttemptAeadVerificationOperationService(
                    wrongCredentialDb,
                    wrongCredentialKek,
                    keyOptions),
                commitments);
            var wrongCredentialResult = await wrongCredentialVerifier.ExecuteAsync(
                new(
                    seeded.ActorPrincipalId,
                    seeded.AttemptId,
                    seeded.Revision,
                    seeded.Fence,
                    written.ObjectCustodyId.Value),
                CancellationToken.None);
            Assert.Equal(
                RawExportR2VerificationDisposition.VerificationIndeterminateRetry,
                wrongCredentialResult.Disposition);
        }

        var afterWrongCredentialAttempt = await ReadR215AttemptSnapshotAsync(seeded.AttemptId);
        var afterWrongCredentialObject = await ReadR215ObjectSnapshotAsync(
            seeded.AttemptId,
            written.ObjectCustodyId.Value);
        Assert.Equal(beforeWrongCredentialAttempt, afterWrongCredentialAttempt);
        Assert.Equal(beforeWrongCredentialObject, afterWrongCredentialObject);
        Assert.Equal("ObjectPresentPendingVerification", afterWrongCredentialObject.State);
        Assert.Null(afterWrongCredentialAttempt.TerminationDisposition);
        Assert.Null(afterWrongCredentialAttempt.TerminatedAtUtc);

        await using var verifyDb = postgres.CreateDbContext();
        await using var verifyLookupDb = postgres.CreateDbContext();
        var verifyKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(verifyDb),
            new PostgresFixtureKekJournal(verifyLookupDb));
        var verifier = new RawExportR2CompletionVerifier(
            new RawExportR2Repository(verifyDb),
            new S3CompatibleProvisionalObjectReconciler(
                minio.Options(ProvisionalObjectCapability.Reconciler)),
            new AttemptAeadVerificationOperationService(verifyDb, verifyKek, keyOptions),
            commitments);
        var verified = await verifier.ExecuteAsync(
            new(
                seeded.ActorPrincipalId,
                seeded.AttemptId,
                seeded.Revision,
                seeded.Fence,
                written.ObjectCustodyId!.Value),
            CancellationToken.None);

        Assert.Equal(RawExportR2VerificationDisposition.Verified, verified.Disposition);
        Assert.Equal(plaintext.Length, verified.VerifiedPlaintextLength);
        Assert.Equal(seeded.ContentCommitment, verified.ContentCommitment);
        Assert.Equal(written.ObjectCustodyId, verified.ObjectCustodyId);
        Assert.NotNull(verified.CiphertextDigest);
        Assert.NotNull(verified.VerificationEvidenceDigest);
        Assert.NotEqual(plaintext, verified.CiphertextDigest!);

        await using var assertionDb = postgres.CreateDbContext();
        var objectRow = await assertionDb.RawExportProvisionalObjects
            .SingleAsync(row => row.ObjectCustodyId == written.ObjectCustodyId);
        Assert.Equal(written.ObjectCustodyId, objectRow.ObjectCustodyId);
        Assert.Equal(seeded.AttemptId, objectRow.AttemptId);
        Assert.Equal(seeded.AttemptKeyReservationId, objectRow.AttemptKeyReservationId);
        Assert.Equal("VerifiedCompleted", objectRow.State);
        Assert.Equal(verified.VerificationEvidenceDigest, objectRow.VerificationEvidenceDigest);
        Assert.Equal(verified.CiphertextLength, objectRow.CiphertextLength);
        Assert.Equal(verified.CiphertextDigest, objectRow.CiphertextDigest);
    }

    private async Task<WrittenSourceFixture> WriteObjectAsync(
        byte[] plaintext,
        DurableObjectMinioFixture minio)
    {
        var source = await SeedReservedSourceAsync(plaintext);
        await using var brokerProvider = CreateBrokerProvider();
        await using var commitmentScope = brokerProvider.CreateAsyncScope();
        await using var writerDb = postgres.CreateDbContext();
        await using var writerLookupDb = postgres.CreateDbContext();
        var writerKek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(writerDb),
            new PostgresFixtureKekJournal(writerLookupDb));
        var keyProvisioning = new PostgresAttemptKeyReservationProvider(
            writerDb,
            new PostgresKeyProviderOperationMap(writerDb),
            writerKek);
        var orchestrator = new RawExportR2EncryptionOrchestrator(
            new RawExportR2Repository(writerDb),
            keyProvisioning,
            new AttemptAeadEncryptionOperationService(
                writerDb,
                writerKek,
                DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            commitmentScope.ServiceProvider.GetRequiredService<IContentCommitmentService>(),
            new S3CompatibleProvisionalObjectWriter(
                minio.Options(ProvisionalObjectCapability.Writer)));
        await using var stream = new MemoryStream(plaintext, writable: false);
        var written = await orchestrator.ExecuteAsync(
            new(
                source.ActorPrincipalId,
                source.AttemptKeyReservationId,
                source.AttemptId,
                source.SourceArtifactId,
                source.Revision,
                source.Fence,
                stream),
            CancellationToken.None);
        Assert.Equal(RawExportR2WriterDisposition.PendingVerification, written.Disposition);
        Assert.NotNull(written.ObjectCustodyId);
        return new(source, written);
    }

    private async Task<RawExportR2VerifierResult> MarkAuthenticationFailureAsync(
        WrittenSourceFixture written,
        DurableObjectMinioFixture minio)
    {
        await using var provider = CreateBrokerProvider();
        await using var scope = provider.CreateAsyncScope();
        await using var db = postgres.CreateDbContext();
        return await new RawExportR2CompletionVerifier(
            new RawExportR2Repository(db),
            new S3CompatibleProvisionalObjectReconciler(
                minio.Options(ProvisionalObjectCapability.Reconciler)),
            new FixedVerificationOperation(AttemptAeadVerificationOutcome.AuthenticationFailed),
            scope.ServiceProvider.GetRequiredService<IContentCommitmentService>())
            .ExecuteAsync(
                new(
                    written.Source.ActorPrincipalId,
                    written.Source.AttemptId,
                    written.Source.Revision,
                    written.Source.Fence,
                    written.Written.ObjectCustodyId!.Value),
                CancellationToken.None);
    }

    private async Task<RawExportProvisionalObjectRow> ReadObjectRowAsync(Guid objectCustodyId)
    {
        await using var db = postgres.CreateDbContext();
        return await db.RawExportProvisionalObjects.AsNoTracking().SingleAsync(
            value => value.ObjectCustodyId == objectCustodyId);
    }

    private async Task<ProvisionalObjectMutationResult> RecordDeleteAcknowledgedAsync(
        Guid actor,
        Guid objectCustodyId,
        long revision,
        byte[] evidence) =>
        await ExecuteObjectMutationAsync(
            actor,
            "raw_export_record_provisional_object_delete_acknowledged",
            ("id", objectCustodyId), ("revision", revision), ("status", 204),
            ("evidence", evidence));

    private async Task<ProvisionalObjectMutationResult> RecordQuarantinedAsync(
        Guid actor,
        Guid objectCustodyId,
        long revision,
        byte[] evidence) =>
        await ExecuteObjectMutationAsync(
            actor,
            "raw_export_record_provisional_object_quarantined",
            ("id", objectCustodyId), ("revision", revision),
            ("reason", "DeleteOutcomeIndeterminateTerminal"), ("evidence", evidence));

    private async Task<ProvisionalObjectMutationResult> ExecuteObjectMutationAsync(
        Guid actor,
        string function,
        params (string Name, object Value)[] values)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand { Connection = connection, Transaction = transaction };
        foreach (var value in values)
            command.Parameters.AddWithValue(value.Name, value.Value);
        command.CommandText = $"SELECT * FROM tagekyc.{function}({string.Join(',', values.Select(value => '@' + value.Name))})";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var result = new ProvisionalObjectMutationResult(
            reader.GetString(0), reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3));
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task ProvisionKeyAsync(ReservedSourceFixture source)
    {
        await using var db = postgres.CreateDbContext();
        await using var lookupDb = postgres.CreateDbContext();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(db),
            new PostgresFixtureKekJournal(lookupDb));
        var provisioning = new PostgresAttemptKeyReservationProvider(
            db,
            new PostgresKeyProviderOperationMap(db),
            kek);
        var result = await provisioning.ProvisionAsync(
            new(source.AttemptKeyReservationId, source.AttemptId, source.SourceArtifactId),
            CancellationToken.None);
        Assert.Contains(
            result.Outcome,
            new[] { AttemptKeyProvisioningOutcome.Activated, AttemptKeyProvisioningOutcome.ExistingMatch });
    }

    private async Task<ReservedSourceFixture> CreateR215KeyDispositionAsync(string disposition)
    {
        var source = await SeedReservedSourceAsync(
            Encoding.ASCII.GetBytes($"r215-key-state-{disposition}"));
        var prepared = await PrepareR215KeyAsync(source);
        switch (disposition)
        {
            case "PreparingLive":
                break;
            case "ProviderOutcomeUnknown":
                Assert.Equal(
                    "ResolvedOutcomeUnknown",
                    await ResolveR215KeyAsync(prepared, "ProviderOutcomeUnknown"));
                break;
            case "ProviderCorruptOrUnverifiable":
                Assert.Equal(
                    "ResolvedCorrupt",
                    await ResolveR215KeyAsync(prepared, "CorruptOrUnverifiable"));
                break;
            case "ProviderCleanupRequired":
                Assert.Equal("CleanupRequired", await MarkR215CleanupRequiredAsync(prepared));
                break;
            case "ReadyForFreshPreparation":
                Assert.Equal("CleanupRequired", await MarkR215CleanupRequiredAsync(prepared));
                Assert.Equal("Acknowledged", await AcknowledgeR215CleanupAsync(prepared));
                break;
            case "AbandonRequested":
                Assert.Equal("AbandonRequested", await RequestR215AbandonAsync(prepared));
                break;
            case "ReservationAbandoned":
                Assert.Equal("AbandonRequested", await RequestR215AbandonAsync(prepared));
                Assert.Equal("CleanupRequired", await MarkR215CleanupRequiredAsync(prepared));
                Assert.Equal("Acknowledged", await AcknowledgeR215CleanupAsync(prepared));
                Assert.Equal("ReservationAbandoned", await FinalizeR215AbandonAsync(prepared));
                break;
            default:
                throw new InvalidOperationException($"R215_UNSUPPORTED_DISPOSITION:{disposition}");
        }

        return source;
    }

    private async Task<R215PreparedKeyFixture> PrepareR215KeyAsync(ReservedSourceFixture source)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE tagekyc_raw_export_custody_encryptor;");
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_prepare_attempt_key_reservation(@reservation,@attempt,@artifact)",
            connection,
            transaction);
        command.Parameters.AddWithValue("reservation", source.AttemptKeyReservationId);
        command.Parameters.AddWithValue("attempt", source.AttemptId);
        command.Parameters.AddWithValue("artifact", source.SourceArtifactId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("PreparingLive", reader.GetString(reader.GetOrdinal("outcome")));
        var prepared = new R215PreparedKeyFixture(
            source,
            reader.GetGuid(reader.GetOrdinal("provider_operation_id")),
            reader.GetGuid(reader.GetOrdinal("preparation_id")),
            reader.GetInt64(reader.GetOrdinal("preparation_fence")),
            reader.GetString(reader.GetOrdinal("provider_operation_token")));
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return prepared;
    }

    private async Task<string> ResolveR215KeyAsync(
        R215PreparedKeyFixture prepared,
        string resolution) =>
        await ExecuteR215KeyTransitionAsync(
            prepared,
            "tagekyc_raw_export_reconciler",
            null,
            """
            SELECT tagekyc.raw_export_resolve_attempt_key_provider_outcome(
              @reservation,@preparation,@fence,@resolution,NULL,'r215-provider-receipt',NULL)
            """,
            new NpgsqlParameter("resolution", resolution));

    private async Task<string> MarkR215CleanupRequiredAsync(R215PreparedKeyFixture prepared) =>
        await ExecuteR215KeyTransitionAsync(
            prepared,
            "tagekyc_raw_export_reconciler",
            null,
            """
            SELECT tagekyc.raw_export_mark_key_provider_cleanup_required(
              @operation,@reservation,@preparation,@fence,@token,'r215-cleanup-reference')
            """);

    private async Task<string> AcknowledgeR215CleanupAsync(R215PreparedKeyFixture prepared) =>
        await ExecuteR215KeyTransitionAsync(
            prepared,
            "tagekyc_raw_export_reconciler",
            null,
            """
            SELECT tagekyc.raw_export_acknowledge_key_provider_cleanup(
              @operation,@reservation,@preparation,@fence,@token,'Cleaned',
              'r215-cleanup-reference','r215-cleanup-receipt')
            """);

    private async Task<string> RequestR215AbandonAsync(R215PreparedKeyFixture prepared) =>
        await ExecuteR215KeyTransitionAsync(
            prepared,
            "tagekyc_raw_export_lifecycle",
            prepared.Source.ActorPrincipalId,
            """
            SELECT tagekyc.raw_export_request_abandon_attempt_key_reservation(
              @reservation,'r215-operator-request')
            """);

    private async Task<string> FinalizeR215AbandonAsync(R215PreparedKeyFixture prepared) =>
        await ExecuteR215KeyTransitionAsync(
            prepared,
            "tagekyc_raw_export_lifecycle",
            prepared.Source.ActorPrincipalId,
            "SELECT tagekyc.raw_export_finalize_abandon_attempt_key_reservation(@reservation)");

    private async Task<string> ExecuteR215KeyTransitionAsync(
        R215PreparedKeyFixture prepared,
        string role,
        Guid? actor,
        string sql,
        params NpgsqlParameter[] extraParameters)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (actor is not null)
            await SetActorAsync(connection, transaction, actor.Value);
        await ExecuteAsync(connection, transaction, $"SET LOCAL ROLE {role};");
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("operation", prepared.OperationId);
        command.Parameters.AddWithValue("reservation", prepared.Source.AttemptKeyReservationId);
        command.Parameters.AddWithValue("preparation", prepared.PreparationId);
        command.Parameters.AddWithValue("fence", prepared.PreparationFence);
        command.Parameters.AddWithValue("token", prepared.ProviderOperationToken);
        command.Parameters.AddRange(extraParameters);
        var outcome = (string)(await command.ExecuteScalarAsync() ?? string.Empty);
        await transaction.CommitAsync();
        return outcome;
    }

    private async Task AssertR215ZeroObjectTerminationAsync(
        ReservedSourceFixture source,
        string expectedKeyDisposition,
        string expectedOutcome)
    {
        var before = await ReadR215AttemptSnapshotAsync(source.AttemptId);
        Assert.Equal(expectedKeyDisposition, before.KeyDisposition);
        Assert.Equal(source.AttemptId, before.KeyAttemptId);
        Assert.Equal(source.AttemptKeyReservationId, before.KeyReservationId);
        Assert.Equal(0, before.ObjectCount);
        Assert.Null(before.TerminationDisposition);
        Assert.Null(before.TerminatedAtUtc);

        await using (var db = postgres.CreateDbContext())
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(source.ActorPrincipalId, CancellationToken.None);
            Assert.Equal(
                expectedOutcome,
                await repository.TerminateAsync(
                    source.AttemptId,
                    source.Revision,
                    source.Fence,
                    "TerminatedBeforeStart",
                    CancellationToken.None));
        }

        var after = await ReadR215AttemptSnapshotAsync(source.AttemptId);
        Assert.Equal(before.StableAttemptJson, after.StableAttemptJson);
        Assert.Equal(before.KeyDisposition, after.KeyDisposition);
        Assert.Equal(before.KeyAttemptId, after.KeyAttemptId);
        Assert.Equal(before.KeyReservationId, after.KeyReservationId);
        Assert.Equal(before.ObjectCount, after.ObjectCount);
        if (expectedOutcome == "StateConflict")
        {
            Assert.Null(after.TerminationDisposition);
            Assert.Null(after.TerminatedAtUtc);
        }
        else
        {
            Assert.Equal("TerminatedBeforeStart", after.TerminationDisposition);
            Assert.NotNull(after.TerminatedAtUtc);
        }
    }

    private async Task<R215ObjectStateFixture> CreateR215ObjectStateAsync(
        string state,
        DurableObjectMinioFixture? minio)
    {
        if (state is "ObjectPresentPendingVerification" or "VerifiedCompleted" or "CleanupPending")
        {
            Assert.NotNull(minio);
            var written = await WriteObjectAsync(
                Encoding.ASCII.GetBytes($"r215-object-state-{state}"),
                minio!);
            var objectCustodyId = written.Written.ObjectCustodyId!.Value;

            if (state == "VerifiedCompleted")
            {
                var row = await ReadObjectRowAsync(objectCustodyId);
                var verified = await ExecuteR215ObjectMutationAsync(
                    written.Source.ActorPrincipalId,
                    "tagekyc_raw_export_reconciler",
                    "raw_export_mark_provisional_object_verified",
                    ("id", objectCustodyId),
                    ("revision", row.StateRevision),
                    ("evidence", SHA256.HashData("r215-verified-evidence"u8)));
                Assert.Equal("Verified", verified.OutcomeCode);
            }
            else if (state == "CleanupPending")
            {
                Assert.Equal(
                    RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup,
                    (await MarkAuthenticationFailureAsync(written, minio!)).Disposition);
            }

            Assert.Equal(state, (await ReadObjectRowAsync(objectCustodyId)).State);
            return new(written.Source, objectCustodyId);
        }

        var source = await SeedReservedSourceAsync(
            Encoding.ASCII.GetBytes($"r215-object-state-{state}"));
        await ProvisionKeyAsync(source);
        var begun = await BeginR215ObjectAsync(source);
        if (state == "Initiated")
            return new(source, begun.ObjectCustodyId);

        if (state == "NoObjectEstablished")
        {
            var evidence = C1HashCanonical.Compute(
                "tip-88c1-object-not-armed-evidence-v1",
                new C1HashCanonical.Scalar(
                    Convert.ToHexString(begun.ObjectBindingDigest).ToLowerInvariant()),
                new C1HashCanonical.Scalar(begun.StateRevision.ToString(CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar("NotArmed"));
            var notArmed = await ExecuteR215ObjectMutationAsync(
                source.ActorPrincipalId,
                "tagekyc_raw_export_custody_encryptor",
                "raw_export_record_provisional_object_not_armed",
                ("id", begun.ObjectCustodyId),
                ("revision", begun.StateRevision),
                ("evidence", evidence));
            Assert.Equal("NotArmed", notArmed.OutcomeCode);
            Assert.Equal(state, notArmed.ObjectState);
            return new(source, begun.ObjectCustodyId);
        }

        var operationId = Guid.NewGuid();
        var armed = await ExecuteR215ObjectMutationAsync(
            source.ActorPrincipalId,
            "tagekyc_raw_export_custody_encryptor",
            "raw_export_arm_provisional_object_put",
            ("id", begun.ObjectCustodyId),
            ("revision", begun.StateRevision),
            ("operation", operationId));
        Assert.Equal("Armed", armed.OutcomeCode);
        if (state == "PutInFlight")
            return new(source, begun.ObjectCustodyId);

        var unknown = await ExecuteR215ObjectMutationAsync(
            source.ActorPrincipalId,
            "tagekyc_raw_export_custody_encryptor",
            "raw_export_record_provisional_object_put_result",
            ("id", begun.ObjectCustodyId),
            ("revision", armed.StateRevision),
            ("operation", operationId),
            ("kind", "OutcomeUnknown"),
            ("status", DBNull.Value),
            ("length", DBNull.Value),
            ("digest", DBNull.Value),
            ("receipt", DBNull.Value));
        Assert.Equal("OutcomeUnknown", unknown.OutcomeCode);
        if (state == "PutOutcomeUnknown")
            return new(source, begun.ObjectCustodyId);

        if (state != "ObjectConflict")
            throw new InvalidOperationException($"R215_UNSUPPORTED_OBJECT_STATE:{state}");

        var firstObservedAtUtc = RoundR215ToMicroseconds(DateTimeOffset.UtcNow.AddSeconds(-1));
        var reconcileEvidence = C1HashCanonical.Compute(
            "tip-88c1-object-reconcile-observation-v1",
            new C1HashCanonical.Scalar(
                Convert.ToHexString(begun.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar(operationId.ToString("N")),
            new C1HashCanonical.Scalar("RecoveredMismatch"),
            new C1HashCanonical.Scalar(firstObservedAtUtc.UtcTicks.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar("none"),
            new C1HashCanonical.Scalar("none"),
            new C1HashCanonical.Scalar("none"));
        var conflict = await ExecuteR215ObjectMutationAsync(
            source.ActorPrincipalId,
            "tagekyc_raw_export_reconciler",
            "raw_export_resolve_provisional_object_put_outcome",
            ("id", begun.ObjectCustodyId),
            ("revision", unknown.StateRevision),
            ("kind", "RecoveredMismatch"),
            ("length", DBNull.Value),
            ("digest", DBNull.Value),
            ("first", firstObservedAtUtc),
            ("second", DBNull.Value),
            ("evidence", reconcileEvidence));
        Assert.Equal("ConditionalConflict", conflict.OutcomeCode);
        Assert.Equal(state, conflict.ObjectState);
        return new(source, begun.ObjectCustodyId);
    }

    private async Task<R215BegunObjectFixture> BeginR215ObjectAsync(ReservedSourceFixture source)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, source.ActorPrincipalId);
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE tagekyc_raw_export_custody_encryptor;");
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_begin_provisional_object_custody(@attempt,@revision,@fence,64)",
            connection,
            transaction);
        command.Parameters.AddWithValue("attempt", source.AttemptId);
        command.Parameters.AddWithValue("revision", source.Revision);
        command.Parameters.AddWithValue("fence", source.Fence);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("Created", reader.GetString(reader.GetOrdinal("OutcomeCode")));
        var result = new R215BegunObjectFixture(
            reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),
            reader.GetString(reader.GetOrdinal("ObjectState")),
            reader.GetInt64(reader.GetOrdinal("StateRevision")),
            (byte[])reader[reader.GetOrdinal("ObjectBindingDigest")]);
        Assert.Equal("Initiated", result.State);
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<ProvisionalObjectMutationResult> ExecuteR215ObjectMutationAsync(
        Guid actor,
        string role,
        string function,
        params (string Name, object Value)[] values)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await ExecuteAsync(connection, transaction, $"SET LOCAL ROLE {role};");
        await using var command = new NpgsqlCommand { Connection = connection, Transaction = transaction };
        foreach (var value in values)
            command.Parameters.AddWithValue(value.Name, value.Value);
        command.CommandText =
            $"SELECT * FROM tagekyc.{function}({string.Join(',', values.Select(value => '@' + value.Name))})";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var result = new ProvisionalObjectMutationResult(
            reader.GetString(reader.GetOrdinal("OutcomeCode")),
            reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),
            reader.GetString(reader.GetOrdinal("ObjectState")),
            reader.GetInt64(reader.GetOrdinal("StateRevision")));
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task AssertR215ObjectStateTerminationAsync(
        ReservedSourceFixture source,
        Guid objectCustodyId,
        string expectedState,
        string expectedOutcome,
        bool proveObjectRowLock = false)
    {
        var beforeAttempt = await ReadR215AttemptSnapshotAsync(source.AttemptId);
        var beforeObject = await ReadR215ObjectSnapshotAsync(source.AttemptId, objectCustodyId);
        Assert.Equal(source.Revision, beforeAttempt.AttemptRevision);
        Assert.Equal(source.Fence, beforeAttempt.AttemptFence);
        Assert.Equal(1, beforeAttempt.ObjectCount);
        Assert.Null(beforeAttempt.TerminationDisposition);
        Assert.Null(beforeAttempt.TerminatedAtUtc);
        Assert.Equal(1, beforeObject.ObjectCount);
        Assert.Equal(objectCustodyId, beforeObject.ObjectCustodyId);
        Assert.Equal(source.AttemptId, beforeObject.AttemptId);
        Assert.Equal(source.AttemptKeyReservationId, beforeObject.AttemptKeyReservationId);
        Assert.Equal(source.Revision, beforeObject.AttemptRevision);
        Assert.Equal(source.Fence, beforeObject.AttemptFence);
        Assert.Equal(expectedState, beforeObject.State);

        async Task<string> TerminateAsync()
        {
            await using var db = postgres.CreateDbContext();
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(source.ActorPrincipalId, CancellationToken.None);
            return await repository.TerminateAsync(
                source.AttemptId,
                source.Revision,
                source.Fence,
                "Terminated",
                CancellationToken.None);
        }

        string outcome;
        if (proveObjectRowLock)
        {
            await using var lockConnection = await OpenAsync();
            await using var lockTransaction = await lockConnection.BeginTransactionAsync();
            await using var lockCommand = new NpgsqlCommand(
                "SELECT 1 FROM tagekyc.raw_export_provisional_objects WHERE \"ObjectCustodyId\"=@id FOR UPDATE",
                lockConnection,
                lockTransaction);
            lockCommand.Parameters.AddWithValue("id", objectCustodyId);
            Assert.Equal(1, await lockCommand.ExecuteScalarAsync());
            var termination = Task.Run(TerminateAsync);
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            Assert.False(termination.IsCompleted);
            await lockTransaction.CommitAsync();
            outcome = await termination;
        }
        else
        {
            outcome = await TerminateAsync();
        }
        Assert.Equal(expectedOutcome, outcome);

        var afterAttempt = await ReadR215AttemptSnapshotAsync(source.AttemptId);
        var afterObject = await ReadR215ObjectSnapshotAsync(source.AttemptId, objectCustodyId);
        Assert.Equal(beforeAttempt.StableAttemptJson, afterAttempt.StableAttemptJson);
        Assert.Equal(beforeAttempt.AttemptRevision, afterAttempt.AttemptRevision);
        Assert.Equal(beforeAttempt.AttemptFence, afterAttempt.AttemptFence);
        Assert.Equal(beforeAttempt.KeyDisposition, afterAttempt.KeyDisposition);
        Assert.Equal(beforeAttempt.KeyAttemptId, afterAttempt.KeyAttemptId);
        Assert.Equal(beforeAttempt.KeyReservationId, afterAttempt.KeyReservationId);
        Assert.Equal(beforeAttempt.ObjectCount, afterAttempt.ObjectCount);
        Assert.Equal(beforeObject, afterObject);
        if (expectedOutcome == "StateConflict")
        {
            Assert.Null(afterAttempt.TerminationDisposition);
            Assert.Null(afterAttempt.TerminatedAtUtc);
        }
        else
        {
            Assert.Equal("Terminated", afterAttempt.TerminationDisposition);
            Assert.NotNull(afterAttempt.TerminatedAtUtc);
        }
    }

    private async Task<R215ObjectSnapshotFixture> ReadR215ObjectSnapshotAsync(
        Guid attemptId,
        Guid objectCustodyId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT pg_catalog.to_jsonb(po)::text,po."ObjectCustodyId",po."AttemptId",
                   po."AttemptKeyReservationId",po."EncryptionAttemptRevision",po."AttemptFence",po."State",
                   (SELECT pg_catalog.count(*) FROM tagekyc.raw_export_provisional_objects sibling
                    WHERE sibling."AttemptId"=@attempt)
            FROM tagekyc.raw_export_provisional_objects po
            WHERE po."AttemptId"=@attempt AND po."ObjectCustodyId"=@object
            """,
            connection);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("object", objectCustodyId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var snapshot = new R215ObjectSnapshotFixture(
            reader.GetString(0),
            reader.GetGuid(1),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetInt64(4),
            reader.GetInt64(5),
            reader.GetString(6),
            reader.GetInt64(7));
        Assert.False(await reader.ReadAsync());
        return snapshot;
    }

    private static DateTimeOffset RoundR215ToMicroseconds(DateTimeOffset value) =>
        new(value.UtcTicks - (value.UtcTicks % 10), TimeSpan.Zero);

    private async Task<R215AttemptSnapshotFixture> ReadR215AttemptSnapshotAsync(Guid attemptId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
              (pg_catalog.to_jsonb(a)-'R2TerminationDisposition'-'R2TerminatedAtUtc')::text,
              a."R2TerminationDisposition",a."R2TerminatedAtUtc",
              k."PreparationDisposition",k."AttemptId",k."AttemptKeyReservationId",
              a."EncryptionAttemptRevision",a."Fence",
              (SELECT pg_catalog.count(*) FROM tagekyc.raw_export_provisional_objects po
               WHERE po."AttemptId"=a."AttemptId")
            FROM tagekyc.raw_export_source_encryption_attempts a
            JOIN tagekyc.raw_export_attempt_key_reservations k
              ON k."AttemptKeyReservationId"=a."AttemptKeyReservationId"
             AND k."AttemptId"=a."AttemptId"
            WHERE a."AttemptId"=@attempt
            """,
            connection);
        command.Parameters.AddWithValue("attempt", attemptId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var snapshot = new R215AttemptSnapshotFixture(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            reader.GetString(3),
            reader.GetGuid(4),
            reader.GetGuid(5),
            reader.GetInt64(6),
            reader.GetInt64(7),
            reader.GetInt64(8));
        Assert.False(await reader.ReadAsync());
        return snapshot;
    }

    private async Task<LifecycleContextFixture> ReadLifecycleContextAsync(
        Guid actor,
        Guid objectCustodyId)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_provisional_object_lifecycle_context(@id)",
            connection,
            transaction);
        command.Parameters.AddWithValue("id", objectCustodyId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var result = new LifecycleContextFixture(
            reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),
            reader.GetString(reader.GetOrdinal("ObjectKey")),
            reader.GetString(reader.GetOrdinal("State")),
            reader.IsDBNull(reader.GetOrdinal("CleanupReasonCode"))
                ? null
                : reader.GetString(reader.GetOrdinal("CleanupReasonCode")));
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<ReconcileContextFixture> ReadReconcileContextAsync(
        Guid actor,
        Guid objectCustodyId)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_read_provisional_object_reconcile_context(@id)",
            connection,
            transaction);
        command.Parameters.AddWithValue("id", objectCustodyId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var result = new ReconcileContextFixture(
            reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),
            reader.GetString(reader.GetOrdinal("ObjectKey")),
            reader.GetString(reader.GetOrdinal("State")));
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<FunctionPostureFixture> ReadFunctionPostureAsync(string signature)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT p.oid::bigint,
                   pg_catalog.pg_get_function_identity_arguments(p.oid),
                   pg_catalog.pg_get_function_result(p.oid),
                   pg_catalog.pg_get_functiondef(p.oid),
                   owner.rolname,
                   COALESCE(p.proacl::text,''),
                   language.lanname,
                   p.prokind::text,p.prosecdef,p.provolatile::text,p.proisstrict,
                   p.proparallel::text,p.proleakproof,COALESCE(p.proconfig::text,''),
                   p.procost::double precision,p.prorows::double precision,
                   COALESCE(p.prosupport::pg_catalog.regprocedure::text,'-')
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_roles owner ON owner.oid=p.proowner
            JOIN pg_catalog.pg_language language ON language.oid=p.prolang
            WHERE p.oid=pg_catalog.to_regprocedure(@signature);
            """,
            connection);
        command.Parameters.AddWithValue("signature", signature);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var definition = reader.GetString(3);
        return new(
            reader.GetInt64(0), reader.GetString(1), reader.GetString(2), definition,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(definition))),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
            reader.GetBoolean(8), reader.GetString(9), reader.GetBoolean(10), reader.GetString(11),
            reader.GetBoolean(12), reader.GetString(13), reader.GetDouble(14), reader.GetDouble(15),
            reader.GetString(16));
    }

    private async Task<(string Name, string Type)[]> ReadProjectionColumnsAsync(string signature)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT p.proargnames[position],
                   pg_catalog.format_type(p.proallargtypes[position],NULL)
            FROM pg_catalog.pg_proc p
            CROSS JOIN LATERAL pg_catalog.generate_subscripts(p.proallargtypes,1) position
            WHERE p.oid=pg_catalog.to_regprocedure(@signature)
              AND p.proargmodes[position] IN ('o','t')
            ORDER BY position;
            """,
            connection);
        command.Parameters.AddWithValue("signature", signature);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<(string, string)>();
        while (await reader.ReadAsync())
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result.ToArray();
    }

    private async Task<(string Grantee, string Grantor)[]> ReadNonOwnerExecuteGrantsAsync(
        string signature)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COALESCE(grantee.rolname,'PUBLIC'),grantor.rolname
            FROM pg_catalog.pg_proc p
            CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) acl
            LEFT JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
            JOIN pg_catalog.pg_roles grantor ON grantor.oid=acl.grantor
            WHERE p.oid=pg_catalog.to_regprocedure(@signature)
              AND acl.privilege_type='EXECUTE'
              AND acl.grantee<>p.proowner
            ORDER BY 1,2;
            """,
            connection);
        command.Parameters.AddWithValue("signature", signature);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<(string, string)>();
        while (await reader.ReadAsync())
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result.ToArray();
    }

    private async Task AssertNoDirectCapabilityTablePrivilegesAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT role_name,table_name,privilege
            FROM (VALUES
              ('tagekyc_raw_export_custody_encryptor'),
              ('tagekyc_raw_export_reconciler'),
              ('tagekyc_runtime')) role(role_name)
            CROSS JOIN (VALUES
              ('raw_export_source_encryption_attempts'),
              ('raw_export_source_head'),
              ('raw_export_source_reservations'),
              ('raw_export_provisional_objects')) table_name(table_name)
            CROSS JOIN (VALUES ('SELECT'),('INSERT'),('UPDATE'),('DELETE')) operation(privilege)
            WHERE pg_catalog.has_table_privilege(
              role_name,
              pg_catalog.format('tagekyc.%I',table_name),
              privilege);
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
    }

    private async Task<string> ReadConstraintDefinitionAsync(string constraintName)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT pg_catalog.pg_get_constraintdef(c.oid,true)
            FROM pg_catalog.pg_constraint c
            JOIN pg_catalog.pg_class t ON t.oid=c.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
            WHERE n.nspname='tagekyc' AND t.relname='raw_export_source_encryption_attempts'
              AND c.conname=@name;
            """,
            connection);
        command.Parameters.AddWithValue("name", constraintName);
        return (string)(await command.ExecuteScalarAsync() ?? string.Empty);
    }

    private async Task<bool> ColumnExistsAsync(string columnName)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS(
              SELECT 1 FROM pg_catalog.pg_attribute a
              JOIN pg_catalog.pg_class t ON t.oid=a.attrelid
              JOIN pg_catalog.pg_namespace n ON n.oid=t.relnamespace
              WHERE n.nspname='tagekyc' AND t.relname='raw_export_source_encryption_attempts'
                AND a.attname=@name AND a.attnum>0 AND NOT a.attisdropped);
            """,
            connection);
        command.Parameters.AddWithValue("name", columnName);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private async Task<bool> FunctionExistsAsync(string signature)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.to_regprocedure(@signature) IS NOT NULL;",
            connection);
        command.Parameters.AddWithValue("signature", signature);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static void AssertSameFrozenPosture(
        FunctionPostureFixture expected,
        FunctionPostureFixture actual)
    {
        Assert.Equal(expected.Oid, actual.Oid);
        Assert.Equal(expected.IdentityArguments, actual.IdentityArguments);
        Assert.Equal(expected.Result, actual.Result);
        Assert.Equal(expected.Owner, actual.Owner);
        Assert.Equal(expected.Acl, actual.Acl);
        Assert.Equal(expected.Language, actual.Language);
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.SecurityDefiner, actual.SecurityDefiner);
        Assert.Equal(expected.Volatility, actual.Volatility);
        Assert.Equal(expected.Strict, actual.Strict);
        Assert.Equal(expected.Parallel, actual.Parallel);
        Assert.Equal(expected.Leakproof, actual.Leakproof);
        Assert.Equal(expected.Config, actual.Config);
        Assert.Equal(expected.Cost, actual.Cost);
        Assert.Equal(expected.Rows, actual.Rows);
        Assert.Equal(expected.Support, actual.Support);
    }

    private static int CountOccurrences(string value, string token) =>
        (value.Length - value.Replace(token, string.Empty, StringComparison.Ordinal).Length)
        / token.Length;

    private async Task<ReservedSourceFixture> SeedReservedSourceAsync(byte[] plaintext)
    {
        var candidate = await SeedCandidateAsync(plaintext);
        await using var provider = CreateBrokerProvider();
        await using var scope = provider.CreateAsyncScope();
        var broker = scope.ServiceProvider.GetRequiredService<IRawExportSourceClaimComparisonBroker>();
        var completed = await broker.CompleteNewCandidateAsync(
            candidate.Command,
            CancellationToken.None);
        Assert.Equal(RawExportSourceClaimComparisonOutcome.NewReservation, completed.Outcome);

        await using var db = postgres.CreateDbContext();
        var sourceArtifactId = completed.SourceArtifactId!.Value;
        var reservation = await db.RawExportSourceReservations.SingleAsync(
            row => row.SourceArtifactId == sourceArtifactId);
        var attempt = await db.RawExportSourceEncryptionAttempts.SingleAsync(
            row => row.SourceArtifactId == sourceArtifactId);
        return new(
            candidate.ActorPrincipalId,
            sourceArtifactId,
            attempt.AttemptId,
            attempt.AttemptKeyReservationId,
            attempt.EncryptionAttemptRevision,
            attempt.Fence,
            reservation.ContentCommitment);
    }

    private async Task<CandidateFixture> SeedCandidateAsync(byte[] plaintext)
    {
        var actor = Guid.NewGuid();
        var client = Guid.NewGuid();
        var session = Guid.NewGuid();
        var artifact = Guid.NewGuid();
        var policy = await SeedConsentPolicyAsync();
        var now = DateTimeOffset.UtcNow;
        await using (var db = postgres.CreateDbContext())
        {
            db.Sessions.Add(new VerificationSessionRow
            {
                Id = session,
                ClientApplicationId = client,
                SubjectRef = $"subject:{Guid.NewGuid():N}",
                Profile = "ChallengeBoundEkycProfile",
                Purpose = "raw-export",
                RequiredChecksJson = "[\"LiveSelfie\"]",
                BindingNonceHash = ChallengeHash,
                RequestId = Guid.NewGuid().ToString("N"),
                CorrelationId = Guid.NewGuid().ToString("N"),
                State = "Completed",
                Result = "Passed",
                AssuranceLevel = "Substantial",
                PolicySnapshotId = "c1b2-r2-policy",
                RetentionClass = "Standard",
                DeletionEligibility = "Pending",
                LegalHoldStatus = "None",
                PurgeBlockReason = "None",
                ExpiresAt = now.AddHours(2),
                CreatedAt = now,
                CompletedAt = now,
            });
            db.CaptureArtifacts.Add(new CaptureArtifactRow
            {
                Id = artifact,
                VerificationSessionId = session,
                ArtifactType = "SelfieImage",
                CaptureSource = "MobileSdk",
                ArtifactHash = $"sha256:{new string('a', 64)}",
                MetadataHash = $"sha256:{new string('b', 64)}",
                QualityState = "Accepted",
                RequestId = Guid.NewGuid().ToString("N"),
                CorrelationId = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
                ExpiresAt = now.AddHours(2),
            });
            await db.SaveChangesAsync();
        }

        var acceptance = await AppendAcceptanceAsync(actor, session, client, artifact);
        await GrantConsentAsync(actor, client, session, policy);
        var authority = await AppendAuthorityAsync(actor, client, session, acceptance, artifact, policy);
        var captured = now.AddSeconds(-5);
        var retentionStart = now.AddSeconds(-4);
        var retentionExpires = now.AddMinutes(30);
        var ingressKey = Guid.NewGuid();
        var owner = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var begin = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.begin_raw_export_source_ingress_claim(
              @actor,@client,@producer,@agent,@ingress,@session,@acceptance,@artifact,1,
              'LiveSelfieImage',@challenge,@authority,@length,'image/jpeg',@captured,
              @retentionStart,@retentionExpires,1800,'fixture-content-commitment',1,@owner,300,100);
            """,
            connection,
            transaction);
        begin.Parameters.AddWithValue("actor", actor);
        begin.Parameters.AddWithValue("client", client);
        begin.Parameters.AddWithValue("producer", actor.ToString("N"));
        begin.Parameters.AddWithValue("agent", "capture-agent-r2");
        begin.Parameters.AddWithValue("ingress", ingressKey.ToString("N"));
        begin.Parameters.AddWithValue("session", session);
        begin.Parameters.AddWithValue("acceptance", acceptance);
        begin.Parameters.AddWithValue("artifact", artifact);
        begin.Parameters.AddWithValue("challenge", ChallengeHash);
        begin.Parameters.AddWithValue("authority", authority.ToString("D"));
        begin.Parameters.AddWithValue("length", (long)plaintext.Length);
        begin.Parameters.AddWithValue("captured", captured);
        begin.Parameters.AddWithValue("retentionStart", retentionStart);
        begin.Parameters.AddWithValue("retentionExpires", retentionExpires);
        begin.Parameters.AddWithValue("owner", owner);
        await using var reader = await begin.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var token = new RawExportClaimEvaluationToken(
            reader.GetGuid(4), reader.GetInt64(5), reader.GetInt64(6),
            reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3), reader.GetString(1));
        await reader.CloseAsync();
        await transaction.CommitAsync();

        return new(
            actor,
            new RawExportSourceClaimComparisonCommand(
                actor, client, actor.ToString("N"), "capture-agent-r2", ingressKey,
                token, plaintext.Length, "image/jpeg", captured, retentionStart,
                retentionExpires, 1800, SHA256.HashData(plaintext)));
    }

    private async Task<Guid> SeedConsentPolicyAsync()
    {
        var policyId = Guid.NewGuid();
        await using var db = postgres.CreateDbContext();
        var policy = await new EfRawExportPolicyRepository(db).AddVersionAsync(
            new AddRawExportPolicyVersionCommand(
                policyId, 0, RawExportMode.EncryptedRawVaultRetained,
                "SubjectRawBiometricExport", "fixture-c1-retained-v1",
                "SubjectRawBiometricExport", RawExportConsentRequirement.Required,
                null, null, "Controller", "controller:fixture", "VN", "VN", "VN",
                null, null,
                new HashSet<RawExportRawClass>
                {
                    RawExportRawClass.ChipDg2Portrait,
                    RawExportRawClass.LiveSelfieImage,
                },
                300));
        Assert.Equal(1, policy.RequirementRuleSetVersion);
        return policyId;
    }

    private async Task<Guid> AppendAcceptanceAsync(
        Guid actor,
        Guid session,
        Guid client,
        Guid artifact)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_append_capture_acceptance(
              @session,@client,'LiveSelfieImage',@artifact,1,@challenge,
              'evidence:c1b2-r2','acceptance-policy:c1b2-r2',1);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("session", session);
        command.Parameters.AddWithValue("client", client);
        command.Parameters.AddWithValue("artifact", artifact);
        command.Parameters.AddWithValue("challenge", ChallengeHash);
        var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
        await transaction.CommitAsync();
        return id;
    }

    private async Task GrantConsentAsync(
        Guid actor,
        Guid client,
        Guid session,
        Guid policy)
    {
        var admin = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction,
            "SELECT tagekyc.raw_export_bootstrap_global_authority(@admin,'RecorderAuthorityAdmin','decision:c1b2-r2-bootstrap');",
            new NpgsqlParameter("admin", admin));
        await SetActorAsync(connection, transaction, admin);
        await ExecuteAsync(connection, transaction,
            """
            SELECT tagekyc.raw_export_append_subject_consent_authority(
              @actor,@client,'SubjectConsentRecorder',0,'Granted',NULL,
              'decision:c1b2-r2',@validUntil);
            """,
            new NpgsqlParameter("actor", actor),
            new NpgsqlParameter("client", client),
            new NpgsqlParameter("validUntil", DateTimeOffset.UtcNow.AddHours(1)));
        await SetActorAsync(connection, transaction, actor);
        await ExecuteAsync(connection, transaction,
            """
            SELECT tagekyc.raw_export_append_subject_consent_granted(
              @session,@policy,1,ARRAY['LiveSelfieImage']::text[],
              'consent-text-v1','sha256:consent-text','artifact:consent-c1b2-r2',
              'decision:c1b2-r2',@validUntil);
            """,
            new NpgsqlParameter("session", session),
            new NpgsqlParameter("policy", policy),
            new NpgsqlParameter("validUntil", DateTimeOffset.UtcNow.AddHours(1)));
        await transaction.CommitAsync();
    }

    private async Task<Guid> AppendAuthorityAsync(
        Guid actor,
        Guid client,
        Guid session,
        Guid acceptance,
        Guid artifact,
        Guid policy)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand(
            """
            SELECT "AuthoritySnapshotId"
            FROM tagekyc.raw_export_append_authority_snapshot(
              @client,@session,@acceptance,'LiveSelfieImage',@artifact,1,
              'controller:fixture','scope:fixture','retention-policy:fixture',1,
              @policy,1,'RawBiometric','CaptureAccepted',@sourceExpires,
              'revocation-policy:fixture','purge-policy:fixture',
              'legal-hold-policy:fixture',@evaluated,@validUntil);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("client", client);
        command.Parameters.AddWithValue("session", session);
        command.Parameters.AddWithValue("acceptance", acceptance);
        command.Parameters.AddWithValue("artifact", artifact);
        command.Parameters.AddWithValue("policy", policy);
        command.Parameters.AddWithValue("sourceExpires", DateTimeOffset.UtcNow.AddHours(1));
        command.Parameters.AddWithValue("evaluated", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("validUntil", DateTimeOffset.UtcNow.AddMinutes(45));
        var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
        await transaction.CommitAsync();
        return id;
    }

    private ServiceProvider CreateBrokerProvider()
    {
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef",
            ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
                "abcdef0123456789abcdef0123456789",
            ["TagEkyc:RawExport:CustodyProfile:Profile"] = "Fixture",
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "30",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300",
        };
        var services = new ServiceCollection();
        services.AddScoped(_ => postgres.CreateDbContext());
        services.AddTagEkycRawExportSourceClaimComparison(configuration);
        return services.BuildServiceProvider();
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actor) =>
        ExecuteAsync(
            connection,
            transaction,
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true);",
            new NpgsqlParameter("actor", actor.ToString("D")));

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record CandidateFixture(
        Guid ActorPrincipalId,
        RawExportSourceClaimComparisonCommand Command);

    private sealed record ReservedSourceFixture(
        Guid ActorPrincipalId,
        Guid SourceArtifactId,
        Guid AttemptId,
        Guid AttemptKeyReservationId,
        long Revision,
        long Fence,
        byte[] ContentCommitment);

    private sealed record R215PreparedKeyFixture(
        ReservedSourceFixture Source,
        Guid OperationId,
        Guid PreparationId,
        long PreparationFence,
        string ProviderOperationToken);

    private sealed record R215AttemptSnapshotFixture(
        string StableAttemptJson,
        string? TerminationDisposition,
        DateTimeOffset? TerminatedAtUtc,
        string KeyDisposition,
        Guid KeyAttemptId,
        Guid KeyReservationId,
        long AttemptRevision,
        long AttemptFence,
        long ObjectCount);

    private sealed record R215ObjectStateFixture(
        ReservedSourceFixture Source,
        Guid ObjectCustodyId);

    private sealed record R215BegunObjectFixture(
        Guid ObjectCustodyId,
        string State,
        long StateRevision,
        byte[] ObjectBindingDigest);

    private sealed record R215ObjectSnapshotFixture(
        string ObjectJson,
        Guid ObjectCustodyId,
        Guid AttemptId,
        Guid AttemptKeyReservationId,
        long AttemptRevision,
        long AttemptFence,
        string State,
        long ObjectCount);

    private sealed record WrittenSourceFixture(
        ReservedSourceFixture Source,
        RawExportR2WriterResult Written);

    private sealed record LifecycleContextFixture(
        Guid ObjectCustodyId,
        string ObjectKey,
        string State,
        string? CleanupReasonCode);

    private sealed record ReconcileContextFixture(
        Guid ObjectCustodyId,
        string ObjectKey,
        string State);

    private sealed record FunctionPostureFixture(
        long Oid,
        string IdentityArguments,
        string Result,
        string Definition,
        string DefinitionSha256,
        string Owner,
        string Acl,
        string Language,
        string Kind,
        bool SecurityDefiner,
        string Volatility,
        bool Strict,
        string Parallel,
        bool Leakproof,
        string Config,
        double Cost,
        double Rows,
        string Support);

    private enum R211ByteMutation
    {
        DataType,
        DataOrdinal,
        TrailingByte,
        TruncatedFrame,
        ReportedLength,
        FinalType,
    }

    private sealed record R211MutationEvidence(
        byte[] CanonicalSha256,
        byte[] MutatedSha256,
        int CanonicalLength,
        int MutatedLength,
        int Offset,
        byte Before,
        byte After,
        string Predicate);

    private sealed record R211DependencyManifest(
        string TargetPredicate,
        string TargetField,
        byte[] CanonicalObjectSha256,
        byte[] CraftedObjectSha256,
        int CanonicalLength,
        int CraftedLength,
        int TargetOffset,
        byte TargetBefore,
        byte TargetAfter,
        int FinalOffset,
        string[] ChangedSerializedRanges,
        string[] ChangedSemanticFields,
        string[] RecomputedDependentFields,
        string[] ResealedFrames,
        string[] FixtureContextFieldsChanged);

    private async Task<RawExportR2VerifierResult> VerifyR211Async(
        WrittenSourceFixture fixture,
        IProvisionalObjectReconciler? reconciler,
        IAttemptAeadVerificationOperation? verification,
        IContentCommitmentService? commitment)
    {
        await using var db = postgres.CreateDbContext();
        await using var lookupDb = postgres.CreateDbContext();
        await using var provider = CreateBrokerProvider();
        await using var scope = provider.CreateAsyncScope();
        var kek = new FixtureDurableKekOperationProvider(
            new PostgresFixtureKekJournal(db),
            new PostgresFixtureKekJournal(lookupDb));
        var verifier = new RawExportR2CompletionVerifier(
            new RawExportR2Repository(db),
            reconciler ?? throw new ArgumentNullException(nameof(reconciler)),
            verification ?? new AttemptAeadVerificationOperationService(
                db, kek, DurableKeyCustodyOptions.Resolve(new ConfigurationManager())),
            commitment ?? scope.ServiceProvider.GetRequiredService<IContentCommitmentService>());
        return await verifier.ExecuteAsync(
            new(
                fixture.Source.ActorPrincipalId,
                fixture.Source.AttemptId,
                fixture.Source.Revision,
                fixture.Source.Fence,
                fixture.Written.ObjectCustodyId!.Value),
            CancellationToken.None);
    }

    private static int R211DataChunkCount(int plaintextLength) =>
        checked((plaintextLength + RawExportR2FrameCodec.FixtureChunkSize - 1)
            / RawExportR2FrameCodec.FixtureChunkSize);

    private static void AssertR211MutationEvidence(
        R211MutationEvidence evidence,
        bool metadataOnly)
    {
        Assert.Equal(32, evidence.CanonicalSha256.Length);
        Assert.Equal(32, evidence.MutatedSha256.Length);
        Assert.InRange(evidence.Offset, 0, evidence.MutatedLength);
        if (metadataOnly)
        {
            Assert.Equal(evidence.CanonicalSha256, evidence.MutatedSha256);
            Assert.Equal(evidence.CanonicalLength, evidence.MutatedLength);
        }
        else
        {
            Assert.NotEqual(evidence.CanonicalSha256, evidence.MutatedSha256);
            Assert.True(evidence.Before != evidence.After || evidence.CanonicalLength != evidence.MutatedLength);
        }
        Assert.False(string.IsNullOrWhiteSpace(evidence.Predicate));
    }

    private static void AssertR211DependencyManifest(
        R211DependencyManifest manifest,
        R211ByteMutation mutation)
    {
        Assert.Equal(mutation.ToString(), manifest.TargetPredicate);
        Assert.Equal(32, manifest.CanonicalObjectSha256.Length);
        Assert.Equal(32, manifest.CraftedObjectSha256.Length);
        Assert.NotEqual(manifest.CanonicalObjectSha256, manifest.CraftedObjectSha256);
        Assert.Equal(manifest.CanonicalLength, manifest.CraftedLength);
        Assert.NotEqual(manifest.TargetBefore, manifest.TargetAfter);
        Assert.Equal(2, manifest.ChangedSerializedRanges.Length);
        Assert.Equal(new[] { "DataCiphertextDigest" }, manifest.ChangedSemanticFields);
        Assert.Equal(
            new[] { "DataCiphertextDigest", "FINAL ciphertext/tag", "whole-object digest fixture metadata" },
            manifest.RecomputedDependentFields);
        Assert.Equal(new[] { "FINAL" }, manifest.ResealedFrames);
        Assert.Equal(new[] { "CiphertextDigest" }, manifest.FixtureContextFieldsChanged);
        Assert.True(manifest.TargetOffset < manifest.FinalOffset);
    }

    private static void AssertR211Pending(RawExportProvisionalObjectRow row)
    {
        Assert.Equal("ObjectPresentPendingVerification", row.State);
        Assert.Null(row.CleanupReasonCode);
        Assert.Null(row.CleanupEvidenceDigest);
        Assert.Null(row.CleanupRequestedAtUtc);
        Assert.Null(row.DeletionEvidenceDigest);
        Assert.Null(row.DeletionEvidenceKind);
        Assert.Null(row.DeletedAtUtc);
        Assert.Null(row.QuarantineReasonCode);
        Assert.Null(row.QuarantineEvidenceDigest);
        Assert.Null(row.QuarantinedAtUtc);
    }

    private static void AssertR211Cleanup(RawExportProvisionalObjectRow row)
    {
        Assert.Equal("CleanupPending", row.State);
        Assert.Equal("VerificationFailed", row.CleanupReasonCode);
        Assert.NotNull(row.CleanupEvidenceDigest);
        Assert.Equal(32, row.CleanupEvidenceDigest!.Length);
        Assert.NotNull(row.CleanupRequestedAtUtc);
        Assert.Null(row.DeletionEvidenceDigest);
        Assert.Null(row.QuarantineEvidenceDigest);
    }

    private async Task<byte[]> ReadR211ObjectBytesAsync(
        WrittenSourceFixture fixture,
        DurableObjectMinioFixture minio)
    {
        var reconciler = new S3CompatibleProvisionalObjectReconciler(
            minio.Options(ProvisionalObjectCapability.Reconciler));
        await using var db = postgres.CreateDbContext();
        var repository = new RawExportR2Repository(db);
        await repository.SetActorAsync(fixture.Source.ActorPrincipalId, CancellationToken.None);
        var context = await repository.ReadObjectContextAsync(
            fixture.Written.ObjectCustodyId!.Value, CancellationToken.None);
        Assert.NotNull(context);
        await using var read = await reconciler.OpenExactReadAsync(
            new(context!.ProvisionalObjectIdentity, context.ObjectKey, context.ObjectBindingDigest),
            CancellationToken.None);
        using var buffer = new MemoryStream();
        await read.Ciphertext.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private async Task<ExactObjectLocator> ReadR211LocatorAsync(WrittenSourceFixture fixture)
    {
        await using var db = postgres.CreateDbContext();
        var repository = new RawExportR2Repository(db);
        await repository.SetActorAsync(fixture.Source.ActorPrincipalId, CancellationToken.None);
        var context = await repository.ReadObjectContextAsync(
            fixture.Written.ObjectCustodyId!.Value, CancellationToken.None);
        Assert.NotNull(context);
        return new(context!.ProvisionalObjectIdentity, context.ObjectKey, context.ObjectBindingDigest);
    }

    private async Task MutateR211ExpectedCiphertextDigestAsync(
        Guid objectCustodyId,
        byte[] ciphertextDigest)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SET session_replication_role=replica; " +
            "UPDATE tagekyc.raw_export_provisional_objects " +
            "SET \"CiphertextDigest\"=@digest " +
            "WHERE \"ObjectCustodyId\"=@id; " +
            "SET session_replication_role=origin;",
            connection);
        command.Parameters.AddWithValue("id", objectCustodyId);
        command.Parameters.AddWithValue("digest", ciphertextDigest);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private async Task RenameR211FunctionAsync(string identity, string newName)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            $"ALTER FUNCTION tagekyc.{identity} RENAME TO {newName}", connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class R211RecordingReconciler(IProvisionalObjectReconciler inner)
        : IProvisionalObjectReconciler
    {
        internal byte[]? CanonicalBytes { get; private set; }
        internal long ReportedLength { get; private set; }

        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        public async Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken)
        {
            await using var exact = await inner.OpenExactReadAsync(locator, cancellationToken);
            using var buffer = new MemoryStream();
            await exact.Ciphertext.CopyToAsync(buffer, cancellationToken);
            CanonicalBytes = buffer.ToArray();
            ReportedLength = exact.CiphertextLength;
            return new(new MemoryStream(CanonicalBytes, writable: false), ReportedLength);
        }
    }

    private sealed class R211ByteMutatingReconciler(
        IProvisionalObjectReconciler inner,
        R211ByteMutation mutation,
        ReservedSourceFixture source) : IProvisionalObjectReconciler
    {
        private byte[]? preparedBytes;
        private long preparedReportedLength;
        internal int OpenCount { get; private set; }
        internal R211MutationEvidence? Evidence { get; private set; }

        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        internal async Task PrepareAsync(ExactObjectLocator locator)
        {
            await using var exact = await inner.OpenExactReadAsync(locator, CancellationToken.None);
            using var buffer = new MemoryStream();
            await exact.Ciphertext.CopyToAsync(buffer);
            var canonical = buffer.ToArray();
            var mutated = canonical.ToArray();
            var headerLength = R211HeaderLength(canonical);
            var finalOffset = checked(canonical.Length - RawExportR2FrameCodec.FinalFrameLength);
            var offset = mutation switch
            {
                R211ByteMutation.DataType => headerLength,
                R211ByteMutation.DataOrdinal => headerLength + 4,
                R211ByteMutation.TrailingByte => canonical.Length,
                R211ByteMutation.TruncatedFrame => headerLength + 22,
                R211ByteMutation.ReportedLength => canonical.Length,
                R211ByteMutation.FinalType => finalOffset,
                _ => throw new ArgumentOutOfRangeException(),
            };
            var before = offset < canonical.Length ? canonical[offset] : (byte)0;
            var after = (byte)(before ^ 0x5a);
            long reported = exact.CiphertextLength;
            switch (mutation)
            {
                case R211ByteMutation.TrailingByte:
                    mutated = canonical.Concat(new[] { after }).ToArray();
                    break;
                case R211ByteMutation.TruncatedFrame:
                    mutated = canonical[..offset];
                    break;
                case R211ByteMutation.ReportedLength:
                    reported--;
                    mutated = canonical;
                    before = after = 0;
                    break;
                default:
                    mutated[offset] = after;
                    break;
            }
            Evidence = new(
                SHA256.HashData(canonical), SHA256.HashData(mutated),
                canonical.Length, mutated.Length, offset, before, after, mutation.ToString());
            Assert.Equal(source.AttemptId, source.AttemptId);
            preparedBytes = mutated;
            preparedReportedLength = reported;
        }

        public Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken)
        {
            OpenCount++;
            Assert.NotNull(preparedBytes);
            return Task.FromResult(new ExactObjectRead(
                new MemoryStream(preparedBytes!, writable: false),
                preparedReportedLength));
        }
    }

    private static int R211HeaderLength(byte[] bytes)
    {
        const int suiteLengthOffset = 143;
        var suiteLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(suiteLengthOffset, 4));
        var nonceLengthOffset = checked(suiteLengthOffset + 4 + (int)suiteLength + 2 + 4);
        var nonceLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(nonceLengthOffset, 4));
        return checked(189 + (int)suiteLength + (int)nonceLength);
    }

    private sealed class R211ReencryptedCompletionReconciler : IProvisionalObjectReconciler
    {
        private readonly IProvisionalObjectReconciler inner;
        private readonly IAttemptAeadVerificationOperation verification;
        private readonly IAttemptAeadEncryptionOperation encryption;
        private readonly RawExportR2VerificationContext context;
        private readonly int completionOffset;
        private byte[]? preparedBytes;
        private long preparedReportedLength;

        private R211ReencryptedCompletionReconciler(
            IProvisionalObjectReconciler inner,
            IAttemptAeadVerificationOperation verification,
            IAttemptAeadEncryptionOperation encryption,
            RawExportR2VerificationContext context,
            int completionOffset)
        {
            this.inner = inner;
            this.verification = verification;
            this.encryption = encryption;
            this.context = context;
            this.completionOffset = completionOffset;
        }

        internal R211MutationEvidence? Evidence { get; private set; }

        internal static async Task<R211ReencryptedCompletionReconciler> CreateAsync(
            IProvisionalObjectReconciler inner,
            RawExportR2Repository repository,
            IAttemptAeadVerificationOperation verification,
            IAttemptAeadEncryptionOperation encryption,
            WrittenSourceFixture fixture,
            int completionOffset)
        {
            await repository.SetActorAsync(fixture.Source.ActorPrincipalId, CancellationToken.None);
            var context = await repository.ReadVerificationContextAsync(
                fixture.Source.AttemptId,
                fixture.Source.Revision,
                fixture.Source.Fence,
                CancellationToken.None);
            Assert.NotNull(context);
            var objectContext = await repository.ReadObjectContextAsync(
                fixture.Written.ObjectCustodyId!.Value,
                CancellationToken.None);
            Assert.NotNull(objectContext);
            var result = new R211ReencryptedCompletionReconciler(
                inner, verification, encryption, context!, completionOffset);
            await result.PrepareAsync(
                new(
                    objectContext!.ProvisionalObjectIdentity,
                    objectContext.ObjectKey,
                    objectContext.ObjectBindingDigest),
                CancellationToken.None);
            return result;
        }

        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        private async Task PrepareAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken)
        {
            await using var exact = await inner.OpenExactReadAsync(locator, cancellationToken);
            using var buffer = new MemoryStream();
            await exact.Ciphertext.CopyToAsync(buffer, cancellationToken);
            var canonical = buffer.ToArray();
            var mutated = canonical.ToArray();
            var header = new RawExportR2Header(
                context.AttemptId,
                context.AttemptKeyReservationId,
                context.SourceArtifactId,
                context.ProvisionalObjectIdentity,
                context.EncryptionAttemptFingerprint,
                locator.ObjectBindingDigest,
                context.EncryptionSuiteId,
                context.EncryptionFramingVersion,
                context.ChunkSize,
                context.NonceStrategyId,
                context.FramingParametersDigest);
            var headerBytes = RawExportR2FrameCodec.SerializeHeader(header);
            var finalOffset = checked(mutated.Length - RawExportR2FrameCodec.FinalFrameLength);
            Assert.Equal(RawExportR2FrameCodec.FinalFrameType, mutated[finalOffset]);
            var finalAad = RawExportR2FrameCodec.CreateFinalAad(
                context.EncryptionAttemptFingerprint,
                SHA256.HashData(headerBytes),
                checked((uint)R211DataChunkCount(checked((int)context.ClaimedPlaintextLength))),
                checked((ulong)context.ClaimedPlaintextLength));
            var nonce = mutated.AsMemory(finalOffset + 1, 12);
            var decrypted = await verification.DecryptAndVerifyBoundedChunkAsync(
                new(
                    context.AttemptKeyReservationId,
                    mutated.AsMemory(finalOffset + 17, RawExportR2FrameCodec.CompletionPlaintextLength),
                    nonce,
                    finalAad),
                mutated.AsMemory(
                    finalOffset + 17 + RawExportR2FrameCodec.CompletionPlaintextLength,
                    16),
                cancellationToken);
            Assert.Equal(AttemptAeadVerificationOutcome.Verified, decrypted.Outcome);
            var completion = Assert.IsType<byte[]>(decrypted.Output).ToArray();
            var before = completion[completionOffset];
            var after = (byte)(before ^ 0x01);
            completion[completionOffset] = after;
            var encrypted = await encryption.EncryptBoundedChunkAsync(
                new(context.AttemptKeyReservationId, completion, nonce, finalAad),
                cancellationToken);
            encrypted.Output.CopyTo(mutated, finalOffset + 17);
            encrypted.AuthenticationTag.CopyTo(
                mutated,
                finalOffset + 17 + RawExportR2FrameCodec.CompletionPlaintextLength);
            Evidence = new(
                SHA256.HashData(canonical), SHA256.HashData(mutated),
                canonical.Length, mutated.Length,
                finalOffset + 17 + completionOffset,
                before, after,
                completionOffset == 116 ? "EnvelopeMetadataBinding" : "CompletionRecordBinding");
            CryptographicOperations.ZeroMemory(completion);
            preparedBytes = mutated;
            preparedReportedLength = exact.CiphertextLength;
        }

        public Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken)
        {
            Assert.NotNull(preparedBytes);
            return Task.FromResult(new ExactObjectRead(
                new MemoryStream(preparedBytes!, writable: false),
                preparedReportedLength));
        }
    }

    private sealed class R211DependencyRepairedDataReconciler : IProvisionalObjectReconciler
    {
        private readonly IProvisionalObjectReconciler inner;
        private readonly byte[] craftedBytes;
        private readonly long reportedLength;

        private R211DependencyRepairedDataReconciler(
            IProvisionalObjectReconciler inner,
            byte[] craftedBytes,
            long reportedLength,
            R211DependencyManifest manifest)
        {
            this.inner = inner;
            this.craftedBytes = craftedBytes;
            this.reportedLength = reportedLength;
            Manifest = manifest;
        }

        internal R211DependencyManifest Manifest { get; }

        internal static async Task<R211DependencyRepairedDataReconciler> CreateAsync(
            IProvisionalObjectReconciler inner,
            RawExportR2Repository repository,
            IAttemptAeadVerificationOperation verification,
            IAttemptAeadEncryptionOperation encryption,
            WrittenSourceFixture fixture,
            R211ByteMutation mutation)
        {
            Assert.Contains(mutation, new[] { R211ByteMutation.DataType, R211ByteMutation.DataOrdinal });
            await repository.SetActorAsync(fixture.Source.ActorPrincipalId, CancellationToken.None);
            var context = await repository.ReadVerificationContextAsync(
                fixture.Source.AttemptId, fixture.Source.Revision, fixture.Source.Fence,
                CancellationToken.None);
            var objectContext = await repository.ReadObjectContextAsync(
                fixture.Written.ObjectCustodyId!.Value, CancellationToken.None);
            Assert.NotNull(context);
            Assert.NotNull(objectContext);
            var locator = new ExactObjectLocator(
                objectContext!.ProvisionalObjectIdentity,
                objectContext.ObjectKey,
                objectContext.ObjectBindingDigest);
            await using var exact = await inner.OpenExactReadAsync(locator, CancellationToken.None);
            using var buffer = new MemoryStream();
            await exact.Ciphertext.CopyToAsync(buffer);
            var canonical = buffer.ToArray();
            var crafted = canonical.ToArray();
            var headerLength = R211HeaderLength(canonical);
            var finalOffset = checked(canonical.Length - RawExportR2FrameCodec.FinalFrameLength);
            var targetOffset = mutation == R211ByteMutation.DataType
                ? headerLength
                : headerLength + 4;
            var before = crafted[targetOffset];
            var after = (byte)(before ^ 0x5a);
            crafted[targetOffset] = after;

            var dataDigest = SHA256.HashData(crafted.AsSpan(headerLength, finalOffset - headerLength));
            var header = new RawExportR2Header(
                context!.AttemptId, context.AttemptKeyReservationId, context.SourceArtifactId,
                context.ProvisionalObjectIdentity, context.EncryptionAttemptFingerprint,
                objectContext.ObjectBindingDigest, context.EncryptionSuiteId,
                context.EncryptionFramingVersion, context.ChunkSize, context.NonceStrategyId,
                context.FramingParametersDigest);
            var headerBytes = RawExportR2FrameCodec.SerializeHeader(header);
            var headerDigest = SHA256.HashData(headerBytes);
            var chunkCount = checked((uint)R211DataChunkCount(checked((int)context.ClaimedPlaintextLength)));
            var finalAad = RawExportR2FrameCodec.CreateFinalAad(
                context.EncryptionAttemptFingerprint,
                headerDigest,
                chunkCount,
                checked((ulong)context.ClaimedPlaintextLength));
            var nonce = canonical.AsMemory(finalOffset + 1, 12);
            var decrypted = await verification.DecryptAndVerifyBoundedChunkAsync(
                new(
                    context.AttemptKeyReservationId,
                    canonical.AsMemory(finalOffset + 17, RawExportR2FrameCodec.CompletionPlaintextLength),
                    nonce,
                    finalAad),
                canonical.AsMemory(finalOffset + 17 + RawExportR2FrameCodec.CompletionPlaintextLength, 16),
                CancellationToken.None);
            Assert.Equal(AttemptAeadVerificationOutcome.Verified, decrypted.Outcome);
            var canonicalCompletionBytes = Assert.IsType<byte[]>(decrypted.Output);
            var canonicalCompletion = RawExportR2FrameCodec.ParseCompletion(canonicalCompletionBytes);
            var repairedCompletion = canonicalCompletion with { DataCiphertextDigest = dataDigest };
            var repairedCompletionBytes = RawExportR2FrameCodec.SerializeCompletion(repairedCompletion);
            var sealedFinal = await encryption.EncryptBoundedChunkAsync(
                new(context.AttemptKeyReservationId, repairedCompletionBytes, nonce, finalAad),
                CancellationToken.None);
            sealedFinal.Output.CopyTo(crafted, finalOffset + 17);
            sealedFinal.AuthenticationTag.CopyTo(
                crafted, finalOffset + 17 + RawExportR2FrameCodec.CompletionPlaintextLength);

            var roundTrip = await verification.DecryptAndVerifyBoundedChunkAsync(
                new(context.AttemptKeyReservationId, sealedFinal.Output, nonce, finalAad),
                sealedFinal.AuthenticationTag,
                CancellationToken.None);
            Assert.Equal(AttemptAeadVerificationOutcome.Verified, roundTrip.Outcome);
            var roundTripCompletion = RawExportR2FrameCodec.ParseCompletion(
                Assert.IsType<byte[]>(roundTrip.Output));
            Assert.Equal(canonicalCompletion.ChunkCount, roundTripCompletion.ChunkCount);
            Assert.Equal(canonicalCompletion.TotalDataPlaintextLength, roundTripCompletion.TotalDataPlaintextLength);
            Assert.Equal(canonicalCompletion.DataCiphertextLength, roundTripCompletion.DataCiphertextLength);
            Assert.Equal(dataDigest, roundTripCompletion.DataCiphertextDigest);
            Assert.Equal(canonicalCompletion.ContentCommitment, roundTripCompletion.ContentCommitment);
            Assert.Equal(canonicalCompletion.AuthenticationChunkCommitment, roundTripCompletion.AuthenticationChunkCommitment);
            Assert.Equal(canonicalCompletion.EncryptionEnvelopeMetadataDigest, roundTripCompletion.EncryptionEnvelopeMetadataDigest);
            Assert.Equal(canonicalCompletion.WrappedKeyMetadataDigest, roundTripCompletion.WrappedKeyMetadataDigest);
            Assert.Equal(canonical.AsSpan(0, targetOffset).ToArray(), crafted.AsSpan(0, targetOffset).ToArray());
            Assert.Equal(
                canonical.AsSpan(targetOffset + 1, finalOffset - targetOffset - 1).ToArray(),
                crafted.AsSpan(targetOffset + 1, finalOffset - targetOffset - 1).ToArray());

            var manifest = new R211DependencyManifest(
                mutation.ToString(),
                mutation == R211ByteMutation.DataType ? "DATA.FrameType" : "DATA.Ordinal",
                SHA256.HashData(canonical), SHA256.HashData(crafted),
                canonical.Length, crafted.Length, targetOffset, before, after, finalOffset,
                new[] { $"[{targetOffset},{targetOffset + 1})", $"[{finalOffset + 17},{canonical.Length})" },
                new[] { "DataCiphertextDigest" },
                new[] { "DataCiphertextDigest", "FINAL ciphertext/tag", "whole-object digest fixture metadata" },
                new[] { "FINAL" },
                new[] { "CiphertextDigest" });
            CryptographicOperations.ZeroMemory(canonicalCompletionBytes);
            CryptographicOperations.ZeroMemory(repairedCompletionBytes);
            return new(inner, crafted, exact.CiphertextLength, manifest);
        }

        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        public Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken) =>
            Task.FromResult(new ExactObjectRead(
                new MemoryStream(craftedBytes, writable: false), reportedLength));
    }

    private sealed class R211ThrowingReadReconciler : IProvisionalObjectReconciler
    {
        internal int OpenCount { get; private set; }

        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator, CancellationToken cancellationToken)
        {
            OpenCount++;
            return Task.FromException<ExactObjectRead>(new IOException("r211-exact-read-unavailable"));
        }
    }

    private sealed class R211CommitmentProbe(ContentCommitmentResult result)
        : IContentCommitmentService
    {
        internal int CallCount { get; private set; }
        internal bool ReturnedSuccess { get; private set; }

        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ReturnedSuccess = result.IsSuccess;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class R211TransitionFailureCommitment(
        Func<ServiceProvider> providerFactory,
        Func<string, string, Task> rename) : IContentCommitmentService
    {
        internal int CallCount { get; private set; }
        internal bool TransitionDisabled { get; private set; }

        public async ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            CallCount++;
            await using var provider = providerFactory();
            await using var scope = provider.CreateAsyncScope();
            var result = await scope.ServiceProvider
                .GetRequiredService<IContentCommitmentService>()
                .ComputeAsync(selector, lpPayload, cancellationToken);
            Assert.True(result.IsSuccess);
            await rename(
                "raw_export_mark_provisional_object_verified(uuid,bigint,bytea)",
                "raw_export_mark_provisional_object_verified_r211_unavailable");
            TransitionDisabled = true;
            return result;
        }
    }

    private sealed class FixedVerificationOperation(AttemptAeadVerificationOutcome outcome)
        : IAttemptAeadVerificationOperation
    {
        public Task<AttemptAeadVerificationResult> DecryptAndVerifyBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            ReadOnlyMemory<byte> authenticationTag,
            CancellationToken cancellationToken) =>
            Task.FromResult(outcome switch
            {
                AttemptAeadVerificationOutcome.KeyAccessIndeterminate =>
                    AttemptAeadVerificationResult.KeyAccessIndeterminate(),
                AttemptAeadVerificationOutcome.AuthenticationFailed =>
                    AttemptAeadVerificationResult.AuthenticationFailed(),
                _ => throw new InvalidOperationException("Fixed verifier is failure-only."),
            });
    }

    private sealed class CancelingVerificationOperation(CancellationTokenSource cancellation)
        : IAttemptAeadVerificationOperation
    {
        public Task<AttemptAeadVerificationResult> DecryptAndVerifyBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            ReadOnlyMemory<byte> authenticationTag,
            CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            return Task.FromException<AttemptAeadVerificationResult>(
                new OperationCanceledException(cancellation.Token));
        }
    }

    private sealed class UnwrapFailingKekOperationProvider : IKekOperationProvider
    {
        public Task<KekWrapResult> WrapDekAsync(
            KekReference reference,
            ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            IAttemptDekCandidate candidate,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<KekOperationLookup> LookupByOperationTokenAsync(
            ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AttemptDekLease> UnwrapDekAsync(
            KekReference reference,
            KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint,
            CancellationToken cancellationToken) =>
            Task.FromException<AttemptDekLease>(new IOException("fixture-key-provider-unavailable"));
    }

    private sealed class LastByteTamperingReconciler(IProvisionalObjectReconciler inner)
        : IProvisionalObjectReconciler
    {
        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        public async Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken)
        {
            await using var exact = await inner.OpenExactReadAsync(locator, cancellationToken);
            using var buffer = new MemoryStream();
            await exact.Ciphertext.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            bytes[^1] ^= 0x01;
            return new ExactObjectRead(new MemoryStream(bytes, writable: false), exact.CiphertextLength);
        }
    }

    private sealed class FixedProvisioner(
        AttemptKeyProvisioningOutcome outcome,
        Guid reservationId) : IAttemptKeyReservationProvisioningOperation
    {
        internal int CallCount { get; private set; }

        public Task<AttemptKeyProvisioningResult> ProvisionAsync(
            AttemptKeyProvisioningRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new AttemptKeyProvisioningResult(outcome, reservationId, null));
        }
    }

    private sealed class CountingEncryptionOperation : IAttemptAeadEncryptionOperation
    {
        internal int CallCount { get; private set; }

        public Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new AttemptAeadChunkResult(
                request.Input.ToArray(),
                new byte[16]));
        }
    }

    private sealed class CountingEncryptionDecorator(IAttemptAeadEncryptionOperation inner)
        : IAttemptAeadEncryptionOperation
    {
        internal int CallCount { get; private set; }

        public async Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return await inner.EncryptBoundedChunkAsync(request, cancellationToken);
        }
    }

    private sealed class DualAeadProbe(IAttemptAeadVerificationOperation verification)
        : IAttemptAeadVerificationOperation, IAttemptAeadEncryptionOperation
    {
        internal int EncryptionCallCount { get; private set; }

        public Task<AttemptAeadVerificationResult> DecryptAndVerifyBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            ReadOnlyMemory<byte> authenticationTag,
            CancellationToken cancellationToken) =>
            verification.DecryptAndVerifyBoundedChunkAsync(
                request,
                authenticationTag,
                cancellationToken);

        public Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            EncryptionCallCount++;
            return Task.FromResult(new AttemptAeadChunkResult(
                request.Input.ToArray(),
                new byte[16]));
        }
    }

    private sealed class CountingObjectWriter(ConditionalPutOutcome outcome)
        : IProvisionalObjectWriter
    {
        internal int CallCount { get; private set; }

        public async Task<ConditionalPutResult> PutIfAbsentAsync(
            ExactWriteRequest request,
            Stream ciphertext,
            CancellationToken cancellationToken)
        {
            CallCount++;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[4096];
            long length = 0;
            try
            {
                while (true)
                {
                    var read = await ciphertext.ReadAsync(buffer, cancellationToken);
                    if (read == 0) break;
                    length += read;
                    hash.AppendData(buffer, 0, read);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
            var digest = hash.GetHashAndReset();
            if (outcome == ConditionalPutOutcome.OutcomeUnknown)
            {
                CryptographicOperations.ZeroMemory(digest);
                return new(outcome, null, 0, null, null);
            }

            var status = outcome == ConditionalPutOutcome.ConditionalConflict ? 412 : 200;
            var kind = outcome == ConditionalPutOutcome.ConditionalConflict
                ? "ConditionalConflictObserved"
                : "Created";
            var binding = new C1HashCanonical.Scalar(
                Convert.ToHexString(request.Locator.ObjectBindingDigest).ToLowerInvariant());
            var operation = new C1HashCanonical.Scalar(request.PutOperationId.ToString("N"));
            var statusText = new C1HashCanonical.Scalar(
                status.ToString(CultureInfo.InvariantCulture));
            var receipt = outcome == ConditionalPutOutcome.ConditionalConflict
                ? C1HashCanonical.Compute(
                    "tip-88c1-object-put-conflict-evidence-v1",
                    binding,
                    operation,
                    new C1HashCanonical.Scalar(kind),
                    statusText)
                : C1HashCanonical.Compute(
                    "tip-88c1-object-put-created-evidence-v1",
                    binding,
                    operation,
                    new C1HashCanonical.Scalar(kind),
                    statusText,
                    new C1HashCanonical.Scalar(length.ToString(CultureInfo.InvariantCulture)),
                    new C1HashCanonical.Scalar(
                        Convert.ToHexString(digest).ToLowerInvariant()));
            if (outcome == ConditionalPutOutcome.ConditionalConflict)
            {
                CryptographicOperations.ZeroMemory(digest);
                return new(outcome, status, 0, null, receipt);
            }
            return new(outcome, status, length, digest, receipt);
        }
    }

    private sealed class UnavailableCommitment : IContentCommitmentService
    {
        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(ContentCommitmentResult.Failed(
                ContentCommitmentFailure.ProviderFailure));
    }

    private sealed class TransactionAssertingProvisioner(
        TagEkycDbContext db,
        IAttemptKeyReservationProvisioningOperation inner)
        : IAttemptKeyReservationProvisioningOperation
    {
        internal int CallCount { get; private set; }

        public async Task<AttemptKeyProvisioningResult> ProvisionAsync(
            AttemptKeyProvisioningRequest request,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            CallCount++;
            var result = await inner.ProvisionAsync(request, cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }
    }

    private sealed class TransactionAssertingEncryption(
        TagEkycDbContext db,
        IAttemptAeadEncryptionOperation inner) : IAttemptAeadEncryptionOperation
    {
        internal int CallCount { get; private set; }

        public async Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            CallCount++;
            var result = await inner.EncryptBoundedChunkAsync(request, cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }
    }

    private sealed class TransactionAssertingVerification(
        TagEkycDbContext db,
        IAttemptAeadVerificationOperation inner) : IAttemptAeadVerificationOperation
    {
        internal int CallCount { get; private set; }

        public async Task<AttemptAeadVerificationResult> DecryptAndVerifyBoundedChunkAsync(
            AttemptAeadChunkRequest request,
            ReadOnlyMemory<byte> authenticationTag,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            CallCount++;
            var result = await inner.DecryptAndVerifyBoundedChunkAsync(
                request,
                authenticationTag,
                cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }
    }

    private sealed class TransactionAssertingCommitment(
        TagEkycDbContext db,
        IContentCommitmentService inner) : IContentCommitmentService
    {
        internal int CallCount { get; private set; }

        public async ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            CallCount++;
            var result = await inner.ComputeAsync(selector, lpPayload, cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }
    }

    private sealed class TransactionAssertingWriter(
        TagEkycDbContext db,
        IProvisionalObjectWriter inner) : IProvisionalObjectWriter
    {
        internal int CallCount { get; private set; }

        public async Task<ConditionalPutResult> PutIfAbsentAsync(
            ExactWriteRequest request,
            Stream ciphertext,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            CallCount++;
            var result = await inner.PutIfAbsentAsync(request, ciphertext, cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }
    }

    private sealed class TransactionAssertingReconciler(
        TagEkycDbContext db,
        IProvisionalObjectReconciler inner) : IProvisionalObjectReconciler
    {
        internal int OpenReadCount { get; private set; }

        public async Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            var result = await inner.InspectExactAsync(locator, cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }

        public async Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken)
        {
            Assert.Null(db.Database.CurrentTransaction);
            OpenReadCount++;
            var result = await inner.OpenExactReadAsync(locator, cancellationToken);
            Assert.Null(db.Database.CurrentTransaction);
            return result;
        }
    }

    private sealed class TransactionAssertingSourceStream(
        TagEkycDbContext db,
        byte[] plaintext) : MemoryStream(plaintext, writable: false)
    {
        internal int ReadCount { get; private set; }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            Assert.Null(db.Database.CurrentTransaction);
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
