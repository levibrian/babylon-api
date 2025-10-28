using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    // This constant is used to identify the root user.
    // Will be removed in the future when proper user management is implemented.
    public static readonly Guid RootUserId = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d");

    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Password).IsRequired();
        entity.Property(e => e.Username).HasMaxLength(50);

        entity.ToTable("users");
    }
}
