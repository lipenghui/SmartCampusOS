using IdentityService.Application.DTOs.Users;
using IdentityService.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 用户管理接口（LLD §3.2 / Common-02）：CRUD、Excel 批量导入、账号激活。
/// </summary>
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(UserService userService) : ControllerBase
{
    /// <summary>分页查询用户。</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] UserQuery query, CancellationToken ct)
    {
        var result = await userService.PageUsersAsync(query, ct);
        return Ok(ApiResponse<PagedResult<UserItemDto>>.Ok(result));
    }

    /// <summary>用户详情。</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var result = await userService.GetUserAsync(id, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserDetailDto>.From(result))
            : NotFound(ApiResponse<UserDetailDto>.From(result));
    }

    /// <summary>创建用户。</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await userService.CreateUserAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserDetailDto>.From(result))
            : BadRequest(ApiResponse<UserDetailDto>.From(result));
    }

    /// <summary>更新用户（资料 + 角色）。</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateUserRequest request, CancellationToken ct)
    {
        var result = await userService.UpdateUserAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<UserDetailDto>.From(result))
            : BadRequest(ApiResponse<UserDetailDto>.From(result));
    }

    /// <summary>删除用户（软删除）。</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await userService.DeleteUserAsync(id, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.From(result))
            : BadRequest(ApiResponse.From(result));
    }

    /// <summary>激活账号（发 UserActivated 事件，LLD §7）。</summary>
    [HttpPost("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        var result = await userService.ActivateUserAsync(id, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.From(result))
            : BadRequest(ApiResponse.From(result));
    }

    /// <summary>Excel 批量导入用户（multipart 字段名 file）。</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail(ErrorCodes.CommonValidationFailed, "请上传 Excel 文件"));
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var result = await userService.ImportUsersExcelAsync(ms.ToArray(), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<ImportResult>.From(result))
            : BadRequest(ApiResponse<ImportResult>.From(result));
    }
}
