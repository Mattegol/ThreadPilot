using Scalar.AspNetCore;
using ThreadPilot.Shared.Results;
using ThreadPilot.VehicleService.Vehicles;

var builder = WebApplication.CreateBuilder(args);

// Fixed port
builder.WebHost.UseUrls("https://localhost:5001");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IVehicleRepository, InMemoryVehicleRepository>();
builder.Services.AddSingleton<IVehicleService, VehicleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();


// TODO Versioning
app.MapGet("/api/vehicles/{registrationNumber}", (string registrationNumber, IVehicleService service) =>
{
    var result = service.GetVehicle(registrationNumber);
    return result.ToHttpResult();
})
.WithName("GetVehicleByRegistrationNumber");

app.Run();

public partial class Program { }