<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">学期与教学计划 <el-tag size="small" type="info">Edu-01</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">新增学期</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #status="{ row }"><StatusTag type="status" :value="row.status" /></template>
        <template #actions>
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="{ row }">
              <el-button v-if="row.status !== 'active'" link type="success" @click="handleActivate(row)">设为当前</el-button>
              <el-button link type="primary" @click="openDialog(row)">编辑</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="form.id ? '编辑学期' : '新增学期'" width="480px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="学期名称" prop="name">
          <el-input v-model="form.name" placeholder="如 2026 学年第一学期" />
        </el-form-item>
        <el-form-item label="学年" prop="schoolYear">
          <el-input v-model="form.schoolYear" placeholder="如 2026-2027" />
        </el-form-item>
        <el-form-item label="起止日期" prop="dateRange">
          <el-date-picker v-model="dateRange" type="daterange" range-separator="至" start-placeholder="开始" end-placeholder="结束" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item label="教学周数" prop="weekCount">
          <el-input-number v-model="form.weekCount" :min="1" :max="40" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { eduApi, type SemesterItem } from '@/api/edu'

const loading = ref(false)
const saving = ref(false)
const rows = ref<SemesterItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumn[] = [
  { prop: 'name', label: '学期名称', minWidth: 180 },
  { prop: 'schoolYear', label: '学年', width: 120 },
  { prop: 'startDate', label: '开始日期', width: 120 },
  { prop: 'endDate', label: '结束日期', width: 120 },
  { prop: 'weekCount', label: '教学周数', width: 100, align: 'center' },
  { prop: 'status', label: '状态', width: 100 },
]

const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const dateRange = ref<[string, string] | null>(null)
const form = reactive<Partial<SemesterItem>>({})

const rules: FormRules = {
  name: [{ required: true, message: '请输入学期名称', trigger: 'blur' }],
  schoolYear: [{ required: true, message: '请输入学年', trigger: 'blur' }],
}

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listSemesters(query)
    rows.value = result?.items ?? []
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

function openDialog(row?: SemesterItem) {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  if (row) {
    Object.assign(form, { id: row.id, name: row.name, schoolYear: row.schoolYear, weekCount: row.weekCount })
    dateRange.value = [row.startDate, row.endDate]
  } else {
    form.weekCount = 20
    dateRange.value = null
  }
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  if (!dateRange.value) {
    ElMessage.warning('请选择起止日期')
    return
  }
  saving.value = true
  try {
    const payload = { ...form, startDate: dateRange.value[0], endDate: dateRange.value[1] } as Omit<SemesterItem, 'id'>
    if (form.id) {
      await eduApi.updateSemester(form.id, payload)
    } else {
      await eduApi.createSemester(payload)
    }
    ElMessage.success('保存成功')
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function handleActivate(row: SemesterItem) {
  await ElMessageBox.confirm(`将「${row.name}」设为当前学期？课表、选课、成绩将以该学期为基准。`, '确认', { type: 'warning' })
  await eduApi.updateSemester(row.id, { ...row, status: 'active' })
  ElMessage.success('已设为当前学期')
  load()
}

onMounted(load)
</script>
