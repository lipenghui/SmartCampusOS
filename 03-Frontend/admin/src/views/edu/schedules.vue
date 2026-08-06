<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">课表管理 <el-tag size="small" type="info">Edu-02</el-tag></h3>
      <el-button :icon="MagicStick" :loading="generating" @click="handleGenerate">自动排课</el-button>
    </div>

    <el-card shadow="never">
      <div class="toolbar">
        <el-select v-model="query.viewType" style="width: 130px" @change="handleSearch">
          <el-option label="按班级" value="class" />
          <el-option label="按教师" value="teacher" />
          <el-option label="按教室" value="room" />
        </el-select>
        <el-select v-model="query.targetId" placeholder="选择目标" filterable clearable style="width: 220px">
          <el-option v-for="t in targetOptions" :key="t.id" :label="t.label" :value="t.id" />
        </el-select>
        <el-select v-model="query.weekday" placeholder="星期" clearable style="width: 120px">
          <el-option v-for="(d, i) in weekdays" :key="i" :label="d" :value="i + 1" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
        <div class="toolbar-spacer" />
        <el-button :icon="Download">导出 iCal / 图片</el-button>
      </div>

      <!-- 周视图：行=节次，列=星期 -->
      <el-table :data="gridRows" v-loading="loading" border>
        <el-table-column label="节次" width="120" align="center" fixed="left">
          <template #default="{ row }">第 {{ row.period }} 节</template>
        </el-table-column>
        <el-table-column v-for="(day, i) in weekdays" :key="day" :label="day" min-width="150">
          <template #default="{ row }">
            <div v-for="item in cellItems(row.period, i + 1)" :key="item.id" class="schedule-cell">
              <div class="schedule-cell__course">{{ item.courseName }}</div>
              <div class="schedule-cell__meta">{{ item.roomName }} · {{ item.teacherName }}</div>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Download, MagicStick, Search } from '@element-plus/icons-vue'
import { eduApi, type ScheduleItem } from '@/api/edu'

const weekdays = ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
const PERIODS = 12

const loading = ref(false)
const generating = ref(false)
const schedules = ref<ScheduleItem[]>([])
const targetOptions = ref<{ id: number; label: string }[]>([])

const query = reactive({
  viewType: 'class' as 'class' | 'teacher' | 'room',
  targetId: undefined as number | undefined,
  weekday: undefined as number | undefined,
})

const gridRows = computed(() => Array.from({ length: PERIODS }, (_, i) => ({ period: i + 1 })))

function cellItems(period: number, weekday: number): ScheduleItem[] {
  return schedules.value.filter(
    (s) => s.periodStart <= period && s.periodEnd >= period && s.weekday === weekday,
  )
}

async function load() {
  loading.value = true
  try {
    const result = await eduApi.listSchedules({ viewType: query.viewType, targetId: query.targetId, weekday: query.weekday })
    schedules.value = result?.items ?? []
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  load()
}

async function handleGenerate() {
  await ElMessageBox.confirm(
    '自动排课将按教师时间、教室容量、课程性质等约束生成，并做冲突检测（教师/教室/班级时间冲突会阻断）。确认开始？',
    '自动排课',
    { type: 'warning' },
  )
  generating.value = true
  try {
    const result = await eduApi.generateSchedules({ semesterId: 0 })
    ElMessage.success(`排课完成，生成 ${result?.generated ?? 0} 条课表记录`)
    load()
  } finally {
    generating.value = false
  }
}

onMounted(() => {
  load()
})
</script>

<style scoped lang="scss">
.schedule-cell {
  padding: 4px 6px;
  border-radius: 4px;
  background: var(--el-color-primary-light-9);
  border: 1px solid var(--el-color-primary-light-7);
  margin-bottom: 4px;
  cursor: pointer;

  &__course {
    font-size: 13px;
    font-weight: 600;
    color: var(--el-color-primary);
  }

  &__meta {
    font-size: 12px;
    color: #7a8699;
    margin-top: 2px;
  }
}
</style>
