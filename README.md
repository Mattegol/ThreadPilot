# ThreadPilot

A demonstration microservices application built with .NET Aspire, showcasing modern cloud-native patterns and best practices.

## Project Overview

ThreadPilot is a sample system that demonstrates:
- **Microservices Architecture**: Two independent services (Vehicle Service and Insurance Service) with HTTP-based communication
- **Graceful Degradation**: Services continue to function even when dependencies are unavailable
- **Result Pattern**: Functional error handling without throwing exceptions for business logic
- **Standardized Error Responses**: RFC 7807 Problem Details for consistent HTTP error responses
- **Observability**: Built-in health checks, telemetry, and distributed tracing with .NET Aspire
- **API Versioning**: URL path versioning strategy for stable, evolvable APIs

### Services

#### Vehicle Service (`ThreadPilot.VehicleService`)
RESTful API for vehicle information lookup by registration number.

**Endpoints:**
- `GET /api/v1/vehicles/{registrationNumber}` - Retrieve vehicle details

**Business Rules:**
- Registration numbers must be uppercase and 6 characters
- Returns 404 if vehicle not found
- Returns 400 for invalid registration format

#### Insurance Service (`ThreadPilot.InsuranceService`)
RESTful API for insurance policy lookup by personal number, with optional vehicle enrichment.

**Endpoints:**
- `GET /api/v1/insurances/{personalNumber}` - Retrieve insurance policies

**Business Rules:**
- Personal numbers must match Swedish format (YYYYMMDD-XXXX)
- Enriches car insurance policies with vehicle data when available
- Continues without vehicle data if Vehicle Service is unavailable (graceful degradation)
- Returns 404 if no policies found
- Returns 400 for invalid personal number format

## Getting Started with .NET Aspire

### Prerequisites

