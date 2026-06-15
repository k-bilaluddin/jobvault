// ─── Mirrors Flask PIPELINE_STAGES exactly ────────────────────
export type ApplicationStage =
  | 'Not Interested'
  | 'Archived'
  | 'Researching'
  | 'Ready to Apply'
  | 'Applied'
  | 'Interview'
  | 'Offer'
  | 'Rejected'

export const PIPELINE_STAGES: ApplicationStage[] = [
  'Not Interested', 'Archived', 'Researching',
  'Ready to Apply', 'Applied', 'Interview', 'Offer', 'Rejected',
]

// Mirrors tracker_data.json interview shape
export interface Interview {
  id: number
  date: string
  type: 'HR' | 'Technical' | 'Onsite' | 'Final' | 'Phone' | string
  notes: string
  outcome: 'Pass' | 'Fail' | 'Pending' | string
}

// Mirrors tracker_data.json salary shape
export interface Salary {
  advertised: string
  target: string
  discussed: string
  offered: string
}

// Mirrors tracker_data.json recruiter shape
export interface Recruiter {
  name: string
  email: string
  linkedin: string
}

// ─── Full company record — mirrors Flask get_companies() response ──
export interface Company {
  name: string
  // From folder scan
  has_report: boolean
  has_notes: boolean
  has_cv_pdf: boolean
  has_letter_pdf: boolean
  // Parsed from compatibility-report.md
  match_pct: number | null
  recommend: 'Strong Apply' | 'Moderate Apply' | 'Risky Apply' | 'Skip' | ''
  job_url: string
  role: string | string[]   // Flask returns array until fix applied, handle both
  // From tracker_data.json
  applied: boolean
  applied_date: string
  stage: ApplicationStage
  personal_notes: string
  interviews: Interview[]
  salary: Salary
  recruiter: Recruiter
  follow_up_date: string
  source: string
}

// ─── Dashboard stats (computed from Company[]) ────────────────
export interface DashboardStats {
  total: number
  applied: number
  interviews: number
  offers: number
  rejected: number
  notInterested: number
  archived: number
  avgMatchPct: number | null
  interviewConversionRate: number
}
