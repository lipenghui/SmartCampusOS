using FileService.Application.Abstractions.Persistence;
using FileService.Application.Abstractions.Storage;
using FileService.Application.DTOs;
using FileService.Application.UseCases;
using FileService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

namespace FileService.UnitTests.UseCases;

/// <summary>
/// FileAppService 单元测试（LLD §12：领域规则覆盖率 ≥ 80%）。
/// </summary>
public class FileAppServiceTests
{
    private readonly Mock<IRepository<FileObject>> _filesMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorageService> _storageMock;
    private readonly Mock<IImageProcessor> _imageProcessorMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly FileAppService _service;

    public FileAppServiceTests()
    {
        _filesMock = new Mock<IRepository<FileObject>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _storageMock = new Mock<IFileStorageService>();
        _imageProcessorMock = new Mock<IImageProcessor>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(u => u.UserId).Returns(1L);

        _service = new FileAppService(
            _filesMock.Object,
            _unitOfWorkMock.Object,
            _storageMock.Object,
            _imageProcessorMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task UploadAsync_空文件_返回失败()
    {
        // Arrange
        var formFile = new Mock<IFormFile>();
        formFile.Setup(f => f.Length).Returns(0);
        var request = new UploadFileRequest(formFile.Object, "repair");

        // Act
        var result = await _service.UploadAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CommonValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task GetFileAsync_文件不存在_返回失败()
    {
        // Arrange
        _filesMock.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileObject?)null);

        // Act
        var result = await _service.GetFileAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CommonNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetFileAsync_文件已删除_返回失败()
    {
        // Arrange
        var file = new FileObject { Id = 1, IsDeleted = true };
        _filesMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        // Act
        var result = await _service.GetFileAsync(1, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CommonNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetFileAsync_文件存在_返回成功()
    {
        // Arrange
        var file = new FileObject
        {
            Id = 1,
            BizType = "repair",
            FileName = "test.jpg",
            StorageKey = "repair/2026/08/06/abc123.jpg",
            Size = 1024,
            MimeType = "image/jpeg",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _filesMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        // Act
        var result = await _service.GetFileAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test.jpg", result.Value!.FileName);
        Assert.Equal("repair", result.Value!.BizType);
    }

    [Fact]
    public async Task DeleteAsync_文件不存在_返回失败()
    {
        // Arrange
        _filesMock.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileObject?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CommonNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task DeleteAsync_文件存在_软删除并返回成功()
    {
        // Arrange
        var file = new FileObject { Id = 1, StorageKey = "repair/test.jpg" };
        _filesMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _filesMock.Verify(r => r.DeleteAsync(file, It.IsAny<CancellationToken>()), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync("repair/test.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BatchUploadAsync_超过最大数量_返回失败()
    {
        // Arrange
        var files = new List<IFormFile>();
        for (int i = 0; i < 10; i++)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.Length).Returns(100);
            files.Add(mock.Object);
        }
        var request = new BatchUploadRequest(files, "repair");

        // Act
        var result = await _service.BatchUploadAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CommonValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_文件不存在_返回失败()
    {
        // Arrange
        _filesMock.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileObject?)null);

        // Act
        var result = await _service.GetPresignedUrlAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CommonNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_文件存在_返回签名URL()
    {
        // Arrange
        var file = new FileObject { Id = 1, StorageKey = "repair/test.jpg" };
        _filesMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _storageMock.Setup(s => s.GetPresignedUrlAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://minio.example.com/bucket/repair/test.jpg?signature=xxx");

        // Act
        var result = await _service.GetPresignedUrlAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("signature", result.Value!.Url);
    }
}