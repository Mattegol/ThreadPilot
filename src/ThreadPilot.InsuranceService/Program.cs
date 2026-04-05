using Scalar.AspNetCore;
using ThreadPilot.InsuranceService.Insurances;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Results;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Fixed port
builder.WebHost.UseUrls("https://localhost:5002");

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IInsuranceRepository, InMemoryInsuranceRepository>();
builder.Services.AddSingleton<IInsurancePricingService, InsurancePricingService>();
builder.Services.AddSingleton<IInsuranceService, InsuranceService>();

builder.Services.AddHttpClient<IVehicleClient, VehicleClient>(client =>
{
    var baseUrl = builder.Configuration["VehicleService:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/api/insurances/{personalNumber}", async (
    string personalNumber,
    IInsuranceService service,
    CancellationToken ct) =>
{
    var result = await service.GetInsurancesAsync(personalNumber, ct);
    return result.ToHttpResult();
})
.WithName("GetInsurancesByPersonalNumber");

app.Run();

public partial class Program { }