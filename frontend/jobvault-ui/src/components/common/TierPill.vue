<script setup lang="ts">
import type { ScoreTier } from '@/types'
import { TIER_COLORS } from '@/utils/score'

const props = defineProps<{ tier: ScoreTier }>()
const colors = TIER_COLORS[props.tier]
</script>

<template>
  <span
    :class="[
      'inline-flex items-center text-xs font-semibold px-2 py-0.5 rounded-full',
      colors.bg,
      colors.text,
    ]"
  >
    {{ tier }}
  </span>
</template>
