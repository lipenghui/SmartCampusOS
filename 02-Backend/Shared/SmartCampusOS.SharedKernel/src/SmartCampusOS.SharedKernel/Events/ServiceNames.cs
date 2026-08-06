namespace SmartCampusOS.SharedKernel.Events;

/// <summary>
/// 服务名常量：标识事件来源与网关路由前缀（LLD §2.3 服务划分 / §7 事件 sourceService）。
/// </summary>
public static class ServiceNames
{
    /// <summary>API 网关（无状态，不产生领域事件）。</summary>
    public const string ApiGateway = "api-gateway";

    /// <summary>认证授权服务。</summary>
    public const string Identity = "identity";

    /// <summary>教务服务。</summary>
    public const string Edu = "edu";

    /// <summary>宿舍后勤服务。</summary>
    public const string Dorm = "dorm";

    /// <summary>通知家校服务。</summary>
    public const string Notice = "notice";

    /// <summary>数据可视化服务。</summary>
    public const string Data = "data";

    /// <summary>推送网关。</summary>
    public const string Push = "push";

    /// <summary>文件服务。</summary>
    public const string File = "file";
}
