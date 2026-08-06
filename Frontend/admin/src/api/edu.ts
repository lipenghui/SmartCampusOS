/**
 * EduService 接口层（LLD §3.3）：学期 / 课表 / 调课 / 选课 / 成绩 / 考试 / 学籍。
 */
import { http } from '@/utils/request'
import type { ApprovalStatus, PagedResult } from '@smartcampus/shared'

// ---------- 学期与教学计划（Edu-01） ----------
export interface SemesterItem {
  id: number
  name: string
  schoolYear: string
  startDate: string
  endDate: string
  weekCount: number
  status: 'draft' | 'active' | 'closed'
}

// ---------- 课表（Edu-02） ----------
export interface ScheduleItem {
  id: number
  semesterId: number
  coursePlanId: number
  courseName: string
  className?: string
  teacherName?: string
  roomName?: string
  weekday: number
  weekList: string
  periodStart: number
  periodEnd: number
}

export interface ScheduleQuery {
  semesterId?: number
  viewType?: 'class' | 'teacher' | 'room'
  targetId?: number
  weekday?: number
}

// ---------- 调课申请（Edu-02） ----------
export interface AdjustmentItem {
  id: number
  scheduleId: number
  applyUserName: string
  oldValue: string
  newValue: string
  reason: string
  status: ApprovalStatus
  auditedBy?: string
  auditedAt?: string
  createdAt: string
}

// ---------- 选课（Edu-03） ----------
export interface SelectionWindowItem {
  id: number
  semesterId: number
  name: string
  scope: string
  creditLimit: number
  strategy: 'first-come' | 'lottery'
  startAt: string
  endAt: string
  status: 'draft' | 'open' | 'closed'
}

export interface SelectionStat {
  courseName: string
  teacherName?: string
  capacity: number
  selectedCount: number
  fillRate: number
}

// ---------- 成绩（Edu-04） ----------
export interface ScoreItem {
  id: number
  coursePlanId: number
  courseName: string
  studentName: string
  studentNo: string
  scoreValue?: number
  gradeLevel?: string
  status: 'draft' | 'submitted'
  submittedBy?: string
  updatedAt?: string
}

export interface ScoreWarningItem {
  studentName: string
  studentNo: string
  className?: string
  failedCount: number
  gpa?: number
  warningLevel: 'yellow' | 'red'
}

// ---------- 考试（Edu-05） ----------
export interface ExamItem {
  id: number
  semesterId: number
  courseName: string
  examDate: string
  startTime: string
  endTime: string
  roomName?: string
  supervisorNames?: string[]
}

// ---------- 学籍（Edu-06） ----------
export interface StudentProfileItem {
  id: number
  studentUserId: number
  studentNo: string
  studentName: string
  className?: string
  gradeName?: string
  enrollDate?: string
  status: 'studying' | 'suspended' | 'graduated'
}

export const eduApi = {
  // 学期
  listSemesters: (params?: { page?: number; pageSize?: number }) =>
    http.get<PagedResult<SemesterItem>>('/semesters', params),
  createSemester: (data: Omit<SemesterItem, 'id'>) => http.post<number>('/semesters', data),
  updateSemester: (id: number, data: Partial<SemesterItem>) => http.put<void>(`/semesters/${id}`, data),

  // 课表
  listSchedules: (params: ScheduleQuery) => http.get<PagedResult<ScheduleItem>>('/schedules', params),
  generateSchedules: (data: { semesterId: number; constraints?: Record<string, unknown> }) =>
    http.post<{ generated: number }>('/schedules/generate', data),
  checkConflict: (data: { weekday: number; periodStart: number; periodEnd: number; roomId?: number; teacherId?: number; classId?: number }) =>
    http.post<{ conflicted: boolean; conflicts?: string[] }>('/schedules/check-conflict', data),

  // 调课
  listAdjustments: (params?: { page?: number; pageSize?: number; status?: string }) =>
    http.get<PagedResult<AdjustmentItem>>('/schedules/adjustments', params),
  createAdjustment: (data: { scheduleId: number; newValue: string; reason: string }) =>
    http.post<number>('/schedules/adjustments', data),
  approveAdjustment: (id: number, approved: boolean, reason?: string) =>
    http.put<void>(`/schedules/adjustments/${id}/approve`, { approved, reason }),

  // 选课
  listSelectionWindows: (params?: { page?: number; pageSize?: number }) =>
    http.get<PagedResult<SelectionWindowItem>>('/selections/windows', params),
  createSelectionWindow: (data: Omit<SelectionWindowItem, 'id'>) =>
    http.post<number>('/selections/windows', data),
  closeLottery: (windowId: number) => http.post<void>(`/selections/${windowId}/lottery`),
  getSelectionStats: (windowId: number) => http.get<SelectionStat[]>(`/selections/stats`, { windowId }),

  // 成绩
  listScores: (params?: { page?: number; pageSize?: number; semesterId?: number; keyword?: string }) =>
    http.get<PagedResult<ScoreItem>>('/scores', params),
  importScores: (file: File, coursePlanId?: number) =>
    http.upload<{ imported: number }>('/scores/import', file, { coursePlanId }),
  listScoreWarnings: (params?: { semesterId?: number }) =>
    http.get<ScoreWarningItem[]>('/scores/warnings', params),

  // 考试
  listExams: (params?: { page?: number; pageSize?: number; semesterId?: number }) =>
    http.get<PagedResult<ExamItem>>('/exams', params),
  createExam: (data: Omit<ExamItem, 'id'>) => http.post<number>('/exams', data),

  // 学籍
  listStudentProfiles: (params?: { page?: number; pageSize?: number; keyword?: string; status?: string }) =>
    http.get<PagedResult<StudentProfileItem>>('/students/profiles', params),
}
