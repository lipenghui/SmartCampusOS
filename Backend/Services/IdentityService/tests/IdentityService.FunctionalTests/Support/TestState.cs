using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdentityService.FunctionalTests.Support;

/// <summary>统一的 API 响应信封: { code, message, data }。</summary>
public sealed record ApiEnvelope(JsonElement Root, HttpStatusCode Status)
{
    public string Code => Root.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : string.Empty;

    public string Message => Root.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;

    public JsonElement? Data => Root.TryGetProperty("data", out var d) ? d : null;
}

/// <summary>登录响应状态(跨 Step 共享)。</summary>
public sealed record LoginState(ApiEnvelope Envelope)
{
    public JsonElement? Data => Envelope.Data;

    public string AccessToken => ReadDataString("accessToken");

    public string RefreshToken => ReadDataString("refreshToken");

    public string UserNo => ReadUserString("userNo");

    public string DataScope => ReadUserString("dataScope");

    public string[] Roles => User.TryGetProperty("roles", out var r)
        ? r.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
        : [];

    private JsonElement User => Data is { } d && d.TryGetProperty("user", out var u) ? u : default;

    private string ReadDataString(string prop) =>
        Data is { } d && d.TryGetProperty(prop, out var v) ? v.GetString() ?? string.Empty : string.Empty;

    private string ReadUserString(string prop) =>
        User.TryGetProperty(prop, out var v) ? v.GetString() ?? string.Empty : string.Empty;
}

/// <summary>/me 响应状态(跨 Step 共享)。</summary>
public sealed record MeState(ApiEnvelope Envelope)
{
    public JsonElement? Data => Envelope.Data;

    public string UserNo => ReadString("userNo");

    public string DataScope => ReadString("dataScope");

    public string[] Permissions => Data is { } d && d.TryGetProperty("permissions", out var p)
        ? p.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
        : [];

    public string[] Roles => Data is { } d && d.TryGetProperty("roles", out var r)
        ? r.EnumerateArray().Select(x => x.TryGetProperty("code", out var c) ? c.GetString() ?? string.Empty : string.Empty).ToArray()
        : [];

    private string ReadString(string prop) =>
        Data is { } d && d.TryGetProperty(prop, out var v) ? v.GetString() ?? string.Empty : string.Empty;
}

public static class Api
{
    public static async Task<ApiEnvelope> PostAsJsonAsync(string url, object body)
    {
        var response = await Env.HttpClient.PostAsJsonAsync(url, body);
        return await ToEnvelope(response);
    }

    public static async Task<ApiEnvelope> GetAsync(string url, string? bearerToken = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new("Bearer", bearerToken);
        }

        var response = await Env.HttpClient.SendAsync(request);
        return await ToEnvelope(response);
    }

    public static async Task<ApiEnvelope> LogoutAsync(string bearerToken, string? refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new("Bearer", bearerToken);
        request.Content = JsonContent.Create(new { refreshToken });
        var response = await Env.HttpClient.SendAsync(request);
        return await ToEnvelope(response);
    }

    private static async Task<ApiEnvelope> ToEnvelope(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrEmpty(json) ? "{}" : json);
        return new ApiEnvelope(doc.RootElement.Clone(), response.StatusCode);
    }
}
