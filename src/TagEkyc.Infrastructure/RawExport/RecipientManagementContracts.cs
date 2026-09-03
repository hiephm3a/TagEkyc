using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

public enum RecipientManagementTopology
{
    Disabled,
    PostgresDurable,
    Invalid,
}

public sealed record RecipientManagementOptions(
    RecipientManagementTopology Topology,
    string? DatabaseConnectionString,
    bool IsSyntacticallyValid)
{
    public const string SectionPath = "TagEkyc:RawExport:RecipientManagement";
    public const string ManagerLogin = "tagekyc_raw_export_recipient_manager_login";

    public override string ToString() =>
        $"RecipientManagementOptions {{ Topology = {Topology}, DatabaseConnectionString = [REDACTED] }}";
}

internal interface IRecipientManagementConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken);
}

internal sealed record RecipientManagementSqlResult(
    string Outcome,
    Guid OperationId,
    string ResultSnapshot,
    bool CandidateCommitted = false);

internal sealed record RecipientManagementAuditInput(
    Guid OperationId,
    string OperationKind,
    Guid ManagerApiKeyId,
    Guid ManagerPrincipalId,
    Guid RecipientClientApplicationId,
    string EventType,
    string TargetIdentity,
    string Reason,
    long PriorRevision,
    long NewRevision,
    byte[] PayloadDigest,
    byte[]? PriorScopesDigest,
    byte[]? NewScopesDigest,
    DateTimeOffset OccurredAtUtc,
    int? AuthorizedDeliveryCount,
    int? StreamingDeliveryCount,
    int? InterruptedDeliveryCount);
