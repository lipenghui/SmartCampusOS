/**
 * 管理端统一请求实例（LLD §2.7.3）：注入 JWT、统一响应码处理、401 自动刷新与重放。
 * 基于 @smartcampus/shared 的 createHttpClient 工厂，注入 Element Plus 提示与用户态。
 */
import { createHttpClient } from '@smartcampus/shared'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useUserStore } from '@/stores/user'

export const http = createHttpClient({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  getAccessToken: () => useUserStore().accessToken,
  getRefreshToken: () => useUserStore().refreshToken,
  onRefreshToken: async () => {
    const userStore = useUserStore()
    const ok = await userStore.refreshAction()
    return ok ? userStore.accessToken : null
  },
  onLogout: () => {
    const userStore = useUserStore()
    userStore.resetAuth()
    if (router.currentRoute.value.path !== '/login') {
      router.replace({ path: '/login', query: { redirect: router.currentRoute.value.fullPath } })
    }
  },
  onError: (code, message) => {
    // 登录态相关错误由 onLogout 流程统一处理，避免重复弹窗
    if (code === '-1' || code === 'AUTH-1002' || code === 'TOKEN_REFRESH_FAILED') return
    ElMessage.error(message)
  },
})

/** 便捷导出：GET 文件流（报表导出等）。 */
export function downloadFile(url: string, params?: Record<string, unknown>, filename = 'export.xlsx'): Promise<void> {
  return http.instance
    .get(url, { params, responseType: 'blob' })
    .then((res) => {
      const blob = res.data as Blob
      const link = document.createElement('a')
      const objectUrl = URL.createObjectURL(blob)
      link.href = objectUrl
      link.download = filename
      link.click()
      URL.revokeObjectURL(objectUrl)
    })
}
