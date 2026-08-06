/**
 * 用户态 store：JWT 与刷新令牌、当前用户信息、多角色、数据范围（LLD §8.1 / §8.2）。
 * 令牌持久化于 localStorage，刷新令牌轮换后同步更新。
 */
import { defineStore } from 'pinia'
import { authApi, normalizeRoles, type LoginRequest, type UserInfo } from '@/api/auth'
import type { DataScope, UserType } from '@smartcampus/shared'

const ACCESS_KEY = 'scos_access_token'
const REFRESH_KEY = 'scos_refresh_token'
const EXPIRE_KEY = 'scos_token_expires_at'

interface UserState {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: number | null
  userInfo: UserInfo | null
  permissions: string[]
}

export const useUserStore = defineStore('user', {
  state: (): UserState => ({
    accessToken: localStorage.getItem(ACCESS_KEY),
    refreshToken: localStorage.getItem(REFRESH_KEY),
    expiresAt: localStorage.getItem(EXPIRE_KEY) ? Number(localStorage.getItem(EXPIRE_KEY)) : null,
    userInfo: null,
    permissions: [],
  }),

  getters: {
    isLoggedIn: (state) => !!state.accessToken,
    roles: (state) => state.userInfo?.roles ?? [],
    dataScope: (state) => state.userInfo?.dataScope ?? 'self',
    /** 超级管理员（内置系统管理角色）拥有全部权限。 */
    isSuperAdmin: (state) => state.userInfo?.roles.some((r) => r === 'admin' || r === 'sys_admin') ?? false,
    displayName: (state) => state.userInfo?.realName ?? state.userInfo?.userNo ?? '未登录',
  },

  actions: {
    async login(payload: LoginRequest) {
      const result = await authApi.login(payload)
      this.applyAuth(result.accessToken, result.refreshToken, result.expiresAt, result.user)
      return result
    },

    /** 刷新令牌：成功返回 true（供请求封装重放），失败清空登录态。 */
    async refreshAction(): Promise<boolean> {
      const token = this.refreshToken
      if (!token) return false
      try {
        const result = await authApi.refresh(token)
        this.applyAuth(result.accessToken, result.refreshToken, result.expiresAt, result.user)
        return true
      } catch {
        this.resetAuth()
        return false
      }
    },

    /** 拉取当前用户信息与权限码（动态路由 / v-permission 依赖）。 */
    async fetchProfile() {
      const me = await authApi.me()
      this.userInfo = this.normalizeUser(me)
      this.permissions = me.permissions ?? []
    },

    /** 后端枚举为大写（Staff/ALL），角色可能是对象列表；统一归一化为前端小写契约。 */
    normalizeUser(user: { userId: number; userNo: string; realName: string; userType: string; roles: unknown; dataScope: string }): UserInfo {
      return {
        userId: user.userId,
        userNo: user.userNo,
        realName: user.realName,
        userType: user.userType.toLowerCase() as UserType,
        roles: normalizeRoles(user.roles as never),
        dataScope: user.dataScope.toLowerCase() as DataScope,
      }
    },

    applyAuth(accessToken: string, refreshToken: string, expiresAt: string, user: UserInfo) {
      this.accessToken = accessToken
      this.refreshToken = refreshToken
      this.expiresAt = new Date(expiresAt).getTime()
      this.userInfo = this.normalizeUser(user)
      this.permissions = []
      localStorage.setItem(ACCESS_KEY, accessToken)
      localStorage.setItem(REFRESH_KEY, refreshToken)
      localStorage.setItem(EXPIRE_KEY, String(this.expiresAt))
    },

    async logout() {
      const refresh = this.refreshToken
      this.resetAuth()
      // 登出通知后端撤销令牌（失败不影响前端登出）
      if (refresh) {
        try {
          await authApi.logout(refresh)
        } catch {
          /* ignore */
        }
      }
    },

    resetAuth() {
      this.accessToken = null
      this.refreshToken = null
      this.expiresAt = null
      this.userInfo = null
      this.permissions = []
      localStorage.removeItem(ACCESS_KEY)
      localStorage.removeItem(REFRESH_KEY)
      localStorage.removeItem(EXPIRE_KEY)
    },
  },
})
