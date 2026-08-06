<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">宿舍结构 <el-tag size="small" type="info">Dorm-01</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openBuildingDialog()">新增楼栋</el-button>
    </div>

    <el-row :gutter="14">
      <!-- 楼栋 -->
      <el-col :md="8">
        <el-card shadow="never">
          <template #header><span>楼栋（{{ buildings.length }}）</span></template>
          <div v-loading="loading">
            <div v-for="b in buildings" :key="b.id" class="building-item" :class="{ active: currentBuilding?.id === b.id }" @click="selectBuilding(b)">
              <div class="building-item__name">{{ b.name }}</div>
              <div class="building-item__meta">{{ b.code }} · {{ b.floors }} 层 · {{ b.campusArea || '本部' }}</div>
              <el-button link type="primary" size="small" @click.stop="openBuildingDialog(b)">编辑</el-button>
            </div>
            <el-empty v-if="!buildings.length" description="暂无楼栋" :image-size="60" />
          </div>
        </el-card>
      </el-col>

      <!-- 房间 -->
      <el-col :md="8">
        <el-card shadow="never">
          <template #header>
            <span>房间（{{ currentBuilding?.name ?? '-' }}）</span>
          </template>
          <div class="toolbar">
            <el-select v-model="floorFilter" placeholder="楼层" clearable style="width: 90px">
              <el-option v-for="f in floors" :key="f" :label="`${f} 层`" :value="f" />
            </el-select>
            <el-button size="small" type="primary" :icon="Plus" @click="openRoomDialog()">新增房间</el-button>
          </div>
          <el-table :data="filteredRooms" v-loading="loading" size="small" max-height="480">
            <el-table-column prop="roomNo" label="房间" width="90" />
            <el-table-column prop="floor" label="楼层" width="70" align="center" />
            <el-table-column prop="roomType" label="类型" width="90" />
            <el-table-column prop="capacity" label="容量" width="70" align="center" />
            <el-table-column label="操作" width="70">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openBedDialog(row)">床位</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <!-- 床位 -->
      <el-col :md="8">
        <el-card shadow="never">
          <template #header><span>床位（{{ currentRoom?.roomNo ?? '-' }}）</span></template>
          <div class="beds" v-loading="loading">
            <div
              v-for="bed in beds"
              :key="bed.id"
              class="bed-item"
              :class="`bed-item--${bed.status}`"
              :title="bed.studentName ? `已入住：${bed.studentName}` : bed.status"
            >
              <div class="bed-item__no">{{ bed.bedNo }}</div>
              <div class="bed-item__name">{{ bed.studentName || bedStatusLabel(bed.status) }}</div>
            </div>
            <el-empty v-if="!beds.length" description="请选择房间查看床位" :image-size="60" />
          </div>
          <p class="text-muted">床位状态：空闲 / 已入住 / 维修中 / 停用（Dorm-01）</p>
        </el-card>
      </el-col>
    </el-row>

    <!-- 楼栋表单 -->
    <el-dialog v-model="buildingVisible" :title="buildingForm.id ? '编辑楼栋' : '新增楼栋'" width="460px">
      <el-form ref="buildingFormRef" :model="buildingForm" :rules="buildingRules" label-width="80px">
        <el-form-item label="楼栋名称" prop="name">
          <el-input v-model="buildingForm.name" placeholder="如 1 号学生公寓" />
        </el-form-item>
        <el-form-item label="楼栋编码" prop="code">
          <el-input v-model="buildingForm.code" placeholder="如 B1" />
        </el-form-item>
        <el-form-item label="楼层数" prop="floors">
          <el-input-number v-model="buildingForm.floors" :min="1" :max="30" />
        </el-form-item>
        <el-form-item label="校区区域" prop="campusArea">
          <el-input v-model="buildingForm.campusArea" placeholder="如 东校区" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="buildingVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveBuilding">保存</el-button>
      </template>
    </el-dialog>

    <!-- 房间表单 -->
    <el-dialog v-model="roomVisible" title="新增房间" width="460px">
      <el-form ref="roomFormRef" :model="roomForm" :rules="roomRules" label-width="80px">
        <el-form-item label="楼栋" prop="buildingId">
          <el-select v-model="roomForm.buildingId" style="width: 100%">
            <el-option v-for="b in buildings" :key="b.id" :label="b.name" :value="b.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="楼层" prop="floor">
          <el-input-number v-model="roomForm.floor" :min="1" :max="30" />
        </el-form-item>
        <el-form-item label="房间号" prop="roomNo">
          <el-input v-model="roomForm.roomNo" placeholder="如 301" />
        </el-form-item>
        <el-form-item label="房型" prop="roomType">
          <el-input v-model="roomForm.roomType" placeholder="如 四人间" />
        </el-form-item>
        <el-form-item label="容量" prop="capacity">
          <el-input-number v-model="roomForm.capacity" :min="1" :max="16" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roomVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveRoom">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import { dormApi, type BedItem, type BuildingItem, type RoomItem } from '@/api/dorm'

