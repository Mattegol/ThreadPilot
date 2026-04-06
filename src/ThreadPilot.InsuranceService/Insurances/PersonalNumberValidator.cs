using System.Text.RegularExpressions;
using ThreadPilot.Shared.Results;

namespace ThreadPilot.InsuranceService.Insurances;

public static class PersonalNumberValidator
{
    private static readonly Regex _pnrRegex = new(@"^\d{8}-\d{4}$", RegexOptions.Compiled);

    public static Result Validate(string personalNumber)
    {
        if (string.IsNullOrWhiteSpace(personalNumber))
            return Result.Failure(Errors.Validation("Personal number is required."));

        if (!_pnrRegex.IsMatch(personalNumber))
            return Result.Failure(Errors.Validation("Personal number must match format YYYYMMDD-XXXX."));

        return Result.Success();
    }
}
