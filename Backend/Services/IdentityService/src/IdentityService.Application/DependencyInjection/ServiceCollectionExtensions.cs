using System.Reflection;
using FluentValidation;
using IdentityService.Application.UseCases.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Application.DependencyInjection;

/// <summary>
/// 应用层服务注册（组合根调用，LLD §2.6.4）：用例服务 + FluentValidation 校验器。
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 用例服务
        services.AddScoped<AuthService>();

        // FluentValidation 校验器自动注册
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
