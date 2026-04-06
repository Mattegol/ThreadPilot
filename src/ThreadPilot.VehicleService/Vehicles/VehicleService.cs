using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.VehicleService.Vehicles;

public sealed class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;

    public VehicleService(IVehicleRepository repo)
    {
        _repo = repo;
    }

    public Result<VehicleDto> GetVehicle(string registrationNumber)
    {
        var validation = VehicleValidator.ValidateRegistrationNumber(registrationNumber);
        if (!validation.IsSuccess)
            return Result<VehicleDto>.Failure(validation.Error!);

        var entity = _repo.GetByRegistrationNumber(registrationNumber);
        if (entity is null)
            return Result<VehicleDto>.Failure(Errors.NotFound);

        return Result<VehicleDto>.Success(new VehicleDto(
            entity.RegistrationNumber,
            entity.Brand,
            entity.Model,
            entity.Year
        ));
    }
}
