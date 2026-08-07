using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace FileService.Api.Extensions;

/// <summary>
/// API 管道中间件扩展。
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// 配置 API 管道：异常处理、请求日志、Swagger。
    /// </summary>
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // 开发环境启用 Swagger
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // 全局异常处理中间件
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        return app;
    }

    /// <summary>
    /// 配置 MVC 选项：统一 JSON 契约（camelCase、忽略 null）、模型校验错误统一响应。
    /// </summary>
    public static IMvcBuilder ConfigureMvcOptions(this IMvcBuilder builder)
    {
        builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var message = string.Join("; ",
                    context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return new BadRequestObjectResult(ApiResponse.Fail(
                    ErrorCodes.CommonValidationFailed,
                    string.IsNullOrEmpty(message) ? "参数校验失败" : message));
            };
        });

        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return builder;
    }
}