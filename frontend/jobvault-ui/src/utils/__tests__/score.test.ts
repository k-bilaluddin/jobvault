import { describe, it, expect } from 'vitest'
import { recommendColor, matchPctColor, matchPctBar, STAGE_COLORS, OUTCOME_COLORS } from '../score'

describe('recommendColor', () => {
  it('returns emerald for Strong Apply', () => {
    const result = recommendColor('Strong Apply')
    expect(result.ring).toBe('#22c55e')
    expect(result.bar).toBe('bg-emerald-500')
  })

  it('returns amber for Moderate Apply', () => {
    const result = recommendColor('Moderate Apply')
    expect(result.ring).toBe('#f59e0b')
    expect(result.bar).toBe('bg-amber-500')
  })

  it('returns red for Risky Apply', () => {
    const result = recommendColor('Risky Apply')
    expect(result.ring).toBe('#ef4444')
    expect(result.bar).toBe('bg-red-500')
  })

  it('returns slate for Skip', () => {
    const result = recommendColor('Skip')
    expect(result.ring).toBe('#64748b')
  })

  it('returns default slate for unknown values', () => {
    const result = recommendColor('Unknown')
    expect(result.ring).toBe('#475569')
    expect(result.bar).toBe('bg-slate-600')
  })

  it('returns default for empty string', () => {
    expect(recommendColor('').ring).toBe('#475569')
  })
})

describe('matchPctColor', () => {
  it('returns muted for null', () => {
    expect(matchPctColor(null)).toBe('text-text-muted')
  })

  it('returns emerald for 80+', () => {
    expect(matchPctColor(80)).toBe('text-emerald-400')
    expect(matchPctColor(100)).toBe('text-emerald-400')
  })

  it('returns blue for 65-79', () => {
    expect(matchPctColor(65)).toBe('text-blue-400')
    expect(matchPctColor(79)).toBe('text-blue-400')
  })

  it('returns amber for 50-64', () => {
    expect(matchPctColor(50)).toBe('text-amber-400')
    expect(matchPctColor(64)).toBe('text-amber-400')
  })

  it('returns red for below 50', () => {
    expect(matchPctColor(49)).toBe('text-red-400')
    expect(matchPctColor(0)).toBe('text-red-400')
  })
})

describe('matchPctBar', () => {
  it('returns slate for null', () => {
    expect(matchPctBar(null)).toBe('bg-slate-600')
  })

  it('returns emerald for 80+', () => {
    expect(matchPctBar(80)).toBe('bg-emerald-500')
  })

  it('returns blue for 65-79', () => {
    expect(matchPctBar(65)).toBe('bg-blue-500')
  })

  it('returns amber for 50-64', () => {
    expect(matchPctBar(50)).toBe('bg-amber-500')
  })

  it('returns red for below 50', () => {
    expect(matchPctBar(49)).toBe('bg-red-500')
  })
})

describe('STAGE_COLORS', () => {
  it('has entries for all 8 visible stages', () => {
    const stages = [
      'Not Interested', 'Archived', 'Researching', 'Ready to Apply',
      'Applied', 'Interview', 'Offer', 'Rejected',
    ] as const
    for (const stage of stages) {
      expect(STAGE_COLORS[stage]).toBeDefined()
      expect(STAGE_COLORS[stage].dot).toBeTruthy()
      expect(STAGE_COLORS[stage].text).toBeTruthy()
      expect(STAGE_COLORS[stage].bg).toBeTruthy()
    }
  })

  it('Applied uses emerald', () => {
    expect(STAGE_COLORS['Applied'].dot).toContain('emerald')
  })

  it('Rejected uses red', () => {
    expect(STAGE_COLORS['Rejected'].dot).toContain('red')
  })
})

describe('OUTCOME_COLORS', () => {
  it('has Pass, Fail, Pending', () => {
    expect(OUTCOME_COLORS['Pass']).toContain('emerald')
    expect(OUTCOME_COLORS['Fail']).toContain('red')
    expect(OUTCOME_COLORS['Pending']).toContain('amber')
  })
})
