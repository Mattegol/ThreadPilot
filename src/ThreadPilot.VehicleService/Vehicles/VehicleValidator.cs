using System.Text.RegularExpressions;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.VehicleService.Vehicles;

public static class VehicleValidator
{
    private static readonly Regex _regNoRegex = new("^[A-Z]{3}[0-9]{3}$", RegexOptions.Compiled);

    public static Result ValidateRegistrationNumber(string registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            return Result.Failure(Errors.Validation("Registration number is required."));

        if (!_regNoRegex.IsMatch(registrationNumber))
            return Result.Failure(Errors.Validation("Registration number must match format ABC123."));

        return Result.Success();
    }
}
