<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import AppHeader from '@/components/layout/AppHeader.vue'
import { api } from '@/api'

// ── Data ─────────────────────────────────────────────────────
interface HistEntry { name: string; applied_date: string; stage: string; source: string; current?: boolean }
interface HistData {
  available: boolean
  historical: HistEntry[]
  current: HistEntry[]
  scanned_at: string
  error?: string
}

const data     = ref<HistData | null>(null)
const loading  = ref(false)
const error    = ref('')
type View = 'monthly' | 'weekly' | 'heatmap'
const activeView = ref<View>('monthly')

async function load() {
  loading.value = true
  error.value   = ''
  try {
    const { data: json } = await api.get('/api/applications/historical')
    data.value  = json
    if (!json.available) error.value = json.error ?? 'Historical data not available'
  } catch {
    error.value = 'Could not load historical data.'
  } finally {
    loading.value = false
  }
}
onMounted(load)

// ── Combined entries ──────────────────────────────────────────
const allEntries = computed((): HistEntry[] => {
  if (!data.value?.available) return []
  const hist = (data.value.historical ?? []).map(e => ({ ...e, current: false }))
  const curr = (data.value.current ?? []).map(e => ({ ...e, current: true }))
  // Deduplicate by name — current wins
  const map = new Map<string, HistEntry>()
  for (const e of hist) map.set(e.name, e)
  for (const e of curr) map.set(e.name, e)
  return [...map.values()].filter(e => !!e.applied_date).sort((a, b) => a.applied_date.localeCompare(b.applied_date))
})

// ── Summary stats ─────────────────────────────────────────────
const stats = computed(() => {
  const all = allEntries.value
  if (!all.length) return null
  const dates = all.map(e => e.applied_date).filter(Boolean)
  const earliest = dates[0]
  const latest   = dates[dates.length - 1]

  // Days span
  const daySpan = Math.round((new Date(latest).getTime() - new Date(earliest).getTime()) / 86400000) + 1
  const weeks   = Math.ceil(daySpan / 7)

  // Per day count
  const byDay: Record<string, number> = {}
  for (const e of all) {
    const d = e.applied_date.slice(0, 10)
    byDay[d] = (byDay[d] ?? 0) + 1
  }
  const peakDay   = Object.entries(byDay).sort(([,a],[,b])=>b-a)[0]
  const avgPerDay = (all.length / daySpan).toFixed(1)

  // Source breakdown
  const sources: Record<string, number> = {}
  for (const e of all) sources[e.source ?? 'Unknown'] = (sources[e.source ?? 'Unknown'] ?? 0) + 1

  return { total: all.length, earliest, latest, daySpan, weeks, peakDay, avgPerDay, sources }
})

// ── Monthly data ─────────────────────────────────────────────
const monthlyData = computed(() => {
  const map: Record<string, number> = {}
  for (const e of allEntries.value) {
    const key = e.applied_date.slice(0, 7) // YYYY-MM
    map[key] = (map[key] ?? 0) + 1
  }
  const sorted = Object.entries(map).sort(([a],[b]) => a.localeCompare(b))
  const max    = Math.max(...sorted.map(([,v])=>v), 1)
  return { bars: sorted, max }
})

// ── Weekly data ───────────────────────────────────────────────
const weeklyData = computed(() => {
  const map: Record<string, number> = {}
  for (const e of allEntries.value) {
    const d   = new Date(e.applied_date)
    const day = d.getDay()
    const mon = new Date(d); mon.setDate(d.getDate() - (day === 0 ? 6 : day - 1))
    const key = mon.toISOString().slice(0, 10)
    map[key] = (map[key] ?? 0) + 1
  }
  const sorted = Object.entries(map).sort(([a],[b]) => a.localeCompare(b))
  const max    = Math.max(...sorted.map(([,v])=>v), 1)
  return { bars: sorted, max }
})

