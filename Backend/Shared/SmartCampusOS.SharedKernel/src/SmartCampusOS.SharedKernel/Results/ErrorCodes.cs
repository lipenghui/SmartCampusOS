namespace SmartCampusOS.SharedKernel.Results;

/// <summary>
/// 全局统一错误码，对齐 LLD §11 错误码约定。
/// </summary>
/// <remarks>
/// 编码规则：<c>code=0</c> 成功、<c>-1</c> 系统异常（不向前端暴露内部细节）；
/// 其余按服务前缀分组：AUTH-1xxx 认证授权、EDU-2xxx 教务、DORM-3xxx 宿舍后勤、
/// NOTI-4xxx 通知家校、DATA-5xxx 数据可视化、PUSH-6xxx 推送、COMMON-2xxx 通用。
/// </remarks>
public static class ErrorCodes
{
    /// <summary>成功（LLD §5.1：code=0）。</summary>
    public const string Success = "0";

    /// <summary>系统异常（LLD §11：-1，不向前端暴露内部细节）。</summary>
    public const string SystemError = "-1";

    // ── 认证授权 AUTH-1xxx ──
    /// <summary>凭证错误（LLD §11 AUTH-1001）。</summary>
    public const string AuthInvalidCredentials = "AUTH-1001";

    /// <summary>令牌过期（LLD §11 AUTH-1002）。</summary>
    public const string AuthTokenExpired = "AUTH-1002";

    /// <summary>无权限（LLD §11 AUTH-1003）。</summary>
    public const string AuthForbidden = "AUTH-1003";

    // ── 教务 EDU-2xxx ──
    /// <summary>排课冲突（LLD §11 EDU-2001）。</summary>
    public const string EduScheduleConflict = "EDU-2001";

    /// <summary>选课名额已满（LLD §11 EDU-2002）。</summary>
    public const string EduSelectionFull = "EDU-2002";

    /// <summary>成绩已锁定（LLD §11 EDU-2003）。</summary>
    public const string EduScoreLocked = "EDU-2003";

    // ── 宿舍后勤 DORM-3xxx ──
    /// <summary>床位不可用（LLD §11 DORM-3001）。</summary>
    public const string DormBedUnavailable = "DORM-3001";

    /// <summary>场地冲突（LLD §11 DORM-3002）。</summary>
    public const string DormVenueConflict = "DORM-3002";

    /// <summary>工单状态非法（LLD §11 DORM-3003）。</summary>
    public const string DormOrderInvalidState = "DORM-3003";

    // ── 通知家校 NOTI-4xxx ──
    /// <summary>接收人未授权（LLD §11 NOTI-4001）。</summary>
    public const string NotiRecipientUnauthorized = "NOTI-4001";

    /// <summary>敏感词拦截（LLD §11 NOTI-4002）。</summary>
    public const string NotiSensitiveWordBlocked = "NOTI-4002";

    // ── 数据可视化 DATA-5xxx ──
    /// <summary>快照不存在（LLD §11 DATA-5001）。</summary>
    public const string DataSnapshotNotFound = "DATA-5001";

    /// <summary>报表生成中（LLD §11 DATA-5002）。</summary>
    public const string DataReportGenerating = "DATA-5002";

    // ── 推送 PUSH-6xxx ──
    /// <summary>渠道不可用（LLD §11 PUSH-6001）。</summary>
    public const string PushChannelUnavailable = "PUSH-6001";

    /// <summary>短信预算不足（LLD §11 PUSH-6002）。</summary>
    public const string PushSmsBudgetExceeded = "PUSH-6002";

    // ── 通用 COMMON-2xxx ──
    /// <summary>请求过于频繁（LLD §11 COMMON-2001）。</summary>
    public const string CommonRateLimited = "COMMON-2001";

    /// <summary>参数校验失败（LLD §11 COMMON-2002）。</summary>
    public const string CommonValidationFailed = "COMMON-2002";

    /// <summary>数据不存在（LLD §11 COMMON-2003）。</summary>
    public const string CommonNotFound = "COMMON-2003";

    /// <summary>上游服务暂不可用（网关扩展，对齐 ApiGateway COMMON-2004）。</summary>
    public const string CommonUpstreamUnavailable = "COMMON-2004";
}
