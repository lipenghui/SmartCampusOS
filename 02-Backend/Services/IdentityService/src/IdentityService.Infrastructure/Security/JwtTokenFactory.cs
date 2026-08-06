using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Application.Abstractions.Security;
using IdentityService.Application.Common;
using IdentityService.Application.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Infrastructure.Security;

/// <summary>
/// JWT 签发实现（LLD §8.1：HS256，有效期 2 小时）：
/// 与 ApiGateway JwtTokenValidator 的校验参数（Issuer/Audience/SigningKey/claim 名）严格一致。
/// </summary>
public sealed class JwtTokenFactory : IJwtTokenFactory
{
    private readonly JwtOptions _jwt;
    private readonly SigningCredentials _credentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenFactory(IOptions<IdentityOptions> options)
    {
        _jwt = options.Value.Jwt;
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey) || _jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT SigningKey 未配置或过短（HS256 要求 ≥ 32 字符）。生产环境请通过 K8s Secret 注入（LLD §10.2）。");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(long userId, IReadOnlyList<string> roles, DataScope dataScope)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(TokenClaims.Subject, userId.ToString()),
            new(TokenClaims.JwtId, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (roles.Count > 0)
        {
            claims.Add(new Claim(TokenClaims.Roles, string.Join(",", roles)));
        }

        claims.Add(new Claim(TokenClaims.DataScope, DataScopeStrings.ToScopeString(dataScope)));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        return new AccessTokenResult(_handler.WriteToken(token), expiresAt);
    }

}