// ── Heatmap (GitHub style) ────────────────────────────────────
const heatmap = computed(() => {
  const byDay: Record<string, number> = {}
  for (const e of allEntries.value) byDay[e.applied_date.slice(0,10)] = (byDay[e.applied_date.slice(0,10)] ?? 0) + 1
  const max = Math.max(...Object.values(byDay), 1)

  // Build weeks from earliest Monday to today
  const all = allEntries.value
  if (!all.length) return { weeks: [], max }
  const start = new Date(all[0].applied_date)
  const dow   = start.getDay()
  start.setDate(start.getDate() - (dow === 0 ? 6 : dow - 1))
  const end   = new Date()
  const weeks: { date: string; count: number }[][] = []
  let cur = new Date(start)
  while (cur <= end) {
    const week: { date: string; count: number }[] = []
    for (let d = 0; d < 7; d++) {
      const iso = cur.toISOString().slice(0, 10)
      week.push({ date: iso, count: byDay[iso] ?? 0 })
      cur.setDate(cur.getDate() + 1)
    }
    weeks.push(week)
  }
  return { weeks, max }
})

// ── Cumulative ────────────────────────────────────────────────
const cumulative = computed(() => {
  const points: { date: string; total: number }[] = []
  let total = 0
  const byDay: Record<string, number> = {}
  for (const e of allEntries.value) byDay[e.applied_date.slice(0,10)] = (byDay[e.applied_date.slice(0,10)] ?? 0) + 1
  for (const [date, count] of Object.entries(byDay).sort(([a],[b]) => a.localeCompare(b))) {
    total += count
    points.push({ date, total })
  }
  return points
})

// ── SVG helpers ───────────────────────────────────────────────
const SVG_W  = 900
const SVG_H  = 180
const PAD_L  = 36
const PAD_R  = 10
const PAD_T  = 16
const PAD_B  = 40
const CW     = SVG_W - PAD_L - PAD_R
const CH     = SVG_H - PAD_T - PAD_B

function barRects(bars: [string, number][], max: number) {
  const n   = bars.length
  if (!n) return []
  const gap = Math.max(1, Math.floor(CW / n * 0.12))
  const bw  = Math.max(3, Math.floor((CW - gap * (n+1)) / n))
  const step= Math.floor((CW - gap) / n)
  return bars.map(([date, count], i) => {
    const bh = Math.max(2, (count / max) * CH)
    return { date, count, x: PAD_L + gap + i * step, y: PAD_T + CH - bh, w: bw, h: bh }
  })
}

const monthRects  = computed(() => barRects(monthlyData.value.bars, monthlyData.value.max))
const weekRects   = computed(() => barRects(weeklyData.value.bars, weeklyData.value.max))

// X labels — every Nth
function xLabels(rects: ReturnType<typeof barRects>, labelFn: (d:string)=>string) {
  const every = rects.length > 40 ? 8 : rects.length > 20 ? 4 : rects.length > 10 ? 2 : 1
  return rects.filter((_,i) => i % every === 0).map(r => ({ x: r.x + r.w/2, label: labelFn(r.date) }))
}
const monthXLabels = computed(() => xLabels(monthRects.value, d => {
  const [y, m] = d.split('-')
  return new Date(+y, +m-1).toLocaleDateString('en-GB', { month: 'short', year: '2-digit' })
}))
const weekXLabels  = computed(() => xLabels(weekRects.value, d => {
  return new Date(d).toLocaleDateString('en-GB', { day:'2-digit', month:'short' })
}))

// Y grid
function yGrid(max: number) {
  const step = max <= 5 ? 1 : max <= 20 ? 5 : max <= 50 ? 10 : max <= 100 ? 20 : 50
  const lines: { y: number; label: number }[] = []
  for (let v = step; v <= max; v += step) {
    lines.push({ y: PAD_T + CH - (v/max)*CH, label: v })
  }
  return lines
}
const monthYGrid = computed(() => yGrid(monthlyData.value.max))
const weekYGrid  = computed(() => yGrid(weeklyData.value.max))

