namespace SmartCampusOS.ApiGateway.Common;

/// <summary>
/// 网关统一错误码，对齐 LLD §11 错误码约定。
/// code=0 成功；错误码形如 "AUTH-1001"，HTTP 状态码映射见 <see cref="ApiErrors"/>。
/// </summary>
public static class ApiErrorCodes
{
    // 认证授权（LLD §11：AUTH-1xxx）
    public const string InvalidCredentials = "AUTH-1001";
    public const string TokenExpired = "AUTH-1002";
    public const string Forbidden = "AUTH-1003";

    // 通用（LLD §11：COMMON-2xxx）
    public const string RateLimited = "COMMON-2001";
    public const string ValidationFailed = "COMMON-2002";
    public const string NotFound = "COMMON-2003";
    public const string UpstreamUnavailable = "COMMON-2004";
}

/// <summary>网关统一错误响应（LLD §5.1：{ code, message, data }）。</summary>
public sealed record ApiError(string Code, string Message, object? Data = null)
{
    public static ApiError From(string code, string message) => new(code, message);
}

/// <summary>错误码 → HTTP 状态码与默认消息的映射。</summary>
public static class ApiErrors
{
    public static (int Status, string Message) Resolve(string code) => code switch
    {
        ApiErrorCodes.InvalidCredentials => (StatusCodes.Status401Unauthorized, "凭证无效或缺失"),
        ApiErrorCodes.TokenExpired => (StatusCodes.Status401Unauthorized, "令牌已过期"),
        ApiErrorCodes.Forbidden => (StatusCodes.Status403Forbidden, "无权限访问"),
        ApiErrorCodes.RateLimited => (StatusCodes.Status429TooManyRequests, "请求过于频繁"),
        ApiErrorCodes.ValidationFailed => (StatusCodes.Status400BadRequest, "参数校验失败"),
        ApiErrorCodes.NotFound => (StatusCodes.Status404NotFound, "资源不存在"),
        ApiErrorCodes.UpstreamUnavailable => (StatusCodes.Status502BadGateway, "上游服务暂不可用"),
        _ => (StatusCodes.Status500InternalServerError, "系统异常")
    };
}
