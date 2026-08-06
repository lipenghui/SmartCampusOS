<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">报表中心 <el-tag size="small" type="info">Data-03</el-tag></h3>
      <el-button :icon="Refresh" @click="load">刷新</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <span class="text-muted">预置报表模板，支持时间/范围/维度筛选；导出文件含水印（含操作人信息）。</span>
      </div>
      <el-table :data="rows" v-loading="loading" stripe row-key="id">
        <el-table-column prop="name" label="报表名称" min-width="180" />
        <el-table-column prop="code" label="报表编码" width="160" />
        <el-table-column prop="source" label="数据来源" width="160" />
        <el-table-column label="状态" width="110">
          <template #default="{ row }">
            <el-tag size="small" :type="row.status === 'ready' ? 'success' : 'warning'">
              {{ row.status === 'ready' ? '可用' : '生成中' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="更新时间" width="170" />
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="handleExport(row, 'excel')">导出 Excel</el-button>
            <el-button link type="primary" @click="handleExport(row, 'pdf')">导出 PDF</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh } from '@element-plus/icons-vue'
import { dataApi, type ReportItem } from '@/api/data'
import { downloadBlob, formatDate } from '@smartcampus/shared'

const loading = ref(false)
const rows = ref<ReportItem[]>([])

async function load() {
  loading.value = true
  try {
    const result = await dataApi.listReports({ pageSize: 100 })
    rows.value = (result?.items ?? []).map((r) => ({ ...r, createdAt: formatDate(r.createdAt) }))
  } finally {
    loading.value = false
  }
}

async function handleExport(row: ReportItem, format: 'excel' | 'pdf') {
  try {
    const res = await dataApi.exportReport(row.code, format)
    const blob = res.data as Blob
    const ext = format === 'excel' ? 'xlsx' : 'pdf'
    downloadBlob(blob, `${row.name}_${new Date().toISOString().slice(0, 10)}.${ext}`)
    ElMessage.success('导出任务已开始，文件将自动下载')
  } catch {
    ElMessage.error('导出失败，报表可能仍在生成中')
  }
}

onMounted(load)
</script>
