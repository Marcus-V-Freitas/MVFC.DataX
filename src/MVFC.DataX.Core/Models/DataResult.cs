namespace MVFC.DataX.Core.Models;

public sealed record DataResult<T>
{
    public bool IsSuccess { get; }
    public bool IsSkipped { get; }
    public bool IsFailure => !IsSuccess && !IsSkipped;
    public T? Value { get; }
    public IReadOnlyList<DataError> Errors { get; }

    internal DataResult(bool isSuccess, T? value, IReadOnlyList<DataError> errors, bool isSkipped = false)
    {
        IsSuccess = isSuccess;
        IsSkipped = isSkipped;
        Value = value;
        Errors = errors;
    }
}

public static class DataResult
{
    public static DataResult<T> Success<T>(T value) => new(true, value, []);

    public static DataResult<T> Failure<T>(IEnumerable<DataError> errors) => new(false, default, [.. errors], false);

    public static DataResult<T> Skipped<T>() => new(false, default, [], true);
}
