using System.Linq.Expressions;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.DTOs.Audit;
using IdentityService.Domain.Entities;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Application.UseCases.Audit;

/// <summary>
/// 审计日志检索用例（LLD §3.2 GET /api/v1/audit-logs）。
/// </summary>
public sealed class AuditService
{
    private readonly IRepository<AuditLog> _auditLogs;

    public AuditService(IRepository<AuditLog> auditLogs) => _auditLogs = auditLogs;

    /// <summary>分页检索审计日志（用户/动作/目标类型/时间过滤）。</summary>
    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditQuery query, CancellationToken ct)
    {
        Expression<Func<AuditLog, bool>> predicate = log =>
            (query.UserId == null || log.UserId == query.UserId)
            && (string.IsNullOrWhiteSpace(query.Action) || log.Action.Contains(query.Action))
            && (string.IsNullOrWhiteSpace(query.TargetType) || log.TargetType == query.TargetType)
            && (query.StartTime == null || log.CreatedAt >= query.StartTime)
            && (query.EndTime == null || log.CreatedAt <= query.EndTime);

        var page = await _auditLogs.PageAsync(predicate, query.Page, query.PageSize, ct);
        var items = page.Items.Select(log => new AuditLogDto(
            log.Id, log.UserId, log.Action, log.TargetType, log.TargetId,
            log.Detail, log.Ip, log.CreatedAt)).ToArray();
        return new PagedResult<AuditLogDto>(items, page.Total, page.Page, page.PageSize);
    }
}
