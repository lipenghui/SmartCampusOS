using FreeSql;
using IdentityService.Application.Configuration;
using IdentityService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Persistence;

/// <summary>
/// 启动初始化：FreeSql CodeFirst 结构同步（LLD §10.4，仅追加不删列）+ 种子数据。
/// </summary>
public sealed class DatabaseInitializer : IHostedService
{
    private static readonly Type[] EntityTypes =
    [
        typeof(User), typeof(StudentProfile), typeof(ParentBinding), typeof(OrgUnit),
        typeof(Role), typeof(Permission), typeof(UserRole), typeof(RefreshToken),
        typeof(AuditLog), typeof(Dict), typeof(DictItem), typeof(RolePermission)
    ];

    private readonly IFreeSql _fsql;
    private readonly SeedOptions _seed;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IFreeSql fsql,
        SeedOptions seed,
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _fsql = fsql;
        _seed = seed;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_fsql.CodeFirst.IsAutoSyncStructure)
            {
                _fsql.CodeFirst.SyncStructure(EntityTypes);
                _logger.LogInformation("FreeSql CodeFirst 结构同步完成（{Count} 个实体）", EntityTypes.Length);
            }

            using var scope = _scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<DbSeeder>().Seed();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // 基础设施（MySQL）不可用时启动失败，便于开发/部署尽早暴露（LLD §10 部署依赖）
            _logger.LogError(ex, "IdentityService 数据库初始化失败");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
