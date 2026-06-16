<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import RecommendBadge from '@/components/common/RecommendBadge.vue'
import { useCompanies } from '@/composables/useCompanies'
import { matchPctColor } from '@/utils/score'
import type { ApplicationStage } from '@/types'

const router = useRouter()
const { companies, loading } = useCompanies()

// Only show active stages in kanban (skip Not Interested / Archived)
const KANBAN_STAGES: ApplicationStage[] = ['Ready to Apply','Applied','Interview','Offer','Rejected']

const byStage = computed(() => {
  const map = Object.fromEntries(KANBAN_STAGES.map(s => [s, [] as typeof companies.value])) as Record<ApplicationStage, typeof companies.value>
  for (const c of companies.value) {
    if (KANBAN_STAGES.includes(c.stage as ApplicationStage)) {
      map[c.stage as ApplicationStage].push(c)
    }
  }
  return map
})

const COLUMN_STYLE: Record<string, { border: string; header: string; headerText: string }> = {
  'Ready to Apply': { border: 'border-sky-500/30',    header: 'bg-sky-500/10',     headerText: 'text-sky-400'     },
  'Applied':        { border: 'border-emerald-500/30', header: 'bg-emerald-500/10', headerText: 'text-emerald-400' },
  'Interview':      { border: 'border-violet-500/30', header: 'bg-violet-500/10',  headerText: 'text-violet-400'  },
  'Offer':          { border: 'border-amber-500/30',  header: 'bg-amber-500/10',   headerText: 'text-amber-400'   },
  'Rejected':       { border: 'border-red-500/30',    header: 'bg-red-500/10',     headerText: 'text-red-400'     },
}
</script>

<template>
  <div class="flex flex-col h-full">
    <AppHeader title="Pipeline" />
    <div class="flex-1 overflow-x-auto overflow-y-hidden p-5">
      <div v-if="loading" class="flex gap-4 h-full animate-pulse">
        <div v-for="i in 5" :key="i" class="w-60 flex-shrink-0 bg-surface-raised border border-border rounded-xl h-full"/>
      </div>

      <div v-else class="flex gap-4 h-full min-w-max">
        <div v-for="stage in KANBAN_STAGES" :key="stage" class="w-64 flex-shrink-0 flex flex-col gap-3">
          <!-- Column header -->
          <div :class="['flex items-center justify-between px-3 py-2 rounded-lg border', COLUMN_STYLE[stage].header, COLUMN_STYLE[stage].border]">
            <span :class="['text-xs font-semibold', COLUMN_STYLE[stage].headerText]">{{ stage }}</span>
            <span :class="['text-xs font-bold font-mono', COLUMN_STYLE[stage].headerText]">{{ byStage[stage]?.length ?? 0 }}</span>
          </div>

          <!-- Cards -->
          <div class="flex-1 overflow-y-auto space-y-2 pr-1">
            <div v-for="c in byStage[stage]" :key="c.name"
              @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
              class="bg-surface-raised border border-border rounded-lg p-3 cursor-pointer hover:border-accent/40 transition-all group">
              <div class="flex items-start gap-2 mb-2">
                <CompanyAvatar :name="c.name" size="sm"/>
                <div class="flex-1 min-w-0">
                  <p class="text-xs font-semibold text-text-primary truncate group-hover:text-accent transition-colors">{{ c.name }}</p>
                  <p v-if="c.applied_date" class="text-[10px] text-text-muted font-mono">{{ c.applied_date }}</p>
                </div>
              </div>
              <div class="flex items-center justify-between gap-2">
                <span v-if="c.interviews?.length" class="text-[10px] text-violet-400">{{ c.interviews.length }} interview{{ c.interviews.length > 1 ? 's' : '' }}</span>
                <span v-else class="flex-1"/>
                <div class="flex items-center gap-1.5">
                  <span v-if="c.match_pct !== null" :class="['text-[10px] font-bold font-mono', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
                  <RecommendBadge v-if="c.recommend" :recommend="c.recommend" size="sm"/>
                </div>
              </div>
            </div>

            <div v-if="!byStage[stage]?.length" class="flex flex-col items-center justify-center py-8 opacity-30 text-center">
              <p class="text-2xl mb-1">📭</p>
              <p class="text-xs text-text-muted">Empty</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
