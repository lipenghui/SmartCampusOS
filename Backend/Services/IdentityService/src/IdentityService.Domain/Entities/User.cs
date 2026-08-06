using IdentityService.Domain.Common;
using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 账号实体（表 sys_user，LLD §4.2）：学工号、手机号（加密存储）、密码、类型、状态。
/// </summary>
[EntityTable("sys_user")]
public sealed class User : AuditableEntity
{
    /// <summary>学工号（登录账号）。</summary>
    public string UserNo { get; set; } = string.Empty;

    /// <summary>手机号（AES-256-GCM 加密后存储，LLD §8.3；展示时脱敏）。</summary>
    public string? MobileEncrypted { get; set; }

    /// <summary>手机号 SHA-256 哈希（十六进制）：加密存储无法索引查询，用不可逆哈希做精确匹配（登录/去重）。</summary>
    public string? MobileHash { get; set; }

    /// <summary>密码哈希（PBKDF2-SHA256）。</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>密码盐（Base64）。</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>用户类型。</summary>
    public UserType UserType { get; set; }

    /// <summary>账号状态。</summary>
    public UserStatus Status { get; set; } = UserStatus.Inactive;

    /// <summary>真实姓名。</summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>头像 URL。</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>是否已激活（状态机校验用快捷属性）。</summary>
    public bool IsActive => Status == UserStatus.Active;

    /// <summary>
    /// 激活账号（LLD §3.2 POST /users/{id}/activate）。
    /// 状态机：Inactive → Active；已激活/停用/锁定状态拒绝重复激活。
    /// </summary>
    public void Activate()
    {
        if (Status is UserStatus.Active)
        {
            throw new InvalidOperationException("账号已激活，无需重复操作");
        }

        if (Status is UserStatus.Disabled or UserStatus.Locked)
        {
            throw new InvalidOperationException($"账号当前状态为{Status}，无法激活");
        }

        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>停用账号：Active → Disabled。</summary>
    public void Disable()
    {
        if (Status != UserStatus.Active)
        {
            throw new InvalidOperationException("仅正常状态账号可停用");
        }

        Status = UserStatus.Disabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>启用账号：Disabled → Active。</summary>
    public void Enable()
    {
        if (Status != UserStatus.Disabled)
        {
            throw new InvalidOperationException("仅停用状态账号可启用");
        }

        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>锁定账号：连续登录失败锁定。</summary>
    public void Lock()
    {
        if (Status == UserStatus.Locked)
        {
            return;
        }

        Status = UserStatus.Locked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>解锁账号：Locked → Active。</summary>
    public void Unlock()
    {
        if (Status != UserStatus.Locked)
        {
            throw new InvalidOperationException("仅锁定状态账号可解锁");
        }

        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>账号是否允许登录校验（未停用未锁定）。</summary>
    public bool CanAttemptLogin => Status is UserStatus.Active or UserStatus.Inactive;
}
