namespace FileService.Infrastructure.Configuration;

/// <summary>
/// FileService 强类型配置（LLD §2.6.7：IOptions&lt;T&gt; + 配置类绑定）。
/// </summary>
public sealed class FileServiceOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "FileService";

    /// <summary>数据库配置。</summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>MinIO 对象存储配置。</summary>
    public MinioOptions Minio { get; set; } = new();

    /// <summary>文件上传限制。</summary>
    public UploadOptions Upload { get; set; } = new();
}

/// <summary>数据库配置。</summary>
public sealed class DatabaseOptions
{
    /// <summary>连接字符串。</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>启动时是否同步表结构（CodeFirst）。</summary>
    public bool SyncStructureOnStartup { get; set; } = true;
}

/// <summary>MinIO 对象存储配置。</summary>
public sealed class MinioOptions
{
    /// <summary>服务端点。</summary>
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>Access Key。</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Secret Key。</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Bucket 名称。</summary>
    public string BucketName { get; set; } = "smartcampus-files";

    /// <summary>是否使用 SSL。</summary>
    public bool UseSsl { get; set; }

    /// <summary>是否在启动时创建 Bucket。</summary>
    public bool CreateBucketOnStartup { get; set; } = true;
}

/// <summary>文件上传限制配置。</summary>
public sealed class UploadOptions
{
    /// <summary>单文件最大大小（字节，默认 50MB）。</summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>允许的文件扩展名（逗号分隔，空表示不限）。</summary>
    public string AllowedExtensions { get; set; } = string.Empty;

    /// <summary>批量上传最大数量。</summary>
    public int MaxBatchCount { get; set; } = 9;
}