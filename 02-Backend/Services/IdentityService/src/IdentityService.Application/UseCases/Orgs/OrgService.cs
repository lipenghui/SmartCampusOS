using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.DTOs.Orgs;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Application.UseCases.Orgs;

/// <summary>
/// 组织架构用例（LLD §3.2 / Common-02）：组织树维护（GET/POST/PUT orgs）。
/// </summary>
public sealed class OrgService
{
    private readonly IRepository<OrgUnit> _orgs;
    private readonly IRepository<User> _users;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;

    public OrgService(IRepository<OrgUnit> orgs, IRepository<User> users, ICurrentUser currentUser, IAuditLogger auditLogger)
    {
        _orgs = orgs;
        _users = users;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    /// <summary>组织树（按 parentId 组装）。</summary>
    public async Task<IReadOnlyList<OrgNodeDto>> GetTreeAsync(CancellationToken ct)
    {
        var all = await _orgs.ListAsync(ct);
        return BuildTree(all, null);
    }

    /// <summary>创建组织：编码全局唯一。</summary>
    public async Task<Result<OrgNodeDto>> CreateAsync(CreateOrgRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<OrgNodeDto>.Fail(ErrorCodes.CommonValidationFailed, "组织名称与编码不能为空");
        }

        if (await _orgs.AnyAsync(o => o.Code == request.Code, ct))
        {
            return Result<OrgNodeDto>.Fail(ErrorCodes.CommonValidationFailed, "组织编码已存在");
        }

        if (request.ParentId is not null
            && !await _orgs.AnyAsync(o => o.Id == request.ParentId, ct))
        {
            return Result<OrgNodeDto>.Fail(ErrorCodes.CommonValidationFailed, "父组织不存在");
        }

        var org = new OrgUnit
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            OrgType = request.OrgType,
            ParentId = request.ParentId,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _orgs.AddAsync(org, ct);
        await _auditLogger.LogAsync("org.create", "org", org.Id.ToString(), $"创建组织 {org.Code}", ct);
        return Result<OrgNodeDto>.Ok(ToDto(org, []));
    }

    /// <summary>更新组织名称。</summary>
    public async Task<Result<OrgNodeDto>> UpdateAsync(long id, UpdateOrgRequest request, CancellationToken ct)
    {
        var org = await _orgs.GetByIdAsync(id, ct);
        if (org is null)
        {
            return Result<OrgNodeDto>.Fail(ErrorCodes.CommonNotFound, "组织不存在");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<OrgNodeDto>.Fail(ErrorCodes.CommonValidationFailed, "组织名称不能为空");
        }

        org.Name = request.Name.Trim();
        org.UpdatedBy = _currentUser.UserId;
        await _orgs.UpdateAsync(org, ct);
        await _auditLogger.LogAsync("org.update", "org", org.Id.ToString(), $"更新组织 {org.Code}", ct);
        return Result<OrgNodeDto>.Ok(ToDto(org, []));
    }

    /// <summary>删除组织：存在子组织或关联用户时拒绝。</summary>
    public async Task<Result> DeleteAsync(long id, CancellationToken ct)
    {
        var org = await _orgs.GetByIdAsync(id, ct);
        if (org is null)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "组织不存在");
        }

        if (await _orgs.AnyAsync(o => o.ParentId == id, ct))
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, "存在子组织，不可删除");
        }

        if (await _users.AnyAsync(u => u.UserNo == org.Code, ct))
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, "组织已关联用户，不可删除");
        }

        await _orgs.DeleteAsync(org, ct);
        await _auditLogger.LogAsync("org.delete", "org", org.Id.ToString(), $"删除组织 {org.Code}", ct);
        return Result.Ok();
    }

    private static IReadOnlyList<OrgNodeDto> BuildTree(IEnumerable<OrgUnit> all, long? parentId) =>
        all.Where(o => o.ParentId == parentId)
            .OrderBy(o => o.Code)
            .Select(o => ToDto(o, BuildTree(all, o.Id)))
            .ToList();

    private static OrgNodeDto ToDto(OrgUnit org, IReadOnlyList<OrgNodeDto> children) =>
        new(org.Id, org.ParentId, org.Code, org.Name, org.OrgType, children);
}
