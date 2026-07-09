namespace MVFC.DataX.Core.Engine;

public sealed class PipelineEngine<TInput, TOutput>(
    IDataReader<TInput> reader,
    IDataTransformer<TInput, TOutput> transformer,
    IDataWriter<TOutput> writer,
    PipelineOptions options,
    IDataWriter<DataResult<TOutput>>? deadLetterWriter = null,
    Func<PipelineStatistics, Task>? onCompleted = null) : IAsyncDisposable
{
    private readonly IDataReader<TInput> _reader = reader;
    private readonly IDataTransformer<TInput, TOutput> _transformer = transformer;
    private readonly IDataWriter<TOutput> _writer = writer;
    private readonly PipelineOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IDataWriter<DataResult<TOutput>>? _deadLetterWriter = deadLetterWriter;
    private readonly Func<PipelineStatistics, Task>? _onCompleted = onCompleted;

    public async Task<PipelineStatistics> RunAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        long totalRead = 0;
        var errors = new ConcurrentQueue<DataError>();

        var channelOptions = new BoundedChannelOptions(_options.ChannelCapacity > 0 ? _options.ChannelCapacity : 1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        };

        var inputChannel = Channel.CreateBounded<TInput>(channelOptions);

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

        var processTasks = new List<Task<(long Succeeded, long Failed, long Skipped)>>();
        for (var i = 0; i < (_options.Parallelism > 0 ? _options.Parallelism : 1); i++)
        {
            processTasks.Add(Task.Run(() => ProcessWorkerAsync(inputChannel.Reader, errors, ct), ct));
        }

        await readTask.ConfigureAwait(false);

        long totalSucceeded = 0;
        long totalFailed = 0;
        long totalSkipped = 0;

        foreach (var task in processTasks)
        {
            var (succeeded, failed, skipped) = await task.ConfigureAwait(false);
            totalSucceeded += succeeded;
            totalFailed += failed;
            totalSkipped += skipped;
        }

        sw.Stop();
        var stats = new PipelineStatistics(totalRead, totalSucceeded, totalFailed, totalSkipped, sw.Elapsed, errors.ToArray());

        if (_onCompleted != null)
        {
            await _onCompleted(stats).ConfigureAwait(false);
        }

        return stats;
    }

    private async Task<(long Succeeded, long Failed, long Skipped)> ProcessWorkerAsync(ChannelReader<TInput> reader, ConcurrentQueue<DataError> errors, CancellationToken ct)
    {
        long localSucceeded = 0;
        long localFailed = 0;
        long localSkipped = 0;
        var inputEnumerable = reader.ReadAllAsync(ct);
        var transformedStream = _transformer.TransformAsync(inputEnumerable, ct);

        var batchSize = _options.BatchSize > 0 ? _options.BatchSize : 100;
        var batch = new List<TOutput>(batchSize);

        await foreach (var result in transformedStream.ConfigureAwait(false))
        {
            if (result.IsSuccess && result.Value is not null)
            {
                batch.Add(result.Value);
                if (batch.Count >= batchSize)
                {
                    await WriteBatchWithRetryAsync(batch, ct).ConfigureAwait(false);
                    localSucceeded += batch.Count;
                    batch = new List<TOutput>(batchSize);
                }
            }
            else if (result.IsSkipped)
            {
                localSkipped++;
            }
            else
            {
                localFailed++;
                await HandleFailureAsync(result, errors, ct).ConfigureAwait(false);
            }
        }

        if (batch.Count > 0)
        {
            await WriteBatchWithRetryAsync(batch, ct).ConfigureAwait(false);
            localSucceeded += batch.Count;
        }

        return (localSucceeded, localFailed, localSkipped);
    }

    private async Task HandleFailureAsync(DataResult<TOutput> result, ConcurrentQueue<DataError> errors, CancellationToken ct)
    {
        foreach (var e in result.Errors)
        {
            errors.Enqueue(e);
        }

        if (_deadLetterWriter != null)
        {
            await _deadLetterWriter.WriteAsync(result, ct).ConfigureAwait(false);
        }
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (retries < _options.MaxRetries && (_options.RetryPredicate == null || _options.RetryPredicate(ex)))
            {
                retries++;
                if (_options.RetryDelay > TimeSpan.Zero)
                {
                    var delay = _options.RetryDelay;
                    if (_options.UseJitter)
                    {
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));
                        delay += jitter;
                    }
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_reader is IAsyncDisposable readerAsyncDisp) await readerAsyncDisp.DisposeAsync().ConfigureAwait(false);
        else if (_reader is IDisposable readerDisp) readerDisp.Dispose();

        if (_transformer is IAsyncDisposable transformerAsyncDisp) await transformerAsyncDisp.DisposeAsync().ConfigureAwait(false);
        else if (_transformer is IDisposable transformerDisp) transformerDisp.Dispose();

        if (_writer is IAsyncDisposable writerAsyncDisp) await writerAsyncDisp.DisposeAsync().ConfigureAwait(false);
        else if (_writer is IDisposable writerDisp) writerDisp.Dispose();

        if (_deadLetterWriter is IAsyncDisposable dlqAsyncDisp) await dlqAsyncDisp.DisposeAsync().ConfigureAwait(false);
        else if (_deadLetterWriter is IDisposable dlqDisp) dlqDisp.Dispose();
    }
}
