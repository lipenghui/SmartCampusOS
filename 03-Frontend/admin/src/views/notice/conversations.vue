<template>
  <div class="page-container">
    <div class="page-header">
      <h3 class="page-title">家校会话 <el-tag size="small" type="info">Noti-04</el-tag></h3>
      <el-tag size="small" type="warning" effect="plain">私信与评论过敏感词过滤 + 审计留痕</el-tag>
    </div>

    <el-card shadow="never">
      <div class="conversations">
        <!-- 会话列表 -->
        <div class="conversations__list">
          <div class="toolbar" style="margin-bottom: 10px">
            <el-input v-model="keyword" placeholder="家长 / 教师姓名" clearable size="small" style="width: 100%" @keyup.enter="load" />
          </div>
          <div v-loading="loading" class="conversations__scroll">
            <div
              v-for="c in rows"
              :key="c.id"
              class="conversation-item"
              :class="{ active: current?.id === c.id }"
              @click="openConversation(c)"
            >
              <div class="conversation-item__head">
                <span class="conversation-item__name">{{ c.parentName }} ↔ {{ c.teacherName }}</span>
                <span class="text-muted">{{ c.lastMessageAt ? formatDay(c.lastMessageAt) : '' }}</span>
              </div>
              <div class="conversation-item__meta">
                {{ c.className || '未绑定班级' }}
                <el-tag v-if="c.unreadCount" size="small" type="danger" style="margin-left: 6px">{{ c.unreadCount }} 未读</el-tag>
              </div>
            </div>
            <el-empty v-if="!rows.length" description="暂无会话" :image-size="60" />
          </div>
        </div>

        <!-- 聊天窗口 -->
        <div class="conversations__chat">
          <template v-if="current">
            <div class="chat-header">
              <span>{{ current.parentName }}（家长） ↔ {{ current.teacherName }}（教师）</span>
              <el-tag v-if="current.status === 'closed'" size="small" type="info">会话已关闭</el-tag>
            </div>
            <div ref="chatBody" class="chat-body" v-loading="repliesLoading">
              <div v-for="r in replies" :key="r.id" class="chat-msg" :class="{ mine: r.senderUserId === current.teacherUserId }">
                <div class="chat-msg__bubble">
                  <div class="chat-msg__name">
                    {{ r.senderName || (r.senderUserId === current.teacherUserId ? current.teacherName : current.parentName) }}
                  </div>
                  <div class="chat-msg__content">{{ r.content }}</div>
                  <div class="chat-msg__time">{{ formatDate(r.createdAt) }}</div>
                </div>
              </div>
              <el-empty v-if="!replies.length" description="暂无消息，发送第一条吧" :image-size="60" />
            </div>
            <div class="chat-input">
              <el-input v-model="draft" type="textarea" :rows="2" placeholder="输入消息（将进行敏感词过滤）" :disabled="current.status === 'closed'" />
              <el-button type="primary" :loading="sending" :disabled="!draft.trim()" @click="sendMessage">发送</el-button>
            </div>
          </template>
          <el-empty v-else description="请选择左侧会话" />
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { nextTick, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { noticeApi, type ConversationItem, type MessageReplyItem } from '@/api/notice'
import { formatDate, formatDay } from '@smartcampus/shared'

const loading = ref(false)
const repliesLoading = ref(false)
const sending = ref(false)
const rows = ref<ConversationItem[]>([])
const replies = ref<MessageReplyItem[]>([])
const current = ref<ConversationItem | null>(null)
const keyword = ref('')
const draft = ref('')
const chatBody = ref<HTMLDivElement>()

const query = reactive({ page: 1, pageSize: 50 })

async function load() {
  loading.value = true
  try {
    const result = await noticeApi.listConversations({ ...query, keyword: keyword.value || undefined })
    rows.value = result?.items ?? []
  } finally {
    loading.value = false
  }
}

async function openConversation(c: ConversationItem) {
  current.value = c
  replies.value = []
  repliesLoading.value = true
  try {
    replies.value = (await noticeApi.listReplies(c.id)) ?? []
    scrollToBottom()
  } finally {
    repliesLoading.value = false
  }
}

async function sendMessage() {
  if (!current.value || !draft.value.trim()) return
  sending.value = true
  try {
    await noticeApi.sendReply(current.value.id, draft.value.trim())
    draft.value = ''
    replies.value = (await noticeApi.listReplies(current.value.id)) ?? []
    scrollToBottom()
  } catch {
    /* 敏感词拦截等错误由请求层提示 */
  } finally {
    sending.value = false
  }
}

function scrollToBottom() {
  nextTick(() => {
    if (chatBody.value) chatBody.value.scrollTop = chatBody.value.scrollHeight
  })
}

onMounted(load)
</script>

<style scoped lang="scss">
.conversations {
  display: flex;
  gap: 16px;
  height: 560px;

  &__list {
    width: 300px;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
  }

  &__scroll {
    flex: 1;
    overflow: auto;
  }

  &__chat {
    flex: 1;
    display: flex;
    flex-direction: column;
    border-left: 1px solid #eef1f6;
    padding-left: 16px;
    min-width: 0;
  }
}

.conversation-item {
  padding: 10px 12px;
  border: 1px solid #eef1f6;
  border-radius: 6px;
  margin-bottom: 8px;
  cursor: pointer;

  &.active {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
  }

  &__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 13px;
  }

  &__name {
    font-weight: 600;
  }

  &__meta {
    margin-top: 4px;
    font-size: 12px;
    color: #7a8699;
  }
}

.chat-header {
  padding-bottom: 10px;
  border-bottom: 1px solid #eef1f6;
  font-weight: 600;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.chat-body {
  flex: 1;
  overflow: auto;
  padding: 14px 4px;
}

.chat-msg {
  display: flex;
  margin-bottom: 12px;

  &.mine {
    justify-content: flex-end;

    .chat-msg__bubble {
      background: var(--el-color-primary);
      color: #fff;
    }
  }

  &__bubble {
    max-width: 72%;
    padding: 8px 12px;
    background: #f3f5f9;
    border-radius: 8px;
  }

  &__name {
    font-size: 12px;
    color: #7a8699;
    margin-bottom: 4px;
  }

  &__content {
    font-size: 14px;
    line-height: 1.6;
  }

  &__time {
    margin-top: 4px;
    font-size: 11px;
    opacity: 0.7;
  }
}

.chat-input {
  display: flex;
  gap: 10px;
  align-items: flex-end;

  .el-textarea {
    flex: 1;
  }
}
</style>
