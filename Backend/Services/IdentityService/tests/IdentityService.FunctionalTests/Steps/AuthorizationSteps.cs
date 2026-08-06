using System.Net;
using IdentityService.FunctionalTests.Support;
using Reqnroll;

namespace IdentityService.FunctionalTests.Steps;

[Binding]
public sealed class AuthorizationSteps(ScenarioContext scenario)
{
    private const string LoginKey = "login";
    private const string MeKey = "me";
    private const string LastStatusKey = "last_status";

    private LoginState LoginState => (LoginState)scenario[LoginKey];

    [When(@"未携带令牌访问 ""(.*)""")]
    public async Task When未携带令牌访问(string path)
    {
        var envelope = await Api.GetAsync(path);
        scenario[LastStatusKey] = envelope.Status;
    }

    [When(@"携带令牌 ""(.*)"" 访问 ""(.*)""")]
    public async Task When携带令牌访问(string token, string path)
    {
        var envelope = await Api.GetAsync(path, bearerToken: token);
        scenario[LastStatusKey] = envelope.Status;
    }

    [When(@"携带登录访问令牌访问 ""(.*)""")]
    public async Task When携带登录访问令牌访问(string path)
    {
        var envelope = await Api.GetAsync(path, bearerToken: LoginState.AccessToken);
        scenario[LastStatusKey] = envelope.Status;
        scenario[MeKey] = new MeState(envelope);
    }

    [When("携带登录访问令牌调用登出接口")]
    public async Task When携带登录访问令牌调用登出接口()
    {
        var envelope = await Api.LogoutAsync(LoginState.AccessToken, LoginState.RefreshToken);
        scenario["logout"] = envelope;
        scenario["logged_out_token"] = LoginState.AccessToken;
    }

    [When(@"携带已登出的访问令牌访问 ""(.*)""")]
    public async Task When携带已登出访问令牌访问(string path)
    {
        var token = (string)scenario["logged_out_token"];
        var envelope = await Api.GetAsync(path, bearerToken: token);
        scenario[LastStatusKey] = envelope.Status;
    }

    [Then("登出响应成功")]
    public void Then登出响应成功()
    {
        var envelope = (ApiEnvelope)scenario["logout"];
        Assert.Equal("0", envelope.Code);
    }

    [Then(@"响应状态码为 (\d+)")]
    public void Then响应状态码为(int expectedStatus)
    {
        var actual = scenario.ContainsKey(LastStatusKey)
            ? (HttpStatusCode)scenario[LastStatusKey]
            : (HttpStatusCode?)null;
        Assert.Equal((HttpStatusCode)expectedStatus, actual);
    }

    [Then(@"响应中的当前用户学工号为 ""(.*)""")]
    public void Then响应当前用户学工号为(string expectedUserNo)
    {
        Assert.Equal(expectedUserNo, ((MeState)scenario[MeKey]).UserNo);
    }

    [Then(@"响应中的当前用户角色包含 ""(.*)""")]
    public void Then响应当前用户角色包含(string expectedRole)
    {
        Assert.Contains(expectedRole, ((MeState)scenario[MeKey]).Roles);
    }
}
