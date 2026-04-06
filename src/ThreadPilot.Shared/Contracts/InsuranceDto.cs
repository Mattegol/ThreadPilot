namespace ThreadPilot.Shared.Contracts;

public sealed record InsuranceDto(
    InsuranceType Type,
    int MonthlyCost,
    VehicleDto? Vehicle
);
