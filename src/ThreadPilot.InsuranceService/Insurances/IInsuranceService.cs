using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Insurances;

public interface IInsuranceService
{
    Task<Result<InsuranceResponseDto>> GetInsurancesAsync(string personalNumber, CancellationToken ct);
}