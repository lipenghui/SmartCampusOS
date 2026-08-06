using System.Text.Json;
using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Infrastructure.Audit;

/// <summary>
/// 审计日志实现（LLD §8.3）：写入 sys_audit_log，记录操作人、动作、目标、IP、时间。
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly IRepository<AuditLog> _auditLogs;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogger(
        IRepository<AuditLog> auditLogs,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task LogAsync(string action, string targetType, string? targetId = null, string? detail = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = _currentUser.UserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail,
            Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        return _auditLogs.AddAsync(log, ct);
    }
}
