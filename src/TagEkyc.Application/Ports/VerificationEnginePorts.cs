namespace TagEkyc.Application.Ports;

public sealed record FaceMatchEngineRequest(
    string ReferenceFaceArtifactHash,
    string LiveFaceArtifactHash,
    string SessionId,
    string? RequestId = null,
    string? CorrelationId = null);

public sealed record FaceMatchEngineResult(
    bool IsMatch,
    decimal Score,
    decimal? Confidence,
    string EngineName,
    string EngineVersion,
    IReadOnlyList<string> ReasonCodes);

public interface IFaceMatchEngine
{
    Task<FaceMatchEngineResult> MatchAsync(
        FaceMatchEngineRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class DeterministicFixtureFaceMatchEngine : IFaceMatchEngine
{
    public const string FixtureEngineName = "fixture-face-match";
    public const string FixtureEngineVersion = "tip-69-seam";

    public Task<FaceMatchEngineResult> MatchAsync(
        FaceMatchEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        var sameHash = string.Equals(
            request.ReferenceFaceArtifactHash,
            request.LiveFaceArtifactHash,
            StringComparison.Ordinal);
        var score = sameHash ? 1.0m : 0.42m;

        return Task.FromResult(new FaceMatchEngineResult(
            sameHash,
            score,
            Confidence: score,
            FixtureEngineName,
            FixtureEngineVersion,
            sameHash ? ["FIXTURE_FACE_MATCH_PASS"] : ["FIXTURE_FACE_MATCH_REVIEW"]));
    }
}
