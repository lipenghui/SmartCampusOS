using System.Linq.Expressions;
using SmartCampusOS.SharedKernel.Results;

namespace FileService.Application.Abstractions.Persistence;

/// <summary>
/// 泛型仓储抽象（LLD §2.6.4：Domain/Application 仅依赖本抽象，FreeSql 只存在于 Infrastructure）。
/// </summary>
/// <typeparam name="T">实体类型。</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>按主键查询。</summary>
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>按条件查询单条。</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>查询全部。</summary>
    Task<List<T>> ListAsync(CancellationToken ct = default);

    /// <summary>按条件查询列表。</summary>
    Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>分页查询（LLD §5.1.3：{ items, total }）。</summary>
    Task<PagedResult<T>> PageAsync(
        Expression<Func<T, bool>>? predicate, int page, int pageSize, CancellationToken ct = default);

    /// <summary>按条件计数。</summary>
    Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>是否存在满足条件的记录。</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>新增。</summary>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>批量新增。</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    /// <summary>更新（含审计字段维护）。</summary>
    Task UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>删除：实体含 IsDeleted 字段时软删除，否则物理删除。</summary>
    Task DeleteAsync(T entity, CancellationToken ct = default);
}