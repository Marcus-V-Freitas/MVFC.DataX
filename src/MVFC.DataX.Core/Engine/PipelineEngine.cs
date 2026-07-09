namespace MVFC.DataX.Core.Engine;

public sealed class PipelineEngine<TInput, TOutput>(
    IDataReader<TInput> reader,
    IDataTransformer<TInput, TOutput> transformer,
    IDataWriter<TOutput> writer,
    PipelineOptions options,
    IDataWriter<DataResult<TOutput>>? deadLetterWriter = null,
    Func<PipelineStatistics, Task>? onCompleted = null,
    IEnumerable<IPipelineMiddleware<TInput>>? middlewares = null) : IAsyncDisposable
{
    private readonly IDataReader<TInput> _reader = reader;
    private readonly IDataTransformer<TInput, TOutput> _transformer = transformer;
    private readonly IDataWriter<TOutput> _writer = writer;
    private readonly PipelineOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IDataWriter<DataResult<TOutput>>? _deadLetterWriter = deadLetterWriter;
    private readonly Func<PipelineStatistics, Task>? _onCompleted = onCompleted;
    private readonly IEnumerable<IPipelineMiddleware<TInput>> _middlewares = middlewares ?? [];

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

        await Task.WhenAll([readTask, ..processTasks]).ConfigureAwait(false);

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
        foreach (var middleware in _middlewares)
        {
            inputEnumerable = middleware.InvokeAsync(inputEnumerable, ct);
        }
        var transformedStream = _transformer.TransformAsync(inputEnumerable, ct);

        var batchSize = _options.BatchSize > 0 ? _options.BatchSize : 100;
        var batch = new List<TOutput>(batchSize);

        try
        {
            await foreach (var result in transformedStream.ConfigureAwait(false))
            {
                if (result.IsSuccess && result.Value is not null)
                {
                    batch.Add(result.Value);
                    if (batch.Count >= batchSize)
                    {
                        var status = await WriteBatchWithRetryAsync(batch, errors, ct).ConfigureAwait(false);
                        UpdateCounters(status, batch.Count, ref localSucceeded, ref localSkipped, ref localFailed);
                        batch.Clear();
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
                var status = await WriteBatchWithRetryAsync(batch, errors, ct).ConfigureAwait(false);
                UpdateCounters(status, batch.Count, ref localSucceeded, ref localSkipped, ref localFailed);
            }
        }
        catch (Exception ex)
        {
            var (exFailed, exSkipped) = await HandleTransformerExceptionAsync(ex, batch, errors, ct).ConfigureAwait(false);
            localFailed += exFailed;
            localSkipped += exSkipped;
        }

        return (localSucceeded, localFailed, localSkipped);
    }

    private static void UpdateCounters(WriteStatus status, int batchCount, ref long succeeded, ref long skipped, ref long failed)
    {
        switch (status)
        {
            case WriteStatus.Success:
                succeeded += batchCount;
                break;
            case WriteStatus.Skipped:
                skipped += batchCount;
                break;
            case WriteStatus.Failed:
                failed += batchCount;
                break;
        }
    }

    private async Task<(long Failed, long Skipped)> HandleTransformerExceptionAsync(Exception ex, List<TOutput> batch, ConcurrentQueue<DataError> errors, CancellationToken ct)
    {
        var action = _options.ErrorClassifier?.Invoke(ex) ?? ErrorAction.Abort;
        if (action == ErrorAction.Abort)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
        }
        if (action == ErrorAction.Skip)
        {
            return (0, batch.Count);
        }
        if (action == ErrorAction.DeadLetter && _deadLetterWriter != null)
        {
            foreach (var item in batch)
            {
                var failureResult = DataResult.Failure<TOutput>([DataError.FromException(ex, propertyName: "Transformer", attemptedValue: item)]);
                await HandleFailureAsync(failureResult, errors, ct).ConfigureAwait(false);
            }
            return (batch.Count, 0);
        }
        return (0, 0);
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

    private async Task<WriteStatus> WriteBatchWithRetryAsync(IReadOnlyList<TOutput> batch, ConcurrentQueue<DataError> errors, CancellationToken ct)
    {
        var retries = 0;
        while (true)
        {
            try
            {
                await _writer.WriteBatchAsync(batch, ct).ConfigureAwait(false);
                return WriteStatus.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var (shouldRetry, delay) = EvaluateRetry(ex, ref retries);
                if (shouldRetry)
                {
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }
                    continue;
                }

                return await HandleWriteExceptionAsync(ex, batch, errors, ct).ConfigureAwait(false);
            }
        }
    }

    private (bool ShouldRetry, TimeSpan Delay) EvaluateRetry(Exception ex, ref int retries)
    {
        var canRetry = retries < _options.MaxRetries && (_options.RetryPredicate == null || _options.RetryPredicate(ex));
        var action = _options.ErrorClassifier?.Invoke(ex) ?? (canRetry ? ErrorAction.Retry : ErrorAction.Abort);

        if (action == ErrorAction.Retry && canRetry)
        {
            retries++;
            var delay = CalculateRetryDelay(retries);
            _options.OnRetry?.Invoke(ex, retries, delay);
            return (true, delay);
        }

        return (false, TimeSpan.Zero);
    }

    private TimeSpan CalculateRetryDelay(int retries)
    {
        var delay = _options.RetryDelay;
        if (delay <= TimeSpan.Zero) return TimeSpan.Zero;

        if (_options.UseExponentialBackoff)
        {
            var factor = Math.Pow(2, retries - 1);
            try
            {
                delay = TimeSpan.FromTicks((long)(delay.Ticks * factor));
            }
            catch (OverflowException)
            {
                delay = TimeSpan.MaxValue;
            }
        }

        if (_options.MaxRetryDelay.HasValue && delay > _options.MaxRetryDelay.Value)
        {
            delay = _options.MaxRetryDelay.Value;
        }

        if (_options.UseJitter)
        {
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));
            delay += jitter;
        }

        return delay;
    }

    private async Task<WriteStatus> HandleWriteExceptionAsync(Exception ex, IReadOnlyList<TOutput> batch, ConcurrentQueue<DataError> errors, CancellationToken ct)
    {
        var action = _options.ErrorClassifier?.Invoke(ex) ?? ErrorAction.Abort;

        if (action == ErrorAction.Skip)
        {
            return WriteStatus.Skipped;
        }

        if (action == ErrorAction.DeadLetter && _deadLetterWriter != null)
        {
            foreach (var item in batch)
            {
                var failureResult = DataResult.Failure<TOutput>([DataError.FromException(ex, propertyName: "Writer", attemptedValue: item)]);
                await HandleFailureAsync(failureResult, errors, ct).ConfigureAwait(false);
            }
            return WriteStatus.Failed;
        }

        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
        return WriteStatus.Failed; // unreachable
    }

    private enum WriteStatus
    {
        Success,
        Skipped,
        Failed
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
