<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">调课审批 <el-tag size="small" type="info">Edu-02</el-tag></h3>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-select v-model="status" placeholder="审批状态" clearable style="width: 130px" @change="load">
          <el-option label="待审批" value="pending" />
          <el-option label="已通过" value="approved" />
          <el-option label="已驳回" value="rejected" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="load">查询</el-button>
        <div class="toolbar-spacer" />
        <el-button type="primary" :icon="Plus" @click="openApplyDialog">发起调课申请</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #status="{ row }"><StatusTag type="approval" :value="row.status" /></template>
        <template #actions>
          <el-table-column label="审批" width="180" fixed="right">
            <template #default="{ row }">
              <template v-if="row.status === 'pending'">
                <el-button link type="success" @click="handleApprove(row, true)">通过</el-button>
                <el-button link type="danger" @click="handleApprove(row, false)">驳回</el-button>
              </template>
              <span v-else class="text-muted">{{ row.auditedBy ? `经办：${row.auditedBy}` : '-' }}</span>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 发起调课申请 -->
    <el-dialog v-model="applyVisible" title="发起调课申请" width="520px">
      <el-form ref="applyFormRef" :model="applyForm" :rules="applyRules" label-width="90px">
        <el-form-item label="原课表" prop="scheduleId">
          <el-select v-model="applyForm.scheduleId" placeholder="选择需调整的课次" style="width: 100%">
            <el-option v-for="s in scheduleOptions" :key="s.id" :label="`${s.courseName} · 周${weekdays[s.weekday - 1]} 第${s.periodStart}-${s.periodEnd}节`" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="调整目标" prop="newValue">
          <el-input v-model="applyForm.newValue" placeholder="如：周三第3-4节 → 教学楼A-201" />
        </el-form-item>
        <el-form-item label="申请原因" prop="reason">
          <el-input v-model="applyForm.reason" type="textarea" :rows="3" placeholder="说明调课原因（会议/出差/冲突等）" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="applyVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleApply">提交申请</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { eduApi, type AdjustmentItem, type ScheduleItem } from '@/api/edu'
import { formatDate } from '@smartcampus/shared'

const weekdays = ['一', '二', '三', '四', '五', '六', '日']

const loading = ref(false)
const saving = ref(false)
const rows = ref<AdjustmentItem[]>([])
const total = ref(0)
const status = ref(undefined as string | undefined)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumn[] = [
  { prop: 'applyUserName', label: '申请人', width: 110 },
  { prop: 'oldValue', label: '原安排', minWidth: 180 },
  { prop: 'newValue', label: '调整后', minWidth: 180 },
  { prop: 'reason', label: '原因', minWidth: 140 },
  { prop: 'status', label: '状态', width: 100 },
  { prop: 'createdAt', label: '申请时间', width: 170 },
]

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listAdjustments({ ...query, status: status.value })
    rows.value = (result?.items ?? []).map((r) => ({ ...r, createdAt: formatDate(r.createdAt) }))
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

async function handleApprove(row: AdjustmentItem, approved: boolean) {
  const action = approved ? '通过' : '驳回'
  await ElMessageBox.confirm(`确认${action}「${row.applyUserName}」的调课申请？${approved ? '通过后将自动更新相关方课表并推送通知。' : ''}`, `${action}确认`, { type: 'warning' })
  await eduApi.approveAdjustment(row.id, approved)
  ElMessage.success(`已${action}`)
  load()
}

// ---------- 发起申请 ----------
const applyVisible = ref(false)
const applyFormRef = ref<FormInstance>()
const scheduleOptions = ref<ScheduleItem[]>([])
const applyForm = reactive({ scheduleId: undefined as number | undefined, newValue: '', reason: '' })

const applyRules: FormRules = {
  scheduleId: [{ required: true, message: '请选择课次', trigger: 'change' }],
  newValue: [{ required: true, message: '请输入调整目标', trigger: 'blur' }],
  reason: [{ required: true, message: '请输入申请原因', trigger: 'blur' }],
}

async function openApplyDialog() {
  applyForm.scheduleId = undefined
  applyForm.newValue = ''
  applyForm.reason = ''
  scheduleOptions.value = (await eduApi.listSchedules({ viewType: 'class' }))?.items ?? []
  applyVisible.value = true
}

async function handleApply() {
  await applyFormRef.value?.validate()
  saving.value = true
  try {
    await eduApi.createAdjustment(applyForm as { scheduleId: number; newValue: string; reason: string })
    ElMessage.success('申请已提交，等待教务审批')
    applyVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>
