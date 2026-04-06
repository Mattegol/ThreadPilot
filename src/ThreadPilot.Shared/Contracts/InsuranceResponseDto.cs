namespace ThreadPilot.Shared.Contracts;

public sealed record InsuranceResponseDto(
    string PersonalNumber,
    IReadOnlyList<InsuranceDto> Insurances,
    int TotalMonthlyCost
);
