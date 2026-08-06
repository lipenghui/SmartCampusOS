using System.Security.Cryptography;
using System.Text;
using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Events;
using IdentityService.Application.Common;
using IdentityService.Application.Abstractions.Import;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.Abstractions.Security;
using IdentityService.Application.DTOs.Users;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Events;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Security;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Application.UseCases.Users;

/// <summary>
/// 用户用例（LLD §3.2）：用户 CRUD、Excel 批量导入、账号激活（发 UserActivated 事件）、当前用户 /me。
/// 手机号敏感字段 AES 加密存储 + 展示脱敏（LLD §8.3）。
/// </summary>
public sealed class UserService
{
    /// <summary>Excel 导入默认初始密码。</summary>
    public const string DefaultPassword = "123456";

    private readonly IRepository<User> _users;
    private readonly IRepository<UserRole> _userRoles;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<RolePermission> _rolePermissions;
    private readonly IRepository<Permission> _permissions;
    private readonly IRepository<StudentProfile> _studentProfiles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFieldEncryptor _encryptor;
    private readonly IEventPublisher _events;
    private readonly ICurrentUser _currentUser;
    private readonly IUserExcelImporter _excelImporter;
    private readonly IAuditLogger _auditLogger;

    public UserService(
        IRepository<User> users,
        IRepository<UserRole> userRoles,
        IRepository<Role> roles,
        IRepository<RolePermission> rolePermissions,
        IRepository<Permission> permissions,
        IRepository<StudentProfile> studentProfiles,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IFieldEncryptor encryptor,
        IEventPublisher events,
        ICurrentUser currentUser,
        IUserExcelImporter excelImporter,
        IAuditLogger auditLogger)
    {
        _users = users;
        _userRoles = userRoles;
        _roles = roles;
        _rolePermissions = rolePermissions;
        _permissions = permissions;
        _studentProfiles = studentProfiles;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _encryptor = encryptor;
        _events = events;
        _currentUser = currentUser;
        _excelImporter = excelImporter;
        _auditLogger = auditLogger;
    }

    /// <summary>分页查询用户（关键字/类型/状态筛选）。</summary>
    public async Task<PagedResult<UserItemDto>> PageUsersAsync(UserQuery query, CancellationToken ct)
    {
        var page = await _users.PageAsync(
            u => (query.UserType == null || u.UserType == query.UserType)
                 && (query.Status == null || u.Status == query.Status)
                 && (string.IsNullOrWhiteSpace(query.Keyword)
                     || u.UserNo.Contains(query.Keyword) || u.RealName.Contains(query.Keyword)),
            query.Page, query.PageSize, ct);

        var items = new List<UserItemDto>(page.Items.Count);
        foreach (var user in page.Items)
        {
            items.Add(new UserItemDto(
                user.Id, user.UserNo, user.RealName,
                MaskUtil.MaskPhone(_encryptor.Decrypt(user.MobileEncrypted)),
                user.UserType, user.Status,
                await LoadRoleCodesAsync(user.Id, ct)));
        }

        return new PagedResult<UserItemDto>(items, page.Total, page.Page, page.PageSize);
    }

