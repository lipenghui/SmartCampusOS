<template>
  <el-tag :type="config?.type ?? 'info'" :effect="config?.effect ?? 'light'" size="small">
    {{ config?.text ?? String(value ?? '-') }}
  </el-tag>
</template>

<script setup lang="ts">
import { computed } from 'vue'

/**
 * 通用状态标签：将业务状态值映射为中文文案 + Element 标签类型。
 * type 取值：approval（审批）/ repair（工单）/ priority（优先级）/ status（启停）/ userStatus / bed / assign / exam / score / inspection
 */
const props = withDefaults(
  defineProps<{
    type?: string
    value?: string | number | boolean | null
  }>(),
  { type: 'approval' },
)

interface TagConfig {
  text: string
  type: 'success' | 'info' | 'warning' | 'danger' | 'primary'
  effect?: 'light' | 'plain' | 'dark'
}

const maps: Record<string, Record<string, TagConfig>> = {
  approval: {
    pending: { text: '待审批', type: 'warning' },
    approved: { text: '已通过', type: 'success' },
    rejected: { text: '已驳回', type: 'danger' },
  },
  repair: {
    pending_dispatch: { text: '待派单', type: 'info' },
    dispatched: { text: '已派单', type: 'primary' },
    processing: { text: '处理中', type: 'warning' },
    pending_accept: { text: '待验收', type: 'warning' },
    completed: { text: '已完成', type: 'success' },
    closed: { text: '已关闭', type: 'info', effect: 'plain' },
  },
  priority: {
    normal: { text: '普通', type: 'info' },
    important: { text: '重要', type: 'warning' },
    urgent: { text: '紧急', type: 'danger' },
  },
  status: {
    enabled: { text: '启用', type: 'success' },
    disabled: { text: '停用', type: 'info' },
    active: { text: '启用', type: 'success' },
    inactive: { text: '停用', type: 'info' },
    locked: { text: '锁定', type: 'danger' },
    draft: { text: '草稿', type: 'info' },
    published: { text: '已发布', type: 'success' },
    scheduled: { text: '定时待发', type: 'warning' },
    open: { text: '进行中', type: 'success' },
    closed: { text: '已结束', type: 'info' },
    expired: { text: '已过期', type: 'info' },
  },
  userStatus: {
    active: { text: '正常', type: 'success' },
    inactive: { text: '未激活', type: 'info' },
    locked: { text: '已锁定', type: 'danger' },
  },
  bed: {
    free: { text: '空闲', type: 'success' },
    occupied: { text: '已入住', type: 'primary' },
    maintenance: { text: '维修中', type: 'warning' },
    disabled: { text: '停用', type: 'info' },
  },
  assign: {
    'check-in': { text: '入住', type: 'primary' },
    transfer: { text: '调宿', type: 'warning' },
    'check-out': { text: '退宿', type: 'info' },
  },
  exam: {
    studying: { text: '在读', type: 'success' },
    suspended: { text: '休学', type: 'warning' },
    graduated: { text: '毕业', type: 'info' },
  },
  score: {
    draft: { text: '草稿', type: 'info' },
    submitted: { text: '已提交', type: 'success' },
  },
  inspection: {
    pending: { text: '待巡检', type: 'warning' },
    done: { text: '已完成', type: 'success' },
  },
  boolean: {
    true: { text: '是', type: 'success' },
    false: { text: '否', type: 'info' },
  },
}

const key = computed(() => String(props.value ?? '').toLowerCase())

const config = computed<TagConfig | undefined>(() => maps[props.type]?.[key.value])
</script>
