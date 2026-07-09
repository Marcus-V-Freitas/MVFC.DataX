namespace MVFC.DataX.Pipeline;

public sealed class DataPipeline<TInput, TOutput>
{
    private readonly PipelineEngine<TInput, TOutput> _engine;

    internal DataPipeline(PipelineEngine<TInput, TOutput> engine)
    {
        _engine = engine;
    }

    public Task<PipelineStatistics> RunAsync(CancellationToken ct = default) =>
        _engine.RunAsync(ct);
}
