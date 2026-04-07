var builder = DistributedApplication.CreateBuilder(args);

// Add VehicleService first
var vehicleService = builder.AddProject<Projects.ThreadPilot_VehicleService>("vehicleservice");

// Add InsuranceService with dependency - waits for VehicleService to be healthy
var insuranceService = builder.AddProject<Projects.ThreadPilot_InsuranceService>("insuranceservice")
    .WithReference(vehicleService)
    .WaitFor(vehicleService);

builder.Build().Run();
