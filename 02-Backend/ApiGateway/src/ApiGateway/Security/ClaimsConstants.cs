namespace SmartCampusOS.ApiGateway.Security;

/// <summary>
/// JWT Claim 名与网关透传 Header 名约定（LLD §3.1 / §6.1 / §8.2）。
/// IdentityService 签发 JWT 时写入 sub / roles / data_scope；网关校验后以 X-* 头透传下游。
/// </summary>
public static class ClaimsConstants
{
    public const string Subject = "sub";                        // user_id（雪花 ID 字符串）
    public const string Roles = "roles";                        // 逗号分隔的角色 code 列表（支持多角色，§8.1）
    public const string DataScope = "data_scope";               // ALL / GRADE / DEPT / CLASS / SELF
    public const string JwtId = "jti";                          // 令牌唯一 ID，登出黑名单用（§8.1）

    // 网关注入的下游透传 Header（下游服务经 ICurrentUser 读取，LLD §2.6.4）
    public const string HeaderUserId = "X-User-Id";
    public const string HeaderUserRoles = "X-User-Roles";
    public const string HeaderDataScope = "X-Data-Scope";
}
