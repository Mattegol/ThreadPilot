using FluentAssertions;
using Moq;
using ThreadPilot.InsuranceService.Insurances;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Tests;

public class InsuranceServiceTests
{
    [Fact]
    public async Task GetInsurancesAsync_WhenInvalidPersonalNumber_ReturnsValidationError()
    {
        var repo = new InMemoryInsuranceRepository();
        var pricing = new InsurancePricingService();

        var vehicleClientMock = new Mock<IVehicleClient>();

        var service = new InsuranceService.Insurances.InsuranceService(
            repo,
            pricing,
            vehicleClientMock.Object);

        var result = await service.GetInsurancesAsync("invalid", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation_error");
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenCarInsuranceExists_ShouldCallVehicleClient()
    {
        var repo = new InMemoryInsuranceRepository();
        var pricing = new InsurancePricingService();

        var vehicleClientMock = new Mock<IVehicleClient>();
        vehicleClientMock
            .Setup(x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Success(new VehicleDto("ABC123", "Volvo", "XC60", 2020)));

        var service = new InsuranceService.Insurances.InsuranceService(
            repo,
            pricing,
            vehicleClientMock.Object);

        var result = await service.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Insurances.Should().Contain(x => x.Type == InsuranceType.Car && x.Vehicle != null);

        vehicleClientMock.Verify(
            x => x.GetVehicleAsync("ABC123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInsurancesAsync_TotalMonthlyCost_ShouldBeCalculatedCorrectly()
    {
        var repo = new InMemoryInsuranceRepository();
        var pricing = new InsurancePricingService();

        var vehicleClientMock = new Mock<IVehicleClient>();
        vehicleClientMock
            .Setup(x => x.GetVehicleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<VehicleDto>.Failure(Errors.DependencyFailure));

        var service = new InsuranceService.Insurances.InsuranceService(
            repo,
            pricing,
            vehicleClientMock.Object);

        var result = await service.GetInsurancesAsync("19770505-1111", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalMonthlyCost.Should().Be(10 + 20 + 30);
    }
}