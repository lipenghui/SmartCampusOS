using Microsoft.AspNetCore.Http;

namespace FileService.Application.DTOs;

/// <summary>
/// 文件上传请求。
/// </summary>
/// <param name="File">上传的文件。</param>
/// <param name="BizType">业务类型。</param>
/// <param name="BizId">业务单据 ID。</param>
/// <param name="WatermarkFlag">是否添加水印。</param>
public sealed record UploadFileRequest(
    IFormFile File,
    string BizType,
    long? BizId = null,
    bool WatermarkFlag = false);

/// <summary>
/// 文件信息响应。
/// </summary>
public sealed record FileObjectDto(
    long Id,
    string BizType,
    long? BizId,
    string FileName,
    string StorageKey,
    long Size,
    string? MimeType,
    bool WatermarkFlag,
    DateTimeOffset CreatedAt);

/// <summary>
/// 文件列表查询请求。
/// </summary>
/// <param name="BizType">业务类型（可选）。</param>
/// <param name="BizId">业务单据 ID（可选）。</param>
/// <param name="Page">页码。</param>
/// <param name="PageSize">每页条数。</param>
public sealed record FileQuery(
    string? BizType = null,
    long? BizId = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// 签名 URL 响应。
/// </summary>
/// <param name="Url">签名 URL。</param>
/// <param name="ExpiresAt">过期时间。</param>
public sealed record PresignedUrlDto(
    string Url,
    DateTimeOffset ExpiresAt);

/// <summary>
/// 批量文件上传请求（如报修图 ≤ 9 张）。
/// </summary>
/// <param name="Files">文件列表。</param>
/// <param name="BizType">业务类型。</param>
/// <param name="BizId">业务单据 ID。</param>
/// <param name="WatermarkFlag">是否添加水印。</param>
public sealed record BatchUploadRequest(
    IReadOnlyList<IFormFile> Files,
    string BizType,
    long? BizId = null,
    bool WatermarkFlag = false);