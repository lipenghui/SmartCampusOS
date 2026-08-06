using System.Security.Cryptography;
using System.Text;
using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.DTOs.Parents;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Application.UseCases.Parents;

/// <summary>
/// 家长-子女绑定用例（LLD §3.2 / BR-04）：绑定申请、审核（通过/驳回）、家长访问校验（X-Child-Id 归属）。
/// </summary>
public sealed class ParentBindingService
{
    private readonly IRepository<ParentBinding> _bindings;
    private readonly IRepository<User> _users;
    private readonly IAuditLogger _auditLogger;

    public ParentBindingService(
        IRepository<ParentBinding> bindings,
        IRepository<User> users,
        IAuditLogger auditLogger)
    {
        _bindings = bindings;
        _users = users;
        _auditLogger = auditLogger;
    }

    /// <summary>家长申请绑定子女（BR-04）：学工号或手机号定位子女，重复申请拦截。</summary>
    public async Task<Result<BindingDto>> ApplyAsync(long parentUserId, CreateBindingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.StudentUserNo) && string.IsNullOrWhiteSpace(request.StudentMobile))
        {
            return Result<BindingDto>.Fail(ErrorCodes.CommonValidationFailed, "请输入子女学工号或手机号");
        }

        User? student;
        if (!string.IsNullOrWhiteSpace(request.StudentUserNo))
        {
            student = await _users.FirstOrDefaultAsync(u => u.UserNo == request.StudentUserNo, ct);
        }
        else
        {
            var mobileHash = HashMobile(request.StudentMobile!);
            student = await _users.FirstOrDefaultAsync(u => u.MobileHash == mobileHash, ct);
        }

        if (student is null || student.UserType != UserType.Student || student.IsDeleted)
        {
            return Result<BindingDto>.Fail(ErrorCodes.CommonValidationFailed, "未找到对应学生账号");
        }

        if (student.Id == parentUserId)
        {
            return Result<BindingDto>.Fail(ErrorCodes.CommonValidationFailed, "不能绑定自己");
        }

        var duplicate = await _bindings.AnyAsync(b =>
            b.ParentUserId == parentUserId
            && b.StudentUserId == student.Id
            && (b.AuditStatus == AuditStatus.Pending || b.AuditStatus == AuditStatus.Approved), ct);
        if (duplicate)
        {
            return Result<BindingDto>.Fail(ErrorCodes.CommonValidationFailed, "该子女已存在待审核或已通过的绑定申请");
        }

        var binding = new ParentBinding
        {
            ParentUserId = parentUserId,
            StudentUserId = student.Id,
            Relation = request.Relation,
            AuditStatus = AuditStatus.Pending,
            AppliedAt = DateTimeOffset.UtcNow
        };
        await _bindings.AddAsync(binding, ct);
        await _auditLogger.LogAsync("parent_binding.apply", "parent_binding",
            binding.Id.ToString(), $"家长 {parentUserId} 申请绑定学生 {student.Id}", ct);
        return Result<BindingDto>.Ok(new BindingDto(
            binding.Id, parentUserId, string.Empty, student.Id, student.RealName,
            binding.Relation, binding.AuditStatus, null, binding.AppliedAt));
    }

    /// <summary>绑定申请列表（按审核状态过滤）。</summary>
    public async Task<SmartCampusOS.SharedKernel.Results.PagedResult<BindingDto>> ListAsync(BindingQuery query, CancellationToken ct)
    {
        var page = await _bindings.PageAsync(
            b => query.Status == null || b.AuditStatus == query.Status,
            query.Page, query.PageSize, ct);

        var parentIds = page.Items.Select(b => b.ParentUserId).Distinct().ToArray();
        var studentIds = page.Items.Select(b => b.StudentUserId).Distinct().ToArray();
        var users = parentIds.Length + studentIds.Length == 0
            ? []
            : await _users.ListAsync(u => parentIds.Contains(u.Id) || studentIds.Contains(u.Id), ct);
        var userMap = users.ToDictionary(u => u.Id);

        var items = page.Items.Select(b => new BindingDto(
            b.Id, b.ParentUserId,
            userMap.TryGetValue(b.ParentUserId, out var p) ? p.RealName : string.Empty,
            b.StudentUserId,
            userMap.TryGetValue(b.StudentUserId, out var s) ? s.RealName : string.Empty,
            b.Relation, b.AuditStatus, b.AuditedBy, b.AppliedAt)).ToArray();

        return new SmartCampusOS.SharedKernel.Results.PagedResult<BindingDto>(items, page.Total, page.Page, page.PageSize);
    }

    /// <summary>审核通过（BR-04：审核后家长可访问子女数据）。</summary>
    public async Task<Result> ApproveAsync(long id, long auditorId, CancellationToken ct)
    {
        var binding = await _bindings.GetByIdAsync(id, ct);
        if (binding is null)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "绑定申请不存在");
        }

        try
        {
            binding.Approve(auditorId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, ex.Message);
        }

        await _bindings.UpdateAsync(binding, ct);
        await _auditLogger.LogAsync("parent_binding.approve", "parent_binding",
            binding.Id.ToString(), $"家长 {binding.ParentUserId} 绑定学生 {binding.StudentUserId} 审核通过", ct);
        return Result.Ok();
    }

    /// <summary>审核驳回。</summary>
    public async Task<Result> RejectAsync(long id, long auditorId, CancellationToken ct)
    {
        var binding = await _bindings.GetByIdAsync(id, ct);
        if (binding is null)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "绑定申请不存在");
        }

        try
        {
            binding.Reject(auditorId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, ex.Message);
        }

        await _bindings.UpdateAsync(binding, ct);
        await _auditLogger.LogAsync("parent_binding.reject", "parent_binding",
            binding.Id.ToString(), $"家长 {binding.ParentUserId} 绑定学生 {binding.StudentUserId} 被驳回", ct);
        return Result.Ok();
    }

    /// <summary>
    /// 家长访问子女校验（BR-04 / LLD §8.2）：存在已通过的绑定方可访问。
    /// 供 X-Child-Id 归属校验与家长端聚合接口使用。
    /// </summary>
    public Task<bool> ValidateChildAccessAsync(long parentUserId, long childUserId, CancellationToken ct = default) =>
        _bindings.AnyAsync(b =>
            b.ParentUserId == parentUserId
            && b.StudentUserId == childUserId
            && b.AuditStatus == AuditStatus.Approved, ct);

    private static string HashMobile(string mobile) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mobile)));
}
