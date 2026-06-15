<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import { useCompanies } from '@/composables/useCompanies'
import { STAGE_COLORS, matchPctColor } from '@/utils/score'
import type { ApplicationStage } from '@/types'

const route = useRoute()
const { companies, search: sidebarSearch } = useCompanies()

const navItems = [
  { name: 'Dashboard',    to: '/dashboard',    icon: 'grid'     },
  { name: 'Applications', to: '/applications', icon: 'briefcase', badge: () => companies.value.length },
  { name: 'Pipeline',     to: '/pipeline',     icon: 'list'     },
  { name: 'Interviews',   to: '/interviews',   icon: 'calendar' },
]

const activeCompanies = computed(() =>
  companies.value
    .filter(c => !['Not Interested','Archived'].includes(c.stage))
    .slice(0, 12)
)

function isActive(path: string) {
  return route.path === path || route.path.startsWith(path + '/')
}
</script>

<template>
  <aside class="w-52 flex-shrink-0 flex flex-col h-full bg-surface-raised border-r border-border">
    <div class="px-4 pt-5 pb-3">
      <span class="text-lg font-bold"><span class="text-text-primary">Job</span><span class="text-accent">Vault</span></span>
      <p class="text-[10px] text-text-muted mt-0.5 tracking-widest uppercase">Bilal · Frankfurt · 2026</p>
    </div>

    <nav class="px-2 space-y-0.5">
      <router-link v-for="item in navItems" :key="item.to" :to="item.to"
        class="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-colors"
        :class="isActive(item.to) ? 'bg-accent/15 text-accent font-medium' : 'text-text-secondary hover:bg-surface-overlay hover:text-text-primary'">
        <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
          <path v-if="item.icon==='grid'"      stroke-linecap="round" stroke-linejoin="round" d="M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z"/>
          <path v-if="item.icon==='briefcase'" stroke-linecap="round" stroke-linejoin="round" d="M20 7H4a2 2 0 00-2 2v10a2 2 0 002 2h16a2 2 0 002-2V9a2 2 0 00-2-2zM16 7V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v2"/>
          <path v-if="item.icon==='list'"      stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
          <path v-if="item.icon==='calendar'"  stroke-linecap="round" stroke-linejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
        </svg>
        <span class="flex-1 truncate">{{ item.name }}</span>
        <span v-if="item.badge" class="text-[10px] font-semibold px-1.5 py-0.5 rounded-md"
          :class="isActive(item.to) ? 'bg-accent text-white' : 'bg-surface-overlay text-text-muted'">
          {{ item.badge() }}
        </span>
      </router-link>
    </nav>

    <div class="flex-1 overflow-y-auto mt-3">
      <div class="px-4 mb-2">
        <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-2">Companies</p>
        <div class="relative">
          <input v-model="sidebarSearch" type="text" placeholder="Search..."
            class="w-full bg-surface-overlay border border-border text-text-secondary text-xs rounded-lg px-3 py-1.5 pl-7 outline-none focus:border-accent transition-colors placeholder:text-text-muted"/>
          <svg class="w-3 h-3 text-text-muted absolute left-2.5 top-1/2 -translate-y-1/2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
          </svg>
        </div>
      </div>

      <div class="px-2 space-y-0.5">
        <router-link v-for="c in activeCompanies" :key="c.name"
          :to="`/company/${encodeURIComponent(c.name)}`"
          class="flex items-center gap-2.5 px-2 py-1.5 rounded-lg hover:bg-surface-overlay transition-colors group"
          :class="route.params.name === encodeURIComponent(c.name) ? 'bg-surface-overlay' : ''">
          <CompanyAvatar :name="c.name" size="sm"/>
          <div class="flex-1 min-w-0">
            <p class="text-xs font-medium text-text-primary truncate leading-tight">{{ c.name }}</p>
            <div class="flex items-center gap-1 mt-0.5">
              <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', STAGE_COLORS[c.stage as ApplicationStage]?.dot ?? 'bg-slate-500']"/>
              <span :class="['text-[10px] truncate', STAGE_COLORS[c.stage as ApplicationStage]?.text ?? 'text-slate-400']">{{ c.stage }}</span>
            </div>
          </div>
          <span v-if="c.match_pct !== null" :class="['text-xs font-bold font-mono flex-shrink-0', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
          <span v-else class="text-xs text-text-muted flex-shrink-0">—</span>
        </router-link>
      </div>
    </div>

    <div class="px-3 py-3 border-t border-border">
      <button class="w-full flex items-center justify-center gap-2 text-xs text-text-muted hover:text-text-primary bg-surface-overlay hover:bg-border rounded-lg px-3 py-2 transition-colors">
        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>
        Sync from GitHub
      </button>
    </div>
  </aside>
</template>
