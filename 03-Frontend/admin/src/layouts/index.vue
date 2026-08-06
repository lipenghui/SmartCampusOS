<template>
  <div class="admin-layout">
    <Sidebar />
    <div class="admin-layout__main" :class="{ collapsed: appStore.sidebarCollapsed }">
      <HeaderBar />
      <main class="admin-layout__content">
        <router-view v-slot="{ Component }">
          <transition name="fade-slide" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import Sidebar from './components/Sidebar.vue'
import HeaderBar from './components/HeaderBar.vue'
import { useAppStore } from '@/stores/app'

const appStore = useAppStore()
</script>

<style scoped lang="scss">
.admin-layout {
  display: flex;
  height: 100%;

  &__main {
    display: flex;
    flex: 1;
    flex-direction: column;
    min-width: 0;
    margin-left: var(--scos-sidebar-width);
    transition: margin-left 0.25s;

    &.collapsed {
      margin-left: 64px;
    }
  }

  &__content {
    flex: 1;
    overflow: auto;
    padding: 14px;
  }
}

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: opacity 0.2s, transform 0.2s;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.fade-slide-leave-to {
  opacity: 0;
}
</style>
