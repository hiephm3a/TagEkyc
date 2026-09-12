using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeOperatorDbContextFactory : ICaptureRuntimeOperatorDbContextFactory
{
    private readonly DbContextOptions<TagEkycDbContext> _options;

    public CaptureRuntimeOperatorDbContextFactory(CaptureRuntimeResolvedDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(options.OperatorConnectionString)
            .Options;
    }

    public ValueTask<TagEkycDbContext> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new TagEkycDbContext(_options));
    }
}
