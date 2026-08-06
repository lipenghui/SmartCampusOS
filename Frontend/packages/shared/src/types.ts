/**
 * 全局共享类型定义，与后端契约对齐（LLD §5.1 / §11）。
 */

/** 统一响应结构：`{ code, message, data }`；code="0" 成功（LLD §5.1.2）。 */
export interface ApiResponse<T = unknown> {
  code: string
  message: string
  data: T | null
}

/** 统一分页响应：`{ items, total, page, pageSize }`（LLD §5.1.3）。 */
export interface PagedResult<T = unknown> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

/** 分页查询参数。 */
export interface PageQuery {
  page?: number
  pageSize?: number
  keyword?: string
}

/** 全局错误码（LLD §11）。 */
export const ErrorCodes = {
  Success: '0',
  SystemError: '-1',
  AuthInvalidCredentials: 'AUTH-1001',
  AuthTokenExpired: 'AUTH-1002',
  AuthForbidden: 'AUTH-1003',
  CommonTooManyRequests: 'COMMON-2001',
  CommonValidationFailed: 'COMMON-2002',
  CommonNotFound: 'COMMON-2003',
  EduScheduleConflict: 'EDU-2001',
  EduSelectionFull: 'EDU-2002',
  EduScoreLocked: 'EDU-2003',
  DormBedUnavailable: 'DORM-3001',
  DormVenueConflict: 'DORM-3002',
  DormOrderInvalidStatus: 'DORM-3003',
  NotiReceiverUnauthorized: 'NOTI-4001',
  NotiSensitiveWord: 'NOTI-4002',
  DataSnapshotMissing: 'DATA-5001',
  DataReportGenerating: 'DATA-5002',
  PushChannelUnavailable: 'PUSH-6001',
  PushSmsBudgetExceeded: 'PUSH-6002',
} as const

export type ErrorCode = (typeof ErrorCodes)[keyof typeof ErrorCodes]

/** 用户类型（枚举序列化为字符串，对齐 IdentityService）。 */
export type UserType = 'student' | 'teacher' | 'parent' | 'staff'

/** 数据范围四档（LLD §8.2）。 */
export type DataScope = 'all' | 'grade' | 'class' | 'dept' | 'self'

/** 账号状态。 */
export type UserStatus = 'active' | 'inactive' | 'locked'

/** 通用启用/停用状态（数据字典解释）。 */
export type EnabledStatus = 'enabled' | 'disabled'

/** 审批状态（调课/预约/绑定等通用）。 */
export type ApprovalStatus = 'pending' | 'approved' | 'rejected'

/**
 * 校验请求是否成功（code === "0"）。
 */
export function isOk<T>(res: ApiResponse<T>): boolean {
  return res.code === ErrorCodes.Success
}
