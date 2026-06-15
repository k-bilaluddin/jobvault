<script setup lang="ts">
import type { DashboardOverview } from '@/types'
import TierPill from '@/components/common/TierPill.vue'

defineProps<{ overview: DashboardOverview }>()
</script>

<template>
  <div class="grid grid-cols-4 gap-4">
    <!-- Total -->
    <div class="bg-surface-raised border border-border rounded-xl p-5">
      <div class="flex items-center justify-between mb-3">
        <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Total</p>
        <svg class="w-4 h-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M20 7H4a2 2 0 00-2 2v10a2 2 0 002 2h16a2 2 0 002-2V9a2 2 0 00-2-2zM16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2" />
        </svg>
      </div>
      <p class="text-3xl font-bold text-text-primary font-mono">{{ overview.total }}</p>
      <p class="text-xs text-emerald-400 mt-1 font-medium">+{{ overview.totalDelta }} this week</p>
    </div>

    <!-- Applied -->
    <div class="bg-surface-raised border border-border rounded-xl p-5">
      <div class="flex items-center justify-between mb-3">
        <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Applied</p>
        <svg class="w-4 h-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M5 13l4 4L19 7" />
        </svg>
      </div>
      <p class="text-3xl font-bold text-text-primary font-mono">{{ overview.applied }}</p>
      <p class="text-xs text-text-muted mt-1">{{ overview.applyRate }}% rate</p>
    </div>

    <!-- Interviews -->
    <div class="bg-surface-raised border border-border rounded-xl p-5">
      <div class="flex items-center justify-between mb-3">
        <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Interviews</p>
        <svg class="w-4 h-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0" />
        </svg>
      </div>
      <p class="text-3xl font-bold text-text-primary font-mono">{{ overview.interviews }}</p>
      <p class="text-xs text-text-muted mt-1">{{ overview.conversionRate }}% conv.</p>
    </div>

    <!-- Avg Score -->
    <div class="bg-surface-raised border border-border rounded-xl p-5">
      <div class="flex items-center justify-between mb-3">
        <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Avg Score</p>
        <svg class="w-4 h-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      </div>
      <p class="text-3xl font-bold text-text-primary font-mono">{{ overview.avgScore }}%</p>
      <TierPill :tier="overview.avgScoreTier" />
    </div>
  </div>
</template>
