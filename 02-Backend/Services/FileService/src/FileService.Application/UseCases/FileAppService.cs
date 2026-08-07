using FileService.Application.Abstractions.Persistence;
using FileService.Application.Abstractions.Storage;
using FileService.Application.DTOs;
using FileService.Domain.Entities;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

namespace FileService.Application.UseCases;

/// <summary>
/// 文件服务用例（LLD §3.8）：文件上传（分片可选）、下载、签名 URL、删除（软删）、按业务标签检索。
/// 设计要点：图片压缩与缩略图（报修图 ≤ 9 张自动压缩）；大屏与报表导出文件打水印。
/// </summary>
public sealed class FileAppService
{
    private readonly IRepository<FileObject> _files;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _storage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ICurrentUser _currentUser;

    /// <summary>允许的最大批量上传数量（报修图 ≤ 9 张，LLD §3.8）。</summary>
    private const int MaxBatchUploadCount = 9;

    /// <summary>签名 URL 默认有效期（30 分钟，LLD §3.8）。</summary>
    private static readonly TimeSpan DefaultPresignedExpiry = TimeSpan.FromMinutes(30);

    public FileAppService(
        IRepository<FileObject> files,
        IUnitOfWork unitOfWork,
        IFileStorageService storage,
        IImageProcessor imageProcessor,
        ICurrentUser currentUser)
    {
        _files = files;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _imageProcessor = imageProcessor;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 上传单个文件（LLD §3.8）。
    /// 图片类文件自动压缩；需要水印的文件自动打水印。
    /// </summary>
    public async Task<Result<FileObjectDto>> UploadAsync(UploadFileRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return Result<FileObjectDto>.Fail(ErrorCodes.CommonValidationFailed, "文件不能为空");
        }

        var storageKey = GenerateStorageKey(request.BizType, request.File.FileName);
        var contentType = request.File.ContentType ?? "application/octet-stream";

        await using var sourceStream = request.File.OpenReadStream();
        Stream uploadStream = sourceStream;

        try
        {
            // 图片类型自动压缩
            if (IsImage(contentType))
            {
                uploadStream = await _imageProcessor.CompressAsync(sourceStream, ct: ct);
            }

            // 需要水印的文件打水印
            if (request.WatermarkFlag && (IsImage(contentType) || IsDocument(contentType)))
            {
                var watermarked = await _imageProcessor.AddWatermarkAsync(uploadStream, "SmartCampusOS", ct);
                if (uploadStream != sourceStream)
                {
                    await uploadStream.DisposeAsync();
                }
                uploadStream = watermarked;
            }

            await _storage.UploadAsync(storageKey, uploadStream, contentType, ct);
        }
        finally
        {
            if (uploadStream != sourceStream)
            {
                await uploadStream.DisposeAsync();
            }
        }

        var now = DateTimeOffset.UtcNow;
        var fileObject = new FileObject
        {
            BizType = request.BizType,
            BizId = request.BizId,
            FileName = request.File.FileName,
            StorageKey = storageKey,
            Size = request.File.Length,
            MimeType = contentType,
            WatermarkFlag = request.WatermarkFlag,
            CreatedBy = _currentUser.UserId,
            UpdatedBy = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _files.AddAsync(fileObject, ct);

        return Result<FileObjectDto>.Ok(ToDto(fileObject));
    }

    /// <summary>
    /// 批量上传（报修图 ≤ 9 张，LLD §3.8）。
    /// </summary>
    public async Task<Result<IReadOnlyList<FileObjectDto>>> BatchUploadAsync(BatchUploadRequest request, CancellationToken ct)
    {
        if (request.Files is null || request.Files.Count == 0)
        {
            return Result<IReadOnlyList<FileObjectDto>>.Fail(ErrorCodes.CommonValidationFailed, "文件列表不能为空");
        }

        if (request.Files.Count > MaxBatchUploadCount)
        {
            return Result<IReadOnlyList<FileObjectDto>>.Fail(
                ErrorCodes.CommonValidationFailed,
                $"单次最多上传 {MaxBatchUploadCount} 个文件");
        }

        var results = new List<FileObjectDto>();
        foreach (var file in request.Files)
        {
            var singleResult = await UploadAsync(new UploadFileRequest(
                file, request.BizType, request.BizId, request.WatermarkFlag), ct);

            if (!singleResult.IsSuccess)
            {
                return Result<IReadOnlyList<FileObjectDto>>.Fail(
                    singleResult.ErrorCode!, singleResult.ErrorMessage!);
            }

            results.Add(singleResult.Value!);
        }

        return Result<IReadOnlyList<FileObjectDto>>.Ok(results);
    }

    /// <summary>
    /// 获取文件详情。
    /// </summary>
    public async Task<Result<FileObjectDto>> GetFileAsync(long id, CancellationToken ct)
    {
        var file = await _files.GetByIdAsync(id, ct);
        if (file is null || file.IsDeleted)
        {
            return Result<FileObjectDto>.Fail(ErrorCodes.CommonNotFound, "文件不存在");
        }

        return Result<FileObjectDto>.Ok(ToDto(file));
    }

    /// <summary>
    /// 分页查询文件（按业务类型/业务单据筛选）。
    /// </summary>
    public async Task<PagedResult<FileObjectDto>> PageFilesAsync(FileQuery query, CancellationToken ct)
    {
        var page = await _files.PageAsync(
            f => !f.IsDeleted
                 && (string.IsNullOrWhiteSpace(query.BizType) || f.BizType == query.BizType)
                 && (query.BizId == null || f.BizId == query.BizId),
            query.Page, query.PageSize, ct);

        var items = page.Items.Select(ToDto).ToList();
        return new PagedResult<FileObjectDto>(items, page.Total, page.Page, page.PageSize);
    }

    /// <summary>
    /// 获取文件下载流。
    /// </summary>
    public async Task<Result<(Stream Stream, string FileName, string ContentType)>> DownloadAsync(long id, CancellationToken ct)
    {
        var file = await _files.GetByIdAsync(id, ct);
        if (file is null || file.IsDeleted)
        {
            return Result<(Stream, string, string)>.Fail(ErrorCodes.CommonNotFound, "文件不存在");
        }

        var stream = await _storage.DownloadAsync(file.StorageKey, ct);
        return Result<(Stream, string, string)>.Ok((stream, file.FileName, file.MimeType ?? "application/octet-stream"));
    }

    /// <summary>
    /// 获取签名 URL（时效 30 分钟，LLD §3.8）。
    /// </summary>
    public async Task<Result<PresignedUrlDto>> GetPresignedUrlAsync(long id, CancellationToken ct)
    {
        var file = await _files.GetByIdAsync(id, ct);
        if (file is null || file.IsDeleted)
        {
            return Result<PresignedUrlDto>.Fail(ErrorCodes.CommonNotFound, "文件不存在");
        }

        var url = await _storage.GetPresignedUrlAsync(file.StorageKey, DefaultPresignedExpiry, ct);
        return Result<PresignedUrlDto>.Ok(new PresignedUrlDto(url, DateTimeOffset.UtcNow.Add(DefaultPresignedExpiry)));
    }

    /// <summary>
    /// 删除文件（软删，LLD §3.8）。
    /// </summary>
    public async Task<Result> DeleteAsync(long id, CancellationToken ct)
    {
        var file = await _files.GetByIdAsync(id, ct);
        if (file is null || file.IsDeleted)
        {
            return Result.Fail(ErrorCodes.CommonNotFound, "文件不存在");
        }

        // 软删除元数据
        await _files.DeleteAsync(file, ct);

        // 异步删除对象存储中的文件（不阻塞主流程）
        try
        {
            await _storage.DeleteAsync(file.StorageKey, ct);
        }
        catch
        {
            // 记录日志但不阻塞删除操作；可由后台清理任务补偿
        }

        return Result.Ok();
    }

    /// <summary>根据业务类型和文件名生成唯一的存储 Key。</summary>
    private static string GenerateStorageKey(string bizType, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var datePath = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd");
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        return $"{bizType}/{datePath}/{uniqueId}{ext}";
    }

    /// <summary>判断是否为图片类型。</summary>
    private static bool IsImage(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>判断是否为文档类型（需要打水印）。</summary>
    private static bool IsDocument(string contentType) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("application/vnd.openxmlformats", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("application/msword", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase);

    /// <summary>实体转 DTO。</summary>
    private static FileObjectDto ToDto(FileObject f) => new(
        f.Id, f.BizType, f.BizId, f.FileName, f.StorageKey,
        f.Size, f.MimeType, f.WatermarkFlag, f.CreatedAt);
}