using IdentityService.Domain.Common;
using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 家长-子女绑定实体（表 sys_parent_binding，LLD §4.2 / BR-04）：
/// 家长账号与子女账号的绑定申请与审核，审核通过后家长方可访问子女数据（X-Child-Id 归属校验）。
/// </summary>
[EntityTable("sys_parent_binding")]
public sealed class ParentBinding : AuditableEntity
{
    /// <summary>家长用户 ID。</summary>
    public long ParentUserId { get; set; }

    /// <summary>学生（子女）用户 ID。</summary>
    public long StudentUserId { get; set; }

    /// <summary>关系（父亲/母亲/其他）。</summary>
    public ParentRelation Relation { get; set; }

    /// <summary>审核状态（待审核/已通过/已驳回）。</summary>
    public AuditStatus AuditStatus { get; set; } = AuditStatus.Pending;

    /// <summary>审核人用户 ID。</summary>
    public long? AuditedBy { get; set; }

    /// <summary>审核时间（UTC）。</summary>
    public DateTimeOffset? AuditedAt { get; set; }

    /// <summary>申请时间（UTC）。</summary>
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 审核通过（BR-04）：仅待审核记录可审批。
    /// </summary>
    public void Approve(long auditorId)
    {
        if (AuditStatus != AuditStatus.Pending)
        {
            throw new InvalidOperationException("仅待审核的绑定申请可审批");
        }

        AuditStatus = AuditStatus.Approved;
        AuditedBy = auditorId;
        AuditedAt = DateTimeOffset.UtcNow;
        UpdatedAt = AuditedAt.Value;
    }

    /// <summary>审核驳回。</summary>
    public void Reject(long auditorId)
    {
        if (AuditStatus != AuditStatus.Pending)
        {
            throw new InvalidOperationException("仅待审核的绑定申请可审批");
        }

        AuditStatus = AuditStatus.Rejected;
        AuditedBy = auditorId;
        AuditedAt = DateTimeOffset.UtcNow;
        UpdatedAt = AuditedAt.Value;
    }
}
