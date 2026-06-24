namespace TagEkyc.Infrastructure.Signing;

public sealed class Pkcs11Es256JwsEvidenceSignerOptions
{
    public const string SectionName = "TagEkyc:EvidenceSigning:Pkcs11";

    public string? LibraryPath { get; init; }

    public string? TokenLabel { get; init; }

    public string? Pin { get; init; }

    public string? KeyLabel { get; init; }

    public string? KeyObjectId { get; init; }

    public string? Kid { get; init; }

    public override string ToString() =>
        $"{nameof(Pkcs11Es256JwsEvidenceSignerOptions)} {{ " +
        $"{nameof(LibraryPath)} = {LibraryPath}, " +
        $"{nameof(TokenLabel)} = {TokenLabel}, " +
        $"{nameof(Pin)} = <redacted>, " +
        $"{nameof(KeyLabel)} = {KeyLabel}, " +
        $"{nameof(KeyObjectId)} = {KeyObjectId}, " +
        $"{nameof(Kid)} = {Kid} }}";
}
