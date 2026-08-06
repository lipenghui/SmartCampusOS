<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">审计日志 <el-tag size="small" type="info">Common-04</el-tag></h3>
      <el-button :icon="Refresh" @click="load">刷新</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="query.keyword" placeholder="操作人 / 目标 / 详情" clearable style="width: 220px" @keyup.enter="handleSearch" />
        <el-select v-model="query.action" placeholder="操作类型" clearable style="width: 160px">
          <el-option label="登录" value="login" />
          <el-option label="审批" value="approve" />
          <el-option label="成绩修改" value="score_change" />
          <el-option label="公告发布" value="announce_publish" />
          <el-option label="权限变更" value="permission_change" />
        </el-select>
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          style="width: 260px"
        />
        <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
      </div>

      <DataTable
        :columns="columns"
        :data="rows"
        :loading="loading"
        :paged="true"
        :page="query.page"
        :page-size="query.pageSize"
        :total="total"
        row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #action="{ row }">
          <el-tag size="small" effect="plain">{{ row.action }}</el-tag>
        </template>
      </DataTable>

      <p class="text-muted" style="margin: 10px 0 0">
        关键操作（登录、审批、成绩修改、公告发布、权限变更）全量留痕，数据留存 ≥ 3 年（BR-08）。
      </p>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { Refresh, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import { identityApi, type AuditLogItem } from '@/api/identity'
import { formatDate } from '@smartcampus/shared'

const loading = ref(false)
const rows = ref<AuditLogItem[]>([])
const total = ref(0)
const dateRange = ref<[string, string] | null>(null)

const query = reactive({ page: 1, pageSize: 20, keyword: '', action: undefined as string | undefined })

const columns: DataTableColumn[] = [
  { prop: 'createdAt', label: '时间', width: 170 },
  { prop: 'userName', label: '操作人', width: 110 },
  { prop: 'action', label: '操作类型', width: 130 },
  { prop: 'targetType', label: '对象类型', width: 110 },
  { prop: 'targetId', label: '对象 ID', width: 110 },
  { prop: 'detail', label: '详情', minWidth: 220 },
  { prop: 'ip', label: 'IP', width: 130 },
]

async function load() {
  loading.value = true
  try {
    const result = await identityApi.listAuditLogs({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      action: query.action,
      startTime: dateRange.value?.[0] ? `${dateRange.value[0]} 00:00:00` : undefined,
      endTime: dateRange.value?.[1] ? `${dateRange.value[1]} 23:59:59` : undefined,
    })
    rows.value = (result?.items ?? []).map((item) => ({ ...item, createdAt: formatDate(item.createdAt) }))
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  query.page = 1
  load()
}

function handleReset() {
  query.keyword = ''
  query.action = undefined
  dateRange.value = null
  handleSearch()
}

onMounted(load)
</script>
