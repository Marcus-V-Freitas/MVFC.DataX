namespace MVFC.DataX.Providers.FileSystem;

public sealed class JsonlDataReader<T>(
    string filePath,
    JsonSerializerOptions? options = null) : IDataReader<T>
{
    private readonly string _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly JsonSerializerOptions? _options = options;

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var item = JsonSerializer.Deserialize<T>(line, _options);
            if (!EqualityComparer<T>.Default.Equals(item, default))
            {
                yield return item!;
            }
        }
    }
}
