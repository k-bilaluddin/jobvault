import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { nextTick } from 'vue'
import { withSetup } from './helpers'

function makeCompany(overrides: Record<string, unknown> = {}) {
  return {
    name: 'TestCorp',
    has_report: true,
    has_notes: false,
    has_cv_pdf: false,
    has_letter_pdf: false,
    match_pct: 75,
    recommend: 'Strong Apply' as const,
    job_url: 'https://example.com',
    role: 'Engineer',
    applied: false,
    applied_date: '',
    stage: 'Ready to Apply' as const,
    personal_notes: '',
    interviews: [],
    salary: { advertised: '', target: '', discussed: '', offered: '' },
    recruiter: { name: '', email: '', linkedin: '' },
    follow_up_date: '',
    source: 'LinkedIn',
    ...overrides,
  }
}

describe('useCompanies', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  async function loadWithMock() {
    vi.stubEnv('VITE_USE_MOCK', 'true')
    const mod = await import('../useCompanies')
    return withSetup(() => mod.useCompanies())
  }

  it('returns loading state initially', async () => {
    const { result } = await loadWithMock()
    expect(result.loading.value).toBeDefined()
  })

  it('returns companies array', async () => {
    const { result } = await loadWithMock()
    expect(Array.isArray(result.companies.value)).toBe(true)
  })

  describe('filtered', () => {
    it('filters by search term', async () => {
      const { result } = await loadWithMock()
      const companies = [
        makeCompany({ name: 'Acme Corp' }),
        makeCompany({ name: 'Beta Inc' }),
        makeCompany({ name: 'Gamma LLC' }),
      ]
      result.companies.value = companies as any
      result.search.value = 'acme'
      await nextTick()
      expect(result.filtered.value).toHaveLength(1)
      expect(result.filtered.value[0].name).toBe('Acme Corp')
    })

    it('filters by stage', async () => {
      const { result } = await loadWithMock()
      const companies = [
        makeCompany({ name: 'A', stage: 'Applied' }),
        makeCompany({ name: 'B', stage: 'Interview' }),
        makeCompany({ name: 'C', stage: 'Applied' }),
      ]
      result.companies.value = companies as any
      result.filterStage.value = 'Applied'
      await nextTick()
      expect(result.filtered.value).toHaveLength(2)
    })

    it('combines search and stage filters', async () => {
      const { result } = await loadWithMock()
      const companies = [
        makeCompany({ name: 'Acme', stage: 'Applied' }),
        makeCompany({ name: 'Beta', stage: 'Applied' }),
        makeCompany({ name: 'Acme Labs', stage: 'Interview' }),
      ]
      result.companies.value = companies as any
      result.search.value = 'acme'
      result.filterStage.value = 'Applied'
      await nextTick()
      expect(result.filtered.value).toHaveLength(1)
      expect(result.filtered.value[0].name).toBe('Acme')
    })

    it('returns all when no filters active', async () => {
      const { result } = await loadWithMock()
      const companies = [makeCompany({ name: 'A' }), makeCompany({ name: 'B' })]
      result.companies.value = companies as any
      result.search.value = ''
      result.filterStage.value = 'All'
      await nextTick()
      expect(result.filtered.value).toHaveLength(2)
    })

    it('search is case-insensitive', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [makeCompany({ name: 'ACME Corp' })] as any
      result.search.value = 'acme'
      await nextTick()
      expect(result.filtered.value).toHaveLength(1)
    })
  })

  describe('getByName', () => {
    it('returns matching company', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [makeCompany({ name: 'TestCorp' })] as any
      expect(result.getByName('TestCorp')).not.toBeNull()
      expect(result.getByName('TestCorp')!.name).toBe('TestCorp')
    })

    it('returns null for unknown name', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [makeCompany({ name: 'TestCorp' })] as any
      expect(result.getByName('Unknown')).toBeNull()
    })
  })

  describe('stats', () => {
    it('computes correct totals', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ stage: 'Applied', applied: true, match_pct: 80 }),
        makeCompany({ stage: 'Interview', applied: true, match_pct: 60 }),
        makeCompany({ stage: 'Offer', applied: true, match_pct: 90 }),
        makeCompany({ stage: 'Rejected', applied: true, match_pct: 40 }),
        makeCompany({ stage: 'Not Interested', applied: false, match_pct: null }),
      ] as any
      await nextTick()
      const s = result.stats.value
      expect(s.total).toBe(5)
      expect(s.applied).toBe(4)
      expect(s.interviews).toBe(1)
      expect(s.offers).toBe(1)
      expect(s.rejected).toBe(1)
      expect(s.notInterested).toBe(1)
    })

    it('computes average match percentage', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ match_pct: 80 }),
        makeCompany({ match_pct: 60 }),
        makeCompany({ match_pct: null }),
      ] as any
      await nextTick()
      expect(result.stats.value.avgMatchPct).toBe(70)
    })

    it('returns null avgMatchPct when no companies have scores', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ match_pct: null }),
      ] as any
      await nextTick()
      expect(result.stats.value.avgMatchPct).toBeNull()
    })

    it('computes interview conversion rate', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ stage: 'Interview', applied: true }),
        makeCompany({ stage: 'Applied', applied: true }),
        makeCompany({ stage: 'Applied', applied: true }),
        makeCompany({ stage: 'Applied', applied: true }),
      ] as any
      await nextTick()
      expect(result.stats.value.interviewConversionRate).toBe(25)
    })

    it('returns 0 conversion rate when no applied', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ stage: 'Ready to Apply', applied: false }),
      ] as any
      await nextTick()
      expect(result.stats.value.interviewConversionRate).toBe(0)
    })
  })

  describe('pipelineCounts', () => {
    it('counts companies per pipeline stage', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ stage: 'Ready to Apply' }),
        makeCompany({ stage: 'Ready to Apply' }),
        makeCompany({ stage: 'Applied' }),
        makeCompany({ stage: 'Interview' }),
      ] as any
      await nextTick()
      const counts = result.pipelineCounts.value
      expect(counts.find(c => c.label === 'Ready to Apply')!.count).toBe(2)
      expect(counts.find(c => c.label === 'Applied')!.count).toBe(1)
      expect(counts.find(c => c.label === 'Interview')!.count).toBe(1)
      expect(counts.find(c => c.label === 'Offer')!.count).toBe(0)
      expect(counts.find(c => c.label === 'Rejected')!.count).toBe(0)
    })

    it('sets total to 1 when no companies to avoid division by zero', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [] as any
      await nextTick()
      expect(result.pipelineCounts.value[0].total).toBe(1)
    })
  })

  describe('scoreDistribution', () => {
    it('groups by recommendation tier', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ recommend: 'Strong Apply' }),
        makeCompany({ recommend: 'Strong Apply' }),
        makeCompany({ recommend: 'Moderate Apply' }),
        makeCompany({ recommend: '' }),
      ] as any
      await nextTick()
      const dist = result.scoreDistribution.value
      expect(dist.find(d => d.tier === 'Strong Apply')!.count).toBe(2)
      expect(dist.find(d => d.tier === 'Moderate Apply')!.count).toBe(1)
      expect(dist.find(d => d.tier === 'No Report')!.count).toBe(1)
    })
  })

  describe('withInterviews', () => {
    it('filters to only companies with interviews', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ name: 'A', interviews: [{ id: 1, date: '2026-01-01', type: 'HR', notes: '', outcome: 'Pass' }] }),
        makeCompany({ name: 'B', interviews: [] }),
        makeCompany({ name: 'C', interviews: [{ id: 2, date: '2026-02-01', type: 'Technical', notes: '', outcome: 'Pending' }] }),
      ] as any
      await nextTick()
      expect(result.withInterviews.value).toHaveLength(2)
    })

    it('sorts by newest interview date first', async () => {
      const { result } = await loadWithMock()
      result.companies.value = [
        makeCompany({ name: 'Old', interviews: [{ id: 1, date: '2026-01-01', type: 'HR', notes: '', outcome: 'Pass' }] }),
        makeCompany({ name: 'New', interviews: [{ id: 2, date: '2026-06-15', type: 'HR', notes: '', outcome: 'Pending' }] }),
      ] as any
      await nextTick()
      expect(result.withInterviews.value[0].name).toBe('New')
    })
  })
})
