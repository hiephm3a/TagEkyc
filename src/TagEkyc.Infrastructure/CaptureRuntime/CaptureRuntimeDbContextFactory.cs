using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeDbContextFactory : ICaptureRuntimeDbContextFactory
{
    private readonly DbContextOptions<TagEkycDbContext> _options;

    public CaptureRuntimeDbContextFactory(CaptureRuntimeResolvedDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(options.OnlineConnectionString)
            .Options;
    }

    public ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new TagEkycDbContext(_options));
    }
}
