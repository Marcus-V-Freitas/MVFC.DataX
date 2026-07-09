namespace MVFC.DataX.Core.Models;

public sealed record PipelineStatistics(
    long TotalRead,
    long Succeeded,
    long Failed,
    long Skipped,
    TimeSpan Elapsed,
    IReadOnlyList<DataError> Errors);