    /// <summary>用户详情。</summary>
    public async Task<Result<UserDetailDto>> GetUserAsync(long id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonNotFound, "用户不存在");
        }

        var roleIds = await LoadRoleIdsAsync(id, ct);
        var roleCodes = await LoadRoleCodesAsync(id, ct);
        return Result<UserDetailDto>.Ok(new UserDetailDto(
            user.Id, user.UserNo, user.RealName,
            MaskUtil.MaskPhone(_encryptor.Decrypt(user.MobileEncrypted)),
            user.UserType, user.Status, user.AvatarUrl,
            roleIds, roleCodes, user.CreatedAt));
    }

    /// <summary>创建用户（学工号/手机号唯一校验；学生创建档案；分配角色）。</summary>
    public async Task<Result<UserDetailDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserNo) || string.IsNullOrWhiteSpace(request.RealName))
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonValidationFailed, "学工号与姓名不能为空");
        }

        if (await _users.AnyAsync(u => u.UserNo == request.UserNo, ct))
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonValidationFailed, "学工号已存在");
        }

        var mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        if (mobile is not null && await _users.AnyAsync(u => u.MobileHash == HashMobile(mobile), ct))
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonValidationFailed, "手机号已存在");
        }

        if (request.RoleIds.Count > 0 && await _roles.CountAsync(r => request.RoleIds.Contains(r.Id), ct) != request.RoleIds.Count)
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonValidationFailed, "存在无效的角色");
        }

        var password = string.IsNullOrWhiteSpace(request.Password) ? DefaultPassword : request.Password;
        var passwordHash = _passwordHasher.Hash(password, out var salt);
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            UserNo = request.UserNo.Trim(),
            RealName = request.RealName.Trim(),
            UserType = request.UserType,
            Status = UserStatus.Inactive,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            MobileEncrypted = _encryptor.Encrypt(mobile),
            MobileHash = mobile is null ? null : HashMobile(mobile),
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _users.AddAsync(user, ct);

            if (request.RoleIds.Count > 0)
            {
                await ReplaceRolesAsync(user.Id, request.RoleIds, ct);
            }

            if (request.UserType == UserType.Student)
            {
                await _studentProfiles.AddAsync(new StudentProfile { UserId = user.Id }, ct);
            }
        }, ct);

        await _auditLogger.LogAsync("user.create", "user", user.Id.ToString(),
            $"创建用户 {user.UserNo} 类型 {user.UserType}", ct);
        var detail = await GetUserAsync(user.Id, ct);
        return detail;
    }

    /// <summary>更新用户（基本资料 + 角色替换）。</summary>
    public async Task<Result<UserDetailDto>> UpdateUserAsync(long id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonNotFound, "用户不存在");
        }

        var mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        if (mobile is not null
            && await _users.AnyAsync(u => u.MobileHash == HashMobile(mobile) && u.Id != id, ct))
        {
            return Result<UserDetailDto>.Fail(ErrorCodes.CommonValidationFailed, "手机号已存在");
        }

        user.RealName = request.RealName.Trim();
        user.MobileEncrypted = _encryptor.Encrypt(mobile);
        user.MobileHash = mobile is null ? null : HashMobile(mobile);
        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        if (request.Status is not null && user.Status != request.Status)
        {
            // 状态变更走状态机（激活/停用/启用/锁定）
            switch (request.Status)
            {
                case UserStatus.Active when user.Status == UserStatus.Inactive:
                    user.Activate();
                    break;
                case UserStatus.Active when user.Status == UserStatus.Disabled:
                    user.Enable();
                    break;
                case UserStatus.Disabled:
                    user.Disable();
                    break;
                case UserStatus.Locked:
                    user.Lock();
                    break;
                default:
                    user.Status = request.Status.Value;
                    break;
            }
        }

        user.UpdatedBy = _currentUser.UserId;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _users.UpdateAsync(user, ct);
            if (request.RoleIds is not null)
            {
                await ReplaceRolesAsync(user.Id, request.RoleIds, ct);
            }
        }, ct);

        await _auditLogger.LogAsync("user.update", "user", id.ToString(),
            $"更新用户 {user.UserNo}", ct);
        return await GetUserAsync(id, ct);
    }

    /// <summary>删除用户（软删除 + 清理角色关联）。</summary>
    public async Task<Result> DeleteUserAsync(long id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "用户不存在");
        }

        if (user.UserNo == "admin")
        {
            return Result.Fail(ErrorCodes.AuthForbidden, "内置管理员账号不可删除");
        }

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _users.DeleteAsync(user, ct);
            var userRoles = await _userRoles.ListAsync(ur => ur.UserId == id, ct);
            foreach (var ur in userRoles)
            {
                await _userRoles.DeleteAsync(ur, ct);
            }
        }, ct);
        await _auditLogger.LogAsync("user.delete", "user", id.ToString(),
            $"删除用户 {user.UserNo}", ct);
        return Result.Ok();
    }

    /// <summary>激活账号：状态机校验 + 发布 UserActivated 事件（LLD §7）。</summary>
    public async Task<Result> ActivateUserAsync(long id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "用户不存在");
        }

        try
        {
            user.Activate();
            user.UpdatedBy = _currentUser.UserId;
            await _users.UpdateAsync(user, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, ex.Message);
        }

        await _events.PublishAsync(new UserActivated(id, _currentUser.UserId, DateTimeOffset.UtcNow), ct);
        await _auditLogger.LogAsync("user.activate", "user", id.ToString(),
            $"激活账号 {user.UserNo}", ct);
        return Result.Ok();
    }

    /// <summary>Excel 批量导入（LLD §3.2）：逐行校验，学工号重复跳过，返回成功/失败明细。</summary>
    public async Task<Result<ImportResult>> ImportUsersExcelAsync(byte[] content, CancellationToken ct)
    {
        if (content.Length == 0)
        {
            return Result<ImportResult>.Fail(ErrorCodes.CommonValidationFailed, "文件内容为空");
        }

        IUserExcelImporter importer = _excelImporter;
        IReadOnlyList<UserImportRow> rows;
        try
        {
            rows = importer.Parse(content);
        }
        catch (Exception ex)
        {
            return Result<ImportResult>.Fail(ErrorCodes.CommonValidationFailed, $"Excel 解析失败：{ex.Message}");
        }

        var errors = new List<string>();
        var succeeded = 0;
        foreach (var row in rows)
        {
            var createResult = await CreateUserAsync(new CreateUserRequest
            {
                UserNo = row.UserNo,
                RealName = row.RealName,
                Mobile = row.Mobile,
                UserType = ParseUserType(row.UserTypeText),
                Password = string.IsNullOrWhiteSpace(row.Password) ? DefaultPassword : row.Password
            }, ct);

            if (createResult.IsSuccess)
            {
                succeeded++;
            }
            else
            {
                errors.Add($"第 {succeeded + errors.Count + 1} 行 {row.UserNo}：{createResult.ErrorMessage}");
            }
        }

        return Result<ImportResult>.Ok(new ImportResult(rows.Count, succeeded, errors.Count, errors));
    }

    /// <summary>当前用户信息（/me）：用户资料 + 全部角色（含数据范围）+ 权限点。</summary>
    public async Task<Result<MeDto>> GetMeAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is not long userId)
        {
            return Result<MeDto>.Fail(ErrorCodes.AuthInvalidCredentials, "未认证");
        }

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || user.IsDeleted)
        {
            return Result<MeDto>.Fail(ErrorCodes.CommonNotFound, "用户不存在");
        }

        var userRoles = await _userRoles.ListAsync(ur => ur.UserId == userId, ct);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToArray();
        var roles = roleIds.Length > 0 ? await _roles.ListAsync(r => roleIds.Contains(r.Id), ct) : [];
        var roleDtos = roles
            .OrderByDescending(r => (int)r.DataScope)
            .Select(r => new MeRoleDto(r.Id, r.Code, r.Name, DataScopeStrings.ToScopeString(r.DataScope)))
            .ToList();

        var permissions = await LoadPermissionCodesAsync(roleIds, ct);

        var dataScope = roles.Count == 0
            ? DataScope.Self
            : (DataScope)roles.Max(r => (int)r.DataScope);

        return Result<MeDto>.Ok(new MeDto(
            user.Id, user.UserNo, user.RealName, user.UserType, user.AvatarUrl,
            DataScopeStrings.ToScopeString(dataScope), roleDtos, permissions));
    }

    private async Task ReplaceRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken ct)
    {
        var existing = await _userRoles.ListAsync(ur => ur.UserId == userId, ct);
        foreach (var ur in existing)
        {
            await _userRoles.DeleteAsync(ur, ct);
        }

        foreach (var roleId in roleIds.Distinct())
        {
            await _userRoles.AddAsync(new UserRole { UserId = userId, RoleId = roleId }, ct);
        }
    }

    private async Task<IReadOnlyList<long>> LoadRoleIdsAsync(long userId, CancellationToken ct)
    {
        var userRoles = await _userRoles.ListAsync(ur => ur.UserId == userId, ct);
        return userRoles.Select(ur => ur.RoleId).Distinct().ToArray();
    }

    private async Task<IReadOnlyList<string>> LoadRoleCodesAsync(long userId, CancellationToken ct)
    {
        var roleIds = await LoadRoleIdsAsync(userId, ct);
        if (roleIds.Count == 0)
        {
            return [];
        }

        var roles = await _roles.ListAsync(r => roleIds.Contains(r.Id), ct);
        return roles.Select(r => r.Code).ToArray();
    }

    private async Task<IReadOnlyList<string>> LoadPermissionCodesAsync(long[] roleIds, CancellationToken ct)
    {
        if (roleIds.Length == 0)
        {
            return [];
        }

        var rolePermissions = await _rolePermissions.ListAsync(rp => roleIds.Contains(rp.RoleId), ct);
        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToArray();
        if (permissionIds.Length == 0)
        {
            return [];
        }

        var permissions = await _permissions.ListAsync(p => permissionIds.Contains(p.Id), ct);
        return permissions.Select(p => p.Code).Distinct().OrderBy(c => c).ToArray();
    }

    private static UserType ParseUserType(string text) => text.Trim() switch
    {
        "学生" or "student" or "1" => UserType.Student,
        "教师" or "teacher" or "2" => UserType.Teacher,
        "家长" or "parent" or "3" => UserType.Parent,
        "教职工" or "staff" or "4" => UserType.Staff,
        _ => UserType.Student
    };

    private static string HashMobile(string mobile) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mobile)));
}
