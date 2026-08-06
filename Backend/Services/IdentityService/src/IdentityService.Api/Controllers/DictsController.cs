using IdentityService.Application.DTOs.Dicts;
using IdentityService.Application.UseCases.Dicts;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 数据字典接口（LLD §3.2 / §4.3：GET/POST /api/v1/dicts，字典项维护）。
/// </summary>
[ApiController]
[Route("api/v1/dicts")]
public sealed class DictsController(DictService dictService) : ControllerBase
{
    /// <summary>字典列表（含字典项）。</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var dicts = await dictService.ListAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<DictDto>>.Ok(dicts));
    }

    /// <summary>字典详情（按编码，Redis 缓存）。</summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code, CancellationToken ct)
    {
        var result = await dictService.GetByCodeAsync(code, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<DictDto>.From(result))
            : NotFound(ApiResponse<DictDto>.From(result));
    }

    /// <summary>创建字典。</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateDictRequest request, CancellationToken ct)
    {
        var result = await dictService.CreateAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<DictDto>.From(result))
            : BadRequest(ApiResponse<DictDto>.From(result));
    }

    /// <summary>更新字典。</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateDictRequest request, CancellationToken ct)
    {
        var result = await dictService.UpdateAsync(id, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<DictDto>.From(result))
            : BadRequest(ApiResponse<DictDto>.From(result));
    }

    /// <summary>删除字典（含字典项）。</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await dictService.DeleteAsync(id, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.From(result))
            : BadRequest(ApiResponse.From(result));
    }

    /// <summary>新增字典项。</summary>
    [HttpPost("{code}/items")]
    public async Task<IActionResult> AddItem(string code, CreateDictItemRequest request, CancellationToken ct)
    {
        var result = await dictService.AddItemAsync(code, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<DictItemDto>.From(result))
            : BadRequest(ApiResponse<DictItemDto>.From(result));
    }

    /// <summary>更新字典项。</summary>
    [HttpPut("items/{itemId:long}")]
    public async Task<IActionResult> UpdateItem(long itemId, UpdateDictItemRequest request, CancellationToken ct)
    {
        var result = await dictService.UpdateItemAsync(itemId, request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<DictItemDto>.From(result))
            : BadRequest(ApiResponse<DictItemDto>.From(result));
    }

    /// <summary>删除字典项。</summary>
    [HttpDelete("items/{itemId:long}")]
    public async Task<IActionResult> DeleteItem(long itemId, CancellationToken ct)
    {
        var result = await dictService.DeleteItemAsync(itemId, ct);
        return result.IsSuccess
            ? Ok(ApiResponse.From(result))
            : BadRequest(ApiResponse.From(result));
    }
}
