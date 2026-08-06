# SmartCampusOS 智慧校园管理系统 — 测试报告(2026-08-06)

- **测试环境**:Windows / amd64;.NET SDK 10.0.302;Node.js v24.16.0;pnpm 9.15.9(corepack);Docker 29.6.2
- **覆盖范围**:后端回归测试(131 用例)+ **BDD 功能测试(18 后端场景 + 4 前端 E2E 场景)** + 前端类型检查与构建
- **结论:全部通过,无失败用例。**

---

## 一、测试体系总览

| 层次 | 技术栈 | 数量 | 状态 |
|---|---|---|---|
| 单元测试 | xUnit | 125(21+31+73) | ✅ |
| 集成测试(Testcontainers) | xUnit + MySQL/Redis 容器 | 6 | ✅ |
| **BDD 功能测试(后端)** | **Reqnroll + 真实网关/服务/容器** | **18 场景** | ✅ |
| **E2E 功能测试(前端)** | **Playwright + playwright-bdd(中文 Gherkin)** | **4 场景** | ✅ |
| 类型检查 / 构建 | vue-tsc / tsc / vite | — | ✅ |

---

## 二、回归测试结果(原有测试套件)

| 项目 | 结果 | 说明 |
|---|---|---|
| ApiGateway.Tests | 21/21 ✅ | TestHost 网关中间件 |
| IdentityService.UnitTests | 31/31 ✅ | — |
| IdentityService.IntegrationTests | 6/6 ✅ | Testcontainers MySQL 8.4 / Redis,真实容器 |
| SmartCampusOS.SharedKernel.Tests | 73/73 ✅ | 修复 3 处测试缺陷后全绿 |
| 前端 typecheck / admin build | ✅ / ✅ | shared + admin;构建 16.7s |

---

## 三、BDD 功能测试(本次新增)

### 3.1 后端 — Reqnroll(18 场景全过)

**入口**:`Backend/Services/IdentityService/tests/IdentityService.FunctionalTests/`
**架构**:真实环境(不走 TestHost)——`scripts/start-e2e-env.sh` 编排 MySQL/Redis 容器 + IdentityService(localhost:5111)+ ApiGateway(localhost:5000),测试经网关走**真实契约**(JWT 校验、X-* 头透传、Redis 黑名单)。

| Feature | 场景数 | 覆盖 |
|---|---|---|
| `Features/Authentication.feature` | 7 | 密码登录成功/失败、空参数校验、演示账号、刷新令牌轮换与无效拒绝 |
| `Features/Authorization.feature` | 5 | 未带/无效令牌 401、有效令牌访问 /me、登出后令牌拉黑 401、白名单免鉴权 |
| `Features/DataScope.feature` | 6 | admin=ALL、student=SELF、teacher=DEPT 数据权限下发;/me 权限点(含/不含 sys:user) |

> 数据权限与鉴权的执行点在网关(IdentityService 仅信任 X-* 头),故 BDD 走真实网关验证,而非直连服务模拟。

### 3.2 前端 — Playwright + playwright-bdd(4 场景全过)

**入口**:`Frontend/e2e/`(独立包,@playwright/test 1.62.1 + playwright-bdd 9.2.0,中文 Gherkin)
**架构**:playwright.config `webServer` 拉起 vite dev(5173,代理至网关 5000),复用 3.1 的后端环境。

| Feature | 场景数 | 覆盖 |
|---|---|---|
| `features/login.feature` | 4 | 管理员登录→驾驶舱+系统管理菜单;错误密码提示;学生登录→无权限 403;退出登录回登录页 |

---

## 四、测试中发现并修复的问题

### 回归测试阶段(详见历史报告)
1. `SnowflakeIdTests.NextId_不同节点实例不冲突` 固定时钟下死循环(序列耗尽自旋等待永不前进的时钟)→ 循环内推进时钟。
2. `SnowflakeIdTests` 两处 `ExtractTimestamp` 断言语义错误 → 修正为绝对 UTC 毫秒时间戳断言。

### BDD 功能测试阶段(新增)
3. **前端守卫无限重定向(真实功能缺陷)**:学生等无任何后台权限的用户登录后,`router/index.ts` 守卫在 `generateRoutes()` 后动态路由仍为空时,对 `/` 反复 `return { path: to.path }` 重放,触发 Vue Router 死循环保护,用户被卡在登录页。**修复**:注册后仍无可访问路由时,静态 `/403` 放行、其余路径统一跳 `/403`。E2E 场景 3 由此从失败转通过。
4. **网关限流干扰测试**:ApiGateway 默认 `DefaultRps=10/s`,测试快速连续请求触发 `COMMON-2001` 限流 → 测试环境脚本以 `Gateway__RateLimit__DefaultRps=10000` 放宽(仅影响测试环境,不改生产配置)。
5. 测试脚本 `setsid` 在 git bash 不可用 → 改用 `nohup`;Gherkin 语言头须为 `# language: zh-CN`(非 `# 语言`)。

---

## 五、测试环境编排

- `scripts/start-e2e-env.sh`:幂等启动 MySQL/Redis 容器 + IdentityService(5111)+ ApiGateway(5000),端到端探活(登录 admin → 200)后返回;退出码非 0 报错。
- `scripts/stop-e2e-env.sh`:停止服务进程并移除容器。
- 后端 BDD 的 `[BeforeTestRun]` Hook 自动检测环境就绪,未就绪则调用启动脚本。
- 前端 E2E 复用同一后端环境,`webServer` 只负责拉起 vite dev。

---

## 六、遗留问题与建议

1. **IntegrationTests nullable 告警**:`IntegrationFixture.cs:76`(CS8603)、`AuthFlowTests.cs:35`(CS8602)未处理,建议修复。
2. **空测试目录**:6 个服务(Data/Dorm/Edu/File/Notice/PushGateway)的 `tests/` 为空;建议按本次 IdentityService 模式为高价值服务补充 BDD 功能测试。
3. **E2E 定位器耦合 UI 文案**:steps 依赖 placeholder/文案(如「登 录」),UI 调整时需同步更新;可引入 `data-testid` 加固。
4. **Reqnroll 3.3.4 首次引入**:nuget.org 最新版;版本升级需回归验证与 xunit 兼容性。
5. **限流放宽仅限测试环境**:通过环境变量注入,生产配置未变。

---

## 七、复现命令

```bash
# 1. 启动功能测试环境(容器 + 网关 + 服务)
bash scripts/start-e2e-env.sh

# 2. 后端 BDD 功能测试(Reqnroll,18 场景)
cd Backend/Services/IdentityService && dotnet test tests/IdentityService.FunctionalTests

# 3. 前端 E2E(Playwright,4 场景;需先装 e2e 依赖与浏览器)
cd Frontend/e2e && npm install && npx playwright install chromium
npx playwright-bdd test          # 或 npx playwright test

# 4. 全量回归
cd Backend && dotnet test        # 各解决方案
cd Frontend && corepack pnpm -r typecheck

# 5. 停止环境
bash scripts/stop-e2e-env.sh
```
