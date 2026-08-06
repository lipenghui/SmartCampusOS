namespace SmartCampusOS.SharedKernel.Results;

/// <summary>
/// 全局统一响应结构，对齐 LLD §5.1：<c>{ "code": 0, "message": "ok", "data": {...} }</c>。
/// </summary>
/// <typeparam name="T">成功载荷类型（<c>data</c>）。</typeparam>
/// <param name="Code">统一响应码：<c>"0"</c> 成功、<c>"-1"</c> 系统异常，其余为 LLD §11 业务错误码。</param>
/// <param name="Message">提示消息，成功默认 "ok"。</param>
/// <param name="Data">成功载荷；失败时为 <c>default</c>（序列化时省略，避免暴露内部细节）。</param>
public sealed record ApiResponse<T>(string Code, string Message, T? Data)
{
    /// <summary>创建成功响应（<c>code = "0"</c>）。</summary>
    public static ApiResponse<T> Ok(T data, string message = "ok") =>
        new(ErrorCodes.Success, message, data);

    /// <summary>创建失败响应：<c>code</c> 为 LLD §11 业务错误码。</summary>
    public static ApiResponse<T> Fail(string errorCode, string message, T? data = default) =>
        new(errorCode, message, data);

    /// <summary>从 <see cref="Result{T}"/> 转换：成功透传载荷，失败透传错误码与消息。</summary>
    public static ApiResponse<T> From(Result<T> result) =>
        result.IsSuccess
            ? Ok(result.Value!)
            : new ApiResponse<T>(result.ErrorCode!, result.ErrorMessage ?? "操作失败", default);

    /// <summary>是否成功（<c>code = "0"</c>）。</summary>
    public bool IsSuccess => Code == ErrorCodes.Success;
}

/// <summary>
/// 无载荷的统一响应，对齐 LLD §5.1：<c>{ "code": 0, "message": "ok" }</c>。
/// </summary>
public static class ApiResponse
{
    /// <summary>创建成功响应（<c>code = "0"</c>）。</summary>
    public static ApiResponse<object?> Ok(string message = "ok") =>
        new(ErrorCodes.Success, message, null);

    /// <summary>创建失败响应：<c>code</c> 为 LLD §11 业务错误码。</summary>
    public static ApiResponse<object?> Fail(string errorCode, string message) =>
        new(errorCode, message, null);

    /// <summary>从无返回值 <see cref="Result"/> 转换。</summary>
    public static ApiResponse<object?> From(Result result) =>
        result.IsSuccess ? Ok() : Fail(result.ErrorCode!, result.ErrorMessage ?? "操作失败");
}
