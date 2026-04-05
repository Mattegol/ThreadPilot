using FluentAssertions;
using ThreadPilot.VehicleService.Vehicles;

namespace ThreadPilot.VehicleService.Tests;

public class VehicleServiceTests
{
    [Fact]
    public void GetVehicle_WithValidRegistration_ReturnsVehicle()
    {
        var repo = new InMemoryVehicleRepository();
        var service = new VehicleService.Vehicles.VehicleService(repo);

        var result = service.GetVehicle("ABC123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RegistrationNumber.Should().Be("ABC123");
    }

    [Fact]
    public void GetVehicle_WithInvalidRegistration_ReturnsValidationError()
    {
        var repo = new InMemoryVehicleRepository();
        var service = new VehicleService.Vehicles.VehicleService(repo);

        var result = service.GetVehicle("INVALID");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("validation_error");
    }

    [Fact]
    public void GetVehicle_WithUnknownRegistration_ReturnsNotFound()
    {
        var repo = new InMemoryVehicleRepository();
        var service = new VehicleService.Vehicles.VehicleService(repo);

        var result = service.GetVehicle("AAA999");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }
}