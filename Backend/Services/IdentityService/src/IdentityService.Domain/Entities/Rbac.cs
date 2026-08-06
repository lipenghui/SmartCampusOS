using IdentityService.Domain.Common;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 权限点实体（表 sys_permission，LLD §4.2 / §8.2）：如 user:create、score:read。
/// </summary>
[EntityTable("sys_permission")]
public sealed class Permission : AuditableEntity
{
    /// <summary>权限点编码（如 user:create，全局唯一）。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>权限点名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>所属模块（如 系统管理/教务）。</summary>
    public string Module { get; set; } = string.Empty;
}

/// <summary>
/// 用户-角色关联实体（表 sys_user_role，LLD §4.2）：支持多角色（LLD §8.1）。
/// </summary>
[EntityTable("sys_user_role")]
public sealed class UserRole : AuditableEntity
{
    /// <summary>用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>角色 ID。</summary>
    public long RoleId { get; set; }
}

/// <summary>
/// 角色-权限点关联实体（表 sys_role_permission，LLD §8.2 RBAC：角色 → 权限点 → 数据范围）。
/// 概念级表清单未单列，按 RBAC 模型补齐；角色通过本表绑定权限点。
/// </summary>
[EntityTable("sys_role_permission")]
public sealed class RolePermission : AuditableEntity
{
    /// <summary>角色 ID。</summary>
    public long RoleId { get; set; }

    /// <summary>权限点 ID。</summary>
    public long PermissionId { get; set; }
}
