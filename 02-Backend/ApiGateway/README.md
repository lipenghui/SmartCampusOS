# SmartCampusOS ApiGateway

智慧校园管理系统 API 网关，基于 **YARP** 反向代理 + 自定义中间件，对齐 LLD §3.1 / §2.4 / §8 / §10 / §11。

## 职责（LLD §3.1）

- **统一路由**：YARP 将 `/api/v1/*` 按服务前缀转发到 7 个业务集群（identity / edu / dorm / notice / data / push / files），路由清单见 `appsettings.json` 的 `ReverseProxy.Routes`（38 条，对应 LLD §3 各服务接口）。
- **JWT 鉴权**：白名单路径免鉴权；校验 HS256 签名与有效期（`AUTH-1001` / `AUTH-1002`）；查询 Redis 令牌黑名单（登出/改密后失效，LLD §8.1）。
- **上下文透传**：校验通过后注入 `X-User-Id` / `X-User-Roles` / `X-Data-Scope`，下游服务经 `ICurrentUser` 读取（LLD §2.6.4 / §8.2）。
- **限流**：默认 10 req/s/用户（未认证按 IP），选课 / 成绩 / 大屏接口经 `RateLimit.Overrides` 放宽；固定窗口 Redis Lua 原子计数，超限返回 `429 + COMMON-2001`。
- **请求日志**：结构化日志（method / path / status / 耗时 / 用户 / IP / RequestId），全链路可观测（LLD §10.3）。
- **灰度发布**：按 `X-Gray-Tag` 显式引流或路径规则 + 用户稳定 hash 百分比引流，注入 `X-Gray-Version`（LLD §10.4）。
- **转发错误统一处理**：上游不可达返回 `502 + COMMON-2004`，不暴露内部细节。

## 中间件管道（Program.cs，顺序敏感）

```
RequestLogging → JwtAuthentication → RateLimit → GrayRelease → YARP（转发错误处理）
```

## 运行

```bash
# 依赖：Redis（localhost:6379；不可用时按 FailOpen 降级并告警）
dotnet run --project src/ApiGateway          # 默认 http://localhost:8080（Development）

# 测试
dotnet test
```

## 配置

- `Gateway` 节：`Whitelist` / `Jwt` / `Redis` / `RateLimit` / `Blacklist` / `GrayRelease` / `Observability`。
- `ReverseProxy` 节：路由与集群地址。开发环境覆盖为 `localhost:8081~8087`（`appsettings.Development.json`），生产为 K8s 服务名。
- **密钥**：`Gateway__Jwt__SigningKey` 等经环境变量 / K8s Secret 注入（LLD §10.2），仓库内仅为开发占位值。

## 结构

```
src/ApiGateway/
├── Common/            # 错误码与统一响应（LLD §11 / §5.1）
├── Configuration/     # 强类型配置（IOptions）
├── Infrastructure/    # ICache 抽象 + RedisCache（黑名单 / 限流 Lua）
├── Security/          # JwtTokenValidator、Claims 与透传 Header 约定
└── Middleware/        # 鉴权 / 限流 / 日志 / 灰度 / 转发错误处理
tests/ApiGateway.Tests/  # 中间件单元测试（内存缓存，TestServer，21 例）
```

> 网关为无状态服务（无独立数据库），不采用业务服务的四层架构（LLD §2.2 / §3.1）。
