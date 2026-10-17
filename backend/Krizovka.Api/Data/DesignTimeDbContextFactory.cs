using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Krizovka.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KrizovkaDbContext>
{
    public KrizovkaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KrizovkaDbContext>()
            .UseNpgsql("Host=localhost;Database=krizovka").Options;
        return new KrizovkaDbContext(options);
    }
}
