using FileService.Application.Abstractions.Persistence;
using FileService.Application.Abstractions.Storage;
using FileService.Infrastructure.Configuration;
using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Storage;
using FreeSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace FileService.Infrastructure.DependencyInjection;

/// <summary>
/// 基础设施层 DI 注册扩展（组合根唯一调用点，LLD §2.6.4）。
/// 注册 FreeSql、MinIO、仓储、图片处理等实现。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 FileService 基础设施层全部依赖。
    /// </summary>
    public static IServiceCollection AddFileInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // 强类型配置
        var options = new FileServiceOptions();
        configuration.GetSection(FileServiceOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        // 数据访问：单例 IFreeSql（LLD §2.4，CodeFirst 结构同步）
        services.AddSingleton<IFreeSql>(_ => FreeSqlFactory.Build(options));

        // 仓储 + 工作单元（LLD §2.6.4）
        services.AddScoped(typeof(IRepository<>), typeof(FreeSqlRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // MinIO 客户端
        services.AddSingleton<IMinioClient>(_ =>
        {
            var client = new MinioClient()
                .WithEndpoint(options.Minio.Endpoint)
                .WithCredentials(options.Minio.AccessKey, options.Minio.SecretKey);

            if (options.Minio.UseSsl)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        // MinIO Options（供 MinioFileStorageService 使用）
        services.AddSingleton(options.Minio);

        // 文件存储服务
        services.AddSingleton<IFileStorageService, MinioFileStorageService>();

        // 图片处理器（SixLabors.ImageSharp 实现，LLD §3.8）
        services.AddSingleton<IImageProcessor, ImageSharpImageProcessor>();

        // 启动初始化
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}