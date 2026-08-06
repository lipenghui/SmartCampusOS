import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

Given('打开登录页面', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByPlaceholder('学工号')).toBeVisible()
})

When('输入学工号 {string}', async ({ page }, userNo: string) => {
  await page.getByPlaceholder('学工号').fill(userNo)
})

When('输入密码 {string}', async ({ page }, password: string) => {
  await page.getByPlaceholder('密码').fill(password)
})

When('点击登录按钮', async ({ page }) => {
  // 按钮文案为「登 录」(含空格)
  await page.getByRole('button', { name: /登\s*录/ }).click()
})

Then('跳转到管理驾驶舱页面', async ({ page }) => {
  await expect(page).toHaveURL(/\/dashboard/, { timeout: 15_000 })
})

Then('停留在登录页面', async ({ page }) => {
  await expect(page).toHaveURL(/\/login/)
})

Then('页面出现登录失败提示', async ({ page }) => {
  // Element Plus ElMessage 错误提示
  await expect(page.locator('.el-message--error')).toBeVisible()
})

Then('跳转到无权限 403 页面', async ({ page }) => {
  await expect(page).toHaveURL(/\/403/, { timeout: 15_000 })
  await expect(page.getByText('403', { exact: true })).toBeVisible()
})

Then('侧边栏显示 {string} 菜单', async ({ page }, title: string) => {
  await expect(page.getByText(title, { exact: true })).toBeVisible()
})

Then('侧边栏不显示 {string} 菜单', async ({ page }, title: string) => {
  await expect(page.getByText(title, { exact: true })).toHaveCount(0)
})

When('点击用户菜单中的退出登录并确认', async ({ page }) => {
  await page.locator('.header__user').click()
  await page.getByText('退出登录', { exact: true }).click()
  // ElMessageBox 确认框
  await page.getByRole('button', { name: '确定' }).click()
})

Then('跳转到登录页面', async ({ page }) => {
  await expect(page).toHaveURL(/\/login/)
})
