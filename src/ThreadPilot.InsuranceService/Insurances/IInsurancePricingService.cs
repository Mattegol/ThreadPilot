using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.InsuranceService.Insurances;

public interface IInsurancePricingService
{
    int GetMonthlyCost(InsuranceType type);
}