// Cumulative SVG path
const cumulativePath = computed(() => {
  const pts = cumulative.value
  if (!pts.length) return ''
  const maxT  = pts[pts.length-1].total
  const minD  = pts[0].date
  const maxD  = pts[pts.length-1].date
  const spanD = new Date(maxD).getTime() - new Date(minD).getTime() || 1
  const points = pts.map(p => {
    const x = PAD_L + ((new Date(p.date).getTime() - new Date(minD).getTime()) / spanD) * CW
    const y = PAD_T + CH - (p.total / maxT) * CH
    return `${x.toFixed(1)},${y.toFixed(1)}`
  })
  return 'M' + points.join(' L')
})
const cumulativeArea = computed(() => {
  const pts = cumulative.value
  if (!pts.length) return ''
  return cumulativePath.value + ` L${PAD_L + CW},${PAD_T + CH} L${PAD_L},${PAD_T + CH} Z`
})

// Heatmap colour
function heatColor(count: number, max: number): string {
  if (count === 0) return 'var(--color-surface-overlay)'
  const ratio = count / max
  if (ratio < 0.25) return 'rgba(99,102,241,0.3)'
  if (ratio < 0.5)  return 'rgba(99,102,241,0.55)'
  if (ratio < 0.75) return 'rgba(99,102,241,0.8)'
  return '#6366f1'
}

// Source colours
const SOURCE_COLORS: Record<string, string> = {
  'This PC':    '#6366f1',
  'Other PC':   '#22c55e',
  'Q2-2026':    '#f59e0b',
}

// Month labels for heatmap
const heatMonthLabels = computed(() => {
  const labels: { label: string; x: number }[] = []
  let lastMonth = ''
  heatmap.value.weeks.forEach((week, wi) => {
    const m = week[0].date.slice(0, 7)
    if (m !== lastMonth) {
      labels.push({ label: new Date(week[0].date).toLocaleDateString('en-GB', { month: 'short', year: '2-digit' }), x: wi * 14 })
      lastMonth = m
    }
  })
  return labels
})

const hoveredCell = ref<{ date: string; count: number } | null>(null)
const hoveredBar  = ref<{ date: string; count: number } | null>(null)
</script>

