<template>
  <div class="login">
    <div class="login__panel">
      <div class="login__brand">
        <img src="/favicon.svg" alt="logo" class="login__logo" />
        <h1 class="login__title">SmartCampusOS 智慧校园管理端</h1>
        <p class="login__subtitle">让校园管理有据可依，让家校沟通畅通无阻</p>
      </div>

      <el-form ref="formRef" :model="form" :rules="rules" size="large" @keyup.enter="handleLogin">
        <el-radio-group v-model="form.grantType" class="login__tabs">
          <el-radio-button value="password">账号密码</el-radio-button>
          <el-radio-button value="captcha">手机验证码</el-radio-button>
        </el-radio-group>

        <el-form-item prop="userNo" v-if="form.grantType === 'password'">
          <el-input v-model="form.userNo" placeholder="学工号" :prefix-icon="User" clearable />
        </el-form-item>
        <el-form-item prop="password" v-if="form.grantType === 'password'">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="密码"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>

        <el-form-item prop="mobile" v-if="form.grantType === 'captcha'">
          <el-input v-model="form.mobile" placeholder="手机号" :prefix-icon="Iphone" clearable />
        </el-form-item>
        <el-form-item prop="code" v-if="form.grantType === 'captcha'">
          <div class="login__captcha">
            <el-input v-model="form.code" placeholder="验证码" :prefix-icon="Key" />
            <el-button :disabled="countdown > 0" @click="handleSendCaptcha">
              {{ countdown > 0 ? `${countdown}s 后重发` : '获取验证码' }}
            </el-button>
          </div>
        </el-form-item>

        <el-button type="primary" size="large" class="login__submit" :loading="loading" @click="handleLogin">
          登 录
        </el-button>
      </el-form>

      <p class="login__footer">统一账号 · 多端协同 · RBAC 权限管理</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Iphone, Key, Lock, User } from '@element-plus/icons-vue'
import { useUserStore } from '@/stores/user'
import { authApi } from '@/api/auth'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const formRef = ref<FormInstance>()
const loading = ref(false)
const countdown = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

const form = reactive({
  grantType: 'password' as 'password' | 'captcha',
  userNo: '',
  password: '',
  mobile: '',
  code: '',
})

const rules: FormRules = {
  userNo: [{ required: true, message: '请输入学工号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
  mobile: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
}

async function handleLogin() {
  await formRef.value?.validate()
  loading.value = true
  try {
    await userStore.login({
      grantType: form.grantType,
      userNo: form.userNo || undefined,
      password: form.password || undefined,
      mobile: form.mobile || undefined,
      code: form.code || undefined,
    })
    ElMessage.success('登录成功')
    const redirect = (route.query.redirect as string) || '/'
    router.replace(redirect)
  } finally {
    loading.value = false
  }
}

async function handleSendCaptcha() {
  if (!form.mobile) {
    ElMessage.warning('请先输入手机号')
    return
  }
  await authApi.sendCaptcha(form.mobile)
  ElMessage.success('验证码已发送')
  countdown.value = 60
  timer = setInterval(() => {
    countdown.value -= 1
    if (countdown.value <= 0 && timer) clearInterval(timer)
  }, 1000)
}

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped lang="scss">
.login {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  background:
    radial-gradient(1200px 600px at 20% 10%, rgba(37, 99, 235, 0.35), transparent 60%),
    radial-gradient(900px 500px at 85% 90%, rgba(14, 165, 233, 0.3), transparent 55%),
    linear-gradient(135deg, #0f1b33 0%, #16294d 55%, #1d3a6b 100%);

  &__panel {
    width: 400px;
    padding: 36px 40px 26px;
    background: rgba(255, 255, 255, 0.97);
    border-radius: 12px;
    box-shadow: 0 24px 60px rgba(6, 18, 45, 0.45);
  }

  &__brand {
    text-align: center;
    margin-bottom: 26px;
  }

  &__logo {
    width: 52px;
    height: 52px;
  }

  &__title {
    margin: 12px 0 6px;
    font-size: 20px;
    color: #16294d;
  }

  &__subtitle {
    margin: 0;
    font-size: 13px;
    color: #7a8699;
  }

  &__tabs {
    display: flex;
    width: 100%;
    margin-bottom: 20px;

    :deep(.el-radio-button) {
      flex: 1;

      .el-radio-button__inner {
        width: 100%;
      }
    }
  }

  &__captcha {
    display: flex;
    width: 100%;
    gap: 10px;

    .el-input {
      flex: 1;
    }
  }

  &__submit {
    width: 100%;
    margin-top: 6px;
  }

  &__footer {
    margin: 18px 0 0;
    text-align: center;
    font-size: 12px;
    color: #a2adc0;
  }
}
</style>
