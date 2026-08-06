using System.Net.Http.Json;
using FreeSql;
using IdentityService.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.IntegrationTests;

/// <summary>
/// 数据库初始化集成测试：FreeSql CodeFirst 建表 + 种子数据（LLD §10.4 / §4.2）。
/// </summary>
[Collection("integration")]
public sealed class DatabaseInitializationTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Tables_Should_Be_Created_By_CodeFirst()
    {
        var fsql = fixture.Factory.Services.GetRequiredService<IFreeSql>();
        foreach (var table in new[] { "sys_user", "sys_role", "sys_dict", "sys_parent_binding" })
        {
            var exists = await fsql.Ado.ExecuteScalarAsync(
                $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{table}'");
            Assert.Equal(1L, Convert.ToInt64(exists));
        }
    }

    [Fact]
    public async Task Seed_Should_Provide_Admin_And_Roles()
    {
        var client = fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            grantType = "password",
            userNo = "admin",
            password = "Admin@123"
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.Equal("0", body!.Code);
        Assert.NotNull(body.Data?.AccessToken);
    }
}
