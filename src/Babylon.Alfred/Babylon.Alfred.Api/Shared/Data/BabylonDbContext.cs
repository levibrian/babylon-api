using Babylon.Alfred.Api.Shared.Data.Models;
using Babylon.Alfred.Api.Shared.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Company> Companies { get; set; }

    public BabylonDbContext(DbContextOptions<BabylonDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
    }
}
