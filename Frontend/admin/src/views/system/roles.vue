<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">角色权限 <el-tag size="small" type="info">Common-03</el-tag></h3>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="keyword" placeholder="角色名称 / 编码" clearable style="width: 220px" @keyup.enter="load" />
        <el-button type="primary" :icon="Search" @click="load">查询</el-button>
        <div class="toolbar-spacer" />
        <el-button type="primary" :icon="Plus" v-permission="'sys:role'" @click="openDialog()">新增角色</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" row-key="id">
        <template #dataScope="{ row }">
          <el-tag size="small" :type="dataScopeType(row.dataScope)">{{ dataScopeLabel(row.dataScope) }}</el-tag>
        </template>
        <template #builtin="{ row }">
          <StatusTag type="boolean" :value="row.builtin" />
        </template>
        <template #actions>
          <el-table-column label="操作" width="220" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="openPermission(row)">分配权限</el-button>
              <el-button link type="primary" @click="openDialog(row)">编辑</el-button>
              <el-button v-if="!row.builtin" link type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 新增/编辑角色 -->
    <el-dialog v-model="dialogVisible" :title="form.id ? '编辑角色' : '新增角色'" width="520px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="角色编码" prop="code">
          <el-input v-model="form.code" placeholder="如 dorm_admin / logistics" :disabled="form.id" />
        </el-form-item>
        <el-form-item label="角色名称" prop="name">
          <el-input v-model="form.name" placeholder="如 宿管 / 后勤管理员" />
        </el-form-item>
        <el-form-item label="数据范围" prop="dataScope">
          <el-select v-model="form.dataScope" style="width: 100%">
            <el-option label="全部数据（ALL）" value="all" />
            <el-option label="本年级（GRADE）" value="grade" />
            <el-option label="本班级/部门（CLASS/DEPT）" value="class" />
            <el-option label="仅本人（SELF）" value="self" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- 权限分配 -->
    <el-dialog v-model="permVisible" :title="`分配权限：${permRole?.name ?? ''}`" width="620px">
      <div v-loading="permLoading" class="perm-tree">
        <div v-for="group in permissionGroups" :key="group.module" class="perm-tree__group">
          <div class="perm-tree__module">{{ moduleLabel(group.module) }}</div>
          <el-checkbox-group v-model="checkedPerms" class="perm-tree__items">
            <el-checkbox v-for="p in group.items" :key="p.code" :value="p.code">{{ p.name }}</el-checkbox>
          </el-checkbox-group>
        </div>
      </div>
      <template #footer>
        <el-button @click="permVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSavePermission">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import StatusTag from '@/components/StatusTag.vue'
import { identityApi, type PermissionItem, type RoleItem } from '@/api/identity'
import type { DataScope } from '@smartcampus/shared'

const loading = ref(false)
const saving = ref(false)
const rows = ref<RoleItem[]>([])
const keyword = ref('')

const columns: DataTableColumn[] = [
  { prop: 'code', label: '角色编码', width: 150 },
  { prop: 'name', label: '角色名称', width: 160 },
  { prop: 'dataScope', label: '数据范围', width: 140 },
  { prop: 'builtin', label: '内置', width: 80 },
]

const dataScopeMap: Record<string, { label: string; type: 'success' | 'warning' | 'primary' | 'info' }> = {
  all: { label: '全部数据', type: 'danger' },
  grade: { label: '本年级', type: 'warning' },
  class: { label: '本班级/部门', type: 'primary' },
  dept: { label: '本部门', type: 'primary' },
  self: { label: '仅本人', type: 'info' },
}

function dataScopeLabel(scope: string): string {
  return dataScopeMap[scope]?.label ?? scope
}
function dataScopeType(scope: string) {
  return dataScopeMap[scope]?.type ?? 'info'
}

async function load() {
  loading.value = true
  try {
    const result = await identityApi.listRoles({ pageSize: 100, keyword: keyword.value || undefined })
    rows.value = result ?? []
  } finally {
    loading.value = false
  }
}

// ---------- 新增/编辑 ----------
const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const form = reactive<Partial<RoleItem>>({})

const rules: FormRules = {
  code: [{ required: true, message: '请输入角色编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  dataScope: [{ required: true, message: '请选择数据范围', trigger: 'change' }],
}

function openDialog(row?: RoleItem) {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  if (row) Object.assign(form, { id: row.id, code: row.code, name: row.name, dataScope: row.dataScope })
  else form.dataScope = 'self'
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  saving.value = true
  try {
    if (form.id) {
      await identityApi.updateRole(form.id, form as RoleItem)
      ElMessage.success('更新成功')
    } else {
      await identityApi.createRole(form as RoleItem)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function handleDelete(row: RoleItem) {
  await ElMessageBox.confirm(`确认删除角色「${row.name}」？`, '删除确认', { type: 'warning' })
  await identityApi.deleteRole(row.id)
  ElMessage.success('已删除')
  load()
}

// ---------- 权限分配 ----------
const permVisible = ref(false)
const permLoading = ref(false)
const permRole = ref<RoleItem | null>(null)
const allPermissions = ref<PermissionItem[]>([])
const checkedPerms = ref<string[]>([])

const permissionGroups = computed(() => {
  const groups = new Map<string, PermissionItem[]>()
  for (const p of allPermissions.value) {
    if (!groups.has(p.module)) groups.set(p.module, [])
    groups.get(p.module)!.push(p)
  }
  return Array.from(groups, ([module, items]) => ({ module, items }))
})

function moduleLabel(module: string): string {
  const map: Record<string, string> = {
    sys: '系统管理', edu: '教务管理', dorm: '宿舍后勤', notice: '通知家校', data: '数据可视化',
  }
  return map[module] ?? module
}

async function openPermission(row: RoleItem) {
  permRole.value = row
  permVisible.value = true
  permLoading.value = true
  checkedPerms.value = row.permissionCodes ?? []
  try {
    allPermissions.value = (await identityApi.listPermissions()) ?? []
  } finally {
    permLoading.value = false
  }
}

async function handleSavePermission() {
  if (!permRole.value) return
  saving.value = true
  try {
    await identityApi.updateRole(permRole.value.id, {
      code: permRole.value.code,
      name: permRole.value.name,
      dataScope: permRole.value.dataScope,
      permissionCodes: checkedPerms.value,
    })
    ElMessage.success('权限已保存')
    permVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
.perm-tree {
  min-height: 200px;

  &__group {
    margin-bottom: 14px;
  }

  &__module {
    font-weight: 600;
    margin-bottom: 8px;
    color: #2c3446;
  }

  &__items {
    display: flex;
    flex-wrap: wrap;
    gap: 4px 18px;
  }
}
</style>
