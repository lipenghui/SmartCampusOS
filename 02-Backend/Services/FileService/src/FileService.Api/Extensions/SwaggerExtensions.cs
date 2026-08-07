using Microsoft.OpenApi;

namespace FileService.Api.Extensions;

/// <summary>
/// Swagger/OpenAPI 扩展（LLD §5.1：接口 JSON 契约以各服务 Swagger/OpenAPI 为准）。
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SmartCampusOS FileService API",
                Version = "v1",
                Description = "文件服务：附件上传、下载、签名 URL、防盗链（LLD §3.8）"
            });

            // 添加 JWT Bearer 认证定义
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
}