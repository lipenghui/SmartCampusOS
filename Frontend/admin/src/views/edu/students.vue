<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">学籍档案 <el-tag size="small" type="info">Edu-06</el-tag></h3>
      <el-button :icon="Download">导出名单</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="query.keyword" placeholder="学生姓名 / 学号" clearable style="width: 200px" @keyup.enter="handleSearch" />
        <el-select v-model="query.status" placeholder="学籍状态" clearable style="width: 130px" @change="handleSearch">
          <el-option label="在读" value="studying" />
          <el-option label="休学" value="suspended" />
          <el-option label="毕业" value="graduated" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #status="{ row }"><StatusTag type="exam" :value="row.status" /></template>
        <template #actions>
          <el-table-column label="操作" width="150" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="openDetail(row)">档案详情</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 档案详情（含异动记录） -->
    <el-dialog v-model="detailVisible" :title="`学籍档案：${detail?.studentName ?? ''}`" width="560px">
      <template v-if="detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="学号">{{ detail.studentNo }}</el-descriptions-item>
          <el-descriptions-item label="姓名">{{ detail.studentName }}</el-descriptions-item>
          <el-descriptions-item label="班级">{{ detail.className || '-' }}</el-descriptions-item>
          <el-descriptions-item label="年级">{{ detail.gradeName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="入学日期">{{ detail.enrollDate || '-' }}</el-descriptions-item>
          <el-descriptions-item label="学籍状态">
            <StatusTag type="exam" :value="detail.status" />
          </el-descriptions-item>
        </el-descriptions>
        <p class="text-muted" style="margin-top: 14px">
          学籍异动（转班 / 休学 / 复学）记录见后端接口 edu_transfer_record；异动期间过渡期不超过 3 个工作日（BR-01）。
        </p>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { Download, Refresh, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { eduApi, type StudentProfileItem } from '@/api/edu'

const loading = ref(false)
const rows = ref<StudentProfileItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20, keyword: '', status: undefined as string | undefined })

const columns: DataTableColumn[] = [
  { prop: 'studentNo', label: '学号', width: 130 },
  { prop: 'studentName', label: '姓名', width: 120 },
  { prop: 'className', label: '班级', width: 150 },
  { prop: 'gradeName', label: '年级', width: 130 },
  { prop: 'enrollDate', label: '入学日期', width: 120 },
  { prop: 'status', label: '学籍状态', width: 110 },
]

const detailVisible = ref(false)
const detail = ref<StudentProfileItem | null>(null)

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listStudentProfiles(query)
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

function openDetail(row: StudentProfileItem) {
  detail.value = row
  detailVisible.value = true
}

onMounted(load)
</script>
