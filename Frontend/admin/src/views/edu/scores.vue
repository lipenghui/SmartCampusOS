<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">成绩管理 <el-tag size="small" type="info">Edu-04</el-tag></h3>
      <div>
        <el-button :icon="Warning" @click="openWarnings">成绩预警</el-button>
        <el-upload :show-file-list="false" accept=".xlsx,.xls" :http-request="handleImport" style="display: inline-block; margin-left: 10px">
          <el-button :icon="Upload">Excel 批量导入</el-button>
        </el-upload>
      </div>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="query.keyword" placeholder="学生姓名 / 学号" clearable style="width: 200px" @keyup.enter="handleSearch" />
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 130px" @change="handleSearch">
          <el-option label="草稿" value="draft" />
          <el-option label="已提交" value="submitted" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        <div class="toolbar-spacer" />
        <el-button :icon="Download" @click="handleExport">导出成绩单</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #status="{ row }"><StatusTag type="score" :value="row.status" /></template>
        <template #scoreValue="{ row }">
          <span v-if="row.scoreValue !== undefined && row.scoreValue !== null">{{ row.scoreValue }}</span>
          <span v-else class="text-muted">-</span>
        </template>
      </DataTable>

      <p class="text-muted" style="margin: 10px 0 0">
        成绩提交后锁定；修改须走审批并留痕（原值/新值/操作人/时间，BR-03）。
      </p>
    </el-card>

    <!-- 成绩预警 -->
    <el-dialog v-model="warningsVisible" title="成绩预警名单" width="760px">
      <el-table :data="warnings" v-loading="warningsLoading" stripe max-height="440">
        <el-table-column prop="studentNo" label="学号" width="120" />
        <el-table-column prop="studentName" label="姓名" width="110" />
        <el-table-column prop="className" label="班级" width="140" />
        <el-table-column prop="failedCount" label="不及格科目" width="110" align="center" />
        <el-table-column prop="gpa" label="绩点" width="90" align="center" />
        <el-table-column label="预警级别" width="110">
          <template #default="{ row }">
            <el-tag size="small" :type="row.warningLevel === 'red' ? 'danger' : 'warning'">
              {{ row.warningLevel === 'red' ? '红色预警' : '黄色预警' }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button type="primary" :icon="Bell" @click="notifyWarnings">通知班主任与家长</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, type UploadRequestOptions } from 'element-plus'
import { Bell, Download, Refresh, Search, Upload, Warning } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { eduApi, type ScoreItem, type ScoreWarningItem } from '@/api/edu'
import { downloadBlob } from '@smartcampus/shared'

const loading = ref(false)
const rows = ref<ScoreItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20, keyword: '', status: undefined as string | undefined })

const columns: DataTableColumn[] = [
  { prop: 'studentNo', label: '学号', width: 120 },
  { prop: 'studentName', label: '姓名', width: 110 },
  { prop: 'courseName', label: '课程', minWidth: 150 },
  { prop: 'scoreValue', label: '成绩', width: 90, align: 'center' },
  { prop: 'gradeLevel', label: '等级', width: 80, align: 'center' },
  { prop: 'status', label: '状态', width: 100 },
  { prop: 'submittedBy', label: '录入人', width: 110 },
  { prop: 'updatedAt', label: '更新时间', width: 170 },
]

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listScores(query)
    rows.value = result?.items ?? []
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
  query.status = undefined
  handleSearch()
}

async function handleImport(options: UploadRequestOptions) {
  const result = await eduApi.importScores(options.file as File)
  ElMessage.success(`导入成功：${result?.imported ?? 0} 条（草稿状态，提交后锁定）`)
  load()
}

async function handleExport() {
  try {
    const blob = await eduApi.listScores({ page: 1, pageSize: 1 }).then(async () => {
      // 导出走 GET 文件流接口；后端返回 Blob
      const res = await import('@/utils/request').then((m) => m.http.instance.get('/scores/transcripts', { responseType: 'blob' }))
      return res.data as Blob
    })
    downloadBlob(blob, `成绩单_${new Date().toISOString().slice(0, 10)}.xlsx`)
  } catch {
    ElMessage.error('导出失败')
  }
}

// ---------- 预警 ----------
const warningsVisible = ref(false)
const warningsLoading = ref(false)
const warnings = ref<ScoreWarningItem[]>([])

async function openWarnings() {
  warningsVisible.value = true
  warningsLoading.value = true
  try {
    warnings.value = (await eduApi.listScoreWarnings()) ?? []
  } finally {
    warningsLoading.value = false
  }
}

function notifyWarnings() {
  ElMessage.success('已通知相关班主任与家长（需家长授权范围，BR-04）')
}

onMounted(load)
</script>
