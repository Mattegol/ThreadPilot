using Microsoft.Extensions.Logging;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.VehicleService.Vehicles;

public sealed class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(IVehicleRepository repo, ILogger<VehicleService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Result<VehicleDto> GetVehicle(string registrationNumber)
    {
        _logger.LogInformation("Retrieving vehicle for registration number {RegistrationNumber}", registrationNumber);

        var validation = VehicleValidator.ValidateRegistrationNumber(registrationNumber);
        if (!validation.IsSuccess)
        {
            _logger.LogWarning("Invalid registration number format: {RegistrationNumber}", registrationNumber);
            return Result<VehicleDto>.Failure(validation.Error!);
        }

        try
        {
            var entity = _repo.GetByRegistrationNumber(registrationNumber);
            if (entity is null)
            {
                _logger.LogWarning("Vehicle not found for registration number {RegistrationNumber}", registrationNumber);
                return Result<VehicleDto>.Failure(Errors.NotFound);
            }

            _logger.LogInformation("Successfully retrieved vehicle {RegistrationNumber} - {Brand} {Model}", 
                entity.RegistrationNumber, entity.Brand, entity.Model);

            return Result<VehicleDto>.Success(new VehicleDto(
                entity.RegistrationNumber,
                entity.Brand,
                entity.Model,
                entity.Year
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error retrieving vehicle for registration number {RegistrationNumber}", 
                registrationNumber);

            return Result<VehicleDto>.Failure(
                new Error("internal_error", "An unexpected error occurred while retrieving vehicle data"));
        }
    }
}
