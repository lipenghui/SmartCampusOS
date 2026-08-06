namespace IdentityService.Application.Abstractions.Persistence;

/// <summary>
/// 工作单元抽象（LLD §2.6.4：事务由用例层协调）。
/// </summary>
public interface IUnitOfWork
{
    /// <summary>在事务中执行有返回值的操作。</summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default);

    /// <summary>在事务中执行无返回值操作。</summary>
    Task ExecuteAsync(Func<Task> action, CancellationToken ct = default);
}
