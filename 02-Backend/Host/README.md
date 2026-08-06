# SmartCampusOS.Host — 本地编排(解决方案根)

> 本 README 位于 `02-Backend/Host/` 解决方案根(`SmartCampusOS.Host.slnx`),覆盖两个项目:
> 
> - **SmartCampusOS.AppHost** — .NET Aspire 本地编排(本 README 的主角)
> - **SmartCampusOS.ServiceDefaults** — 各服务共享的默认配置(OpenTelemetry / 服务发现 / 健康检查)

本项目的**本地开发一键入口**:通过 .NET Aspire 编排,一条命令拉起全部 7 个微服务、API 网关及基础设施容器(MySQL × 7、Redis、RabbitMQ),并提供 Aspire Dashboard 统一查看日志、指标、链路与资源状态。

对应设计文档:LLD §2.4 编排选型、§10.1 开发环境。

## 前置要求

| 依赖             | 版本     | 说明                                    |
| -------------- | ------ | ------------------------------------- |
| .NET SDK       | 10.0+  | 编译并运行 AppHost 与各服务                    |
| Docker Desktop | 任意可用版本 | 托管 MySQL / Redis / RabbitMQ 容器(需保持运行) |

> 无需手工初始化数据库:各服务启动时由 **FreeSql CodeFirst 自动建表 + 幂等播种**(`DbSeeder`),默认账号见仓库根 `README.md`。

## 快速开始

```bash
# 1. 从仓库根目录启动(或直接运行 02-Backend/Host/SmartCampusOS.Host.slnx)
dotnet run --project src/SmartCampusOS.AppHost
```

启动后:

| 资源                                                                                 | 地址                                          |
| ---------------------------------------------------------------------------------- | ------------------------------------------- |
| API 网关(统一入口)                                                                       | http://localhost:5000                       |
| IdentityService                                                                    | http://localhost:5111                       |
| EduService / DormService / NoticeService / DataService / PushGateway / FileService | http://localhost:5112 ~ 5117                |
| Aspire Dashboard                                                                   | 启动日志中的动态地址(含登录 token;`launchBrowser` 会自动打开) |

- 前端联调:`cd 03-Frontend/admin && corepack pnpm dev`,dev 服务器(5173)将 `/api` 代理至网关 5000。
- 停止:在 AppHost 终端按 `Ctrl+C`(Aspire 会一并回收全部子进程与容器)。

## 编排的资源

```
ApiGateway :5000 (YARP 反向代理,唯一入口)
 ├── identity-service :5111 ── MySQL(identity-db) ─┐
 ├── edu-service     :5112 ── MySQL(edu-db)      ─┤
 ├── dorm-service    :5113 ── MySQL(dorm-db)     ─┤── Redis ── RabbitMQ
 ├── notice-service  :5114 ── MySQL(notice-db)   ─┤
 ├── data-service    :5115 ── MySQL(data-db)     ─┤
 ├── push-gateway    :5116 ── MySQL(push-db)     ─┘
 └── file-service    :5117 ── MySQL(file-db)
```

| 类别   | 资源                                              | 说明                                            |
| ---- | ----------------------------------------------- | --------------------------------------------- |
| 网关   | `api-gateway`                                   | YARP 反向代理:JWT 校验 / 限流 / 灰度 / Token 黑名单(Redis) |
| 微服务  | 7 个 `*-service`                                 | 每服务独立数据库,端口固定 5111~5117                       |
| 数据库  | `mysql-identity/edu/dorm/notice/data/push/file` | 每服务一库,`AddDatabase` 注册                        |
| 缓存   | `redis`                                         | 会话 / 黑名单 / 限流计数 / 字典缓存                        |
| 消息总线 | `rabbitmq`                                      | MassTransit 领域事件                              |

> **实现状态提示**:当前仅 ApiGateway 与 IdentityService 完整落地,其余 6 个服务为脚手架(Api 占位,`Application/Domain/Infrastructure` 层为空)。Aspire 会照常拉起全部 8 个项目,但空服务的业务接口会 404。实现状态详见 `01-Docs/` 下 LLD v1.1 各服务标注。

## 关键约定(修改前必读)

1. **端口必须显式固定**:`Program.cs` 中每个服务通过 `WithHttpEndpoint(port: ...)` 显式绑定端口。没有 `launchSettings.json` 的服务若不显式配置,不会收到 Aspire 注入的 `ASPNETCORE_URLS`,将全部默认绑定 5000 导致冲突。
2. **连接串强类型注入**:数据库连接串显式注入服务实际读取的强类型配置键(如 `Identity:Database:ConnectionString`),避免服务回退到 `appsettings.json` 的默认连接串。
3. **网关固定 5000**:与前端 `Web/admin` dev 代理默认目标一致(LLD §2.7.3.4)。
4. **启动顺序**:AppHost 通过 `WaitFor(...)` 保证服务等待其依赖的 MySQL/Redis/RabbitMQ 就绪后再启动。

## 常见问题

| 现象                     | 排查                                                                         |
| ---------------------- | -------------------------------------------------------------------------- |
| 端口占用(5000 / 5111~5117) | 检查是否有残留的旧服务进程;`Ctrl+C` 未正常回收时用任务管理器结束 `ApiGateway` / `IdentityService` 等进程 |
| 容器启动失败                 | 确认 Docker Desktop 已启动;`docker ps` 检查 Aspire 容器是否运行;首次拉镜像较慢属正常              |
| Dashboard 地址/token     | 每次启动动态生成,查看 AppHost 启动日志(Dashboard 登录页也会显示)                                |
| 业务接口 404               | 确认访问的是已实现的服务(Identity/ApiGateway);其余服务接口尚未实现(见上文实现状态提示)                    |

## 与其他启动方式的关系

- **`04-scripts/start-e2e-env.sh`**:功能测试环境的独立编排(容器 + IdentityService + ApiGateway),**不依赖 Aspire**,供 Reqnroll BDD 与前端 Playwright E2E 使用。两者并存,互不干扰。
- **生产部署**:Aspire 仅用于本地开发;生产环境按 LLD §10.1 走 Kubernetes(目标架构,仓库暂无 K8s 清单)。
