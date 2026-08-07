using FileService.Domain.Common;

namespace FileService.Domain.Entities;

/// <summary>
/// 文件元数据实体（表 file_object，LLD §9 / 数据库设计文档 §9.1）：附件上传、下载、签名URL、防盗链。
/// </summary>
[EntityTable("file_object")]
public sealed class FileObject : AuditableEntity
{
    /// <summary>业务类型（如 repair/avatar/announcement 等）。</summary>
    public string BizType { get; set; } = string.Empty;

    /// <summary>业务单据 ID。</summary>
    public long? BizId { get; set; }

    /// <summary>原始文件名。</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>对象存储 Key，唯一索引。</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>文件大小（字节）。</summary>
    public long Size { get; set; }

    /// <summary>MIME 类型。</summary>
    public string? MimeType { get; set; }

    /// <summary>是否水印：0 否 / 1 是。</summary>
    public bool WatermarkFlag { get; set; }
}