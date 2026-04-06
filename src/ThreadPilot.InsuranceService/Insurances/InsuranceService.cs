using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Insurances;

public sealed class InsuranceService : IInsuranceService
{
    private readonly IInsuranceRepository _repo;
    private readonly IInsurancePricingService _pricing;
    private readonly IVehicleClient _vehicleClient;

    public InsuranceService(
        IInsuranceRepository repo,
        IInsurancePricingService pricing,
        IVehicleClient vehicleClient)
    {
        _repo = repo;
        _pricing = pricing;
        _vehicleClient = vehicleClient;
    }

    public async Task<Result<InsuranceResponseDto>> GetInsurancesAsync(string personalNumber, CancellationToken ct)
    {
        var validation = PersonalNumberValidator.Validate(personalNumber);
        if (!validation.IsSuccess)
            return Result<InsuranceResponseDto>.Failure(validation.Error!);

        var profile = _repo.GetByPersonalNumber(personalNumber);
        if (profile is null)
            return Result<InsuranceResponseDto>.Failure(Errors.NotFound);

        var insurances = new List<InsuranceDto>();

        foreach (var product in profile.Products)
        {
            var cost = _pricing.GetMonthlyCost(product);

            if (product == InsuranceType.Car)
            {
                VehicleDto? vehicle = null;

                if (!string.IsNullOrWhiteSpace(profile.CarRegistrationNumber))
                {
                    var vehicleResult = await _vehicleClient.GetVehicleAsync(profile.CarRegistrationNumber, ct);

                    // graceful degradation: still return car insurance even if vehicle service fails
                    if (vehicleResult.IsSuccess)
                        vehicle = vehicleResult.Value;
                }

                insurances.Add(new InsuranceDto(product, cost, vehicle));
            }
            else
            {
                insurances.Add(new InsuranceDto(product, cost, null));
            }
        }

        var total = insurances.Sum(x => x.MonthlyCost);

        return Result<InsuranceResponseDto>.Success(new InsuranceResponseDto(
            profile.PersonalNumber,
            insurances,
            total
        ));
    }
}
