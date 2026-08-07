using FileService.Application.Abstractions.Storage;
using FileService.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace FileService.Infrastructure.Storage;

/// <summary>
/// MinIO 对象存储实现（LLD §2.4：对象存储 MinIO-OSS）。
/// </summary>
public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(
        IMinioClient minio,
        MinioOptions options,
        ILogger<MinioFileStorageService> logger)
    {
        _minio = minio;
        _options = options;
        _logger = logger;
    }

    public async Task UploadAsync(string storageKey, Stream stream, string contentType, CancellationToken ct = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(args, ct);
        _logger.LogInformation("文件上传成功: {StorageKey}, Size={Size}", storageKey, stream.Length);
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken ct = default)
    {
        var memoryStream = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey)
            .WithCallbackStream(async (stream, token) =>
            {
                await stream.CopyToAsync(memoryStream, token);
                memoryStream.Position = 0;
            });

        await _minio.GetObjectAsync(args, ct);
        return memoryStream;
    }

    public async Task<string> GetPresignedUrlAsync(string storageKey, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var effectiveExpiry = expiry ?? TimeSpan.FromMinutes(30);

        var args = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey)
            .WithExpiry((int)effectiveExpiry.TotalSeconds);

        var url = await _minio.PresignedGetObjectAsync(args);
        _logger.LogInformation("生成签名URL: {StorageKey}, Expires={Expiry}", storageKey, effectiveExpiry);
        return url;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storageKey);

        await _minio.RemoveObjectAsync(args, ct);
        _logger.LogInformation("文件删除成功: {StorageKey}", storageKey);
    }
}