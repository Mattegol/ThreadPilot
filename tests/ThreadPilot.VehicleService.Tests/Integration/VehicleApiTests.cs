using System.Net;
using System.Net.Http.Json;
using Shouldly;
using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.VehicleService.Tests.Integration;

public class VehicleApiTests : IClassFixture<VehicleApiFactory>
{
    private readonly HttpClient _client;

    public VehicleApiTests(VehicleApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("ABC123", "Volvo", "XC60", 2020)]
    [InlineData("XYZ987", "Tesla", "Model 3", 2022)]
    [InlineData("QWE456", "Toyota", "Corolla", 2018)]
    public async Task GetVehicle_WithValidRegistration_ReturnsOkWithCorrectData(
        string registration, string brand, string model, int year)
    {
        var response = await _client.GetAsync($"/api/vehicles/{registration}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var vehicle = await response.Content.ReadFromJsonAsync<VehicleDto>();
        vehicle.ShouldNotBeNull();
        vehicle!.RegistrationNumber.ShouldBe(registration);
        vehicle.Brand.ShouldBe(brand);
        vehicle.Model.ShouldBe(model);
        vehicle.Year.ShouldBe(year);
    }

    [Fact]
    public async Task GetVehicle_WithInvalidRegistration_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/vehicles/invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetVehicle_WithEmptyRegistration_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/vehicles/");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVehicle_WithLowercaseRegistration_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/vehicles/abc123");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetVehicle_WithUnknownRegistration_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/vehicles/ZZZ999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVehicle_ResponseContentType_IsApplicationJson()
    {
        var response = await _client.GetAsync("/api/vehicles/ABC123");

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task GetVehicle_WithValidRegistration_ReturnsValidJsonStructure()
    {
        var response = await _client.GetAsync("/api/vehicles/ABC123");
        var json = await response.Content.ReadAsStringAsync();

        json.ShouldContain("registrationNumber");
        json.ShouldContain("brand");
        json.ShouldContain("model");
        json.ShouldContain("year");
    }
}
