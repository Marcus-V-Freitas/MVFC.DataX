namespace MVFC.DataX.Providers.MongoDB;

public sealed class MongoWriter<T> : IDataWriter<T>
{
    private readonly IMongoCollection<T> _collection;

    public MongoWriter(string connectionString, string database, string collectionName)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(database);
        _collection = db.GetCollection<T>(collectionName);
    }

    public Task WriteAsync(T item, CancellationToken ct = default)
        => _collection.InsertOneAsync(item, cancellationToken: ct);

    public Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
        => _collection.InsertManyAsync(items, cancellationToken: ct);
}
