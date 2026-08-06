# SmartCampusOS Web 前端工程

智慧校园管理系统前端 monorepo（对应 LLD §2.7「前端代码层级架构」），采用 **pnpm workspace** 组织三个前端工程与一个共享包。

## 目录结构

```
Web/
├── admin/                  # Web 管理端（Vue 3 + TypeScript + Vite + Pinia + Element Plus）
│   └── src/
│       ├── api/            # 接口层：按后端服务分组（auth/identity/edu/dorm/notice/data）
│       ├── views/          # 页面（login/dashboard/system/edu/dorm/notice/data/error）
│       ├── router/         # 路由 + 菜单（动态路由按 RBAC 权限过滤，LLD §2.7.2）
│       ├── stores/         # Pinia（user 用户态 / permission 权限 / app 全局 UI）
│       ├── components/     # 通用组件（DataTable / StatusTag / EChart）
│       ├── layouts/        # 管理端框架（侧边栏 / 顶栏 / 多角色切换）
│       ├── directives/     # v-permission 权限指令（对齐后端 RBAC，§8.2）
│       ├── utils/          # request 封装（统一响应码 / 错误处理 / 令牌刷新）
│       └── styles/         # 全局样式与 Element Plus 主题变量
├── app/                    # App/小程序（uni-app，规划中）
├── screen/                 # 数据大屏（Vue 3 + ECharts，规划中）
└── packages/shared/        # 共享包：axios 请求封装、API 类型、通用工具
```

## 环境要求

- Node.js ≥ 20
- pnpm ≥ 9（可通过 `corepack enable` 启用，或 `npm i -g pnpm@9`）

## 快速开始

```bash
# 在 Web/ 根目录安装全部 workspace 依赖
pnpm install

# 启动管理端开发服务器（http://localhost:5173）
pnpm --filter @smartcampus/admin dev

# 类型检查（shared + admin）
pnpm -r typecheck

# 生产构建
pnpm --filter @smartcampus/admin build
```

## 环境变量（admin）

| 变量 | 说明 | 默认值 |
| --- | --- | --- |
| `VITE_API_BASE_URL` | API 网关前缀（LLD §5.1 统一 `/api/v1`） | `/api/v1` |
| `VITE_PROXY_TARGET` | 本地开发代理目标（.NET Aspire 网关地址） | `http://localhost:5000` |

开发模式：Vite 将 `/api` 代理至网关（`vite.config.ts` server.proxy）。
生产模式：Nginx 托管 `dist/` 静态产物，并将 `/api` 反向代理至 ApiGateway（LLD §2.7.4）。

## 与后端对接约定

1. **统一响应结构**：`{ "code": "0", "message": "ok", "data": {...} }`；`code="0"` 成功，其余为 LLD §11 错误码（字符串）。请求封装（`packages/shared/src/request.ts`）自动解包 `data` 并在业务错误时 reject。
2. **分页**：查询参数 `page` / `pageSize`，响应 `{ items, total }`（`PagedResult<T>`）。
3. **认证**：JWT（`Authorization: Bearer`）；access token 过期（HTTP 401 / `AUTH-1002`）自动调用 `/auth/refresh` 单飞刷新并重放原请求，刷新失败跳转登录页。
4. **接口路径**：`src/api/*.ts` 与 LLD §3 各服务接口清单一一对应（`/api/v1/...`，经网关）。
5. **权限**：路由 `meta.permission` 与 `v-permission` 指令使用权限码（如 `sys:user`），与后端 `sys_permission.code` 对齐；数据范围四档（`ALL/GRADE/CLASS/DEPT/SELF`）由后端在网关注入 `X-Data-Scope` 后于仓储层过滤，前端仅控制展示（LLD §8.2）。

## 管理端功能清单

| 模块 | 页面 | 对应需求 |
| --- | --- | --- |
| 数据驾驶舱 | 管理驾驶舱（指标卡 / 趋势图 / 下钻） | Data-01 |
| 系统管理 | 用户管理、组织架构、角色权限、数据字典、审计日志 | Common-02~04 |
| 教务管理 | 学期计划、课表管理、调课审批、选课管理、成绩管理、考试管理、学籍档案 | Edu-01~06 |
| 宿舍后勤 | 宿舍结构（楼栋/房间/床位）、入住管理、报修工单、场地预约、设施管理 | Dorm-01~05 |
| 通知家校 | 公告发布（含已读回执统计）、消息中心、家校会话 | Noti-01~04 |
| 数据可视化 | 报表中心（Excel/PDF 导出） | Data-03 |
