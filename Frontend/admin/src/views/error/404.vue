<template>
  <div class="error-page">
    <div class="error-page__code">404</div>
    <p class="error-page__text">页面不存在或已被移除</p>
    <el-button type="primary" @click="$router.replace('/')">返回首页</el-button>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped lang="scss">
.error-page {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: 14px;

  &__code {
    font-size: 88px;
    font-weight: 800;
    color: var(--el-color-primary);
    line-height: 1;
  }

  &__text {
    color: #7a8699;
    font-size: 15px;
  }
}
</style>
