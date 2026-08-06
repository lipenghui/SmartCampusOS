namespace SmartCampusOS.ApiGateway.Infrastructure;

/// <summary>
/// 网关缓存抽象。实现（Redis）不可用时抛异常，由调用方按 FailOpen 策略降级。
/// 独立抽象便于单元测试使用内存实现（对齐 LLD §12 测试策略）。
/// </summary>
public interface ICache
{
    /// <summary>键是否存在（用于令牌黑名单检查）。</summary>
    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// 固定窗口原子计数：窗口内计数未超 <paramref name="limit"/> 返回 true，否则 false。
    /// 首次计数时设置 <paramref name="windowSeconds"/> 过期。
    /// </summary>
    Task<bool> TryAcquireAsync(string key, long limit, int windowSeconds, CancellationToken ct = default);
}
