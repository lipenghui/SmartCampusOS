namespace SmartCampusOS.ApiGateway.Tests;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using SmartCampusOS.ApiGateway.Middleware;
using SmartCampusOS.ApiGateway.Tests.Support;

public sealed class RequestLoggingMiddlewareTests
{
    private readonly InMemoryCache _cache = new();

    private async Task<(WebApplication App, HttpClient Client)> CreateContextAsync()
    {
        var app = await TestGateway.CreateStartedAppAsync(TestGateway.DefaultOptions(), _cache, app =>
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.Run(ctx => ctx.Response.WriteAsync("ok"));
        });
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task GeneratesAndPropagatesRequestId()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var requestId = response.Headers.GetValues("X-Request-Id").Single();
        Assert.False(string.IsNullOrEmpty(requestId));
    }

    [Fact]
    public async Task RespectsIncomingRequestId()
    {
        var (app, client) = await CreateContextAsync();
        using var dispose = app;
        client.DefaultRequestHeaders.Add("X-Request-Id", "trace-abc-123");
        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal("trace-abc-123", response.Headers.GetValues("X-Request-Id").Single());
    }
}
