<script setup lang="ts">
import { STAGE_COLORS } from '@/utils/score'
import type { ApplicationStage, Company } from '@/types'

defineProps<{ company: Company; open: boolean }>()
const emit = defineEmits<{
  toggle: []
  'open-detail': []
  'open-url': []
  'copy-email': []
  'quick-stage': [stage: ApplicationStage]
}>()

const QUICK_STAGES: ApplicationStage[] = ['Ready to Apply', 'Applied', 'Interview', 'Offer', 'Rejected', 'Not Interested']
</script>

<template>
  <div class="relative">
    <button @click.stop="emit('toggle')"
      class="w-6 h-6 flex items-center justify-center rounded-md text-text-muted hover:text-text-primary hover:bg-surface-overlay transition-colors"
      :class="open ? 'bg-surface-overlay text-text-primary' : 'md:opacity-0 md:group-hover:opacity-100'">
      <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24">
        <circle cx="12" cy="5" r="1.5"/><circle cx="12" cy="12" r="1.5"/><circle cx="12" cy="19" r="1.5"/>
      </svg>
    </button>

    <Transition enter-active-class="transition-all duration-100 ease-out" enter-from-class="opacity-0 scale-95" enter-to-class="opacity-100 scale-100"
      leave-active-class="transition-all duration-75 ease-in" leave-from-class="opacity-100 scale-100" leave-to-class="opacity-0 scale-95">
      <div v-if="open" @click.stop
        class="absolute right-0 top-9 z-30 bg-surface-raised border border-border rounded-xl shadow-2xl p-1.5 w-52 origin-top-right">

        <button @click="emit('open-detail')"
          class="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-text-secondary hover:text-text-primary hover:bg-surface-overlay rounded-lg transition-colors">
          <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
          Open Detail
        </button>

        <button v-if="company.job_url" @click="emit('open-url')"
          class="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-text-secondary hover:text-text-primary hover:bg-surface-overlay rounded-lg transition-colors">
          <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/></svg>
          Job Posting
        </button>

        <button v-if="company.recruiter?.email" @click="emit('copy-email')"
          class="w-full flex items-center gap-2.5 px-3 py-2 text-xs text-text-secondary hover:text-text-primary hover:bg-surface-overlay rounded-lg transition-colors">
          <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg>
          <span class="truncate">Copy · {{ company.recruiter.email }}</span>
        </button>

        <div class="border-t border-border my-1"/>

        <p class="px-3 py-1 text-[10px] font-semibold text-text-muted uppercase tracking-wider">Move to stage</p>
        <button v-for="stage in QUICK_STAGES" :key="stage"
          @click="emit('quick-stage', stage)"
          class="w-full flex items-center gap-2 px-3 py-1.5 text-xs rounded-lg transition-colors"
          :class="company.stage === stage ? 'text-accent bg-accent/10 font-medium' : 'text-text-secondary hover:text-text-primary hover:bg-surface-overlay'">
          <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', STAGE_COLORS[stage as ApplicationStage]?.dot]"/>
          {{ stage }}
          <svg v-if="company.stage === stage" class="w-3 h-3 ml-auto text-accent" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
        </button>
      </div>
    </Transition>
  </div>
</template>
