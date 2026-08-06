using System.Security.Cryptography;
using System.Text;
using IdentityService.Infrastructure.Security;

namespace IdentityService.UnitTests.Security;

public sealed class AesEncryptorTests
{
    private static AesGcmFieldEncryptor NewEncryptor() =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes("test-aes-key")));

    [Fact]
    public void Encrypt_Decrypt_Should_RoundTrip()
    {
        var encryptor = NewEncryptor();
        var cipher = encryptor.Encrypt("13800138000");

        Assert.NotNull(cipher);
        Assert.NotEqual("13800138000", cipher);
        Assert.Equal("13800138000", encryptor.Decrypt(cipher));
    }

    [Fact]
    public void Same_Plain_Should_Produce_Different_Ciphertext()
    {
        var encryptor = NewEncryptor();
        var cipher1 = encryptor.Encrypt("13800138000");
        var cipher2 = encryptor.Encrypt("13800138000");

        Assert.NotEqual(cipher1, cipher2);
        Assert.Equal(encryptor.Decrypt(cipher1), encryptor.Decrypt(cipher2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encrypt_NullOrEmpty_Should_Return_Null(string? value)
    {
        var encryptor = NewEncryptor();
        Assert.Null(encryptor.Encrypt(value));
        Assert.Null(encryptor.Decrypt(value));
    }

    [Fact]
    public void Decrypt_Tampered_Should_Throw()
    {
        var encryptor = NewEncryptor();
        var cipher = encryptor.Encrypt("13800138000")!;
        var bytes = Convert.FromBase64String(cipher);
        bytes[0] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => encryptor.Decrypt(Convert.ToBase64String(bytes)));
    }
}
