using IdentityService.Domain.Common;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 审计日志实体（表 sys_audit_log，LLD §4.2 / §8.3）：审批、成绩修改、公告发布、权限变更等写操作留痕。
/// </summary>
[EntityTable("sys_audit_log")]
public sealed class AuditLog
{
    /// <summary>主键（雪花 ID）。</summary>
    public long Id { get; set; }

    /// <summary>操作人用户 ID（匿名操作可为 null）。</summary>
    public long? UserId { get; set; }

    /// <summary>操作动作（如 user.create / role.grant）。</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>目标类型（如 user / role / org）。</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>目标 ID（字符串形式）。</summary>
    public string? TargetId { get; set; }

    /// <summary>操作详情（JSON 或摘要）。</summary>
    public string? Detail { get; set; }

    /// <summary>客户端 IP。</summary>
    public string? Ip { get; set; }

    /// <summary>操作时间（UTC）。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