1. **.NET 10 SDK or later** - [Download here](https://dotnet.microsoft.com/download)
2. **Visual Studio 2026 17.14 or later** (or Visual Studio Code with C# Dev Kit)
3. **.NET Aspire workload**:
   ```bash
   dotnet workload install aspire
   ```

### Running the Application

#### Option 1: Visual Studio
1. Open `ThreadPilot.sln`
2. Set `ThreadPilot.AppHost` as the startup project
3. Press **F5** to run with debugging (or **Ctrl+F5** without debugging)
4. The Aspire Dashboard will open automatically in your browser

#### Option 2: Command Line
```bash
cd ThreadPilot.AppHost
dotnet run
```

### Aspire Dashboard

When you run the application, the Aspire Dashboard launches automatically. It provides:

- **Resources View**: See all running services, their health status, and endpoints
- **Logs**: Unified logging across all services with filtering and search
- **Traces**: Distributed tracing to follow requests across services
- **Metrics**: Performance metrics and telemetry data

**Default URLs:**
- **Aspire Dashboard**: `http://localhost:15888` or `https://localhost:17251`
- **Vehicle Service API**: Check the Aspire Dashboard Resources tab for the dynamically assigned port
- **Insurance Service API**: Check the Aspire Dashboard Resources tab for the dynamically assigned port

### Exploring the APIs

Each service exposes its API documentation through **Scalar** (modern OpenAPI UI):

1. Start the application via Aspire
2. Open the Aspire Dashboard
3. In the **Resources** view, click the endpoint link for either service
4. Add `/scalar/v1` to the URL to view interactive API documentation
5. Try out the endpoints directly from the Scalar UI

**Example requests:**
```bash
# Get vehicle information
GET https://localhost:{port}/api/v1/vehicles/ABC123

# Get insurance policies
GET https://localhost:{port}/api/v1/insurances/19770505-1111
```

### Quick Start for New Developers

If you're new to the project, here's a recommended learning path:

1. **Read the Architecture**: Review the "Project Overview" and "Error Handling & Result Pattern" sections to understand the design principles
2. **Run the Application**: Start with Visual Studio (Option 1) to see everything working end-to-end
3. **Explore the Aspire Dashboard**: Familiarize yourself with the Resources, Logs, and Traces views
4. **Test the APIs**: Use Scalar UI (`/scalar/v1`) to make test requests and understand the endpoints
5. **Review the Code**: Start with `Program.cs` files to see endpoint definitions, then dive into service classes
6. **Run the Tests**: Execute `dotnet test` to see comprehensive test coverage and understand expected behaviors
7. **Make a Small Change**: Try adding a new field to the Vehicle model to practice the full development cycle

**Key Concepts to Understand:**
- **Result Pattern**: How we handle errors without throwing exceptions
- **Problem Details**: How HTTP errors are structured
- **Graceful Degradation**: How InsuranceService continues working when VehicleService is unavailable
- **API Versioning**: How we maintain backward compatibility with `/api/v1/` URLs

## Error Handling & Result Pattern

ThreadPilot demonstrates modern error handling practices using the **Result Pattern** and **RFC 7807 Problem Details**.

### Result Pattern

Instead of throwing exceptions for business logic failures, services return a `Result<T>` type that encapsulates success or failure:

```csharp
public record Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
}

public record Error(string Code, string Message);
```

**Benefits:**
- ? Explicit error handling - forces consumers to handle failures
- ? No hidden control flow - no exceptions for expected failures
- ? Performance - no exception overhead for business validation failures
- ? Composable - easy to chain operations and transform results

**Example:**
```csharp
public Result<Vehicle> GetVehicle(string registrationNumber)
{
    if (string.IsNullOrWhiteSpace(registrationNumber))
        return Result<Vehicle>.Failure(Errors.InvalidRegistrationNumber);

    var vehicle = _repository.GetByRegistrationNumber(registrationNumber);
    
    return vehicle is null 
        ? Result<Vehicle>.Failure(Errors.VehicleNotFound)
        : Result<Vehicle>.Success(vehicle);
}
```

### Exception Handling

**Exceptions are reserved for truly unexpected scenarios** (database failures, network issues, bugs). When exceptions occur:

1. **Try-Catch at Service Layer**: Each service method wraps repository/external calls in try-catch
2. **Structured Logging**: All exceptions are logged with `ILogger<T>` including context
3. **Return Internal Error**: Convert exception to `Result.Failure` with generic `internal_error` code
4. **No Exception Propagation**: Services never throw exceptions to controllers/endpoints

**Example:**
```csharp
try
{
    var vehicle = _repository.GetByRegistrationNumber(registrationNumber);
    // ... business logic
}
catch (Exception ex)
{
    _logger.LogError(ex, 
        \"Unexpected error retrieving vehicle {RegistrationNumber}\", 
        registrationNumber);
    
    return Result<Vehicle>.Failure(Errors.InternalError);
}
```

### Problem Details (RFC 7807)

All HTTP error responses follow the **Problem Details** standard for consistent, machine-readable error format:

```json
{
  \"type\": \"https://tools.ietf.org/html/rfc9110#section-15.5.1\",
  \"title\": \"Bad Request\",
  \"status\": 400,
  \"detail\": \"Invalid registration number format. Expected uppercase, 6 characters.\",
  \"instance\": \"/api/v1/vehicles/abc123\",
  \"error_code\": \"invalid_registration_number\"
}
```

**Error Code Mapping:**
| Error Code                     | HTTP Status | Description                                    |
|--------------------------------|-------------|------------------------------------------------|
| `invalid_registration_number`  | 400         | Registration number format is invalid          |
| `invalid_personal_number`      | 400         | Personal number format is invalid              |
| `vehicle_not_found`            | 404         | No vehicle found for registration number       |
| `insurances_not_found`         | 404         | No insurance policies found for personal number|
| `internal_error`               | 500         | Unexpected server error occurred               |

**Implementation:**

The `ResultHttpExtensions` class automatically converts `Result<T>` to HTTP responses with Problem Details:

```csharp
app.MapGet(\"/api/v1/vehicles/{registrationNumber}\", 
    (string registrationNumber, IVehicleService service) =>
{
    var result = service.GetVehicle(registrationNumber);
    return result.ToHttpResult(); // Automatically converts to Problem Details on failure
});
```

## API Versioning Strategy

ThreadPilot uses **URL Path Versioning** to provide stable, evolvable APIs.

### Current Version: v1

All endpoints are currently version 1, accessible under the `/api/v1` prefix:

- Vehicle Service: `/api/v1/vehicles/{registrationNumber}`
- Insurance Service: `/api/v1/insurances/{personalNumber}`

### Why URL Path Versioning?

We chose URL path versioning (`/api/v1/`) over other approaches because:

? **Explicit and Obvious**: Version is visible in the URL, making it clear which API version clients are using  
? **Easy Discovery**: Different versions can be documented separately in OpenAPI/Scalar  
? **Routing Simplicity**: .NET Minimal APIs handle routing cleanly with `MapGroup`  
? **Service Independence**: Each microservice can version independently  
? **Client Control**: Clients explicitly choose their version and can test new versions before migrating  

### Versioning Guidelines

#### Breaking Changes Require New Version

A new major version (v2, v3, etc.) is required when making **breaking changes**:

- ? Removing or renaming endpoints
- ? Removing or renaming request/response fields
- ? Changing field data types (e.g., string ? int)
- ? Adding required request parameters
- ? Changing error codes or status codes
- ? Modifying business logic that affects existing behavior

#### Non-Breaking Changes Stay in Current Version

These changes can be added to the current version:

- ? Adding new endpoints
- ? Adding optional request parameters (with defaults)
- ? Adding new fields to responses (clients ignore unknown fields)
- ? Fixing bugs that don't change contract behavior
- ? Performance improvements

### Implementing a New Version

When you need to introduce v2:

1. **Create New Endpoint Group**:
   ```csharp
   var v2 = app.MapGroup(\"/api/v2\")
       .WithTags(\"V2\")
       .WithOpenApi();
   
   v2.MapGet(\"/vehicles/{registrationNumber}\", ...)
   ```

2. **Maintain v1 Alongside v2**:
   ```csharp
   // v1 continues to work
   var v1 = app.MapGroup(\"/api/v1\").WithTags(\"V1\").WithOpenApi();
   v1.MapGet(\"/vehicles/{registrationNumber}\", ...);
   
   // v2 introduces breaking changes
   var v2 = app.MapGroup(\"/api/v2\").WithTags(\"V2\").WithOpenApi();
   v2.MapGet(\"/vehicles/{registrationNumber}\", ...);
   ```

3. **Deprecation Strategy**:
   - Announce v1 deprecation with clear timeline
   - Support both versions during migration period (typically 6-12 months)
   - Eventually remove v1 after all clients have migrated

### Version Discovery

Clients can discover available versions through:

- **Scalar UI**: Each version appears as a separate tag (`V1`, `V2`) in the API documentation
- **OpenAPI Spec**: Accessible at `/openapi/v1.json` (and `/openapi/v2.json` when available)
- **Aspire Dashboard**: Shows all service endpoints with their version prefixes

### Current Implementation

The version is defined using ASP.NET Core's `MapGroup` feature:

```csharp
// VehicleService/Program.cs
var v1 = app.MapGroup(\"/api/v1\")
    .WithTags(\"V1\")
    .WithOpenApi();

v1.MapGet(\"/vehicles/{registrationNumber}\", 
    (string registrationNumber, IVehicleService service) =>
    {
        var result = service.GetVehicle(registrationNumber);
        return result.ToHttpResult();
    })
    .WithName(\"GetVehicle\")
    .WithOpenApi();
```

This approach keeps version management clean and allows easy addition of v2, v3, etc. in the future.

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test tests/ThreadPilot.VehicleService.Tests
dotnet test tests/ThreadPilot.InsuranceService.Tests
```

### Test Coverage

The solution includes comprehensive test coverage:

#### Unit Tests
- **VehicleServiceTests.cs**: Core business logic validation, Result pattern, error codes
- **InsuranceServiceTests.cs**: Business logic, vehicle enrichment, graceful degradation
- **VehicleServiceExceptionTests.cs**: Exception handling, logging verification, resilience
- **InsuranceServiceExceptionTests.cs**: Repository failures, pricing errors, VehicleClient exceptions

#### Integration Tests
- **VehicleApiTests.cs**: HTTP endpoints, Problem Details responses, status codes
- **InsuranceApiTests.cs**: HTTP endpoints, service-to-service communication, error scenarios

**Test Stack:**
- **xUnit**: Test framework
- **Shouldly**: Fluent assertions
- **Moq**: Mocking dependencies
- **WebApplicationFactory**: Integration testing for HTTP endpoints

## Project Structure

```
ThreadPilot/
+-- src/
¦   +-- ThreadPilot.AppHost/                 # Aspire orchestration
¦   ¦   +-- AppHost.cs                       # Service configuration
¦   ¦
¦   +-- ThreadPilot.ServiceDefaults/         # Shared service configuration
¦   ¦   +-- Extensions.cs                    # Health checks, telemetry, etc.
¦   ¦   +-- Middleware/
¦   ¦       +-- CorrelationIdMiddleware.cs   # Request correlation
¦   ¦
¦   +-- ThreadPilot.VehicleService/          # Vehicle microservice
¦   ¦   +-- Program.cs                       # API endpoints & configuration
¦   ¦   +-- Vehicles/
¦   ¦       +-- VehicleService.cs            # Business logic
¦   ¦       +-- IVehicleRepository.cs        # Data access abstraction
¦   ¦
¦   +-- ThreadPilot.InsuranceService/        # Insurance microservice
¦   ¦   +-- Program.cs                       # API endpoints & configuration
¦   ¦   +-- Insurances/
¦   ¦       +-- InsuranceService.cs          # Business logic
¦   ¦       +-- IInsuranceRepository.cs      # Data access abstraction
¦   ¦   +-- VehicleClient/
¦   ¦       +-- VehicleClient.cs             # HTTP client for Vehicle Service
¦   ¦
¦   +-- ThreadPilot.Shared/                  # Shared libraries
¦       +-- Results/
¦           +-- Result.cs                    # Result pattern implementation
¦           +-- ResultHttpExtensions.cs      # Problem Details mapping
¦           +-- Error.cs                     # Error model
¦
+-- tests/
    +-- ThreadPilot.VehicleService.Tests/    # Vehicle service tests
    +-- ThreadPilot.InsuranceService.Tests/  # Insurance service tests
```

## Technologies Used

- **.NET 10**: Latest .NET framework
- **C# 14**: Latest C# language features
- **.NET Aspire**: Cloud-ready stack for distributed applications
- **Minimal APIs**: Lightweight HTTP API pattern
- **Scalar**: Modern OpenAPI documentation UI
- **xUnit + Shouldly + Moq**: Testing stack
- **Microsoft.Extensions.Http.Resilience**: HTTP resilience and fault handling

---

## Reflection

**Challenging Aspects:**
- **Finding the Right Abstraction Level**: Balancing between over-engineering (e.g., adding unnecessary patterns) and keeping it production-relevant. I aimed for a solution that demonstrates real-world patterns without excessive complexity.


**Interesting Aspects:**
- **.NET Aspire**: Aspire's orchestration capabilities. The unified dashboard for logs, traces, and metrics across services to significantly improve the development experience. 


### Similar Projects or Experience

I have worked with similar microservices architectures and integration patterns in both PwC and Dustin:

- **PwC (Case Management System)**: Built integrations between Azure App Services and Azure Functions for document processing and workflow orchestration. Gained experience with service-to-service communication, error handling, and monitoring distributed systems.

- **Dustin (Pricing & Availability Platform)**: Developed event-driven architecture using Azure Service Bus and Azure Functions to handle real-time price updates and inventory synchronization across multiple systems.


### Future Improvements

If given more time, here are recommended enhancements to make ThreadPilot production-ready:

**Infrastructure & Resilience:**
- **Database Integration**: Replace in-memory repositories with Entity Framework Core + PostgreSQL/SQL Server
- **Distributed Caching**: Add HybridCache or FusionCache with Redis cache for frequently accessed vehicle/insurance data
- **Advanced Resilience Policies**: Implement Polly circuit breakers, bulkhead isolation, and advanced retry strategies beyond basic HTTP resilience
- **Message Queue Integration**: Add Azure Service Bus or RabbitMQ for asynchronous event-driven communication between services

**Security & Authentication:**
- **API Authentication**: Implement JWT bearer tokens or API key authentication
- **Authorization**: Add role-based access control (RBAC) with claims-based authorization
- **Rate Limiting**: Implement API rate limiting to prevent abuse (ASP.NET Core rate limiting middleware)
- **Secrets Management**: Integrate Azure Key Vault for secure configuration and connection strings

**Observability & Monitoring:**
- **Application Insights Integration**: Deep telemetry, dependency tracking, and performance monitoring
- **Custom Metrics**: Add business metrics (e.g., request success rate, average response time per endpoint)
- **Distributed Tracing Correlation**: Ensure all log entries include correlation IDs across service boundaries
- **Health Check Endpoints**: Add comprehensive health checks that verify database connectivity, external service availability, etc.

**Testing & Quality:**
- **Unit Testing**: Continue adding unit tests
- **Integration Testing**: Continue adding integration tests
- **Performance Testing**: Implement load testing to identify bottlenecks under realistic traffic patterns

**API Evolution:**
- **API Gateway**: Add Azure API Management to provide:
  - Single entry point for all microservices
  - Centralized authentication, rate limiting, and request logging
  - Backend service routing without exposing internal service URLs to clients
- **Webhook Support**: Allow external systems to register webhook URLs to receive real-time notifications when:
  - Insurance policies are created/updated/cancelled
  - Vehicle data changes (useful for third-party integrations like accounting systems or CRM platforms)
  - This eliminates the need for clients to constantly poll your APIs for changes
- **Batch Operations**: Add bulk endpoints for scenarios where clients need to process multiple records efficiently:
  - `POST /api/v1/vehicles/batch` - retrieve details for multiple registration numbers in one request
  - `POST /api/v1/insurances/batch` - retrieve policies for multiple personal numbers
  - Reduces network overhead and improves performance for data-intensive operations (e.g., nightly report generation)

**DevOps & Deployment:**
- **CI/CD Pipeline**: GitHub Actions or Azure DevOps pipeline with automated build, test, code quality checks, and deployment stages
- **Infrastructure as Code**: Bicep templates for Azure infrastructure (App Services, SQL Database, Redis, Key Vault, Application Insights) to ensure consistent, repeatable deployments
- **Azure Container Apps**: Deploy services as containers with automatic scaling, traffic splitting for A/B testing, and simplified Aspire deployment
- **Blue-Green Deployment**: Deploy new versions alongside existing ones, validate in production with minimal traffic, then switch all traffic over - enabling instant rollback if issues occur

**Architecture & Domain Modeling:**
- **Domain-Driven Design (DDD) Refinement**: Introduce explicit domain models with value objects (e.g., `RegistrationNumber`, `PersonalNumber`, `Money`) to encapsulate validation logic and make domain rules explicit in the type system
- **Repository Pattern with Unit of Work**: Add proper transaction management for scenarios where multiple database operations must succeed or fail together
- **Background Jobs**: Use Hangfire or Azure Functions to handle asynchronous operations like sending email notifications, generating reports, or syncing data with external systems
- **Feature Flags**: Implement feature toggles (using Azure App Configuration) to enable/disable features in production without redeployment, supporting gradual rollouts and A/B testing

**Database & Performance:**
- **SQL Database Optimization**: When moving to SQL Server/PostgreSQL:
  - Add proper indexes on frequently queried columns (registration number, personal number, policy dates)
  - Implement connection pooling and configure appropriate timeout settings
  - Use query profiling tools to identify slow queries and optimize with appropriate indexes or query restructuring

- **Caching Strategy**: Implement multi-level caching:
  - **In-Memory Cache** (IMemoryCache) for frequently accessed data with short TTL
  - **Distributed Cache** (Redis) for data shared across service instances
  - **HybridCache** or **FusionCache** for more advanced features

- **Pagination**: Add pagination support for potential list endpoints (e.g., all vehicles, all insurances for admin dashboards) with skip/take or cursor-based pagination
- **Database Connection Resilience**: Use Polly retry policies for transient database failures with exponential backoff

These improvements would transform ThreadPilot from a demonstration project into an enterprise-grade, production-ready microservices platform.

