using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class PortfolioSnapshotConfiguration : IEntityTypeConfiguration<PortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<PortfolioSnapshot> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        
        // Foreign key to User
        entity.Property(e => e.UserId).IsRequired();
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        entity.Property(e => e.SnapshotDate).IsRequired();
        entity.Property(e => e.TotalInvested).IsRequired().HasPrecision(18, 2);
        entity.Property(e => e.TotalMarketValue).IsRequired().HasPrecision(18, 2);
        entity.Property(e => e.UnrealizedPnL).IsRequired().HasPrecision(18, 2);
        entity.Property(e => e.UnrealizedPnLPercentage).IsRequired().HasPrecision(8, 4);
        entity.Property(e => e.CreatedAt).IsRequired();

        // Unique index on UserId + SnapshotDate - one snapshot per user per day
        entity.HasIndex(e => new { e.UserId, e.SnapshotDate }).IsUnique();
        
        // Index on SnapshotDate for date range queries
        entity.HasIndex(e => e.SnapshotDate);

        entity.ToTable("portfolio_snapshots");
    }
}

