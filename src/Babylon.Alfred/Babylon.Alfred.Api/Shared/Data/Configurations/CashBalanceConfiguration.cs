using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class CashBalanceConfiguration : IEntityTypeConfiguration<CashBalance>
{
    public void Configure(EntityTypeBuilder<CashBalance> entity)
    {
        entity.HasKey(e => e.UserId);
        entity.Property(e => e.Amount).HasPrecision(18, 4).IsRequired();
        entity.Property(e => e.LastUpdatedAt).IsRequired();
        entity.Property(e => e.LastUpdatedSource).IsRequired();

        entity.ToTable("cash_balances");

        entity.HasOne(e => e.User)
            .WithOne(u => u.CashBalance)
            .HasForeignKey<CashBalance>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
