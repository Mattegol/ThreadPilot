using Moq;
using Shouldly;
using ThreadPilot.InsuranceService.Insurances;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Tests;

public class InsuranceServiceTests
{
    private readonly IInsuranceRepository _repo;
    private readonly IInsurancePricingService _pricing;
    private readonly Mock<IVehicleClient> _vehicleClientMock;
    private readonly Insurances.InsuranceService _sut;

    public InsuranceServiceTests()
    {
        _repo = new InMemoryInsuranceRepository();
        _pricing = new InsurancePricingService();
        _vehicleClientMock = new Mock<IVehicleClient>();
        _sut = new Insurances.InsuranceService(
            _repo,
            _pricing,
            _vehicleClientMock.Object);
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenInvalidPersonalNumber_ReturnsValidationError()
    {
        var result = await _sut.GetInsurancesAsync("invalid", CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenEmptyPersonalNumber_ReturnsValidationError()
    {
        var result = await _sut.GetInsurancesAsync("", CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("validation_error");
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenPersonNotFound_ReturnsNotFound()
    {
        var result = await _sut.GetInsurancesAsync("19990101-9999", CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("not_found");
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenCarInsuranceExists_ShouldCallVehicleClient()
    {
        _vehicleClientMock
            .Setup(x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Success(new VehicleDto("ABC123", "Volvo", "XC60", 2020)));

        var result = await _sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        result.Value!.Insurances.Any(x => x.Type == InsuranceType.Car && x.Vehicle != null)
            .ShouldBeTrue();

        _vehicleClientMock.Verify(
            x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenCarInsuranceExists_ShouldMapVehicleDataCorrectly()
    {
        var expectedVehicle = new VehicleDto("ABC123", "Volvo", "XC60", 2020);
        _vehicleClientMock
            .Setup(x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Success(expectedVehicle));

        var result = await _sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        var carInsurance = result.Value!.Insurances.First(x => x.Type == InsuranceType.Car);
        carInsurance.Vehicle.ShouldNotBeNull();
        carInsurance.Vehicle!.RegistrationNumber.ShouldBe(expectedVehicle.RegistrationNumber);
        carInsurance.Vehicle.Brand.ShouldBe(expectedVehicle.Brand);
        carInsurance.Vehicle.Model.ShouldBe(expectedVehicle.Model);
        carInsurance.Vehicle.Year.ShouldBe(expectedVehicle.Year);
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenVehicleClientFails_ShouldReturnInsuranceWithoutVehicleData()
    {
        _vehicleClientMock
            .Setup(x => x.GetVehicleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Failure(Errors.DependencyFailure));

        var result = await _sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Insurances.Any(x => x.Type == InsuranceType.Car && x.Vehicle == null)
            .ShouldBeTrue();
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenNoCarInsurance_ShouldNotCallVehicleClient()
    {
        var result = await _sut.GetInsurancesAsync("19951212-9999", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Insurances.ShouldNotContain(x => x.Type == InsuranceType.Car);

        _vehicleClientMock.Verify(
            x => x.GetVehicleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetInsurancesAsync_WithMultipleInsurances_ShouldCalculateTotalCostCorrectly()
    {
        _vehicleClientMock
            .Setup(x => x.GetVehicleAsync("XYZ987", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Failure(Errors.DependencyFailure));

        var result = await _sut.GetInsurancesAsync("19770505-1111", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        // 19770505-1111 has Pet (20) + Health (20) + Car (20) = 60
        result.Value!.TotalMonthlyCost.ShouldBe(60);
        result.Value!.Insurances.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetInsurancesAsync_ShouldReturnAllInsurancesForPerson()
    {
        _vehicleClientMock
            .Setup(x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Success(new VehicleDto("ABC123", "Volvo", "XC60", 2020)));

        var result = await _sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.PersonalNumber.ShouldBe("19800101-1234");
        result.Value!.Insurances.Count.ShouldBe(2); // Pet and Car
        result.Value!.Insurances.ShouldContain(x => x.Type == InsuranceType.Pet);
        result.Value!.Insurances.ShouldContain(x => x.Type == InsuranceType.Car);
    }

    [Fact]
    public async Task GetInsurancesAsync_ShouldReturnValidInsuranceData()
    {
        var result = await _sut.GetInsurancesAsync("19951212-9999", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Insurances.ShouldAllBe(x =>
            x.Type != null &&
            x.MonthlyCost > 0 &&
            !string.IsNullOrEmpty(x.Type.ToString()));
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenHealthInsuranceOnly_ShouldReturnCorrectCost()
    {
        var result = await _sut.GetInsurancesAsync("19951212-9999", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Insurances.Count.ShouldBe(1);
        result.Value!.Insurances.First().Type.ShouldBe(InsuranceType.Health);
        result.Value!.TotalMonthlyCost.ShouldBe(20); // Health insurance cost
    }
}