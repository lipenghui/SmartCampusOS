using IdentityService.Domain.Common;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 角色实体（表 sys_role，LLD §4.2 / §8.2 RBAC）：角色 → 权限点 → 数据范围。
/// 内置角色（builtin=true）不可删除、不可修改编码。
/// </summary>
[EntityTable("sys_role")]
public sealed class Role : AuditableEntity
{
    /// <summary>角色编码（如 admin / teacher / student / parent / staff，全局唯一）。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>角色名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>数据范围（LLD §8.2 四档：ALL/GRADE/DEPT·CLASS/SELF）。</summary>
    public DataScope DataScope { get; set; } = DataScope.Self;

    /// <summary>是否内置角色（内置不可删改编码）。</summary>
    public bool Builtin { get; set; }

    /// <summary>内置角色禁止修改编码。</summary>
    public void EnsureCodeChangeAllowed(string newCode)
    {
        if (Builtin && !string.Equals(Code, newCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("内置角色编码不可修改");
        }
    }

    /// <summary>内置角色禁止删除。</summary>
    public void EnsureDeletable()
    {
        if (Builtin)
        {
            throw new InvalidOperationException("内置角色不可删除");
        }
    }
}
