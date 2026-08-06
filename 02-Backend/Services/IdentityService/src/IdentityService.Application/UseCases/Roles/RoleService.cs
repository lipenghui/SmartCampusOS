using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.DTOs.Roles;
using IdentityService.Domain.Entities;
using Mapster;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Application.UseCases.Roles;

/// <summary>
/// 角色与权限配置用例（LLD §3.2 / Common-03 / §8.2 RBAC）：角色 CRUD、权限点绑定、数据范围。
/// </summary>
public sealed class RoleService
{
    private readonly IRepository<Role> _roles;
    private readonly IRepository<Permission> _permissions;
    private readonly IRepository<RolePermission> _rolePermissions;
    private readonly IRepository<UserRole> _userRoles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;

    public RoleService(
        IRepository<Role> roles,
        IRepository<Permission> permissions,
        IRepository<RolePermission> rolePermissions,
        IRepository<UserRole> userRoles,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAuditLogger auditLogger)
    {
        _roles = roles;
        _permissions = permissions;
        _rolePermissions = rolePermissions;
        _userRoles = userRoles;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    /// <summary>角色列表（含权限点 ID）。</summary>
    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct)
    {
        var roles = await _roles.ListAsync(ct);
        var rolePermissions = await _rolePermissions.ListAsync(ct);
        var result = new List<RoleDto>(roles.Count);
        foreach (var role in roles.OrderBy(r => r.Id))
        {
            result.Add(new RoleDto(
                role.Id, role.Code, role.Name, role.DataScope, role.Builtin,
                rolePermissions.Where(rp => rp.RoleId == role.Id).Select(rp => rp.PermissionId).ToArray()));
        }

        return result;
    }

    /// <summary>权限点列表（用于角色配置界面，Mapster 纯映射示例，LLD §2.6.4）。</summary>
    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct)
    {
        var permissions = await _permissions.ListAsync(ct);
        return permissions
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Adapt<List<PermissionDto>>();
    }

    /// <summary>创建角色：编码唯一。</summary>
    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<RoleDto>.Fail(ErrorCodes.CommonValidationFailed, "角色编码与名称不能为空");
        }

        if (await _roles.AnyAsync(r => r.Code == request.Code, ct))
        {
            return Result<RoleDto>.Fail(ErrorCodes.CommonValidationFailed, "角色编码已存在");
        }

        if (!await ValidatePermissionsAsync(request.PermissionIds, ct))
        {
            return Result<RoleDto>.Fail(ErrorCodes.CommonValidationFailed, "存在无效的权限点");
        }

        var role = new Role
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            DataScope = request.DataScope,
            Builtin = false,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _roles.AddAsync(role, ct);
            await ReplacePermissionsAsync(role.Id, request.PermissionIds, ct);
        }, ct);

        await _auditLogger.LogAsync("role.create", "role", role.Id.ToString(),
            $"创建角色 {role.Code} 数据范围 {role.DataScope}", ct);
        return Result<RoleDto>.Ok(new RoleDto(
            role.Id, role.Code, role.Name, role.DataScope, role.Builtin, request.PermissionIds.ToArray()));
    }

    /// <summary>更新角色：内置角色编码不可改；权限点与数据范围可配。</summary>
    public async Task<Result<RoleDto>> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _roles.GetByIdAsync(id, ct);
        if (role is null)
        {
            return Result<RoleDto>.Fail(ErrorCodes.CommonNotFound, "角色不存在");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<RoleDto>.Fail(ErrorCodes.CommonValidationFailed, "角色名称不能为空");
        }

        if (!await ValidatePermissionsAsync(request.PermissionIds, ct))
        {
            return Result<RoleDto>.Fail(ErrorCodes.CommonValidationFailed, "存在无效的权限点");
        }

        role.Name = request.Name.Trim();
        role.DataScope = request.DataScope;
        role.UpdatedBy = _currentUser.UserId;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _roles.UpdateAsync(role, ct);
            await ReplacePermissionsAsync(role.Id, request.PermissionIds, ct);
        }, ct);

        await _auditLogger.LogAsync("role.update", "role", role.Id.ToString(),
            $"更新角色 {role.Code} 数据范围 {role.DataScope}", ct);
        return Result<RoleDto>.Ok(new RoleDto(
            role.Id, role.Code, role.Name, role.DataScope, role.Builtin, request.PermissionIds.ToArray()));
    }

    /// <summary>删除角色：内置角色禁止删除；已分配用户的角色拒绝删除。</summary>
    public async Task<Result> DeleteAsync(long id, CancellationToken ct)
    {
        var role = await _roles.GetByIdAsync(id, ct);
        if (role is null)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "角色不存在");
        }

        try
        {
            role.EnsureDeletable();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, ex.Message);
        }

        if (await _userRoles.AnyAsync(ur => ur.RoleId == id, ct))
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, "角色已分配给用户，不可删除");
        }

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _roles.DeleteAsync(role, ct);
            var rolePermissions = await _rolePermissions.ListAsync(rp => rp.RoleId == id, ct);
            foreach (var rp in rolePermissions)
            {
                await _rolePermissions.DeleteAsync(rp, ct);
            }
        }, ct);
        await _auditLogger.LogAsync("role.delete", "role", role.Id.ToString(),
            $"删除角色 {role.Code}", ct);
        return Result.Ok();
    }

    private async Task<bool> ValidatePermissionsAsync(IReadOnlyCollection<long> permissionIds, CancellationToken ct)
    {
        if (permissionIds.Count == 0)
        {
            return true;
        }

        return await _permissions.CountAsync(p => permissionIds.Contains(p.Id), ct) == permissionIds.Count;
    }

    private async Task ReplacePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken ct)
    {
        var existing = await _rolePermissions.ListAsync(rp => rp.RoleId == roleId, ct);
        foreach (var rp in existing)
        {
            await _rolePermissions.DeleteAsync(rp, ct);
        }

        foreach (var permissionId in permissionIds.Distinct())
        {
            await _rolePermissions.AddAsync(new RolePermission { RoleId = roleId, PermissionId = permissionId }, ct);
        }
    }
}
