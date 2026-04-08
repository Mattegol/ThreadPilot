using Scalar.AspNetCore;
using ThreadPilot.InsuranceService.Insurances;
using ThreadPilot.InsuranceService.Vehicles;
using ThreadPilot.ServiceDefaults.Middleware;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, resilience)
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IInsuranceRepository, InMemoryInsuranceRepository>();
builder.Services.AddSingleton<IInsurancePricingService, InsurancePricingService>();
builder.Services.AddSingleton<IInsuranceService, InsuranceService>();

// HttpClient with service discovery and resilience (configured by ServiceDefaults)
builder.Services.AddHttpClient<IVehicleClient, VehicleClient>(client =>
{
    var baseUrl = builder.Configuration["VehicleService__BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    else
    {
        // Use service discovery when running under Aspire
        client.BaseAddress = new Uri("https://vehicleservice");
    }
});
// Note: AddStandardResilienceHandler is now added by ServiceDefaults.ConfigureHttpClientDefaults

var app = builder.Build();

// Redirect root to Scalar API docs
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .ExcludeFromDescription();

// Add correlation ID middleware
app.UseCorrelationId();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// ============================================
// V1 API Endpoints
// ============================================
var v1 = app.MapGroup("/api/v1")
    .WithTags("V1");

v1.MapGet("/insurances/{personalNumber}", async (
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

// Map health check endpoints
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
