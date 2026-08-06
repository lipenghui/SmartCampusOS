using IdentityService.FunctionalTests.Support;
using Reqnroll;

namespace IdentityService.FunctionalTests.Steps;

[Binding]
public sealed class DataScopeSteps(ScenarioContext scenario)
{
    private const string MeKey = "me";

    private MeState MeState => (MeState)scenario[MeKey];

    [Then(@"响应中的当前用户数据权限为 ""(.*)""")]
    public void Then响应当前用户数据权限为(string expectedScope)
    {
        Assert.Equal(expectedScope, MeState.DataScope);
    }

    [Then(@"响应中的当前用户权限包含 ""(.*)""")]
    public void Then响应当前用户权限包含(string expectedPermission)
    {
        Assert.Contains(expectedPermission, MeState.Permissions);
    }

    [Then(@"响应中的当前用户权限不包含 ""(.*)""")]
    public void Then响应当前用户权限不包含(string unexpectedPermission)
    {
        Assert.DoesNotContain(unexpectedPermission, MeState.Permissions);
    }
}
