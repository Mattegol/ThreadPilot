# ThreadPilot

A .NET 10 microservices demonstration project built with .NET Aspire, showcasing modern distributed application development patterns, service orchestration, and best practices for error handling.

## 🎯 What is ThreadPilot?

ThreadPilot is a sample integration layer that connects a new core system with multiple legacy systems. It demonstrates how to build resilient, observable, and maintainable microservices using modern .NET development practices.

The application consists of two interconnected Minimal API services:

- **VehicleService**: Provides vehicle information lookup by registration number
- **InsuranceService**: Manages insurance products and enriches car insurance data with vehicle information

### Purpose

This project demonstrates:
- **Microservices Architecture**: Service decomposition and inter-service communication
- **.NET Aspire Integration**: Service orchestration, discovery, health checks, telemetry, and resilience
- **Result Pattern**: Functional error handling without exceptions
- **Problem Details (RFC 7807)**: Standardized HTTP error responses
- **Correlation IDs**: Distributed request tracing across services
- **Modern .NET Development**: Built on .NET 10 with C# 14
- **Graceful Degradation**: Services remain operational even when dependencies are unavailable

---

## 🏗️ Project Structure

```
ThreadPilot/
├── ThreadPilot.AppHost/              # .NET Aspire orchestration host
├── ThreadPilot.ServiceDefaults/      # Shared Aspire service defaults
├── src/
│   ├── ThreadPilot.VehicleService/   # Vehicle lookup microservice
│   ├── ThreadPilot.InsuranceService/ # Insurance management microservice
│   └── ThreadPilot.Shared/           # Shared contracts and patterns
└── tests/
    ├── ThreadPilot.VehicleService.Tests/
    └── ThreadPilot.InsuranceService.Tests/
```

### Services Overview

**VehicleService**
- Minimal API exposing `/api/vehicles/{registrationNumber}`
- In-memory repository simulating legacy system adapter
- Provides vehicle data for insurance enrichment

**InsuranceService**
- Minimal API exposing `/api/insurances/{personalNumber}`
- Integrates with VehicleService using typed HttpClient with resilience
- Returns insurance products and enriches car insurance with vehicle details

**ThreadPilot.Shared**
- Shared DTO contracts (records)
- Result pattern implementation (no exceptions for flow control)
- HTTP mapping extensions for Problem Details

**ThreadPilot.ServiceDefaults**
- Common service defaults for both APIs
- Service discovery, resilience, health checks, OpenTelemetry
- Correlation ID middleware

---

## 🚀 Getting Started for New Developers

### Prerequisites

Before you begin, ensure you have:

