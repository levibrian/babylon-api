using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SecurityId).IsRequired();
        entity.Property(e => e.TransactionType).IsRequired();
        entity.Property(e => e.Date).IsRequired();
        entity.Property(e => e.SharesQuantity).HasPrecision(18, 8);
        entity.Property(e => e.SharePrice).HasPrecision(18, 4);
        entity.Property(e => e.Fees).HasPrecision(18, 4);
        entity.Property(e => e.Tax).HasPrecision(18, 4);
        entity.Property(e => e.UserId);

        entity.ToTable("transactions");

        entity.HasIndex(e => e.SecurityId);
        entity.HasIndex(e => e.UserId);

        // Transactions -> User Configuration
        entity.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transactions -> Security Configuration
        entity.HasOne(t => t.Security)
            .WithMany(s => s.Transactions)
            .HasForeignKey(t => t.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


