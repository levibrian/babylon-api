using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AllocationStrategy> AllocationStrategies { get; set; }
    public DbSet<MarketPrice> MarketPrices { get; set; }

    public BabylonDbContext(DbContextOptions<BabylonDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new AllocationStrategyConfiguration());
        modelBuilder.ApplyConfiguration(new MarketPriceConfiguration());
    }
}
