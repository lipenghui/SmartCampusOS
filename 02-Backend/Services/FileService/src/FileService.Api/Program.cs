using FileService.Api.CurrentUser;
using FileService.Api.Extensions;
using FileService.Application.DependencyInjection;
using FileService.Infrastructure.DependencyInjection;
using SmartCampusOS.SharedKernel.Users;

var builder = WebApplication.CreateBuilder(args);

// 服务默认（OpenTelemetry / 健康检查 / 服务发现，LLD §10.3）
builder.AddServiceDefaults();

// 应用层用例
builder.Services.AddApplicationServices();

// 基础设施：FreeSql / MinIO / 仓储 / 图片处理
builder.Services.AddFileInfrastructure(builder.Configuration);

// 控制器：统一 JSON 契约（camelCase、忽略 null）+ 模型校验错误统一响应（LLD §5.1）
builder.Services.AddControllers().ConfigureMvcOptions();

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