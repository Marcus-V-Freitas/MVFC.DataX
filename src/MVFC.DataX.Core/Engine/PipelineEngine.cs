namespace MVFC.DataX.Core.Engine;

public sealed class PipelineEngine<TInput, TOutput>(
    IDataReader<TInput> reader,
    IDataTransformer<TInput, TOutput> transformer,
    IDataWriter<TOutput> writer,
    IDataWriter<DataResult<TOutput>>? deadLetterWriter = null,
    int parallelism = 1,
    int batchSize = 100,
    int channelCapacity = 1000,
    int maxRetries = 0,
    TimeSpan retryDelay = default,
    Func<PipelineStatistics, Task>? onCompleted = null)
{
    private readonly IDataReader<TInput> _reader = reader;
    private readonly IDataTransformer<TInput, TOutput> _transformer = transformer;
    private readonly IDataWriter<TOutput> _writer = writer;
    private readonly IDataWriter<DataResult<TOutput>>? _deadLetterWriter = deadLetterWriter;
    private readonly int _parallelism = parallelism > 0 ? parallelism : 1;
    private readonly int _batchSize = batchSize > 0 ? batchSize : 100;
    private readonly int _channelCapacity = channelCapacity > 0 ? channelCapacity : 1000;
    private readonly int _maxRetries = maxRetries >= 0 ? maxRetries : 0;
    private readonly TimeSpan _retryDelay = retryDelay < TimeSpan.Zero ? TimeSpan.Zero : retryDelay;
    private readonly Func<PipelineStatistics, Task>? _onCompleted = onCompleted;

    public async Task<PipelineStatistics> RunAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        long totalRead = 0;
        var errors = new List<DataError>();

        var options = new BoundedChannelOptions(_channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        };

        var inputChannel = Channel.CreateBounded<TInput>(options);

        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in _reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    Interlocked.Increment(ref totalRead);
                    await inputChannel.Writer.WriteAsync(item, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                inputChannel.Writer.Complete();
            }
        }, ct);

        var processTasks = new List<Task<(long Succeeded, long Failed)>>();
        for (var i = 0; i < _parallelism; i++)
        {
            processTasks.Add(Task.Run(() => ProcessWorkerAsync(inputChannel.Reader, errors, ct), ct));
        }

        var results = await Task.WhenAll(processTasks).ConfigureAwait(false);
        await readTask.ConfigureAwait(false);

        var totalSucceeded = results.Sum(r => r.Succeeded);
        var totalFailed = results.Sum(r => r.Failed);
        var skipped = totalRead - totalSucceeded - totalFailed;

        sw.Stop();

        var stats = new PipelineStatistics(totalRead, totalSucceeded, totalFailed, skipped, sw.Elapsed, errors);

        if (_onCompleted != null)
        {
            await _onCompleted(stats).ConfigureAwait(false);
        }

        return stats;
    }

    private async Task<(long Succeeded, long Failed)> ProcessWorkerAsync(ChannelReader<TInput> reader, List<DataError> errors, CancellationToken ct)
    {
        long localSucceeded = 0;
        long localFailed = 0;
        var inputEnumerable = reader.ReadAllAsync(ct);
        var transformedStream = _transformer.TransformAsync(inputEnumerable, ct);

        var batch = new List<TOutput>(_batchSize);

        await foreach (var result in transformedStream.ConfigureAwait(false))
        {
            if (result.IsSuccess && result.Value is not null)
            {
                batch.Add(result.Value);
                if (batch.Count >= _batchSize)
                {
                    await WriteBatchWithRetryAsync(batch, ct).ConfigureAwait(false);
                    localSucceeded += batch.Count;
                    batch = new List<TOutput>(_batchSize);
                }
            }
            else
            {
                localFailed++;
                lock (errors)
                {
                    errors.AddRange(result.Errors);
                }

                if (_deadLetterWriter != null)
                {
                    await _deadLetterWriter.WriteAsync(result, ct).ConfigureAwait(false);
                }
            }
        }

        if (batch.Count > 0)
        {
            await WriteBatchWithRetryAsync(batch, ct).ConfigureAwait(false);
            localSucceeded += batch.Count;
        }

        return (localSucceeded, localFailed);
    }

    private async Task WriteBatchWithRetryAsync(IReadOnlyList<TOutput> batch, CancellationToken ct)
    {
        var retries = 0;
        while (true)
        {
            try
            {
                await _writer.WriteBatchAsync(batch, ct).ConfigureAwait(false);
                return;
            }
            catch (Exception) when (retries < _maxRetries)
            {
                retries++;
                if (_retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_retryDelay, ct).ConfigureAwait(false);
                }
            }
        }
    }
}
