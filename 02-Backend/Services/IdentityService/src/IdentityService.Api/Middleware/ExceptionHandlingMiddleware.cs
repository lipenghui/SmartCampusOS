using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityService.Application.Common;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Middleware;

/// <summary>
/// 全局异常处理中间件：未处理异常统一转换为 LLD §5.1 响应结构 { code, message, data }。
/// <see cref="BusinessException"/> 透传业务错误码；其余异常记错误日志并返回 -1（不暴露内部细节）。
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("业务异常 {ErrorCode}: {Message} Path={Path}", ex.ErrorCode, ex.Message, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status400BadRequest, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常 Path={Path} Method={Method}", context.Request.Path, context.Request.Method);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, ErrorCodes.SystemError, "系统异常，请稍后重试");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = JsonSerializer.Serialize(ApiResponse.Fail(code, message), JsonOptions);
        await context.Response.WriteAsync(body);
    }
}
