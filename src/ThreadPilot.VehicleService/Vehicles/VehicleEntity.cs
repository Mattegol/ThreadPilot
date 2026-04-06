namespace ThreadPilot.VehicleService.Vehicles;

public sealed record VehicleEntity(
    string RegistrationNumber,
    string Brand,
    string Model,
    int Year
);
