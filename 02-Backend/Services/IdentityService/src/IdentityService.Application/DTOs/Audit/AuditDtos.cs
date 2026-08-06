using IdentityService.Domain.Enums;

namespace IdentityService.Application.DTOs.Audit;

/// <summary>审计日志检索条件（LLD §3.2 GET /api/v1/audit-logs）。</summary>
public sealed record AuditQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public long? UserId { get; set; }
    public string? Action { get; set; }
    public string? TargetType { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

/// <summary>审计日志 DTO。</summary>
public sealed record AuditLogDto(
    long Id,
    long? UserId,
    string Action,
    string TargetType,
    string? TargetId,
    string? Detail,
    string? Ip,
    DateTimeOffset CreatedAt);
