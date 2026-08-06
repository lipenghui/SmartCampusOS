/**
 * axios 统一请求封装工厂（LLD §2.7.3.2 / §5.1）。
 *
 * 职责：
 * 1. 注入 `Authorization: Bearer <JWT>`；
 * 2. 统一处理 `{ code, message, data }` 响应结构：code="0" 返回 data，否则 reject；
 * 3. 令牌过期（HTTP 401 或 AUTH-1002）自动走刷新并重放请求（并发单飞）；
 * 4. 网络/系统错误（code="-1"）不向前端暴露内部细节。
 *
 * UI 提示通过 `onError` 回调注入，保持本包不依赖具体 UI 库。
 */
import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios'
import type { ApiResponse } from './types'

export interface HttpClientOptions {
  /** 网关地址（如 `VITE_API_BASE_URL`）。 */
  baseURL: string
  /** 读取当前 access token。 */
  getAccessToken?: () => string | null
  /** 读取当前 refresh token。 */
  getRefreshToken?: () => string | null
  /**
   * 刷新令牌：返回新的 access token；失败返回 null 触发登出。
   * 实现方负责调用 `/api/v1/auth/refresh` 并落库新令牌。
   */
  onRefreshToken?: () => Promise<string | null>
  /** 刷新失败/登出回调（跳转登录页等）。 */
  onLogout?: () => void
  /** 业务错误提示回调（默认静默）。 */
  onError?: (code: string, message: string) => void
  /** 请求超时（毫秒，默认 10s）。 */
  timeout?: number
}

export interface HttpClient {
  instance: AxiosInstance
  get<T = unknown>(url: string, params?: unknown, config?: AxiosRequestConfig): Promise<T>
  post<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>
  put<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>
  delete<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T>
  upload<T = unknown>(url: string, file: File | Blob, extra?: Record<string, unknown>): Promise<T>
}

/** 业务错误（携带后端错误码）。 */
export class ApiBusinessError extends Error {
  code: string
  constructor(code: string, message: string) {
    super(message)
    this.name = 'ApiBusinessError'
    this.code = code
  }
}

/** 令牌刷新失败的异常标记。 */
export const TOKEN_REFRESH_FAILED = 'TOKEN_REFRESH_FAILED'

export function createHttpClient(options: HttpClientOptions): HttpClient {
  const {
    baseURL,
    getAccessToken,
    getRefreshToken,
    onRefreshToken,
    onLogout,
    onError,
    timeout = 10_000,
  } = options

  const instance = axios.create({
    baseURL,
    timeout,
    headers: { 'Content-Type': 'application/json' },
  })

  let refreshing: Promise<string | null> | null = null

  /** 单飞刷新：并发 401 只触发一次刷新。 */
  function refreshTokenOnce(): Promise<string | null> {
    if (!refreshing) {
      refreshing = (async () => {
        try {
          if (!onRefreshToken) return null
          return await onRefreshToken()
        } catch {
          return null
        } finally {
          refreshing = null
        }
      })()
    }
    return refreshing
  }

  /** 业务层统一错误处理。 */
  function handleError(code: string, message: string): never {
    if (onError) onError(code, message)
    throw new ApiBusinessError(code, message)
  }

  instance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = getAccessToken?.()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  })

  instance.interceptors.response.use(
    (response) => {
      // 兼容文件流/非标准结构
      const body = response.data
      if (body && typeof body === 'object' && 'code' in body) {
        const apiBody = body as ApiResponse
        if (apiBody.code === '0') {
          // 拦截器将响应解包为 data；类型上以 never 桥接 axios 的 AxiosResponse 约束
          return (apiBody.data ?? null) as never
        }
        return handleError(apiBody.code, apiBody.message || '请求失败')
      }
      return body as never
    },
    async (error: AxiosError<ApiResponse>) => {
      const status = error.response?.status
      const code = error.response?.data?.code
      const message = error.response?.data?.message || error.message

      // 未配置刷新回调：按 401 登出处理
      if (status === 401 || code === 'AUTH-1002') {
        if (!onRefreshToken) {
          onLogout?.()
          return handleError(code ?? 'AUTH-1002', '登录已过期，请重新登录')
        }
        try {
          const newToken = await refreshTokenOnce()
          if (!newToken) {
            onLogout?.()
            return handleError(TOKEN_REFRESH_FAILED, '登录已过期，请重新登录')
          }
          // 重放原请求
          const original = error.config as InternalAxiosRequestConfig | undefined
          if (original) {
            original.headers.Authorization = `Bearer ${newToken}`
            return instance(original)
          }
          onLogout?.()
          return handleError(TOKEN_REFRESH_FAILED, '登录已过期，请重新登录')
        } catch (err) {
          if (err instanceof ApiBusinessError) throw err
          onLogout?.()
          return handleError(TOKEN_REFRESH_FAILED, '登录已过期，请重新登录')
        }
      }

      if (status === 403 || code === 'AUTH-1003') {
        return handleError(code ?? 'AUTH-1003', '没有权限执行该操作')
      }
      if (status === 429 || code === 'COMMON-2001') {
        return handleError(code ?? 'COMMON-2001', '请求过于频繁，请稍后再试')
      }
      if (!error.response) {
        return handleError('-1', '网络异常，请检查网络连接')
      }
      return handleError(code ?? '-1', message)
    },
  )

  return {
    instance,
    async get<T = unknown>(url: string, params?: unknown, config?: AxiosRequestConfig) {
      // 响应拦截器已将 AxiosResponse 解包为 data（code="0" 时），此处断言返回类型
      return instance.get(url, { params, ...config }) as unknown as Promise<T>
    },
    async post<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig) {
      return instance.post(url, data, config) as unknown as Promise<T>
    },
    async put<T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig) {
      return instance.put(url, data, config) as unknown as Promise<T>
    },
    async delete<T = unknown>(url: string, config?: AxiosRequestConfig) {
      return instance.delete(url, config) as unknown as Promise<T>
    },
    async upload<T = unknown>(url: string, file: File | Blob, extra?: Record<string, unknown>) {
      const form = new FormData()
      form.append('file', file)
      if (extra) {
        for (const [k, v] of Object.entries(extra)) {
          form.append(k, String(v))
        }
      }
      return instance.post(url, form, { headers: { 'Content-Type': 'multipart/form-data' } }) as unknown as Promise<T>
    },
  }
}
