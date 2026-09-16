namespace SecureBank.Application.Abstractions.Models;

public class Result<T>
{
    private Result(T? value, IReadOnlyCollection<string> errors)
    {
        Value = value;
        Errors = errors;
        IsSuccess = errors.Count == 0;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static Result<T> Ok(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(
            value,
            []);
    }

    public static Result<T> Fail(
        params string[] errors)
    {
        if (errors.Length == 0)
        {
            throw new ArgumentException(
                "At least one error is required.",
                nameof(errors));
        }

        return new Result<T>(
            default,
            errors);
    }
}
