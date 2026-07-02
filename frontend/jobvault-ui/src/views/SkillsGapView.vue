<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import AppHeader from '@/components/layout/AppHeader.vue'
import { useRouter } from 'vue-router'
import { api } from '@/api'
const router = useRouter()

interface GapSkill {
  skill: string
  count: number
  companies: string[]
}

const gaps          = ref<GapSkill[]>([])
const reportsScanned = ref(0)
const loading       = ref(false)
const error         = ref('')
const search        = ref('')
const expandedSkill = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value   = ''
  try {
    const { data } = await api.get('/api/applications/skills-gap')
    gaps.value          = data.gaps ?? []
    reportsScanned.value = data.reports_scanned ?? 0
  } catch {
    error.value = 'Could not load skills gap data.'
  } finally {
    loading.value = false
  }
}

onMounted(load)

const filtered = computed(() => {
  if (!search.value.trim()) return gaps.value
  const q = search.value.toLowerCase()
  return gaps.value.filter(g => g.skill.toLowerCase().includes(q))
})

const maxCount = computed(() => Math.max(...gaps.value.map(g => g.count), 1))

// Colour by severity
function severityColor(count: number) {
  const ratio = count / maxCount.value
  if (ratio >= 0.7) return { bar: 'bg-red-500',    text: 'text-red-400',    bg: 'bg-red-500/10 border-red-500/20'    }
  if (ratio >= 0.4) return { bar: 'bg-amber-500',  text: 'text-amber-400',  bg: 'bg-amber-500/10 border-amber-500/20'  }
  return               { bar: 'bg-blue-500',    text: 'text-blue-400',    bg: 'bg-blue-500/10 border-blue-500/20'    }
}

// Top 3 insight
const topGaps = computed(() => gaps.value.slice(0, 3))
</script>

<template>
  <div class="flex flex-col h-full">
    <AppHeader title="Skills Gap" />
    <div class="flex-1 overflow-y-auto p-4 md:p-6 space-y-5">

      <!-- Loading -->
      <div v-if="loading" class="space-y-3 animate-pulse">
        <div v-for="i in 8" :key="i" class="h-12 bg-surface-raised border border-border rounded-xl"/>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="flex flex-col items-center justify-center py-24 text-center">
        <p class="text-3xl mb-3">⚠️</p>
        <p class="text-text-secondary font-medium">{{ error }}</p>
        <button @click="load" class="mt-4 text-xs text-accent hover:underline">Try again</button>
      </div>

      <template v-else>

        <!-- Summary cards -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-2">Unique Gaps</p>
            <p class="text-3xl font-bold text-text-primary font-mono">{{ gaps.length }}</p>
            <p class="text-xs text-text-muted mt-1">distinct skills flagged</p>
          </div>
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-2">Reports Scanned</p>
            <p class="text-3xl font-bold text-text-primary font-mono">{{ reportsScanned }}</p>
            <p class="text-xs text-text-muted mt-1">compatibility reports</p>
          </div>
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-2">Most Common Gap</p>
            <p class="text-sm font-bold text-red-400 truncate mt-1">{{ gaps[0]?.skill ?? '—' }}</p>
            <p class="text-xs text-text-muted mt-1">flagged in {{ gaps[0]?.count ?? 0 }} reports</p>
          </div>
        </div>

        <!-- Top 3 insight box -->
        <div v-if="topGaps.length" class="bg-amber-500/5 border border-amber-500/20 rounded-xl p-5">
          <div class="flex items-center gap-2 mb-3">
            <span class="text-lg">💡</span>
            <p class="text-sm font-semibold text-amber-400">Focus areas — address these in your next CV update</p>
          </div>
          <div class="flex flex-wrap gap-2">
            <span v-for="g in topGaps" :key="g.skill"
              class="text-xs px-3 py-1.5 rounded-full bg-amber-500/15 text-amber-400 border border-amber-500/30 font-medium">
              {{ g.skill }} <span class="opacity-60 ml-1">×{{ g.count }}</span>
            </span>
          </div>
        </div>

        <!-- Search -->
        <div class="relative max-w-sm">
          <svg class="w-4 h-4 text-text-muted absolute left-3 top-1/2 -translate-y-1/2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
          </svg>
          <input v-model="search" type="text" placeholder="Filter skills..."
            class="w-full bg-surface-raised border border-border rounded-lg pl-9 pr-4 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"/>
        </div>

        <!-- Gap list -->
        <div v-if="filtered.length === 0" class="text-center py-12 text-text-muted text-sm">
          No skills match "{{ search }}"
        </div>

        <div v-else class="space-y-2">
          <div v-for="(gap, i) in filtered" :key="gap.skill"
            class="bg-surface-raised border rounded-xl overflow-hidden transition-all"
            :class="severityColor(gap.count).bg">

            <!-- Row -->
            <div class="flex items-center gap-2 sm:gap-4 px-3 sm:px-4 py-3 cursor-pointer"
              @click="expandedSkill = expandedSkill === gap.skill ? null : gap.skill">
              <!-- Rank -->
              <span class="text-xs font-mono text-text-muted w-5 flex-shrink-0 text-right">{{ i + 1 }}</span>

              <!-- Skill name -->
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold text-text-primary truncate">{{ gap.skill }}</p>
                <p class="text-xs text-text-muted mt-0.5">
                  flagged in {{ gap.count }} report{{ gap.count > 1 ? 's' : '' }}
                </p>
              </div>

              <!-- Bar -->
              <div class="hidden sm:block w-32 h-1.5 bg-surface-overlay rounded-full overflow-hidden flex-shrink-0">
                <div :class="['h-full rounded-full transition-all duration-700', severityColor(gap.count).bar]"
                  :style="{ width: `${(gap.count / maxCount) * 100}%` }"/>
              </div>

              <!-- Count badge -->
              <span :class="['text-xs font-bold font-mono px-2 py-0.5 rounded-full flex-shrink-0', severityColor(gap.count).text, 'bg-surface-overlay']">
                ×{{ gap.count }}
              </span>

              <!-- Expand arrow -->
              <svg class="w-3.5 h-3.5 text-text-muted flex-shrink-0 transition-transform"
                :class="expandedSkill === gap.skill ? 'rotate-180' : ''"
                fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
              </svg>
            </div>

            <!-- Expanded companies list -->
            <div v-if="expandedSkill === gap.skill" class="px-4 pb-3 border-t border-border/50">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mt-3 mb-2">Flagged in:</p>
              <div class="flex flex-wrap gap-1.5">
                <button v-for="company in gap.companies" :key="company"
                  @click.stop="router.push(`/company/${encodeURIComponent(company)}`)"
                  class="text-xs px-2.5 py-1 rounded-lg bg-surface-overlay hover:bg-border text-text-secondary hover:text-text-primary transition-colors">
                  {{ company }}
                </button>
              </div>
            </div>
          </div>
        </div>

      </template>
    </div>
  </div>
</template>
