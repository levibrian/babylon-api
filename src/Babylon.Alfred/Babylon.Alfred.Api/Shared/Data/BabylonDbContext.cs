using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }

    public BabylonDbContext(DbContextOptions<BabylonDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ticker).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TransactionType).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.SharesQuantity).HasPrecision(18, 6);
            entity.Property(e => e.SharePrice).HasPrecision(18, 4);
            entity.Property(e => e.Fees).HasPrecision(18, 4);
            
            entity.ToTable("transactions");
        });
    }
}
