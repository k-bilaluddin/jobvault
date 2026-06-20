import { ref, computed, onMounted } from 'vue'
import type { Company, ApplicationStage, DashboardStats } from '@/types'
import { mockCompanies } from '@/mocks/companies'
import { api, FLASK_API_BASE } from '@/api'

const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'

// Singleton cache — loaded once, shared across composable instances
const _companies = ref<Company[]>([])
const _loaded = ref(false) // eslint-disable-line
const _loading = ref(false)
const _error = ref<string | null>(null)

async function loadCompanies() {
  if (_loaded.value) return
  _loading.value = true
  _error.value = null
  try {
    if (USE_MOCK) {
      await new Promise(r => setTimeout(r, 350))
      _companies.value = mockCompanies
    } else {
      // Fetch from both .NET API (MongoDB) and Flask (vault), merge results
      const [dotnetRes, flaskRes] = await Promise.allSettled([
        api.get<Company[]>('/api/applications'),
        fetch(`${FLASK_API_BASE}/api/companies`),
      ])

      const dotnetApps: Company[] = dotnetRes.status === 'fulfilled'
        ? dotnetRes.value.data
        : []

      let flaskApps: Company[] = []
      if (flaskRes.status === 'fulfilled' && flaskRes.value.ok) {
        flaskApps = await flaskRes.value.json()
      }

      // Merge: .NET is primary, Flask fills gaps for vault-only apps
      const byName = new Map<string, Company>()
      for (const app of dotnetApps) byName.set(app.name, app)
      for (const app of flaskApps) {
        if (!byName.has(app.name)) byName.set(app.name, app)
      }

      _companies.value = Array.from(byName.values())
    }
    _loaded.value = true
  } catch (e: unknown) {
    _error.value = e instanceof Error ? e.message : 'Unknown error'
  } finally {
    _loading.value = false
  }
}

export async function forceRefreshCompanies() {
  _loaded.value = false
  await loadCompanies()
}

export function useCompanies() {
  const search = ref('')
  const filterStage = ref<ApplicationStage | 'All'>('All')

  onMounted(loadCompanies)

  const filtered = computed(() => {
    let list = _companies.value
    if (filterStage.value !== 'All') {
      list = list.filter(c => c.stage === filterStage.value)
    }
    if (search.value.trim()) {
      const q = search.value.toLowerCase()
      list = list.filter(c => c.name.toLowerCase().includes(q))
    }
    return list
  })

  const getByName = (name: string) =>
    _companies.value.find(c => c.name === name) ?? null

  // Dashboard stats computed from real data — mirrors Flask logic
  const stats = computed<DashboardStats>(() => {
    const all = _companies.value
    const withPct = all.filter(c => c.match_pct !== null)
    const applied = all.filter(c => c.applied)
    const interviewStage = all.filter(c => c.stage === 'Interview')
    return {
      total:                 all.length,
      applied:               applied.length,
      interviews:            interviewStage.length,
      offers:                all.filter(c => c.stage === 'Offer').length,
      rejected:              all.filter(c => c.stage === 'Rejected').length,
      notInterested:         all.filter(c => c.stage === 'Not Interested').length,
      archived:              all.filter(c => c.stage === 'Archived').length,
      avgMatchPct:           withPct.length
        ? Math.round(withPct.reduce((s, c) => s + (c.match_pct ?? 0), 0) / withPct.length)
        : null,
      interviewConversionRate: applied.length
        ? Math.round((interviewStage.length / applied.length) * 100)
        : 0,
    }
  })

  // Pipeline funnel counts
  const pipelineCounts = computed(() => {
    const all = _companies.value
    const total = all.length || 1
    return [
      { label: 'Ready to Apply', count: all.filter(c => c.stage === 'Ready to Apply').length, total },
      { label: 'Applied',        count: all.filter(c => c.stage === 'Applied').length, total },
      { label: 'Interview',      count: all.filter(c => c.stage === 'Interview').length, total },
      { label: 'Offer',          count: all.filter(c => c.stage === 'Offer').length, total },
      { label: 'Rejected',       count: all.filter(c => c.stage === 'Rejected').length, total },
    ]
  })

  // Score distribution by recommend tier
  const scoreDistribution = computed(() => {
    const all = _companies.value
    return [
      { tier: 'Strong Apply',   count: all.filter(c => c.recommend === 'Strong Apply').length },
      { tier: 'Moderate Apply', count: all.filter(c => c.recommend === 'Moderate Apply').length },
      { tier: 'Risky Apply',    count: all.filter(c => c.recommend === 'Risky Apply').length },
      { tier: 'Skip',           count: all.filter(c => c.recommend === 'Skip').length },
      { tier: 'No Report',      count: all.filter(c => !c.recommend).length },
    ]
  })

  // Companies with interviews (for Interviews view)
  const withInterviews = computed(() =>
    _companies.value.filter(c => c.interviews && c.interviews.length > 0)
      .sort((a, b) => {
        const aDate = a.interviews[0]?.date ?? ''
        const bDate = b.interviews[0]?.date ?? ''
        return bDate.localeCompare(aDate)
      })
  )

  return {
    companies: _companies,
    filtered,
    loading: _loading,
    error: _error,
    search,
    filterStage,
    getByName,
    stats,
    pipelineCounts,
    scoreDistribution,
    withInterviews,
    refresh: forceRefreshCompanies,
  }
}
