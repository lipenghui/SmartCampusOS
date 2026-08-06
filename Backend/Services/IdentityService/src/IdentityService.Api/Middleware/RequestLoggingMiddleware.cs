using System.Diagnostics;
using IdentityService.Api.Common;

namespace IdentityService.Api.Middleware;

/// <summary>
/// 请求日志中间件（LLD §10.3 可观测）：生成 / 透传 X-Request-Id，记录 method、path、状态码、耗时、用户、客户端 IP。
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[HeaderNames.RequestId].ToString();
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
            context.Request.Headers[HeaderNames.RequestId] = requestId;
        }

        context.Response.Headers[HeaderNames.RequestId] = requestId;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var userId = context.Request.Headers[HeaderNames.UserId].ToString();
            _logger.LogInformation(
                "HTTP {Method} {Path} -> {StatusCode} {ElapsedMs}ms RequestId={RequestId} UserId={UserId} ClientIp={ClientIp}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                requestId,
                string.IsNullOrEmpty(userId) ? "-" : userId,
                context.Connection.RemoteIpAddress?.ToString() ?? "-");
        }
    }
}
