using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.Abstractions.Security;

/// <summary>
/// 密码哈希抽象（LLD §8.3 / 安全原语决策）：实现为 PBKDF2-SHA256 + 随机盐。
/// 用例层不感知算法细节。
/// </summary>
public interface IPasswordHasher
{
    /// <summary>生成密码哈希与盐。</summary>
    /// <param name="password">明文密码。</param>
    /// <param name="salt">输出的随机盐（Base64）。</param>
    /// <returns>密码哈希（Base64）。</returns>
    string Hash(string password, out string salt);

    /// <summary>校验密码与存储的哈希/盐是否匹配。</summary>
    bool Verify(string password, string hash, string salt);
}

/// <summary>
/// 访问令牌签发抽象：签发与网关校验契约一致的 JWT（LLD §8.1，HS256）。
/// </summary>
public interface IJwtTokenFactory
{
    /// <summary>签发访问令牌（JWT）。</summary>
    /// <param name="userId">用户 ID（雪花 ID）。</param>
    /// <param name="roles">角色编码列表（支持多角色，LLD §8.1）。</param>
    /// <param name="dataScope">数据范围（LLD §8.2）。</param>
    AccessTokenResult CreateAccessToken(long userId, IReadOnlyList<string> roles, DataScope dataScope);
}

/// <summary>访问令牌结果。</summary>
/// <param name="Token">JWT 字符串。</param>
/// <param name="ExpiresAt">过期时间（UTC）。</param>
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// 敏感字段加解密抽象（LLD §8.3：手机号等 AES-256 加密存储）。
/// </summary>
public interface IFieldEncryptor
{
    /// <summary>加密明文；null/空白返回 null。</summary>
    string? Encrypt(string? plain);

    /// <summary>解密密文；null/空白返回 null。</summary>
    string? Decrypt(string? cipher);
}

/// <summary>
/// 刷新令牌哈希抽象（LLD §8.1 / §4.2 sys_refresh_token.token_hash：不存明文）。
/// </summary>
public interface IRefreshTokenHasher
{
    /// <summary>计算刷新令牌哈希。</summary>
    string Hash(string token);
}
