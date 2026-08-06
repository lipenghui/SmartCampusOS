using System.Diagnostics;

namespace IdentityService.FunctionalTests.Support;

/// <summary>
/// 功能测试环境入口:真实 ApiGateway(localhost:5000) + IdentityService(localhost:5111) + MySQL/Redis 容器。
/// 首次使用时自动执行 scripts/start-e2e-env.sh 确保环境就绪(可重复执行、幂等)。
/// </summary>
public static class Env
{
    public const string GatewayBaseUrl = "http://localhost:5000";

    private static readonly HttpClient _client = CreateReadyClient();

    /// <summary>指向 ApiGateway 的 HTTP 客户端(基地址 localhost:5000)。</summary>
    public static HttpClient HttpClient => _client;

    /// <summary>SmartCampusOS 仓库根目录(含 scripts/ 与 Backend/)。</summary>
    public static string RootDirectory { get; } = FindRepoRoot();

    private static HttpClient CreateReadyClient()
    {
        EnsureReady();
        return new HttpClient { BaseAddress = new Uri(GatewayBaseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    public static bool IsReady()
    {
        using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            return probe.GetAsync($"{GatewayBaseUrl}/health").GetAwaiter().GetResult().IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static void EnsureReady()
    {
        if (IsReady())
        {
            return;
        }

        var script = Path.Combine(RootDirectory, "scripts", "start-e2e-env.sh");
        if (!File.Exists(script))
        {
            throw new InvalidOperationException($"找不到环境启动脚本: {script}");
        }

        var psi = new ProcessStartInfo("bash", script)
        {
            WorkingDirectory = RootDirectory,
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动环境脚本进程");

        if (!proc.WaitForExit(TimeSpan.FromMinutes(8)))
        {
            proc.Kill(entireProcessTree: true);
            throw new TimeoutException("功能测试环境启动超时(8 分钟), 请查看 scripts/logs/*.log");
        }

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"环境启动失败(exit {proc.ExitCode}), 请查看 scripts/logs/*.log");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "scripts")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Backend")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("未找到 SmartCampusOS 仓库根目录(含 scripts/ 与 Backend/)");
    }
}
