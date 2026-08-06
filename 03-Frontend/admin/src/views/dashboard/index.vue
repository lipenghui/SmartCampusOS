<template>
  <div class="dashboard" v-loading="loading">
    <!-- 指标卡片（Data-01 一页式核心指标） -->
    <el-row :gutter="14" class="dashboard__cards">
      <el-col v-for="card in overview.cards" :key="card.code" :xs="12" :sm="8" :md="4">
        <div class="metric-card" @click="openDrilldown(card)">
          <div class="metric-card__name">{{ card.name }}</div>
          <div class="metric-card__value">
            {{ card.value.toLocaleString() }}<span class="metric-card__unit">{{ card.unit }}</span>
          </div>
          <div class="metric-card__trend" :class="card.trend && card.trend >= 0 ? 'up' : 'down'">
            <el-icon><CaretTop v-if="card.trend && card.trend >= 0" /><CaretBottom v-else /></el-icon>
            {{ Math.abs(card.trend ?? 0) }}% 较上期
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 时间范围 + 趋势图 -->
    <el-card shadow="never" class="dashboard__panel">
      <template #header>
        <div class="dashboard__panel-header">
          <span>数据趋势</span>
          <el-radio-group v-model="days" size="small" @change="loadTrends">
            <el-radio-button :value="7">近 7 天</el-radio-button>
            <el-radio-button :value="15">近 15 天</el-radio-button>
            <el-radio-button :value="30">近 30 天</el-radio-button>
          </el-radio-group>
        </div>
      </template>
      <el-row :gutter="14">
        <el-col :md="12">
          <EChart :option="repairTrendOption" height="300px" />
        </el-col>
        <el-col :md="12">
          <EChart :option="venueTrendOption" height="300px" />
        </el-col>
      </el-row>
    </el-card>

    <!-- 通知触达 -->
    <el-card shadow="never" class="dashboard__panel">
      <template #header><span>公告通知触达率（近 {{ days }} 天）</span></template>
      <EChart :option="receiptTrendOption" height="300px" />
    </el-card>

    <!-- 指标下钻明细 -->
    <el-dialog v-model="drilldownVisible" :title="drilldownTitle" width="720px">
      <el-table :data="drilldownItems" v-loading="drilldownLoading" stripe max-height="420">
        <el-table-column v-for="col in drilldownColumns" :key="col" :prop="col" :label="col" />
        <template #empty>该指标暂无明细数据</template>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import type { EChartsOption } from 'echarts'
import EChart from '@/components/EChart.vue'
import { dataApi, type DashboardOverview, type MetricCard, type TrendSeries } from '@/api/data'

const loading = ref(false)
const days = ref(7)
const overview = ref<DashboardOverview>({ cards: [] })
const trends = ref<TrendSeries[]>([])

const drilldownVisible = ref(false)
const drilldownLoading = ref(false)
const drilldownTitle = ref('')
const drilldownItems = ref<Record<string, unknown>[]>([])
const drilldownColumns = ref<string[]>([])

/** 各指标图表的名称映射（codes 与后端 data_metric_snapshot.metric_code 对齐）。 */
const METRIC_NAMES: Record<string, string> = {
  repair_orders: '报修工单量',
  venue_reservations: '场地预约量',
  announcements: '公告发布量',
  message_read_rate: '通知已读率',
}

async function loadOverview() {
  loading.value = true
  try {
    overview.value = await dataApi.getOverview()
  } finally {
    loading.value = false
  }
}

async function loadTrends() {
  const result = await dataApi.getTrends({ codes: ['repair_orders', 'venue_reservations', 'message_read_rate'], days: days.value })
  trends.value = result ?? []
}

function seriesOf(code: string): TrendSeries | undefined {
  return trends.value.find((t) => t.code === code)
}

function baseXAxis(points: { date: string }[]): string[] {
  return (points ?? []).map((p) => p.date)
}

