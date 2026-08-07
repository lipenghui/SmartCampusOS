# IdentityService — 认证授权服务

SmartCampusOS 的**认证授权微服务**(HTTP `5111`),承担账号认证、组织架构、RBAC 权限、数据范围(DataScope)、数据字典、审计日志与家长-子女绑定等基础平台能力(对应 PRD Common-01~04、BR-04,LLD §3.2)。

> **实现状态:✅ 已完整落地**(当前 7 个微服务中唯一完成 Api/Application/Domain/Infrastructure 四层 + 全套测试的服务)。

## 职责与功能

| 功能域 | 说明 | 对应需求 |
| --- | --- | --- |
| 账号认证 | 密码登录、验证码登录、JWT 签发、刷新令牌轮换、登出(令牌进 Redis 黑名单) | Common-01 |
| 组织与人员 | 学校-年级-班级-部门组织树、用户 CRUD、Excel 批量导入、账号激活 | Common-02 |
| RBAC | 角色-权限配置、权限点、数据范围(DataScope:ALL/GRADE/DEPT/SELF) | Common-03 |
| 审计日志 | 关键操作全量留痕,可按人员/时间/类型检索 | Common-04 |
| 数据字典 | 字典与字典项维护,其他服务启动时缓存副本 | 通用 |
| 家长绑定 | 家长-子女绑定申请 → 班主任审核(通过/驳回),控制家长端数据访问 | BR-04 |

## 目录结构(Clean Architecture 四层)

```
IdentityService/
├── src/
│   ├── IdentityService.Api/            # ① 表现层:Controllers、中间件、Swagger、DI 组合根
│   ├── IdentityService.Application/    # ② 应用层:UseCases、DTO、接口抽象、FluentValidation、Mapster
│   ├── IdentityService.Domain/         # ③ 领域层:实体、枚举、领域事件(零第三方依赖)
│   └── IdentityService.Infrastructure/ # ④ 基础设施:FreeSql 仓储、Redis、MassTransit、AES-GCM、NPOI
├── tests/
│   ├── IdentityService.UnitTests/        # 单测(31):领域规则、密码哈希、JWT、AES、用例
│   ├── IdentityService.IntegrationTests/ # 集成(6):Testcontainers 真实 MySQL/Redis
│   └── IdentityService.FunctionalTests/  # BDD(18 场景):Reqnroll,经真实网关走完整契约
└── IdentityService.slnx
```

依赖方向:Api → Application → Domain;Api → Infrastructure(仅组合根);Domain 零外部依赖。设计细节见 LLD §2.6。

## 快速开始

### 方式 1:经 Aspire(推荐,一键起全部)

```bash
dotnet run --project 02-Backend/Host/src/SmartCampusOS.AppHost
```

本服务由 Aspire 编排自动拉起,端口 5111,依赖的 MySQL(`identity_db`)/Redis 容器自动创建,启动时自动建表 + 幂等播种。

### 方式 2:独立运行(仅本服务,需自备 MySQL/Redis)

```bash
cd 02-Backend/Services/IdentityService/src/IdentityService.Api
dotnet run --urls http://localhost:5111
```

需先在 `appsettings.json`(或环境变量 `Identity__*`)配置好 `Database.ConnectionString` 与 `Redis.ConnectionString`。

### 方式 3:功能测试环境(BDD/E2E 专用,自动拉起最小依赖)

```bash
bash 04-scripts/start-e2e-env.sh
```

### 默认账号(启动时幂等播种,见 `DbSeeder`)

| 账号 | 密码 | 说明 |
| --- | --- | --- |
| `admin` | `Admin@123` | 系统管理员(数据权限 ALL,配置于 `Seed` 节) |
| `T1001` / `S2026001` 等 | `Test@123` | 22 个演示账号(教师/学生/宿管/后勤/教务/校领导/家长,写死在 `DbSeeder.UserDefs`) |

## 配置说明(`appsettings.json` → `Identity:` 节)

| 节 | 键 | 说明 |
| --- | --- | --- |
| `Jwt` | `SigningKey` / `AccessTokenMinutes`(120) / `RefreshTokenDays`(7) | JWT HS256 签发参数;**生产必须替换 SigningKey** |
| `Database` | `ConnectionString` / `SyncStructureOnStartup` | FreeSql 连接串;启动时 CodeFirst 建表 |
| `Redis` | `ConnectionString` | 缓存 / 黑名单 / 验证码 |
| `RabbitMq` | `Enabled`(默认 false) | MassTransit 领域事件总线;`false` 时降级为进程内发布 |
| `Captcha` | `Enabled` / `TestCodeEnabled` / `TestCode`(123456) | 验证码;测试码可一键登录调试 |
| `Encryption` | `AesKey` | 手机号等敏感字段 AES-GCM 加密密钥;**开发占位符,生产必须替换** |
| `Seed` | `AdminUserNo` / `AdminPassword` / `AdminRealName` | 系统管理员播种配置 |

