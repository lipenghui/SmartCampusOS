using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityService.Api.Common;
using IdentityService.Api.CurrentUser;
using IdentityService.Api.Extensions;
using IdentityService.Application.Configuration;
using IdentityService.Application.DependencyInjection;
using IdentityService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Users;

var builder = WebApplication.CreateBuilder(args);

// 服务默认（OpenTelemetry / 健康检查 / 服务发现，LLD §10.3）
builder.AddServiceDefaults();

// 强类型配置（LLD §2.6.7）
builder.Services.AddIdentityOptions(builder.Configuration);

// 应用层用例与校验器
builder.Services.AddApplicationServices();

// 基础设施：FreeSql / Redis / 事件发布 / 仓储 / 安全原语
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// 控制器：统一 JSON 契约（camelCase、忽略 null）+ 模型校验错误统一响应（LLD §5.1）
var mvcBuilder = builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
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
mvcBuilder.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    // 枚举序列化为字符串（与网关 X-Data-Scope 等字符串契约一致，前端友好）
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// 当前用户上下文：解析网关透传 X-* 头（LLD §2.6.4 / §8.2）
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Swagger（契约来源，LLD §5.1）
builder.Services.AddApiSwagger();

var app = builder.Build();

app.UseApiPipeline();
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();

public partial class Program;