const repairTrendOption = computed<EChartsOption>(() => {
  const s = seriesOf('repair_orders')
  return {
    title: { text: '报修工单趋势', textStyle: { fontSize: 14 } },
    tooltip: { trigger: 'axis' },
    grid: { left: 40, right: 16, top: 44, bottom: 28 },
    xAxis: { type: 'category', data: baseXAxis(s?.points ?? []) },
    yAxis: { type: 'value', minInterval: 1 },
    series: [{ name: METRIC_NAMES.repair_orders, type: 'line', smooth: true, data: (s?.points ?? []).map((p) => p.value), areaStyle: { opacity: 0.12 } }],
  }
})

const venueTrendOption = computed<EChartsOption>(() => {
  const s = seriesOf('venue_reservations')
  return {
    title: { text: '场地预约热度', textStyle: { fontSize: 14 } },
    tooltip: { trigger: 'axis' },
    grid: { left: 40, right: 16, top: 44, bottom: 28 },
    xAxis: { type: 'category', data: baseXAxis(s?.points ?? []) },
    yAxis: { type: 'value', minInterval: 1 },
    series: [{ name: METRIC_NAMES.venue_reservations, type: 'bar', barMaxWidth: 26, data: (s?.points ?? []).map((p) => p.value) }],
  }
})

const receiptTrendOption = computed<EChartsOption>(() => {
  const s = seriesOf('message_read_rate')
  return {
    tooltip: { trigger: 'axis', valueFormatter: (v) => `${v}%` },
    grid: { left: 40, right: 16, top: 30, bottom: 28 },
    xAxis: { type: 'category', data: baseXAxis(s?.points ?? []) },
    yAxis: { type: 'value', max: 100, axisLabel: { formatter: '{value}%' } },
    series: [{ name: METRIC_NAMES.message_read_rate, type: 'line', smooth: true, data: (s?.points ?? []).map((p) => p.value), areaStyle: { opacity: 0.15 } }],
  }
})

async function openDrilldown(card: MetricCard) {
  drilldownTitle.value = `${card.name} 明细`
  drilldownVisible.value = true
  drilldownLoading.value = true
  drilldownItems.value = []
  try {
    const result = await dataApi.drilldown(card.drilldownId ?? 0, { page: 1, pageSize: 50 })
    drilldownItems.value = result?.items ?? []
    drilldownColumns.value = drilldownItems.value.length ? Object.keys(drilldownItems.value[0]) : []
  } catch {
    drilldownItems.value = []
    drilldownColumns.value = []
    ElMessage.info('该指标暂无下钻数据')
  } finally {
    drilldownLoading.value = false
  }
}

onMounted(() => {
  loadOverview()
  loadTrends().catch(() => ElMessage.warning('趋势数据加载失败'))
})
</script>

<style scoped lang="scss">
.dashboard {
  &__cards {
    margin-bottom: 14px;
  }

  &__panel {
    margin-bottom: 14px;

    &-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-weight: 600;
    }
  }
}

.metric-card {
  padding: 14px 16px;
  margin-bottom: 14px;
  background: #fff;
  border: 1px solid #eef1f6;
  border-radius: 8px;
  cursor: pointer;
  transition: transform 0.15s, box-shadow 0.15s;

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 6px 18px rgba(31, 45, 61, 0.08);
  }

  &__name {
    font-size: 13px;
    color: #7a8699;
  }

  &__value {
    margin: 8px 0 6px;
    font-size: 26px;
    font-weight: 700;
    color: #1f2d3d;
  }

  &__unit {
    margin-left: 4px;
    font-size: 13px;
    font-weight: 400;
    color: #a2adc0;
  }

  &__trend {
    display: flex;
    align-items: center;
    gap: 2px;
    font-size: 12px;

    &.up {
      color: #16a34a;
    }

    &.down {
      color: #dc2626;
    }
  }
}
</style>
