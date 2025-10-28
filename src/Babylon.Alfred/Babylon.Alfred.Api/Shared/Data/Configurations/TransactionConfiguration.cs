using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Ticker).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TransactionType).IsRequired();
        entity.Property(e => e.Date).IsRequired();
        entity.Property(e => e.SharesQuantity).HasPrecision(18, 8);
        entity.Property(e => e.SharePrice).HasPrecision(18, 4);
        entity.Property(e => e.Fees).HasPrecision(18, 4);

        entity.ToTable("transactions");

        entity.HasIndex(e => e.Ticker);
    }
}


