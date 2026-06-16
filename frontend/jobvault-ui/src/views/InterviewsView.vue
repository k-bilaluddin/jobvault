<script setup lang="ts">
import { useRouter } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import { useCompanies } from '@/composables/useCompanies'
import { OUTCOME_COLORS } from '@/utils/score'

const router = useRouter()
const { withInterviews, loading } = useCompanies()

const INTERVIEW_TYPE_COLOR: Record<string, string> = {
  HR:        'bg-blue-500/15 text-blue-400',
  Technical: 'bg-violet-500/15 text-violet-400',
  Onsite:    'bg-amber-500/15 text-amber-400',
  Final:     'bg-emerald-500/15 text-emerald-400',
  Phone:     'bg-sky-500/15 text-sky-400',
}
</script>

<template>
  <div class="flex flex-col h-full">
    <AppHeader title="All Interviews" />
    <div class="flex-1 overflow-y-auto p-6">

      <div v-if="loading" class="space-y-3 animate-pulse">
        <div v-for="i in 4" :key="i" class="h-32 bg-surface-raised border border-border rounded-xl"/>
      </div>

      <div v-else-if="withInterviews.length === 0" class="flex flex-col items-center justify-center py-24 text-center">
        <p class="text-5xl mb-4">📅</p>
        <p class="text-text-secondary font-medium text-lg">No interview records</p>
        <p class="text-text-muted text-sm mt-1">Interview rounds will appear here once added to tracker_data.json</p>
      </div>

      <div v-else class="space-y-4 max-w-3xl">
        <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-2">
          {{ withInterviews.length }} companies · {{ withInterviews.reduce((s,c) => s + c.interviews.length, 0) }} total rounds
        </p>

        <div v-for="c in withInterviews" :key="c.name"
          class="bg-surface-raised border border-border rounded-xl overflow-hidden hover:border-accent/30 transition-colors">
          <!-- Company header -->
          <div class="flex items-center gap-3 px-5 py-3 border-b border-border cursor-pointer hover:bg-surface-overlay transition-colors"
            @click="router.push(`/company/${encodeURIComponent(c.name)}`)">
            <CompanyAvatar :name="c.name" size="sm"/>
            <div class="flex-1">
              <p class="text-sm font-semibold text-text-primary">{{ c.name }}</p>
              <p class="text-xs text-text-muted">{{ c.stage }} · {{ c.interviews.length }} round{{ c.interviews.length > 1 ? 's' : '' }}</p>
            </div>
            <!-- Pass/fail summary -->
            <div class="flex items-center gap-2 text-xs">
              <span class="text-emerald-400 font-medium">{{ c.interviews.filter(i=>i.outcome==='Pass').length }}✓</span>
              <span class="text-red-400 font-medium">{{ c.interviews.filter(i=>i.outcome==='Fail').length }}✗</span>
              <span v-if="c.interviews.filter(i=>i.outcome==='Pending').length" class="text-amber-400 font-medium">{{ c.interviews.filter(i=>i.outcome==='Pending').length }}⏳</span>
            </div>
          </div>

          <!-- Interview rounds -->
          <div class="divide-y divide-border">
            <div v-for="iv in c.interviews" :key="iv.id" class="flex items-start gap-4 px-5 py-3">
              <div class="flex items-center gap-1.5 mt-0.5 w-5 flex-shrink-0">
                <span class="text-xs text-text-muted font-mono">#{{ iv.id + 1 }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1 flex-wrap">
                  <span :class="['text-[10px] font-semibold px-2 py-0.5 rounded-full', INTERVIEW_TYPE_COLOR[iv.type] ?? 'bg-slate-500/15 text-slate-400']">{{ iv.type }}</span>
                  <span v-if="iv.date" class="text-[10px] text-text-muted font-mono">{{ iv.date }}</span>
                </div>
                <p class="text-xs text-text-secondary">{{ iv.notes }}</p>
              </div>
              <span :class="['text-[10px] font-semibold px-2 py-0.5 rounded-full flex-shrink-0', OUTCOME_COLORS[iv.outcome] ?? 'bg-slate-500/15 text-slate-400']">
                {{ iv.outcome }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
