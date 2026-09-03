using System.Data;
using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageDeliveryRepository(IRecipientPackageDeliveryConnectionFactory connections)
{
    internal Task<RecipientPackageDeliveryMutation> CreateAsync(
        Guid recipientId, Guid packageId, Guid deliveryId, byte[] idempotencyDigest,
        Guid apiKeyId, Guid principalId, byte[] correlationDigest, CancellationToken cancellationToken) =>
        CallAsync("raw_export_create_recipient_package_delivery", cancellationToken,
            ("recipient", recipientId), ("package", packageId), ("delivery", deliveryId),
            ("idempotency", idempotencyDigest), ("api_key", apiKeyId), ("principal", principalId),
            ("correlation", correlationDigest));

    internal Task<RecipientPackageDeliveryMutation> ReadAsync(
        Guid recipientId, Guid deliveryId, CancellationToken cancellationToken) =>
        CallAsync("raw_export_read_recipient_package_delivery", cancellationToken,
            ("recipient", recipientId), ("delivery", deliveryId));

    internal Task<RecipientPackageDeliveryMutation> ProbeContentAsync(
        Guid recipientId, Guid deliveryId, CancellationToken cancellationToken) =>
        CallAsync("raw_export_probe_recipient_package_delivery_content", cancellationToken,
            ("recipient", recipientId), ("delivery", deliveryId));

    internal Task<RecipientPackageDeliveryMutation> BeginAsync(
        Guid recipientId, Guid deliveryId, Guid apiKeyId, Guid principalId,
        byte[] correlationDigest, CancellationToken cancellationToken) =>
        CallAsync("raw_export_begin_recipient_package_delivery_stream", cancellationToken,
            ("recipient", recipientId), ("delivery", deliveryId), ("api_key", apiKeyId),
            ("principal", principalId), ("correlation", correlationDigest));

    internal Task<RecipientPackageDeliveryMutation> InterruptAsync(
        Guid deliveryId, long revision, long fence, string kind, byte[] evidence,
        CancellationToken cancellationToken) =>
        CallAsync("raw_export_record_recipient_package_delivery_interrupted", cancellationToken,
            ("delivery", deliveryId), ("revision", revision), ("fence", fence),
            ("kind", kind), ("evidence", evidence));

    internal Task<RecipientPackageDeliveryMutation> IntegrityUnavailableAsync(
        Guid deliveryId, long revision, long fence, string kind, byte[] evidence,
        CancellationToken cancellationToken) =>
        CallAsync("raw_export_record_recipient_package_integrity_unavailable", cancellationToken,
            ("delivery", deliveryId), ("revision", revision), ("fence", fence),
            ("kind", kind), ("evidence", evidence));

    internal Task<RecipientPackageDeliveryMutation> CompleteAsync(
        Guid deliveryId, long revision, long fence, long length, byte[] digest,
        CancellationToken cancellationToken) =>
        CallAsync("raw_export_complete_recipient_package_delivery", cancellationToken,
            ("delivery", deliveryId), ("revision", revision), ("fence", fence),
            ("length", length), ("digest", digest));

    internal Task<RecipientPackageDeliveryMutation> ReconcileNextAsync(CancellationToken cancellationToken) =>
        CallAsync("raw_export_reconcile_next_recipient_package_delivery", cancellationToken);

    private async Task<RecipientPackageDeliveryMutation> CallAsync(
        string function, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var connection = await connections.OpenAsync(cancellationToken).ConfigureAwait(false);
        var placeholders = string.Join(',', parameters.Select(value => '@' + value.Name));
        await using var command = new NpgsqlCommand($"SELECT * FROM tagekyc.{function}({placeholders})", connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return new("Unavailable", null);
        var outcome = Text(reader, "outcome") ?? "Unavailable";
        return new(outcome, Has(reader, "delivery_id") && !reader.IsDBNull(reader.GetOrdinal("delivery_id"))
            ? Projection(reader) : null);
    }

    private static RecipientPackageDeliveryProjection Projection(NpgsqlDataReader reader) => new(
        GuidValue(reader, "delivery_id"), GuidValue(reader, "package_id"), GuidValue(reader, "recipient_client_application_id"),
        Text(reader, "state")!, Long(reader, "revision"), Time(reader, "authorized_at_utc"),
        Time(reader, "authorization_expires_at_utc"), Int(reader, "stream_attempt_count"), Long(reader, "delivery_fence"),
        NullableTime(reader, "stream_started_at_utc"), NullableTime(reader, "stream_lease_expires_at_utc"),
        NullableTime(reader, "server_stream_completed_at_utc"), Long(reader, "encrypted_package_length"),
        Bytes(reader, "package_ciphertext_digest"), Bytes(reader, "envelope_digest"), Bytes(reader, "object_binding_digest"),
        Text(reader, "provider_configuration_id")!, Text(reader, "bucket_name")!, Text(reader, "object_key")!,
        NullableBytes(reader, "delivery_receipt_digest"));

    private static bool Has(NpgsqlDataReader reader, string name) => Enumerable.Range(0, reader.FieldCount).Any(i => string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase));
    private static string? Text(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetString(i); }
    private static Guid GuidValue(NpgsqlDataReader r, string n) => r.GetGuid(r.GetOrdinal(n));
    private static long Long(NpgsqlDataReader r, string n) => r.GetInt64(r.GetOrdinal(n));
    private static int Int(NpgsqlDataReader r, string n) => r.GetInt32(r.GetOrdinal(n));
    private static byte[] Bytes(NpgsqlDataReader r, string n) => (byte[])r.GetValue(r.GetOrdinal(n));
    private static byte[]? NullableBytes(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:(byte[])r.GetValue(i); }
    private static DateTimeOffset Time(NpgsqlDataReader r, string n) => r.GetFieldValue<DateTimeOffset>(r.GetOrdinal(n));
    private static DateTimeOffset? NullableTime(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetFieldValue<DateTimeOffset>(i); }
}
