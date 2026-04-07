using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using ThreadPilot.VehicleService.Vehicles;

namespace ThreadPilot.VehicleService.Tests;

public class VehicleServiceTests
{
    private readonly IVehicleRepository _repo;
    private readonly Vehicles.VehicleService _sut;

    public VehicleServiceTests()
    {
        _repo = new InMemoryVehicleRepository();
        _sut = new Vehicles.VehicleService(_repo, NullLogger<Vehicles.VehicleService>.Instance);
    }

    [Theory]
    [InlineData("ABC123", "Volvo", "XC60", 2020)]
    [InlineData("XYZ987", "Tesla", "Model 3", 2022)]
    [InlineData("QWE456", "Toyota", "Corolla", 2018)]
    public void GetVehicle_WithValidRegistration_ReturnsCompleteVehicleData(
        string registration, string brand, string model, int year)
    {
        var result = _sut.GetVehicle(registration);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.RegistrationNumber.ShouldBe(registration);
        result.Value!.Brand.ShouldBe(brand);
        result.Value!.Model.ShouldBe(model);
        result.Value!.Year.ShouldBe(year);
    }

    [Fact]
    public void GetVehicle_WithInvalidFormat_ReturnsValidationError()
    {
        var result = _sut.GetVehicle("INVALID");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithTooShortRegistration_ReturnsValidationError()
    {
        var result = _sut.GetVehicle("AB12");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithEmptyString_ReturnsValidationError()
    {
        var result = _sut.GetVehicle("");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithNull_ReturnsValidationError()
    {
        var result = _sut.GetVehicle(null!);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithWhitespace_ReturnsValidationError()
    {
        var result = _sut.GetVehicle("   ");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithLowercase_ReturnsValidationError()
    {
        var result = _sut.GetVehicle("abc123");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithSpecialCharacters_ReturnsValidationError()
    {
        var result = _sut.GetVehicle("ABC@123");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public void GetVehicle_WithUnknownRegistration_ReturnsNotFound()
    {
        var result = _sut.GetVehicle("ZZZ999");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("not_found");
    }

    [Fact]
    public void GetVehicle_WithValidFormatButNotInRepository_ReturnsNotFound()
    {
        var result = _sut.GetVehicle("AAA999");

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("not_found");
    }
}
