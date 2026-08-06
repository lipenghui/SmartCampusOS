namespace SmartCampusOS.ApiGateway.Middleware;

using System.Diagnostics;
using SmartCampusOS.ApiGateway.Security;

/// <summary>
/// 请求日志中间件（LLD §10.3：结构化 JSON 日志，全链路可观测）：
/// 生成 / 透传 X-Request-Id，记录 method、path、状态码、耗时、用户、客户端 IP。
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
        var requestId = context.Request.Headers["X-Request-Id"].ToString();
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
            context.Request.Headers["X-Request-Id"] = requestId;
        }
        context.Response.Headers["X-Request-Id"] = requestId;

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var userId = context.Request.Headers[ClaimsConstants.HeaderUserId].ToString();
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
