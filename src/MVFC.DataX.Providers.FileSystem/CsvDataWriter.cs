namespace MVFC.DataX.Providers.FileSystem;

public sealed class CsvDataWriter<T>(
    string filePath,
    CsvConfiguration? configuration = null,
    bool append = false) : IDataWriter<T>, IAsyncDisposable
{
    private readonly string _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly CsvConfiguration _configuration = configuration ?? new CsvConfiguration(CultureInfo.InvariantCulture);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private StreamWriter? _streamWriter;
    private CsvWriter? _csv;
    private bool _initialized;

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            _csv!.WriteRecord(item);
            await _csv.NextRecordAsync().ConfigureAwait(false);
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
            await _csv!.WriteRecordsAsync(items, ct).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        var fileExists = File.Exists(_filePath);
        _streamWriter = new StreamWriter(_filePath, append);
        _csv = new CsvWriter(_streamWriter, _configuration);

        if (!append || !fileExists)
        {
            _csv.WriteHeader<T>();
            await _csv.NextRecordAsync().ConfigureAwait(false);
        }

        _initialized = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_csv != null) await _csv.DisposeAsync().ConfigureAwait(false);
        if (_streamWriter != null) await _streamWriter.DisposeAsync().ConfigureAwait(false);
        _semaphore.Dispose();
    }
}
