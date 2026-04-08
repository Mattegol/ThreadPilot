using Scalar.AspNetCore;
using ThreadPilot.ServiceDefaults.Middleware;
using ThreadPilot.Shared.Contracts;
using ThreadPilot.Shared.Results;
using ThreadPilot.VehicleService.Vehicles;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, resilience)
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
builder.Services.AddSingleton<IVehicleService, VehicleService>();

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

v1.MapGet("/vehicles/{registrationNumber}", (
    string registrationNumber,
    IVehicleService service) =>
{
    var result = service.GetVehicle(registrationNumber);
    return result.ToHttpResult();
})
.WithName("GetVehicleByRegistrationNumber")
.WithSummary("Get vehicle by registration number")
.WithDescription("Returns vehicle information for a given registration number.")
.Produces<VehicleDto>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError);

// Map health check endpoints
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
