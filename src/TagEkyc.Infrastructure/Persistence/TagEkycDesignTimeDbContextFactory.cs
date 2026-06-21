using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class TagEkycDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TagEkycDbContext>
{
    public TagEkycDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TAGEKYC_DESIGN_TIME_CONNECTION_STRING") ??
            Environment.GetEnvironmentVariable("TAGEKYC_POSTGRES_CONNECTION_STRING") ??
            "Host=localhost;Port=5432;Database=tagekyc_design_time;Username=tagekyc;Password=tagekyc";

        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TagEkycDbContext(options);
    }
}
