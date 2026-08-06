<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">用户管理 <el-tag size="small" type="info">Common-02</el-tag></h3>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="query.keyword" placeholder="学工号 / 姓名 / 手机号" clearable style="width: 220px" @keyup.enter="handleSearch" />
        <el-select v-model="query.userType" placeholder="用户类型" clearable style="width: 130px">
          <el-option label="学生" value="student" />
          <el-option label="教师" value="teacher" />
          <el-option label="家长" value="parent" />
          <el-option label="教职工" value="staff" />
        </el-select>
        <el-select v-model="query.status" placeholder="状态" clearable style="width: 120px">
          <el-option label="正常" value="active" />
          <el-option label="未激活" value="inactive" />
          <el-option label="已锁定" value="locked" />
        </el-select>
        <el-button type="primary" :icon="Search" @click="handleSearch">查询</el-button>
        <el-button :icon="Refresh" @click="handleReset">重置</el-button>
        <div class="toolbar-spacer" />
        <el-upload
          :show-file-list="false"
          accept=".xlsx,.xls"
          :http-request="handleImport"
          style="display: inline-block"
        >
          <el-button :icon="Upload">Excel 导入</el-button>
        </el-upload>
        <el-button type="primary" :icon="Plus" v-permission="'sys:user'" @click="openDialog()">新增用户</el-button>
      </div>

      <DataTable
        :columns="columns"
        :data="rows"
        :loading="loading"
        :paged="true"
        :page="query.page"
        :page-size="query.pageSize"
        :total="total"
        row-key="id"
        @update:page="(p) => { query.page = p; load() }"
        @update:pageSize="(s) => { query.pageSize = s; load() }"
      >
        <template #mobileMasked="{ row }">{{ row.mobileMasked || '-' }}</template>
        <template #userType="{ row }">{{ userTypeLabel(row.userType) }}</template>
        <template #status="{ row }"><StatusTag type="userStatus" :value="row.status" /></template>
        <template #actions>
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="openDialog(row)">编辑</el-button>
              <el-button v-if="row.status === 'inactive'" link type="success" @click="handleActivate(row)">激活</el-button>
              <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 新增/编辑 -->
    <el-dialog v-model="dialogVisible" :title="form.id ? '编辑用户' : '新增用户'" width="520px">
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="90px">
        <el-form-item label="学工号" prop="userNo">
          <el-input v-model="form.userNo" placeholder="如 S2026001 / T1002" />
        </el-form-item>
        <el-form-item label="姓名" prop="realName">
          <el-input v-model="form.realName" />
        </el-form-item>
        <el-form-item label="手机号" prop="mobile">
          <el-input v-model="form.mobile" maxlength="11" />
        </el-form-item>
        <el-form-item label="用户类型" prop="userType">
          <el-select v-model="form.userType" style="width: 100%">
            <el-option label="学生" value="student" />
            <el-option label="教师" value="teacher" />
            <el-option label="家长" value="parent" />
            <el-option label="教职工" value="staff" />
          </el-select>
        </el-form-item>
        <el-form-item label="初始密码" prop="password" v-if="!form.id">
          <el-input v-model="form.password" type="password" show-password placeholder="留空使用默认密码" />
        </el-form-item>
        <el-form-item label="所属组织" prop="orgId">
          <el-tree-select
            v-model="form.orgId"
            :data="orgTree"
            :props="{ label: 'name', value: 'id', children: 'children' }"
            check-strictly
            clearable
            style="width: 100%"
            placeholder="选择班级 / 部门"
          />
        </el-form-item>
        <el-form-item label="角色" prop="roleIds">
          <el-select v-model="form.roleIds" multiple style="width: 100%" placeholder="可多选">
            <el-option v-for="role in roleOptions" :key="role.id" :label="role.name" :value="role.id" />
          </el-select>
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules, type UploadRequestOptions } from 'element-plus'
import { Plus, Refresh, Search, Upload } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { identityApi, type OrgNode, type RoleItem, type UserItem, type UserSavePayload } from '@/api/identity'

const loading = ref(false)
const saving = ref(false)
const rows = ref<UserItem[]>([])
const total = ref(0)
const orgTree = ref<OrgNode[]>([])
const roleOptions = ref<RoleItem[]>([])

const query = reactive({ page: 1, pageSize: 20, keyword: '', userType: undefined as string | undefined, status: undefined as string | undefined })

const columns: DataTableColumn[] = [
  { prop: 'userNo', label: '学工号', width: 130 },
  { prop: 'realName', label: '姓名', width: 110 },
  { prop: 'mobileMasked', label: '手机号', width: 140 },
  { prop: 'userType', label: '类型', width: 90 },
  { prop: 'status', label: '状态', width: 90 },
  { prop: 'orgName', label: '所属组织', minWidth: 130, formatter: (row) => (row.orgName as string) || '-' },
  { prop: 'createdAt', label: '创建时间', width: 170, formatter: (row) => (row.createdAt as string) || '-' },
]

function userTypeLabel(type: string): string {
  const map: Record<string, string> = { student: '学生', teacher: '教师', parent: '家长', staff: '教职工' }
  return map[type.toLowerCase()] ?? type
}

async function load() {
  loading.value = true
  try {
    const result = await identityApi.listUsers(query)
    rows.value = result?.items ?? []
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
  query.userType = undefined
  query.status = undefined
  handleSearch()
}

// ---------- 新增/编辑 ----------
const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const form = reactive<Partial<UserSavePayload> & { id?: number }>({})

const formRules: FormRules = {
  userNo: [{ required: true, message: '请输入学工号', trigger: 'blur' }],
  realName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  userType: [{ required: true, message: '请选择用户类型', trigger: 'change' }],
}

function openDialog(row?: UserItem) {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  if (row) {
    Object.assign(form, { id: row.id, userNo: row.userNo, realName: row.realName, mobile: row.mobile, userType: row.userType })
  } else {
    form.userType = 'student'
  }
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  saving.value = true
  try {
    if (form.id) {
      await identityApi.updateUser(form.id, form)
      ElMessage.success('更新成功')
    } else {
      await identityApi.createUser(form as UserSavePayload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function handleActivate(row: UserItem) {
  await ElMessageBox.confirm(`确认激活账号「${row.realName}（${row.userNo}）」？`, '激活确认', { type: 'warning' })
  await identityApi.activateUser(row.id)
  ElMessage.success('已激活')
  load()
}

async function handleDelete(row: UserItem) {
  await ElMessageBox.confirm(`确认删除用户「${row.realName}」？该操作不可恢复。`, '删除确认', { type: 'warning' })
  await identityApi.deleteUser(row.id)
  ElMessage.success('已删除')
  load()
}

async function handleImport(options: UploadRequestOptions) {
  try {
    const result = await identityApi.importUsers(options.file as File)
    ElMessage.success(`导入成功：${result?.imported ?? 0} 条`)
    load()
  } catch {
    /* 错误提示已由请求层处理 */
  }
}

onMounted(async () => {
  load()
  try {
    const [orgs, roles] = await Promise.all([identityApi.getOrgTree(), identityApi.listRoles({ pageSize: 100 })])
    orgTree.value = orgs ?? []
    roleOptions.value = roles ?? []
  } catch {
    /* 组织/角色加载失败不影响用户列表 */
  }
})
</script>
