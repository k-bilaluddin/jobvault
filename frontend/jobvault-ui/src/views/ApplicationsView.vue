<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import RecommendBadge from '@/components/common/RecommendBadge.vue'
import { useCompanies } from '@/composables/useCompanies'
import { STAGE_COLORS, matchPctColor, matchPctBar } from '@/utils/score'
import { PIPELINE_STAGES } from '@/types'
import type { ApplicationStage, Company } from '@/types'

const router = useRouter()
const { filtered, loading, search, filterStage, companies } = useCompanies()
const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5100'

// ── Tabs ─────────────────────────────────────────────────────
const STAGE_TABS = ['All', ...PIPELINE_STAGES] as const
const stageCounts = computed(() => {
  const m: Record<string, number> = { All: companies.value.length }
  for (const s of PIPELINE_STAGES) m[s] = companies.value.filter(c => c.stage === s).length
  return m
})

// ── Sort ─────────────────────────────────────────────────────
type SortKey = 'name' | 'applied_date' | 'stage' | 'match_pct' | 'salary'
const sortKey = ref<SortKey>('applied_date')
const sortDir = ref<'asc' | 'desc'>('desc')

function toggleSort(key: SortKey) {
  if (sortKey.value === key) { sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc' }
  else { sortKey.value = key; sortDir.value = 'desc' }
}

function salaryNum(c: Company): number {
  const v = c.salary?.advertised
  if (!v) return 0
  if (v.includes('-')) return parseInt(v.split('-')[1]) || 0
  return parseInt(v) || 0
}

const sorted = computed(() => {
  // Attach original index so ties preserve API insertion order (newest last → reverse index = newest first)
  const indexed = filtered.value.map((c, i) => ({ c, i }))
  return indexed.sort((x, y) => {
    const a = x.c, b = y.c
    let va: string | number = ''
    let vb: string | number = ''
    if (sortKey.value === 'name')         { va = a.name.toLowerCase();   vb = b.name.toLowerCase()   }
    if (sortKey.value === 'applied_date') { va = a.applied_date || '';   vb = b.applied_date || ''   }
    if (sortKey.value === 'stage')        { va = a.stage;                vb = b.stage                }
    if (sortKey.value === 'match_pct')    { va = a.match_pct ?? -1;      vb = b.match_pct ?? -1      }
    if (sortKey.value === 'salary')       { va = salaryNum(a);           vb = salaryNum(b)           }
    if (va < vb) return sortDir.value === 'asc' ? -1 : 1
    if (va > vb) return sortDir.value === 'asc' ? 1 : -1
    // Tiebreaker: higher index (later in API response) comes first
    return y.i - x.i
  }).map(x => x.c)
})

function sortIcon(key: SortKey) {
  if (sortKey.value !== key) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Salary ────────────────────────────────────────────────────
function fmtSalary(c: Company): string {
  const v = c.salary?.advertised
  if (!v) return ''
  if (v.includes('-')) {
    const parts = v.split('-').map(n => { const x = parseInt(n); return x >= 1000 ? Math.round(x/1000)+'k' : '' })
    return parts[0] && parts[1] ? `€${parts[0]}–${parts[1]}` : ''
  }
  const n = parseInt(v)
  return n >= 1000 ? `€${Math.round(n/1000)}k` : ''
}

// ── Role ─────────────────────────────────────────────────────
function roleStr(role: string | string[] | undefined): string {
  if (!role) return ''
  return Array.isArray(role) ? (role[0] ?? '') : role
}

// ── Follow-up ────────────────────────────────────────────────
const today = new Date().toISOString().split('T')[0]
function isFollowUpDue(c: Company): boolean {
  return !!c.follow_up_date && c.follow_up_date <= today
}

// ── Row actions ───────────────────────────────────────────────
const activeActionRow = ref<string | null>(null)
const stageUpdating   = ref<string | null>(null)

function toggleActions(name: string, e: MouseEvent) {
  e.stopPropagation()
  activeActionRow.value = activeActionRow.value === name ? null : name
}

function onDocClick() { activeActionRow.value = null }

async function quickStage(c: Company, stage: ApplicationStage, e: MouseEvent) {
  e.stopPropagation()
  stageUpdating.value = c.name
  activeActionRow.value = null
  try {
    await fetch(`${API_BASE}/api/company/${encodeURIComponent(c.name)}/stage`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ stage }),
    })
    c.stage = stage
  } finally {
    stageUpdating.value = null
  }
}

function openUrl(c: Company, e: MouseEvent) {
  e.stopPropagation()
  if (c.job_url) window.open(c.job_url, '_blank')
}

function openDetail(c: Company, e: MouseEvent) {
  e.stopPropagation()
  router.push(`/company/${encodeURIComponent(c.name)}`)
}

function copyEmail(email: string, e: MouseEvent) {
  e.stopPropagation()
  navigator.clipboard?.writeText(email).catch(() => {
    const el = document.createElement('textarea')
    el.value = email
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    document.body.removeChild(el)
  })
  activeActionRow.value = null
}

