<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">选课管理 <el-tag size="small" type="info">Edu-03</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">配置选课窗口</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #strategy="{ row }">
          <el-tag size="small" :type="row.strategy === 'lottery' ? 'warning' : 'primary'">
            {{ row.strategy === 'lottery' ? '志愿抽签' : '先到先得' }}
          </el-tag>
        </template>
        <template #status="{ row }"><StatusTag type="status" :value="row.status" /></template>
        <template #actions>
          <el-table-column label="操作" width="230" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="openStats(row)">选课统计</el-button>
              <el-button v-if="row.strategy === 'lottery' && row.status === 'open'" link type="warning" @click="handleLottery(row)">截止抽签</el-button>
              <el-button link type="primary" @click="openDialog(row)">编辑</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 配置窗口 -->
    <el-dialog v-model="dialogVisible" :title="form.id ? '编辑选课窗口' : '配置选课窗口'" width="560px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="窗口名称" prop="name">
          <el-input v-model="form.name" placeholder="如 2026 秋公共选修课" />
        </el-form-item>
        <el-form-item label="适用范围" prop="scope">
          <el-input v-model="form.scope" placeholder="如 2026 级全体学生 / 高二年级" />
        </el-form-item>
        <el-form-item label="学分上限" prop="creditLimit">
          <el-input-number v-model="form.creditLimit" :min="1" :max="40" />
        </el-form-item>
        <el-form-item label="选课策略" prop="strategy">
          <el-radio-group v-model="form.strategy">
            <el-radio-button value="first-come">先到先得</el-radio-button>
            <el-radio-button value="lottery">志愿抽签</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="起止时间" prop="timeRange">
          <el-date-picker v-model="timeRange" type="datetimerange" range-separator="至" start-placeholder="开始" end-placeholder="截止" value-format="YYYY-MM-DD HH:mm:ss" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- 选课统计 -->
    <el-dialog v-model="statsVisible" :title="`选课统计：${statsWindow?.name ?? ''}`" width="680px">
      <el-table :data="stats" v-loading="statsLoading" stripe max-height="420">
        <el-table-column prop="courseName" label="课程" min-width="180" />
        <el-table-column prop="teacherName" label="任课教师" width="120" />
        <el-table-column prop="capacity" label="容量" width="80" align="center" />
        <el-table-column prop="selectedCount" label="已选" width="80" align="center" />
        <el-table-column label="选课率" width="120" align="center">
          <template #default="{ row }">
            <el-progress :percentage="Math.min(100, Math.round(row.fillRate * 100))" :stroke-width="10" />
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { eduApi, type SelectionStat, type SelectionWindowItem } from '@/api/edu'

const loading = ref(false)
const saving = ref(false)
const rows = ref<SelectionWindowItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumn[] = [
  { prop: 'name', label: '窗口名称', minWidth: 180 },
  { prop: 'scope', label: '适用范围', minWidth: 150 },
  { prop: 'creditLimit', label: '学分上限', width: 100, align: 'center' },
  { prop: 'strategy', label: '选课策略', width: 110 },
  { prop: 'startAt', label: '开始时间', width: 165 },
  { prop: 'endAt', label: '截止时间', width: 165 },
  { prop: 'status', label: '状态', width: 100 },
]

const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const timeRange = ref<[string, string] | null>(null)
const form = reactive<Partial<SelectionWindowItem>>({})

const rules: FormRules = {
  name: [{ required: true, message: '请输入窗口名称', trigger: 'blur' }],
  scope: [{ required: true, message: '请输入适用范围', trigger: 'blur' }],
  creditLimit: [{ required: true, message: '请设置学分上限', trigger: 'change' }],
  strategy: [{ required: true, message: '请选择策略', trigger: 'change' }],
}

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listSelectionWindows(query)
    rows.value = result?.items ?? []
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

function openDialog(row?: SelectionWindowItem) {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  if (row) {
    Object.assign(form, { id: row.id, name: row.name, scope: row.scope, creditLimit: row.creditLimit, strategy: row.strategy })
    timeRange.value = [row.startAt, row.endAt]
  } else {
    form.strategy = 'first-come'
    form.creditLimit = 10
    timeRange.value = null
  }
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  if (!timeRange.value) {
    ElMessage.warning('请选择起止时间')
    return
  }
  saving.value = true
  try {
    const payload = { ...form, startAt: timeRange.value[0], endAt: timeRange.value[1] } as Omit<SelectionWindowItem, 'id'>
    if (form.id) {
      await eduApi.createSelectionWindow(payload)
      ElMessage.success('已更新')
    } else {
      await eduApi.createSelectionWindow(payload)
      ElMessage.success('已创建')
    }
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

// ---------- 统计 / 抽签 ----------
const statsVisible = ref(false)
const statsLoading = ref(false)
const stats = ref<SelectionStat[]>([])
const statsWindow = ref<SelectionWindowItem | null>(null)

async function openStats(row: SelectionWindowItem) {
  statsWindow.value = row
  statsVisible.value = true
  statsLoading.value = true
  try {
    stats.value = (await eduApi.getSelectionStats(row.id)) ?? []
  } finally {
    statsLoading.value = false
  }
}

async function handleLottery(row: SelectionWindowItem) {
  await ElMessageBox.confirm(`确认截止「${row.name}」并执行抽签？截止后将统一公布结果并推送通知。`, '抽签截止', { type: 'warning' })
  await eduApi.closeLottery(row.id)
  ElMessage.success('抽签完成，结果已发布')
  load()
}

onMounted(load)
</script>
