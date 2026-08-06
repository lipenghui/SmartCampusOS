using FreeSql;
using IdentityService.Application.Abstractions.Persistence;

namespace IdentityService.Infrastructure.Persistence;

/// <summary>
/// 工作单元实现：基于 FreeSql 事务（LLD §2.6.4 事务由用例层协调）。
/// FreeSql IAdo.Transaction 为同步 API（官方说明不支持异步），
/// 通过 Task.Run 承载异步用例动作，避免在无同步上下文的 ASP.NET Core 环境死锁。
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IFreeSql _fsql;

    public UnitOfWork(IFreeSql fsql) => _fsql = fsql;

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            T? result = default;
            _fsql.Ado.Transaction(() => { result = action().GetAwaiter().GetResult(); });
            return result!;
        }, ct);

    public Task ExecuteAsync(Func<Task> action, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            _fsql.Ado.Transaction(() => action().GetAwaiter().GetResult());
        }, ct);
}
