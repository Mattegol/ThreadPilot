var builder = DistributedApplication.CreateBuilder(args);

// Add services
var vehicleService = builder.AddProject<Projects.ThreadPilot_VehicleService>("vehicleservice")
    .WithHttpsEndpoint(port: 5001, name: "https");

var insuranceService = builder.AddProject<Projects.ThreadPilot_InsuranceService>("insuranceservice")
    .WithHttpsEndpoint(port: 5002, name: "https")
    .WithReference(vehicleService)
    .WithEnvironment("VehicleService__BaseUrl", vehicleService.GetEndpoint("https"));

builder.Build().Run();
