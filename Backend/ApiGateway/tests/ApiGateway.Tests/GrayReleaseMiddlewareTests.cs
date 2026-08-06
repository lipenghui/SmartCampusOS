namespace SmartCampusOS.ApiGateway.Tests;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Middleware;
using SmartCampusOS.ApiGateway.Tests.Support;

public sealed class GrayReleaseMiddlewareTests
{
    private readonly InMemoryCache _cache = new();
    private readonly GatewayOptions _options;

    public GrayReleaseMiddlewareTests() => _options = TestGateway.DefaultOptions();

    private Action<WebApplication> BuildPipeline() => app =>
    {
        app.UseMiddleware<GrayReleaseMiddleware>();
        app.Run(ctx =>
        {
            // 仅当灰度中间件实际注入时才回写，避免空值 header 干扰断言
            var version = ctx.Request.Headers[_options.GrayRelease.VersionHeader].ToString();
            if (!string.IsNullOrEmpty(version))
            {
                ctx.Response.Headers["X-Seen-Version"] = version;
            }
            return ctx.Response.WriteAsync("ok");
        });
    };

    private async Task<(WebApplication App, HttpClient Client)> CreateContextAsync()
    {
        var app = await TestGateway.CreateStartedAppAsync(_options, _cache, BuildPipeline());
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task Disabled_DoesNotInjectVersion()
    {
        _options.GrayRelease.Enabled = false;
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        var response = await client.GetAsync("/api/v1/schedules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Seen-Version"));
    }

    [Fact]
    public async Task Percent100_InjectsVersionForMatchingPath()
    {
        _options.GrayRelease.Enabled = true;
        _options.GrayRelease.Rules =
        [
            new GrayReleaseRule { PathPrefix = "/api/v1/schedules", Percent = 100, Version = "v2" }
        ];
        var (app, client) = await CreateContextAsync();
        using var dispose = app;

        var response = await client.GetAsync("/api/v1/schedules");
        Assert.Equal("v2", response.Headers.GetValues("X-Seen-Version").Single());

        // 非规则路径不注入
        var other = await client.GetAsync("/api/v1/users");
        Assert.False(other.Headers.Contains("X-Seen-Version"));
    }

    [Fact]
    public async Task Percent0_DoesNotInject()
    {
        _options.GrayRelease.Enabled = true;
        _options.GrayRelease.Rules =
        [
            new GrayReleaseRule { PathPrefix = "/api/v1/schedules", Percent = 0, Version = "v2" }
        ];
        var (app, client) = await CreateContextAsync();
        using var dispose = app;

        var response = await client.GetAsync("/api/v1/schedules");
        Assert.False(response.Headers.Contains("X-Seen-Version"));
    }

    [Fact]
    public async Task ExplicitGrayTag_TakesPrecedence()
    {
        _options.GrayRelease.Enabled = true;
        _options.GrayRelease.Rules =
        [
            new GrayReleaseRule { PathPrefix = "/api/v1/schedules", Percent = 0, Version = "v2" }
        ];
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Add(_options.GrayRelease.SourceHeader, "canary-01");

        var response = await client.GetAsync("/api/v1/schedules");
        Assert.Equal("canary-01", response.Headers.GetValues("X-Seen-Version").Single());
    }

    [Fact]
    public async Task ExistingVersionHeader_IsRespected()
    {
        _options.GrayRelease.Enabled = true;
        _options.GrayRelease.Rules =
        [
            new GrayReleaseRule { PathPrefix = "/api/v1/schedules", Percent = 100, Version = "v2" }
        ];
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Add(_options.GrayRelease.VersionHeader, "client-pinned");

        var response = await client.GetAsync("/api/v1/schedules");
        Assert.Equal("client-pinned", response.Headers.GetValues("X-Seen-Version").Single());
    }
}
