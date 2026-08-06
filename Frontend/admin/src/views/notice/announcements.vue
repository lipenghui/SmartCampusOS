<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">公告发布 <el-tag size="small" type="info">Noti-01 / Noti-03</el-tag></h3>
      <el-button type="primary" :icon="Plus" @click="openDialog()">发布公告</el-button>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="keyword" placeholder="公告标题" clearable style="width: 210px" @keyup.enter="load" />
        <el-select v-model="priority" placeholder="优先级" clearable style="width: 120px" @change="load">
          <el-option label="普通" value="normal" />
          <el-option label="重要" value="important" />
          <el-option label="紧急" value="urgent" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="load">查询</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" :paged="true" :page="query.page" :page-size="query.pageSize" :total="total" row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #priority="{ row }"><StatusTag type="priority" :value="row.priority" /></template>
        <template #status="{ row }"><StatusTag type="status" :value="row.status" /></template>
        <template #needReceipt="{ row }"><StatusTag type="boolean" :value="row.needReceipt" /></template>
        <template #topFlag="{ row }">
          <el-tag v-if="row.topFlag" size="small" type="danger" effect="dark">置顶</el-tag>
        </template>
        <template #actions>
          <el-table-column label="操作" width="160" fixed="right">
            <template #default="{ row }">
              <el-button v-if="row.needReceipt" link type="primary" @click="openReceipts(row)">已读统计</el-button>
              <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 发布表单 -->
    <el-dialog v-model="dialogVisible" title="发布公告" width="620px" top="6vh">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="标题" prop="title">
          <el-input v-model="form.title" placeholder="公告标题" maxlength="60" />
        </el-form-item>
        <el-form-item label="正文" prop="content">
          <el-input v-model="form.content" type="textarea" :rows="5" placeholder="公告正文（支持富文本，此处简版）" />
        </el-form-item>
        <el-form-item label="优先级" prop="priority">
          <el-radio-group v-model="form.priority">
            <el-radio-button value="normal">普通</el-radio-button>
            <el-radio-button value="important">重要</el-radio-button>
            <el-radio-button value="urgent">紧急</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="接收范围" prop="targets">
          <el-checkbox-group v-model="targetTypes" class="announce-targets">
            <el-checkbox value="school">全校</el-checkbox>
            <el-checkbox value="grade">按年级</el-checkbox>
            <el-checkbox value="class">按班级</el-checkbox>
            <el-checkbox value="dept">按部门</el-checkbox>
          </el-checkbox-group>
        </el-form-item>
        <el-form-item label="发送时间" prop="sendType">
          <el-radio-group v-model="form.sendType">
            <el-radio-button value="now">立即发送</el-radio-button>
            <el-radio-button value="scheduled">定时发送</el-radio-button>
          </el-radio-group>
          <el-date-picker
            v-if="form.sendType === 'scheduled'"
            v-model="form.sendAt"
            type="datetime"
            placeholder="选择发送时间"
            value-format="YYYY-MM-DD HH:mm:ss"
            style="margin-left: 12px; width: 220px"
          />
        </el-form-item>
        <el-form-item label="回执与置顶">
          <el-checkbox v-model="form.needReceipt">需要已读回执</el-checkbox>
          <el-checkbox v-model="form.topFlag" style="margin-left: 16px">置顶显示</el-checkbox>
        </el-form-item>
        <p class="text-muted">
          「紧急」通知强制全渠道推送（App + 短信，短信受预算额度控制 BR-05）；敏感通知仅推送给本人或授权家长。
        </p>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">发布</el-button>
      </template>
    </el-dialog>

    <!-- 已读统计 -->
    <el-dialog v-model="receiptsVisible" :title="`已读统计：${receiptsTitle}`" width="640px">
      <template v-if="receipts">
        <el-row :gutter="12" class="receipt-summary">
          <el-col :span="8"><div class="receipt-summary__item"><span class="stat-number">{{ receipts.totalCount }}</span> 接收人</div></el-col>
          <el-col :span="8"><div class="receipt-summary__item"><span class="stat-number" style="color: #16a34a">{{ receipts.readCount }}</span> 已读</div></el-col>
          <el-col :span="8"><div class="receipt-summary__item"><span class="stat-number" style="color: #dc2626">{{ receipts.unreadCount }}</span> 未读</div></el-col>
        </el-row>
        <el-table :data="receipts.unreadList" size="small" max-height="320" style="margin-top: 12px">
          <el-table-column prop="userName" label="未读人员" />
          <el-table-column label="备注" width="200">
            <template #default>
              <span class="text-muted">未读（自动提醒）</span>
            </template>
          </el-table-column>
        </el-table>
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
import { noticeApi, type AnnouncementItem, type AnnouncementSavePayload, type ReceiptStat } from '@/api/notice'

