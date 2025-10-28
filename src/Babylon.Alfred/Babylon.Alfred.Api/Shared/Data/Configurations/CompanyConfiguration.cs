using Babylon.Alfred.Api.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babylon.Alfred.Api.Shared.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> entity)
    {
        entity.HasKey(e => e.Ticker);
        entity.Property(e => e.Ticker).IsRequired().HasMaxLength(50);
        entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LastUpdated);

        entity.ToTable("companies");
    }
}


