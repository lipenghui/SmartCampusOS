namespace SmartCampusOS.ApiGateway.Middleware;

using SmartCampusOS.ApiGateway.Common;
using Yarp.ReverseProxy.Forwarder;

/// <summary>
/// YARP 转发错误统一处理（注册在 MapReverseProxy 管道内，LLD §5.1 / §11）：
/// 上游不可达 / 超时等返回 502 + COMMON-2004，避免向上游客户端暴露内部细节。
/// </summary>
public sealed class UpstreamErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UpstreamErrorHandlingMiddleware> _logger;

    public UpstreamErrorHandlingMiddleware(RequestDelegate next, ILogger<UpstreamErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // YARP 转发失败时在 HttpContext 上挂载 IForwarderErrorFeature（未失败则为 null）
        if (context.GetForwarderErrorFeature() is { } errorFeature)
        {
            _logger.LogError(errorFeature.Exception, "上游转发失败 Path={Path} Error={Error}",
                context.Request.Path, errorFeature.Error);

            if (!context.Response.HasStarted)
            {
                await ErrorResponseWriter.WriteAsync(context, ApiErrorCodes.UpstreamUnavailable,
                    "上游服务暂不可用，请稍后重试", context.RequestAborted);
            }
        }
    }
}
