using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.InsuranceService.Insurances;

public sealed class InMemoryInsuranceRepository : IInsuranceRepository
{
    private static readonly Dictionary<string, InsuranceProfileEntity> Profiles = new()
    {
        ["19800101-1234"] = new InsuranceProfileEntity(
            "19800101-1234",
            new List<InsuranceType> { InsuranceType.Pet, InsuranceType.Car },
            "ABC123"
        ),
        ["19951212-9999"] = new InsuranceProfileEntity(
            "19951212-9999",
            new List<InsuranceType> { InsuranceType.Health },
            null
        ),
        ["19770505-1111"] = new InsuranceProfileEntity(
            "19770505-1111",
            new List<InsuranceType> { InsuranceType.Pet, InsuranceType.Health, InsuranceType.Car },
            "XYZ987"
        )
    };

    public InsuranceProfileEntity? GetByPersonalNumber(string personalNumber)
    {
        Profiles.TryGetValue(personalNumber, out var profile);
        return profile;
    }
}