1. **Visual Studio 2026 or later** (recommended) OR **Visual Studio Code** with C# Dev Kit
2. **.NET 10 SDK** or later - [Download here](https://dotnet.microsoft.com/download)
3. **Docker Desktop** - Required for Aspire Dashboard and container support
   - [Download for Windows](https://www.docker.com/products/docker-desktop/)
   - Ensure Docker Desktop is running before starting the application

### Quick Start with .NET Aspire

.NET Aspire is a cloud-ready stack for building distributed applications. It provides:
- **Orchestration**: Runs all your services with a single command
- **Service Discovery**: Automatic service-to-service communication
- **Observability**: Built-in logging, tracing, and metrics
- **Resilience**: Automatic retries, timeouts, and circuit breakers

#### Option 1: Visual Studio (Recommended)

1. **Clone the repository**
   ```powershell
   git clone https://github.com/Mattegol/ThreadPilot.git
   cd ThreadPilot
   ```

2. **Open the solution**
   - Double-click `ThreadPilot.sln` or open it from Visual Studio

3. **Set the AppHost as startup project**
   - Right-click on `ThreadPilot.AppHost` in Solution Explorer
   - Select **"Set as Startup Project"**
   - The AppHost project orchestrates all services using Aspire

4. **Run the application**
   - Press **F5** or click the **"Start"** button
   - Visual Studio will:
     - Build all projects
     - Start the Aspire orchestrator
     - Launch all microservices
     - Open the Aspire Dashboard in your default browser

#### Option 2: Command Line

```powershell
# Restore dependencies (first time only)
dotnet restore

# Navigate to the AppHost project directory
cd ThreadPilot.AppHost

# Run the Aspire orchestrator
dotnet run

# The Aspire Dashboard URL will be displayed in the console
```

### Understanding the Aspire Dashboard

Once running, the **Aspire Dashboard** opens automatically at `http://localhost:15888` (default port).

The dashboard provides real-time insights into your distributed application:

**📊 Resources Tab**
- View all running services and their current status
- See service endpoints (URLs and ports)
- Monitor health check status
- View environment variables and configuration

**📝 Logs Tab**
- Centralized logging from all services
- Filter logs by service, severity, or correlation ID
- Real-time log streaming
- Search and highlight functionality

**🔍 Traces Tab**
- Distributed tracing across service boundaries
- See the complete request flow: `Client → InsuranceService → VehicleService`
- Identify performance bottlenecks
- View timing information for each operation

**📈 Metrics Tab**
- HTTP request rates and durations
- Error rates and status codes
- Custom application metrics
- Resource utilization

### Accessing the Services

When the application is running via Aspire:

1. **Open the Aspire Dashboard** (`http://localhost:15888`)
2. Navigate to the **Resources** tab
3. Find the service endpoint URLs (ports are dynamically assigned)

Example URLs (ports may vary):
- **VehicleService**: `http://localhost:5116`
- **InsuranceService**: `http://localhost:5275`

Each service provides interactive API documentation at `/scalar/v1`:
- VehicleService: `http://localhost:5116/scalar/v1`
- InsuranceService: `http://localhost:5275/scalar/v1`

### Making API Requests

**Get Vehicle Information:**
```powershell
# Replace <port> with actual port from Aspire Dashboard
curl http://localhost:<vehicleservice-port>/api/vehicles/ABC123
```

**Get Insurance Information:**
```powershell
# This will also fetch vehicle data if car insurance exists
curl http://localhost:<insuranceservice-port>/api/insurances/197001011234
```

### Development Workflow

1. **Make code changes** in any service
2. **Save the file** - Aspire hot reload automatically restarts affected services
3. **View impact** in the Aspire Dashboard:
   - Check logs for startup confirmation
   - Monitor health checks
   - Test endpoints using Scalar API docs
4. **Debug issues** using correlation IDs in traces and logs

### Troubleshooting

**Docker not running?**
```
Error: Docker is not running
Solution: Start Docker Desktop and ensure it's fully initialized
```

**Port conflicts?**
```
Error: Address already in use
Solution: Aspire uses dynamic port assignment, but if issues persist:
- Stop other applications using the same ports
- Restart the Aspire AppHost
```

**Services not communicating?**
- Check the Aspire Dashboard Resources tab - both services should show "Running"
- Verify health checks are passing (green status)
- Review logs for any startup errors
- Ensure service discovery is working (VehicleService should be resolvable as `https://vehicleservice`)

---

## 📋 Result Pattern & Error Handling

ThreadPilot implements the **Result pattern** for robust, explicit error handling without relying on exceptions for business logic flow.

### What is the Result Pattern?

The Result pattern is a functional programming approach that makes success and failure explicit in the type system. Instead of throwing exceptions for expected failures (like "not found" or "validation error"), we return a `Result` type that clearly indicates whether an operation succeeded or failed.

### Result Types

**Basic Result** (for operations without return values):
```csharp
public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    
    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
}
```

**Generic Result** (for operations that return data):
```csharp
public sealed class Result<T> : Result
{
    public T? Value { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}
```

### Using the Result Pattern

**In Service Layer:**
```csharp
public Result<VehicleDto> GetVehicle(string registrationNumber)
{
    // Validation
    if (string.IsNullOrWhiteSpace(registrationNumber))
        return Result<VehicleDto>.Failure(
            Errors.ValidationError("Registration number is required"));
    
    // Business logic
    var vehicle = _repository.Find(registrationNumber);
    
    if (vehicle is null)
        return Result<VehicleDto>.Failure(
            Errors.NotFound($"Vehicle with registration number '{registrationNumber}' was not found"));
    
    // Success case
    return Result<VehicleDto>.Success(new VehicleDto(vehicle));
}
```

**In API Endpoints:**
```csharp
app.MapGet("/api/vehicles/{registrationNumber}", (
    string registrationNumber,
    IVehicleService service) =>
{
    var result = service.GetVehicle(registrationNumber);
    return result.ToHttpResult(); // Automatically converts to HTTP response
})
```

### Benefits of the Result Pattern

✅ **Explicit Error Handling** - Callers must check `IsSuccess` and handle failures  
✅ **Type Safety** - Errors are part of the return type, not hidden exceptions  
✅ **No Hidden Control Flow** - No surprise exceptions for expected failures  
✅ **Better Performance** - Avoids expensive exception stack traces  
✅ **Easier Testing** - Test success and failure paths without exception handling  
✅ **Self-Documenting** - Method signature shows it can fail

### Error Types

Errors are immutable records defined in `ThreadPilot.Shared.Results`:

```csharp
public sealed record Error(string Code, string Message);
```

**Common Error Factories** (from `Errors` static class):
```csharp
public static class Errors
{
    public static Error ValidationError(string message) 
        => new("validation_error", message);
    
    public static Error NotFound(string message) 
        => new("not_found", message);
    
    public static Error InvalidInput(string message) 
        => new("invalid_input", message);
    
    public static Error DependencyFailure(string message) 
        => new("dependency_failure", message);
}
```

**Error Code to HTTP Status Mapping:**

| Error Code | HTTP Status | When to Use |
|------------|-------------|-------------|
| `invalid_input` | 400 Bad Request | Invalid or malformed input |
| `validation_error` | 400 Bad Request | Business rule validation failures |
| `not_found` | 404 Not Found | Resource doesn't exist |
| `dependency_failure` | 503 Service Unavailable | External service is down |
| *(unknown)* | 500 Internal Server Error | Unexpected errors |

---

## 🚨 Problem Details (RFC 7807)

ThreadPilot uses **RFC 7807 Problem Details** as the standard error response format across all APIs.

### What are Problem Details?

Problem Details is an internet standard (RFC 7807) that defines a consistent JSON format for HTTP API errors. This makes errors:
- **Machine-readable**: Clients can parse and handle errors programmatically
- **Human-readable**: Includes detailed error messages
- **Consistent**: Same format across all endpoints and services
- **Extensible**: Can include custom fields

### Automatic Conversion

Results are automatically converted to Problem Details using extension methods:

```csharp
public static class ResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return HttpResults.Ok(result.Value);
        
        return MapProblemDetails(result.Error);
    }
    
    private static IResult MapProblemDetails(Error? error)
    {
        // Maps error codes to HTTP status codes
        var (status, title) = error.Code switch
        {
            "invalid_input" => (400, "Invalid input"),
            "validation_error" => (400, "Validation error"),
            "not_found" => (404, "Not found"),
            "dependency_failure" => (503, "Dependency failure"),
            _ => (500, "Internal server error")
        };
        
        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = error.Message,
            Type = $"https://httpstatuses.com/{status}"
        };
        
        problem.Extensions["errorCode"] = error.Code;
        
        return HttpResults.Problem(problem);
    }
}
```

### Example Problem Details Response

**Request:**
```http
GET /api/vehicles/INVALID999
```

**Response (404 Not Found):**
```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not found",
  "status": 404,
  "detail": "Vehicle with registration number 'INVALID999' was not found",
  "errorCode": "not_found"
}
```

**Response Headers:**
```
Content-Type: application/problem+json
X-Correlation-Id: 7f8d9e2a-3b1c-4d5e-6f7a-8b9c0d1e2f3a
```

### Problem Details Fields

| Field | Description | Example |
|-------|-------------|---------|
| `type` | URI reference identifying the problem type | `https://httpstatuses.com/404` |
| `title` | Short, human-readable summary | `"Not found"` |
| `status` | HTTP status code | `404` |
| `detail` | Detailed explanation specific to this occurrence | `"Vehicle with registration number 'ABC123' was not found"` |
| `errorCode` | Application-specific error code (extension) | `"not_found"` |

### Benefits

✅ **Standardized Format** - Industry-standard error responses  
✅ **Machine & Human Readable** - Easy for both code and developers  
✅ **Consistent Across Services** - Same format in all microservices  
✅ **Extensible** - Add custom fields via `Extensions` dictionary  
✅ **Well-Documented** - RFC 7807 is widely understood  

---

## 🔗 Correlation IDs & Distributed Tracing

ThreadPilot implements **correlation ID tracking** to trace requests across service boundaries.

### What are Correlation IDs?

A correlation ID is a unique identifier attached to each incoming request. This ID flows through the entire request chain, allowing you to:
- Track a single request across multiple services
- Group all logs related to one request
- Debug complex inter-service flows
- Measure end-to-end latency

### Request Flow with Correlation IDs

```
Client Request
   ↓ [correlation-id: 7f8d9e2a-3b1c-4d5e-6f7a-8b9c0d1e2f3a]
InsuranceService
   ↓ [propagates same correlation-id]
VehicleService
   ↓ [uses same correlation-id]
Response flows back with same ID
```

### How It Works

**1. Correlation ID Middleware** (applied to both services):
```csharp
app.UseCorrelationId(); // Automatically generates or extracts correlation ID
```

**2. HTTP Header Propagation:**
- **Request Header**: `X-Correlation-Id` (if provided by client)
- **Response Header**: `X-Correlation-Id` (always returned)

**3. Logging Integration:**
All logs include the correlation ID, making it easy to filter:
```
[7f8d9e2a] InsuranceService: Received request for personal number 197001011234
[7f8d9e2a] InsuranceService: Calling VehicleService for registration ABC123
[7f8d9e2a] VehicleService: Received request for registration ABC123
[7f8d9e2a] VehicleService: Vehicle found
[7f8d9e2a] InsuranceService: Enriched car insurance with vehicle data
```

### Using Correlation IDs

**In the Aspire Dashboard:**
1. Go to the **Traces** tab
2. Click on any trace to see the complete request flow
3. View timing for each service call
4. Identify bottlenecks and failures

**In Logs:**
1. Go to the **Logs** tab
2. Filter by correlation ID to see all related log entries
3. Follow the request flow across services

**For Debugging:**
1. Get the correlation ID from the response headers
2. Search logs using that ID
3. See the complete story of what happened

### Graceful Degradation Example

When VehicleService is unavailable:

```csharp
// InsuranceService still returns data, but without vehicle details
var result = await _vehicleClient.GetVehicleAsync(registrationNumber, ct);
if (result.IsSuccess)
{
    carInsurance.Vehicle = result.Value; // Enrich with vehicle data
}
// If VehicleService fails, Vehicle remains null - graceful degradation
```

**Logs with Correlation ID:**
```
[abc-123] InsuranceService: Attempting to fetch vehicle for ABC123
[abc-123] VehicleService: Connection timeout
[abc-123] InsuranceService: VehicleService unavailable, returning car insurance without vehicle data
```

---

## 🎯 Endpoints

### VehicleService

**Get Vehicle by Registration Number**
```
GET /api/vehicles/{registrationNumber}
```

**Example Request:**
```powershell
curl http://localhost:5116/api/vehicles/ABC123
```

**Success Response (200 OK):**
```json
{
  "registrationNumber": "ABC123",
  "make": "Volvo",
  "model": "XC90",
  "year": 2023
}
```

**Error Response (404 Not Found):**
```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not found",
  "status": 404,
  "detail": "Vehicle with registration number 'ABC123' was not found",
  "errorCode": "not_found"
}
```

---

### InsuranceService

**Get Insurances by Personal Number**
```
GET /api/insurances/{personalNumber}
```

**Example Request:**
```powershell
curl http://localhost:5275/api/insurances/197001011234
```

**Success Response (200 OK):**
```json
{
  "insurances": [
    {
      "type": "Home",
      "monthlyPrice": 450.00,
      "vehicle": null
    },
    {
      "type": "Car",
      "monthlyPrice": 850.00,
      "vehicle": {
        "registrationNumber": "ABC123",
        "make": "Volvo",
        "model": "XC90",
        "year": 2023
      }
    }
  ],
  "totalMonthlyPrice": 1300.00
}
```

**Note**: If VehicleService is unavailable, car insurance is still returned but `vehicle` will be `null` (graceful degradation).

---

## 🧪 Testing

Run tests using Visual Studio Test Explorer or the command line:

```powershell
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/ThreadPilot.VehicleService.Tests

# Run tests with detailed output
dotnet test --verbosity detailed
```

---

## 🏛️ Design Decisions

### Minimal APIs
Minimal APIs (instead of controllers) keep services lightweight, focused, and easier to understand for microservices.

### In-Memory Repositories
Repositories use in-memory storage for demonstration. They can easily be replaced with:
- Database integrations (Entity Framework Core, Dapper)
- HTTP clients to external systems
- Message bus integrations (Azure Service Bus, RabbitMQ)

### Typed HttpClient with Resilience
InsuranceService uses a typed HttpClient (`IVehicleClient`) with:
- **Service Discovery**: Resolves `https://vehicleservice` via Aspire
- **Standard Resilience Handler**: Automatic retries, timeouts, circuit breakers
- **Correlation ID Propagation**: Passes correlation IDs across service calls

### Scalar API Documentation
Scalar provides a modern, interactive API reference (instead of Swagger UI) with:
- Better UX for testing endpoints
- Automatic code generation
- Dark mode support

---

## 📚 Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Result Pattern Explained](https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/)
- [RFC 7807 - Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807)
- [Scalar API Documentation](https://github.com/scalar/scalar)
- [Minimal APIs in .NET](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)

---

## 🤝 Contributing

When contributing to ThreadPilot:

1. **Follow the Result Pattern** - No exceptions for expected failures
2. **Use Problem Details** - RFC 7807 format for all error responses
3. **Propagate Correlation IDs** - For new inter-service HTTP calls
4. **Add Unit Tests** - Cover both success and failure scenarios
5. **Update API Documentation** - Use `.WithSummary()` and `.WithDescription()` on endpoints
6. **Follow Existing Patterns** - Consistency is key

---

## 📄 License

This is a demonstration project for educational purposes.

---

## 🚀 Running Without Aspire (Optional)

If you prefer not to use Aspire, you can still run services individually:

### Start VehicleService
```powershell
dotnet run --project src/ThreadPilot.VehicleService
```

### Start InsuranceService
```powershell
# Set VehicleService URL
$env:VehicleService__BaseUrl = "https://localhost:5001"

# Run InsuranceService
dotnet run --project src/ThreadPilot.InsuranceService
```

> **Note**: Exact local ports may differ based on launch settings. Check the console output for actual URLs.
