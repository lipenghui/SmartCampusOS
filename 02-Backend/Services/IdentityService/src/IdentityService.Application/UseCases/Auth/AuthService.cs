using System.Security.Cryptography;
using IdentityService.Application.Abstractions.Auth;
using IdentityService.Application.Common;
using IdentityService.Application.Abstractions.Caching;
using IdentityService.Application.Abstractions.Persistence;
using IdentityService.Application.Abstractions.Security;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Application.Configuration;
using SmartCampusOS.SharedKernel.Results;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.UseCases.Auth;

/// <summary>
/// 认证用例（LLD §3.2 / §6.1 / §8.1）：密码/验证码登录、发送验证码、刷新令牌、登出（jti 黑名单）。
/// </summary>
public sealed class AuthService
{
    private readonly IRepository<User> _users;
    private readonly IRepository<UserRole> _userRoles;
    private readonly IRepository<Role> _roles;
    private readonly IRepository<RefreshToken> _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly IRefreshTokenHasher _tokenHasher;
    private readonly ICache _cache;
    private readonly ICodeSender _codeSender;
    private readonly CaptchaOptions _captcha;
    private readonly JwtOptions _jwt;

    public AuthService(
        IRepository<User> users,
        IRepository<UserRole> userRoles,
        IRepository<Role> roles,
        IRepository<RefreshToken> refreshTokens,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenFactory jwtTokenFactory,
        IRefreshTokenHasher tokenHasher,
        ICache cache,
        ICodeSender codeSender,
        IdentityOptions options)
    {
        _users = users;
        _userRoles = userRoles;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenFactory = jwtTokenFactory;
        _tokenHasher = tokenHasher;
        _cache = cache;
        _codeSender = codeSender;
        _captcha = options.Captcha;
        _jwt = options.Jwt;
    }

