using IdentityService.Domain.Enums;

namespace IdentityService.Application.DTOs.Parents;

/// <summary>家长-子女绑定申请请求（LLD §3.2 POST /api/v1/parents/bind，BR-04）。</summary>
public sealed record CreateBindingRequest
{
    /// <summary>子女学工号。</summary>
    public string? StudentUserNo { get; set; }

    /// <summary>子女手机号（二选一：学工号或手机号）。</summary>
    public string? StudentMobile { get; set; }

    public ParentRelation Relation { get; set; } = ParentRelation.Other;
}

/// <summary>绑定申请列表查询。</summary>
public sealed record BindingQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public AuditStatus? Status { get; set; }
}

/// <summary>绑定申请 DTO。</summary>
public sealed record BindingDto(
    long Id,
    long ParentUserId,
    string ParentName,
    long StudentUserId,
    string StudentName,
    ParentRelation Relation,
    AuditStatus AuditStatus,
    long? AuditedBy,
    DateTimeOffset AppliedAt);
