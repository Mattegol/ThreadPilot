using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.InsuranceService.Insurances;

public sealed class InsurancePricingService : IInsurancePricingService
{
    public int GetMonthlyCost(InsuranceType type) => type switch
    {
        InsuranceType.Pet => 10,
        InsuranceType.Health => 20,
        InsuranceType.Car => 30,
        _ => 0
    };
}