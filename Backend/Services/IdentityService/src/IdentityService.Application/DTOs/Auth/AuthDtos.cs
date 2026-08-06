using IdentityService.Domain.Enums;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.DTOs.Auth;

/// <summary>登录请求（LLD §3.2 POST /api/v1/auth/login：密码或验证码登录）。</summary>
public sealed record LoginRequest
{
    /// <summary>登录方式：password（学工号+密码）| captcha（手机号+验证码）。</summary>
    public string GrantType { get; set; } = "password";

    /// <summary>学工号（password 方式）。</summary>
    public string? UserNo { get; set; }

    /// <summary>密码（password 方式）。</summary>
    public string? Password { get; set; }

    /// <summary>手机号（captcha 方式）。</summary>
    public string? Mobile { get; set; }

    /// <summary>验证码（captcha 方式）。</summary>
    public string? Code { get; set; }
}

/// <summary>发送验证码请求（POST /api/v1/auth/captcha）。</summary>
public sealed record SendCaptchaRequest
{
    public string Mobile { get; set; } = string.Empty;
}

/// <summary>刷新令牌请求（LLD §3.2 POST /api/v1/auth/refresh）。</summary>
public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>登出请求（LLD §3.2 POST /api/v1/auth/logout）。</summary>
public sealed record LogoutRequest
{
    /// <summary>要一并撤销的刷新令牌（可选）。</summary>
    public string? RefreshToken { get; set; }
}

/// <summary>登录/刷新成功结果（JWT + 刷新令牌 + 用户信息，LLD §3.2 / §8.1）。</summary>
public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserInfo User);

/// <summary>当前登录用户信息（多角色，LLD §8.1）。</summary>
public sealed record UserInfo(
    long UserId,
    string UserNo,
    string RealName,
    UserType UserType,
    IReadOnlyList<string> Roles,
    string DataScope);
