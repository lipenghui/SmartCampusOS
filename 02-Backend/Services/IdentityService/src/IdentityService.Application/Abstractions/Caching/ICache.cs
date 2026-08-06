namespace IdentityService.Application.Abstractions.Caching;

/// <summary>
/// 缓存抽象（LLD §9：令牌黑名单、数据字典/组织树缓存等）。
/// Redis 不可用时实现自行降级（可用性优先，对齐网关 FailOpen 思路）。
/// </summary>
public interface ICache
{
    /// <summary>读取字符串缓存；未命中返回 null。</summary>
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);

    /// <summary>写入字符串缓存（可选过期时间）。</summary>
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>删除缓存；存在并删除返回 true。</summary>
    Task<bool> RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>键是否存在。</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
