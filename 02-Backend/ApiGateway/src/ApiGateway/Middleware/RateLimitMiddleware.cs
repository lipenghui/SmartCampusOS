namespace SmartCampusOS.ApiGateway.Middleware;

using Microsoft.Extensions.Options;
using SmartCampusOS.ApiGateway.Common;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Infrastructure;
using SmartCampusOS.ApiGateway.Security;

/// <summary>
/// 限流中间件（LLD §3.1 / PRD R-04）：
/// - 已认证请求按用户限流（X-User-Id），未认证（登录/验证码等白名单）按客户端 IP 限流；
/// - 默认 10 req/s/用户，选课 / 成绩发布等峰值接口经 Overrides 放宽；
/// - 固定窗口计数（Redis Lua 原子操作），超限返回 429 + COMMON-2001；
/// - Redis 不可用时按 FailOpen 放行并告警（避免 Redis 抖动拖垮网关）。
/// </summary>
public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private readonly ICache _cache;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        IOptions<GatewayOptions> options,
        ICache cache,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _options = options.Value.RateLimit;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        var scopeKey = ResolveScopeKey(context);
        var rps = ResolveLimit(path);
        var windowKey = $"{scopeKey}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var cacheKey = $"gateway:ratelimit:{windowKey}";

        try
        {
            var allowed = await _cache.TryAcquireAsync(cacheKey, rps, _options.WindowSeconds, context.RequestAborted);
            if (!allowed)
            {
                context.Response.Headers.RetryAfter = "1";
                _logger.LogWarning("限流触发 Path={Path} Scope={Scope} Limit={Rps}/s", path, scopeKey, rps);
                await ErrorResponseWriter.WriteAsync(context, ApiErrorCodes.RateLimited);
                return;
            }
        }
        catch (Exception ex)
        {
            if (!_options.FailOpen)
            {
                await ErrorResponseWriter.WriteAsync(context, ApiErrorCodes.RateLimited);
                return;
            }
            _logger.LogError(ex, "限流计数失败，按 FailOpen 放行 Path={Path} Scope={Scope}", path, scopeKey);
        }

        await _next(context);
    }

    private bool IsExempt(string path)
    {
        foreach (var prefix in _options.PathExemptions)
        {
            if (!string.IsNullOrEmpty(prefix) && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string ResolveScopeKey(HttpContext context)
    {
        // 已认证请求（JWT 中间件已注入 X-User-Id）按用户限流，否则按 IP
        var userId = context.Request.Headers[ClaimsConstants.HeaderUserId].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            return $"u:{userId}";
        }
        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private int ResolveLimit(string path)
    {
        // 最长前缀匹配：选课/成绩发布等高峰接口的 Overrides 优先于默认值
        RateLimitOverride? best = null;
        foreach (var item in _options.Overrides)
        {
            if (string.IsNullOrEmpty(item.PathPrefix)) continue;
            if (path.StartsWith(item.PathPrefix, StringComparison.OrdinalIgnoreCase)
                && (best is null || item.PathPrefix.Length > best.PathPrefix.Length))
            {
                best = item;
            }
        }
        return best?.Rps > 0 ? best.Rps : _options.DefaultRps;
    }
}
