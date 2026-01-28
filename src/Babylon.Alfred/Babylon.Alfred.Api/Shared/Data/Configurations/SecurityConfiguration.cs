using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class SecurityConfiguration : IEntityTypeConfiguration<Security>
{
    public void Configure(EntityTypeBuilder<Security> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.Ticker).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Isin).HasMaxLength(12);
        entity.Property(e => e.SecurityName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.SecurityType)
            .IsRequired()
            .HasConversion<int>();
        entity.Property(e => e.Currency).HasMaxLength(10);
        entity.Property(e => e.Exchange).HasMaxLength(50);
        entity.Property(e => e.Sector).HasMaxLength(100);
        entity.Property(e => e.Industry).HasMaxLength(150);
        entity.Property(e => e.Geography).HasMaxLength(50);
        entity.Property(e => e.MarketCap).HasColumnType("decimal(20,2)"); // Support up to hundreds of trillions
        entity.Property(e => e.LastUpdated);

        // Unique index on Ticker (can have multiple tickers per company in future)
        entity.HasIndex(e => e.Ticker)
            .IsUnique();

        // Non-unique index on ISIN (one ISIN can map to multiple tickers)
        entity.HasIndex(e => e.Isin);

        entity.ToTable("securities");
    }
}

