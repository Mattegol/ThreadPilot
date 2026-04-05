using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.VehicleService.Vehicles;

public interface IVehicleService
{
    Result<VehicleDto> GetVehicle(string registrationNumber);
}