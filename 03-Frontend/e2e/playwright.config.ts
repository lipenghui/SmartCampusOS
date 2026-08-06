import { defineConfig } from '@playwright/test'
import { defineBddConfig } from 'playwright-bdd'

const testDir = defineBddConfig({
  features: 'features/*.feature',
  steps: 'steps/*.ts',
})

export default defineConfig({
  testDir,
  timeout: 60_000,
  retries: 0,
  workers: 1, // 串行执行:共享同一后端环境,避免限流与状态干扰
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:5173',
    headless: true,
    // 使用已安装的完整 chromium(新 headless 模式),避免额外下载 chromium-headless-shell
    launchOptions: { channel: 'chromium' },
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: {
    // 前端 dev server;后端环境(网关 localhost:5000 + IdentityService)由 04-scripts/start-e2e-env.sh 提供
    command: 'npx vite --port 5173 --strictPort',
    cwd: '../admin',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
    timeout: 120_000,
  },
})
