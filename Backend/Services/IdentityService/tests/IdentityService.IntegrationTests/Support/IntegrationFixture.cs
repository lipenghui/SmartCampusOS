using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MySql;
using Testcontainers.Redis;

namespace IdentityService.IntegrationTests.Support;

/// <summary>
/// 集成测试容器 fixture（LLD §12）：Testcontainers 启动真实 MySQL + Redis，
/// WebApplicationFactory 启动 IdentityService 并覆盖连接串指向容器。
/// </summary>
public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mySql = new MySqlBuilder("mysql:8.4")
        .WithDatabase("identity_db")
        .WithUsername("root")
        .WithPassword("root")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_mySql.StartAsync(), _redis.StartAsync());

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Identity:Database:ConnectionString", _mySql.GetConnectionString());
                builder.UseSetting("Identity:Redis:ConnectionString", _redis.GetConnectionString());
                builder.UseSetting("Identity:Database:SyncStructureOnStartup", "true");
            });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_mySql.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    /// <summary>创建带 Bearer 令牌（可选）与模拟网关注入头（可选）的 HttpClient。</summary>
    public async Task<HttpClient> CreateClientAsync(string? accessToken = null, long? userId = null)
    {
        var client = Factory.CreateClient();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (userId is not null)
        {
            // 模拟 ApiGateway 透传（LLD §3.1 / §8.2）：生产环境由网关注入
            client.DefaultRequestHeaders.Add("X-User-Id", userId.Value.ToString());
            client.DefaultRequestHeaders.Add("X-Data-Scope", "ALL");
        }

        return client;
    }

    /// <summary>登录并返回 accessToken。</summary>
    public async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            grantType = "password",
            userNo = "admin",
            password = "Admin@123"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        return body!.Data!.AccessToken;
    }
}

public sealed class LoginResponseEnvelope
{
    public string? Code { get; set; }
    public LoginData? Data { get; set; }
}

public sealed class LoginData
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public LoginUser? User { get; set; }
}

public sealed class LoginUser
{
    public long UserId { get; set; }
    public string? UserNo { get; set; }
    public IReadOnlyList<string>? Roles { get; set; }
    public string? DataScope { get; set; }
}

[CollectionDefinition("integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>;
