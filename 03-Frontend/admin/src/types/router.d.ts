import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** 菜单/面包屑标题。 */
    title?: string
    /** 菜单图标（Element Plus 图标组件名）。 */
    icon?: string
    /** 访问所需权限码（空表示公开），支持数组（任一命中即可）。 */
    permission?: string | string[]
    /** 不在侧边栏菜单展示（隐藏路由）。 */
    hidden?: boolean
  }
}
