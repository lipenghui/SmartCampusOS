using IdentityService.Application.DTOs.Orgs;
using IdentityService.Application.UseCases.Orgs;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 组织架构接口（LLD §3.2 / Common-02：GET/POST/PUT orgs 组织树维护）。
/// </summary>
[ApiController]
[Route("api/v1/orgs")]
public sealed class OrgsController(OrgService orgService) : ControllerBase
{
    /// <summary>组织树。</summary>
    [HttpGet]
    public async Task<IActionResult> Tree(CancellationToken ct)
    {
        var tree = await orgService.GetTreeAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<OrgNodeDto>>.Ok(tree));
    }

    /// <summary>创建组织。</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrgRequest request, CancellationToken ct)
    {
        var result = await orgService.CreateAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<OrgNodeDto>.From(result))
            : BadRequest(ApiResponse<OrgNodeDto>.From(result));
    }

    /// <summary>更新组织。</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateOrgRequest request, CancellationToken ct)
    {
        var result = await orgService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<OrgNodeDto>.From(result))
            : BadRequest(ApiResponse<OrgNodeDto>.From(result));
    }

    /// <summary>删除组织（存在子组织/关联用户时拒绝）。</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await orgService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.From(result))
            : BadRequest(ApiResponse.From(result));
    }
}
