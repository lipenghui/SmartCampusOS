/**
 * NoticeService 接口层（LLD §3.5）：公告 / 已读回执 / 消息中心 / 家校会话 / 订阅设置。
 */
import { http } from '@/utils/request'
import type { PagedResult } from '@smartcampus/shared'

// ---------- 公告（Noti-01） ----------
export type AnnouncementPriority = 'normal' | 'important' | 'urgent'

export interface AnnouncementItem {
  id: number
  title: string
  content: string
  priority: AnnouncementPriority
  sendType: 'now' | 'scheduled'
  sendAt?: string
  needReceipt: boolean
  topFlag: boolean
  status: 'draft' | 'published' | 'scheduled' | 'expired'
  publishUserName?: string
  createdAt?: string
}

export interface AnnouncementTarget {
  targetType: 'org' | 'role' | 'class' | 'tag'
  targetId: number | string
  targetName?: string
}

export interface AnnouncementSavePayload {
  title: string
  content: string
  priority: AnnouncementPriority
  sendType: 'now' | 'scheduled'
  sendAt?: string
  needReceipt: boolean
  topFlag: boolean
  targets: AnnouncementTarget[]
}

// ---------- 已读回执（Noti-03） ----------
export interface ReceiptStat {
  totalCount: number
  readCount: number
  unreadCount: number
  readRate: number
  unreadList: { userId: number; userName: string; readAt?: string }[]
}

// ---------- 消息中心（Noti-02） ----------
export type MessageType = 'announcement' | 'business' | 'system'

export interface MessageItem {
  id: number
  messageType: MessageType
  title: string
  content: string
  bizType?: string
  bizId?: number
  readFlag: boolean
  createdAt: string
}

export interface MessageQuery {
  page?: number
  pageSize?: number
  type?: MessageType
  read?: boolean
}

// ---------- 家校会话（Noti-04） ----------
export interface ConversationItem {
  id: number
  parentUserId: number
  parentName: string
  teacherUserId: number
  teacherName: string
  className?: string
  lastMessageAt?: string
  unreadCount?: number
  status: 'active' | 'closed'
}

export interface MessageReplyItem {
  id: number
  conversationId: number
  senderUserId: number
  senderName?: string
  content: string
  createdAt: string
}

// ---------- 订阅设置（Noti-05） ----------
export interface SubscriptionItem {
  id: number
  userId: number
  channelMask: number
  quietPeriod?: string
}

export const noticeApi = {
  // 公告
  listAnnouncements: (params?: { page?: number; pageSize?: number; status?: string; keyword?: string }) =>
    http.get<PagedResult<AnnouncementItem>>('/announcements', params),
  createAnnouncement: (data: AnnouncementSavePayload) => http.post<number>('/announcements', data),
  deleteAnnouncement: (id: number) => http.delete<void>(`/announcements/${id}`),
  getReceipts: (id: number) => http.get<ReceiptStat>(`/announcements/${id}/receipts`),

  // 消息中心
  listMessages: (params: MessageQuery) => http.get<PagedResult<MessageItem>>('/messages', params),
  readAllMessages: () => http.post<void>('/messages/read-all'),

  // 家校会话
  listConversations: (params?: { page?: number; pageSize?: number; keyword?: string }) =>
    http.get<PagedResult<ConversationItem>>('/conversations', params),
  listReplies: (conversationId: number) =>
    http.get<MessageReplyItem[]>(`/conversations/${conversationId}/messages`),
  sendReply: (conversationId: number, content: string) =>
    http.post<void>(`/conversations/${conversationId}/messages`, { content }),

  // 订阅设置
  getSubscription: () => http.get<SubscriptionItem>('/subscriptions/me'),
  updateSubscription: (data: Partial<SubscriptionItem>) => http.put<void>('/subscriptions', data),
}
