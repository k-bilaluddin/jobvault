<script setup lang="ts">
import type { ScoreDistribution } from '@/types'
import { TIER_COLORS } from '@/utils/score'

const props = defineProps<{ distribution: ScoreDistribution[] }>()

const total = props.distribution.reduce((s, d) => s + d.count, 0)
</script>

<template>
  <div class="bg-surface-raised border border-border rounded-xl p-5">
    <h3 class="text-sm font-semibold text-text-primary mb-5">Score Distribution</h3>

    <div class="space-y-3">
      <div v-for="item in distribution" :key="item.tier" class="flex items-center gap-3">
        <span :class="['text-xs font-medium w-16 flex-shrink-0', TIER_COLORS[item.tier].text]">
          {{ item.tier }}
        </span>
        <div class="flex-1 h-1.5 bg-surface-overlay rounded-full overflow-hidden">
          <div
            :class="['h-full rounded-full transition-all duration-700', TIER_COLORS[item.tier].bar]"
            :style="{ width: total > 0 ? `${(item.count / total) * 100}%` : '0%' }"
          />
        </div>
        <span class="text-xs font-mono font-semibold text-text-secondary w-6 text-right">
          {{ item.count }}
        </span>
      </div>
    </div>
  </div>
</template>
