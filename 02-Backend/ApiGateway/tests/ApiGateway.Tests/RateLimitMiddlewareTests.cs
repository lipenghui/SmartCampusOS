namespace SmartCampusOS.ApiGateway.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using SmartCampusOS.ApiGateway.Common;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Middleware;
using SmartCampusOS.ApiGateway.Tests.Support;

public sealed class RateLimitMiddlewareTests
{
    private readonly InMemoryCache _cache = new();
    private readonly GatewayOptions _options;

    public RateLimitMiddlewareTests() => _options = TestGateway.DefaultOptions();

    private Action<WebApplication> BuildPipeline() => app =>
    {
        app.UseMiddleware<JwtAuthenticationMiddleware>();
        app.UseMiddleware<RateLimitMiddleware>();
        app.Run(ctx => ctx.Response.WriteAsync("ok"));
    };

    private async Task<(WebApplication App, HttpClient Client)> CreateContextAsync(
        bool withAuth = false, string userId = "u-1")
    {
        var app = await TestGateway.CreateStartedAppAsync(_options, _cache, BuildPipeline());
        var client = app.GetTestClient();
        if (withAuth)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwt.Create(userId: userId));
        }
        return (app, client);
    }

    [Fact]
    public async Task AuthenticatedUser_ExceedsDefaultRps_Returns429()
    {
        var (app, client) = await CreateContextAsync(withAuth: true);
        using var dispose = app;

        // 默认 10 rps：前 10 次放行
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/scores")).StatusCode);
        }

        // 第 11 次触发限流
        var limited = await client.GetAsync("/api/v1/scores");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        var code = JsonDocument.Parse(await limited.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString();
        Assert.Equal(ApiErrorCodes.RateLimited, code);
        Assert.Equal(TimeSpan.FromSeconds(1), limited.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task DifferentUsers_HaveIndependentCounters()
    {
        var (appA, clientA) = await CreateContextAsync(withAuth: true, userId: "10001");
        using var disposeA = appA;
        var (appB, clientB) = await CreateContextAsync(withAuth: true, userId: "10002");
        using var disposeB = appB;

        // 用户 A 打满配额
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await clientA.GetAsync("/api/v1/users")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await clientA.GetAsync("/api/v1/users")).StatusCode);

        // 用户 B 不受影响
        Assert.Equal(HttpStatusCode.OK, (await clientB.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task OverridePath_SelectionWindow_HasRelaxedLimit()
    {
        var (app, client) = await CreateContextAsync(withAuth: true);
        using var dispose = app;

        // /api/v1/selections 放宽至 100 rps：默认配额（10）之外仍放行
        for (var i = 0; i < 15; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/selections/w-1/select", null)).StatusCode);
        }
    }

    [Fact]
    public async Task ExemptedPath_IsNotRateLimited()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;

        for (var i = 0; i < 30; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }
    }

    [Fact]
    public async Task UnauthenticatedRequest_LimitedByIp()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app; // 无 token，按 IP 计数

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/login")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/v1/auth/login")).StatusCode);
    }

    [Fact]
    public async Task RedisDown_FailOpen_AllowsRequest()
    {
        _cache.FailOnAccess = true;
        var (app, client) = await CreateContextAsync(withAuth: true);
        using var dispose = app;

        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/users")).StatusCode);
        }
    }
}
