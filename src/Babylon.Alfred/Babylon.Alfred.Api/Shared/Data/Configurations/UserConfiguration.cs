using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Password).IsRequired(false); // Nullable for Google-only users
        entity.Property(e => e.Username).HasMaxLength(50);
        entity.Property(e => e.AuthProvider).HasMaxLength(20);
        entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.ToTable("users");
    }
}
