namespace SecureBank.Application.Abstractions.Models;

public class Result
{
    private Result(IReadOnlyCollection<string> errors)
    {
        Errors = errors;
        IsSuccess = errors.Count == 0;
    }

    public bool IsSuccess { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static Result Ok()
    {
        return new Result([]);
    }

    public static Result Fail(
        params string[] errors)
    {
        if (errors.Length == 0)
        {
            throw new ArgumentException(
                "At least one error is required.",
                nameof(errors));
        }

        return new Result(errors);
    }
}
