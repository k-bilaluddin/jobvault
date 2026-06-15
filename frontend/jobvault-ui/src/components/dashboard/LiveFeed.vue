<script setup lang="ts">
import type { LiveFeedEvent } from '@/types'
import ScoreBadge from '@/components/common/ScoreBadge.vue'

defineProps<{ events: LiveFeedEvent[] }>()

const EVENT_ICONS: Record<string, { path: string; class: string }> = {
  added: {
    path: 'M12 4v16m8-8H4',
    class: 'text-emerald-400 bg-emerald-500/10',
  },
  updated: {
    path: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15',
    class: 'text-blue-400 bg-blue-500/10',
  },
  status_changed: {
    path: 'M7 16V4m0 0L3 8m4-4l4 4M17 8v12m0 0l4-4m-4 4l-4-4',
    class: 'text-amber-400 bg-amber-500/10',
  },
}
</script>

<template>
  <div class="bg-surface-raised border border-border rounded-xl p-5">
    <div class="flex items-center justify-between mb-5">
      <div class="flex items-center gap-2">
        <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
        <h3 class="text-sm font-semibold text-text-primary">Live Activity</h3>
      </div>
      <button class="text-xs text-accent hover:underline">View all</button>
    </div>

    <div class="space-y-3">
      <div
        v-for="event in events"
        :key="event.id"
        class="flex items-start gap-3 p-3 rounded-lg hover:bg-surface-overlay transition-colors cursor-pointer"
      >
        <!-- Icon -->
        <div :class="['w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0', EVENT_ICONS[event.type].class]">
          <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.2">
            <path stroke-linecap="round" stroke-linejoin="round" :d="EVENT_ICONS[event.type].path" />
          </svg>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0">
          <p class="text-xs font-semibold text-text-primary truncate">{{ event.company }}</p>
          <p class="text-xs text-text-muted truncate">{{ event.role }} · {{ event.location }}</p>
          <p v-if="event.description" class="text-xs text-text-muted mt-0.5">{{ event.description }}</p>
          <p class="text-xs text-text-muted mt-0.5">{{ event.occurredAt }}</p>
        </div>

        <!-- Score -->
        <ScoreBadge :score="event.score" :tier="event.scoreTier" size="sm" />
      </div>
    </div>
  </div>
</template>
