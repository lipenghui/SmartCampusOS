namespace IdentityService.Application.Abstractions.Audit;

/// <summary>
/// 审计日志写入抽象（LLD §8.3：审批、成绩修改、公告发布、权限变更等写操作留痕 sys_audit_log）。
/// </summary>
public interface IAuditLogger
{
    /// <summary>记录一条审计日志。</summary>
    /// <param name="action">操作动作（如 user.create / role.grant）。</param>
    /// <param name="targetType">目标类型（如 user / role / org）。</param>
    /// <param name="targetId">目标 ID（字符串形式）。</param>
    /// <param name="detail">操作详情（JSON 或摘要）。</param>
    Task LogAsync(string action, string targetType, string? targetId = null, string? detail = null, CancellationToken ct = default);
}
