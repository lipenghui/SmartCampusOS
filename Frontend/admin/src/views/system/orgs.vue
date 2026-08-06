<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">组织架构 <el-tag size="small" type="info">Common-02</el-tag></h3>
    </div>

    <el-card shadow="never">
      <div class="orgs">
        <div class="orgs__tree">
          <div class="orgs__toolbar">
            <el-button type="primary" size="small" :icon="Plus" @click="openDialog(null)">新增根节点</el-button>
          </div>
          <el-tree
            :data="treeData"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            highlight-current
            default-expand-all
            @node-click="onNodeClick"
          >
            <template #default="{ data }">
              <span class="orgs__node">
                <el-icon><OfficeBuilding /></el-icon>
                <span>{{ data.name }}</span>
                <el-tag size="small" type="info" effect="plain">{{ orgTypeLabel(data.orgType) }}</el-tag>
              </span>
            </template>
          </el-tree>
        </div>

        <div class="orgs__detail">
          <el-empty v-if="!selected" description="请选择左侧组织节点" />
          <template v-else>
            <div class="orgs__detail-header">
              <span class="orgs__detail-name">{{ selected.name }}</span>
              <div>
                <el-button size="small" :icon="Plus" @click="openDialog(selected)">新增子节点</el-button>
                <el-button size="small" :icon="Edit" @click="openDialog(selected, true)">编辑</el-button>
                <el-button size="small" type="danger" :icon="Delete" @click="handleDelete(selected)">删除</el-button>
              </div>
            </div>
            <el-descriptions :column="2" border>
              <el-descriptions-item label="编码">{{ selected.code || '-' }}</el-descriptions-item>
              <el-descriptions-item label="类型">{{ orgTypeLabel(selected.orgType) }}</el-descriptions-item>
              <el-descriptions-item label="排序">{{ selected.sort ?? '-' }}</el-descriptions-item>
              <el-descriptions-item label="节点 ID">{{ selected.id }}</el-descriptions-item>
            </el-descriptions>
            <p class="text-muted" style="margin-top: 12px">
              子节点 {{ (selected.children ?? []).length }} 个：{{ (selected.children ?? []).map((c) => c.name).join('、') || '无' }}
            </p>
          </template>
        </div>
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="form.id ? '编辑组织' : '新增组织'" width="480px">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="上级" v-if="form.parentId">
          <el-input :model-value="parentName" disabled />
        </el-form-item>
        <el-form-item label="类型" prop="orgType">
          <el-select v-model="form.orgType" style="width: 100%">
            <el-option label="学校" value="school" />
            <el-option label="年级" value="grade" />
            <el-option label="班级" value="class" />
            <el-option label="部门" value="dept" />
          </el-select>
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="如：2026 级 / 高一（1）班 / 后勤处" />
        </el-form-item>
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" placeholder="如 G2026 / C2601（可选）" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="form.sort" :min="0" :max="999" />
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
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Delete, Edit, Plus } from '@element-plus/icons-vue'
import { identityApi, type OrgNode, type OrgSavePayload, type OrgType } from '@/api/identity'

const treeData = ref<OrgNode[]>([])
const selected = ref<OrgNode | null>(null)
const dialogVisible = ref(false)
const saving = ref(false)
const formRef = ref<FormInstance>()

const form = reactive<Partial<OrgSavePayload> & { id?: number }>({})

const rules: FormRules = {
  orgType: [{ required: true, message: '请选择类型', trigger: 'change' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
}

const parentName = computed(() => (form.parentId ? findNode(treeData.value, form.parentId)?.name ?? '' : ''))

function orgTypeLabel(type: string): string {
  const map: Record<string, string> = { school: '学校', grade: '年级', class: '班级', dept: '部门' }
  return map[type] ?? type
}

function findNode(nodes: OrgNode[], id: number | null): OrgNode | null {
  for (const node of nodes) {
    if (node.id === id) return node
    const found = findNode(node.children ?? [], id)
    if (found) return found
  }
  return null
}

async function load() {
  treeData.value = (await identityApi.getOrgTree()) ?? []
}

function onNodeClick(node: OrgNode) {
  selected.value = node
}

function openDialog(parent: OrgNode | null, edit = false) {
  Object.keys(form).forEach((k) => delete (form as Record<string, unknown>)[k])
  if (edit && parent) {
    Object.assign(form, { id: parent.id, parentId: parent.parentId, orgType: parent.orgType, name: parent.name, code: parent.code, sort: parent.sort })
  } else {
    form.parentId = parent?.id ?? null
    form.orgType = parent ? (parent.orgType === 'grade' ? 'class' : 'dept') : ('school' as OrgType)
    form.sort = 0
  }
  dialogVisible.value = true
}

async function handleSave() {
  await formRef.value?.validate()
  saving.value = true
  try {
    if (form.id) {
      await identityApi.updateOrg(form.id, form)
      ElMessage.success('更新成功')
    } else {
      await identityApi.createOrg(form as OrgSavePayload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    load()
  } finally {
    saving.value = false
  }
}

async function handleDelete(node: OrgNode) {
  await ElMessageBox.confirm(`确认删除组织「${node.name}」？其下所有子组织将一并删除。`, '删除确认', { type: 'warning' })
  await identityApi.deleteOrg(node.id)
  ElMessage.success('已删除')
  if (selected.value?.id === node.id) selected.value = null
  load()
}

onMounted(() => {
  load()
})
</script>

<style scoped lang="scss">
.orgs {
  display: flex;
  gap: 16px;
  min-height: 480px;

  &__tree {
    width: 320px;
    flex-shrink: 0;
    border-right: 1px solid #eef1f6;
    padding-right: 12px;
  }

  &__toolbar {
    margin-bottom: 10px;
  }

  &__node {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;

    .el-tag {
      margin-left: 4px;
    }
  }

  &__detail {
    flex: 1;
    padding: 4px 8px;
  }

  &__detail-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 14px;
  }

  &__detail-name {
    font-size: 16px;
    font-weight: 600;
  }
}
</style>
