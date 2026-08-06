/**
 * 全局 UI store：侧边栏折叠、多角色切换等布局状态（LLD §2.7.2 layouts 多角色切换）。
 */
import { defineStore } from 'pinia'
import { useUserStore } from '@/stores/user'

interface AppState {
  sidebarCollapsed: boolean
  /** 当前展示角色（同一账号多角色，JWT 携带全部角色声明，前端切换视图，LLD §8.1）。 */
  activeRole: string | null
}

export const useAppStore = defineStore('app', {
  state: (): AppState => ({
    sidebarCollapsed: false,
    activeRole: null,
  }),

  getters: {
    /** 可切换的角色列表（去重；默认全部）。 */
    availableRoles(state): string[] {
      const roles = useUserStore().roles
      const base = roles.length ? roles : ['user']
      const set = new Set([...base, ...(state.activeRole ? [state.activeRole] : [])])
      return Array.from(set)
    },
    currentRole(state): string {
      return state.activeRole ?? useUserStore().roles[0] ?? 'user'
    },
  },

  actions: {
    toggleSidebar() {
      this.sidebarCollapsed = !this.sidebarCollapsed
    },
    switchRole(role: string) {
      this.activeRole = role
    },
  },
})
