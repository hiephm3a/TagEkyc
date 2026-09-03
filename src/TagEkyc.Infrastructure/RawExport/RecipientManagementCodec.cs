using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientManagementCodec
{
    private const string IdempotencyLabel = "tip-88c1-c5-idempotency-v1";
    private const string PayloadLabel = "tip-88c1-c5-payload-v1";
    private const string EqualityLabel = "tip-88c1-c5-equality-v1";
    private const string ScopeSetLabel = "tip-88c1-c5-scope-set-v1";
    private const string TargetLabel = "tip-88c1-c5-target-v1";
    private const string AuditLabel = "tip-88c1-c5-audit-v2";

    public static readonly string[] ActivationScopes =
    [
        "business.raw-export.package.download",
        "business.raw-export.package.references.read",
    ];

    public static byte[] IdempotencyKeyDigest(string idempotencyKey) =>
        Hash(Fields(Utf8(IdempotencyLabel), Encoding.ASCII.GetBytes(idempotencyKey)));

    public static byte[] PayloadDigest(string operationKind, params byte[][] canonicalFields) =>
        Hash(Fields(Utf8(PayloadLabel), Utf8(operationKind), Fields(canonicalFields)));

    public static byte[] EqualityFingerprint(
        Guid managerPrincipalId,
        string operationKind,
        Guid recipientClientApplicationId,
        ReadOnlySpan<byte> payloadDigest) =>
        Hash(Fields(
            Utf8(EqualityLabel),
            Uuid(managerPrincipalId),
            Utf8(operationKind),
            Uuid(recipientClientApplicationId),
            Utf8(Hex(payloadDigest))));

    public static byte[] ScopeSetDigest(IEnumerable<string> scopes)
    {
        var ordered = scopes.Order(StringComparer.Ordinal).ToArray();
        var fields = new List<byte[]> { Utf8(ScopeSetLabel), Invariant(ordered.Length) };
        fields.AddRange(ordered.Select(Utf8));
        return Hash(Fields(fields.ToArray()));
    }

    public static string TargetIdentity(
        string operationKind,
        Guid recipientClientApplicationId,
        string recipientKeyId,
        int currentOrOnlyVersion,
        int? newVersion)
    {
        var payload = Fields(
            Utf8(TargetLabel), Utf8(operationKind), Uuid(recipientClientApplicationId),
            Utf8(recipientKeyId), Invariant(currentOrOnlyVersion),
            Utf8(newVersion is null ? "-" : newVersion.Value.ToString(CultureInfo.InvariantCulture)));
        return "v1." + Base64Url(payload);
    }

    internal static byte[] AuditEvidenceDigest(RecipientManagementAuditInput value) =>
        Hash(Fields(
            Utf8(AuditLabel), Uuid(value.OperationId), Utf8(value.OperationKind),
            Uuid(value.ManagerApiKeyId), Uuid(value.ManagerPrincipalId),
            Uuid(value.RecipientClientApplicationId), Utf8(value.EventType),
            Utf8(value.TargetIdentity), Utf8(value.Reason), Invariant(value.PriorRevision),
            Invariant(value.NewRevision), Utf8(Hex(value.PayloadDigest)),
            Utf8(DigestOrDash(value.PriorScopesDigest)),
            Utf8(DigestOrDash(value.NewScopesDigest)), Utc(value.OccurredAtUtc),
            Utf8(CountOrDash(value.AuthorizedDeliveryCount)),
            Utf8(CountOrDash(value.StreamingDeliveryCount)),
            Utf8(CountOrDash(value.InterruptedDeliveryCount))));

    public static byte[] CanonicalGuid(Guid value) => Uuid(value);
    public static byte[] CanonicalString(string value) => Utf8(value);
    public static byte[] CanonicalInteger(long value) => Invariant(value);
    public static byte[] CanonicalTimestamp(DateTimeOffset value) => Utc(value);
    public static byte[] CanonicalNullableTimestamp(DateTimeOffset? value) =>
        value is null ? [] : Utc(value.Value);

    public static byte[] CanonicalPayload(params byte[][] values) => Fields(values);

    private static string DigestOrDash(byte[]? value) => value is null ? "-" : Hex(value);
    private static string CountOrDash(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "-";
    private static byte[] Uuid(Guid value) => Encoding.ASCII.GetBytes(value.ToString("N"));
    private static byte[] Invariant(long value) => Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);
    private static byte[] Utc(DateTimeOffset value) => Encoding.ASCII.GetBytes(
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
    private static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();
    private static string Base64Url(ReadOnlySpan<byte> value) => Convert.ToBase64String(value)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Hash(byte[] value)
    {
        try { return SHA256.HashData(value); }
        finally { CryptographicOperations.ZeroMemory(value); }
    }

    private static byte[] Fields(params byte[][] values)
    {
        var length = values.Sum(value => checked(4 + value.Length));
        var result = new byte[length];
        var offset = 0;
        foreach (var value in values)
        {
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset, 4), value.Length);
            offset += 4;
            value.CopyTo(result.AsSpan(offset));
            offset += value.Length;
        }
        return result;
    }
}
