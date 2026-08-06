using IdentityService.Api.Middleware;
using Microsoft.OpenApi;

namespace IdentityService.Api.Extensions;

/// <summary>Api 层服务注册与管道配置扩展。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>注册 Swagger（Swashbuckle，LLD §2.4 / §5.1 契约来源）。</summary>
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SmartCampusOS IdentityService API",
                Version = "v1",
                Description = "认证授权服务：账号、组织架构、RBAC、数据字典、审计日志、家长绑定（LLD §3.2）"
            });

            // 网关校验 JWT 后透传 X-* 头，Swagger 提供输入框便于本地调试
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "粘贴 JWT（经网关签发）"
            };
            options.AddSecurityDefinition("Bearer", bearerScheme);
        });
        return services;
    }

    /// <summary>注册 API 管道中间件：请求日志 → 异常处理 → 控制器。</summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
