using FileService.Application.DTOs;
using FileService.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace FileService.Api.Controllers;

/// <summary>
/// 文件服务控制器（LLD §3.8）：附件上传、下载、签名 URL、删除、按业务标签检索。
/// </summary>
[ApiController]
[Route("api/v1/files")]
public class FilesController : ControllerBase
{
    private readonly FileAppService _fileService;

    public FilesController(FileAppService fileService) => _fileService = fileService;

    /// <summary>
    /// 上传单个文件（LLD §3.8）。
    /// 图片类文件自动压缩；需要水印的文件自动打水印。
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string bizType,
        [FromForm] long? bizId = null,
        [FromForm] bool watermarkFlag = false,
        CancellationToken ct = default)
    {
        var request = new UploadFileRequest(file, bizType, bizId, watermarkFlag);
        var result = await _fileService.UploadAsync(request, ct);
        return Ok(ApiResponse<FileObjectDto>.From(result));
    }

    /// <summary>
    /// 批量上传（报修图 ≤ 9 张，LLD §3.8）。
    /// </summary>
    [HttpPost("batch-upload")]
    [RequestSizeLimit(450 * 1024 * 1024)] // 9 * 50MB
    public async Task<IActionResult> BatchUpload(
        [FromForm] List<IFormFile> files,
        [FromForm] string bizType,
        [FromForm] long? bizId = null,
        [FromForm] bool watermarkFlag = false,
        CancellationToken ct = default)
    {
        var request = new BatchUploadRequest(files, bizType, bizId, watermarkFlag);
        var result = await _fileService.BatchUploadAsync(request, ct);
        return Ok(ApiResponse<IReadOnlyList<FileObjectDto>>.From(result));
    }

    /// <summary>
    /// 获取文件详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetFile(long id, CancellationToken ct)
    {
        var result = await _fileService.GetFileAsync(id, ct);
        return Ok(ApiResponse<FileObjectDto>.From(result));
    }

    /// <summary>
    /// 分页查询文件（按业务类型/业务单据筛选）。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PageFiles(
        [FromQuery] string? bizType,
        [FromQuery] long? bizId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new FileQuery(bizType, bizId, page, pageSize);
        var result = await _fileService.PageFilesAsync(query, ct);
        return Ok(ApiResponse<PagedResult<FileObjectDto>>.Ok(result));
    }

    /// <summary>
    /// 下载文件。
    /// </summary>
    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        var result = await _fileService.DownloadAsync(id, ct);
        if (!result.IsSuccess)
        {
            return Ok(ApiResponse<object?>.Fail(result.ErrorCode!, result.ErrorMessage!));
        }

        var (stream, fileName, contentType) = result.Value!;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// 获取签名 URL（时效 30 分钟，LLD §3.8）。
    /// </summary>
    [HttpGet("{id:long}/presigned-url")]
    public async Task<IActionResult> GetPresignedUrl(long id, CancellationToken ct)
    {
        var result = await _fileService.GetPresignedUrlAsync(id, ct);
        return Ok(ApiResponse<PresignedUrlDto>.From(result));
    }

    /// <summary>
    /// 删除文件（软删，LLD §3.8）。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _fileService.DeleteAsync(id, ct);
        return Ok(ApiResponse.From(result));
    }
}