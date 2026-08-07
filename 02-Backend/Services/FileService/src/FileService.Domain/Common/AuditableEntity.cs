namespace FileService.Domain.Common;

/// <summary>
/// 审计字段基类：主键雪花 ID + 通用审计字段（LLD §4.1 约定：created_at / updated_at / created_by / updated_by / is_deleted）。
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>主键（BIGINT 雪花 ID，LLD §1.4）。</summary>
    public long Id { get; set; }

    /// <summary>创建时间（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>更新时间（UTC）。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>创建人（雪花 ID）。</summary>
    public long? CreatedBy { get; set; }

    /// <summary>更新人（雪花 ID）。</summary>
    public long? UpdatedBy { get; set; }

    /// <summary>软删除标记（LLD §4.1）。</summary>
    public bool IsDeleted { get; set; }
}