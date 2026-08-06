<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">入住管理 <el-tag size="small" type="info">Dorm-01</el-tag></h3>
      <el-upload :show-file-list="false" accept=".xlsx,.xls" :http-request="handleImport" style="display: inline-block">
        <el-button :icon="Upload">新生批量导入分配</el-button>
      </el-upload>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-select v-model="status" placeholder="状态" clearable style="width: 130px" @change="load">
          <el-option label="待审批" value="pending" />
          <el-option label="已通过" value="approved" />
          <el-option label="已驳回" value="rejected" />
        </el-select>
        <el-select v-model="type" placeholder="类型" clearable style="width: 120px" @change="load">
          <el-option label="入住" value="check-in" />
          <el-option label="调宿" value="transfer" />
          <el-option label="退宿" value="check-out" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="load">查询</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #type="{ row }"><StatusTag type="assign" :value="row.type" /></template>
        <template #status="{ row }"><StatusTag type="approval" :value="row.status" /></template>
        <template #actions>
          <el-table-column label="审批" width="180" fixed="right">
            <template #default="{ row }">
              <template v-if="row.status === 'pending'">
                <el-button link type="success" @click="handleApprove(row, true)">通过</el-button>
                <el-button link type="danger" @click="handleApprove(row, false)">驳回</el-button>
              </template>
              <span v-else class="text-muted">已处理</span>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type UploadRequestOptions } from 'element-plus'
import { Search, Upload } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { dormApi, type DormAssignmentItem } from '@/api/dorm'

const loading = ref(false)
const rows = ref<DormAssignmentItem[]>([])
const total = ref(0)
const status = ref(undefined as string | undefined)
const type = ref(undefined as string | undefined)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumn[] = [
  { prop: 'studentNo', label: '学号', width: 130 },
  { prop: 'studentName', label: '姓名', width: 110 },
  { prop: 'type', label: '类型', width: 90 },
  { prop: 'buildingName', label: '楼栋', width: 120 },
  { prop: 'roomNo', label: '房间', width: 90 },
  { prop: 'bedNo', label: '床位', width: 90 },
  { prop: 'appliedAt', label: '申请时间', width: 170 },
  { prop: 'status', label: '状态', width: 100 },
]

async function load() {
  loading.value = true
  try {
    const result = await dormApi.listAssignments({ ...query, status: status.value })
    rows.value = result?.items ?? []
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

async function handleApprove(row: DormAssignmentItem, approved: boolean) {
  await ElMessageBox.confirm(`确认${approved ? '通过' : '驳回'}「${row.studentName}」的${row.type === 'check-in' ? '入住' : row.type === 'transfer' ? '调宿' : '退宿'}申请？`, '审批确认', { type: 'warning' })
  await dormApi.approveAssignment(row.id, approved)
  ElMessage.success('已处理')
  load()
}

async function handleImport(options: UploadRequestOptions) {
  const result = await dormApi.importAssignments(options.file as File)
  ElMessage.success(`批量分配完成：${result?.assigned ?? 0} 人`)
  load()
}

onMounted(load)
</script>