    /// <summary>密码登录（LLD §6.1）：校验学工号+密码，签发 JWT 与刷新令牌。</summary>
    public async Task<Result<LoginResult>> LoginByPasswordAsync(
        LoginRequest request, string? clientIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserNo) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResult>.Fail(ErrorCodes.CommonValidationFailed, "学工号与密码不能为空");
        }

        var user = await _users.FirstOrDefaultAsync(u => u.UserNo == request.UserNo, ct);
        if (user is null || !user.CanAttemptLogin)
        {
            return Result<LoginResult>.Fail(ErrorCodes.AuthInvalidCredentials, "学工号或密码错误");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Result<LoginResult>.Fail(ErrorCodes.AuthInvalidCredentials, "学工号或密码错误");
        }

        return await IssueTokensAsync(user, ct);
    }

    /// <summary>验证码登录：手机号+验证码（开发环境测试验证码兜底）。</summary>
    public async Task<Result<LoginResult>> LoginByCaptchaAsync(
        LoginRequest request, string? clientIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<LoginResult>.Fail(ErrorCodes.CommonValidationFailed, "手机号与验证码不能为空");
        }

        if (!await VerifyCaptchaAsync(request.Mobile, request.Code, ct))
        {
            return Result<LoginResult>.Fail(ErrorCodes.AuthInvalidCredentials, "验证码错误或已过期");
        }

        var mobileHash = HashMobile(request.Mobile);
        var user = await _users.FirstOrDefaultAsync(u => u.MobileHash == mobileHash, ct);
        if (user is null || !user.CanAttemptLogin)
        {
            return Result<LoginResult>.Fail(ErrorCodes.AuthInvalidCredentials, "该手机号未注册或账号不可用");
        }

        return await IssueTokensAsync(user, ct);
    }

    /// <summary>发送验证码：生成随机码存入 Redis（TTL 可配），经渠道发送。</summary>
    public async Task<Result> SendCaptchaAsync(SendCaptchaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Mobile))
        {
            return Result.Fail(ErrorCodes.CommonValidationFailed, "手机号不能为空");
        }

        string code;
        if (_captcha.TestCodeEnabled && !string.IsNullOrWhiteSpace(_captcha.TestCode))
        {
            code = _captcha.TestCode;
        }
        else
        {
            code = GenerateCode(_captcha.Length);
        }

        await _cache.SetStringAsync(
            CaptchaKey(request.Mobile), code,
            TimeSpan.FromSeconds(Math.Max(30, _captcha.TtlSeconds)), ct);
        await _codeSender.SendAsync(request.Mobile, code, ct);
        return Result.Ok();
    }

    /// <summary>刷新令牌（LLD §8.1：7 天可续期；轮换：旧令牌撤销、签发新对）。</summary>
    public async Task<Result<LoginResult>> RefreshTokenAsync(
        RefreshTokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<LoginResult>.Fail(ErrorCodes.CommonValidationFailed, "刷新令牌不能为空");
        }

        var tokenHash = _tokenHasher.Hash(request.RefreshToken);
        var stored = await _refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (stored is null || !stored.IsUsable(DateTimeOffset.UtcNow))
        {
            return Result<LoginResult>.Fail(ErrorCodes.AuthInvalidCredentials, "刷新令牌无效或已过期");
        }

        var user = await _users.GetByIdAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
        {
            return Result<LoginResult>.Fail(ErrorCodes.AuthInvalidCredentials, "账号不可用");
        }

        var result = await _unitOfWork.ExecuteAsync(async () =>
        {
            stored.Revoked = true;
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await _refreshTokens.UpdateAsync(stored, ct);
            return await IssueTokensAsync(user, ct);
        }, ct);

        return result;
    }

    /// <summary>登出：access token 的 jti 进 Redis 黑名单（TTL 对齐令牌有效期），撤销刷新令牌。</summary>
    public async Task<Result> LogoutAsync(string? jti, string? refreshToken, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(jti))
        {
            await _cache.SetStringAsync(
                $"gateway:token:blacklist:{jti}", "1",
                TimeSpan.FromMinutes(Math.Max(1, _jwt.AccessTokenMinutes)), ct);
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = _tokenHasher.Hash(refreshToken);
            var stored = await _refreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
            if (stored is not null && !stored.Revoked)
            {
                stored.Revoked = true;
                stored.RevokedAt = DateTimeOffset.UtcNow;
                await _refreshTokens.UpdateAsync(stored, ct);
            }
        }

        return Result.Ok();
    }

    /// <summary>签发访问令牌 + 刷新令牌（事务内）。</summary>
    private async Task<Result<LoginResult>> IssueTokensAsync(User user, CancellationToken ct)
    {
        var roleCodes = await LoadRoleCodesAsync(user.Id, ct);
        var dataScope = await ComputeDataScopeAsync(user.Id, ct);
        var accessToken = _jwtTokenFactory.CreateAccessToken(user.Id, roleCodes, dataScope);

        var refreshToken = GenerateRefreshToken();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenHasher.Hash(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(Math.Max(1, _jwt.RefreshTokenDays))
        };
        await _refreshTokens.AddAsync(refreshEntity, ct);

        return Result<LoginResult>.Ok(new LoginResult(
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresAt,
            new UserInfo(user.Id, user.UserNo, user.RealName, user.UserType, roleCodes,
                DataScopeStrings.ToScopeString(dataScope))));
    }

    /// <summary>加载用户全部角色编码（多角色，LLD §8.1）。</summary>
    private async Task<IReadOnlyList<string>> LoadRoleCodesAsync(long userId, CancellationToken ct)
    {
        var userRoles = await _userRoles.ListAsync(ur => ur.UserId == userId, ct);
        if (userRoles.Count == 0)
        {
            return [];
        }

        var roleIds = userRoles.Select(ur => ur.RoleId).ToArray();
        var roles = await _roles.ListAsync(r => roleIds.Contains(r.Id), ct);
        return roles.Select(r => r.Code).Distinct().ToArray();
    }

    /// <summary>数据范围取用户全部角色中最严格的档位（LLD §8.2 最小可见范围安全兜底）。</summary>
    private async Task<DataScope> ComputeDataScopeAsync(long userId, CancellationToken ct)
    {
        var userRoles = await _userRoles.ListAsync(ur => ur.UserId == userId, ct);
        if (userRoles.Count == 0)
        {
            return DataScope.Self;
        }

        var roleIds = userRoles.Select(ur => ur.RoleId).ToArray();
        var roles = await _roles.ListAsync(r => roleIds.Contains(r.Id), ct);
        return roles.Count == 0
            ? DataScope.Self
            : (DataScope)roles.Max(r => (int)r.DataScope);
    }

    private async Task<bool> VerifyCaptchaAsync(string mobile, string code, CancellationToken ct)
    {
        var stored = await _cache.GetStringAsync(CaptchaKey(mobile), ct);
        if (stored is null)
        {
            return false;
        }

        await _cache.RemoveAsync(CaptchaKey(mobile), ct); // 一次性使用
        return string.Equals(stored, code, StringComparison.Ordinal);
    }

    private static string CaptchaKey(string mobile) => $"identity:captcha:{mobile}";

    private static string GenerateCode(int length) =>
        RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, length)).ToString($"D{length}");

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashMobile(string mobile) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(mobile)));
}
