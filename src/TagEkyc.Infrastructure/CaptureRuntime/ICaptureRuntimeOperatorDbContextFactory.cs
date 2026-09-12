using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public interface ICaptureRuntimeOperatorDbContextFactory
{
    ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken = default);
}
