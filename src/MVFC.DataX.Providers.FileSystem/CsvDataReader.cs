namespace MVFC.DataX.Providers.FileSystem;

public sealed class CsvDataReader<T>(
    string filePath,
    CsvConfiguration? configuration = null) : IDataReader<T>
{
    private readonly string _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly CsvConfiguration _configuration = configuration ?? new CsvConfiguration(CultureInfo.InvariantCulture);

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _configuration);
        
        var records = csv.GetRecordsAsync<T>(ct);
        
        await foreach (var record in records.ConfigureAwait(false))
        {
            yield return record;
        }
    }
}
