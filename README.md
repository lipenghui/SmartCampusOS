# SmartCampusOS 智慧校园管理系统

一站式智慧校园解决方案:基于 **.NET 10 微服务** 后端与 **Vue 3** 管理端,覆盖身份认证、教务管理、宿舍后勤、通知家校、数据可视化、消息推送与文件服务等核心场景,内置 RBAC 权限、数据权限(DataScope)、统一审计与 .NET Aspire 本地编排。

## ✨ 功能模块

| 模块 | 服务 | 说明 |
|---|---|---|
| 认证授权 | IdentityService | 登录/刷新/登出、JWT 签发、RBAC 角色权限、数据权限(DataScope)、验证码登录、审计日志 |
| 教务管理 | EduService | 学期/课表/选课/成绩/考试/学籍 |
| 宿舍后勤 | DormService | 宿舍结构/入住/报修工单/场地预约/设施管理 |
| 通知家校 | NoticeService | 公告/消息中心/家校会话/家长绑定 |
| 数据可视化 | DataService | 管理驾驶舱/大屏/报表 |
| 消息推送 | PushGateway | 推送任务 |
| 文件服务 | FileService | 文件上传/访问 |
| API 网关 | ApiGateway | YARP 反向代理、JWT 校验、限流、Token 黑名单、灰度 |

## 🧱 技术栈

- **后端**:.NET 10 · ASP.NET Core · YARP · FreeSql · MassTransit + RabbitMQ · StackExchange.Redis · JWT(HS256) · OpenTelemetry
- **前端**:Vue 3 · TypeScript · Vite · Element Plus · Pinia · Vue Router · ECharts
- **基础设施**:MySQL 8.4 · Redis · RabbitMQ · Docker
- **编排**:.NET Aspire AppHost(本地一键起全部服务)
- **测试**:xUnit · Testcontainers · Reqnroll(BDD) · Playwright + playwright-bdd(E2E)

## 📁 目录结构

```
SmartCampusOS/
├── 01-Docs/                    # PRD / LLD / 数据库设计 / 测试报告
├── 02-Backend/
│   ├── ApiGateway/             # API 网关(端口 5000)
│   ├── Host/                   # .NET Aspire AppHost 编排 + ServiceDefaults
│   ├── Services/               # 7 个微服务(端口 5111~5117)
│   └── Shared/SharedKernel/    # 共享内核:Result/雪花ID/时间/脱敏/DataScope
├── 03-Frontend/
│   ├── admin/                  # Vue 3 管理端(端口 5173)
│   └── packages/shared/        # 前端共享包(请求封装/类型/工具)
├── 04-scripts/                 # 功能测试环境编排脚本
└── tests 相关:后端 BDD(Reqnroll)、前端 E2E(Playwright)
```

## 🚀 快速开始

### 1. 启动后端(全部服务 + 基础设施)

```bash
dotnet run --project 02-Backend/Host/src/SmartCampusOS.AppHost
```

> 编排细节、资源清单、常见问题见 [02-Backend/Host/README.md](02-Backend/Host/README.md)。

Aspire 会自动拉起 7 个 MySQL 库（每服务一库）+ Redis + RabbitMQ 与全部服务:

| 资源 | 地址 |
|---|---|
| API 网关 | http://localhost:5000 |
| 微服务 | http://localhost:5111 ~ 5117 |
| Aspire Dashboard | 启动日志中的动态地址(含登录 token) |

### 2. 启动前端

```bash
cd 03-Frontend
corepack pnpm install        # 首次
cd admin && corepack pnpm dev   # http://localhost:5173
```

### 3. 默认账号

| 账号 | 密码 | 角色 |
|---|---|---|
| `admin` | `Admin@123` | 系统管理员(数据权限 ALL) |
| `T1001` / `S2026001` 等演示账号 | `Test@123` | 教师 / 学生等 |

> 数据库由服务启动时自动建表并幂等播种(`FreeSql CodeFirst + DbSeeder`),无需手动初始化。

## 🧪 测试

| 层次 | 技术 | 数量 |
|---|---|---|
| 单元测试 | xUnit | 125 |
| 集成测试 | xUnit + Testcontainers(真实 MySQL/Redis) | 6 |
| BDD 功能测试(后端) | Reqnroll,经真实网关走完整契约 | 18 场景 |
| E2E 功能测试(前端) | Playwright + playwright-bdd(中文 Gherkin) | 4 场景 |

```bash
# 功能测试环境(容器 + 服务)
bash 04-scripts/start-e2e-env.sh

# 后端 BDD
cd 02-Backend/Services/IdentityService && dotnet test tests/IdentityService.FunctionalTests

# 前端 E2E
cd 03-Frontend/e2e && npm install && npx playwright install chromium && npx playwright-bdd test

# 停止环境
bash 04-scripts/stop-e2e-env.sh
```

详细测试报告见 [`01-Docs/SmartCampusOS-测试报告-20260806.md`](01-Docs/SmartCampusOS-测试报告-20260806.md)。

## 📚 文档

- [产品需求文档 PRD](01-Docs/SmartCampusOS-智慧校园管理系统-PRD.md)
- [低层设计文档 LLD](01-Docs/SmartCampusOS-智慧校园管理系统-LLD.md)（v1.1 起标注各模块实现状态：✅ 已实现 / 🚧 规划中 / ➡️ 目标架构）
- [数据库设计文档](01-Docs/SmartCampusOS-智慧校园管理系统-数据库设计文档.md)（v1.1 起标注各库实现状态，identity_db 字段与代码实体核对一致）
- [架构决策记录 ADR](01-Docs/ADR-SmartCampusOS-架构决策记录.md)
- [测试报告](01-Docs/SmartCampusOS-测试报告-20260806.md)

## ⚠️ 安全提示

仓库中 `appsettings.json` 的 JWT 签名密钥(`change-me-in-production-32bytes-min`)与 AES 密钥(`dev-only-aes-key-change-me`)均为**开发占位符**,部署生产环境前务必替换,并通过环境变量/密钥管理注入。