<template>
  <div class="flex flex-col h-full">
    <AppHeader title="Historical" />
    <div class="flex-1 overflow-y-auto p-4 md:p-6 space-y-6">

      <!-- Loading -->
      <div v-if="loading" class="space-y-4 animate-pulse">
        <div v-for="i in 4" :key="i" class="h-48 bg-surface-raised border border-border rounded-2xl"/>
      </div>

      <!-- Error -->
      <div v-else-if="error || !data?.available" class="flex flex-col items-center justify-center py-32 text-center">
        <div class="w-16 h-16 rounded-2xl bg-surface-raised border border-border flex items-center justify-center mb-4">
          <svg class="w-8 h-8 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </div>
        <p class="text-text-secondary font-semibold text-lg mb-1">Historical data unavailable</p>
        <p class="text-text-muted text-sm max-w-sm">{{ error || 'Make sure old_applications.json is present and Flask is running.' }}</p>
        <button @click="load" class="mt-5 px-4 py-2 bg-accent text-white text-sm font-medium rounded-lg hover:bg-accent/90 transition-colors">Retry</button>
      </div>

      <template v-else-if="stats">

        <!-- Header stats -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div class="bg-surface-raised border border-border rounded-2xl p-5">
            <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-3">Total Applications</p>
            <p class="text-4xl font-bold text-text-primary font-mono">{{ stats.total.toLocaleString() }}</p>
            <p class="text-xs text-text-muted mt-1.5">across full job search</p>
          </div>
          <div class="bg-surface-raised border border-border rounded-2xl p-5">
            <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-3">Time Span</p>
            <p class="text-4xl font-bold text-text-primary font-mono">{{ stats.weeks }}<span class="text-xl text-text-muted ml-1">wks</span></p>
            <p class="text-xs text-text-muted mt-1.5">{{ stats.earliest }} → {{ stats.latest }}</p>
          </div>
          <div class="bg-surface-raised border border-border rounded-2xl p-5">
            <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-3">Peak Day</p>
            <p class="text-4xl font-bold text-accent font-mono">{{ stats.peakDay?.[1] }}<span class="text-xl text-text-muted ml-1">apps</span></p>
            <p class="text-xs text-text-muted mt-1.5">{{ stats.peakDay?.[0] }}</p>
          </div>
          <div class="bg-surface-raised border border-border rounded-2xl p-5">
            <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-3">Daily Average</p>
            <p class="text-4xl font-bold text-emerald-400 font-mono">{{ stats.avgPerDay }}</p>
            <p class="text-xs text-text-muted mt-1.5">applications per day</p>
          </div>
        </div>

        <!-- Source breakdown -->
        <div class="bg-surface-raised border border-border rounded-2xl p-4 md:p-6">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-5">Source Breakdown</p>
          <div class="flex flex-wrap items-end gap-4 md:gap-6">
            <div v-for="(count, source) in stats.sources" :key="source" class="flex-1 min-w-[110px]">
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center gap-2">
                  <span class="w-2.5 h-2.5 rounded-full flex-shrink-0" :style="{ background: SOURCE_COLORS[source] ?? '#64748b' }"/>
                  <span class="text-xs font-medium text-text-secondary">{{ source }}</span>
                </div>
                <span class="text-sm font-bold font-mono text-text-primary">{{ count.toLocaleString() }}</span>
              </div>
              <div class="h-2 bg-surface-overlay rounded-full overflow-hidden">
                <div class="h-full rounded-full transition-all duration-700"
                  :style="{ width: `${(count/stats.total)*100}%`, background: SOURCE_COLORS[source] ?? '#64748b' }"/>
              </div>
              <p class="text-[10px] text-text-muted mt-1.5">{{ ((count/stats.total)*100).toFixed(1) }}% of total</p>
            </div>
          </div>
        </div>

        <!-- View toggle -->
        <div class="flex items-center gap-1 bg-surface-raised border border-border rounded-xl p-1 w-fit">
          <button v-for="v in (['monthly','weekly','heatmap'] as View[])" :key="v"
            @click="activeView = v"
            :class="['px-4 py-1.5 text-xs font-semibold rounded-lg transition-colors capitalize',
              activeView === v ? 'bg-accent text-white shadow-sm' : 'text-text-muted hover:text-text-primary']">
            {{ v }}
          </button>
        </div>

        <!-- Monthly chart -->
        <div v-if="activeView === 'monthly'" class="bg-surface-raised border border-border rounded-2xl p-4 md:p-6">
          <div class="flex items-center justify-between mb-1">
            <div>
              <h3 class="text-sm font-semibold text-text-primary">Applications per Month</h3>
              <p class="text-xs text-text-muted mt-0.5">{{ monthlyData.bars.length }} months · peak {{ monthlyData.max }} in a month</p>
            </div>
            <div v-if="hoveredBar" class="text-right">
              <p class="text-xs text-text-muted">{{ hoveredBar.date }}</p>
              <p class="text-sm font-bold text-accent">{{ hoveredBar.count }} applications</p>
            </div>
          </div>
          <svg :viewBox="`0 0 ${SVG_W} ${SVG_H}`" class="w-full" style="height:180px">
            <g v-for="l in monthYGrid" :key="l.label">
              <line :x1="PAD_L" :y1="l.y" :x2="SVG_W-PAD_R" :y2="l.y" stroke="var(--color-border)" stroke-width="0.5" stroke-dasharray="3,3"/>
              <text :x="PAD_L-4" :y="l.y+3" fill="var(--color-text-muted)" font-size="9" text-anchor="end" font-family="monospace">{{ l.label }}</text>
            </g>
            <line :x1="PAD_L" :y1="PAD_T+CH" :x2="SVG_W-PAD_R" :y2="PAD_T+CH" stroke="var(--color-border)" stroke-width="1"/>
            <g v-for="r in monthRects" :key="r.date" @mouseenter="hoveredBar = { date: r.date, count: r.count }" @mouseleave="hoveredBar = null" class="cursor-pointer">
              <rect :x="r.x" :y="PAD_T" :width="r.w" :height="CH" fill="transparent"/>
              <rect :x="r.x" :y="r.y" :width="r.w" :height="r.h" rx="2"
                :fill="hoveredBar?.date === r.date ? '#818cf8' : 'rgba(99,102,241,0.6)'"
                class="transition-colors duration-100"/>
              <text v-if="hoveredBar?.date === r.date" :x="r.x+r.w/2" :y="r.y-5" fill="var(--color-accent)" font-size="9" text-anchor="middle" font-family="monospace">{{ r.count }}</text>
            </g>
            <g v-for="l in monthXLabels" :key="l.label">
              <text :x="l.x" :y="SVG_H-4" fill="var(--color-text-muted)" font-size="9" text-anchor="middle" font-family="system-ui">{{ l.label }}</text>
            </g>
          </svg>
        </div>

        <!-- Weekly chart -->
        <div v-else-if="activeView === 'weekly'" class="bg-surface-raised border border-border rounded-2xl p-4 md:p-6">
          <div class="flex items-center justify-between mb-1">
            <div>
              <h3 class="text-sm font-semibold text-text-primary">Applications per Week</h3>
              <p class="text-xs text-text-muted mt-0.5">{{ weeklyData.bars.length }} weeks · peak {{ weeklyData.max }} in a week</p>
            </div>
            <div v-if="hoveredBar" class="text-right">
              <p class="text-xs text-text-muted">w/c {{ hoveredBar.date }}</p>
              <p class="text-sm font-bold text-emerald-400">{{ hoveredBar.count }} applications</p>
            </div>
          </div>
          <svg :viewBox="`0 0 ${SVG_W} ${SVG_H}`" class="w-full" style="height:180px">
            <g v-for="l in weekYGrid" :key="l.label">
              <line :x1="PAD_L" :y1="l.y" :x2="SVG_W-PAD_R" :y2="l.y" stroke="var(--color-border)" stroke-width="0.5" stroke-dasharray="3,3"/>
              <text :x="PAD_L-4" :y="l.y+3" fill="var(--color-text-muted)" font-size="9" text-anchor="end" font-family="monospace">{{ l.label }}</text>
            </g>
            <line :x1="PAD_L" :y1="PAD_T+CH" :x2="SVG_W-PAD_R" :y2="PAD_T+CH" stroke="var(--color-border)" stroke-width="1"/>
            <g v-for="r in weekRects" :key="r.date" @mouseenter="hoveredBar = { date: r.date, count: r.count }" @mouseleave="hoveredBar = null" class="cursor-pointer">
              <rect :x="r.x" :y="PAD_T" :width="r.w" :height="CH" fill="transparent"/>
              <rect :x="r.x" :y="r.y" :width="r.w" :height="r.h" rx="2"
                :fill="hoveredBar?.date === r.date ? '#34d399' : 'rgba(34,197,94,0.55)'"
                class="transition-colors duration-100"/>
            </g>
            <g v-for="l in weekXLabels" :key="l.label">
              <text :x="l.x" :y="SVG_H-4" fill="var(--color-text-muted)" font-size="9" text-anchor="middle" font-family="system-ui">{{ l.label }}</text>
            </g>
          </svg>
        </div>

        <!-- Heatmap -->
        <div v-else-if="activeView === 'heatmap'" class="bg-surface-raised border border-border rounded-2xl p-4 md:p-6">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h3 class="text-sm font-semibold text-text-primary">Activity Heatmap</h3>
              <p class="text-xs text-text-muted mt-0.5">{{ stats.total.toLocaleString() }} applications · each cell = 1 day</p>
            </div>
            <div v-if="hoveredCell" class="text-right">
              <p class="text-xs text-text-muted">{{ hoveredCell.date }}</p>
              <p class="text-sm font-bold text-accent">{{ hoveredCell.count || 'No' }} application{{ hoveredCell.count !== 1 ? 's' : '' }}</p>
            </div>
            <div v-else class="flex items-center gap-1.5 text-[10px] text-text-muted">
              <span>Less</span>
              <span v-for="n in 4" :key="n" class="w-3 h-3 rounded-sm" :style="{ background: heatColor(n-1, 3) }"/>
              <span>More</span>
            </div>
          </div>

          <!-- Month labels -->
          <div class="overflow-x-auto">
            <div class="relative" :style="{ width: heatmap.weeks.length * 14 + 'px', minWidth: '100%' }">
              <!-- Month labels -->
              <div class="flex mb-1" style="height:14px">
                <div v-for="lbl in heatMonthLabels" :key="lbl.label + lbl.x"
                  class="absolute text-[9px] text-text-muted" :style="{ left: lbl.x + 'px' }">
                  {{ lbl.label }}
                </div>
              </div>
              <!-- Day rows (Mon=0 ... Sun=6) -->
              <div class="flex gap-0.5">
                <!-- Day labels -->
                <div class="flex flex-col gap-0.5 mr-1">
                  <div v-for="d in ['M','T','W','T','F','S','S']" :key="d"
                    class="text-[8px] text-text-muted flex items-center justify-end" style="height:12px;width:10px">{{ d }}</div>
                </div>
                <!-- Weeks -->
                <div v-for="(week, wi) in heatmap.weeks" :key="wi" class="flex flex-col gap-0.5">
                  <div v-for="day in week" :key="day.date"
                    class="w-3 h-3 rounded-sm cursor-pointer transition-opacity hover:opacity-80 flex-shrink-0"
                    :style="{ background: heatColor(day.count, heatmap.max) }"
                    :title="`${day.date}: ${day.count} applications`"
                    @mouseenter="hoveredCell = day"
                    @mouseleave="hoveredCell = null"/>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Cumulative growth -->
        <div class="bg-surface-raised border border-border rounded-2xl p-4 md:p-6">
          <div class="flex items-center justify-between mb-1">
            <div>
              <h3 class="text-sm font-semibold text-text-primary">Cumulative Growth</h3>
              <p class="text-xs text-text-muted mt-0.5">Total applications over time</p>
            </div>
            <p class="text-2xl font-bold font-mono text-accent">{{ stats.total.toLocaleString() }}</p>
          </div>
          <svg :viewBox="`0 0 ${SVG_W} ${SVG_H}`" class="w-full" style="height:160px">
            <defs>
              <linearGradient id="cumGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#6366f1" stop-opacity="0.4"/>
                <stop offset="100%" stop-color="#6366f1" stop-opacity="0"/>
              </linearGradient>
            </defs>
            <!-- Grid -->
            <line :x1="PAD_L" :y1="PAD_T+CH" :x2="SVG_W-PAD_R" :y2="PAD_T+CH" stroke="var(--color-border)" stroke-width="1"/>
            <!-- Area fill -->
            <path :d="cumulativeArea" fill="url(#cumGrad)"/>
            <!-- Line -->
            <path :d="cumulativePath" fill="none" stroke="#6366f1" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <!-- Start + end labels -->
            <text v-if="cumulative.length" :x="PAD_L+4" :y="PAD_T+CH-4" fill="var(--color-text-muted)" font-size="9" font-family="system-ui">{{ cumulative[0].date }}</text>
            <text v-if="cumulative.length" :x="SVG_W-PAD_R-4" :y="PAD_T+CH-4" fill="var(--color-text-muted)" font-size="9" text-anchor="end" font-family="system-ui">{{ cumulative[cumulative.length-1].date }}</text>
          </svg>
        </div>

        <!-- Top months table -->
        <div class="bg-surface-raised border border-border rounded-2xl p-4 md:p-6">
          <h3 class="text-sm font-semibold text-text-primary mb-4">Top Months by Volume</h3>
          <div class="space-y-2">
            <div v-for="([month, count], i) in [...monthlyData.bars].sort(([,a],[,b])=>b-a).slice(0,8)" :key="month"
              class="flex items-center gap-4">
              <span class="text-[10px] font-mono text-text-muted w-4 text-right flex-shrink-0">{{ i+1 }}</span>
              <span class="text-xs font-medium text-text-secondary w-20 flex-shrink-0">
                {{ new Date(month+'-01').toLocaleDateString('en-GB',{month:'long',year:'numeric'}) }}
              </span>
              <div class="flex-1 h-1.5 bg-surface-overlay rounded-full overflow-hidden">
                <div class="h-full rounded-full bg-accent/70 transition-all duration-700"
                  :style="{ width: `${(count/monthlyData.max)*100}%` }"/>
              </div>
              <span class="text-xs font-bold font-mono text-accent w-12 text-right">{{ count }} apps</span>
            </div>
          </div>
        </div>

      </template>
    </div>
  </div>
</template>
