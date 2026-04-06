using System.Net;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Vehicles;

public sealed class VehicleClient : IVehicleClient
{
    private readonly HttpClient _httpClient;

    public VehicleClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<VehicleDto>> GetVehicleAsync(string registrationNumber, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync($"/api/vehicles/{registrationNumber}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<VehicleDto>.Failure(Errors.NotFound);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return Result<VehicleDto>.Failure(Errors.InvalidInput);

        if (!response.IsSuccessStatusCode)
            return Result<VehicleDto>.Failure(Errors.DependencyFailure);

        var dto = await response.Content.ReadFromJsonAsync<VehicleDto>(cancellationToken: ct);

        return dto is null
            ? Result<VehicleDto>.Failure(Errors.DependencyFailure)
            : Result<VehicleDto>.Success(dto);
    }
}