const loading = ref(false)
const saving = ref(false)

const buildings = ref<BuildingItem[]>([])
const rooms = ref<RoomItem[]>([])
const beds = ref<BedItem[]>([])
const currentBuilding = ref<BuildingItem | null>(null)
const currentRoom = ref<RoomItem | null>(null)
const floorFilter = ref<number | undefined>(undefined)

const floors = computed(() => {
  const max = currentBuilding.value?.floors ?? 0
  return Array.from({ length: max }, (_, i) => i + 1)
})

const filteredRooms = computed(() =>
  floorFilter.value ? rooms.value.filter((r) => r.floor === floorFilter.value) : rooms.value,
)

function bedStatusLabel(status: string): string {
  const map: Record<string, string> = { free: '空闲', occupied: '已入住', maintenance: '维修中', disabled: '停用' }
  return map[status] ?? status
}

async function loadBuildings() {
  loading.value = true
  try {
    buildings.value = (await dormApi.listBuildings({ pageSize: 100 }))?.items ?? []
  } finally {
    loading.value = false
  }
}

async function selectBuilding(b: BuildingItem) {
  currentBuilding.value = b
  currentRoom.value = null
  beds.value = []
  loading.value = true
  try {
    rooms.value = (await dormApi.listRooms({ pageSize: 200, buildingId: b.id }))?.items ?? []
  } finally {
    loading.value = false
  }
}

async function openBedDialog(room: RoomItem) {
  currentRoom.value = room
  loading.value = true
  try {
    beds.value = (await dormApi.listBeds({ roomId: room.id })) ?? []
  } finally {
    loading.value = false
  }
}

// ---------- 楼栋 ----------
const buildingVisible = ref(false)
const buildingFormRef = ref<FormInstance>()
const buildingForm = reactive<Partial<BuildingItem>>({})
const buildingRules: FormRules = {
  name: [{ required: true, message: '请输入楼栋名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入楼栋编码', trigger: 'blur' }],
}

function openBuildingDialog(b?: BuildingItem) {
  Object.keys(buildingForm).forEach((k) => delete (buildingForm as Record<string, unknown>)[k])
  if (b) Object.assign(buildingForm, { id: b.id, name: b.name, code: b.code, floors: b.floors, campusArea: b.campusArea })
  else buildingForm.floors = 6
  buildingVisible.value = true
}

async function saveBuilding() {
  await buildingFormRef.value?.validate()
  saving.value = true
  try {
    if (buildingForm.id) {
      await dormApi.updateBuilding(buildingForm.id, buildingForm)
    } else {
      await dormApi.createBuilding(buildingForm as Omit<BuildingItem, 'id'>)
    }
    ElMessage.success('保存成功')
    buildingVisible.value = false
    loadBuildings()
  } finally {
    saving.value = false
  }
}

// ---------- 房间 ----------
const roomVisible = ref(false)
const roomFormRef = ref<FormInstance>()
const roomForm = reactive<Partial<RoomItem>>({})
const roomRules: FormRules = {
  buildingId: [{ required: true, message: '请选择楼栋', trigger: 'change' }],
  roomNo: [{ required: true, message: '请输入房间号', trigger: 'blur' }],
}

function openRoomDialog() {
  Object.keys(roomForm).forEach((k) => delete (roomForm as Record<string, unknown>)[k])
  roomForm.buildingId = currentBuilding.value?.id
  roomForm.floor = 1
  roomForm.capacity = 4
  roomVisible.value = true
}

async function saveRoom() {
  await roomFormRef.value?.validate()
  saving.value = true
  try {
    await dormApi.createRoom(roomForm as Omit<RoomItem, 'id' | 'buildingName'>)
    ElMessage.success('保存成功')
    roomVisible.value = false
    if (currentBuilding.value) selectBuilding(currentBuilding.value)
  } finally {
    saving.value = false
  }
}

onMounted(loadBuildings)
</script>

<style scoped lang="scss">
.building-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  margin-bottom: 8px;
  border: 1px solid #eef1f6;
  border-radius: 6px;
  cursor: pointer;

  &.active {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
  }

  &__name {
    font-weight: 600;
    font-size: 14px;
  }

  &__meta {
    flex: 1;
    font-size: 12px;
    color: #7a8699;
  }
}

.beds {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  min-height: 120px;
  margin-bottom: 10px;
}

.bed-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 88px;
  height: 62px;
  border-radius: 6px;
  border: 1px solid;
  font-size: 12px;

  &--free {
    border-color: #86efac;
    background: #f0fdf4;
    color: #16a34a;
  }

  &--occupied {
    border-color: #93c5fd;
    background: #eff6ff;
    color: #2563eb;
  }

  &--maintenance {
    border-color: #fcd34d;
    background: #fefce8;
    color: #ca8a04;
  }

  &--disabled {
    border-color: #d1d5db;
    background: #f3f4f6;
    color: #9ca3af;
  }

  &__no {
    font-weight: 700;
  }
}
</style>
