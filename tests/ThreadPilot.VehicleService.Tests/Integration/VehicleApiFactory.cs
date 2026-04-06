using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ThreadPilot.VehicleService.Tests.Integration;

/// <summary>
/// Custom factory for integration tests.
/// Currently uses in-memory repository.
/// When migrating to EF Core + Testcontainers:
/// 1. Implement IAsyncLifetime
/// 2. Add PostgreSqlContainer field
/// 3. Override ConfigureWebHost to swap DbContext
/// 4. Initialize/dispose container in IAsyncLifetime methods
/// </summary>
public class VehicleApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Future: Remove in-memory repo and add EF Core DbContext here
            // services.RemoveAll<IVehicleRepository>();
            // services.AddDbContext<VehicleDbContext>(options =>
            //     options.UseNpgsql(_dbContainer.GetConnectionString()));
            // services.AddScoped<IVehicleRepository, EfVehicleRepository>();
        });

        builder.UseEnvironment("Testing");
    }
}
