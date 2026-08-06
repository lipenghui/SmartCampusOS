using System.Security.Cryptography;
using System.Text;
using FreeSql;
using IdentityService.Application.Abstractions.Audit;
using IdentityService.Application.Abstractions.Auth;
using IdentityService.Application.Abstractions.Caching;
using IdentityService.Application.Abstractions.Events;
using IdentityService.Application.Abstractions.Import;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.Abstractions.Security;
using IdentityService.Application.Configuration;
using IdentityService.Application.UseCases.Audit;
using IdentityService.Application.UseCases.Dicts;
using IdentityService.Application.UseCases.Orgs;
using IdentityService.Application.UseCases.Parents;
using IdentityService.Application.UseCases.Roles;
using IdentityService.Application.UseCases.Users;
using IdentityService.Infrastructure.Audit;
using IdentityService.Infrastructure.Auth;
using IdentityService.Infrastructure.Caching;
using IdentityService.Infrastructure.Events;
using IdentityService.Infrastructure.Import;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Infrastructure.Security;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IdentityService.Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层 DI 注册扩展（组合根唯一调用点，LLD §2.6.4）。
/// 注册 FreeSql、Redis、事件发布、仓储、安全原语等实现。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 IdentityService 基础设施层全部依赖。
    /// </summary>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var identityOptions = configuration.GetIdentityOptions();

        // 数据访问：单例 IFreeSql（LLD §2.4，CodeFirst 结构同步）
        services.AddSingleton(identityOptions);
        services.AddSingleton(identityOptions.Seed);
        services.AddSingleton<IFreeSql>(_ => FreeSqlFactory.Build(identityOptions));

        // 仓储 + 工作单元（LLD §2.6.4：用例层依赖 IRepository<T> / IUnitOfWork 抽象）
        services.AddScoped(typeof(IRepository<>), typeof(FreeSqlRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 缓存：Redis（LLD §9），连接失败自动降级
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(identityOptions.Redis.ConnectionString));
        services.AddSingleton<ICache, RedisCache>();

        // 安全原语（LLD §8.3）
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
        services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
        services.AddSingleton<IFieldEncryptor>(_ =>
            new AesGcmFieldEncryptor(SHA256.HashData(Encoding.UTF8.GetBytes(identityOptions.Encryption.AesKey))));

        // 启动初始化：结构同步 + 种子数据
        services.AddScoped<DbSeeder>();
        services.AddHostedService<DatabaseInitializer>();

        // 验证码发送（开发环境日志输出，真实渠道为扩展点）
        services.AddSingleton<ICodeSender, CaptchaCodeSender>();

        // Excel 导入
        services.AddSingleton<IUserExcelImporter, NpoiUserExcelImporter>();

        // 应用层用例（组合根经 Application 注册，此处补充依赖 Infrastructure 实现的用例）
        services.AddScoped<UserService>();
        services.AddScoped<OrgService>();
        services.AddScoped<RoleService>();
        services.AddScoped<DictService>();
        services.AddScoped<AuditService>();
        services.AddScoped<ParentBindingService>();

        // 审计日志（LLD §8.3：写操作留痕 sys_audit_log）
        services.AddScoped<IAuditLogger, AuditLogger>();

        // 事件发布（LLD §7）：配置 RabbitMq.Enabled 时走 MassTransit + RabbitMQ，否则进程内发布
        if (identityOptions.RabbitMq.Enabled)
        {
            services.AddMassTransit(bus =>
            {
                bus.UsingRabbitMq((_, cfg) =>
                {
                    cfg.Host(identityOptions.RabbitMq.Host, identityOptions.RabbitMq.VirtualHost, host =>
                    {
                        host.Username(identityOptions.RabbitMq.Username);
                        host.Password(identityOptions.RabbitMq.Password);
                    });
                });
            });
            services.AddSingleton<IEventPublisher, MassTransitEventPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        }

        return services;
    }
}
