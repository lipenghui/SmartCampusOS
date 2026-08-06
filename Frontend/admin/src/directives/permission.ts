/**
 * v-permission 权限指令：无权限时移除元素（对齐后端 RBAC，LLD §2.7.2 / §8.2）。
 *
 * 用法：`v-permission="'sys:user'"` 或 `v-permission="['sys:user', 'sys:role']"`。
 */
import type { Directive, DirectiveBinding } from 'vue'
import { hasPermissionCode } from '@/stores/permission'

function apply(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
  if (!hasPermissionCode(binding.value)) {
    el.parentNode?.removeChild(el)
  }
}

export const permissionDirective: Directive<HTMLElement, string | string[]> = {
  mounted: apply,
  updated: apply,
}
