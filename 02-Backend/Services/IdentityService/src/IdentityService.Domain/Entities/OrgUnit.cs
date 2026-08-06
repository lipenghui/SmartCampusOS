using IdentityService.Domain.Common;
using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 组织单元实体（表 sys_org，LLD §4.2）：学校/年级/班级/部门组织树。
/// </summary>
[EntityTable("sys_org")]
public sealed class OrgUnit : AuditableEntity
{
    /// <summary>父组织 ID（根节点为 null）。</summary>
    public long? ParentId { get; set; }

    /// <summary>组织类型。</summary>
    public OrgType OrgType { get; set; }

    /// <summary>组织名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>组织编码（全局唯一）。</summary>
    public string Code { get; set; } = string.Empty;
}
