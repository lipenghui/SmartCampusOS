using FileService.Infrastructure.Configuration;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace FileService.Infrastructure.Persistence;

/// <summary>
/// 启动初始化：FreeSql 结构同步 + MinIO Bucket 创建（LLD §10.4）。
/// </summary>
public sealed class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IServiceProvider sp, ILogger<DatabaseInitializer> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();

        // FreeSql CodeFirst 结构同步
        var fsql = scope.ServiceProvider.GetRequiredService<IFreeSql>();
        var options = scope.ServiceProvider.GetRequiredService<FileServiceOptions>();

        if (options.Database.SyncStructureOnStartup)
        {
            _logger.LogInformation("开始同步 file_db 表结构...");
            fsql.CodeFirst.SyncStructure(typeof(FileService.Domain.Entities.FileObject));
            _logger.LogInformation("file_db 表结构同步完成");
        }

        // MinIO Bucket 初始化
        if (options.Minio.CreateBucketOnStartup)
        {
            var minio = scope.ServiceProvider.GetRequiredService<IMinioClient>();
            var bucketExists = await minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(options.Minio.BucketName), ct);

            if (!bucketExists)
            {
                await minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(options.Minio.BucketName), ct);
                _logger.LogInformation("MinIO Bucket '{Bucket}' 创建成功", options.Minio.BucketName);
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}