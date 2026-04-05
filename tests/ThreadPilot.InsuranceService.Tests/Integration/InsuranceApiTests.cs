using Shouldly;
using System.Net;
using System.Net.Http.Json;
using ThreadPilot.Shared.Contracts;

namespace ThreadPilot.InsuranceService.Tests.Integration;

public class InsuranceApiTests : IClassFixture<InsuranceApiFactory>
{
    private readonly HttpClient _client;

    public InsuranceApiTests(InsuranceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInsurances_WithValidPersonalNumber_ReturnsOkWithData()
    {
        var response = await _client.GetAsync("/api/insurances/19951212-9999");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<InsuranceResponseDto>();
        result.ShouldNotBeNull();
        result!.PersonalNumber.ShouldBe("19951212-9999");
        result.Insurances.ShouldNotBeEmpty();
        result.TotalMonthlyCost.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetInsurances_WithCarInsurance_IncludesVehicleData()
    {
        var response = await _client.GetAsync("/api/insurances/19800101-1234");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<InsuranceResponseDto>();
        result.ShouldNotBeNull();
        
        var carInsurance = result!.Insurances.FirstOrDefault(x => x.Type == InsuranceType.Car);
        carInsurance.ShouldNotBeNull();
        carInsurance!.Vehicle.ShouldNotBeNull();
        carInsurance.Vehicle!.RegistrationNumber.ShouldBe("ABC123");
    }

    [Fact]
    public async Task GetInsurances_WithMultipleInsurances_CalculatesTotalCostCorrectly()
    {
        var response = await _client.GetAsync("/api/insurances/19770505-1111");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<InsuranceResponseDto>();
        result.ShouldNotBeNull();
        result!.Insurances.Count.ShouldBe(3); // Pet, Health, Car
        result.TotalMonthlyCost.ShouldBe(60); // 20 + 20 + 20
    }

    [Fact]
    public async Task GetInsurances_WithInvalidPersonalNumber_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/insurances/invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetInsurances_WithUnknownPerson_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/insurances/19990101-9999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInsurances_ResponseContentType_IsApplicationJson()
    {
        var response = await _client.GetAsync("/api/insurances/19951212-9999");

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task GetInsurances_ReturnsValidJsonStructure()
    {
        var response = await _client.GetAsync("/api/insurances/19951212-9999");
        var json = await response.Content.ReadAsStringAsync();

        json.ShouldContain("personalNumber");
        json.ShouldContain("insurances");
        json.ShouldContain("totalMonthlyCost");
    }

    [Fact]
    public async Task GetInsurances_AllInsurancesHaveRequiredFields()
    {
        var response = await _client.GetAsync("/api/insurances/19770505-1111");
        var result = await response.Content.ReadFromJsonAsync<InsuranceResponseDto>();

        result.ShouldNotBeNull();
        result!.Insurances.ShouldAllBe(insurance =>
            insurance.Type != null &&
            insurance.MonthlyCost > 0);
    }

    [Fact]
    public async Task GetInsurances_WithMissingPersonalNumber_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/insurances/");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInsurances_WithWhitespacePersonalNumber_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/insurances/%20");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
