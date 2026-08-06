namespace SmartCampusOS.ApiGateway.Tests.Support;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartCampusOS.ApiGateway.Security;

/// <summary>测试用 JWT 生成器（与默认测试配置同一 HS256 密钥）。</summary>
public static class TestJwt
{
    public const string SigningKey = "dev-only-change-me-please-32bytes-min";
    public const string Issuer = "SmartCampusOS";
    public const string Audience = "SmartCampusOS.Clients";

    public static string Create(
        string userId = "10001",
        IEnumerable<string>? roles = null,
        string? dataScope = "self",
        TimeSpan? expiresIn = null,
        string? jti = null,
        string? issuerOverride = null,
        string? audienceOverride = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimsConstants.Subject, userId),
            new(JwtRegisteredClaimNames.Jti, jti ?? Guid.NewGuid().ToString("N"))
        };
        foreach (var role in roles ?? ["teacher"])
        {
            claims.Add(new Claim(ClaimsConstants.Roles, role));
        }
        if (dataScope is not null)
        {
            claims.Add(new Claim(ClaimsConstants.DataScope, dataScope));
        }

        var now = DateTime.UtcNow;
        var notBefore = now.AddMinutes(-1);
        var expires = now.Add(expiresIn ?? TimeSpan.FromHours(2));
        if (notBefore >= expires)
        {
            // 过期场景：确保 notBefore 早于 expires（JWT 构造约束）
            notBefore = expires.AddMinutes(-5);
        }
        var token = new JwtSecurityToken(
            issuer: issuerOverride ?? Issuer,
            audience: audienceOverride ?? Audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
