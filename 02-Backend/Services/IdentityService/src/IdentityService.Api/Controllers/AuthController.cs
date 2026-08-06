using IdentityService.Application.Abstractions.Auth;
using IdentityService.Application.Abstractions.Caching;
using IdentityService.Application.Abstractions.Events;
using IdentityService.Application.Abstractions.Security;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Application.UseCases.Auth;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 认证接口（LLD §3.2 / §6.1）：密码/验证码登录、发送验证码、刷新令牌、登出。
/// 登录/刷新为网关白名单路径（免鉴权，网关 §3.1）。
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>登录：grantType=password（学工号+密码）| captcha（手机号+验证码）。</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        Result<LoginResult> result = string.Equals(request.GrantType, "captcha", StringComparison.OrdinalIgnoreCase)
            ? await authService.LoginByCaptchaAsync(request, ClientIp(), ct)
            : await authService.LoginByPasswordAsync(request, ClientIp(), ct);
        return result.IsSuccess
            ? Ok(ApiResponse<LoginResult>.From(result))
            : BadRequest(ApiResponse<LoginResult>.From(result));
    }

    /// <summary>发送验证码（验证码登录前调用）。</summary>
    [HttpPost("captcha")]
    public async Task<IActionResult> SendCaptcha(SendCaptchaRequest request, CancellationToken ct)
    {
        var result = await authService.SendCaptchaAsync(request, ct);
        return result.IsSuccess ? Ok(ApiResponse.From(result)) : BadRequest(ApiResponse.From(result));
    }

    /// <summary>刷新令牌（轮换：旧令牌撤销、签发新对）。</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request, ct);
        return result.IsSuccess
            ? Ok(ApiResponse<LoginResult>.From(result))
            : BadRequest(ApiResponse<LoginResult>.From(result));
    }

    /// <summary>登出：access token 的 jti 进黑名单，撤销刷新令牌（LLD §8.1）。</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        var result = await authService.LogoutAsync(ExtractJti(Request.Headers.Authorization.ToString()), request.RefreshToken, ct);
        return result.IsSuccess ? Ok(ApiResponse.From(result)) : BadRequest(ApiResponse.From(result));
    }

    private string? ClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// 从 Authorization 头提取 JWT 的 jti（仅用于登出黑名单，服务内不解析 JWT 做鉴权，LLD §2.6.4）。
    /// </summary>
    private static string? ExtractJti(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : authorization;
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            return handler.CanReadToken(token) ? handler.ReadJwtToken(token).Id : null;
        }
        catch
        {
            return null;
        }
    }
}
