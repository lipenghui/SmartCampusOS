using System.Security.Cryptography;
using System.Text;
using IdentityService.Application.Abstractions.Security;

namespace IdentityService.Infrastructure.Security;

/// <summary>
/// AES-256-GCM 敏感字段加解密实现（LLD §8.3：手机号等存储加密）。
/// 密钥由配置字符串经 SHA-256 派生为 32 字节；密文格式 base64(nonce‖cipher‖tag)。
/// </summary>
public sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmFieldEncryptor(byte[] key) => _key = key;

    public string? Encrypt(string? plain)
    {
        if (string.IsNullOrEmpty(plain))
        {
            return null;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plain);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var combined = new byte[nonce.Length + cipher.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(cipher, 0, combined, nonce.Length, cipher.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + cipher.Length, tag.Length);
        return Convert.ToBase64String(combined);
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return null;
        }

        var combined = Convert.FromBase64String(cipherText);
        if (combined.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("密文长度非法");
        }

        var nonce = combined.AsSpan(0, NonceSize).ToArray();
        var cipher = combined.AsSpan(NonceSize, combined.Length - NonceSize - TagSize).ToArray();
        var tag = combined.AsSpan(combined.Length - TagSize, TagSize).ToArray();
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
