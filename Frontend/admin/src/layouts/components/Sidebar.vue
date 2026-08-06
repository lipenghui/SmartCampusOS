<template>
  <aside class="sidebar" :class="{ collapsed: appStore.sidebarCollapsed }">
    <div class="sidebar__logo">
      <img src="/favicon.svg" alt="logo" class="sidebar__logo-icon" />
      <span v-show="!appStore.sidebarCollapsed" class="sidebar__logo-title">SmartCampusOS</span>
    </div>
    <el-scrollbar class="sidebar__scroll">
      <el-menu
        :default-active="activeMenu"
        :collapse="appStore.sidebarCollapsed"
        :collapse-transition="false"
        router
        background-color="transparent"
        text-color="#a6b3cf"
        active-text-color="#ffffff"
        class="sidebar__menu"
      >
        <template v-for="route in menuRoutes" :key="route.path">
          <el-sub-menu v-if="visibleChildren(route).length" :index="route.path">
            <template #title>
              <el-icon v-if="route.meta?.icon"><component :is="route.meta.icon" /></el-icon>
              <span>{{ route.meta?.title }}</span>
            </template>
            <el-menu-item v-for="child in visibleChildren(route)" :key="child.path" :index="child.path">
              <el-icon v-if="child.meta?.icon"><component :is="child.meta.icon" /></el-icon>
              <template #title>{{ child.meta?.title }}</template>
            </el-menu-item>
          </el-sub-menu>
          <el-menu-item v-else :index="route.path">
            <el-icon v-if="route.meta?.icon"><component :is="route.meta.icon" /></el-icon>
            <template #title>{{ route.meta?.title }}</template>
          </el-menu-item>
        </template>
      </el-menu>
    </el-scrollbar>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAppStore } from '@/stores/app'
import { usePermissionStore } from '@/stores/permission'

const appStore = useAppStore()
const permissionStore = usePermissionStore()
const route = useRoute()

const menuRoutes = computed(() => permissionStore.menuRoutes)

const activeMenu = computed(() => route.path)

/** 分组下可显示的叶子菜单（过滤 hidden 与无权限项）。 */
function visibleChildren(route: RouteRecordRaw): RouteRecordRaw[] {
  return (route.children ?? []).filter((c) => !c.meta?.hidden)
}
</script>

<style scoped lang="scss">
.sidebar {
  position: fixed;
  top: 0;
  bottom: 0;
  left: 0;
  z-index: 100;
  display: flex;
  flex-direction: column;
  width: var(--scos-sidebar-width);
  background: var(--scos-sidebar-bg);
  transition: width 0.25s;

  &.collapsed {
    width: 64px;
  }

  &__logo {
    display: flex;
    align-items: center;
    gap: 10px;
    height: var(--scos-header-height);
    padding: 0 16px;
    color: #fff;
    white-space: nowrap;
    overflow: hidden;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }

  &__logo-icon {
    width: 30px;
    height: 30px;
    flex-shrink: 0;
  }

  &__logo-title {
    font-size: 16px;
    font-weight: 700;
    letter-spacing: 0.5px;
  }

  &__scroll {
    flex: 1;
  }

  &__menu {
    border-right: none;
    --el-menu-bg-color: transparent;
    --el-menu-hover-bg-color: rgba(37, 99, 235, 0.18);
    --el-menu-active-color: #ffffff;

    :deep(.el-menu-item.is-active) {
      background: var(--el-color-primary);
    }
  }
}
</style>
