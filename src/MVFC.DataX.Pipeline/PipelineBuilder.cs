namespace MVFC.DataX.Pipeline;

public static class PipelineBuilder
{
    public static PipelineBuilder<TInput> ReadFrom<TInput>(IDataReader<TInput> reader)
        => new(reader);

    public static PipelineBuilder<TInput> ReadFrom<TInput>(IEnumerable<TInput> source)
        => new(new EnumerableReader<TInput>(source));

    public static PipelineBuilder<TInput> ReadFrom<TInput>(IAsyncEnumerable<TInput> source)
        => new(new EnumerableReader<TInput>(source));

    public static PipelineBuilder<TInput> ReadFrom<TInput>(params IDataReader<TInput>[] readers)
    {
        if (readers == null || readers.Length == 0)
            throw new ArgumentException("At least one reader must be provided.", nameof(readers));

        return readers.Length == 1 ? new(readers[0]) : new(new MergeReader<TInput>(readers));
    }
}

public sealed class PipelineBuilder<TInput>
{
    private readonly IDataReader<TInput> _reader;

    internal PipelineBuilder(IDataReader<TInput> reader)
    {
        _reader = reader;
    }

    public PipelineBuilder<TInput, TOutput> TransformWith<TOutput>(IDataTransformer<TInput, TOutput> transformer)
        => new(_reader, transformer);

    public PipelineBuilder<TInput, TInput> Skip(int count)
        => TransformWith(new SkipTransformer<TInput>(count));

    public PipelineBuilder<TInput, TInput> Take(int count)
        => TransformWith(new TakeTransformer<TInput>(count));

    public PipelineBuilder<TInput, TInput> Filter(Func<TInput, bool> predicate)
        => TransformWith(new FilterTransformer<TInput>(predicate));

    public PipelineBuilder<TInput, TInput> Distinct(IEqualityComparer<TInput>? comparer = null, int? maxCapacity = null)
        => TransformWith(new DistinctTransformer<TInput>(comparer, maxCapacity));

    public PipelineBuilder<TInput, TInput> OrderBy<TKey>(Func<TInput, TKey> keySelector, bool descending = false, IComparer<TKey>? comparer = null, int? maxItems = null)
        => TransformWith(new OrderByTransformer<TInput, TKey>(keySelector, descending, comparer, maxItems));

    public PipelineBuilder<TInput, TOut> FlatMap<TOut>(Func<TInput, IEnumerable<TOut>?> mapFunc)
        => TransformWith(new FlatMapTransformer<TInput, TOut>(mapFunc));

    public PipelineBuilder<TInput, TAccumulate> Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, TInput, TAccumulate> func)
        => TransformWith(new AggregateTransformer<TInput, TAccumulate>(seed, func));

    public PipelineBuilder<TInput, IReadOnlyList<TInput>> Batch(int batchSize)
        => TransformWith(new BatchTransformer<TInput>(batchSize));
}

public sealed class PipelineBuilder<TInput, TOutput>
{
    private readonly IDataReader<TInput> _reader;
    private readonly IDataTransformer<TInput, TOutput> _transformer;
    private IDataWriter<TOutput>? _writer;
    private IDataWriter<DataResult<TOutput>>? _deadLetterWriter;
    private PipelineOptions _options = new();
    private Func<PipelineStatistics, Task>? _onCompleted;
    private readonly List<IPipelineMiddleware<TInput>> _middlewares = [];

    internal PipelineBuilder(IDataReader<TInput> reader, IDataTransformer<TInput, TOutput> transformer)
    {
        _reader = reader;
        _transformer = transformer;
    }

    private PipelineBuilder(IDataReader<TInput> reader, IDataTransformer<TInput, TOutput> transformer, PipelineOptions options, List<IPipelineMiddleware<TInput>>? middlewares = null)
    {
        _reader = reader;
        _transformer = transformer;
        _options = options;
        if (middlewares != null)
        {
            _middlewares.AddRange(middlewares);
        }
    }

    public PipelineBuilder<TInput, TNext> TransformWith<TNext>(IDataTransformer<TOutput, TNext> nextTransformer)
    {
        var composite = new CompositeTransformer<TInput, TOutput, TNext>(_transformer, nextTransformer);
        return new PipelineBuilder<TInput, TNext>(_reader, composite, _options, _middlewares);
    }

    public PipelineBuilder<TInput, TOutput> Skip(int count)
        => TransformWith(new SkipTransformer<TOutput>(count));

