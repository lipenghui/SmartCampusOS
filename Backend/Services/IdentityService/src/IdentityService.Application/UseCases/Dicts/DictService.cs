using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Caching;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.DTOs.Dicts;
using IdentityService.Domain.Entities;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Application.UseCases.Dicts;

/// <summary>
/// 数据字典用例（LLD §3.2 / §4.3）：字典 CRUD + 字典项维护，Redis 缓存（写时失效）。
/// 字典为全局统一，其他服务启动时缓存本地副本（LLD §4.3）。
/// </summary>
public sealed class DictService
{
    private const string CacheKeyPrefix = "identity:dict:";

    private readonly IRepository<Dict> _dicts;
    private readonly IRepository<DictItem> _dictItems;
    private readonly ICache _cache;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUser _currentUser;

    public DictService(
        IRepository<Dict> dicts,
        IRepository<DictItem> dictItems,
        ICache cache,
        IAuditLogger auditLogger,
        ICurrentUser currentUser)
    {
        _dicts = dicts;
        _dictItems = dictItems;
        _cache = cache;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    /// <summary>字典列表（含字典项）。</summary>
    public async Task<IReadOnlyList<DictDto>> ListAsync(CancellationToken ct)
    {
        var dicts = await _dicts.ListAsync(ct);
        var items = await _dictItems.ListAsync(ct);
        var result = new List<DictDto>(dicts.Count);
        foreach (var dict in dicts.OrderBy(d => d.Code))
        {
            result.Add(ToDto(dict, items.Where(i => i.DictCode == dict.Code).OrderBy(i => i.Sort)));
        }

        return result;
    }

    /// <summary>字典详情（按编码，命中 Redis 缓存）。</summary>
    public async Task<Result<DictDto>> GetByCodeAsync(string code, CancellationToken ct)
    {
        var dict = await _dicts.FirstOrDefaultAsync(d => d.Code == code, ct);
        if (dict is null)
        {
            return Result<DictDto>.Fail(ErrorCodes.CommonNotFound, "字典不存在");
        }

        var items = await GetItemsCachedAsync(code, ct);
        return Result<DictDto>.Ok(ToDto(dict, items));
    }

    /// <summary>创建字典：编码唯一。</summary>
    public async Task<Result<DictDto>> CreateAsync(CreateDictRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<DictDto>.Fail(ErrorCodes.CommonValidationFailed, "字典编码与名称不能为空");
        }

        if (await _dicts.AnyAsync(d => d.Code == request.Code, ct))
        {
            return Result<DictDto>.Fail(ErrorCodes.CommonValidationFailed, "字典编码已存在");
        }

        var dict = new Dict
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId
        };
        await _dicts.AddAsync(dict, ct);
        await _auditLogger.LogAsync("dict.create", "dict", dict.Id.ToString(), $"创建字典 {dict.Code}", ct);
        return Result<DictDto>.Ok(ToDto(dict, []));
    }

    /// <summary>更新字典（编码不可改）。</summary>
    public async Task<Result<DictDto>> UpdateAsync(long id, UpdateDictRequest request, CancellationToken ct)
    {
        var dict = await _dicts.GetByIdAsync(id, ct);
        if (dict is null)
        {
            return Result<DictDto>.Fail(ErrorCodes.CommonNotFound, "字典不存在");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<DictDto>.Fail(ErrorCodes.CommonValidationFailed, "字典名称不能为空");
        }

        dict.Name = request.Name.Trim();
        dict.Description = request.Description;
        dict.UpdatedBy = _currentUser.UserId;
        await _dicts.UpdateAsync(dict, ct);
        await _cache.RemoveAsync(CacheKey(dict.Code), ct);
        await _auditLogger.LogAsync("dict.update", "dict", dict.Id.ToString(), $"更新字典 {dict.Code}", ct);
        return Result<DictDto>.Ok(ToDto(dict, await GetItemsCachedAsync(dict.Code, ct)));
    }

    /// <summary>删除字典（含字典项）。</summary>
    public async Task<Result> DeleteAsync(long id, CancellationToken ct)
    {
        var dict = await _dicts.GetByIdAsync(id, ct);
        if (dict is null)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "字典不存在");
        }

        var items = await _dictItems.ListAsync(i => i.DictCode == dict.Code, ct);
        foreach (var item in items)
        {
            await _dictItems.DeleteAsync(item, ct);
        }

        await _dicts.DeleteAsync(dict, ct);
        await _cache.RemoveAsync(CacheKey(dict.Code), ct);
        await _auditLogger.LogAsync("dict.delete", "dict", dict.Id.ToString(), $"删除字典 {dict.Code}", ct);
        return Result.Ok();
    }

    /// <summary>新增字典项。</summary>
    public async Task<Result<DictItemDto>> AddItemAsync(string dictCode, CreateDictItemRequest request, CancellationToken ct)
    {
        var dict = await _dicts.FirstOrDefaultAsync(d => d.Code == dictCode, ct);
        if (dict is null)
        {
            return Result<DictItemDto>.Fail(ErrorCodes.CommonNotFound, "字典不存在");
        }

        if (string.IsNullOrWhiteSpace(request.ItemCode) || string.IsNullOrWhiteSpace(request.ItemName))
        {
            return Result<DictItemDto>.Fail(ErrorCodes.CommonValidationFailed, "字典项编码与名称不能为空");
        }

        if (await _dictItems.AnyAsync(i => i.DictCode == dictCode && i.ItemCode == request.ItemCode, ct))
        {
            return Result<DictItemDto>.Fail(ErrorCodes.CommonValidationFailed, "字典项编码已存在");
        }

        var item = new DictItem
        {
            DictCode = dictCode,
            ItemCode = request.ItemCode.Trim(),
            ItemName = request.ItemName.Trim(),
            Sort = request.Sort,
            Enabled = request.Enabled
        };
        await _dictItems.AddAsync(item, ct);
        await _cache.RemoveAsync(CacheKey(dictCode), ct);
        return Result<DictItemDto>.Ok(ToItemDto(item));
    }

    /// <summary>更新字典项。</summary>
    public async Task<Result<DictItemDto>> UpdateItemAsync(long itemId, UpdateDictItemRequest request, CancellationToken ct)
    {
        var item = await _dictItems.GetByIdAsync(itemId, ct);
        if (item is null)
        {
            return Result<DictItemDto>.Fail(ErrorCodes.CommonNotFound, "字典项不存在");
        }

        if (string.IsNullOrWhiteSpace(request.ItemName))
        {
            return Result<DictItemDto>.Fail(ErrorCodes.CommonValidationFailed, "字典项名称不能为空");
        }

        item.ItemName = request.ItemName.Trim();
        item.Sort = request.Sort;
        item.Enabled = request.Enabled;
        item.UpdatedBy = _currentUser.UserId;
        await _dictItems.UpdateAsync(item, ct);
        await _cache.RemoveAsync(CacheKey(item.DictCode), ct);
        return Result<DictItemDto>.Ok(ToItemDto(item));
    }

    /// <summary>删除字典项。</summary>
    public async Task<Result> DeleteItemAsync(long itemId, CancellationToken ct)
    {
        var item = await _dictItems.GetByIdAsync(itemId, ct);
        if (item is null)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "字典项不存在");
        }

        await _dictItems.DeleteAsync(item, ct);
        await _cache.RemoveAsync(CacheKey(item.DictCode), ct);
        return Result.Ok();
    }

    private async Task<IReadOnlyList<DictItem>> GetItemsCachedAsync(string dictCode, CancellationToken ct)
    {
        var key = CacheKey(dictCode);
        var cached = await _cache.GetStringAsync(key, ct);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<DictItem>>(cached) ?? [];
        }

        var items = await _dictItems.ListAsync(i => i.DictCode == dictCode, ct);
        await _cache.SetStringAsync(
            key,
            System.Text.Json.JsonSerializer.Serialize(items),
            TimeSpan.FromMinutes(30), ct);
        return items;
    }

    private static string CacheKey(string dictCode) => $"{CacheKeyPrefix}{dictCode}";

    private static DictDto ToDto(Dict dict, IEnumerable<DictItem> items) =>
        new(dict.Id, dict.Code, dict.Name, dict.Description,
            items.Select(ToItemDto).ToArray());

    private static DictItemDto ToItemDto(DictItem item) =>
        new(item.Id, item.ItemCode, item.ItemName, item.Sort, item.Enabled);
}
