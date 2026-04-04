namespace ThreadPilot.Shared.Contracts;

public sealed record VehicleDto(
    string RegistrationNumber,
    string Brand,
    string Model,
    int Year
);