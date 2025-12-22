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

        entity.Property(e => e.Timestamp).IsRequired();
        entity.Property(e => e.TotalInvested).IsRequired().HasPrecision(18, 2);
        entity.Property(e => e.TotalMarketValue).IsRequired().HasPrecision(18, 2);
        entity.Property(e => e.UnrealizedPnL).IsRequired().HasPrecision(18, 2);
        entity.Property(e => e.UnrealizedPnLPercentage).IsRequired().HasPrecision(8, 4);

        // Index on UserId + Timestamp for efficient user queries
        entity.HasIndex(e => new { e.UserId, e.Timestamp });

        // Index on Timestamp for date range queries
        entity.HasIndex(e => e.Timestamp);

        entity.ToTable("portfolio_snapshots");
    }
}

