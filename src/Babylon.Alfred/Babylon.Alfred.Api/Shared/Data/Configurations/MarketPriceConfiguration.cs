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
        entity.Property(e => e.Ticker).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Price).IsRequired().HasPrecision(18, 4);
        entity.Property(e => e.LastUpdated).IsRequired();

        // Index on Ticker for fast lookups
        entity.HasIndex(e => e.Ticker);
        
        // Index on LastUpdated for filtering stale prices
        entity.HasIndex(e => e.LastUpdated);

        entity.ToTable("market_prices");
    }
}

