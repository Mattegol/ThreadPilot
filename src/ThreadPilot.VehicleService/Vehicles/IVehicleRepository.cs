namespace ThreadPilot.VehicleService.Vehicles;

public interface IVehicleRepository
{
    VehicleEntity? GetByRegistrationNumber(string registrationNumber);
}
