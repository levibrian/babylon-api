using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class RecurringScheduleConfiguration : IEntityTypeConfiguration<RecurringSchedule>
{
    public void Configure(EntityTypeBuilder<RecurringSchedule> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.UserId).IsRequired();
        entity.Property(e => e.SecurityId).IsRequired();
        entity.Property(e => e.Platform).HasMaxLength(100);
        entity.Property(e => e.TargetAmount).HasPrecision(18, 4);
        entity.Property(e => e.IsActive).IsRequired();
        entity.Property(e => e.CreatedAt).IsRequired();

        // Unique constraint: one active schedule per security per user
        // Using partial unique index to enforce only one active schedule per UserId+SecurityId
        entity.HasIndex(e => new { e.UserId, e.SecurityId })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");

        // Foreign key to User
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Security
        entity.HasOne(e => e.Security)
            .WithMany()
            .HasForeignKey(e => e.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable("recurring_schedules");
    }
}

