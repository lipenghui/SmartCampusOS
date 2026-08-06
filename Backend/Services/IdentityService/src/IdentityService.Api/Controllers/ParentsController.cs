using IdentityService.Application.DTOs.Parents;
using IdentityService.Application.UseCases.Parents;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 家长-子女绑定接口（LLD §3.2 POST /api/v1/parents/bind，BR-04）。
/// 申请由家长发起，审核由班主任/管理员完成；审核通过后家长可访问子女数据。
/// </summary>
[ApiController]
[Route("api/v1/parents")]
public sealed class ParentsController(
    ParentBindingService bindingService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>家长发起绑定申请。</summary>
    [HttpPost("bind")]
    public async Task<IActionResult> Apply(CreateBindingRequest request, CancellationToken ct)
    {
        if (currentUser.UserId is not long parentUserId)
        {
            return BadRequest(ApiResponse<BindingDto>.Fail(ErrorCodes.AuthInvalidCredentials, "未认证"));
        }

        var result = await bindingService.ApplyAsync(parentUserId, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<BindingDto>.From(result))
            : BadRequest(ApiResponse<BindingDto>.From(result));
    }

    /// <summary>绑定申请列表（审核人视角，按状态过滤）。</summary>
    [HttpGet("bind")]
    public async Task<IActionResult> List([FromQuery] BindingQuery query, CancellationToken ct)
    {
        var result = await bindingService.ListAsync(query, ct);
        return Ok(ApiResponse<PagedResult<BindingDto>>.Ok(result));
    }

    /// <summary>审核通过。</summary>
    [HttpPut("bind/{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        if (currentUser.UserId is not long auditorId)
        {
            return BadRequest(ApiResponse.Fail(ErrorCodes.AuthInvalidCredentials, "未认证"));
        }

        var result = await bindingService.ApproveAsync(id, auditorId, ct);
        return result.IsSuccess ? Ok(ApiResponse.From(result)) : BadRequest(ApiResponse.From(result));
    }

    /// <summary>审核驳回。</summary>
    [HttpPut("bind/{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, CancellationToken ct)
    {
        if (currentUser.UserId is not long auditorId)
        {
            return BadRequest(ApiResponse.Fail(ErrorCodes.AuthInvalidCredentials, "未认证"));
        }

        var result = await bindingService.RejectAsync(id, auditorId, ct);
        return result.IsSuccess ? Ok(ApiResponse.From(result)) : BadRequest(ApiResponse.From(result));
    }
}
