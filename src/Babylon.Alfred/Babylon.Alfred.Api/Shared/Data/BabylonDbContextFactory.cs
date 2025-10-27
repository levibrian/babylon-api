using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Babylon.Alfred.Api.Shared.Data;

public class BabylonDbContextFactory : IDesignTimeDbContextFactory<BabylonDbContext>
{
    public BabylonDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BabylonDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=babylon_dev;Username=postgres;Password=postgres;Port=5432");

        return new BabylonDbContext(optionsBuilder.Options);
    }
}

