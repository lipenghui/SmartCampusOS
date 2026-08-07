using FileService.Application.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FileService.Infrastructure.Storage;

/// <summary>
/// 图片处理器实现（LLD §3.8：图片压缩与缩略图、报表导出文件打水印）。
/// 基于 SixLabors.ImageSharp。
/// </summary>
public sealed class ImageSharpImageProcessor : IImageProcessor
{
    private readonly ILogger<ImageSharpImageProcessor> _logger;

    public ImageSharpImageProcessor(ILogger<ImageSharpImageProcessor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Stream> CompressAsync(Stream source, int maxWidth = 1920, int quality = 80, CancellationToken ct = default)
    {
        _logger.LogDebug("图片压缩: MaxWidth={MaxWidth}, Quality={Quality}", maxWidth, quality);

        source.Position = 0;
        using var image = await Image.LoadAsync(source, ct);

        // 等比缩放：宽度超过 maxWidth 时缩放
        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(ctx => ctx.Resize(maxWidth, newHeight));
        }

        var result = new MemoryStream();
        var encoder = new JpegEncoder { Quality = quality };
        await image.SaveAsync(result, encoder, ct);
        result.Position = 0;
        return result;
    }

    /// <inheritdoc />
    public async Task<Stream> GenerateThumbnailAsync(Stream source, int width = 200, int height = 200, CancellationToken ct = default)
    {
        _logger.LogDebug("生成缩略图: {Width}x{Height}", width, height);

        source.Position = 0;
        using var image = await Image.LoadAsync(source, ct);

        // 等比缩放，裁剪为正方形
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        var result = new MemoryStream();
        var encoder = new JpegEncoder { Quality = 75 };
        await image.SaveAsync(result, encoder, ct);
        result.Position = 0;
        return result;
    }

    /// <inheritdoc />
    public async Task<Stream> AddWatermarkAsync(Stream source, string watermarkText, CancellationToken ct = default)
    {
        _logger.LogDebug("添加水印: Text={Text}", watermarkText);

        source.Position = 0;
        using var image = await Image.LoadAsync(source, ct);

        // 尝试加载系统字体；若无则使用 ImageSharp 内置字体
        Font font;
        if (SystemFonts.TryGet("Microsoft YaHei", out var family))
        {
            font = family.CreateFont(image.Width / 20f, FontStyle.Regular);
        }
        else
        {
            var defaultCollection = SystemFonts.Collection;
            font = defaultCollection.Families.First()
                .CreateFont(image.Width / 20f, FontStyle.Regular);
        }

        var color = Color.ParseHex("#80FFFFFF"); // 半透明白色

        // 对角线水印：在图片中心绘制旋转文字
        image.Mutate(ctx =>
        {
            ctx.DrawText(watermarkText, font, color,
                new PointF(image.Width / 2f, image.Height / 2f));
        });

        var result = new MemoryStream();
        // 根据原始格式保存
        await image.SaveAsync(result, image.Metadata.DecodedImageFormat ?? SixLabors.ImageSharp.Formats.Png.PngFormat.Instance, ct);
        result.Position = 0;
        return result;
    }
}