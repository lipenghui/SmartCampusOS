using IdentityService.Domain.Common;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 数据字典实体（表 sys_dict，LLD §4.2）：全局统一字典，由 IdentityService 维护，
/// 其他服务启动时缓存本地副本（LLD §4.3）。
/// </summary>
[EntityTable("sys_dict")]
public sealed class Dict : AuditableEntity
{
    /// <summary>字典编码（如 user_status，全局唯一）。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>字典名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>字典说明。</summary>
    public string? Description { get; set; }
}

/// <summary>
/// 字典项实体（表 sys_dict_item，LLD §4.2）。
/// </summary>
[EntityTable("sys_dict_item")]
public sealed class DictItem : AuditableEntity
{
    /// <summary>所属字典编码。</summary>
    public string DictCode { get; set; } = string.Empty;

    /// <summary>字典项编码。</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>字典项名称。</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>排序号。</summary>
    public int Sort { get; set; }

    /// <summary>是否启用。</summary>
    public bool Enabled { get; set; } = true;
}
