namespace SmartCampusOS.ApiGateway.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartCampusOS.ApiGateway.Configuration;

/// <summary>
/// JWT 校验器：HS256 签名 + 发行方/受众/有效期校验（LLD §8.1）。
/// 网关层只做签名与时效校验，不签发令牌（签发方为 IdentityService）。
/// </summary>
public sealed class JwtTokenValidator
{
    private readonly TokenValidationParameters _parameters;
    private readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };

    public JwtTokenValidator(IOptions<GatewayOptions> options)
    {
        var jwt = options.Value.Jwt;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT SigningKey 未配置或过短（HS256 要求 ≥ 32 字符）。生产环境请通过环境变量/K8s Secret 注入（LLD §10.2）。");
        }

        _parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(Math.Max(0, jwt.ClockSkewSeconds)),
            // roles 在 JWT 中为 JSON 数组字符串，需按数组展开（多角色，§8.1）
            RoleClaimType = ClaimsConstants.Roles,
            NameClaimType = ClaimsConstants.Subject
        };
    }

    /// <summary>校验令牌，成功返回 ClaimsPrincipal；签名/过期/格式非法返回 null。</summary>
    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            return _handler.ValidateToken(token, _parameters, out _);
        }
        catch (SecurityTokenExpiredException)
        {
            return null; // 过期由调用方区分 AUTH-1002
        }
        catch (SecurityTokenException)
        {
            return null; // 签名/格式/发行方非法 → AUTH-1001
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>是否因过期失败（配合 <see cref="Validate"/> 返回 null 时判断错误码）。</summary>
    public static bool IsExpired(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo != default && jwt.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }
}
