using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.IntegrationTests.Support;

namespace IdentityService.IntegrationTests;

/// <summary>
/// 认证链路集成测试：登录 → 刷新 → 登出（LLD §6.1 / §8.1，真实 MySQL/Redis）。
/// </summary>
[Collection("integration")]
public sealed class AuthFlowTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Login_Refresh_Logout_Should_Work_EndToEnd()
    {
        var client = await fixture.CreateClientAsync();

        // 1. 登录（密码）
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            grantType = "password",
            userNo = "admin",
            password = "Admin@123"
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.Equal("0", login!.Code);
        Assert.False(string.IsNullOrWhiteSpace(login.Data?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.Data?.RefreshToken));
        Assert.Contains("admin", login.Data?.User?.Roles ?? []);

        // 2. 刷新令牌（轮换）
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = login.Data.RefreshToken
        });
        if (!refreshResponse.IsSuccessStatusCode)
        {
            Assert.Fail($"refresh failed: {(int)refreshResponse.StatusCode} {await refreshResponse.Content.ReadAsStringAsync()}");
        }
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.Equal("0", refreshed!.Code);
        Assert.NotEqual(login.Data.AccessToken, refreshed.Data?.AccessToken);
        Assert.NotEqual(login.Data.RefreshToken, refreshed.Data?.RefreshToken);

        // 3. 旧刷新令牌不可再用
        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = login.Data.RefreshToken
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, reuseResponse.StatusCode);

        // 4. 用新令牌访问 /api/v1/me（模拟网关注入 X-User-Id）
        var meClient = await fixture.CreateClientAsync(refreshed.Data!.AccessToken, userId: 1);
        var meResponse = await meClient.GetAsync("/api/v1/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("0", me.GetProperty("code").GetString());
        Assert.Equal("admin", me.GetProperty("data").GetProperty("userNo").GetString());
        Assert.Equal("ALL", me.GetProperty("data").GetProperty("dataScope").GetString());

        // 5. 登出（撤销新刷新令牌 + jti 黑名单）
        var logoutResponse = await meClient.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken = refreshed.Data.RefreshToken
        });
        logoutResponse.EnsureSuccessStatusCode();
        var logout = await logoutResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("0", logout.GetProperty("code").GetString());

        // 6. 登出后的刷新令牌不可再用
        var afterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = refreshed.Data.RefreshToken
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Should_Return_Auth1001()
    {
        var client = await fixture.CreateClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            grantType = "password",
            userNo = "admin",
            password = "WrongPassword"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AUTH-1001", body.GetProperty("code").GetString());
    }
}
