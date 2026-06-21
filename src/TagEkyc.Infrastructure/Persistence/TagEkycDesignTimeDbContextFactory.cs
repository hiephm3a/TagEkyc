using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class TagEkycDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TagEkycDbContext>
{
    public TagEkycDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=tagekyc_design_time;Username=postgres;Password=postgres")
            .Options;

        return new TagEkycDbContext(options);
    }
}
