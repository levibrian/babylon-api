using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContext(DbContextOptions<BabylonDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Security> Securities { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AllocationStrategy> AllocationStrategies { get; set; }
    public DbSet<MarketPrice> MarketPrices { get; set; }
    public DbSet<RecurringSchedule> RecurringSchedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new AllocationStrategyConfiguration());
        modelBuilder.ApplyConfiguration(new MarketPriceConfiguration());
        modelBuilder.ApplyConfiguration(new RecurringScheduleConfiguration());
    }
}
