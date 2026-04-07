using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using ThreadPilot.VehicleService.Vehicles;

namespace ThreadPilot.VehicleService.Tests;

public class VehicleServiceExceptionTests
{
    [Fact]
    public void GetVehicle_WhenRepositoryThrowsException_ReturnsInternalError()
    {
        // Arrange: Create mock repository that throws
        var repoMock = new Mock<IVehicleRepository>();
        repoMock
            .Setup(x => x.GetByRegistrationNumber(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Database connection failed"));

        var loggerMock = new Mock<ILogger<Vehicles.VehicleService>>();

        var sut = new Vehicles.VehicleService(repoMock.Object, loggerMock.Object);

        // Act
        var result = sut.GetVehicle("ABC123");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe("internal_error");
        result.Error.Message.ShouldBe("An unexpected error occurred while retrieving vehicle data");

        // Verify exception was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error retrieving vehicle")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetVehicle_WhenRepositoryThrowsNullReference_ReturnsInternalError()
    {
        // Arrange
        var repoMock = new Mock<IVehicleRepository>();
        repoMock
            .Setup(x => x.GetByRegistrationNumber(It.IsAny<string>()))
            .Throws(new NullReferenceException("Unexpected null reference"));

        var loggerMock = new Mock<ILogger<Vehicles.VehicleService>>();
        var sut = new Vehicles.VehicleService(repoMock.Object, loggerMock.Object);

        // Act
        var result = sut.GetVehicle("ABC123");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("internal_error");

        // Verify NullReferenceException was logged
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<NullReferenceException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetVehicle_WhenRepositoryThrowsTimeout_ReturnsInternalError()
    {
        // Arrange
        var repoMock = new Mock<IVehicleRepository>();
        repoMock
            .Setup(x => x.GetByRegistrationNumber("ABC123"))
            .Throws(new TimeoutException("Database timeout"));

        var loggerMock = new Mock<ILogger<Vehicles.VehicleService>>();
        var sut = new Vehicles.VehicleService(repoMock.Object, loggerMock.Object);

        // Act
        var result = sut.GetVehicle("ABC123");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("internal_error");

        // Verify registration number was logged with exception
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ABC123")),
                It.IsAny<TimeoutException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetVehicle_WhenExceptionOccurs_DoesNotThrowException()
    {
        // Arrange
        var repoMock = new Mock<IVehicleRepository>();
        repoMock
            .Setup(x => x.GetByRegistrationNumber(It.IsAny<string>()))
            .Throws(new Exception("Catastrophic failure"));

        var loggerMock = new Mock<ILogger<Vehicles.VehicleService>>();
        var sut = new Vehicles.VehicleService(repoMock.Object, loggerMock.Object);

        // Act & Assert: Should not throw, should return Result.Failure
        Should.NotThrow(() => sut.GetVehicle("ABC123"));

        var result = sut.GetVehicle("ABC123");
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("internal_error");
    }

    [Fact]
    public void GetVehicle_AfterException_CanStillProcessValidRequests()
    {
        // Arrange
        var repo = new InMemoryVehicleRepository();
        var loggerMock = new Mock<ILogger<Vehicles.VehicleService>>();
        var sut = new Vehicles.VehicleService(repo, loggerMock.Object);

        // Act: Get a valid vehicle to verify service still works after potential errors
        var result = sut.GetVehicle("ABC123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.RegistrationNumber.ShouldBe("ABC123");
    }
}