const loading = ref(false)
const saving = ref(false)
const rows = ref<AnnouncementItem[]>([])
const total = ref(0)
const keyword = ref('')
const priority = ref(undefined as string | undefined)
const query = reactive({ page: 1, pageSize: 20 })

const columns: DataTableColumn[] = [
  { prop: 'title', label: '标题', minWidth: 200 },
  { prop: 'priority', label: '优先级', width: 90 },
  { prop: 'topFlag', label: '置顶', width: 80 },
  { prop: 'needReceipt', label: '回执', width: 80 },
  { prop: 'publishUserName', label: '发布人', width: 110 },
  { prop: 'sendAt', label: '发送时间', width: 165 },
  { prop: 'status', label: '状态', width: 100 },
]

async function load() {
  loading.value = true
  try {
    const result = await noticeApi.listAnnouncements({ ...query, keyword: keyword.value || undefined, status: priority.value })
    rows.value = result?.items ?? []
    total.value = result?.total ?? 0
  } finally {
    loading.value = false
  }
}

// ---------- 发布 ----------
const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const targetTypes = ref<string[]>([])
const form = reactive<Partial<AnnouncementSavePayload>>({})

const rules: FormRules = {
  title: [{ required: true, message: '请输入标题', trigger: 'blur' }],
  content: [{ required: true, message: '请输入正文', trigger: 'blur' }],
  priority: [{ required: true, message: '请选择优先级', trigger: 'change' }],
}

function openDialog() {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  form.priority = 'normal'
  form.sendType = 'now'
  form.needReceipt = true
  form.topFlag = false
  targetTypes.value = []
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  if (form.sendType === 'scheduled' && !form.sendAt) {
    ElMessage.warning('请选择定时发送时间')
    return
  }
  saving.value = true
  try {
    await noticeApi.createAnnouncement({
      title: form.title!,
      content: form.content!,
      priority: form.priority as AnnouncementSavePayload['priority'],
      sendType: form.sendType as AnnouncementSavePayload['sendType'],
      sendAt: form.sendAt,
      needReceipt: !!form.needReceipt,
      topFlag: !!form.topFlag,
      targets: targetTypes.value.map((t) => ({ targetType: t as 'org', targetId: 0, targetName: t })),
    })
    ElMessage.success('公告已发布')
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function handleDelete(row: AnnouncementItem) {
  await ElMessageBox.confirm(`确认删除公告「${row.title}」？`, '删除确认', { type: 'warning' })
  await noticeApi.deleteAnnouncement(row.id)
  ElMessage.success('已删除')
  load()
}

// ---------- 已读统计 ----------
const receiptsVisible = ref(false)
const receiptsTitle = ref('')
const receipts = ref<ReceiptStat | null>(null)

async function openReceipts(row: AnnouncementItem) {
  receiptsTitle.value = row.title
  receiptsVisible.value = true
  receipts.value = (await noticeApi.getReceipts(row.id)) ?? null
}

onMounted(load)
</script>

<style scoped lang="scss">
.announce-targets {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 18px;
}

.receipt-summary {
  &__item {
    text-align: center;
    padding: 12px 0;
    background: #f7f9fc;
    border-radius: 6px;
    font-size: 13px;
    color: #5a6478;
  }
}
</style>
