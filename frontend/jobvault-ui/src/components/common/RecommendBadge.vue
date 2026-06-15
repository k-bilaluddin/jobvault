<script setup lang="ts">
import { recommendColor } from '@/utils/score'
const props = defineProps<{ recommend: string; size?: 'sm' | 'md' }>()
const c = recommendColor(props.recommend)
</script>
<template>
  <span v-if="recommend" :class="['inline-flex items-center font-semibold rounded-full', c.bg, c.text, size === 'sm' ? 'text-[10px] px-2 py-0.5' : 'text-xs px-2.5 py-1']">
    {{ recommend }}
  </span>
  <span v-else class="inline-flex items-center text-[10px] px-2 py-0.5 rounded-full bg-slate-500/10 text-slate-500">No report</span>
</template>
