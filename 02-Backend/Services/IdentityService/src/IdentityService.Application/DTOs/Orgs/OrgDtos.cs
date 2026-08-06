using IdentityService.Domain.Enums;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.DTOs.Orgs;

/// <summary>创建组织请求（LLD §3.2 POST /api/v1/orgs）。</summary>
public sealed record CreateOrgRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OrgType OrgType { get; set; }
    public long? ParentId { get; set; }
}

/// <summary>更新组织请求（PUT /api/v1/orgs/{id}）。</summary>
public sealed record UpdateOrgRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>组织树节点（LLD §3.2 GET /api/v1/orgs 组织树）。</summary>
public sealed record OrgNodeDto(
    long Id,
    long? ParentId,
    string Code,
    string Name,
    OrgType OrgType,
    IReadOnlyList<OrgNodeDto> Children);
