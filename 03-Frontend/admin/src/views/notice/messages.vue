<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">消息中心 <el-tag size="small" type="info">Noti-02</el-tag></h3>
      <el-button :icon="Check" :disabled="!unreadCount" @click="handleReadAll">全部已读</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-radio-group v-model="query.type" @change="handleSearch">
          <el-radio-button value="">全部</el-radio-button>
          <el-radio-button value="announcement">公告</el-radio-button>
          <el-radio-button value="business">业务</el-radio-button>
          <el-radio-button value="system">系统</el-radio-button>
        </el-radio-group>
        <el-radio-group v-model="readFilter" @change="handleSearch" style="margin-left: 14px">
          <el-radio-button :value="undefined">全部状态</el-radio-button>
          <el-radio-button :value="false">未读</el-radio-button>
          <el-radio-button :value="true">已读</el-radio-button>
        </el-radio-group>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #messageType="{ row }">
          <el-tag size="small" :type="typeTag(row.messageType)">{{ typeLabel(row.messageType) }}</el-tag>
        </template>
        <template #readFlag="{ row }">
          <el-tag v-if="row.readFlag" size="small" type="info" effect="plain">已读</el-tag>
          <el-tag v-else size="small" type="danger" effect="dark">未读</el-tag>
        </template>
        <template #actions>
          <el-table-column label="操作" width="120" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="openDetail(row)">查看</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>

      <p class="text-muted" style="margin: 10px 0 0">
        消息按「未读/已读」「全部/公告/业务/系统」筛选；已读回执类消息不可删除（仅可标记已读）。
      </p>
    </el-card>

    <el-dialog v-model="detailVisible" :title="current?.title ?? '消息详情'" width="560px">
      <template v-if="current">
        <div class="message-meta">
          <el-tag size="small" :type="typeTag(current.messageType)">{{ typeLabel(current.messageType) }}</el-tag>
          <span class="text-muted" style="margin-left: 10px">{{ current.createdAt }}</span>
        </div>
        <p class="message-content">{{ current.content }}</p>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Check } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import { noticeApi, type MessageItem, type MessageType } from '@/api/notice'
import { formatDate } from '@smartcampus/shared'

const loading = ref(false)
const rows = ref<MessageItem[]>([])
const total = ref(0)
const readFilter = ref<boolean | undefined>(undefined)
const query = reactive({ page: 1, pageSize: 20, type: '' as '' | MessageType })

const unreadCount = computed(() => rows.value.filter((m) => !m.readFlag).length)

const columns: DataTableColumn[] = [
  { prop: 'messageType', label: '类型', width: 90 },
  { prop: 'title', label: '标题', minWidth: 220 },
  { prop: 'content', label: '内容摘要', minWidth: 200, ellipsis: true },
  { prop: 'readFlag', label: '状态', width: 90 },
  { prop: 'createdAt', label: '时间', width: 170 },
]

function typeLabel(type: string): string {
  const map: Record<string, string> = { announcement: '公告', business: '业务', system: '系统' }
  return map[type] ?? type
}

function typeTag(type: string) {
  const map: Record<string, 'primary' | 'success' | 'info'> = { announcement: 'primary', business: 'success', system: 'info' }
  return map[type] ?? 'info'
}

async function load() {
  loading.value = true
  try {
    const result = await noticeApi.listMessages({
      page: query.page,
      pageSize: query.pageSize,
      type: query.type || undefined,
      read: readFilter.value,
    })
    rows.value = (result?.items ?? []).map((m) => ({ ...m, createdAt: formatDate(m.createdAt) }))
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  query.page = 1
  load()
}

async function handleReadAll() {
  await noticeApi.readAllMessages()
  ElMessage.success('已全部标记为已读')
  load()
}

const detailVisible = ref(false)
const current = ref<MessageItem | null>(null)

function openDetail(row: MessageItem) {
  current.value = row
  detailVisible.value = true
}

onMounted(load)
</script>

<style scoped lang="scss">
.message-meta {
  margin-bottom: 12px;
}

.message-content {
  line-height: 1.8;
  color: #2c3446;
  white-space: pre-wrap;
}
</style>
