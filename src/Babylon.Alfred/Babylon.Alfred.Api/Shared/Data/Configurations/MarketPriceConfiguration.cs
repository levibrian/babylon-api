using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class MarketPriceConfiguration : IEntityTypeConfiguration<MarketPrice>
{
    public void Configure(EntityTypeBuilder<MarketPrice> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        
        // Foreign key to Security (normalized)
        entity.Property(e => e.SecurityId).IsRequired();
        entity.HasOne(e => e.Security)
            .WithMany()
            .HasForeignKey(e => e.SecurityId)
            .OnDelete(DeleteBehavior.Cascade);
        
        entity.Property(e => e.Price).IsRequired().HasPrecision(18, 4);
        entity.Property(e => e.Currency).HasMaxLength(10);
        entity.Property(e => e.LastUpdated).IsRequired();

        // Unique index on SecurityId - one price per security
        entity.HasIndex(e => e.SecurityId).IsUnique();
        
        // Index on LastUpdated for filtering stale prices
        entity.HasIndex(e => e.LastUpdated);

        entity.ToTable("market_prices");
    }
}

