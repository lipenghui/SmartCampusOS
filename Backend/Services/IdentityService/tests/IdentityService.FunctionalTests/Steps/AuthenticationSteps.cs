using System.Net;
using IdentityService.FunctionalTests.Support;
using Reqnroll;

namespace IdentityService.FunctionalTests.Steps;

[Binding]
public sealed class AuthenticationSteps(ScenarioContext scenario)
{
    private const string LoginKey = "login";
    private const string RefreshKey = "refresh";
    private const string LastEnvelopeKey = "last_envelope";

    private LoginState LoginState => (LoginState)scenario[LoginKey];

    [Given("功能测试环境已就绪")]
    public void Given功能测试环境已就绪()
    {
    }

    [Given("数据库已初始化种子数据")]
    public void Given数据库已初始化种子数据()
    {
    }

    [When(@"用户 ""(.*)"" 使用密码 ""(.*)"" 通过密码模式登录")]
    public async Task When用户使用密码登录(string userNo, string password)
    {
        var envelope = await Api.PostAsJsonAsync("/api/v1/auth/login",
            new { grantType = "password", userNo, password });
        scenario[LoginKey] = new LoginState(envelope);
        scenario[LastEnvelopeKey] = envelope;
    }

    [When("发起空登录请求")]
    public async Task When发起空登录请求()
    {
        var envelope = await Api.PostAsJsonAsync("/api/v1/auth/login", new { });
        scenario[LoginKey] = new LoginState(envelope);
        scenario[LastEnvelopeKey] = envelope;
    }

    [Then("登录响应成功")]
    public void Then登录响应成功()
    {
        Assert.Equal("0", LoginState.Envelope.Code);
    }

    [Then("登录响应失败")]
    public void Then登录响应失败()
    {
        Assert.NotEqual("0", LoginState.Envelope.Code);
        Assert.Equal(HttpStatusCode.BadRequest, LoginState.Envelope.Status);
    }

    [Then(@"错误码为 ""(.*)""")]
    public void Then错误码为(string expectedCode)
    {
        Assert.Equal(expectedCode, ((ApiEnvelope)scenario[LastEnvelopeKey]).Code);
    }

    [Then("返回访问令牌")]
    public void Then返回访问令牌()
    {
        Assert.False(string.IsNullOrEmpty(LoginState.AccessToken), "accessToken 不应为空");
    }

    [Then("返回刷新令牌")]
    public void Then返回刷新令牌()
    {
        Assert.False(string.IsNullOrEmpty(LoginState.RefreshToken), "refreshToken 不应为空");
    }

    [Then(@"登录用户学工号为 ""(.*)""")]
    public void Then登录用户学工号为(string expectedUserNo)
    {
        Assert.Equal(expectedUserNo, LoginState.UserNo);
    }

    [Then(@"登录用户角色包含 ""(.*)""")]
    public void Then登录用户角色包含(string expectedRole)
    {
        Assert.Contains(expectedRole, LoginState.Roles);
    }

    [Then(@"登录用户数据权限为 ""(.*)""")]
    public void Then登录用户数据权限为(string expectedScope)
    {
        Assert.Equal(expectedScope, LoginState.DataScope);
    }

    [When("使用当前刷新令牌发起刷新")]
    public async Task When使用当前刷新令牌发起刷新()
    {
        await RefreshAsync(LoginState.RefreshToken);
    }

    [When(@"使用无效的刷新令牌 ""(.*)"" 发起刷新")]
    public async Task When使用无效刷新令牌(string refreshToken)
    {
        await RefreshAsync(refreshToken);
    }

    [Then("刷新响应成功")]
    public void Then刷新响应成功()
    {
        var envelope = (ApiEnvelope)scenario[RefreshKey];
        Assert.Equal("0", envelope.Code);
    }

    [Then("返回新的访问令牌")]
    public void Then返回新的访问令牌()
    {
        var envelope = (ApiEnvelope)scenario[RefreshKey];
        var token = envelope.Data?.TryGetProperty("accessToken", out var t) == true
            ? t.GetString() ?? string.Empty
            : string.Empty;
        Assert.False(string.IsNullOrEmpty(token), "刷新后应返回新的 accessToken");
        Assert.NotEqual(LoginState.AccessToken, token);
    }

    [Then("旧的刷新令牌已失效")]
    public void Then旧的刷新令牌已失效()
    {
        var result = RefreshAsync(LoginState.RefreshToken).GetAwaiter().GetResult();
        Assert.Equal("AUTH-1001", result.Code);
    }

    [Then("刷新响应失败")]
    public void Then刷新响应失败()
    {
        var envelope = (ApiEnvelope)scenario[RefreshKey];
        Assert.NotEqual("0", envelope.Code);
    }

    private async Task<ApiEnvelope> RefreshAsync(string refreshToken)
    {
        var envelope = await Api.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        scenario[RefreshKey] = envelope;
        scenario[LastEnvelopeKey] = envelope;
        return envelope;
    }
}
