using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88B4RawExportJobTests
{
    private static readonly FingerprintInput Golden = new(
        Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
        Guid.Parse("10112233-4455-6677-8899-aabbccddeeff"),
        Guid.Parse("20112233-4455-6677-8899-aabbccddeeff"),
        Guid.Parse("30112233-4455-6677-8899-aabbccddeeff"),
        Guid.Parse("40112233-4455-6677-8899-aabbccddeeff"),
        "subject:e\u0301kyc",
        Guid.Parse("50112233-4455-6677-8899-aabbccddeeff"),
        7,
        "purpose:chia se\u0309",
        Guid.Parse("60112233-4455-6677-8899-aabbccddeeff"),
        RawExportMode.EncryptedExportPacket,
        DateTimeOffset.Parse("2026-07-26T12:34:56Z"),
        DateTimeOffset.Parse("2026-07-26T12:30:00Z"),
        "B4.Key-01:retry",
        [RawExportRawClass.LiveSelfieImage, RawExportRawClass.HandSignatureImage]);

    [Fact]
    public void M4_job_bind_fingerprint_matches_independent_golden_vectors()
    {
        var independentGolden = ComputeIndependent(Golden);
        var productionGolden = ComputeProduction(Golden);

        Assert.Equal(
            "524b0b03389cd0a16ae4af78f9151687083ce950287483c7fdf2f75f8b1a9d87",
            Convert.ToHexString(independentGolden).ToLowerInvariant());
        Assert.Equal(independentGolden, productionGolden);
        Assert.Equal(
            independentGolden,
            ComputeProduction(Golden with { SubjectRef = "subject:\u00e9kyc" }));
        Assert.Equal(
            independentGolden,
            ComputeProduction(Golden with { PurposeCode = "purpose:chia s\u1ebb" }));

        var mutations = new FingerprintInput[]
        {
            Golden with { PrincipalId = Guid.Parse("01112233-4455-6677-8899-aabbccddeeff") },
            Golden with { ClientApplicationId = Guid.Parse("11112233-4455-6677-8899-aabbccddeeff") },
            Golden with { PermitId = Guid.Parse("21112233-4455-6677-8899-aabbccddeeff") },
            Golden with { AuthorizationDecisionId = Guid.Parse("31112233-4455-6677-8899-aabbccddeeff") },
            Golden with { VerificationSessionId = Guid.Parse("41112233-4455-6677-8899-aabbccddeeff") },
            Golden with { SubjectRef = "subject:other" },
            Golden with { PolicyId = Guid.Parse("51112233-4455-6677-8899-aabbccddeeff") },
            Golden with { PolicyVersion = 8 },
            Golden with { PurposeCode = "purpose:other" },
            Golden with { RecipientClientApplicationId = Guid.Parse("61112233-4455-6677-8899-aabbccddeeff") },
            Golden with { ExportMode = RawExportMode.ExternalExportOnlyNoRetain },
            Golden with { ExportMode = RawExportMode.EncryptedRawVaultRetained },
            Golden with { PermitExpiresAt = Golden.PermitExpiresAt.AddTicks(1) },
            Golden with { JobExpiresAt = Golden.JobExpiresAt.AddTicks(1) },
            Golden with { IdempotencyKey = "b4.Key-01:retry" },
            Golden with { Classes = [RawExportRawClass.HandSignatureImage, RawExportRawClass.LiveSelfieImage] },
            Golden with { Classes = [RawExportRawClass.LiveSelfieImage] },
            Golden with { Classes = [RawExportRawClass.LiveSelfieImage, RawExportRawClass.HandSignatureImage, RawExportRawClass.LivenessMedia] },
            Golden with { Classes = [RawExportRawClass.LiveSelfieImage, RawExportRawClass.LiveSelfieImage] },
            Golden with { Classes = [RawExportRawClass.LiveSelfieImage, RawExportRawClass.LivenessMedia] },
        };

        Assert.All(
            mutations,
            mutation =>
            {
                var independentMutation = ComputeIndependent(mutation);
                Assert.NotEqual(independentGolden, independentMutation);
                Assert.Equal(independentMutation, ComputeProduction(mutation));
            });
    }

    [Theory]
    [InlineData(null, 60, true)]
    [InlineData("10", 10, true)]
    [InlineData("300", 300, true)]
    [InlineData("", 0, false)]
    [InlineData("9", 0, false)]
    [InlineData("301", 0, false)]
    [InlineData("not-an-integer", 0, false)]
    public void Lease_configuration_is_closed_and_bounded(
        string? configured,
        int expectedSeconds,
        bool expectedValid)
    {
        var result = RawExportJobLeaseOptions.Resolve(configured);

        Assert.Equal(expectedSeconds, result.LeaseSeconds);
        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(
            expectedValid ? null : RawExportJobLeaseOptions.InvalidCode,
            result.InvalidCode);
    }

    private static byte[] ComputeProduction(FingerprintInput input) =>
        RawExportJobFingerprintCodec.Compute(
            input.PrincipalId,
            input.ClientApplicationId,
            input.PermitId,
            input.AuthorizationDecisionId,
            input.VerificationSessionId,
            input.SubjectRef,
            input.PolicyId,
            input.PolicyVersion,
            input.PurposeCode,
            input.RecipientClientApplicationId,
            input.ExportMode,
            input.PermitExpiresAt,
            input.JobExpiresAt,
            input.IdempotencyKey,
            input.Classes);

    private static byte[] ComputeIndependent(FingerprintInput input)
    {
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes("tagekyc:raw-export-job-bind:v1"));
        AppendGuid(stream, input.PrincipalId);
        AppendGuid(stream, input.ClientApplicationId);
        AppendGuid(stream, input.PermitId);
        AppendGuid(stream, input.AuthorizationDecisionId);
        AppendGuid(stream, input.VerificationSessionId);
        AppendText(stream, input.SubjectRef);
        AppendGuid(stream, input.PolicyId);
        AppendInt32(stream, input.PolicyVersion);
        AppendText(stream, input.PurposeCode);
        AppendGuid(stream, input.RecipientClientApplicationId);
        AppendText(stream, input.ExportMode.ToString());
        AppendInt64(stream, input.PermitExpiresAt.UtcTicks);
        AppendInt64(stream, input.JobExpiresAt.UtcTicks);
        AppendText(stream, input.IdempotencyKey);
        AppendInt32(stream, input.Classes.Count);
        for (var ordinal = 0; ordinal < input.Classes.Count; ordinal++)
        {
            AppendInt32(stream, ordinal);
            AppendText(stream, input.Classes[ordinal].ToString());
        }

        return SHA256.HashData(stream.ToArray());
    }

    private static void AppendGuid(Stream stream, Guid value) =>
        stream.Write(value.ToByteArray(bigEndian: true));

    private static void AppendText(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC));
        AppendInt32(stream, bytes.Length);
        stream.Write(bytes);
    }

    private static void AppendInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void AppendInt64(Stream stream, long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private sealed record FingerprintInput(
        Guid PrincipalId,
        Guid ClientApplicationId,
        Guid PermitId,
        Guid AuthorizationDecisionId,
        Guid VerificationSessionId,
        string SubjectRef,
        Guid PolicyId,
        int PolicyVersion,
        string PurposeCode,
        Guid RecipientClientApplicationId,
        RawExportMode ExportMode,
        DateTimeOffset PermitExpiresAt,
        DateTimeOffset JobExpiresAt,
        string IdempotencyKey,
        IReadOnlyList<RawExportRawClass> Classes);
}
