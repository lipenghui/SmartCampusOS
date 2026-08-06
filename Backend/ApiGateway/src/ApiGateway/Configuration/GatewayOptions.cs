namespace SmartCampusOS.ApiGateway.Configuration;

/// <summary>网关配置根节点，对应 appsettings.json 的 "Gateway" 节。</summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>免鉴权白名单路径前缀（登录、验证码、公开接口、第三方回调；LLD §3.1）。</summary>
    public List<string> Whitelist { get; set; } = [];
    public JwtOptions Jwt { get; set; } = new();
    public RedisOptions Redis { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
    public BlacklistOptions Blacklist { get; set; } = new();
    public GrayReleaseOptions GrayRelease { get; set; } = new();
    public ObservabilityOptions Observability { get; set; } = new();
}

/// <summary>JWT 校验配置（LLD §8.1：HS256，有效期 2 小时由 IdentityService 签发）。</summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "SmartCampusOS";
    public string Audience { get; set; } = "SmartCampusOS.Clients";
    /// <summary>HS256 签名密钥，生产环境由 K8s Secret 注入（LLD §10.2），禁止明文入库。</summary>
    public string SigningKey { get; set; } = string.Empty;
    /// <summary>时钟偏移，默认 30s。</summary>
    public int ClockSkewSeconds { get; set; } = 30;
}

/// <summary>Redis 配置（黑名单 / 限流计数，LLD §8.1 / §9）。</summary>
public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
    /// <summary>Redis 不可用时的行为：true = 降级放行（可用性优先，记告警日志）；false = 拒绝请求（安全优先）。</summary>
    public bool FailOpen { get; set; } = true;
    public int ConnectTimeoutSeconds { get; set; } = 3;
    public int OperationTimeoutMilliseconds { get; set; } = 500;
}

/// <summary>限流配置（LLD §3.1：默认 10 req/s/用户，选课/成绩发布接口按配置放宽）。</summary>
public sealed class RateLimitOptions
{
    /// <summary>默认每秒请求上限（按用户或 IP）。</summary>
    public int DefaultRps { get; set; } = 10;
    /// <summary>限流窗口长度（秒），固定窗口。</summary>
    public int WindowSeconds { get; set; } = 1;
    /// <summary>限流豁免路径前缀（如健康检查、SignalR 长连接协商），精确前缀匹配。</summary>
    public List<string> PathExemptions { get; set; } = [];
    /// <summary>路径级限流覆盖：如选课 / 成绩发布高峰放宽。</summary>
    public List<RateLimitOverride> Overrides { get; set; } = [];
    /// <summary>Redis 不可用时是否放行（true=可用性优先）。</summary>
    public bool FailOpen { get; set; } = true;
}

/// <summary>路径前缀 → 放宽后的每秒上限。</summary>
public sealed class RateLimitOverride
{
    public string PathPrefix { get; set; } = string.Empty;
    public int Rps { get; set; }
}

/// <summary>令牌黑名单配置（LLD §8.1：登出/改密后令牌进 Redis 黑名单）。</summary>
public sealed class BlacklistOptions
{
    /// <summary>Redis key 前缀。</summary>
    public string KeyPrefix { get; set; } = "gateway:token:blacklist";
    /// <summary>Redis 不可用时：true = 放行（签名/过期已校验通过，仅失去即时吊销能力）；false = 拒绝。</summary>
    public bool FailOpen { get; set; } = true;
}

/// <summary>灰度发布配置（LLD §10.4：网关按 Header/用户比例引流）。</summary>
public sealed class GrayReleaseOptions
{
    public bool Enabled { get; set; }
    /// <summary>灰度版本 Header 名，注入到下游服务。</summary>
    public string VersionHeader { get; set; } = "X-Gray-Version";
    /// <summary>客户端/CI 可显式指定的灰度标签 Header（存在则优先采用）。</summary>
    public string SourceHeader { get; set; } = "X-Gray-Tag";
    public List<GrayReleaseRule> Rules { get; set; } = [];
}

/// <summary>灰度规则：路径前缀 + 引流百分比 + 目标版本。</summary>
public sealed class GrayReleaseRule
{
    public string PathPrefix { get; set; } = string.Empty;
    /// <summary>引流百分比 0-100。</summary>
    public int Percent { get; set; }
    /// <summary>注入的版本值（下游服务据此选择版本/分支）。</summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>可观测性配置（LLD §10.3：OpenTelemetry 全链路追踪默认开启）。</summary>
public sealed class ObservabilityOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>OTLP 导出端点，如 http://otel-collector:4317。</summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string ServiceName { get; set; } = "ApiGateway";
}
