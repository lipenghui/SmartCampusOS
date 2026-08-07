using FileService.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Application.DependencyInjection;

/// <summary>
/// 应用层服务注册（组合根调用，LLD §2.6.4）：用例服务。
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 用例服务
        services.AddScoped<FileAppService>();
        return services;
    }
}