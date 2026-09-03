using System.Reflection;
using System.Text;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2CoreArchitectureTests
{
    private static readonly string[] IntendedIdentifiers =
    [
        "raw_export_source_reservations",
        "raw_export_source_encryption_attempts",
        "raw_export_source_head",
        "pk_raw_export_source_reservations",
        "uq_raw_export_source_ingress_source",
        "ck_raw_export_source_reservation_values",
        "fk_raw_export_source_reservation_claim",
        "fk_raw_export_source_reservation_consent_policy",
        "ix_raw_export_source_reservation_consent",
        "pk_raw_export_source_encryption_attempts",
        "uq_raw_export_source_attempt_fence",
        "uq_raw_export_source_attempt_revision",
        "ck_raw_export_source_attempt_values",
        "fk_raw_export_source_attempt_reservation",
        "pk_raw_export_source_head",
        "ck_raw_export_source_head_values",
        "fk_raw_export_source_head_attempt",
        "fk_raw_export_source_head_reservation",
        "ix_raw_export_source_head_attempt",
        "fk_raw_export_authority_snapshot_consent_policy",
        "ix_raw_export_authority_consent_policy",
        "tr_raw_export_source_reservations_guard",
        "tr_raw_export_source_attempts_guard",
        "tr_raw_export_source_head_guard",
        "enforce_raw_export_source_core_write",
        "enforce_raw_export_source_head_write",
        "raw_export_c1_hash_canonical",
        "complete_raw_export_source_ingress_claim",
    ];

    [Fact]
    public void C1B2CORE_all_intended_identifiers_are_at_most_63_utf8_bytes()
    {
        Assert.All(
            IntendedIdentifiers,
            identifier => Assert.InRange(
                Encoding.UTF8.GetByteCount(identifier),
                1,
                63));
    }

    [Fact]
    public void C1B2CORE_persisted_entities_have_no_claimed_plaintext_digest_surface()
    {
        var properties = new[]
            {
                typeof(RawExportSourceReservationRow),
                typeof(RawExportSourceEncryptionAttemptRow),
                typeof(RawExportSourceHeadRow),
            }
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain(
            properties,
            name => name.Contains(
                "PlaintextDigest",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void C1B2CORE_broker_contract_is_not_an_http_or_signflow_surface()
    {
        var contract = typeof(IRawExportSourceClaimComparisonBroker);
        Assert.Equal("TagEkyc.Contracts.RawExport", contract.Namespace);
        Assert.DoesNotContain(
            contract.Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name?.Contains(
                "SignFlow",
                StringComparison.OrdinalIgnoreCase) == true);

        var repo = FindRepoRoot();
        var program = File.ReadAllText(
            Path.Combine(repo, "src", "TagEkyc.Api", "Program.cs"));
        Assert.DoesNotContain(
            "AddTagEkycRawExportSourceClaimComparison",
            program,
            StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
