namespace MVFC.DataX.Providers.FileSystem;

public sealed class JsonlDataWriter<T>(
    string filePath,
    JsonSerializerOptions? options = null,
    bool append = false) : IDataWriter<T>, IAsyncDisposable
{
    private readonly string _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly JsonSerializerOptions? _options = options;
    private readonly bool _append = append;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private StreamWriter? _streamWriter;
    private bool _initialized;

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var json = JsonSerializer.Serialize(item, _options);
            await _streamWriter!.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return;

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                var json = JsonSerializer.Serialize(item, _options);
                await _streamWriter!.WriteLineAsync(json.AsMemory(), ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            _streamWriter = new StreamWriter(_filePath, _append);
            _initialized = true;
        }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_streamWriter != null) await _streamWriter.DisposeAsync().ConfigureAwait(false);
        _semaphore.Dispose();
    }
}
