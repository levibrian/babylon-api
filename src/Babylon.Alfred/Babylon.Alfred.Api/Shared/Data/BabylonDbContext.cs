using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContext : DbContext
{
    public BabylonDbContext(DbContextOptions<BabylonDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added here as we create entities
    // public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added here
    }
}
