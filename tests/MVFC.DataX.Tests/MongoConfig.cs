namespace MVFC.DataX.Tests;

public static class MongoConfig
{
    private static readonly Lock _sync = new();
    private static bool _initialized;

    public static void Initialize()
    {
        lock (_sync)
        {
            if (_initialized) return;

            if (!BsonClassMap.IsClassMapRegistered(typeof(BankInfo)))
            {
                BsonClassMap.RegisterClassMap<BankInfo>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
            _initialized = true;
        }
    }
}