// ── Export CSV ────────────────────────────────────────────────
function exportCsv() {
  const header = ['Company', 'Role', 'Stage', 'Applied', 'Match%', 'Salary', 'Verdict'].join(',')
  const rows = sorted.value.map(c =>
    [c.name, roleStr(c.role), c.stage, c.applied_date, c.match_pct ?? '', fmtSalary(c), c.recommend]
      .map(v => '"' + String(v ?? '').replace(/"/g, '""') + '"').join(',')
  )
  const csv = [header, ...rows].join('\n')
  const link = document.createElement('a')
  link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv)
  link.download = 'applications.csv'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

const QUICK_STAGES: ApplicationStage[] = ['Ready to Apply', 'Applied', 'Interview', 'Offer', 'Rejected', 'Not Interested']
</script>

<template>
  <div class="flex flex-col h-full" @click="onDocClick">
    <AppHeader title="Applications" />
    <div class="flex-1 overflow-y-auto">

      <!-- Toolbar -->
      <div class="px-6 pt-5 pb-0 border-b border-border space-y-3">
        <div class="flex items-center gap-3">
          <div class="relative flex-1 max-w-sm">
            <svg class="w-4 h-4 text-text-muted absolute left-3 top-1/2 -translate-y-1/2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
            <input v-model="search" type="text" placeholder="Search company or role..."
              class="w-full bg-surface-raised border border-border rounded-lg pl-9 pr-4 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"/>
          </div>
          <button @click.stop="exportCsv"
            class="flex items-center gap-1.5 px-3 py-2 text-xs text-text-muted hover:text-text-primary bg-surface-raised border border-border rounded-lg hover:border-accent/50 transition-colors flex-shrink-0">
            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
            </svg>
            Export CSV
          </button>
        </div>

        <!-- Stage tabs -->
        <div class="flex items-end gap-0 overflow-x-auto">
          <button v-for="tab in STAGE_TABS" :key="tab"
            @click="filterStage = tab === 'All' ? 'All' : (tab as ApplicationStage)"
            class="px-3 py-2 text-xs font-medium whitespace-nowrap border-b-2 transition-colors flex-shrink-0"
            :class="filterStage === tab ? 'border-accent text-accent' : 'border-transparent text-text-muted hover:text-text-primary'">
            {{ tab }} <span class="ml-1 opacity-60">{{ stageCounts[tab] ?? 0 }}</span>
          </button>
        </div>
      </div>

      <!-- Table -->
      <div class="px-6 py-4">
        <div v-if="loading" class="space-y-2 animate-pulse">
          <div v-for="i in 8" :key="i" class="h-14 bg-surface-raised border border-border rounded-lg"/>
        </div>

        <div v-else-if="sorted.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
          <p class="text-4xl mb-3">🔍</p>
          <p class="text-text-secondary font-medium">No companies found</p>
        </div>

        <div v-else>
          <!-- Header -->
          <div class="grid grid-cols-[2.5rem_1fr_7rem_9rem_7rem_6rem_7rem_2rem] gap-3 px-3 pb-2 text-[10px] font-semibold text-text-muted uppercase tracking-wider select-none">
            <div/>
            <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('name')">
              Company / Role <span class="opacity-40 font-mono">{{ sortIcon('name') }}</span>
            </button>
            <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('salary')">
              Salary <span class="opacity-40 font-mono">{{ sortIcon('salary') }}</span>
            </button>
            <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('stage')">
              Stage <span class="opacity-40 font-mono">{{ sortIcon('stage') }}</span>
            </button>
            <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('applied_date')">
              Applied <span class="opacity-40 font-mono">{{ sortIcon('applied_date') }}</span>
            </button>
            <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('match_pct')">
              Match <span class="opacity-40 font-mono">{{ sortIcon('match_pct') }}</span>
            </button>
            <div>Verdict</div>
            <div/>
          </div>

          <!-- Rows -->
          <div v-for="c in sorted" :key="c.name" class="relative">
            <!-- Follow-up left border indicator -->
            <div v-if="isFollowUpDue(c)" class="absolute left-0 top-1 bottom-1 w-0.5 rounded-full bg-amber-400 z-10"/>

            <div @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
              class="grid grid-cols-[2.5rem_1fr_7rem_9rem_7rem_6rem_7rem_2rem] gap-3 items-center px-3 py-2.5 rounded-lg border border-transparent cursor-pointer transition-all group"
              :class="[
                stageUpdating === c.name ? 'opacity-50 pointer-events-none' : '',
                isFollowUpDue(c)
                  ? 'hover:bg-amber-500/5 hover:border-amber-500/20'
                  : 'hover:bg-surface-raised hover:border-border'
              ]">

              <CompanyAvatar :name="c.name" size="sm"/>

              <!-- Company + role -->
              <div class="min-w-0">
                <div class="flex items-center gap-1.5 min-w-0">
                  <p class="text-sm font-semibold text-text-primary truncate group-hover:text-accent transition-colors">{{ c.name }}</p>
                  <span v-if="c.has_report" class="w-1.5 h-1.5 rounded-full bg-emerald-500 flex-shrink-0" title="Has report"/>
                  <span v-if="(c.interviews?.length ?? 0) > 0" class="text-[10px] text-violet-400 flex-shrink-0 font-medium">{{ c.interviews.length }}✦</span>
                  <span v-if="isFollowUpDue(c)" class="text-[10px] text-amber-400 flex-shrink-0" title="Follow-up due">⏰</span>
                  <!-- Job posting link — always visible when URL exists -->
                  <a v-if="c.job_url" :href="c.job_url" target="_blank" @click.stop
                    class="flex-shrink-0 text-text-muted hover:text-blue-400 transition-colors"
                    title="Open job posting">
                    <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/>
                    </svg>
                  </a>
                </div>
                <p v-if="roleStr(c.role)" class="text-xs text-text-muted truncate mt-0.5">{{ roleStr(c.role) }}</p>
              </div>

              <!-- Salary -->
              <span class="text-xs font-mono text-text-secondary">{{ fmtSalary(c) || '—' }}</span>

              <!-- Stage -->
              <div class="flex items-center gap-1.5 min-w-0">
                <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', STAGE_COLORS[c.stage as ApplicationStage]?.dot]"/>
                <span :class="['text-xs font-medium truncate', STAGE_COLORS[c.stage as ApplicationStage]?.text]">{{ c.stage }}</span>
              </div>

              <!-- Applied date -->
              <span class="text-xs text-text-muted font-mono">{{ c.applied_date || '—' }}</span>

              <!-- Match % -->
              <div v-if="c.match_pct !== null" class="flex items-center gap-1.5">
                <div class="flex-1 h-1 bg-surface-overlay rounded-full overflow-hidden">
                  <div :class="['h-full rounded-full', matchPctBar(c.match_pct)]" :style="{ width: `${c.match_pct}%` }"/>
                </div>
                <span :class="['text-xs font-mono font-bold w-7 text-right flex-shrink-0', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
              </div>
              <span v-else class="text-xs text-text-muted">—</span>

              <!-- Verdict -->
              <RecommendBadge :recommend="c.recommend" size="sm"/>

              <!-- Actions trigger -->
              <button @click="toggleActions(c.name, $event)"
                class="w-6 h-6 flex items-center justify-center rounded-md text-text-muted hover:text-text-primary hover:bg-surface-overlay transition-colors opacity-0 group-hover:opacity-100"
                :class="activeActionRow === c.name ? '!opacity-100 bg-surface-overlay text-text-primary' : ''">
                <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24">
                  <circle cx="12" cy="5" r="1.5"/><circle cx="12" cy="12" r="1.5"/><circle cx="12" cy="19" r="1.5"/>
                </svg>
              </button>
            </div>

            <!-- Action dropdown -->
            <Transition enter-active-class="transition-all duration-100 ease-out" enter-from-class="opacity-0 scale-95" enter-to-class="opacity-100 scale-100"
              leave-active-class="transition-all duration-75 ease-in" leave-from-class="opacity-100 scale-100" leave-to-class="opacity-0 scale-95">
              <div v-if="activeActionRow === c.name" @click.stop
                class="absolute right-0 top-11 z-30 bg-surface-raised border border-border rounded-xl shadow-2xl p-1.5 w-52 origin-top-right">

                <button @click="openDetail(c, $event)"
                  class="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-text-secondary hover:text-text-primary hover:bg-surface-overlay rounded-lg transition-colors">
                  <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
                  Open Detail
                </button>

                <button v-if="c.job_url" @click="openUrl(c, $event)"
                  class="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-text-secondary hover:text-text-primary hover:bg-surface-overlay rounded-lg transition-colors">
                  <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/></svg>
                  Job Posting
                </button>

                <button v-if="c.recruiter?.email" @click="copyEmail(c.recruiter.email, $event)"
                  class="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-text-secondary hover:text-text-primary hover:bg-surface-overlay rounded-lg transition-colors">
                  <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg>
                  <span class="truncate">Copy · {{ c.recruiter.email }}</span>
                </button>

                <div class="border-t border-border my-1"/>

                <p class="px-3 py-1 text-[10px] font-semibold text-text-muted uppercase tracking-wider">Move to stage</p>
                <button v-for="stage in QUICK_STAGES" :key="stage"
                  @click="quickStage(c, stage, $event)"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-xs rounded-lg transition-colors"
                  :class="c.stage === stage ? 'text-accent bg-accent/10 font-medium' : 'text-text-secondary hover:text-text-primary hover:bg-surface-overlay'">
                  <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', STAGE_COLORS[stage as ApplicationStage]?.dot]"/>
                  {{ stage }}
                  <svg v-if="c.stage === stage" class="w-3 h-3 ml-auto text-accent" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                </button>
              </div>
            </Transition>
          </div>
        </div>

        <p class="text-xs text-text-muted mt-4 px-3">{{ sorted.length }} of {{ companies.length }} companies</p>
      </div>
    </div>
  </div>
</template>
