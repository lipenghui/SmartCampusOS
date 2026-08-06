namespace SmartCampusOS.ApiGateway.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using SmartCampusOS.ApiGateway.Common;
using SmartCampusOS.ApiGateway.Configuration;
using SmartCampusOS.ApiGateway.Middleware;
using SmartCampusOS.ApiGateway.Security;
using SmartCampusOS.ApiGateway.Tests.Support;

public sealed class JwtAuthenticationMiddlewareTests
{
    private readonly InMemoryCache _cache = new();
    private readonly GatewayOptions _options;

    public JwtAuthenticationMiddlewareTests() => _options = TestGateway.DefaultOptions();

    private Action<WebApplication> BuildPipeline() => app =>
    {
        app.UseMiddleware<JwtAuthenticationMiddleware>();
        app.Run(async ctx =>
        {
            // 把注入的下游透传头回写，便于断言
            ctx.Response.Headers[ClaimsConstants.HeaderUserId] = ctx.Request.Headers[ClaimsConstants.HeaderUserId].ToString();
            ctx.Response.Headers[ClaimsConstants.HeaderUserRoles] = ctx.Request.Headers[ClaimsConstants.HeaderUserRoles].ToString();
            ctx.Response.Headers[ClaimsConstants.HeaderDataScope] = ctx.Request.Headers[ClaimsConstants.HeaderDataScope].ToString();
            await ctx.Response.WriteAsync("ok");
        });
    };

    private async Task<(WebApplication App, HttpClient Client)> CreateContextAsync()
    {
        var app = await TestGateway.CreateStartedAppAsync(_options, _cache, BuildPipeline());
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task WhitelistedPath_IsAllowed_WithoutToken()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        var response = await client.GetAsync("/api/v1/auth/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MissingToken_Returns401_InvalidCredentials()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var code = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString();
        Assert.Equal(ApiErrorCodes.InvalidCredentials, code);
    }

    [Fact]
    public async Task InvalidToken_Returns401_InvalidCredentials()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var code = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString();
        Assert.Equal(ApiErrorCodes.InvalidCredentials, code);
    }

    [Fact]
    public async Task ExpiredToken_Returns401_TokenExpired()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Create(expiresIn: TimeSpan.FromMinutes(-30)));
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var code = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString();
        Assert.Equal(ApiErrorCodes.TokenExpired, code);
    }

    [Fact]
    public async Task ValidToken_IsAllowed_And_InjectsUserContextHeaders()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestJwt.Create(userId: "10001", roles: ["teacher", "class_teacher"], dataScope: "class"));
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("10001", response.Headers.GetValues(ClaimsConstants.HeaderUserId).Single());
        Assert.Equal("teacher,class_teacher", response.Headers.GetValues(ClaimsConstants.HeaderUserRoles).Single());
        Assert.Equal("class", response.Headers.GetValues(ClaimsConstants.HeaderDataScope).Single());
    }

    [Fact]
    public async Task BlacklistedJti_Returns401()
    {
        var jti = Guid.NewGuid().ToString("N");
        _cache.SetKey($"{_options.Blacklist.KeyPrefix}:{jti}");

        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create(jti: jti));
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var code = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("code").GetString();
        Assert.Equal(ApiErrorCodes.InvalidCredentials, code);
    }

    [Fact]
    public async Task RedisDown_FailOpen_AllowsRequest()
    {
        _cache.FailOnAccess = true;
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create());
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RedisDown_StrictMode_RejectsRequest()
    {
        _options.Blacklist.FailOpen = false;
        _cache.FailOnAccess = true;
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create());
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
