using System.Text.Json;
using IdentityService.Application.Abstractions.Caching;
using IdentityService.Application.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IdentityService.Infrastructure.Caching;

/// <summary>
/// Redis 缓存实现（LLD §9：令牌黑名单、字典/组织树缓存、会话）。
/// 连接失败或单次操作异常时降级返回（可用性优先，与网关 Redis FailOpen 思路一致），并记录告警日志。
/// </summary>
public sealed class RedisCache : ICache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCache> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisCache(IConnectionMultiplexer redis, ILogger<RedisCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _redis.GetDatabase().StringGetAsync(key).WaitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET 失败（降级返回 null）：{Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var expiration = expiry.HasValue ? expiry.Value : Expiration.Persist;
            await _redis.GetDatabase().StringSetAsync(key, value, expiration).WaitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET 失败（降级跳过）：{Key}", key);
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _redis.GetDatabase().KeyDeleteAsync(key).WaitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DEL 失败（降级返回 false）：{Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _redis.GetDatabase().KeyExistsAsync(key).WaitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS 失败（降级返回 false）：{Key}", key);
            return false;
        }
    }
}
