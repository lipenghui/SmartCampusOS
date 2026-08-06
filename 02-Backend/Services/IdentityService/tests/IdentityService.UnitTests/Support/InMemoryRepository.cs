using System.Linq.Expressions;
using IdentityService.Application.Abstractions.Persistence;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.UnitTests.Support;

/// <summary>
/// 内存仓储实现（单元测试用）：基于 List 的简单 IRepository&lt;T&gt;。
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items = [];
    private readonly Func<T, long> _idSelector;

    public InMemoryRepository(Func<T, long>? idSelector = null)
    {
        _idSelector = idSelector ?? (entity =>
            (long)(entity.GetType().GetProperty("Id")?.GetValue(entity) ?? 0L));
    }

    public IReadOnlyList<T> Items => _items;

    public Task<T?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(e => _idSelector(e) == id));

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(predicate.Compile()));

    public Task<List<T>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.ToList());

    public Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Task.FromResult(_items.Where(predicate.Compile()).ToList());

    public async Task<PagedResult<T>> PageAsync(
        Expression<Func<T, bool>>? predicate, int page, int pageSize, CancellationToken ct = default)
    {
        var query = predicate is null ? _items : _items.Where(predicate.Compile()).ToList();
        var total = query.Count;
        var items = query
            .OrderByDescending(_idSelector)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return PagedResult<T>.From(items, total, page, pageSize);
    }

    public Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Task.FromResult((long)_items.Count(predicate.Compile()));

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Task.FromResult(_items.Any(predicate.Compile()));

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        _items.AddRange(entities);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        var id = _idSelector(entity);
        var index = _items.FindIndex(e => _idSelector(e) == id);
        if (index >= 0)
        {
            _items[index] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _items.Remove(entity);
        return Task.CompletedTask;
    }
}
