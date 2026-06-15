<script setup lang="ts">
import { computed, ref } from 'vue'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import { useCompanies } from '@/composables/useCompanies'
import { STAGE_COLORS, matchPctColor } from '@/utils/score'
import { useRouter } from 'vue-router'
import type { ApplicationStage } from '@/types'

const { stats, pipelineCounts, scoreDistribution, companies, loading } = useCompanies()
const router = useRouter()

// ── Chart ─────────────────────────────────────────────────────
type Range = 'week' | 'month' | 'all'
const chartRange  = ref<Range>('all')
const selectedDate = ref<string | null>(null)

const chartData = computed(() => {
  const counts: Record<string, number> = {}
  const now = new Date()
  for (const c of companies.value) {
    if (!c.applied_date) continue
    const d = new Date(c.applied_date)
    if (chartRange.value === 'week') {
      const cut = new Date(now); cut.setDate(now.getDate() - 7); if (d < cut) continue
    } else if (chartRange.value === 'month') {
      const cut = new Date(now); cut.setDate(now.getDate() - 30); if (d < cut) continue
    }
    counts[c.applied_date] = (counts[c.applied_date] ?? 0) + 1
  }
  const bars = Object.entries(counts).sort(([a], [b]) => a.localeCompare(b))
  const max  = Math.max(...bars.map(([, v]) => v), 1)
  return { bars, max }
})

// SVG chart constants
const SVG_W  = 900
const SVG_H  = 160
const PAD_L  = 28
const PAD_R  = 10
const PAD_T  = 16
const PAD_B  = 36
const CHART_W = SVG_W - PAD_L - PAD_R
const CHART_H = SVG_H - PAD_T - PAD_B

const barRects = computed(() => {
  const bars = chartData.value.bars
  if (!bars.length) return []
  const n      = bars.length
  const gap    = Math.min(4, Math.floor(CHART_W / n * 0.15))
  const barW   = Math.max(6, Math.floor((CHART_W - gap * (n + 1)) / n))
  const step   = Math.floor((CHART_W - gap) / n)

  return bars.map(([date, count], i) => {
    const barH = Math.max(3, (count / chartData.value.max) * CHART_H)
    const x    = PAD_L + gap + i * step
    const y    = PAD_T + CHART_H - barH
    return { date, count, x, y, w: barW, h: barH }
  })
})

// Y-axis grid lines
const yLines = computed(() => {
  const m = chartData.value.max
  const step = m <= 5 ? 1 : m <= 10 ? 2 : m <= 20 ? 5 : 10
  const lines: { y: number; label: number }[] = []
  for (let v = step; v <= m; v += step) {
    const y = PAD_T + CHART_H - (v / m) * CHART_H
    lines.push({ y, label: v })
  }
  return lines
})

// X labels — show only every Nth to avoid crowding
const xLabels = computed(() => {
  const rects = barRects.value
  if (!rects.length) return []
  const every = rects.length > 30 ? 7 : rects.length > 14 ? 3 : rects.length > 7 ? 2 : 1
  return rects.filter((_, i) => i % every === 0).map(r => ({
    x: r.x + r.w / 2,
    label: fmtAxisDate(r.date),
  }))
})

function fmtAxisDate(d: string) {
  const dt = new Date(d)
  return dt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' })
}

const filteredByDate = computed(() =>
  selectedDate.value
    ? companies.value.filter(c => c.applied_date === selectedDate.value)
    : null
)

// ── Funnel / distribution colours ────────────────────────────
const FUNNEL_BAR: Record<string, string> = {
  'Ready to Apply': 'bg-sky-500',
  'Applied':        'bg-emerald-500',
  'Interview':      'bg-violet-500',
  'Offer':          'bg-amber-500',
  'Rejected':       'bg-red-500',
}
const DIST_BAR: Record<string, string> = {
  'Strong Apply':   'bg-emerald-500',
  'Moderate Apply': 'bg-amber-500',
  'Risky Apply':    'bg-red-500',
  'Skip':           'bg-slate-500',
  'No Report':      'bg-slate-700',
}
const DIST_TEXT: Record<string, string> = {
  'Strong Apply':   'text-emerald-400',
  'Moderate Apply': 'text-amber-400',
  'Risky Apply':    'text-red-400',
  'Skip':           'text-slate-400',
  'No Report':      'text-slate-500',
}

