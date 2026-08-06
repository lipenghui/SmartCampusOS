using IdentityService.Domain.Enums;

namespace IdentityService.Application.DTOs.Dicts;

/// <summary>创建字典请求（LLD §3.2 GET/POST /api/v1/dicts）。</summary>
public sealed record CreateDictRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>更新字典请求。</summary>
public sealed record UpdateDictRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>创建字典项请求。</summary>
public sealed record CreateDictItemRequest
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Sort { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>更新字典项请求。</summary>
public sealed record UpdateDictItemRequest
{
    public string ItemName { get; set; } = string.Empty;
    public int Sort { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>字典项 DTO。</summary>
public sealed record DictItemDto(long Id, string ItemCode, string ItemName, int Sort, bool Enabled);

/// <summary>字典 DTO（含字典项）。</summary>
public sealed record DictDto(long Id, string Code, string Name, string? Description, IReadOnlyList<DictItemDto> Items);
