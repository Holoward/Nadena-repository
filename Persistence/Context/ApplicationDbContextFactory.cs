using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Persistence.Context;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebApi"))
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=nadena_dev;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(
            optionsBuilder.Options,
            new DesignTimeDateTimeService(),
            new DesignTimeCurrentUserService());
    }
}

internal class DesignTimeDateTimeService : Application.Interfaces.IDateTimeService
{
    public DateTime NowUtc => DateTime.UtcNow;
}

internal class DesignTimeCurrentUserService : Application.Interfaces.ICurrentUserService
{
    public string GetCurrentUserId() => "design-time";
    public string GetCurrentUserName() => "design-time";
}
