namespace MVFC.DataX.Core.Models;

public sealed record DataError(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue);
