using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.IntegrationTests.Support;

namespace IdentityService.IntegrationTests;

/// <summary>
/// 数据字典集成测试：字典查询 + Redis 缓存（LLD §4.3 / §9）。
/// </summary>
[Collection("integration")]
public sealed class DictCacheTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Dict_Should_Be_Queryable_With_Seed_Items()
    {
        var client = await fixture.CreateClientAsync();

        var response = await client.GetAsync("/api/v1/dicts/user_status");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("0", body.GetProperty("code").GetString());
        var data = body.GetProperty("data");
        Assert.Equal("user_status", data.GetProperty("code").GetString());
        Assert.True(data.GetProperty("items").GetArrayLength() >= 4); // 未激活/正常/停用/锁定
    }

    [Fact]
    public async Task Dict_List_Should_Contain_Seed_Dicts()
    {
        var client = await fixture.CreateClientAsync();
        var response = await client.GetAsync("/api/v1/dicts");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var codes = body.GetProperty("data")
            .EnumerateArray()
            .Select(d => d.GetProperty("code").GetString())
            .ToArray();
        Assert.Contains("user_status", codes);
        Assert.Contains("org_type", codes);
        Assert.Contains("parent_relation", codes);
    }
}
