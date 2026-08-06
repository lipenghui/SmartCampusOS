namespace SmartCampusOS.ApiGateway.Common;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 网关统一错误响应写入器：按错误码输出 LLD §5.1 约定的 { code, message, data } JSON。
/// </summary>
public static class ErrorResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteAsync(HttpContext context, string code, string? message = null, CancellationToken ct = default)
    {
        var (status, defaultMessage) = ApiErrors.Resolve(code);
        var error = new ApiError(code, message ?? defaultMessage);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions), ct);
    }
}
