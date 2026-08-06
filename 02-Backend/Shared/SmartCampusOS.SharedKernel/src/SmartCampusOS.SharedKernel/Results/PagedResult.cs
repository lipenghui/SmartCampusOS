namespace SmartCampusOS.SharedKernel.Results;

/// <summary>
/// 统一分页响应，对齐 LLD §5.1.3：<c>{ "items": [], "total": n }</c>。
/// </summary>
/// <typeparam name="T">列表项类型。</typeparam>
/// <param name="Items">当前页数据。</param>
/// <param name="Total">满足条件的总条数。</param>
/// <param name="Page">当前页码（从 1 开始）。</param>
/// <param name="PageSize">每页条数。</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, long Total, int Page = 1, int PageSize = 20)
{
    /// <summary>空页（查询前或无条件时使用）。</summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
        new([], 0, page, pageSize);

    /// <summary>从枚举构造分页结果。</summary>
    public static PagedResult<T> From(IEnumerable<T> items, long total, int page, int pageSize) =>
        new(items as IReadOnlyList<T> ?? items.ToArray(), total, page, pageSize);

    /// <summary>总页数（向上取整）。</summary>
    public long TotalPages => PageSize <= 0 ? 0 : (Total + PageSize - 1) / PageSize;

    /// <summary>是否还有下一页。</summary>
    public bool HasNext => Page < TotalPages;
}
