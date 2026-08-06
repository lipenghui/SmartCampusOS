namespace IdentityService.Infrastructure.Security;

/// <summary>
/// JWT Claim 名常量，与 ApiGateway Security/ClaimsConstants.cs 完全对齐（LLD §3.1 / §8.1）。
/// IdentityService 签发时必须使用这些 claim 名，网关才能正确校验与透传。
/// </summary>
public static class TokenClaims
{
    /// <summary>主题：用户 ID（雪花 ID 字符串）。</summary>
    public const string Subject = "sub";

    /// <summary>角色编码列表（逗号分隔，支持多角色，§8.1）。</summary>
    public const string Roles = "roles";

    /// <summary>数据范围（ALL / GRADE / DEPT / CLASS / SELF，§8.2）。</summary>
    public const string DataScope = "data_scope";

    /// <summary>令牌唯一 ID（登出黑名单用，§8.1）。</summary>
    public const string JwtId = "jti";
}
