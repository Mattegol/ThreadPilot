using Microsoft.Extensions.Http.Resilience;
using Scalar.AspNetCore;
using ThreadPilot.InsuranceService.Insurances;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

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
    options.Retry.MaxRetryAttempts = builder.Configuration.GetValue<int>("HttpClient:MaxRetryAttempts");
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("HttpClient:AttemptTimeoutSeconds"));
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("HttpClient:TotalTimeoutSeconds"));
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
.WithName("GetInsurancesByPersonalNumber")
.WithSummary("Get insurance products by personal number")
.WithDescription("Returns all insurance products and monthly costs for a person. If car insurance exists, vehicle information is included when available.")
.Produces<InsuranceResponseDto>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.Run();

public partial class Program { }