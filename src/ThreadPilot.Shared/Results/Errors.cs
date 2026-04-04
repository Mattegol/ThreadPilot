namespace ThreadPilot.Shared.Results;

public static class Errors
{
    public static readonly Error InvalidInput =
        new("invalid_input", "Input is invalid.");

    public static readonly Error NotFound =
        new("not_found", "Resource was not found.");

    public static readonly Error DependencyFailure =
        new("dependency_failure", "Dependency call failed.");

    public static Error Validation(string message) =>
        new("validation_error", message);
}