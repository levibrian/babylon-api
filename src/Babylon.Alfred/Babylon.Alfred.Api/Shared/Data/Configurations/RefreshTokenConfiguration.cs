using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
        entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.ExpiresAt).IsRequired();
        entity.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false);

        entity.HasOne(d => d.User)
            .WithMany(p => p.RefreshTokens)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("refresh_tokens");
    }
}
