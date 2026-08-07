namespace FileService.Application.Abstractions.Storage;

/// <summary>
/// 对象存储抽象（LLD §2.4：MinIO-OSS）：文件上传、下载、签名 URL 生成、删除。
/// Infrastructure 层提供 MinIO 实现。
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传文件到对象存储。
    /// </summary>
    /// <param name="storageKey">存储 Key（唯一标识）。</param>
    /// <param name="stream">文件流。</param>
    /// <param name="contentType">MIME 类型。</param>
    /// <param name="ct">取消令牌。</param>
    Task UploadAsync(string storageKey, Stream stream, string contentType, CancellationToken ct = default);

    /// <summary>
    /// 下载文件。
    /// </summary>
    /// <param name="storageKey">存储 Key。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>文件流。</returns>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken ct = default);

    /// <summary>
    /// 生成签名 URL（时效 30 分钟，LLD §3.8）。
    /// </summary>
    /// <param name="storageKey">存储 Key。</param>
    /// <param name="expiry">过期时间（默认 30 分钟）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>签名 URL。</returns>
    Task<string> GetPresignedUrlAsync(string storageKey, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// 删除对象存储中的文件。
    /// </summary>
    /// <param name="storageKey">存储 Key。</param>
    /// <param name="ct">取消令牌。</param>
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}