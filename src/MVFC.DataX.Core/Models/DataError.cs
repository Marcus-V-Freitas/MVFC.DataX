namespace MVFC.DataX.Core.Models;

public sealed record DataError(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue,
    string? ErrorCode = null,
    string? StageId = null,
    string? ExceptionType = null,
    string? StackTrace = null)
{
    public static DataError FromException(
        Exception ex,
        string propertyName = "Exception",
        object? attemptedValue = null,
        string? errorCode = null,
        string? stageId = null)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return new DataError(
            propertyName,
            ex.Message,
            attemptedValue,
            errorCode,
            stageId,
            ex.GetType().FullName,
            ex.StackTrace);
    }
}
