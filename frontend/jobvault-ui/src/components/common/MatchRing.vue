<script setup lang="ts">
import { computed } from 'vue'
import { recommendColor } from '@/utils/score'
const props = defineProps<{ pct: number | null; recommend: string; size?: number }>()
const sz = props.size ?? 120
const R = sz * 0.42
const CIRC = 2 * Math.PI * R
const dash = computed(() => props.pct !== null ? (props.pct / 100) * CIRC : 0)
const gap  = computed(() => CIRC - dash.value)
const color = computed(() => recommendColor(props.recommend))
</script>
<template>
  <div class="relative flex-shrink-0" :style="{ width: sz + 'px', height: sz + 'px' }">
    <svg :viewBox="`0 0 ${sz} ${sz}`" class="w-full h-full -rotate-90">
      <circle :cx="sz/2" :cy="sz/2" :r="R" fill="none" stroke="var(--color-surface-overlay)" stroke-width="7"/>
      <circle v-if="pct !== null" :cx="sz/2" :cy="sz/2" :r="R" fill="none"
        :stroke="color.ring" stroke-width="7" stroke-linecap="round"
        :stroke-dasharray="`${dash} ${gap}`" class="transition-all duration-700"/>
    </svg>
    <div class="absolute inset-0 flex flex-col items-center justify-center">
      <span v-if="pct !== null" class="font-bold font-mono text-text-primary" :style="{ fontSize: sz * 0.19 + 'px' }">{{ pct }}%</span>
      <span v-else class="text-text-muted" :style="{ fontSize: sz * 0.12 + 'px' }">N/A</span>
    </div>
  </div>
</template>
