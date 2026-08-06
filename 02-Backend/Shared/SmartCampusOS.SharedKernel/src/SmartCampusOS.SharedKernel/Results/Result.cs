namespace SmartCampusOS.SharedKernel.Results;

/// <summary>
/// 无返回值的结果对象：表示用例执行成功或失败，失败时携带统一错误码与消息（LLD §11）。
/// </summary>
/// <remarks>
/// 供 Application 层用例方法返回，不直接用于 HTTP 传输；HTTP 层统一用 <see cref="ApiResponse{T}"/> 包装。
/// </remarks>
public sealed class Result
{
    /// <summary>是否成功。</summary>
    public bool IsSuccess { get; }

    /// <summary>失败时的统一错误码（对齐 LLD §11），成功时为 <c>null</c>。</summary>
    public string? ErrorCode { get; }

    /// <summary>失败时的用户可读消息，成功时为 <c>null</c>。</summary>
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>创建成功结果。</summary>
    public static Result Ok() => new(true, null, null);

    /// <summary>创建失败结果。</summary>
    /// <param name="errorCode">统一错误码（LLD §11，如 <c>EDU-2001</c>）。</param>
    /// <param name="errorMessage">用户可读的错误消息。</param>
    public static Result Fail(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);

    /// <summary>失败时抛出 <see cref="InvalidOperationException"/>，成功时直接返回。用于强制成功路径。</summary>
    public void ThrowIfFailed()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException($"操作失败：{ErrorCode} {ErrorMessage}");
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({ErrorCode}: {ErrorMessage})";
}

/// <summary>
/// 带返回值的结果对象：<typeparamref name="T"/> 为成功时的载荷（LLD §5.1 <c>data</c>）。
/// </summary>
/// <typeparam name="T">成功载荷类型。</typeparam>
public sealed class Result<T>
{
    /// <summary>是否成功。</summary>
    public bool IsSuccess { get; }

    /// <summary>成功载荷；失败时为 <c>default</c>。</summary>
    public T? Value { get; }

    /// <summary>失败时的统一错误码（对齐 LLD §11），成功时为 <c>null</c>。</summary>
    public string? ErrorCode { get; }

    /// <summary>失败时的用户可读消息，成功时为 <c>null</c>。</summary>
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>创建成功结果。</summary>
    public static Result<T> Ok(T value) => new(true, value, null, null);

    /// <summary>创建失败结果。</summary>
    /// <param name="errorCode">统一错误码（LLD §11，如 <c>AUTH-1003</c>）。</param>
    /// <param name="errorMessage">用户可读的错误消息。</param>
    public static Result<T> Fail(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);

    /// <summary>从无返回值结果转换（失败信息透传）。</summary>
    public static Result<T> From(Result result) =>
        result.IsSuccess
            ? throw new InvalidOperationException("无返回值结果成功时无法生成载荷，请使用 Ok(value)。")
            : new Result<T>(false, default, result.ErrorCode, result.ErrorMessage);

    /// <summary>成功时返回载荷，失败时抛出 <see cref="InvalidOperationException"/>。</summary>
    public T GetValueOrThrow()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException($"操作失败：{ErrorCode} {ErrorMessage}");
        }

        return Value!;
    }

    /// <summary>成功时返回载荷，失败时返回 <paramref name="fallback"/>。</summary>
    public T GetValueOrDefault(T fallback) => IsSuccess ? Value! : fallback;

    /// <summary>隐式转换：值可直接作为成功结果返回。</summary>
    public static implicit operator Result<T>(T value) => Ok(value);

    /// <summary>成功时执行 <paramref name="success"/>，失败时执行 <paramref name="failure"/>。</summary>
    public TResult Match<TResult>(Func<T, TResult> success, Func<string, string, TResult> failure) =>
        IsSuccess ? success(Value!) : failure(ErrorCode!, ErrorMessage!);

    /// <inheritdoc />
    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({ErrorCode}: {ErrorMessage})";
}
