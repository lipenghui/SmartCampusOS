import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

// 本地开发经 .NET Aspire 网关访问后端（LLD §2.7.3.4）；VITE_PROXY_TARGET 默认网关地址。
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const proxyTarget = env.VITE_PROXY_TARGET || 'http://localhost:5000'

  return {
    plugins: [vue()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
        '@smartcampus/shared': fileURLToPath(new URL('../packages/shared/src/index.ts', import.meta.url)),
      },
    },
    server: {
      port: 5173,
      host: true,
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
        },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: false,
      chunkSizeWarningLimit: 1500,
      rollupOptions: {
        output: {
          // 三方库分包：控制首屏体积（LLD §6.1 首屏 ≤ 2s）
          manualChunks: {
            vue: ['vue', 'vue-router', 'pinia'],
            'element-plus': ['element-plus', '@element-plus/icons-vue'],
            echarts: ['echarts'],
          },
        },
      },
    },
  }
})
