/**
 * DataService 接口层（LLD §3.6）：驾驶舱指标 / 趋势 / 下钻 / 报表。
 */
import { http } from '@/utils/request'

// ---------- 管理驾驶舱（Data-01） ----------
export interface MetricCard {
  code: string
  name: string
  value: number
  unit?: string
  trend?: number
  drilldownId?: number
}

export interface TrendPoint {
  date: string
  value: number
}

export interface TrendSeries {
  code: string
  name: string
  points: TrendPoint[]
}

export interface DashboardOverview {
  cards: MetricCard[]
  updatedAt?: string
}

// ---------- 报表（Data-03） ----------
export interface ReportItem {
  id: number
  code: string
  name: string
  source: string
  status: 'ready' | 'generating'
  createdAt?: string
}

export const dataApi = {
  getOverview: () => http.get<DashboardOverview>('/dashboards/overview'),
  getTrends: (params: { codes?: string[]; days?: number }) =>
    http.get<TrendSeries[]>('/dashboards/trends', params),
  drilldown: (id: number, params?: { page?: number; pageSize?: number; scopeId?: number }) =>
    http.get<{ items: Record<string, unknown>[]; total: number }>(`/dashboards/${id}/drilldown`, params),

  listReports: (params?: { page?: number; pageSize?: number }) =>
    http.get<{ items: ReportItem[]; total: number }>('/reports', params),
  generateReport: (code: string) => http.post<{ taskId: number }>('/reports/generate', { code }),
  exportReport: (code: string, format: 'excel' | 'pdf') =>
    http.instance.get(`/reports/${code}/export`, { params: { format }, responseType: 'blob' }),
}
