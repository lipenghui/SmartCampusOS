/**
 * DormService 接口层（LLD §3.4）：宿舍结构 / 入住分配 / 报修工单 / 场地预约 / 设施管理。
 */
import { http } from '@/utils/request'
import type { ApprovalStatus, PagedResult } from '@smartcampus/shared'

// ---------- 宿舍结构（Dorm-01） ----------
export interface BuildingItem {
  id: number
  name: string
  code?: string
  floors: number
  campusArea?: string
}

export interface RoomItem {
  id: number
  buildingId: number
  buildingName?: string
  floor: number
  roomNo: string
  roomType: string
  capacity: number
  status: 'available' | 'full' | 'maintenance'
}

export interface BedItem {
  id: number
  roomId: number
  bedNo: string
  status: 'free' | 'occupied' | 'maintenance' | 'disabled'
  studentName?: string
}

// ---------- 入住分配（Dorm-01/02） ----------
export interface DormAssignmentItem {
  id: number
  studentName: string
  studentNo: string
  buildingName?: string
  roomNo?: string
  bedNo?: string
  type: 'check-in' | 'transfer' | 'check-out'
  status: ApprovalStatus
  appliedAt?: string
}

// ---------- 报修工单（Dorm-03） ----------
export type RepairStatus =
  | 'pending_dispatch'
  | 'dispatched'
  | 'processing'
  | 'pending_accept'
  | 'completed'
  | 'closed'

export interface RepairOrderItem {
  id: number
  orderNo: string
  reporterName: string
  locationType: string
  locationName?: string
  category: string
  urgency: 'normal' | 'urgent'
  description: string
  status: RepairStatus
  slaDeadline?: string
  createdAt: string
  handlerName?: string
}

export interface RepairOrderQuery {
  page?: number
  pageSize?: number
  status?: RepairStatus
  category?: string
  keyword?: string
}

// ---------- 场地预约（Dorm-04） ----------
export interface VenueItem {
  id: number
  name: string
  venueType: string
  location?: string
  capacity: number
  openPeriods?: string
}

export interface VenueReservationItem {
  id: number
  venueName: string
  applicantName: string
  reserveDate: string
  periodStart: number
  periodEnd: number
  purpose: string
  status: ApprovalStatus
  noShowCount?: number
}

// ---------- 设施管理（Dorm-05） ----------
export interface MeterReadingItem {
  id: number
  roomNo: string
  meterType: 'water' | 'electricity'
  reading: number
  readingDate: string
  readerName?: string
}

export interface InspectionTaskItem {
  id: number
  title: string
  area: string
  inspectorName?: string
  status: 'pending' | 'done'
  result?: string
  dueDate?: string
}

export const dormApi = {
  // 宿舍结构
  listBuildings: (params?: { page?: number; pageSize?: number }) =>
    http.get<PagedResult<BuildingItem>>('/buildings', params),
  createBuilding: (data: Omit<BuildingItem, 'id'>) => http.post<number>('/buildings', data),
  updateBuilding: (id: number, data: Partial<BuildingItem>) => http.put<void>(`/buildings/${id}`, data),

  listRooms: (params?: { page?: number; pageSize?: number; buildingId?: number }) =>
    http.get<PagedResult<RoomItem>>('/rooms', params),
  createRoom: (data: Omit<RoomItem, 'id' | 'buildingName'>) => http.post<number>('/rooms', data),

  listBeds: (params?: { roomId?: number; status?: string }) => http.get<BedItem[]>('/beds', params),
  updateBedStatus: (id: number, status: BedItem['status']) => http.put<void>(`/beds/${id}`, { status }),

  // 入住分配
  listAssignments: (params?: { page?: number; pageSize?: number; status?: string }) =>
    http.get<PagedResult<DormAssignmentItem>>('/dorm-assignments', params),
  approveAssignment: (id: number, approved: boolean, bedId?: number) =>
    http.put<void>(`/dorm-assignments/${id}/approve`, { approved, bedId }),
  importAssignments: (file: File) => http.upload<{ assigned: number }>('/dorm-assignments/import', file),

  // 报修
  listRepairOrders: (params: RepairOrderQuery) => http.get<PagedResult<RepairOrderItem>>('/repair-orders', params),
  dispatchOrder: (id: number, handlerId?: number) => http.post<void>(`/repair-orders/${id}/dispatch`, { handlerId }),
  progressOrder: (id: number, comment: string) => http.post<void>(`/repair-orders/${id}/progress`, { comment }),
  closeOrder: (id: number, comment?: string) => http.post<void>(`/repair-orders/${id}/accept`, { comment }),

  // 场地
  listVenues: (params?: { page?: number; pageSize?: number }) =>
    http.get<PagedResult<VenueItem>>('/venues', params),
  createVenue: (data: Omit<VenueItem, 'id'>) => http.post<number>('/venues', data),
  listReservations: (params?: { page?: number; pageSize?: number; status?: string }) =>
    http.get<PagedResult<VenueReservationItem>>('/venues/reservations', params),
  approveReservation: (id: number, approved: boolean, reason?: string) =>
    http.put<void>(`/venues/reservations/${id}/approve`, { approved, reason }),

  // 设施
  listMeterReadings: (params?: { page?: number; pageSize?: number; meterType?: string }) =>
    http.get<PagedResult<MeterReadingItem>>('/meter-readings', params),
  createMeterReading: (data: Omit<MeterReadingItem, 'id'>) => http.post<number>('/meter-readings', data),
  listInspections: (params?: { page?: number; pageSize?: number; status?: string }) =>
    http.get<PagedResult<InspectionTaskItem>>('/inspections', params),
}