// ── Recent activity ───────────────────────────────────────────
const recentActivity = computed(() =>
  [...companies.value]
    .sort((a, b) => (b.applied_date ?? '').localeCompare(a.applied_date ?? ''))
    .slice(0, 5)
)

// ── Top matches ───────────────────────────────────────────────
const topMatches = computed(() =>
  [...companies.value]
    .filter(c => c.match_pct !== null && !['Rejected','Not Interested','Archived'].includes(c.stage))
    .sort((a, b) => (b.match_pct ?? 0) - (a.match_pct ?? 0))
    .slice(0, 5)
)

// ── Rejection rate ────────────────────────────────────────────
const rejectionRate = computed(() => {
  const applied = companies.value.filter(c => c.applied).length
  const rejected = companies.value.filter(c => c.stage === 'Rejected').length
  return applied > 0 ? Math.round((rejected / applied) * 100) : 0
})

// ── Avg salary ────────────────────────────────────────────────
const avgSalary = computed(() => {
  const vals = companies.value
    .map(c => {
      const v = c.salary?.advertised
      if (!v) return null
      if (v.includes('-')) {
        const [lo, hi] = v.split('-').map(Number)
        return (lo + hi) / 2
      }
      return Number(v) || null
    })
    .filter((v): v is number => v !== null && v > 10000)
  if (!vals.length) return null
  return Math.round(vals.reduce((a, b) => a + b, 0) / vals.length / 1000)
})
</script>

