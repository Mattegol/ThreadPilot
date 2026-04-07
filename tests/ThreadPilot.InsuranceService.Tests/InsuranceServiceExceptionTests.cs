using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using ThreadPilot.InsuranceService.Insurances;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.InsuranceService.Tests;

public class InsuranceServiceExceptionTests
{
    [Fact]
    public async Task GetInsurancesAsync_WhenRepositoryThrowsException_ReturnsInternalError()
    {
        // Arrange: Create mock repository that throws
        var repoMock = new Mock<IInsuranceRepository>();
        repoMock
            .Setup(x => x.GetByPersonalNumber(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Database connection failed"));

        var pricingMock = new Mock<IInsurancePricingService>();
        var vehicleClientMock = new Mock<IVehicleClient>();
        var loggerMock = new Mock<ILogger<Insurances.InsuranceService>>();

        var sut = new Insurances.InsuranceService(
            repoMock.Object,
            pricingMock.Object,
            vehicleClientMock.Object,
            loggerMock.Object);

        // Act
        var result = await sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("internal_error");
        result.Error.Message.ShouldBe("An unexpected error occurred while retrieving insurances");

        // Verify exception was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error retrieving insurances")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenPricingServiceThrowsException_ReturnsInternalError()
    {
        // Arrange
        var repo = new InMemoryInsuranceRepository();

        var pricingMock = new Mock<IInsurancePricingService>();
        pricingMock
            .Setup(x => x.GetMonthlyCost(It.IsAny<InsuranceType>()))
            .Throws(new InvalidOperationException("Pricing calculation failed"));

        var vehicleClientMock = new Mock<IVehicleClient>();
        var loggerMock = new Mock<ILogger<Insurances.InsuranceService>>();

        var sut = new Insurances.InsuranceService(
            repo,
            pricingMock.Object,
            vehicleClientMock.Object,
            loggerMock.Object);

        // Act
        var result = await sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("internal_error");

        // Verify exception was logged with personal number
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("19800101-1234")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetInsurancesAsync_WhenMultipleExceptions_LogsAllAndReturnsError()
    {
        // Arrange: Simulate a scenario where pricing fails for the first insurance
        var repo = new InMemoryInsuranceRepository();

        var pricingMock = new Mock<IInsurancePricingService>();
        pricingMock
            .Setup(x => x.GetMonthlyCost(It.IsAny<InsuranceType>()))
            .Throws(new Exception("Critical pricing error"));

        var vehicleClientMock = new Mock<IVehicleClient>();
        var loggerMock = new Mock<ILogger<Insurances.InsuranceService>>();

        var sut = new Insurances.InsuranceService(
            repo,
            pricingMock.Object,
            vehicleClientMock.Object,
            loggerMock.Object);

        // Act
        var result = await sut.GetInsurancesAsync("19800101-1234", CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("internal_error");

        // Should have logged the error
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
