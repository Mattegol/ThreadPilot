namespace ThreadPilot.VehicleService.Vehicles;

public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private static readonly Dictionary<string, VehicleEntity> Vehicles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ABC123"] = new VehicleEntity("ABC123", "Volvo", "XC60", 2020),
        ["XYZ987"] = new VehicleEntity("XYZ987", "Tesla", "Model 3", 2022),
        ["QWE456"] = new VehicleEntity("QWE456", "Toyota", "Corolla", 2018)
    };

    public VehicleEntity? GetByRegistrationNumber(string registrationNumber)
    {
        Vehicles.TryGetValue(registrationNumber, out var vehicle);
        return vehicle;
    }
}