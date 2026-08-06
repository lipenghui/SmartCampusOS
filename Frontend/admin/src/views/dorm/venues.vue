<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">场地预约 <el-tag size="small" type="info">Dorm-04</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openVenueDialog()">新增场地</el-button>
    </div>

    <el-row :gutter="14">
      <el-col :md="10">
        <el-card shadow="never">
          <template #header><span>场地列表</span></template>
          <el-table :data="venues" v-loading="venuesLoading" size="small" max-height="520">
            <el-table-column prop="name" label="场地" min-width="120" />
            <el-table-column prop="venueType" label="类型" width="100" />
            <el-table-column prop="capacity" label="容量" width="70" align="center" />
            <el-table-column prop="location" label="位置" min-width="110" />
          </el-table>
        </el-card>
      </el-col>

      <el-col :md="14">
        <el-card shadow="never">
          <template #header>
            <div class="reserve-header">
              <span>预约审批</span>
              <el-select v-model="status" placeholder="状态" clearable size="small" style="width: 120px" @change="loadReservations">
                <el-option label="待审批" value="pending" />
                <el-option label="已通过" value="approved" />
                <el-option label="已驳回" value="rejected" />
              </el-select>
            </div>
          </template>
          <el-table :data="reservations" v-loading="reservationsLoading" size="small" max-height="520">
            <el-table-column prop="venueName" label="场地" min-width="110" />
            <el-table-column prop="applicantName" label="申请人" width="90" />
            <el-table-column prop="reserveDate" label="日期" width="105" />
            <el-table-column label="时段" width="100">
              <template #default="{ row }">第 {{ row.periodStart }}-{{ row.periodEnd }} 节</template>
            </el-table-column>
            <el-table-column prop="purpose" label="用途" min-width="110" show-overflow-tooltip />
            <el-table-column label="审批" width="130" fixed="right">
              <template #default="{ row }">
                <template v-if="row.status === 'pending'">
                  <el-button link type="success" size="small" @click="handleApprove(row, true)">通过</el-button>
                  <el-button link type="danger" size="small" @click="handleApprove(row, false)">驳回</el-button>
                </template>
                <el-tag v-else size="small" :type="row.status === 'approved' ? 'success' : 'danger'">
                  {{ row.status === 'approved' ? '已通过' : '已驳回' }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <el-dialog v-model="venueVisible" title="新增场地" width="480px">
      <el-form ref="venueFormRef" :model="venueForm" :rules="venueRules" label-width="80px">
        <el-form-item label="场地名称" prop="name">
          <el-input v-model="venueForm.name" placeholder="如 体育馆 / 阶梯教室 A" />
        </el-form-item>
        <el-form-item label="场地类型" prop="venueType">
          <el-select v-model="venueForm.venueType" style="width: 100%">
            <el-option label="教室" value="教室" />
            <el-option label="会议室" value="会议室" />
            <el-option label="体育场馆" value="体育场馆" />
            <el-option label="活动场地" value="活动场地" />
          </el-select>
        </el-form-item>
        <el-form-item label="容量" prop="capacity">
          <el-input-number v-model="venueForm.capacity" :min="1" :max="1000" />
        </el-form-item>
        <el-form-item label="位置" prop="location">
          <el-input v-model="venueForm.location" placeholder="如 东区 2 号楼" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="venueVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveVenue">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import { dormApi, type VenueItem, type VenueReservationItem } from '@/api/dorm'

const venuesLoading = ref(false)
const reservationsLoading = ref(false)
const saving = ref(false)
const venues = ref<VenueItem[]>([])
const reservations = ref<VenueReservationItem[]>([])
const status = ref(undefined as string | undefined)

async function loadVenues() {
  venuesLoading.value = true
  try {
    venues.value = (await dormApi.listVenues({ pageSize: 100 }))?.items ?? []
  } finally {
    venuesLoading.value = false
  }
}

async function loadReservations() {
  reservationsLoading.value = true
  try {
    reservations.value = (await dormApi.listReservations({ pageSize: 100, status: status.value }))?.items ?? []
  } finally {
    reservationsLoading.value = false
  }
}

async function handleApprove(row: VenueReservationItem, approved: boolean) {
  await ElMessageBox.confirm(`确认${approved ? '通过' : '驳回'}「${row.applicantName}」对「${row.venueName}」的预约？`, '审批确认', { type: 'warning' })
  await dormApi.approveReservation(row.id, approved)
  ElMessage.success('已处理')
  loadReservations()
}

// ---------- 新增场地 ----------
const venueVisible = ref(false)
const venueFormRef = ref<FormInstance>()
const venueForm = reactive<Partial<VenueItem>>({})
const venueRules: FormRules = {
  name: [{ required: true, message: '请输入场地名称', trigger: 'blur' }],
  venueType: [{ required: true, message: '请选择场地类型', trigger: 'change' }],
}

function openVenueDialog() {
  Object.keys(venueForm).forEach((k) => delete (venueForm as Record<string, unknown>)[k])
  venueForm.capacity = 50
  venueVisible.value = true
}

async function saveVenue() {
  await venueFormRef.value?.validate()
  saving.value = true
  try {
    await dormApi.createVenue(venueForm as Omit<VenueItem, 'id'>)
    ElMessage.success('场地已添加')
    venueVisible.value = false
    loadVenues()
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadVenues()
  loadReservations()
})
</script>

<style scoped lang="scss">
.reserve-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
</style>
