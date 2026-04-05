using Shouldly;
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

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.RegistrationNumber.ShouldBe("ABC123");
    }

    [Fact]
    public void GetVehicle_WithInvalidRegistration_ReturnsValidationError()
    {
        var repo = new InMemoryVehicleRepository();
        var service = new VehicleService.Vehicles.VehicleService(repo);

        var result = service.GetVehicle("INVALID");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithUnknownRegistration_ReturnsNotFound()
    {
        var repo = new InMemoryVehicleRepository();
        var service = new VehicleService.Vehicles.VehicleService(repo);

        var result = service.GetVehicle("AAA999");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("not_found");
    }
}