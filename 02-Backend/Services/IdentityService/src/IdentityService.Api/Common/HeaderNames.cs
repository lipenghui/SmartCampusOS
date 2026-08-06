namespace IdentityService.Api.Common;

/// <summary>
/// 网关透传 Header 名约定，与 ApiGateway Security/ClaimsConstants.cs 对齐（LLD §3.1 / §6.1 / §8.2）。
/// 服务内不持有 JWT 解析逻辑，仅读取网关注入的 X-* 头。
/// </summary>
public static class HeaderNames
{
    /// <summary>当前用户 ID（雪花 ID 字符串）。</summary>
    public const string UserId = "X-User-Id";

    /// <summary>当前用户角色编码列表（逗号分隔，支持多角色 §8.1）。</summary>
    public const string UserRoles = "X-User-Roles";

    /// <summary>数据范围（ALL / GRADE / DEPT / CLASS / SELF，§8.2）。</summary>
    public const string DataScope = "X-Data-Scope";

    /// <summary>家长端当前访问的子女用户 ID（BR-04）。</summary>
    public const string ChildId = "X-Child-Id";

    /// <summary>请求追踪 ID（网关生成或透传，LLD §10.3）。</summary>
    public const string RequestId = "X-Request-Id";
}
