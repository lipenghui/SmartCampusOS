/**
 * IdentityService 接口层（LLD §3.2）：用户 / 组织 / 角色权限 / 数据字典 / 审计日志 / 家长绑定。
 */
import { http } from '@/utils/request'
import type { DataScope, PagedResult, UserType } from '@smartcampus/shared'

// ---------- 用户（Common-02） ----------
export interface UserItem {
  id: number
  userNo: string
  realName: string
  mobile?: string
  userType: UserType
  status: 'active' | 'inactive' | 'locked'
  orgName?: string
  roleNames?: string[]
  createdAt?: string
}

export interface UserQuery {
  page?: number
  pageSize?: number
  keyword?: string
  userType?: UserType
  status?: string
  orgId?: number
}

export interface UserSavePayload {
  userNo: string
  realName: string
  mobile?: string
  userType: UserType
  password?: string
  orgId?: number
  roleIds?: number[]
}

// ---------- 组织（Common-02） ----------
export type OrgType = 'school' | 'grade' | 'class' | 'dept'

export interface OrgNode {
  id: number
  parentId: number | null
  orgType: OrgType
  name: string
  code?: string
  sort?: number
  children?: OrgNode[]
}

export interface OrgSavePayload {
  parentId: number | null
  orgType: OrgType
  name: string
  code?: string
  sort?: number
}

// ---------- 角色权限（Common-03） ----------
export interface PermissionItem {
  code: string
  name: string
  module: string
}

export interface RoleItem {
  id: number
  code: string
  name: string
  dataScope: DataScope
  builtin: boolean
  permissionCodes?: string[]
}

export interface RoleSavePayload {
  code: string
  name: string
  dataScope: DataScope
  permissionCodes: string[]
}

// ---------- 数据字典（通用） ----------
export interface DictItem {
  id: number
  dictCode: string
  itemCode: string
  itemName: string
  sort?: number
}

export interface Dict {
  id: number
  code: string
  name: string
  items?: DictItem[]
}

// ---------- 审计日志（Common-04） ----------
export interface AuditLogItem {
  id: number
  userId?: number
  userName?: string
  action: string
  targetType?: string
  targetId?: string
  detail?: string
  ip?: string
  createdAt: string
}

export interface AuditLogQuery {
  page?: number
  pageSize?: number
  keyword?: string
  action?: string
  startTime?: string
  endTime?: string
}

// ---------- 家长绑定（BR-04） ----------
export interface ParentBindingItem {
  id: number
  parentUserId: number
  parentName: string
  studentUserId: number
  studentName: string
  relation: string
  auditStatus: 'pending' | 'approved' | 'rejected'
  auditedBy?: string
  createdAt?: string
}

export const identityApi = {
  // 用户
  listUsers: (params: UserQuery) => http.get<PagedResult<UserItem>>('/users', params),
  createUser: (data: UserSavePayload) => http.post<number>('/users', data),
  updateUser: (id: number, data: Partial<UserSavePayload>) => http.put<void>(`/users/${id}`, data),
  deleteUser: (id: number) => http.delete<void>(`/users/${id}`),
  activateUser: (id: number) => http.post<void>(`/users/${id}/activate`),
  importUsers: (file: File) => http.upload<{ imported: number }>('/users/import', file),

  // 组织
  getOrgTree: () => http.get<OrgNode[]>('/orgs'),
  createOrg: (data: OrgSavePayload) => http.post<number>('/orgs', data),
  updateOrg: (id: number, data: Partial<OrgSavePayload>) => http.put<void>(`/orgs/${id}`, data),
  deleteOrg: (id: number) => http.delete<void>(`/orgs/${id}`),

  // 角色权限
  listRoles: (params?: { page?: number; pageSize?: number; keyword?: string }) =>
    http.get<RoleItem[]>('/roles', params),
  listPermissions: () => http.get<PermissionItem[]>('/roles/permissions'),
  createRole: (data: RoleSavePayload) => http.post<number>('/roles', data),
  updateRole: (id: number, data: RoleSavePayload) => http.put<void>(`/roles/${id}`, data),
  deleteRole: (id: number) => http.delete<void>(`/roles/${id}`),

  // 数据字典
  listDicts: (params?: { page?: number; pageSize?: number; keyword?: string }) =>
    http.get<Dict[]>('/dicts', params),
  createDict: (data: { code: string; name: string }) => http.post<number>('/dicts', data),
  updateDict: (id: number, data: { code: string; name: string }) => http.put<void>(`/dicts/${id}`, data),
  deleteDict: (id: number) => http.delete<void>(`/dicts/${id}`),

  // 审计日志
  listAuditLogs: (params: AuditLogQuery) => http.get<PagedResult<AuditLogItem>>('/audit-logs', params),

  // 家长绑定
  listParentBindings: (params?: { page?: number; pageSize?: number; status?: string }) =>
    http.get<PagedResult<ParentBindingItem>>('/parents/bindings', params),
  auditParentBinding: (id: number, approved: boolean, reason?: string) =>
    http.put<void>(`/parents/bindings/${id}/audit`, { approved, reason }),
}
