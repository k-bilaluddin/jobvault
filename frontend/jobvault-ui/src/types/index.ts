// ─── Mirrors backend ApplicationStatus values ─────────────────
export type ApplicationStage =
  | 'Processing'     // Transient — async ingestion accepted, Worker not yet done
  | 'Regenerating'   // Documents being regenerated after content edit
  | 'Failed'         // Worker encountered an unrecoverable error
  | 'Not Interested'
  | 'Archived'
  | 'Researching'
  | 'Ready to Apply'
  | 'Applied'
  | 'Interview'
  | 'Offer'
  | 'Rejected'

export const PIPELINE_STAGES: ApplicationStage[] = [
  'Processing', 'Regenerating', 'Failed',
  'Not Interested', 'Archived', 'Researching',
  'Ready to Apply', 'Applied', 'Interview', 'Offer', 'Rejected',
]

export interface SkillRow {
  label: string
  value: string
}

export interface RolePayload {
  id: string
  bullets: string[]
}

export interface ApplicationContent {
  headline: string
  summary: string
  skills: SkillRow[]
  roles: RolePayload[]
  recipient: string
  coverLetterParagraphs: string[]
  strengths: string[]
  gaps: string[]
}

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
  synced_at: string        // ISO 8601 UTC — folder mtime, set by Flask on each sync
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
  notes: ApplicationNote[]
  salary: Salary
  recruiter: Recruiter
  follow_up_date: string
  source: string
  status?: string
  has_content?: boolean
}

// ─── Categorized application note ─────────────────────────────
export type NoteCategory = 'Application' | 'Interview' | 'Follow-up' | 'Rejection' | 'General'

export const NOTE_CATEGORIES: NoteCategory[] = [
  'Application', 'Interview', 'Follow-up', 'Rejection', 'General',
]

export interface ApplicationNote {
  id: number
  category: NoteCategory
  content: string
  stage: string
  pinned: boolean
  created_at: string
  updated_at: string
}

// ─── Categorized application note ─────────────────────────────
export type NoteCategory = 'Application' | 'Interview' | 'Follow-up' | 'Rejection' | 'General'

export const NOTE_CATEGORIES: NoteCategory[] = [
  'Application', 'Interview', 'Follow-up', 'Rejection', 'General',
]

export interface ApplicationNote {
  id: number
  category: NoteCategory
  content: string
  stage: string
  pinned: boolean
  created_at: string
  updated_at: string
}

// ─── In-app notification ──────────────────────────────────────
export type NotificationType = 'new_application' | 'stage_changed' | 'score_computed' | 'sync_completed'

export interface AppNotification {
  id: string
  type: NotificationType
  title: string
  body: string
  companyName: string | null
  companySlug: string | null
  occurredAt: string  // ISO 8601 UTC
  read: boolean
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
