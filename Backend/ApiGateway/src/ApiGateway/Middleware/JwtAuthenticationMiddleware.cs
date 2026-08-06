namespace SmartCampusOS.ApiGateway.Middleware;

using System.Security.Claims;
using Microsoft.Extensions.Options;
using SmartCampusOS.ApiGateway.Common;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Infrastructure;
using SmartCampusOS.ApiGateway.Security;

/// <summary>
/// JWT 鉴权中间件（LLD §3.1 / §6.1 / §8.1）：
/// 1. 白名单路径免鉴权直接放行；
/// 2. 校验 Bearer JWT（签名 / 有效期）；
/// 3. 校验 Redis 令牌黑名单（登出/改密后令牌失效）；
/// 4. 校验通过后注入 X-User-Id / X-User-Roles / X-Data-Scope 透传下游。
/// 网关只鉴权不授权，RBAC 数据权限由下游服务按 X-Data-Scope 执行（LLD §8.2）。
/// </summary>
public sealed class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatewayOptions _options;
    private readonly JwtTokenValidator _validator;
    private readonly ICache _cache;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<GatewayOptions> options,
        JwtTokenValidator validator,
        ICache cache,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _validator = validator;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsWhitelisted(path))
        {
            await _next(context);
            return;
        }

        // SignalR 等 WebSocket 场景令牌可能经 query string 传递
        var token = ResolveToken(context);
        if (string.IsNullOrEmpty(token))
        {
            await ErrorResponseWriter.WriteAsync(context, ApiErrorCodes.InvalidCredentials);
            return;
        }

        var principal = _validator.Validate(token);
        if (principal is null)
        {
            var code = JwtTokenValidator.IsExpired(token) ? ApiErrorCodes.TokenExpired : ApiErrorCodes.InvalidCredentials;
            await ErrorResponseWriter.WriteAsync(context, code);
            return;
        }

        var jti = principal.FindFirstValue(ClaimsConstants.JwtId);
        if (!string.IsNullOrEmpty(jti) && await IsBlacklistedAsync(jti, context.RequestAborted) is { } blacklisted && blacklisted)
        {
            _logger.LogWarning("已吊销令牌被使用 jti={Jti}", jti);
            await ErrorResponseWriter.WriteAsync(context, ApiErrorCodes.InvalidCredentials, "令牌已失效，请重新登录");
            return;
        }

        InjectUserContext(context, principal);
        await _next(context);
    }

    private bool IsWhitelisted(string path)
    {
        foreach (var prefix in _options.Whitelist)
        {
            if (string.IsNullOrEmpty(prefix)) continue;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string? ResolveToken(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return header["Bearer ".Length..].Trim();
        }

        // SignalR 协商：access_token query 参数
        if (context.Request.Query.TryGetValue("access_token", out var queryToken) && !string.IsNullOrEmpty(queryToken))
        {
            return queryToken.ToString();
        }

        return null;
    }

    /// <summary>黑名单查询；Redis 异常时按 FailOpen 策略处理（null 表示降级放行并已记告警）。</summary>
    private async Task<bool?> IsBlacklistedAsync(string jti, CancellationToken ct)
    {
        try
        {
            return await _cache.KeyExistsAsync($"{_options.Blacklist.KeyPrefix}:{jti}", ct);
        }
        catch (Exception ex)
        {
            if (_options.Blacklist.FailOpen)
            {
                _logger.LogError(ex, "令牌黑名单查询失败，按 FailOpen 放行（jti={Jti}）", jti);
                return null;
            }
            _logger.LogError(ex, "令牌黑名单查询失败，按安全模式拒绝（jti={Jti}）", jti);
            return true;
        }
    }

    private static void InjectUserContext(HttpContext context, ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimsConstants.Subject);
        if (!string.IsNullOrEmpty(userId))
        {
            context.Request.Headers[ClaimsConstants.HeaderUserId] = userId;
        }

        var roles = string.Join(',', principal.FindAll(ClaimsConstants.Roles).Select(c => c.Value));
        if (!string.IsNullOrEmpty(roles))
        {
            context.Request.Headers[ClaimsConstants.HeaderUserRoles] = roles;
        }

        var dataScope = principal.FindFirstValue(ClaimsConstants.DataScope);
        if (!string.IsNullOrEmpty(dataScope))
        {
            context.Request.Headers[ClaimsConstants.HeaderDataScope] = dataScope;
        }
    }
}
