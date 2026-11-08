namespace MVFC.DataX.Providers.FileSystem;

public sealed class CsvDataReader<T>(
    string filePath,
    CsvReaderOptions? configuration = null) : IDataReader<T>
{
    private readonly string _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly CsvReaderOptions _configuration = configuration ?? new CsvReaderOptions()
    {
        FormatProvider = CultureInfo.InvariantCulture,
    };

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(_filePath);
        await using var csv = CsvReader.Create(reader, _configuration);

        var records = csv.GetRecordsAsync<T>(ct);

        await foreach (var record in records.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return record;
        }
    }
}
