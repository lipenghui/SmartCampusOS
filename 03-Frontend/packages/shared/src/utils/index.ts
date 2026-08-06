/**
 * 通用前端工具：日期格式化、敏感信息脱敏（对齐 SharedKernel.MaskUtil 思路）、文件下载等。
 */

/** 日期/时间格式化，支持 `yyyy-MM-dd HH:mm:ss` 风格（LLD §1.4 时间约定）。 */
export function formatDate(input: string | number | Date | null | undefined, fmt = 'yyyy-MM-dd HH:mm:ss'): string {
  if (input === null || input === undefined || input === '') return '-'
  const date = input instanceof Date ? input : new Date(input)
  if (Number.isNaN(date.getTime())) return '-'
  const pad = (n: number, len = 2) => String(n).padStart(len, '0')
  return fmt
    .replace('yyyy', String(date.getFullYear()))
    .replace('MM', pad(date.getMonth() + 1))
    .replace('dd', pad(date.getDate()))
    .replace('HH', pad(date.getHours()))
    .replace('mm', pad(date.getMinutes()))
    .replace('ss', pad(date.getSeconds()))
}

/** 仅日期（yyyy-MM-dd）。 */
export function formatDay(input: string | number | Date | null | undefined): string {
  return formatDate(input, 'yyyy-MM-dd')
}

/** 手机号脱敏：138****5678。 */
export function maskPhone(mobile?: string | null): string {
  if (!mobile || mobile.length < 7) return mobile ?? '-'
  return `${mobile.slice(0, 3)}****${mobile.slice(-4)}`
}

/** 身份证脱敏：保留前 4 后 4（SharedKernel 同规则）。 */
export function maskIdCard(id?: string | null): string {
  if (!id || id.length < 8) return id ?? '-'
  return `${id.slice(0, 4)}**********${id.slice(-4)}`
}

/** 姓名脱敏：2 字显示首字，3 字及以上显示首尾。 */
export function maskName(name?: string | null): string {
  if (!name) return '-'
  if (name.length <= 1) return name
  if (name.length === 2) return `${name[0]}*`
  return `${name[0]}${'*'.repeat(name.length - 2)}${name[name.length - 1]}`
}

/** 睡眠（用于演示/重试场景）。 */
export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

/** 前端触发浏览器下载（文件流 / Blob）。 */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  URL.revokeObjectURL(url)
}

/** 将对象转为 URL 查询串（跳过空值）。 */
export function toQueryString(params?: Record<string, unknown> | null): string {
  if (!params) return ''
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
  return parts.length ? `?${parts.join('&')}` : ''
}
