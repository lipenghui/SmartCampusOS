using IdentityService.Domain.Enums;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.DTOs.Roles;

/// <summary>创建角色请求（LLD §3.2 POST /api/v1/roles）。</summary>
public sealed record CreateRoleRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DataScope DataScope { get; set; } = DataScope.Self;
    /// <summary>绑定的权限点 ID 列表（RBAC，LLD §8.2）。</summary>
    public List<long> PermissionIds { get; set; } = [];
}

/// <summary>更新角色请求（PUT /api/v1/roles/{id}）。</summary>
public sealed record UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public DataScope DataScope { get; set; } = DataScope.Self;
    public List<long> PermissionIds { get; set; } = [];
}

/// <summary>角色列表项（含权限点 ID 与数据范围）。</summary>
public sealed record RoleDto(
    long Id,
    string Code,
    string Name,
    DataScope DataScope,
    bool Builtin,
    IReadOnlyList<long> PermissionIds);

/// <summary>权限点（LLD §8.2）。</summary>
public sealed record PermissionDto(long Id, string Code, string Name, string Module);
