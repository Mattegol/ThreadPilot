using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Tests.Integration;

/// <summary>
/// Custom factory for integration tests.
/// Currently uses in-memory repository and mocked VehicleClient.
/// When migrating to EF Core + Testcontainers:
/// 1. Implement IAsyncLifetime
/// 2. Add PostgreSqlContainer field
/// 3. Override ConfigureWebHost to swap DbContext
/// 4. Initialize/dispose container in IAsyncLifetime methods
/// For end-to-end tests, replace mock with real VehicleService instance
/// </summary>
public class InsuranceApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Mock the VehicleClient for isolated testing
            // For E2E tests, you could spin up both services and use real HttpClient
            var mockVehicleClient = new Mock<IVehicleClient>();

            mockVehicleClient
                .Setup(x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<VehicleDto>.Success(new VehicleDto("ABC123", "Volvo", "XC60", 2020)));

            mockVehicleClient
                .Setup(x => x.GetVehicleAsync("XYZ987", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<VehicleDto>.Success(new VehicleDto("XYZ987", "Tesla", "Model 3", 2022)));

            mockVehicleClient
                .Setup(x => x.GetVehicleAsync(It.IsNotIn("ABC123", "XYZ987"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<VehicleDto>.Failure(Errors.NotFound));

            services.AddSingleton(mockVehicleClient.Object);

            // Future: Remove in-memory repo and add EF Core DbContext here
            // services.RemoveAll<IInsuranceRepository>();
            // services.AddDbContext<InsuranceDbContext>(options =>
            //     options.UseNpgsql(_dbContainer.GetConnectionString()));
            // services.AddScoped<IInsuranceRepository, EfInsuranceRepository>();
        });

        builder.UseEnvironment("Testing");
    }
}
