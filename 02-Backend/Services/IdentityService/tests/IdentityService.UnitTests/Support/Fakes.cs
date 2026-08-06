using IdentityService.Application.Abstractions.Auth;
using IdentityService.Application.Abstractions.Caching;
using IdentityService.Application.Abstractions.Persistence;

namespace IdentityService.UnitTests.Support;

/// <summary>内存缓存（单元测试用）。</summary>
public sealed class FakeCache : ICache
{
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _store = [];

    public IReadOnlyDictionary<string, (string Value, DateTimeOffset? ExpiresAt)> Store => _store;

    public Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry)
            && (entry.ExpiresAt is null || entry.ExpiresAt > DateTimeOffset.UtcNow))
        {
            return Task.FromResult<string?>(entry.Value);
        }

        return Task.FromResult<string?>(null);
    }

    public Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        _store[key] = (value, expiry is null ? null : DateTimeOffset.UtcNow + expiry);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_store.Remove(key));

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_store.ContainsKey(key));
}

/// <summary>内存验证码发送器（记录调用）。</summary>
public sealed class FakeCodeSender : ICodeSender
{
    public List<(string Mobile, string Code)> Sent { get; } = [];

    public Task SendAsync(string mobile, string code, CancellationToken ct = default)
    {
        Sent.Add((mobile, code));
        return Task.CompletedTask;
    }
}

/// <summary>内存工作单元（直接执行，不做事务）。</summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default) => action();

    public Task ExecuteAsync(Func<Task> action, CancellationToken ct = default) => action();
}
