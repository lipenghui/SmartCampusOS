<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">设施管理 <el-tag size="small" type="info">Dorm-05</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openReadingDialog()">新增抄表</el-button>
    </div>

    <el-tabs v-model="activeTab" class="facility-tabs">
      <!-- 水电抄表 -->
      <el-tab-pane label="水电抄表" name="meter">
        <el-card shadow="never" class="table-card">
          <div class="toolbar">
            <el-select v-model="meterQuery.meterType" placeholder="类型" clearable style="width: 120px" @change="loadMeters">
              <el-option label="水表" value="water" />
              <el-option label="电表" value="electricity" />
            </el-select>
            <el-button type="primary" :icon="Search" @click="loadMeters">查询</el-button>
          </div>
          <DataTable :columns="meterColumns" :data="meterRows" :loading="meterLoading" :paged="true" :page="meterQuery.page" :page-size="meterQuery.pageSize" :total="meterTotal" row-key="id"
            @update:page="(p) => { meterQuery.page = p; loadMeters() }"
            @update:pageSize="(s) => { meterQuery.pageSize = s; loadMeters() }"
          >
            <template #meterType="{ row }">
              <el-tag size="small" :type="row.meterType === 'water' ? 'primary' : 'warning'">
                {{ row.meterType === 'water' ? '水表' : '电表' }}
              </el-tag>
            </template>
          </DataTable>
        </el-card>
      </el-tab-pane>

      <!-- 设施巡检 -->
      <el-tab-pane label="设施巡检" name="inspection">
        <el-card shadow="never" class="table-card">
          <DataTable :columns="inspectionColumns" :data="inspectionRows" :loading="inspectionLoading" :paged="true" :page="inspectionQuery.page" :page-size="inspectionQuery.pageSize" :total="inspectionTotal" row-key="id"
            @update:page="(p) => { inspectionQuery.page = p; loadInspections() }"
            @update:pageSize="(s) => { inspectionQuery.pageSize = s; loadInspections() }"
          >
            <template #status="{ row }"><StatusTag type="inspection" :value="row.status" /></template>
          </DataTable>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <!-- 新增抄表 -->
    <el-dialog v-model="readingVisible" title="新增抄表记录" width="460px">
      <el-form ref="readingFormRef" :model="readingForm" :rules="readingRules" label-width="90px">
        <el-form-item label="房间号" prop="roomNo">
          <el-input v-model="readingForm.roomNo" placeholder="如 3-301" />
        </el-form-item>
        <el-form-item label="类型" prop="meterType">
          <el-radio-group v-model="readingForm.meterType">
            <el-radio-button value="water">水表</el-radio-button>
            <el-radio-button value="electricity">电表</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="读数" prop="reading">
          <el-input-number v-model="readingForm.reading" :min="0" :precision="2" />
        </el-form-item>
        <el-form-item label="抄表日期" prop="readingDate">
          <el-date-picker v-model="readingForm.readingDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="readingVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveReading">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { dormApi, type InspectionTaskItem, type MeterReadingItem } from '@/api/dorm'

const activeTab = ref('meter')

// ---------- 抄表 ----------
const meterLoading = ref(false)
const meterRows = ref<MeterReadingItem[]>([])
const meterTotal = ref(0)
const meterQuery = reactive({ page: 1, pageSize: 20, meterType: undefined as string | undefined })

const meterColumns: DataTableColumn[] = [
  { prop: 'roomNo', label: '房间', width: 120 },
  { prop: 'meterType', label: '类型', width: 100 },
  { prop: 'reading', label: '读数', width: 120, align: 'right' },
  { prop: 'readingDate', label: '抄表日期', width: 130 },
  { prop: 'readerName', label: '抄表人', width: 110 },
]

async function loadMeters() {
  meterLoading.value = true
  try {
    const result = await dormApi.listMeterReadings(meterQuery)
    meterRows.value = result?.items ?? []
    meterTotal.value = result?.total ?? 0
  } finally {
    meterLoading.value = false
  }
}

// ---------- 巡检 ----------
const inspectionLoading = ref(false)
const inspectionRows = ref<InspectionTaskItem[]>([])
const inspectionTotal = ref(0)
const inspectionQuery = reactive({ page: 1, pageSize: 20 })

const inspectionColumns: DataTableColumn[] = [
  { prop: 'title', label: '巡检任务', minWidth: 160 },
  { prop: 'area', label: '区域', width: 130 },
  { prop: 'inspectorName', label: '巡检人', width: 110 },
  { prop: 'dueDate', label: '计划日期', width: 130 },
  { prop: 'status', label: '状态', width: 100 },
  { prop: 'result', label: '结果', minWidth: 140 },
]

async function loadInspections() {
  inspectionLoading.value = true
  try {
    const result = await dormApi.listInspections(inspectionQuery)
    inspectionRows.value = result?.items ?? []
    inspectionTotal.value = result?.total ?? 0
  } finally {
    inspectionLoading.value = false
  }
}

// ---------- 新增抄表 ----------
const saving = ref(false)
const readingVisible = ref(false)
const readingFormRef = ref<FormInstance>()
const readingForm = reactive<Partial<MeterReadingItem>>({})

const readingRules: FormRules = {
  roomNo: [{ required: true, message: '请输入房间号', trigger: 'blur' }],
  meterType: [{ required: true, message: '请选择类型', trigger: 'change' }],
  reading: [{ required: true, message: '请输入读数', trigger: 'change' }],
}

function openReadingDialog() {
  Object.keys(readingForm).forEach((k) => delete (readingForm as Record<string, unknown>)[k])
  readingForm.meterType = 'water'
  readingForm.reading = 0
  readingVisible.value = true
}

async function saveReading() {
  await readingFormRef.value?.validate()
  saving.value = true
  try {
    await dormApi.createMeterReading(readingForm as Omit<MeterReadingItem, 'id'>)
    ElMessage.success('抄表记录已保存')
    readingVisible.value = false
    loadMeters()
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadMeters()
  loadInspections()
})
</script>

<style scoped lang="scss">
.facility-tabs {
  :deep(.el-tabs__header) {
    margin-bottom: 4px;
  }
}
</style>
