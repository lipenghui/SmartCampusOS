<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">数据字典 <el-tag size="small" type="info">通用</el-tag></h3>
    </div>

    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <el-input v-model="keyword" placeholder="字典编码 / 名称" clearable style="width: 220px" @keyup.enter="load" />
        <el-button type="primary" :icon="Search" @click="load">查询</el-button>
        <div class="toolbar-spacer" />
        <el-button type="primary" :icon="Plus" v-permission="'sys:dict'" @click="openDialog()">新增字典</el-button>
      </div>

      <DataTable :columns="columns" :data="rows" :loading="loading" row-key="id">
        <template #actions>
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="{ row }">
              <el-button link type="primary" @click="openDictItems(row)">字典项</el-button>
              <el-button link type="primary" @click="openDialog(row)">编辑</el-button>
              <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
            </template>
          </el-table-column>
        </template>
      </DataTable>
    </el-card>

    <!-- 新增/编辑字典 -->
    <el-dialog v-model="dialogVisible" :title="form.id ? '编辑字典' : '新增字典'" width="440px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" placeholder="如 repair_category" :disabled="!!form.id" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="如 报修类别" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>

    <!-- 字典项管理 -->
    <el-dialog v-model="itemsVisible" :title="`字典项：${currentDict?.name ?? ''}`" width="560px">
      <div class="dict-items">
        <el-table :data="currentDict?.items ?? []" size="small" border>
          <el-table-column prop="itemCode" label="项编码" width="160" />
          <el-table-column prop="itemName" label="项名称" min-width="140" />
          <el-table-column prop="sort" label="排序" width="80" align="center" />
          <el-table-column label="操作" width="80" align="center">
            <template #default="{ $index }">
              <el-button link type="danger" @click="removeItem($index)">移除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <div class="dict-items__add">
          <el-input v-model="newItem.itemCode" placeholder="项编码" style="width: 160px" />
          <el-input v-model="newItem.itemName" placeholder="项名称" style="width: 180px" />
          <el-input-number v-model="newItem.sort" :min="0" :max="999" />
          <el-button type="primary" :icon="Plus" @click="addItem">添加</el-button>
        </div>
      </div>
      <template #footer>
        <el-button @click="itemsVisible = false">关闭</el-button>
        <el-button type="primary" :loading="saving" @click="handleSaveItems">保存字典项</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Search } from '@element-plus/icons-vue'
import DataTable, { type DataTableColumn } from '@/components/DataTable.vue'
import { identityApi, type Dict, type DictItem } from '@/api/identity'

const loading = ref(false)
const saving = ref(false)
const rows = ref<Dict[]>([])
const keyword = ref('')

const columns: DataTableColumn[] = [
  { prop: 'code', label: '字典编码', width: 200 },
  { prop: 'name', label: '字典名称', minWidth: 200 },
  { prop: 'createdAt', label: '创建时间', width: 170 },
]

async function load() {
  loading.value = true
  try {
    const result = await identityApi.listDicts({ pageSize: 100, keyword: keyword.value || undefined })
    rows.value = result ?? []
  } finally {
    loading.value = false
  }
}

// ---------- 字典 ----------
const dialogVisible = ref(false)
const formRef = ref<FormInstance>()
const form = reactive<{ id?: number; code: string; name: string }>({ code: '', name: '' })

const rules: FormRules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
}

function openDialog(row?: Dict) {
  form.id = row?.id
  form.code = row?.code ?? ''
  form.name = row?.name ?? ''
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  saving.value = true
  try {
    if (form.id) {
      await identityApi.updateDict(form.id, { code: form.code, name: form.name })
    } else {
      await identityApi.createDict({ code: form.code, name: form.name })
    }
    ElMessage.success('保存成功')
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function handleDelete(row: Dict) {
  await ElMessageBox.confirm(`确认删除字典「${row.name}」？`, '删除确认', { type: 'warning' })
  await identityApi.deleteDict(row.id)
  ElMessage.success('已删除')
  load()
}

// ---------- 字典项 ----------
const itemsVisible = ref(false)
const currentDict = ref<Dict | null>(null)
const newItem = reactive({ itemCode: '', itemName: '', sort: 0 })

function openDictItems(row: Dict) {
  currentDict.value = { ...row, items: [...(row.items ?? [])] }
  newItem.itemCode = ''
  newItem.itemName = ''
  newItem.sort = (row.items?.length ?? 0) + 1
  itemsVisible.value = true
}

function addItem() {
  if (!newItem.itemCode || !newItem.itemName) {
    ElMessage.warning('请填写项编码与项名称')
    return
  }
  if (!currentDict.value) return
  currentDict.value.items = [...(currentDict.value.items ?? []), { ...newItem, id: 0, dictCode: currentDict.value.code }]
  newItem.itemCode = ''
  newItem.itemName = ''
  newItem.sort += 1
}

function removeItem(index: number) {
  if (!currentDict.value) return
  currentDict.value.items = (currentDict.value.items ?? []).filter((_, i) => i !== index)
}

async function handleSaveItems() {
  if (!currentDict.value) return
  saving.value = true
  try {
    await identityApi.updateDict(currentDict.value.id, {
      code: currentDict.value.code,
      name: currentDict.value.name,
    })
    ElMessage.success('字典项已保存')
    itemsVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped lang="scss">
.dict-items {
  &__add {
    display: flex;
    gap: 10px;
    margin-top: 12px;
    align-items: center;
  }
}
</style>
