namespace MVFC.DataX.Providers.MongoDB;

public sealed class MongoReader<T> : IDataReader<T>
{
    private readonly IMongoCollection<T> _collection;
    private readonly FilterDefinition<T> _filter;
    private readonly FindOptions<T>? _options;

    public MongoReader(string connectionString, string database, string collectionName, FilterDefinition<T>? filter = null, FindOptions<T>? options = null)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(database);
        _collection = db.GetCollection<T>(collectionName);
        _filter = filter ?? Builders<T>.Filter.Empty;
        _options = options;
    }

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var cursor = await _collection.FindAsync(_filter, _options, ct);
        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var doc in cursor.Current)
            {
                ct.ThrowIfCancellationRequested();
                yield return doc;
            }
        }
    }
}
