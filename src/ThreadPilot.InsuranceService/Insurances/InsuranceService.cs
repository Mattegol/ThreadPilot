using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Insurances;

public sealed class InsuranceService : IInsuranceService
{
    private readonly IInsuranceRepository _repo;
    private readonly IInsurancePricingService _pricing;
    private readonly IVehicleClient _vehicleClient;
    private readonly ILogger<InsuranceService> _logger;

    public InsuranceService(
        IInsuranceRepository repo,
        IInsurancePricingService pricing,
        IVehicleClient vehicleClient,
        ILogger<InsuranceService> logger)
    {
        _repo = repo;
        _pricing = pricing;
        _vehicleClient = vehicleClient;
        _logger = logger;
    }

    public async Task<Result<InsuranceResponseDto>> GetInsurancesAsync(string personalNumber, CancellationToken ct)
    {
        _logger.LogInformation("Retrieving insurances for personal number {PersonalNumber}", personalNumber);

        var validation = PersonalNumberValidator.Validate(personalNumber);
        if (!validation.IsSuccess)
        {
            _logger.LogWarning("Invalid personal number format: {PersonalNumber}", personalNumber);
            return Result<InsuranceResponseDto>.Failure(validation.Error!);
        }

        try
        {
            var profile = _repo.GetByPersonalNumber(personalNumber);
            if (profile is null)
            {
                _logger.LogWarning("Insurance profile not found for personal number {PersonalNumber}", personalNumber);
                return Result<InsuranceResponseDto>.Failure(Errors.NotFound);
            }

            _logger.LogInformation("Found {ProductCount} insurance product(s) for {PersonalNumber}",
                profile.Products.Count, personalNumber);

            var insurances = new List<InsuranceDto>();

            foreach (var product in profile.Products)
            {
                var cost = _pricing.GetMonthlyCost(product);

                if (product == InsuranceType.Car)
                {
                    VehicleDto? vehicle = null;

                    if (!string.IsNullOrWhiteSpace(profile.CarRegistrationNumber))
                    {
                        _logger.LogDebug("Fetching vehicle data for registration {RegistrationNumber}",
                            profile.CarRegistrationNumber);

                        try
                        {
                            var vehicleResult = await _vehicleClient.GetVehicleAsync(profile.CarRegistrationNumber, ct);

                            // graceful degradation: still return car insurance even if vehicle service fails
                            if (vehicleResult.IsSuccess)
                            {
                                vehicle = vehicleResult.Value;
                                _logger.LogInformation("Successfully enriched car insurance with vehicle data for {RegistrationNumber}",
                                    profile.CarRegistrationNumber);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to fetch vehicle data for {RegistrationNumber}: {ErrorCode} - {ErrorMessage}",
                                    profile.CarRegistrationNumber, vehicleResult.Error?.Code, vehicleResult.Error?.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Unexpected error calling VehicleService for registration {RegistrationNumber}. Returning car insurance without vehicle data",
                                profile.CarRegistrationNumber);

                            // Graceful degradation: continue without vehicle data if call throws
                            vehicle = null;
                        }
                    }

                    insurances.Add(new InsuranceDto(product, cost, vehicle));
                }
                else
                {
                    insurances.Add(new InsuranceDto(product, cost, null));
                }
            }

            var total = insurances.Sum(x => x.MonthlyCost);

            _logger.LogInformation("Successfully retrieved {InsuranceCount} insurance(s) with total monthly cost {TotalCost:C} for {PersonalNumber}",
                insurances.Count, total, personalNumber);

            return Result<InsuranceResponseDto>.Success(new InsuranceResponseDto(
                profile.PersonalNumber,
                insurances,
                total
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error retrieving insurances for personal number {PersonalNumber}",
                personalNumber);

            return Result<InsuranceResponseDto>.Failure(
                new Error("internal_error", "An unexpected error occurred while retrieving insurances"));
        }
    }
}
