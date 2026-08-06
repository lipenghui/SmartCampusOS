namespace SmartCampusOS.ApiGateway.Tests.Support;

using System.Collections.Concurrent;
using SmartCampusOS.ApiGateway.Infrastructure;

/// <summary>
/// 内存版 ICache，用于中间件单元测试（对齐 LLD §12：不依赖真实 Redis）。
/// 中间件传入的 key 已含固定窗口时间戳，因此仅需原子计数；可配置访问异常以模拟 Redis 故障降级。
/// </summary>
public sealed class InMemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, long> _store = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _expirations = new();

    /// <summary>置 true 后所有访问抛异常，用于验证 FailOpen 降级路径。</summary>
    public bool FailOnAccess { get; set; }

    /// <summary>直接写入一个键（模拟 Redis SET），供黑名单等测试预置数据。</summary>
    public void SetKey(string key) => _store.TryAdd(key, 1);

    public Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
    {
        EnsureAvailable();
        return Task.FromResult(_store.ContainsKey(key));
    }

    public Task<bool> TryAcquireAsync(string key, long limit, int windowSeconds, CancellationToken ct = default)
    {
        EnsureAvailable();
        var now = DateTimeOffset.UtcNow;

        // 窗口内过期则重置（key 已含窗口时间戳，此处为防御性清理）
        if (_expirations.TryGetValue(key, out var expiresAt) && expiresAt <= now)
        {
            _store.TryRemove(key, out _);
            _expirations.TryRemove(key, out _);
        }

        var count = _store.AddOrUpdate(key, 1, (_, current) => current + 1);
        _expirations.TryAdd(key, now.AddSeconds(windowSeconds));
        return Task.FromResult(count <= limit);
    }

    private void EnsureAvailable()
    {
        if (FailOnAccess)
        {
            throw new InvalidOperationException("模拟 Redis 不可用");
        }
    }
}
