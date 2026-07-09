namespace MVFC.DataX.Core.Models;

public sealed record DataError(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue,
    string? ErrorCode = null,
    string? StageId = null);
