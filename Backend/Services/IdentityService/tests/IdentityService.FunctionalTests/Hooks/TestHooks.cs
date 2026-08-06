using IdentityService.FunctionalTests.Support;
using Reqnroll;

namespace IdentityService.FunctionalTests.Hooks;

[Binding]
public sealed class TestHooks
{
    /// <summary>整个测试运行前确保功能测试环境(容器 + 服务)就绪。</summary>
    [BeforeTestRun]
    public static void EnsureEnvironmentReady()
    {
        Env.EnsureReady();
    }
}
