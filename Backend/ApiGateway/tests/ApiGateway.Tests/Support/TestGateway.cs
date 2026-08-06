namespace SmartCampusOS.ApiGateway.Tests.Support;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Infrastructure;
using SmartCampusOS.ApiGateway.Security;

/// <summary>TestServer 构造工厂：WebApplication + UseTestServer（.NET 10 推荐方式），内存缓存 + 默认测试配置。</summary>
public static class TestGateway
{
    public static GatewayOptions DefaultOptions() => new()
    {
        Whitelist = ["/health", "/api/v1/auth/login", "/api/v1/auth/refresh"],
        Jwt = new JwtOptions
        {
            Issuer = TestJwt.Issuer,
            Audience = TestJwt.Audience,
            SigningKey = TestJwt.SigningKey,
            ClockSkewSeconds = 30
        },
        Redis = new RedisOptions { FailOpen = true },
        Blacklist = new BlacklistOptions { KeyPrefix = "gateway:token:blacklist", FailOpen = true },
        RateLimit = new RateLimitOptions
        {
            DefaultRps = 10,
            WindowSeconds = 1,
            PathExemptions = ["/health", "/hubs/"],
            Overrides =
            [
                new RateLimitOverride { PathPrefix = "/api/v1/selections", Rps = 100 }
            ]
        },
        GrayRelease = new GrayReleaseOptions
        {
            Enabled = false,
            VersionHeader = "X-Gray-Version",
            SourceHeader = "X-Gray-Tag"
        }
    };

    /// <summary>构建并启动 TestServer 应用；调用方 using 持有（返回已启动实例），用 GetTestClient() 获取客户端。</summary>
    public static async Task<WebApplication> CreateStartedAppAsync(
        GatewayOptions options, ICache cache, Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddSingleton(cache);
        builder.Services.AddSingleton(Options.Create(options));
        builder.Services.AddSingleton<JwtTokenValidator>();
        var app = builder.Build();
        configure(app);
        await app.StartAsync();
        return app;
    }
}
