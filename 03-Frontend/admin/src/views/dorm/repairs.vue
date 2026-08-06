<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">报修工单 <el-tag size="small" type="info">Dorm-03</el-tag></h3>
      <div class="sla-hint">
        <el-tag size="small" type="info" effect="plain">SLA：一般 4h 响应 / 48h 闭环；紧急 30min 响应 / 24h 闭环</el-tag>
      </div>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="query.keyword" placeholder="工单号 / 报修人 / 地点" clearable style="width: 210px" @keyup.enter="handleSearch" />
        <el-select v-model="query.category" placeholder="类别" clearable style="width: 120px">
          <el-option label="水电" value="水电" />
          <el-option label="门窗" value="门窗" />
          <el-option label="网络" value="网络" />
          <el-option label="设备" value="设备" />
          <el-option label="其他" value="其他" />
        </el-select>
        <el-select v-model="query.urgency" placeholder="紧急程度" clearable style="width: 120px">
          <el-option label="一般" value="normal" />
          <el-option label="紧急" value="urgent" />
        </el-select>
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 130px">
          <el-option label="待派单" value="pending_dispatch" />
          <el-option label="已派单" value="dispatched" />
          <el-option label="处理中" value="processing" />
          <el-option label="待验收" value="pending_accept" />
          <el-option label="已完成" value="completed" />
          <el-option label="已关闭" value="closed" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
        <div class="toolbar-spacer" />
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #urgency="{ row }"><StatusTag type="priority" :value="row.urgency" /></template>
        <template #status="{ row }">
          <StatusTag type="repair" :value="row.status" />
          <el-tooltip v-if="isOverdue(row)" :content="`SLA 截止：${row.slaDeadline}`">
            <el-tag size="small" type="danger" effect="dark" style="margin-left: 4px">超时</el-tag>
          </el-tooltip>
        </template>
        <template #actions>
          <el-table-column label="操作" width="220" fixed="right">
            <template #default="{ row }">
              <el-button v-if="row.status === 'pending_dispatch'" link type="primary" @click="handleDispatch(row)">派单</el-button>
              <el-button v-if="row.status === 'dispatched' || row.status === 'processing'" link type="warning" @click="handleProgress(row)">进度</el-button>
              <el-button v-if="row.status === 'completed'" link type="success" @click="handleClose(row)">关闭</el-button>
              <el-button link type="primary" @click="openDetail(row)">详情</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 派单 -->
    <el-dialog v-model="dispatchVisible" title="派单" width="440px">
      <el-form label-width="80px">
        <el-form-item label="工单号">
          <span>{{ current?.orderNo }}</span>
        </el-form-item>
        <el-form-item label="维修班组">
          <el-select v-model="dispatchHandler" placeholder="选择处理人/班组" style="width: 100%">
            <el-option label="水电班组" value="water_electric" />
            <el-option label="门窗班组" value="door_window" />
            <el-option label="网络信息中心" value="network" />
            <el-option label="设备维修组" value="equipment" />
          </el-select>
        </el-form-item>
        <p class="text-muted">紧急工单 30 分钟未接单将自动升级提醒主管。</p>
      </el-form>
      <template #footer>
        <el-button @click="dispatchVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmDispatch">确认派单</el-button>
      </template>
    </el-dialog>

    <!-- 处理进度 -->
    <el-dialog v-model="progressVisible" title="补充处理进度" width="480px">
      <el-form label-width="80px">
        <el-form-item label="工单号"><span>{{ current?.orderNo }}</span></el-form-item>
        <el-form-item label="进度说明">
          <el-input v-model="progressComment" type="textarea" :rows="3" placeholder="说明处理情况，可附处理结果" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="progressVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmProgress">提交进度</el-button>
      </template>
    </el-dialog>

    <!-- 详情 -->
    <el-dialog v-model="detailVisible" title="工单详情" width="560px">
      <template v-if="current">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="工单号">{{ current.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="报修人">{{ current.reporterName }}</el-descriptions-item>
          <el-descriptions-item label="类别">{{ current.category }}</el-descriptions-item>
          <el-descriptions-item label="紧急程度">
            <StatusTag type="priority" :value="current.urgency" />
          </el-descriptions-item>
          <el-descriptions-item label="地点">{{ current.locationType }} {{ current.locationName || '' }}</el-descriptions-item>
          <el-descriptions-item label="状态"><StatusTag type="repair" :value="current.status" /></el-descriptions-item>
          <el-descriptions-item label="SLA 截止" :span="2">{{ current.slaDeadline || '-' }}</el-descriptions-item>
          <el-descriptions-item label="描述" :span="2">{{ current.description }}</el-descriptions-item>
        </el-descriptions>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { dormApi, type RepairOrderItem } from '@/api/dorm'
import { formatDate } from '@smartcampus/shared'

const loading = ref(false)
const rows = ref<RepairOrderItem[]>([])
const total = ref(0)
const query = reactive({
  page: 1, pageSize: 20, keyword: '', category: undefined as string | undefined,
  urgency: undefined as string | undefined, status: undefined as string | undefined,
})

const columns: DataTableColumn[] = [
  { prop: 'orderNo', label: '工单号', width: 160 },
  { prop: 'reporterName', label: '报修人', width: 100 },
  { prop: 'category', label: '类别', width: 90 },
  { prop: 'urgency', label: '紧急', width: 90 },
  { prop: 'locationName', label: '地点', minWidth: 130 },
  { prop: 'status', label: '状态', width: 120 },
  { prop: 'createdAt', label: '提交时间', width: 165 },
  { prop: 'slaDeadline', label: 'SLA 截止', width: 165 },
]

function isOverdue(row: RepairOrderItem): boolean {
  if (!row.slaDeadline || ['completed', 'closed'].includes(row.status)) return false
  return new Date(row.slaDeadline).getTime() < Date.now()
}

async function load() {
  loading.value = true
  try {
    const result = await dormApi.listRepairOrders(query)
    rows.value = (result?.items ?? []).map((r) => ({ ...r, createdAt: formatDate(r.createdAt), slaDeadline: formatDate(r.slaDeadline) }))
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
  query.category = undefined
  query.urgency = undefined
  query.status = undefined
  handleSearch()
}

// ---------- 操作 ----------
const current = ref<RepairOrderItem | null>(null)
const dispatchVisible = ref(false)
const dispatchHandler = ref('')
const progressVisible = ref(false)
const progressComment = ref('')
const detailVisible = ref(false)

function openDetail(row: RepairOrderItem) {
  current.value = row
  detailVisible.value = true
}

function handleDispatch(row: RepairOrderItem) {
  current.value = row
  dispatchHandler.value = ''
  dispatchVisible.value = true
}

async function confirmDispatch() {
  if (!current.value || !dispatchHandler.value) {
    ElMessage.warning('请选择维修班组')
    return
  }
  await dormApi.dispatchOrder(current.value.id, undefined)
  ElMessage.success('已派单')
  dispatchVisible.value = false
  load()
}

function handleProgress(row: RepairOrderItem) {
  current.value = row
  progressComment.value = ''
  progressVisible.value = true
}

async function confirmProgress() {
  if (!current.value) return
  await dormApi.progressOrder(current.value.id, progressComment.value || '已更新处理进度')
  ElMessage.success('进度已更新')
  progressVisible.value = false
  load()
}

async function handleClose(row: RepairOrderItem) {
  current.value = row
  await dormApi.closeOrder(row.id)
  ElMessage.success('工单已关闭')
  load()
}

onMounted(load)
</script>

<style scoped lang="scss">
.sla-hint {
  .el-tag {
    font-weight: 400;
  }
}
</style>
