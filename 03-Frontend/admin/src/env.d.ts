/// <reference types="vite/client" />

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const component: DefineComponent<{}, {}, any>
  export default component
}

interface ImportMetaEnv {
  /** API 网关前缀（默认 /api/v1）。 */
  readonly VITE_API_BASE_URL: string
  /** 本地开发代理目标（.NET Aspire 网关）。 */
  readonly VITE_PROXY_TARGET?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
