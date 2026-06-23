import axios from 'axios'

export const API_BASE = import.meta.env.VITE_API_BASE ?? 'https://api.kbilaluddin.dev'

export const api = axios.create({ baseURL: API_BASE })

// Attach JWT token to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('jv_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Extract Problem Details error and redirect on 401
api.interceptors.response.use(
  res => res,
  err => {
    const data = err.response?.data
    if (data?.code) {
      err.errorCode = data.code as string
      err.problemDetails = data as ProblemDetails
    }

    if (err.response?.status === 401 && window.location.pathname !== '/login') {
      localStorage.removeItem('jv_token')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

export interface ProblemDetails {
  type: string
  title: string
  status: number
  detail: string
  code: string
  traceId: string
  instance: string
}

const userMessages: Record<string, string> = {
  'auth.invalid_credentials': 'Wrong email or password. Try again.',
  'application.not_found': 'Application not found.',
  'vault.file_not_found': 'This file is not available yet.',
  'vault.company_required': 'Company name is required.',
  'vault.no_files_uploaded': 'Please upload at least one file.',
  'vault.empty_files': 'All uploaded files are empty.',
}

const fallbackMessage = 'Something went wrong. Please try again later.'

export function getUserMessage(code?: string): string {
  return (code && userMessages[code]) || fallbackMessage
}
