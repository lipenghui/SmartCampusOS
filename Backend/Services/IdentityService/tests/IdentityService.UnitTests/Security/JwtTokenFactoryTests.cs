using System.IdentityModel.Tokens.Jwt;
using IdentityService.Application.Configuration;
using IdentityService.Infrastructure.Security;
using Microsoft.Extensions.Options;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.UnitTests.Security;

public sealed class JwtTokenFactoryTests
{
    private static JwtTokenFactory NewFactory()
    {
        var options = new IdentityOptions
        {
            Jwt = new JwtOptions
            {
                Issuer = "SmartCampusOS",
                Audience = "SmartCampusOS.Clients",
                SigningKey = "test-signing-key-32bytes-minimum!!",
                AccessTokenMinutes = 120,
                RefreshTokenDays = 7
            }
        };
        return new JwtTokenFactory(Options.Create(options));
    }

    [Fact]
    public void CreateAccessToken_Should_Contain_Required_Claims()
    {
        var factory = NewFactory();
        var result = factory.CreateAccessToken(42, ["admin", "teacher"], DataScope.All);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        Assert.Equal("42", jwt.Subject);
        Assert.Equal("SmartCampusOS", jwt.Issuer);
        Assert.Equal("SmartCampusOS.Clients", jwt.Audiences.First());
        Assert.NotNull(jwt.Id); // jti
        var roles = jwt.Claims.FirstOrDefault(c => c.Type == TokenClaims.Roles)?.Value;
        Assert.Contains("admin", roles);
        Assert.Contains("teacher", roles);
        Assert.Equal("ALL", jwt.Claims.FirstOrDefault(c => c.Type == TokenClaims.DataScope)?.Value);
    }

    [Fact]
    public void DataScope_Mapping_Should_Match_Gateway_Parser()
    {
        var factory = NewFactory();

        Assert.Equal("ALL", ReadScope(factory, DataScope.All));
        Assert.Equal("GRADE", ReadScope(factory, DataScope.Grade));
        Assert.Equal("DEPT", ReadScope(factory, DataScope.DeptClass));
        Assert.Equal("SELF", ReadScope(factory, DataScope.Self));
    }

    private static string? ReadScope(JwtTokenFactory factory, DataScope scope)
    {
        var result = factory.CreateAccessToken(1, [], scope);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        return jwt.Claims.FirstOrDefault(c => c.Type == TokenClaims.DataScope)?.Value;
    }

    [Fact]
    public void Short_SigningKey_Should_Throw()
    {
        var options = new IdentityOptions
        {
            Jwt = new JwtOptions { SigningKey = "short" }
        };

        Assert.Throws<InvalidOperationException>(() => new JwtTokenFactory(Options.Create(options)));
    }
}
