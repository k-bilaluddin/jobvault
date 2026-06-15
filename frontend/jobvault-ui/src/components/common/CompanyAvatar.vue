<script setup lang="ts">
const props = defineProps<{
  name: string
  size?: 'sm' | 'md' | 'lg'
}>()

const size = props.size ?? 'md'

const PALETTE = [
  '#6366f1', '#3b82f6', '#22c55e', '#f59e0b',
  '#ef4444', '#8b5cf6', '#ec4899', '#14b8a6',
]

function colorFor(name: string) {
  let hash = 0
  for (const c of name) hash = c.charCodeAt(0) + ((hash << 5) - hash)
  return PALETTE[Math.abs(hash) % PALETTE.length]
}

const initial = props.name.charAt(0).toUpperCase()
const bg = colorFor(props.name)

const sizeClass = {
  sm: 'w-7 h-7 text-xs',
  md: 'w-9 h-9 text-sm',
  lg: 'w-11 h-11 text-base',
}[size]
</script>

<template>
  <div
    :class="['flex items-center justify-center rounded-lg font-bold text-white flex-shrink-0', sizeClass]"
    :style="{ backgroundColor: bg }"
  >
    {{ initial }}
  </div>
</template>
