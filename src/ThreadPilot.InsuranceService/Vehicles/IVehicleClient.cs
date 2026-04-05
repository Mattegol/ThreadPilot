using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Vehicles;

public interface IVehicleClient
{
    Task<Result<VehicleDto>> GetVehicleAsync(string registrationNumber, CancellationToken ct);
}