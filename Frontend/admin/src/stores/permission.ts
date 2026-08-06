/**
 * 权限 store：按用户权限码过滤动态路由，提供菜单与权限点判断（LLD §2.7.2 动态路由按 RBAC 过滤）。
 */
import { defineStore } from 'pinia'
import type { RouteRecordRaw } from 'vue-router'
import router from '@/router'
import { asyncRoutes } from '@/router/modules'
import { useUserStore } from '@/stores/user'

interface PermissionState {
  /** 已注册的动态路由（含父级路由路径链）。 */
  dynamicRoutes: RouteRecordRaw[]
}

/** 判断某权限码是否可用：超管放行；否则在用户权限码集合中。 */
export function hasPermissionCode(permission?: string | string[]): boolean {
  if (!permission || (Array.isArray(permission) && permission.length === 0)) return true
  const userStore = useUserStore()
  if (userStore.isSuperAdmin) return true
  const required = Array.isArray(permission) ? permission : [permission]
  return required.some((p) => userStore.permissions.includes(p))
}

export const usePermissionStore = defineStore('permission', {
  state: (): PermissionState => ({
    dynamicRoutes: [],
  }),

  getters: {
    /** 侧边栏菜单树（仅包含有权限的、可导航的路由）。 */
    menuRoutes: (state): RouteRecordRaw[] => state.dynamicRoutes,

    /** 当前用户可访问的第一个菜单路径（登录后落点；非管理员无 data:dashboard 权限时兜底首个有权限菜单）。 */
    firstMenuPath: (state): string => {
      for (const route of state.dynamicRoutes) {
        if (route.meta?.hidden) continue
        const children = route.children ?? []
        if (children.length > 0) {
          const first = children.find((c) => !c.meta?.hidden)
          if (first) return first.path
        } else {
          // 无 children 的顶层叶子（如 /dashboard）
          return route.path
        }
      }
      return '/403'
    },
  },

  actions: {
    /** 权限点判断（供路由守卫 / 组件内使用）。 */
    hasPermission(permission?: string | string[]): boolean {
      return hasPermissionCode(permission)
    },

    /** 过滤并注册动态路由（挂在 Layout 下）。 */
    generateRoutes(): RouteRecordRaw[] {
      const routes = filterRoutes(asyncRoutes)
      this.dynamicRoutes = routes
      routes.forEach((route) => {
        router.addRoute('layout', route)
      })
      return routes
    },

    resetRoutes() {
      this.dynamicRoutes = []
      router.getRoutes().forEach((r) => {
        if (r.name && String(r.name).startsWith('admin-')) {
          router.removeRoute(r.name)
        }
      })
    },
  },
})

/** 递归过滤无权限的路由（meta.permission 为空表示公开）；子路由全部被过滤的组整体剔除。 */
function filterRoutes(routes: RouteRecordRaw[]): RouteRecordRaw[] {
  const result: RouteRecordRaw[] = []
  for (const route of routes) {
    const routeCopy = { ...route }
    if (route.meta?.permission && !hasPermissionCode(route.meta.permission)) {
      continue
    }
    if (routeCopy.children && routeCopy.children.length) {
      routeCopy.children = filterRoutes(routeCopy.children)
      // 菜单分组：子路由无剩余权限项时整个分组不注册（避免空菜单与错误落点）
      if (routeCopy.children.length === 0) {
        continue
      }
    }
    result.push(routeCopy)
  }
  return result
}
