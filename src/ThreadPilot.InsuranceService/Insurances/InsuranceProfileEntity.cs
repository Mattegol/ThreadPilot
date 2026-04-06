using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.InsuranceService.Insurances;

public sealed record InsuranceProfileEntity(
    string PersonalNumber,
    List<InsuranceType> Products,
    string? CarRegistrationNumber
);
