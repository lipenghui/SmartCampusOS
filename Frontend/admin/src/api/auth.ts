/**
 * 认证接口（LLD §3.2 IdentityService）：登录 / 刷新 / 登出 / 当前用户。
 * 请求/响应结构对齐 IdentityService.Api DTO（AuthDtos.cs）。
 */
import { http } from '@/utils/request'
import type { DataScope, UserType } from '@smartcampus/shared'

export interface UserInfo {
  userId: number
  userNo: string
  realName: string
  userType: UserType
  roles: string[]
  dataScope: DataScope
}

/**
 * 兼容后端两种 roles 形态：`string[]`（登录接口，角色码列表）或
 * `{ roleId, code, name, dataScope }[]`（/api/v1/me，角色对象列表）。
 */
export type RolesPayload = string[] | { code: string; [k: string]: unknown }[]

/** 归一化 roles 为角色码数组。 */
export function normalizeRoles(roles: RolesPayload): string[] {
  if (!Array.isArray(roles) || roles.length === 0) return []
  return typeof roles[0] === 'string' ? (roles as string[]) : (roles as { code: string }[]).map((r) => r.code)
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserInfo
}

export interface LoginRequest {
  /** password（学工号+密码）| captcha（手机号+验证码） */
  grantType: 'password' | 'captcha'
  userNo?: string
  password?: string
  mobile?: string
  code?: string
}

/** GET /api/v1/me：当前用户信息 + 权限码列表（Common-03）。 */
export interface MeResult {
  userId: number
  userNo: string
  realName: string
  userType: UserType
  roles: RolesPayload
  dataScope: DataScope
  permissions: string[]
  avatarUrl?: string
}

export const authApi = {
  login: (data: LoginRequest) => http.post<LoginResult>('/auth/login', data),
  sendCaptcha: (mobile: string) => http.post<void>('/auth/captcha', { mobile }),
  refresh: (refreshToken: string) => http.post<LoginResult>('/auth/refresh', { refreshToken }),
  logout: (refreshToken?: string) => http.post<void>('/auth/logout', { refreshToken }),
  me: () => http.get<MeResult>('/me'),
}
