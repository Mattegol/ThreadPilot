namespace ThreadPilot.InsuranceService.Insurances;

public interface IInsuranceRepository
{
    InsuranceProfileEntity? GetByPersonalNumber(string personalNumber);
}
