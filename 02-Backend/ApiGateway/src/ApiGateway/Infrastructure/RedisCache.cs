namespace SmartCampusOS.ApiGateway.Infrastructure;

using StackExchange.Redis;

/// <summary>
/// Redis 缓存实现：令牌黑名单（KeyExists）与固定窗口限流计数（Lua 原子 INCR + EXPIRE）。
/// Redis 连接异常向上抛出，由中间件按 FailOpen 配置降级（LLD §9 / PRD R-04 限流降级预案）。
/// </summary>
public sealed class RedisCache : ICache, IDisposable
{
    private static readonly string AcquireScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[2])
        end
        if current > tonumber(ARGV[1]) then
            return 0
        end
        return 1
        """;

    private readonly Lazy<ConnectionMultiplexer> _lazyConnection;

    public RedisCache(string connectionString, int connectTimeoutSeconds = 3, int operationTimeoutMilliseconds = 500)
    {
        var configuration = ConfigurationOptions.Parse(connectionString);
        configuration.ConnectTimeout = connectTimeoutSeconds * 1000;
        configuration.AbortOnConnectFail = false;
        configuration.SyncTimeout = operationTimeoutMilliseconds;
        configuration.AsyncTimeout = operationTimeoutMilliseconds;
        _lazyConnection = new Lazy<ConnectionMultiplexer>(
            () => ConnectionMultiplexer.Connect(configuration),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private IDatabase Database => _lazyConnection.Value.GetDatabase();

    public Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
        => Database.KeyExistsAsync(key);

    public async Task<bool> TryAcquireAsync(string key, long limit, int windowSeconds, CancellationToken ct = default)
    {
        RedisKey[] keys = [key];
        RedisValue[] values = [limit, windowSeconds];
        var result = await Database.ScriptEvaluateAsync(AcquireScript, keys, values);
        return !result.IsNull && (int)result == 1;
    }

    public void Dispose()
    {
        if (_lazyConnection.IsValueCreated)
        {
            _lazyConnection.Value.Dispose();
        }
    }
}
