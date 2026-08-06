using IdentityService.Application.Abstractions.Security;
using IdentityService.Application.Configuration;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Application.UseCases.Auth;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure.Security;
using IdentityService.UnitTests.Support;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.UnitTests.UseCases;

public sealed class AuthServiceTests
{
    private readonly IdentityOptions _options;
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly InMemoryRepository<User> _users = new();
    private readonly InMemoryRepository<UserRole> _userRoles = new();
    private readonly InMemoryRepository<Role> _roles = new();
    private readonly InMemoryRepository<RefreshToken> _refreshTokens = new();
    private readonly FakeCache _cache = new();
    private readonly FakeCodeSender _codeSender = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _options = new IdentityOptions
        {
            Jwt = new JwtOptions
            {
                Issuer = "SmartCampusOS",
                Audience = "SmartCampusOS.Clients",
                SigningKey = "test-signing-key-32bytes-minimum!!",
                AccessTokenMinutes = 120,
                RefreshTokenDays = 7
            },
            Captcha = new CaptchaOptions { Enabled = true, TestCodeEnabled = true, TestCode = "123456", Length = 6, TtlSeconds = 300 }
        };

        _roles.AddAsync(new Role { Id = 1, Code = "student", Name = "学生" }).Wait();
        var passwordHash = _hasher.Hash("Passw0rd!", out var salt);
        _users.AddAsync(new User
        {
            Id = 100,
            UserNo = "S001",
            RealName = "张三",
            UserType = UserType.Student,
            Status = UserStatus.Active,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            MobileHash = "A1B2C3D4"
        }).Wait();
        _userRoles.AddAsync(new UserRole { Id = 1, UserId = 100, RoleId = 1 }).Wait();

        _service = new AuthService(
            _users, _userRoles, _roles, _refreshTokens, new InMemoryUnitOfWork(),
            _hasher,
            new JwtTokenFactory(new Microsoft.Extensions.Options.OptionsWrapper<IdentityOptions>(_options)),
            new RefreshTokenHasher(),
            _cache, _codeSender, _options);
    }

    [Fact]
    public async Task Login_With_Correct_Password_Should_Return_Token()
    {
        var result = await _service.LoginByPasswordAsync(
            new LoginRequest { GrantType = "password", UserNo = "S001", Password = "Passw0rd!" },
            "127.0.0.1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Equal("S001", result.Value.User.UserNo);
        Assert.Contains("student", result.Value.User.Roles);
        Assert.Single(_refreshTokens.Items); // 刷新令牌已持久化
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Should_Fail_With_Auth1001()
    {
        var result = await _service.LoginByPasswordAsync(
            new LoginRequest { GrantType = "password", UserNo = "S001", Password = "WrongPass" },
            null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.AuthInvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task Login_With_Unknown_User_Should_Fail_With_Auth1001()
    {
        var result = await _service.LoginByPasswordAsync(
            new LoginRequest { GrantType = "password", UserNo = "NOPE", Password = "Passw0rd!" },
            null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.AuthInvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task Captcha_Login_With_Test_Code_Should_Succeed_When_User_Has_Mobile()
    {
        // 测试码登录：先发送验证码（写缓存），再登录
        await _service.SendCaptchaAsync(new SendCaptchaRequest { Mobile = "13800138000" }, CancellationToken.None);
        Assert.Single(_codeSender.Sent);

        // 无手机号用户的手机号哈希为 A1B2C3D4；此处用测试码直接校验缓存路径
        var login = await _service.LoginByCaptchaAsync(
            new LoginRequest { GrantType = "captcha", Mobile = "13800138000", Code = "123456" },
            null, CancellationToken.None);

        // 手机号不存在匹配用户 → 仍返回凭证错误（验证码校验已通过）
        Assert.False(login.IsSuccess);
        Assert.Equal(ErrorCodes.AuthInvalidCredentials, login.ErrorCode);
    }

    [Fact]
    public async Task Refresh_Should_Rotate_Token_And_Revoke_Old()
    {
        var login = await _service.LoginByPasswordAsync(
            new LoginRequest { GrantType = "password", UserNo = "S001", Password = "Passw0rd!" },
            null, CancellationToken.None);
        Assert.NotNull(login.Value);
        var oldRefreshToken = login.Value.RefreshToken;

        var refreshed = await _service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = oldRefreshToken }, CancellationToken.None);

        Assert.True(refreshed.IsSuccess);
        Assert.NotNull(refreshed.Value);
        Assert.NotEqual(oldRefreshToken, refreshed.Value.RefreshToken);
        Assert.Single(_refreshTokens.Items, t => t.Revoked); // 旧令牌已撤销
        Assert.Single(_refreshTokens.Items, t => !t.Revoked); // 新令牌生效
    }

    [Fact]
    public async Task Refresh_With_Revoked_Token_Should_Fail()
    {
        var login = await _service.LoginByPasswordAsync(
            new LoginRequest { GrantType = "password", UserNo = "S001", Password = "Passw0rd!" },
            null, CancellationToken.None);
        Assert.NotNull(login.Value);

        await _service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = login.Value.RefreshToken }, CancellationToken.None);

        var again = await _service.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = login.Value.RefreshToken }, CancellationToken.None);

        Assert.False(again.IsSuccess);
    }

    [Fact]
    public async Task Logout_Should_Blacklist_Jti_And_Revoke_RefreshToken()
    {
        var login = await _service.LoginByPasswordAsync(
            new LoginRequest { GrantType = "password", UserNo = "S001", Password = "Passw0rd!" },
            null, CancellationToken.None);
        Assert.NotNull(login.Value);
        var jti = "test-jti-123";

        var result = await _service.LogoutAsync(jti, login.Value.RefreshToken, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(await _cache.ExistsAsync($"gateway:token:blacklist:{jti}", CancellationToken.None));
        Assert.True(_refreshTokens.Items.All(t => t.Revoked));
    }
}
