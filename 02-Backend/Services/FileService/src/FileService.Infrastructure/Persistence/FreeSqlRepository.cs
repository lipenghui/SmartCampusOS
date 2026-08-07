using System.Linq.Expressions;
using System.Reflection;
using FileService.Application.Abstractions.Persistence;
using FreeSql;
using SmartCampusOS.SharedKernel.Results;

namespace FileService.Infrastructure.Persistence;

/// <summary>
/// FreeSql 泛型仓储实现（LLD §2.6.4：FreeSql 仅存在于 Infrastructure）。
/// 软删除：实体含 IsDeleted 属性时置位更新。
/// </summary>
public sealed class FreeSqlRepository<T> : IRepository<T> where T : class
{
    private static readonly PropertyInfo? IsDeletedProperty =
        typeof(T).GetProperty("IsDeleted");

    private readonly IFreeSql _fsql;

    public FreeSqlRepository(IFreeSql fsql) => _fsql = fsql;

    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _fsql.Select<T>().WhereDynamic(id).FirstAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await _fsql.Select<T>().Where(predicate).FirstAsync(ct);

    public Task<List<T>> ListAsync(CancellationToken ct = default) =>
        _fsql.Select<T>().ToListAsync(ct);

    public Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _fsql.Select<T>().Where(predicate).ToListAsync(ct);

    public async Task<PagedResult<T>> PageAsync(
        Expression<Func<T, bool>>? predicate, int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 200);
        var query = _fsql.Select<T>();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByPropertyName("Id", true)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(ct);
        return PagedResult<T>.From(items, total, safePage, safeSize);
    }

    public Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _fsql.Select<T>().Where(predicate).CountAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        _fsql.Select<T>().Where(predicate).AnyAsync(ct);

    public Task AddAsync(T entity, CancellationToken ct = default) =>
        _fsql.Insert(entity).ExecuteAffrowsAsync(ct);

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) =>
        _fsql.Insert(entities).ExecuteAffrowsAsync(ct);

    public Task UpdateAsync(T entity, CancellationToken ct = default) =>
        _fsql.Update<T>().SetSource(entity).ExecuteAffrowsAsync(ct);

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        if (IsDeletedProperty is not null)
        {
            IsDeletedProperty.SetValue(entity, true);
            return UpdateAsync(entity, ct);
        }

        return _fsql.Delete<T>(entity).ExecuteAffrowsAsync(ct);
    }
}