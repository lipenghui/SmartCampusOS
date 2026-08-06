using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdentityService.Application.Configuration;

/// <summary>
/// IdentityService 配置根节点，对应 appsettings.json 的 "Identity" 节（LLD §2.6.7 强类型配置）。
/// </summary>
public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public JwtOptions Jwt { get; set; } = new();
    public DatabaseOptions Database { get; set; } = new();
    public RedisOptions Redis { get; set; } = new();
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public CaptchaOptions Captcha { get; set; } = new();
    public EncryptionOptions Encryption { get; set; } = new();
    public SeedOptions Seed { get; set; } = new();
}

/// <summary>JWT 签发配置，对齐 LLD §8.1 与 ApiGateway §3.1 契约（Issuer/Audience/SigningKey 必须与网关一致）。</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "SmartCampusOS";
    public string Audience { get; set; } = "SmartCampusOS.Clients";
    /// <summary>HS256 签名密钥（≥32 字符）；生产环境由 K8s Secret 注入（LLD §10.2）。</summary>
    public string SigningKey { get; set; } = string.Empty;
    /// <summary>访问令牌有效期（分钟），默认 120（2 小时，LLD §8.1）。</summary>
    public int AccessTokenMinutes { get; set; } = 120;
    /// <summary>刷新令牌有效期（天），默认 7（LLD §8.1）。</summary>
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>MySQL 连接配置（identity_db，LLD §4.1）。</summary>
public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    /// <summary>启动时是否执行 FreeSql CodeFirst 结构同步（LLD §10.4，开发默认开启）。</summary>
    public bool SyncStructureOnStartup { get; set; } = true;
}

/// <summary>Redis 缓存配置（LLD §9：会话/黑名单/字典）。</summary>
public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
}

/// <summary>RabbitMQ 事件总线配置（LLD §2.4 MassTransit + RabbitMQ）。</summary>
public sealed class RabbitMqOptions
{
    /// <summary>是否启用 RabbitMQ 发布；false 时使用进程内事件发布（开发/单机独立启动）。</summary>
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    /// <summary>事件交换机名，默认 smartcampus.events。</summary>
    public string ExchangeName { get; set; } = "smartcampus.events";
}

/// <summary>验证码登录配置（LLD §3.2 登录接口支持验证码）。</summary>
public sealed class CaptchaOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>开发环境测试验证码开关：开启时任意手机号使用固定验证码。</summary>
    public bool TestCodeEnabled { get; set; }
    public string TestCode { get; set; } = "123456";
    public int Length { get; set; } = 6;
    /// <summary>验证码有效期（秒）。</summary>
    public int TtlSeconds { get; set; } = 300;
}

/// <summary>敏感字段存储加密配置（LLD §8.3：手机号等 AES-256 加密存储）。</summary>
public sealed class EncryptionOptions
{
    /// <summary>加密密钥（任意长度字符串，运行时经 SHA-256 派生为 32 字节 AES 密钥）。</summary>
    public string AesKey { get; set; } = string.Empty;
}

/// <summary>启动种子数据配置（内置管理员账号）。</summary>
public sealed class SeedOptions
{
    public string AdminUserNo { get; set; } = "admin";
    public string AdminPassword { get; set; } = "Admin@123";
    public string AdminRealName { get; set; } = "系统管理员";
}

/// <summary>注册并绑定 "Identity" 配置节。</summary>
public static class IdentityConfigurationExtensions
{
    public static IServiceCollection AddIdentityOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityOptions>()
            .Bind(configuration.GetSection(IdentityOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Jwt.SigningKey)
                           && o.Jwt.SigningKey.Length >= 32, "JWT SigningKey 未配置或过短（HS256 要求 ≥ 32 字符）")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Database.ConnectionString), "MySQL 连接串未配置")
            .ValidateOnStart();
        return services;
    }

    /// <summary>便捷读取：获取已绑定的 IdentityOptions。</summary>
    public static IdentityOptions GetIdentityOptions(this IConfiguration configuration) =>
        configuration.GetSection(IdentityOptions.SectionName).Get<IdentityOptions>()
        ?? throw new InvalidOperationException($"配置节 {IdentityOptions.SectionName} 缺失");
}
