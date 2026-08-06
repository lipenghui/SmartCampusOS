<template>
  <div class="portal">
    <!-- 欢迎横幅 -->
    <el-card shadow="never" class="portal__hero">
      <div class="portal__hero-avatar">{{ avatarText }}</div>
      <div class="portal__hero-info">
        <h2 class="portal__hero-title">{{ userStore.displayName }}</h2>
        <p class="portal__hero-sub">欢迎使用 SmartCampusOS 智慧校园 · 学生门户</p>
        <el-tag v-for="r in userStore.roles" :key="r" size="small" class="portal__hero-tag">
          {{ roleName(r) }}
        </el-tag>
      </div>
    </el-card>

    <!-- 个人信息 -->
    <el-card shadow="never" class="portal__panel">
      <template #header><span class="portal__panel-title">我的信息</span></template>
      <el-descriptions :column="2" border>
        <el-descriptions-item label="姓名">{{ userStore.userInfo?.realName }}</el-descriptions-item>
        <el-descriptions-item label="学工号">{{ userStore.userInfo?.userNo }}</el-descriptions-item>
        <el-descriptions-item label="用户类型">{{ typeName(userStore.userType) }}</el-descriptions-item>
        <el-descriptions-item label="数据范围">{{ scopeName(userStore.dataScope) }}</el-descriptions-item>
        <el-descriptions-item label="角色">
          {{ userStore.roles.map((r) => roleName(r)).join('、') || '-' }}
        </el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 功能入口（后端业务服务落地后开放） -->
    <el-card shadow="never" class="portal__panel">
      <template #header>
        <span class="portal__panel-title">我的服务</span>
      </template>
      <el-row :gutter="14">
        <el-col v-for="item in services" :key="item.title" :xs="12" :sm="12" :md="6">
          <div class="portal__service">
            <el-icon :size="26" class="portal__service-icon"><component :is="item.icon" /></el-icon>
            <div class="portal__service-title">{{ item.title }}</div>
            <div class="portal__service-desc">{{ item.desc }}</div>
            <el-tag size="small" type="info" effect="plain">建设中</el-tag>
          </div>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Calendar, ChatDotRound, Tools, Trophy } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

/** 头像首字（无真实头像，取姓名首字符）。 */
const avatarText = computed(() => (userStore.userInfo?.realName ?? '学').slice(0, 1))

/** 内置角色码 → 中文名（对齐后端 Role 种子数据）。 */
const ROLE_NAMES: Record<string, string> = {
  admin: '系统管理员',
  teacher: '教师',
  student: '学生',
  parent: '家长',
  staff: '教职工',
  dorm_admin: '宿管',
  logistics: '后勤',
  dean: '教务',
  leader: '校领导',
  head_teacher: '班主任',
}

/** 用户类型 → 中文名（对齐 @smartcampus/shared UserType）。 */
const TYPE_NAMES: Record<string, string> = {
  student: '学生',
  teacher: '教师',
  parent: '家长',
  staff: '教职工',
}

/** 数据范围 → 中文名（对齐 LLD §8.2）。 */
const SCOPE_NAMES: Record<string, string> = {
  all: '全部数据',
  grade: '本年级',
  class: '本班级',
  dept: '本部门',
  self: '仅本人',
}

function roleName(code: string): string {
  return ROLE_NAMES[code] ?? code
}

function typeName(type: string): string {
  return TYPE_NAMES[type] ?? type
}

function scopeName(scope: string): string {
  return SCOPE_NAMES[scope] ?? scope
}

/**
 * 学生服务占位入口。对应 PRD 中学生端功能（我的课表 Edu-02 / 我的成绩 Edu-04 /
 * 我的报修 Dorm-03 / 我的消息 Noti-01），后端业务服务（EduService / DormService /
 * NoticeService）当前为脚手架，接口就绪后在此接入。
 */
const services = [
  { title: '我的课表', desc: '查看个人课表与上课安排', icon: Calendar },
  { title: '我的成绩', desc: '查看本人各科成绩与绩点', icon: Trophy },
  { title: '我的报修', desc: '报修进度与历史工单', icon: Tools },
  { title: '我的消息', desc: '班级通知与家校消息', icon: ChatDotRound },
]
</script>

<style scoped lang="scss">
.portal {
  &__hero {
    margin-bottom: 14px;
    background: linear-gradient(120deg, #2563eb 0%, #1d4ed8 100%);
    border: none;
    color: #fff;

    :deep(.el-card__body) {
      display: flex;
      align-items: center;
      gap: 18px;
      padding: 24px;
    }
  }

  &__hero-avatar {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 64px;
    height: 64px;
    flex-shrink: 0;
    font-size: 28px;
    font-weight: 700;
    color: #1d4ed8;
    background: #fff;
    border-radius: 50%;
  }

  &__hero-title {
    margin: 0 0 6px;
    font-size: 22px;
    color: #fff;
  }

  &__hero-sub {
    margin: 0 0 8px;
    font-size: 13px;
    color: rgba(255, 255, 255, 0.82);
  }

  &__hero-tag {
    margin-right: 8px;
  }

  &__panel {
    margin-bottom: 14px;

    &-title {
      font-weight: 600;
    }
  }

  &__service {
    padding: 18px 16px;
    margin-bottom: 14px;
    text-align: center;
    background: #f8fafc;
    border: 1px solid #eef1f6;
    border-radius: 8px;
    transition: transform 0.15s, box-shadow 0.15s;

    &:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 18px rgba(31, 45, 61, 0.08);
    }

    &-icon {
      color: #2563eb;
    }

    &-title {
      margin: 10px 0 4px;
      font-size: 15px;
      font-weight: 600;
      color: #1f2d3d;
    }

    &-desc {
      margin-bottom: 10px;
      font-size: 12px;
      color: #7a8699;
    }
  }
}
</style>
