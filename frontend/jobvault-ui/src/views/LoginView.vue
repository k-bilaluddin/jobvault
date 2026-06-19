<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useTheme } from '@/composables/useTheme'
import { api } from '@/api'

const router  = useRouter()
const { isDark, toggle } = useTheme()

const email    = ref('')
const password = ref('')
const error    = ref('')
const loading  = ref(false)
const showPass = ref(false)

async function login() {
  error.value   = ''
  loading.value = true

  try {
    const res = await api.post<{ token: string }>('/api/auth/login', {
      email: email.value,
      password: password.value,
    })
    localStorage.setItem('jv_token', res.data.token)
    router.push('/dashboard')
  } catch {
    error.value = 'Invalid email or password'
  } finally {
    loading.value = false
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') login()
}
</script>

<template>
  <div class="min-h-screen bg-surface flex flex-col">

    <!-- Theme toggle top right -->
    <div class="absolute top-4 right-4">
      <button @click="toggle"
        class="p-2 rounded-lg text-text-muted hover:text-text-primary hover:bg-surface-raised transition-colors">
        <svg v-if="isDark" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"/>
        </svg>
        <svg v-else class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"/>
        </svg>
      </button>
    </div>

    <!-- Centred card -->
    <div class="flex-1 flex items-center justify-center px-4">
      <div class="w-full max-w-sm">

        <!-- Logo -->
        <div class="text-center mb-8">
          <h1 class="text-3xl font-bold">
            <span class="text-text-primary">Job</span><span class="text-accent">Vault</span>
          </h1>
          <p class="text-text-muted text-sm mt-2">Sign in to your workspace</p>
        </div>

        <!-- Card -->
        <div class="bg-surface-raised border border-border rounded-2xl p-7 shadow-xl">

          <!-- Error -->
          <div v-if="error"
            class="flex items-center gap-2.5 px-3.5 py-3 bg-red-500/10 border border-red-500/30 rounded-xl mb-5">
            <svg class="w-4 h-4 text-red-400 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            <p class="text-red-400 text-sm">{{ error }}</p>
          </div>

          <!-- Email -->
          <div class="space-y-4">
            <div>
              <label class="block text-xs font-semibold text-text-secondary mb-1.5">Email</label>
              <div class="relative">
                <svg class="w-4 h-4 text-text-muted absolute left-3 top-1/2 -translate-y-1/2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207"/>
                </svg>
                <input
                  v-model="email"
                  type="email"
                  placeholder="you@jobvault.dev"
                  autocomplete="email"
                  @keydown="onKeydown"
                  class="w-full bg-surface-overlay border border-border rounded-xl pl-10 pr-4 py-2.5 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"
                  :class="error ? 'border-red-500/50' : ''"
                />
              </div>
            </div>

            <!-- Password -->
            <div>
              <label class="block text-xs font-semibold text-text-secondary mb-1.5">Password</label>
              <div class="relative">
                <svg class="w-4 h-4 text-text-muted absolute left-3 top-1/2 -translate-y-1/2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
                </svg>
                <input
                  v-model="password"
                  :type="showPass ? 'text' : 'password'"
                  placeholder="••••••••"
                  autocomplete="current-password"
                  @keydown="onKeydown"
                  class="w-full bg-surface-overlay border border-border rounded-xl pl-10 pr-10 py-2.5 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"
                  :class="error ? 'border-red-500/50' : ''"
                />
                <!-- Show/hide toggle -->
                <button @click="showPass = !showPass" type="button"
                  class="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted hover:text-text-primary transition-colors">
                  <svg v-if="!showPass" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                  </svg>
                  <svg v-else class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <!-- Sign in button -->
          <button
            @click="login"
            :disabled="loading || !email || !password"
            class="w-full mt-6 py-2.5 rounded-xl text-sm font-semibold transition-all duration-150 flex items-center justify-center gap-2"
            :class="loading || !email || !password
              ? 'bg-accent/40 text-white/50 cursor-not-allowed'
              : 'bg-accent hover:bg-accent/90 text-white shadow-lg shadow-accent/20'">
            <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>

        </div>

        <!-- Footer -->
        <p class="text-center text-xs text-text-muted mt-6">
          JobVault · Bilal · Frankfurt · 2026
        </p>

      </div>
    </div>
  </div>
</template>
