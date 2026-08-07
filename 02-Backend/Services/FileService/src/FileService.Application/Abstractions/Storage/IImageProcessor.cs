namespace FileService.Application.Abstractions.Storage;

/// <summary>
/// 图片处理抽象（LLD §3.8：图片压缩与缩略图、报表导出文件打水印）。
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// 压缩图片（报修图 ≤ 9 张自动压缩，LLD §3.8）。
    /// </summary>
    /// <param name="source">原始图片流。</param>
    /// <param name="maxWidth">最大宽度（像素）。</param>
    /// <param name="quality">压缩质量（1-100）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>压缩后的图片流。</returns>
    Task<Stream> CompressAsync(Stream source, int maxWidth = 1920, int quality = 80, CancellationToken ct = default);

    /// <summary>
    /// 生成缩略图。
    /// </summary>
    /// <param name="source">原始图片流。</param>
    /// <param name="width">缩略图宽度。</param>
    /// <param name="height">缩略图高度。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>缩略图流。</returns>
    Task<Stream> GenerateThumbnailAsync(Stream source, int width = 200, int height = 200, CancellationToken ct = default);

    /// <summary>
    /// 添加水印（大屏与报表导出文件打水印，LLD §3.8）。
    /// </summary>
    /// <param name="source">原始文件流。</param>
    /// <param name="watermarkText">水印文字。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>添加水印后的文件流。</returns>
    Task<Stream> AddWatermarkAsync(Stream source, string watermarkText, CancellationToken ct = default);
}