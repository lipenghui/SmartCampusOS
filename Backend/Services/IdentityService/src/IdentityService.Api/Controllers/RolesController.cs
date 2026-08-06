using IdentityService.Application.DTOs.Roles;
using IdentityService.Application.UseCases.Roles;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 角色与权限接口（LLD §3.2 / Common-03 / §8.2 RBAC：GET/POST/PUT roles）。
/// </summary>
[ApiController]
[Route("api/v1/roles")]
public sealed class RolesController(RoleService roleService) : ControllerBase
{
    /// <summary>角色列表（含权限点 ID 与数据范围）。</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var roles = await roleService.ListAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(roles));
    }

    /// <summary>权限点列表（角色配置用）。</summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken ct)
    {
        var permissions = await roleService.ListPermissionsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(permissions));
    }

    /// <summary>创建角色（编码唯一，绑定权限点）。</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.CreateAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<RoleDto>.From(result))
            : BadRequest(ApiResponse<RoleDto>.From(result));
    }

    /// <summary>更新角色（内置角色编码不可改，数据范围/权限点可配）。</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<RoleDto>.From(result))
            : BadRequest(ApiResponse<RoleDto>.From(result));
    }

    /// <summary>删除角色（内置/已分配用户时拒绝）。</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await roleService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.From(result))
            : BadRequest(ApiResponse.From(result));
    }
}
