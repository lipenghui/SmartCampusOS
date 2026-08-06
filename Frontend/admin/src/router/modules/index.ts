/**
 * 动态路由表：按模块分组，meta.permission 为 RBAC 权限码（LLD §2.7.2 动态路由按 RBAC 过滤）。
 * 权限码与后端 sys_permission.code 对齐（模块:功能）。
 */
import type { RouteRecordRaw } from 'vue-router'

const modules: Record<string, () => Promise<unknown>> = {
  dashboard: () => import('@/views/dashboard/index.vue'),
  // 系统管理
  'system/users': () => import('@/views/system/users.vue'),
  'system/orgs': () => import('@/views/system/orgs.vue'),
  'system/roles': () => import('@/views/system/roles.vue'),
  'system/dicts': () => import('@/views/system/dicts.vue'),
  'system/audit-logs': () => import('@/views/system/audit-logs.vue'),
  // 教务管理
  'edu/semesters': () => import('@/views/edu/semesters.vue'),
  'edu/schedules': () => import('@/views/edu/schedules.vue'),
  'edu/adjustments': () => import('@/views/edu/adjustments.vue'),
  'edu/selections': () => import('@/views/edu/selections.vue'),
  'edu/scores': () => import('@/views/edu/scores.vue'),
  'edu/exams': () => import('@/views/edu/exams.vue'),
  'edu/students': () => import('@/views/edu/students.vue'),
  // 宿舍后勤
  'dorm/buildings': () => import('@/views/dorm/buildings.vue'),
  'dorm/assignments': () => import('@/views/dorm/assignments.vue'),
  'dorm/repairs': () => import('@/views/dorm/repairs.vue'),
  'dorm/venues': () => import('@/views/dorm/venues.vue'),
  'dorm/facilities': () => import('@/views/dorm/facilities.vue'),
  // 通知家校
  'notice/announcements': () => import('@/views/notice/announcements.vue'),
  'notice/messages': () => import('@/views/notice/messages.vue'),
  'notice/conversations': () => import('@/views/notice/conversations.vue'),
  // 数据可视化
  'data/reports': () => import('@/views/data/reports.vue'),
}

export const asyncRoutes: RouteRecordRaw[] = [
  {
    path: '/dashboard',
    name: 'admin-dashboard',
    component: modules['dashboard'],
    meta: { title: '管理驾驶舱', icon: 'DataAnalysis', permission: 'data:dashboard' },
  },
  {
    path: '/system',
    name: 'admin-system',
    redirect: '/system/users',
    meta: { title: '系统管理', icon: 'Setting' },
    children: [
      { path: '/system/users', name: 'admin-system-users', component: modules['system/users'], meta: { title: '用户管理', icon: 'User', permission: 'sys:user' } },
      { path: '/system/orgs', name: 'admin-system-orgs', component: modules['system/orgs'], meta: { title: '组织架构', icon: 'OfficeBuilding', permission: 'sys:org' } },
      { path: '/system/roles', name: 'admin-system-roles', component: modules['system/roles'], meta: { title: '角色权限', icon: 'Lock', permission: 'sys:role' } },
      { path: '/system/dicts', name: 'admin-system-dicts', component: modules['system/dicts'], meta: { title: '数据字典', icon: 'Collection', permission: 'sys:dict' } },
      { path: '/system/audit-logs', name: 'admin-system-audit-logs', component: modules['system/audit-logs'], meta: { title: '审计日志', icon: 'Document', permission: 'sys:audit' } },
    ],
  },
  {
    path: '/edu',
    name: 'admin-edu',
    redirect: '/edu/semesters',
    meta: { title: '教务管理', icon: 'Reading' },
    children: [
      { path: '/edu/semesters', name: 'admin-edu-semesters', component: modules['edu/semesters'], meta: { title: '学期计划', icon: 'Calendar', permission: 'edu:semester' } },
      { path: '/edu/schedules', name: 'admin-edu-schedules', component: modules['edu/schedules'], meta: { title: '课表管理', icon: 'Grid', permission: 'edu:schedule' } },
      { path: '/edu/adjustments', name: 'admin-edu-adjustments', component: modules['edu/adjustments'], meta: { title: '调课审批', icon: 'Switch', permission: 'edu:adjust' } },
      { path: '/edu/selections', name: 'admin-edu-selections', component: modules['edu/selections'], meta: { title: '选课管理', icon: 'Checked', permission: 'edu:selection' } },
      { path: '/edu/scores', name: 'admin-edu-scores', component: modules['edu/scores'], meta: { title: '成绩管理', icon: 'Trophy', permission: 'edu:score' } },
      { path: '/edu/exams', name: 'admin-edu-exams', component: modules['edu/exams'], meta: { title: '考试管理', icon: 'Timer', permission: 'edu:exam' } },
      { path: '/edu/students', name: 'admin-edu-students', component: modules['edu/students'], meta: { title: '学籍档案', icon: 'Postcard', permission: 'edu:student' } },
    ],
  },
  {
    path: '/dorm',
    name: 'admin-dorm',
    redirect: '/dorm/buildings',
    meta: { title: '宿舍与后勤', icon: 'House' },
    children: [
      { path: '/dorm/buildings', name: 'admin-dorm-buildings', component: modules['dorm/buildings'], meta: { title: '宿舍结构', icon: 'OfficeBuilding', permission: 'dorm:building' } },
      { path: '/dorm/assignments', name: 'admin-dorm-assignments', component: modules['dorm/assignments'], meta: { title: '入住管理', icon: 'UserFilled', permission: 'dorm:assign' } },
      { path: '/dorm/repairs', name: 'admin-dorm-repairs', component: modules['dorm/repairs'], meta: { title: '报修工单', icon: 'Tools', permission: 'dorm:repair' } },
      { path: '/dorm/venues', name: 'admin-dorm-venues', component: modules['dorm/venues'], meta: { title: '场地预约', icon: 'MapLocation', permission: 'dorm:venue' } },
      { path: '/dorm/facilities', name: 'admin-dorm-facilities', component: modules['dorm/facilities'], meta: { title: '设施管理', icon: 'Memo', permission: 'dorm:facility' } },
    ],
  },
  {
    path: '/notice',
    name: 'admin-notice',
    redirect: '/notice/announcements',
    meta: { title: '通知与家校', icon: 'Bell' },
    children: [
      { path: '/notice/announcements', name: 'admin-notice-announcements', component: modules['notice/announcements'], meta: { title: '公告发布', icon: 'Promotion', permission: 'notice:announce' } },
      { path: '/notice/messages', name: 'admin-notice-messages', component: modules['notice/messages'], meta: { title: '消息中心', icon: 'Message', permission: 'notice:message' } },
      { path: '/notice/conversations', name: 'admin-notice-conversations', component: modules['notice/conversations'], meta: { title: '家校会话', icon: 'ChatDotRound', permission: 'notice:conversation' } },
    ],
  },
  {
    path: '/data',
    name: 'admin-data',
    redirect: '/data/dashboard',
    meta: { title: '数据可视化', icon: 'PieChart' },
    children: [
      { path: '/data/dashboard', name: 'admin-data-dashboard', component: modules['dashboard'], meta: { title: '管理驾驶舱', icon: 'DataAnalysis', permission: 'data:dashboard' } },
      { path: '/data/reports', name: 'admin-data-reports', component: modules['data/reports'], meta: { title: '报表中心', icon: 'Files', permission: 'data:report' } },
    ],
  },
]