环境变量覆盖示例:`Identity__Jwt__SigningKey=...`、`Identity__Seed__AdminPassword=...`。

## 接口清单(Swagger:http://localhost:5111/swagger)

| 控制器 | 端点(前缀 `/api/v1`) | 说明 |
| --- | --- | --- |
| AuthController | `POST /auth/login`、`POST /auth/captcha`、`POST /auth/refresh`、`POST /auth/logout` | 登录/验证码/刷新/登出 |
| UsersController | `GET/POST/PUT/DELETE /users(/id)`、`POST /users/{id}/activate`、`POST /users/import` | 用户管理 + 批量导入 |
| OrgsController | `GET/POST/PUT/DELETE /orgs(/id)` | 组织树维护 |
| RolesController | `GET/POST/PUT/DELETE /roles(/id)`、`GET /roles/permissions` | 角色与权限点 |
| MeController | `GET /me` | 当前用户信息(含角色/数据范围) |
| AuditLogsController | `GET /audit-logs` | 审计日志检索 |
| DictsController | `GET/POST/PUT/DELETE /dicts(/id)`、`POST /dicts/{code}/items` 等 | 数据字典与字典项 |
| ParentsController | `POST/GET /parents/bind`、`PUT /parents/bind/{id}/approve\|reject` | 家长绑定申请与审核 |

完整契约以 Swagger 为准(LLD v1.1 约定:接口 JSON 契约以各服务 Swagger/OpenAPI 为准)。

## 安全设计要点

1. **信任链**:本服务**不校验 JWT**——JWT 由 ApiGateway 校验后以 `X-User-Id` / `X-User-Roles` / `X-Data-Scope` 请求头透传,服务经 `HttpCurrentUser`(实现 `ICurrentUser`)解析。**服务端口(5111)不应对外暴露**。
2. **密码**:PBKDF2 加盐哈希(`Pbkdf2PasswordHasher`),库中不存明文。
3. **敏感字段**:手机号 AES-GCM 加密存储(`mobile_encrypted`)+ SHA-256 哈希(`mobile_hash`)供检索,展示脱敏(`MaskUtil`)。
4. **刷新令牌**:库中仅存哈希,轮换 + 吊销;登出后 access token 进 Redis 黑名单(由网关执行)。
5. **数据权限**:角色绑定 DataScope,经网关注入 `X-Data-Scope`,本服务仓储层统一过滤。
6. **审计**:`AuditLogger` 记录登录/审批/权限变更等关键操作到 `sys_audit_log`。

## 测试

```bash
# 单元测试(31,无需外部依赖)
dotnet test tests/IdentityService.UnitTests

# 集成测试(6,需 Docker:Testcontainers 起真实 MySQL/Redis)
dotnet test tests/IdentityService.IntegrationTests

# BDD 功能测试(18 场景,经真实网关;环境未就绪会自动执行 04-scripts/start-e2e-env.sh)
cd 02-Backend/Services/IdentityService && dotnet test tests/IdentityService.FunctionalTests
```

BDD 覆盖:密码登录成功/失败/参数校验、刷新令牌轮换与无效拒绝、未带/无效令牌 401、登出后令牌拉黑、白名单免鉴权、admin=ALL / student=SELF / teacher=DEPT 数据权限下发。详见 `01-Docs/SmartCampusOS-测试报告-20260806.md`。

## 常见问题

| 现象 | 排查 |
| --- | --- |
| 登录报 401 | 确认经网关(5000)访问而非直连 5111 的受保护接口;令牌是否过期/被拉黑 |
| 验证码登录 | `Captcha.TestCodeEnabled=true` 时可用测试码 `123456`;生产环境关闭 |
| 接口返回 `COMMON-2001` | 网关限流(默认 10 rps),测试环境已放宽 |
| RabbitMQ 报错 | `RabbitMq.Enabled` 默认 false,使用进程内事件发布;开启需自备 RabbitMQ |
| 数据库不建表 | 确认 `Database.SyncStructureOnStartup=true` 且连接串正确;库由 Aspire 自动创建 |

## 关联文档

- 设计:LLD §3.2(接口清单)、§8(权限与安全)、§4.2 identity_db(12 表,字段以代码实体为准)
- 数据表:数据库设计文档 §3(`sys_*` 12 表,已与 `Domain/Entities` 核对一致)
- 决策背景:ADR-003(FreeSql 选型)、ADR-004(JWT 网关集中校验)
