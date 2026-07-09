namespace MVFC.DataX.Core.Models;

public sealed record PipelineStatistics(
    long TotalRead,
    long Succeeded,
    long Failed,
    long Skipped,
    TimeSpan Elapsed,
    IReadOnlyList<DataError> Errors)
{
    public double Throughput => Elapsed.TotalSeconds > 0 ? TotalRead / Elapsed.TotalSeconds : 0;
    public double SuccessRate => TotalRead > 0 ? (double)Succeeded / TotalRead * 100 : 0;
    public double FailureRate => TotalRead > 0 ? (double)Failed / TotalRead * 100 : 0;
}
