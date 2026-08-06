using IdentityService.Application.DTOs.Users;
using IdentityService.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 当前用户接口（LLD §3.2 GET /api/v1/me）：多角色 + 权限点 + 数据范围（§8.1 / §8.2）。
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class MeController(UserService userService) : ControllerBase
{
    /// <summary>当前登录用户信息与多角色列表。</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await userService.GetMeAsync(ct);
        return result.IsSuccess
            ? Ok(ApiResponse<MeDto>.From(result))
            : BadRequest(ApiResponse<MeDto>.From(result));
    }
}
