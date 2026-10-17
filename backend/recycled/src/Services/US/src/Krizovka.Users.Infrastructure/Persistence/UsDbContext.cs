using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Krizovka.Users.Infrastructure.Persistence;

public class UsDbContext : DbContext
{
    public UsDbContext(DbContextOptions<UsDbContext> options) : base(options) { }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<AccountClaim> AccountClaims => Set<AccountClaim>();
    public DbSet<BackofficeUser> BackofficeUsers => Set<BackofficeUser>();
    public DbSet<BackofficeRole> BackofficeRoles => Set<BackofficeRole>();
    public DbSet<BackofficePermission> BackofficePermissions => Set<BackofficePermission>();
    public DbSet<UserVerification> UserVerifications => Set<UserVerification>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsDbContext).Assembly);
    }


}

// Design-time factory for migrations
public class UsDbContextFactory : IDesignTimeDbContextFactory<UsDbContext>
{
    public UsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<UsDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? "Host=localhost;Database=krizovka_users")
                .UseSnakeCaseNamingConvention()
            .Options;
        return new UsDbContext(opts);
    }
}