    public PipelineBuilder<TInput, TOutput> Take(int count)
        => TransformWith(new TakeTransformer<TOutput>(count));

    public PipelineBuilder<TInput, TOutput> Filter(Func<TOutput, bool> predicate)
        => TransformWith(new FilterTransformer<TOutput>(predicate));

    public PipelineBuilder<TInput, TOutput> Distinct(IEqualityComparer<TOutput>? comparer = null, int? maxCapacity = null)
        => TransformWith(new DistinctTransformer<TOutput>(comparer, maxCapacity));

    public PipelineBuilder<TInput, TOutput> OrderBy<TKey>(Func<TOutput, TKey> keySelector, bool descending = false, IComparer<TKey>? comparer = null, int? maxItems = null)
        => TransformWith(new OrderByTransformer<TOutput, TKey>(keySelector, descending, comparer, maxItems));

    public PipelineBuilder<TInput, TNext> FlatMap<TNext>(Func<TOutput, IEnumerable<TNext>?> mapFunc)
        => TransformWith(new FlatMapTransformer<TOutput, TNext>(mapFunc));

    public PipelineBuilder<TInput, TAccumulate> Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, TOutput, TAccumulate> func)
        => TransformWith(new AggregateTransformer<TOutput, TAccumulate>(seed, func));

    public PipelineBuilder<TInput, IReadOnlyList<TOutput>> Batch(int batchSize)
        => TransformWith(new BatchTransformer<TOutput>(batchSize));

    public PipelineBuilder<TInput, TOutput> WriteTo(IDataWriter<TOutput> writer)
    {
        _writer = writer;
        return this;
    }

    public PipelineBuilder<TInput, TOutput> ReplaceWriter(Func<IDataWriter<TOutput>, IDataWriter<TOutput>> replaceFunc)
    {
        ArgumentNullException.ThrowIfNull(replaceFunc);

        if (_writer != null)
        {
            _writer = replaceFunc(_writer);
        }
        return this;
    }

    public PipelineBuilder<TInput, TOutput> WithParallelism(int degree)
    {
        _options = _options with { Parallelism = degree };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> WithBatchSize(int batchSize)
    {
        _options = _options with { BatchSize = batchSize };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> OnError(IDataWriter<DataResult<TOutput>> deadLetterWriter)
    {
        _deadLetterWriter = deadLetterWriter;
        return this;
    }

    public PipelineBuilder<TInput, TOutput> OnError(Func<Exception, ErrorAction> classifier)
    {
        _options = _options with { ErrorClassifier = classifier };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> WithChannelCapacity(int capacity)
    {
        _options = _options with { ChannelCapacity = capacity };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> WithRetry(int maxRetries, TimeSpan delay)
    {
        _options = _options with { MaxRetries = maxRetries, RetryDelay = delay };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> WithExponentialBackoff(int maxRetries, TimeSpan baseDelay, TimeSpan? maxDelay = null)
    {
        _options = _options with
        {
            MaxRetries = maxRetries,
            RetryDelay = baseDelay,
            UseExponentialBackoff = true,
            MaxRetryDelay = maxDelay
        };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> OnRetry(Action<Exception, int, TimeSpan> callback)
    {
        _options = _options with { OnRetry = callback };
        return this;
    }

    public PipelineBuilder<TInput, TOutput> Use(IPipelineMiddleware<TInput> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middlewares.Add(middleware);
        return this;
    }

    public PipelineBuilder<TInput, TOutput> Use(Func<IAsyncEnumerable<TInput>, CancellationToken, IAsyncEnumerable<TInput>> middlewareFunc)
    {
        ArgumentNullException.ThrowIfNull(middlewareFunc);
        _middlewares.Add(new DelegatePipelineMiddleware<TInput>(middlewareFunc));
        return this;
    }

    public PipelineBuilder<TInput, TOutput> OnCompleted(Func<PipelineStatistics, Task> callback)
    {
        _onCompleted = callback;
        return this;
    }

    public DataPipeline<TInput, TOutput> Build()
    {
        if (_writer == null)
            throw new InvalidOperationException("Writer must be configured.");

        var engine = new PipelineEngine<TInput, TOutput>(
            _reader,
            _transformer,
            _writer,
            _options,
            _deadLetterWriter,
            _onCompleted,
            _middlewares);

        return new DataPipeline<TInput, TOutput>(engine);
    }
}
