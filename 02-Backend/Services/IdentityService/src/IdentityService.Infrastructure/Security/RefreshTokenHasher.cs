using System.Security.Cryptography;
using System.Text;
using IdentityService.Application.Abstractions.Security;

namespace IdentityService.Infrastructure.Security;

/// <summary>
/// 刷新令牌哈希实现：SHA-256 十六进制摘要（LLD §8.1 刷新令牌 7 天可续期；§4.2 存 token_hash 不存明文）。
/// </summary>
public sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
