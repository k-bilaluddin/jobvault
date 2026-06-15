<script setup lang="ts">
import type { PipelineStage } from '@/types'
import { STATUS_LABELS } from '@/utils/score'

defineProps<{ stages: PipelineStage[] }>()

const BAR_COLORS: Record<string, string> = {
  ReadyToApply: 'bg-indigo-500',
  Applied:      'bg-emerald-500',
  Interview:    'bg-amber-400',
  Offer:        'bg-violet-500',
  Rejected:     'bg-red-500',
}
</script>

<template>
  <div class="bg-surface-raised border border-border rounded-xl p-5">
    <div class="flex items-center justify-between mb-5">
      <h3 class="text-sm font-semibold text-text-primary">Pipeline Funnel</h3>
      <button class="text-xs text-accent hover:underline">View all</button>
    </div>

    <div class="space-y-3.5">
      <div v-for="stage in stages" :key="stage.label" class="flex items-center gap-3">
        <span class="text-xs text-text-secondary w-24 flex-shrink-0">
          {{ STATUS_LABELS[stage.label] }}
        </span>
        <div class="flex-1 h-1.5 bg-surface-overlay rounded-full overflow-hidden">
          <div
            :class="['h-full rounded-full transition-all duration-700', BAR_COLORS[stage.label]]"
            :style="{ width: stage.max > 0 ? `${(stage.count / stage.max) * 100}%` : '0%' }"
          />
        </div>
        <span class="text-xs font-mono font-semibold text-text-secondary w-6 text-right">
          {{ stage.count }}
        </span>
      </div>
    </div>
  </div>
</template>
