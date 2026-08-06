using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Application.Common;

/// <summary>
/// 业务异常：携带 LLD §11 统一错误码，由 Api 层异常中间件转为统一响应结构。
/// 用于用例/领域服务中需要中断且携带错误码的场景；参数级校验仍走 FluentValidation。
/// </summary>
public sealed class BusinessException : Exception
{
    /// <summary>统一错误码（LLD §11，如 AUTH-1001、COMMON-2003）。</summary>
    public string ErrorCode { get; }

    public BusinessException(string errorCode, string message) : base(message)
        => ErrorCode = errorCode;

    public static BusinessException NotFound(string message = "数据不存在") =>
        new(ErrorCodes.CommonNotFound, message);

    public static BusinessException Validation(string message) =>
        new(ErrorCodes.CommonValidationFailed, message);
}
