/**
 * 路由入口：静态路由（登录/错误页/布局）+ 全局守卫（登录态、权限过滤、动态路由注册）。
 * 动态路由按 RBAC 权限过滤（LLD §2.7.2），首次进入加载用户信息后注册。
 */
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'
import { asyncRoutes } from '@/router/modules'

const Layout = () => import('@/layouts/index.vue')

/** 静态路由（无需权限过滤）。 */
export const constantRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/login/index.vue'),
    meta: { title: '登录', hidden: true },
  },
  {
    path: '/',
    name: 'layout',
    component: Layout,
    // 无静态 redirect：登录落点由守卫按权限动态决定（非管理员无 data:dashboard 权限）
    children: [],
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/views/error/403.vue'),
    meta: { title: '无权限', hidden: true },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/error/404.vue'),
    meta: { title: '页面不存在', hidden: true },
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes: constantRoutes,
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async (to) => {
  const userStore = useUserStore()
  const permissionStore = usePermissionStore()

  // 未登录 → 登录页（保留 redirect）
  if (!userStore.isLoggedIn) {
    if (to.path === '/login') return true
    return { path: '/login', query: { redirect: to.fullPath } }
  }

  // 已登录访问登录页 → 首页
  if (to.path === '/login') return { path: '/' }

  // 首次进入：用户信息未加载、权限码未加载或动态路由未注册时，拉取用户信息并注册动态路由。
  // 注意：登录成功（applyAuth）已写入 userInfo 但 permissions 为空（登录接口不返回权限码，
  // 权限码仅来自 /me），因此必须额外检查 permissions 为空也调用 fetchProfile，
  // 否则会以空权限码过滤动态路由，导致所有页面落 403。
  if (!userStore.userInfo || userStore.permissions.length === 0 || permissionStore.dynamicRoutes.length === 0) {
    try {
      if (!userStore.userInfo || userStore.permissions.length === 0) {
        await userStore.fetchProfile()
      }
      permissionStore.generateRoutes()
      // 注册后仍无任何可访问的动态路由：当前用户无后台菜单权限（如学生登录管理端）。
      // 直接落静态 403，避免对根路径/原路径反复 return 重放造成无限重定向。
      if (permissionStore.dynamicRoutes.length === 0) {
        if (to.path === '/403') return true
        return { path: '/403', replace: true }
      }
      // 刷新深层页面时 to 可能已被解析为 404（动态路由注册前），
      // 不能用 {...to} 重放（会保留 404 的 name/matched）；仅重建 path 让路由重新解析
      return { path: to.path, query: to.query, hash: to.hash, replace: true }
    } catch {
      userStore.resetAuth()
      ElMessage.error('获取用户信息失败，请重新登录')
      return { path: '/login', query: { redirect: to.fullPath } }
    }
  }

  // 权限校验（meta.permission）
  const permission = to.meta.permission as string | string[] | undefined
  if (permission && !permissionStore.hasPermission(permission)) {
    return { path: '/403' }
  }

  // 登录落点：访问根路径 → 跳到当前角色有权限的首个菜单（管理员=驾驶舱，教师=教务首页等）
  if (to.path === '/') {
    return { path: permissionStore.firstMenuPath, replace: true }
  }

  // 命中 404 兜底：已知业务路径说明该用户无权限（动态路由被权限过滤）→ 403；未知路径保持 404
  if (to.name === 'not-found') {
    if (isKnownDynamicPath(to.path)) {
      return { path: '/403' }
    }
    return true
  }

  return true
})

/** 判断路径是否为受权限控制的后台业务路径（存在于原始动态路由表中）。 */
function isKnownDynamicPath(path: string): boolean {
  return asyncRoutes.some(
    (r) => r.path === path || (r.children ?? []).some((c) => c.path === path),
  )
}

// 注册后置钩子：动态路由未加载时（如刷新直接访问深层路径）重试
router.afterEach(() => {})

export default router
