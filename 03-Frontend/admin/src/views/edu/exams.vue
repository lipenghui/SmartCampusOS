<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">考试管理 <el-tag size="small" type="info">Edu-05</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">发布考试安排</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #supervisorNames="{ row }">
          <el-tag v-for="name in row.supervisorNames" :key="name" size="small" class="exam-tag">{{ name }}</el-tag>
          <span v-if="!row.supervisorNames?.length" class="text-muted">待分配</span>
        </template>
      </DataTable>
      <p class="text-muted" style="margin: 10px 0 0">
        发布前自动做冲突检测（同一学生同一时段多场考试将阻断）。
      </p>
    </el-card>

    <el-dialog v-model="dialogVisible" title="发布考试安排" width="560px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="课程" prop="courseName">
          <el-input v-model="form.courseName" placeholder="如 高等数学" />
        </el-form-item>
        <el-form-item label="考试日期" prop="examDate">
          <el-date-picker v-model="form.examDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item label="时间段" prop="timeRange">
          <el-time-picker v-model="timeRange" is-range range-separator="至" start-placeholder="开始" end-placeholder="结束" format="HH:mm" value-format="HH:mm:ss" style="width: 100%" />
        </el-form-item>
        <el-form-item label="考场" prop="roomName">
          <el-input v-model="form.roomName" placeholder="如 教学楼 A-201" />
        </el-form-item>
        <el-form-item label="监考教师" prop="supervisorInput">
          <el-input v-model="supervisorInput" placeholder="多个教师用逗号分隔，如 张老师, 李老师" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">发布</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import { eduApi, type ExamItem } from '@/api/edu'

const loading = ref(false)
const saving = ref(false)
const rows = ref<ExamItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumn[] = [
  { prop: 'courseName', label: '课程', minWidth: 140 },
  { prop: 'examDate', label: '考试日期', width: 120 },
  { prop: 'startTime', label: '开始', width: 90 },
  { prop: 'endTime', label: '结束', width: 90 },
  { prop: 'roomName', label: '考场', minWidth: 130 },
  { prop: 'supervisorNames', label: '监考教师', minWidth: 160 },
]

const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const timeRange = ref<[string, string] | null>(null)
const supervisorInput = ref('')
const form = reactive<Partial<ExamItem>>({})

const rules: FormRules = {
  courseName: [{ required: true, message: '请输入课程', trigger: 'blur' }],
  examDate: [{ required: true, message: '请选择考试日期', trigger: 'change' }],
  roomName: [{ required: true, message: '请输入考场', trigger: 'blur' }],
}

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listExams(query)
    rows.value = result?.items ?? []
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

function openDialog() {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  supervisorInput.value = ''
  timeRange.value = null
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  if (!timeRange.value) {
    ElMessage.warning('请选择时间段')
    return
  }
  saving.value = true
  try {
    await eduApi.createExam({
      courseName: form.courseName!,
      examDate: form.examDate!,
      startTime: timeRange.value[0],
      endTime: timeRange.value[1],
      roomName: form.roomName!,
      supervisorNames: supervisorInput.value.split(/[,，]/).map((s) => s.trim()).filter(Boolean),
    })
    ElMessage.success('考试安排已发布')
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
.exam-tag {
  margin-right: 4px;
}
</style>
