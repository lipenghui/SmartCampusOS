namespace SmartCampusOS.ApiGateway.Middleware;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Security;

/// <summary>
/// 灰度发布中间件（LLD §10.4：网关按 Header / 用户比例引流，失败一键回滚）：
/// 1. 客户端 / CI 显式携带 X-Gray-Tag 时直接采用（按 Header 引流）；
/// 2. 否则按路径规则 + 稳定 hash（用户 ID / IP）百分比引流；
/// 3. 命中规则则注入 X-Gray-Version 头，下游服务据此选择版本 / 分支。
/// 未命中规则或灰度关闭时不注入任何头。
/// </summary>
public sealed class GrayReleaseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GrayReleaseOptions _options;
    private readonly ILogger<GrayReleaseMiddleware> _logger;

    public GrayReleaseMiddleware(
        RequestDelegate next,
        IOptions<GatewayOptions> options,
        ILogger<GrayReleaseMiddleware> logger)
    {
        _next = next;
        _options = options.Value.GrayRelease;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Enabled && !context.Request.Headers.ContainsKey(_options.VersionHeader))
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // 显式灰度标签优先（CI 冒烟 / 内部测试指定）
            var explicitTag = context.Request.Headers[_options.SourceHeader].ToString();
            if (!string.IsNullOrEmpty(explicitTag))
            {
                context.Request.Headers[_options.VersionHeader] = explicitTag;
            }
            else if (TryResolveRule(path) is { } rule)
            {
                var bucket = StableBucket(context);
                if (bucket < rule.Percent)
                {
                    context.Request.Headers[_options.VersionHeader] = rule.Version;
                    _logger.LogDebug("灰度引流 Path={Path} Bucket={Bucket}% Version={Version}", path, bucket, rule.Version);
                }
            }
        }

        await _next(context);
    }

    private GrayReleaseRule? TryResolveRule(string path)
    {
        GrayReleaseRule? best = null;
        foreach (var rule in _options.Rules)
        {
            if (string.IsNullOrEmpty(rule.PathPrefix)) continue;
            if (path.StartsWith(rule.PathPrefix, StringComparison.OrdinalIgnoreCase)
                && (best is null || rule.PathPrefix.Length > best.PathPrefix.Length))
            {
                best = rule;
            }
        }
        return best;
    }

    /// <summary>稳定分流桶：同一用户 / IP 在同一路径下始终命中同一灰度桶（0-99）。</summary>
    private static int StableBucket(HttpContext context)
    {
        var identity = context.Request.Headers[ClaimsConstants.HeaderUserId].ToString();
        if (string.IsNullOrEmpty(identity))
        {
            identity = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
        var path = context.Request.Path.Value ?? string.Empty;
        var seed = Encoding.UTF8.GetBytes($"{identity}|{path}");
        var hash = SHA256.HashData(seed);
        var unsigned = BitConverter.ToInt32(hash.AsSpan(0, 4)) & 0x7FFFFFFF;
        return unsigned % 100;
    }
}
