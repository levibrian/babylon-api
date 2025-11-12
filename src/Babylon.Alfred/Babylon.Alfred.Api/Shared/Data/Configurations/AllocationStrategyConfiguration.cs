using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class AllocationStrategyConfiguration : IEntityTypeConfiguration<AllocationStrategy>
{
    public void Configure(EntityTypeBuilder<AllocationStrategy> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.SecurityId).IsRequired();
        entity.Property(e => e.TargetPercentage).IsRequired().HasPrecision(18, 4);
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();

        // Unique constraint: one target allocation per security per user
        entity.HasIndex(e => new { e.UserId, e.SecurityId })
            .IsUnique();

        // Foreign key to User
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Security
        entity.HasOne(e => e.Security)
            .WithMany(s => s.AllocationStrategies)
            .HasForeignKey(e => e.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable("allocation_strategies");
    }
}

