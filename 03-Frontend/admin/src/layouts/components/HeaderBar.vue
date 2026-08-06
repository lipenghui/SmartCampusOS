<template>
  <header class="header">
    <div class="header__left">
      <el-icon class="header__collapse" :size="20" @click="appStore.toggleSidebar()">
        <Expand v-if="appStore.sidebarCollapsed" />
        <Fold v-else />
      </el-icon>
      <el-breadcrumb separator="/">
        <el-breadcrumb-item v-for="item in breadcrumbs" :key="item.path">
          {{ item.title }}
        </el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <div class="header__right">
      <!-- 多角色切换（同一账号多角色，JWT 携带全部角色，LLD §8.1） -->
      <el-dropdown v-if="appStore.availableRoles.length > 1" trigger="click" @command="appStore.switchRole">
        <span class="header__role">
          <el-tag size="small" type="warning" effect="plain">
            当前角色：{{ roleLabel(appStore.currentRole) }}
          </el-tag>
        </span>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item
              v-for="role in appStore.availableRoles"
              :key="role"
              :command="role"
              :class="{ 'is-active': role === appStore.currentRole }"
            >
              {{ roleLabel(role) }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <el-dropdown trigger="click" @command="handleCommand">
        <span class="header__user">
          <el-avatar :size="30" class="header__avatar">{{ avatarText }}</el-avatar>
          <span class="header__name">{{ userStore.displayName }}</span>
          <el-icon><ArrowDown /></el-icon>
        </span>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item disabled>
              {{ userStore.userInfo?.userNo }} · {{ userTypeLabel }}
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <el-icon><SwitchButton /></el-icon>退出登录
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessageBox } from 'element-plus'
import { useAppStore } from '@/stores/app'
import { useUserStore } from '@/stores/user'
import { usePermissionStore } from '@/stores/permission'

const appStore = useAppStore()
const userStore = useUserStore()
const route = useRoute()
const router = useRouter()

const breadcrumbs = computed(() =>
  route.matched
    .filter((r) => r.meta?.title && !r.meta?.hidden)
    .map((r) => ({ path: r.path, title: r.meta!.title as string })),
)

const avatarText = computed(() => userStore.displayName.slice(0, 1).toUpperCase())

const userTypeLabel = computed(() => {
  const map: Record<string, string> = {
    student: '学生',
    teacher: '教师',
    parent: '家长',
    staff: '教职工',
  }
  return map[userStore.userInfo?.userType ?? ''] ?? '用户'
})

const roleLabels: Record<string, string> = {
  admin: '系统管理员',
  sys_admin: '系统管理员',
  leader: '校领导',
  teacher: '教师',
  dorm_admin: '宿管',
  logistics: '后勤',
  security: '安保',
}

function roleLabel(role: string): string {
  return roleLabels[role] ?? role
}

async function handleCommand(command: string | number | object) {
  if (command === 'logout') {
    await ElMessageBox.confirm('确定退出登录吗？', '提示', { type: 'warning' })
    await userStore.logout()
    // 清理动态路由，避免不同角色重新登录后路由残留
    usePermissionStore().resetRoutes()
    router.replace('/login')
  }
}
</script>

<style scoped lang="scss">
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: var(--scos-header-height);
  padding: 0 16px;
  background: #fff;
  border-bottom: 1px solid #e8ecf2;
  flex-shrink: 0;

  &__left {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  &__collapse {
    cursor: pointer;
    color: #5a6478;
  }

  &__right {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  &__role {
    cursor: pointer;
    outline: none;
  }

  &__user {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    color: #2c3446;
    outline: none;
  }

  &__avatar {
    background: var(--el-color-primary);
    color: #fff;
    font-weight: 600;
  }

  &__name {
    font-size: 14px;
  }
}
</style>