<template>
  <div class="flex flex-col h-full">
    <AppHeader title="Dashboard" />

    <div v-if="loading" class="flex-1 p-6 grid grid-cols-4 gap-4 animate-pulse">
      <div v-for="i in 4" :key="i" class="bg-surface-raised border border-border rounded-xl h-28"/>
    </div>

    <div v-else class="flex-1 overflow-y-auto p-6 space-y-5">

      <!-- Overview cards -->
      <div class="grid grid-cols-4 gap-4">
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3">Total</p>
          <p class="text-3xl font-bold text-text-primary font-mono">{{ stats.total }}</p>
          <p class="text-xs text-text-muted mt-1">tracked in vault</p>
        </div>
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3">Applied</p>
          <p class="text-3xl font-bold text-text-primary font-mono">{{ stats.applied }}</p>
          <p class="text-xs text-text-muted mt-1">{{ stats.total > 0 ? Math.round(stats.applied/stats.total*100) : 0 }}% of total</p>
        </div>
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3">Interviews</p>
          <p class="text-3xl font-bold text-text-primary font-mono">{{ stats.interviews }}</p>
          <p class="text-xs text-text-muted mt-1">{{ stats.interviewConversionRate }}% conv. rate</p>
        </div>
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3">Avg Match</p>
          <p class="text-3xl font-bold font-mono" :class="matchPctColor(stats.avgMatchPct)">
            {{ stats.avgMatchPct !== null ? stats.avgMatchPct + '%' : '—' }}
          </p>
          <p class="text-xs text-text-muted mt-1">across {{ companies.filter(c=>c.match_pct!==null).length }} reports</p>
        </div>
      </div>

      <!-- Secondary stats -->
      <div class="grid grid-cols-3 gap-4">
        <div class="bg-surface-raised border border-border rounded-xl p-4 flex items-center gap-4">
          <div class="w-10 h-10 rounded-xl bg-red-500/15 flex items-center justify-center flex-shrink-0">
            <svg class="w-5 h-5 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </div>
          <div>
            <p class="text-xs text-text-muted">Rejection Rate</p>
            <p class="text-xl font-bold text-red-400 font-mono">{{ rejectionRate }}%</p>
            <p class="text-[10px] text-text-muted">{{ stats.rejected }} of {{ stats.applied }} applied</p>
          </div>
        </div>
        <div class="bg-surface-raised border border-border rounded-xl p-4 flex items-center gap-4">
          <div class="w-10 h-10 rounded-xl bg-emerald-500/15 flex items-center justify-center flex-shrink-0">
            <svg class="w-5 h-5 text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
          </div>
          <div>
            <p class="text-xs text-text-muted">Avg Advertised Salary</p>
            <p class="text-xl font-bold text-emerald-400 font-mono">{{ avgSalary ? `€${avgSalary}k` : '—' }}</p>
            <p class="text-[10px] text-text-muted">across roles with salary data</p>
          </div>
        </div>
        <div class="bg-surface-raised border border-border rounded-xl p-4 flex items-center gap-4">
          <div class="w-10 h-10 rounded-xl bg-violet-500/15 flex items-center justify-center flex-shrink-0">
            <svg class="w-5 h-5 text-violet-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
          </div>
          <div>
            <p class="text-xs text-text-muted">Ready to Apply</p>
            <p class="text-xl font-bold text-violet-400 font-mono">{{ companies.filter(c=>c.stage==='Ready to Apply').length }}</p>
            <p class="text-[10px] text-text-muted">pending in queue</p>
          </div>
        </div>
      </div>

      <!-- Applications by Date — SVG chart -->
      <div class="bg-surface-raised border border-border rounded-xl p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-sm font-semibold text-text-primary">Applications by Date</h3>
            <p v-if="selectedDate" class="text-xs text-accent mt-0.5">
              {{ filteredByDate?.length }} applications on {{ fmtAxisDate(selectedDate) }} —
              <button @click="selectedDate = null" class="underline">clear</button>
            </p>
          </div>
          <select v-model="chartRange"
            class="bg-surface-overlay border border-border text-text-secondary text-xs rounded-lg px-2 py-1.5 outline-none focus:border-accent">
            <option value="all">All time</option>
            <option value="month">Last 30 days</option>
            <option value="week">Last 7 days</option>
          </select>
        </div>

        <!-- SVG bar chart -->
        <div v-if="chartData.bars.length === 0" class="flex items-center justify-center h-24 text-text-muted text-xs">
          No application dates recorded
        </div>
        <svg v-else :viewBox="`0 0 ${SVG_W} ${SVG_H}`" class="w-full" style="height:160px">
          <!-- Y grid lines -->
          <g v-for="line in yLines" :key="line.label">
            <line :x1="PAD_L" :y1="line.y" :x2="SVG_W - PAD_R" :y2="line.y"
              stroke="var(--color-border)" stroke-width="0.5" stroke-dasharray="3,3"/>
            <text :x="PAD_L - 4" :y="line.y + 3" fill="var(--color-text-muted)"
              font-size="9" text-anchor="end" font-family="monospace">{{ line.label }}</text>
          </g>

          <!-- Baseline -->
          <line :x1="PAD_L" :y1="PAD_T + CHART_H" :x2="SVG_W - PAD_R" :y2="PAD_T + CHART_H"
            stroke="var(--color-border)" stroke-width="1"/>

          <!-- Bars -->
          <g v-for="r in barRects" :key="r.date" @click="selectedDate = selectedDate === r.date ? null : r.date"
            class="cursor-pointer group">
            <!-- Hit area -->
            <rect :x="r.x" :y="PAD_T" :width="r.w" :height="CHART_H" fill="transparent"/>
            <!-- Bar -->
            <rect :x="r.x" :y="r.y" :width="r.w" :height="r.h" rx="2"
              :fill="selectedDate === r.date ? 'var(--color-accent)' : 'rgba(99,102,241,0.55)'"
              class="transition-all duration-150 hover:opacity-90"/>
            <!-- Count tooltip on hover -->
            <text :x="r.x + r.w / 2" :y="r.y - 4" fill="var(--color-text-muted)"
              font-size="9" text-anchor="middle" font-family="monospace"
              class="opacity-0 group-hover:opacity-100 transition-opacity">{{ r.count }}</text>
          </g>

          <!-- X axis labels -->
          <g v-for="lbl in xLabels" :key="lbl.label">
            <text :x="lbl.x" :y="SVG_H - 4" fill="var(--color-text-muted)"
              font-size="9" text-anchor="middle" font-family="system-ui">{{ lbl.label }}</text>
          </g>
        </svg>

        <p class="text-[10px] text-text-muted mt-2">Click a bar to see who applied that day</p>

        <!-- Filtered list -->
        <div v-if="selectedDate && filteredByDate?.length" class="mt-3 pt-3 border-t border-border space-y-1">
          <div v-for="c in filteredByDate" :key="c.name"
            @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
            class="flex items-center gap-2.5 px-2 py-1.5 rounded-lg hover:bg-surface-overlay cursor-pointer transition-colors group">
            <CompanyAvatar :name="c.name" size="sm"/>
            <span class="text-xs font-medium text-text-primary group-hover:text-accent transition-colors flex-1">{{ c.name }}</span>
            <span v-if="c.match_pct !== null" :class="['text-xs font-mono font-bold', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
          </div>
        </div>
      </div>

      <!-- Pipeline + Score Distribution -->
      <div class="grid grid-cols-2 gap-5">
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <div class="flex items-center justify-between mb-5">
            <h3 class="text-sm font-semibold text-text-primary">Pipeline Funnel</h3>
            <button @click="router.push('/pipeline')" class="text-xs text-accent hover:underline">View all</button>
          </div>
          <div class="space-y-3.5">
            <div v-for="s in pipelineCounts" :key="s.label" class="flex items-center gap-3">
              <span class="text-xs text-text-secondary w-24 flex-shrink-0">{{ s.label }}</span>
              <div class="flex-1 h-1.5 bg-surface-overlay rounded-full overflow-hidden">
                <div :class="['h-full rounded-full transition-all duration-700', FUNNEL_BAR[s.label]]"
                  :style="{ width: s.total > 0 ? `${(s.count/s.total)*100}%` : '0%' }"/>
              </div>
              <span class="text-xs font-mono font-semibold text-text-secondary w-5 text-right">{{ s.count }}</span>
            </div>
          </div>
        </div>

        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <h3 class="text-sm font-semibold text-text-primary mb-5">Recommendation Distribution</h3>
          <div class="space-y-3">
            <div v-for="d in scoreDistribution.filter(d=>d.count>0)" :key="d.tier" class="flex items-center gap-3">
              <span :class="['text-xs font-medium w-24 flex-shrink-0', DIST_TEXT[d.tier]]">{{ d.tier }}</span>
              <div class="flex-1 h-1.5 bg-surface-overlay rounded-full overflow-hidden">
                <div :class="['h-full rounded-full transition-all duration-700', DIST_BAR[d.tier]]"
                  :style="{ width: stats.total > 0 ? `${(d.count/stats.total)*100}%` : '0%' }"/>
              </div>
              <span class="text-xs font-mono font-semibold text-text-secondary w-5 text-right">{{ d.count }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Top Matches + Recent Activity -->
      <div class="grid grid-cols-2 gap-5">

        <!-- Top matches not yet applied -->
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-text-primary">Top Matches</h3>
            <span class="text-[10px] text-text-muted">Active only</span>
          </div>
          <div class="space-y-1">
            <div v-for="(c, i) in topMatches" :key="c.name"
              @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
              class="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-surface-overlay cursor-pointer transition-colors group">
              <span class="text-[10px] font-mono text-text-muted w-4 flex-shrink-0">{{ i + 1 }}</span>
              <CompanyAvatar :name="c.name" size="sm"/>
              <div class="flex-1 min-w-0">
                <p class="text-xs font-semibold text-text-primary truncate group-hover:text-accent transition-colors">{{ c.name }}</p>
                <p class="text-[10px] text-text-muted">{{ c.stage }}</p>
              </div>
              <span :class="['text-xs font-mono font-bold', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
            </div>
          </div>
        </div>

        <!-- Recent Activity -->
        <div class="bg-surface-raised border border-border rounded-xl p-5">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-text-primary">Recent Activity</h3>
            <button @click="router.push('/applications')" class="text-xs text-accent hover:underline">View all</button>
          </div>
          <div class="space-y-1">
            <div v-for="c in recentActivity" :key="c.name"
              @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
              class="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-surface-overlay cursor-pointer transition-colors group">
              <CompanyAvatar :name="c.name" size="sm"/>
              <div class="flex-1 min-w-0">
                <p class="text-xs font-semibold text-text-primary truncate group-hover:text-accent transition-colors">{{ c.name }}</p>
                <p class="text-[10px] text-text-muted">{{ c.applied_date || 'No date' }}</p>
              </div>
              <span :class="['text-[10px] px-2 py-0.5 rounded-full font-medium', STAGE_COLORS[c.stage as ApplicationStage]?.bg, STAGE_COLORS[c.stage as ApplicationStage]?.text]">
                {{ c.stage }}
              </span>
            </div>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>
