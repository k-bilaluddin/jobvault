import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_BASE ?? 'https://api.kbilaluddin.dev'

export const api = axios.create({ baseURL: API_BASE })

// Attach JWT token to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('jv_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Redirect to login on 401
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('jv_token')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)
