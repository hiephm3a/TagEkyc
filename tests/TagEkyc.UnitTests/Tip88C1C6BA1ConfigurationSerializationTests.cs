using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1ConfigurationSerializationTests
{
    [Fact]
    public void ConfigurationEtag_BindsExactClosedDocument_NotBusinessRevision()
    {
        var configuration = new CaptureRuntimeConfigurationResponse(Guid.NewGuid(), Guid.NewGuid(), 7,
            new DateTimeOffset(2026, 9, 11, 7, 0, 0, TimeSpan.FromHours(7)),
            new DateTimeOffset(2026, 9, 11, 8, 0, 0, TimeSpan.FromHours(7)),
            true, 60, 100, 10, 1024, 1024, 2048, 1024, 2048, 64);
        var first = CaptureRuntimeExecutionApplicationService.SerializeConfiguration(configuration);
        var same = CaptureRuntimeExecutionApplicationService.SerializeConfiguration(configuration with
        {
            EffectiveAtUtc = configuration.EffectiveAtUtc.ToUniversalTime(),
            ExpiresAtUtc = configuration.ExpiresAtUtc.ToUniversalTime()
        });
        Assert.Equal(first.Utf8Document, same.Utf8Document);
        Assert.Equal(first.ETag, same.ETag);
        Assert.Equal("\"" + Convert.ToBase64String(SHA256.HashData(first.Utf8Document))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + "\"", first.ETag);
        using var document = JsonDocument.Parse(first.Utf8Document);
        Assert.Equal(15, document.RootElement.EnumerateObject().Count());
        Assert.Equal(configuration.CaptureAgentId.ToString("N"), document.RootElement.GetProperty("CaptureAgentId").GetString());
        Assert.Equal("2026-09-11T00:00:00.0000000Z", document.RootElement.GetProperty("EffectiveAtUtc").GetString());
        Assert.DoesNotContain("\r", Encoding.UTF8.GetString(first.Utf8Document));
        var changed = CaptureRuntimeExecutionApplicationService.SerializeConfiguration(configuration with { PlaintextBudgetSeconds = 59 });
        Assert.NotEqual(first.ETag, changed.ETag);
        Assert.NotEqual(first.Utf8Document, changed.Utf8Document);
    }
}
