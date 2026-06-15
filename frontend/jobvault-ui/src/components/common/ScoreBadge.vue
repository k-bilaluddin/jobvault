<script setup lang="ts">
import type { ScoreTier } from '@/types'
import { TIER_COLORS } from '@/utils/score'

const props = defineProps<{
  score: number
  tier: ScoreTier
  size?: 'sm' | 'md'
}>()

const size = props.size ?? 'md'
const colors = TIER_COLORS[props.tier]
</script>

<template>
  <span
    :class="[
      'inline-flex items-center gap-1 font-semibold rounded-full font-mono',
      colors.bg,
      colors.text,
      size === 'sm' ? 'text-xs px-2 py-0.5' : 'text-sm px-2.5 py-1',
    ]"
  >
    {{ score }}%
  </span>
</template>
