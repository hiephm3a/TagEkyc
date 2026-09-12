using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public interface ICaptureRuntimeDbContextFactory
{
    ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken = default);
}
