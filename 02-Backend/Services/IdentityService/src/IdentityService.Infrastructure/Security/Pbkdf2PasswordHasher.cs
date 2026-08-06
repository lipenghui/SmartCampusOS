using System.Security.Cryptography;
using System.Text;
using IdentityService.Application.Abstractions.Security;
using Microsoft.Extensions.Options;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Infrastructure.Security;

/// <summary>
/// PBKDF2-SHA256 密码哈希实现（LLD §8.3；.NET 内置 Rfc2898DeriveBytes，无第三方依赖）。
/// 迭代次数 210k（对齐 OWASP 对 SHA-256 的建议量级），16 字节随机盐，32 字节派生密钥。
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password, out string salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        salt = Convert.ToBase64String(saltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string hash, string salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        var saltBytes = Convert.FromBase64String(salt);
        var expected = Convert.FromBase64String(hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password, saltBytes, Iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
