import type { ApplicationStage } from '@/types'

export function recommendColor(rec: string): { bg: string; text: string; bar: string; ring: string } {
  switch (rec) {
    case 'Strong Apply':   return { bg: 'bg-emerald-500/15', text: 'text-emerald-400', bar: 'bg-emerald-500', ring: '#22c55e' }
    case 'Moderate Apply': return { bg: 'bg-amber-500/15',   text: 'text-amber-400',   bar: 'bg-amber-500',   ring: '#f59e0b' }
    case 'Risky Apply':    return { bg: 'bg-red-500/15',     text: 'text-red-400',     bar: 'bg-red-500',     ring: '#ef4444' }
    case 'Skip':           return { bg: 'bg-slate-500/15',   text: 'text-slate-400',   bar: 'bg-slate-500',   ring: '#64748b' }
    default:               return { bg: 'bg-slate-500/10',   text: 'text-slate-500',   bar: 'bg-slate-600',   ring: '#475569' }
  }
}

export function matchPctColor(pct: number | null): string {
  if (pct === null) return 'text-text-muted'
  if (pct >= 80) return 'text-emerald-400'
  if (pct >= 65) return 'text-blue-400'
  if (pct >= 50) return 'text-amber-400'
  return 'text-red-400'
}

export function matchPctBar(pct: number | null): string {
  if (pct === null) return 'bg-slate-600'
  if (pct >= 80) return 'bg-emerald-500'
  if (pct >= 65) return 'bg-blue-500'
  if (pct >= 50) return 'bg-amber-500'
  return 'bg-red-500'
}

export const STAGE_COLORS: Record<ApplicationStage, { dot: string; text: string; bg: string }> = {
  'Processing':     { dot: 'bg-blue-400',   text: 'text-blue-400',   bg: 'bg-blue-400/15'   },
  'Failed':         { dot: 'bg-red-600',    text: 'text-red-500',    bg: 'bg-red-600/15'    },
  'Not Interested': { dot: 'bg-slate-500',  text: 'text-slate-400',  bg: 'bg-slate-500/15'  },
  'Archived':       { dot: 'bg-slate-600',  text: 'text-slate-500',  bg: 'bg-slate-600/15'  },
  'Researching':    { dot: 'bg-sky-400',    text: 'text-sky-400',    bg: 'bg-sky-400/15'    },
  'Ready to Apply': { dot: 'bg-sky-400',    text: 'text-sky-400',    bg: 'bg-sky-400/15'    },
  'Applied':        { dot: 'bg-emerald-400',text: 'text-emerald-400',bg: 'bg-emerald-500/15'},
  'Interview':      { dot: 'bg-violet-400', text: 'text-violet-400', bg: 'bg-violet-500/15' },
  'Offer':          { dot: 'bg-amber-400',  text: 'text-amber-400',  bg: 'bg-amber-500/15'  },
  'Rejected':       { dot: 'bg-red-400',    text: 'text-red-400',    bg: 'bg-red-500/15'    },
}

export const OUTCOME_COLORS: Record<string, string> = {
  Pass:    'text-emerald-400 bg-emerald-500/15',
  Fail:    'text-red-400 bg-red-500/15',
  Pending: 'text-amber-400 bg-amber-500/15',
}
