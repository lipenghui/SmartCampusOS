<template>
  <el-table
    :data="data"
    v-loading="loading"
    stripe
    :empty-text="emptyText"
    :row-key="rowKey"
    :default-sort="defaultSort"
    @sort-change="onSortChange"
  >
    <el-table-column
      v-for="col in columns"
      :key="col.key ?? col.prop"
      :prop="col.prop"
      :label="col.label"
      :width="col.width"
      :min-width="col.minWidth"
      :align="col.align"
      :sortable="col.sortable"
      :fixed="col.fixed"
      :show-overflow-tooltip="col.ellipsis !== false && !$slots[col.prop ?? '']"
    >
      <template #default="scope">
        <slot v-if="$slots[col.prop ?? '']" :name="col.prop" :row="scope.row" :index="scope.$index" />
        <span v-else-if="col.formatter">{{ col.formatter(scope.row) }}</span>
        <span v-else>{{ scope.row[col.prop ?? ''] }}</span>
      </template>
    </el-table-column>
    <!-- 操作列等由父组件通过具名插槽注入；必须作为默认插槽普通内容与 v-for 列共存，
         不能包在 <template #default> 中（会覆盖上面的列定义导致表格无列不渲染） -->
    <slot name="actions" />
  </el-table>

  <div v-if="paged" class="data-table__pagination">
    <el-pagination
      :current-page="page"
      :page-size="pageSize"
      :page-sizes="[10, 20, 50, 100]"
      :total="total"
      layout="total, sizes, prev, pager, next, jumper"
      background
      @update:current-page="onPageChange"
      @update:page-size="onSizeChange"
    />
  </div>
</template>

<script setup lang="ts">
export interface DataTableColumn {
  key?: string
  prop?: string
  label: string
  width?: number | string
  minWidth?: number | string
  align?: 'left' | 'center' | 'right'
  sortable?: boolean | 'custom'
  fixed?: boolean | 'left' | 'right'
  /** 自定义格式化（无插槽时使用）。 */
  formatter?: (row: Record<string, unknown>) => string | number
  /** 是否省略显示（默认 true，有插槽自动关闭）。 */
  ellipsis?: boolean
}

const props = withDefaults(
  defineProps<{
    columns: DataTableColumn[]
    data: Record<string, unknown>[]
    loading?: boolean
    emptyText?: string
    paged?: boolean
    page?: number
    pageSize?: number
    total?: number
    rowKey?: string
    defaultSort?: { prop: string; order: 'ascending' | 'descending' }
  }>(),
  {
    loading: false,
    emptyText: '暂无数据',
    paged: false,
    page: 1,
    pageSize: 20,
    total: 0,
  },
)

const emit = defineEmits<{
  (e: 'update:page', page: number): void
  (e: 'update:pageSize', size: number): void
  (e: 'sort-change', payload: { prop: string; order: string }): void
}>()

function onPageChange(page: number) {
  emit('update:page', page)
}

function onSizeChange(size: number) {
  emit('update:pageSize', size)
  emit('update:page', 1)
}

function onSortChange({ prop, order }: { prop: string; order: string | null }) {
  if (order) emit('sort-change', { prop, order })
}
</script>

<style scoped lang="scss">
.data-table__pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 14px;
}
</style>
