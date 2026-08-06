using IdentityService.Infrastructure.Security;

namespace IdentityService.UnitTests.Security;

public sealed class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_And_Verify_Should_Succeed_For_Correct_Password()
    {
        var hash = _hasher.Hash("Admin@123", out var salt);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.False(string.IsNullOrWhiteSpace(salt));
        Assert.True(_hasher.Verify("Admin@123", hash, salt));
    }

    [Fact]
    public void Verify_Should_Fail_For_Wrong_Password()
    {
        var hash = _hasher.Hash("Admin@123", out var salt);

        Assert.False(_hasher.Verify("WrongPass", hash, salt));
    }

    [Fact]
    public void Same_Password_Should_Produce_Different_Hash_With_Different_Salt()
    {
        var hash1 = _hasher.Hash("Password1", out var salt1);
        var hash2 = _hasher.Hash("Password1", out var salt2);

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.Verify("Password1", hash1, salt1));
        Assert.True(_hasher.Verify("Password1", hash2, salt2));
    }
}
