using IdentityService.Domain.Enums;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.DTOs.Users;

/// <summary>创建用户请求（LLD §3.2 POST /api/v1/users）。</summary>
public sealed record CreateUserRequest
{
    public string UserNo { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public UserType UserType { get; set; } = UserType.Student;
    /// <summary>初始密码；为空时使用系统默认初始密码。</summary>
    public string? Password { get; set; }
    /// <summary>角色 ID 列表（支持多角色，LLD §8.1）。</summary>
    public List<long> RoleIds { get; set; } = [];
}

/// <summary>更新用户请求（PUT /api/v1/users/{id}）。</summary>
public sealed record UpdateUserRequest
{
    public string RealName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus? Status { get; set; }
    public List<long>? RoleIds { get; set; }
}

/// <summary>用户列表项（脱敏后展示，LLD §8.3）。</summary>
public sealed record UserItemDto(
    long Id,
    string UserNo,
    string RealName,
    string? MobileMasked,
    UserType UserType,
    UserStatus Status,
    IReadOnlyList<string> RoleCodes);

/// <summary>用户详情（含角色 ID 与权限点）。</summary>
public sealed record UserDetailDto(
    long Id,
    string UserNo,
    string RealName,
    string? MobileMasked,
    UserType UserType,
    UserStatus Status,
    string? AvatarUrl,
    IReadOnlyList<long> RoleIds,
    IReadOnlyList<string> RoleCodes,
    DateTimeOffset CreatedAt);

/// <summary>用户查询条件（GET /api/v1/users）。</summary>
public sealed record UserQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public UserType? UserType { get; set; }
    public UserStatus? Status { get; set; }
    /// <summary>关键字：学工号或姓名模糊匹配。</summary>
    public string? Keyword { get; set; }
}

/// <summary>Excel 批量导入结果（LLD §3.2 批量导入）。</summary>
public sealed record ImportResult(int Total, int Succeeded, int Failed, IReadOnlyList<string> Errors);

/// <summary>Excel 导入行（解析后的原始数据）。</summary>
public sealed record UserImportRow(string UserNo, string RealName, string? Mobile, string UserTypeText, string? Password);

/// <summary>当前用户信息（LLD §3.2 GET /api/v1/me：多角色 + 权限点 + 数据范围）。</summary>
public sealed record MeDto(
    long UserId,
    string UserNo,
    string RealName,
    UserType UserType,
    string? AvatarUrl,
    string DataScope,
    IReadOnlyList<MeRoleDto> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>当前用户角色（含数据范围）。</summary>
public sealed record MeRoleDto(long RoleId, string Code, string Name, string DataScope);
