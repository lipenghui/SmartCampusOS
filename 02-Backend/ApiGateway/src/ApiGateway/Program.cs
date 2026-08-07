using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Infrastructure;
using SmartCampusOS.ApiGateway.Middleware;
using SmartCampusOS.ApiGateway.Security;

var builder = WebApplication.CreateBuilder(args);

// Kestrel：文件上传最大 100MB（LLD §3.8 FileService 单文件 ≤ 50MB，批量 ≤ 9 张）
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

var gatewaySection = builder.Configuration.GetSection(GatewayOptions.SectionName);
builder.Services.Configure<GatewayOptions>(gatewaySection);

// ---- 基础设施：Redis 缓存（黑名单 + 限流计数，LLD §8.1 / §9） ----
var redisOptions = gatewaySection.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
builder.Services.AddSingleton<ICache>(new RedisCache(
    redisOptions.ConnectionString,
    redisOptions.ConnectTimeoutSeconds,
    redisOptions.OperationTimeoutMilliseconds));

// ---- 安全：JWT 校验（LLD §8.1 HS256） ----
builder.Services.AddSingleton<JwtTokenValidator>();

// ---- 网关：YARP 反向代理（LLD §2.4），路由/集群见 appsettings.json 的 ReverseProxy 节 ----
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ---- 可观测性：OpenTelemetry 指标/追踪（LLD §10.3，默认开启，OTLP 导出） ----
var observability = gatewaySection.GetSection("Observability").Get<ObservabilityOptions>() ?? new ObservabilityOptions();
if (observability.Enabled)
{
    var resourceBuilder = ResourceBuilder.CreateDefault().AddService(observability.ServiceName);
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(observability.ServiceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(observability.OtlpEndpoint)))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(observability.OtlpEndpoint)));
}

var app = builder.Build();

// ---- 中间件管道（顺序敏感：日志 → 鉴权 → 限流 → 灰度 → 转发） ----
// 鉴权在前：白名单路径（登录等）跳过鉴权，后续限流按 IP 计数；业务路径限流按已注入的 X-User-Id 计数。
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<GrayReleaseMiddleware>();

// 健康检查（K8s liveness/readiness probe，白名单路径）
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = observability.ServiceName, time = DateTimeOffset.UtcNow }));

// YARP 反向代理（管道内挂转发错误统一处理）
app.MapReverseProxy(proxyApp =>
{
    proxyApp.UseMiddleware<UpstreamErrorHandlingMiddleware>();
});

app.Run();

/// <summary>供集成测试（WebApplicationFactory）引用 Program。</summary>
public partial class Program;
