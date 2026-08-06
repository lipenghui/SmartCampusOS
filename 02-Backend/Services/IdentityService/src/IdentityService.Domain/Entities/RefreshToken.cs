using IdentityService.Domain.Common;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 刷新令牌实体（表 sys_refresh_token，LLD §4.2 / §8.1）：存储令牌哈希，支持 7 天续期、撤销。
/// </summary>
[EntityTable("sys_refresh_token")]
public sealed class RefreshToken : AuditableEntity
{
    /// <summary>所属用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>刷新令牌哈希（SHA-256，不存明文）。</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>过期时间（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>是否已撤销（登出/轮换后置真）。</summary>
    public bool Revoked { get; set; }

    /// <summary>撤销时间（UTC）。</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>令牌当前是否可用：未撤销且未过期。</summary>
    public bool IsUsable(DateTimeOffset now) =>
        !Revoked && ExpiresAt > now;
}
