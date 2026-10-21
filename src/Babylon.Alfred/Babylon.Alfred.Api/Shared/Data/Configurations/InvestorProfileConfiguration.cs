using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class InvestorProfileConfiguration : IEntityTypeConfiguration<InvestorProfile>
{
    public void Configure(EntityTypeBuilder<InvestorProfile> entity)
    {
        entity.HasKey(e => e.UserId);
        entity.Property(e => e.ProfileMarkdown).HasColumnType("text");
        entity.Property(e => e.RawAnswers).HasColumnType("text");
        entity.Property(e => e.RubricVersion);
        entity.Property(e => e.UpdatedAt).IsRequired();

        entity.ToTable("investor_profiles");

        entity.HasOne(e => e.User)
            .WithOne(u => u.InvestorProfile)
            .HasForeignKey<InvestorProfile>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
