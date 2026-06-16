import { ref, watch } from 'vue'

type Theme = 'dark' | 'light'

const STORAGE_KEY = 'jobvault-theme'
const theme = ref<Theme>('dark')

function applyTheme(t: Theme) {
  const root = document.documentElement
  root.classList.add('theme-transition')
  if (t === 'light') {
    root.classList.add('light')
  } else {
    root.classList.remove('light')
  }
  setTimeout(() => root.classList.remove('theme-transition'), 300)
}

// Init from localStorage or default to dark
const stored = localStorage.getItem(STORAGE_KEY) as Theme | null
if (stored === 'light') {
  theme.value = 'light'
  applyTheme('light')
}

watch(theme, (t) => {
  applyTheme(t)
  localStorage.setItem(STORAGE_KEY, t)
})

export function useTheme() {
  const isDark = ref(theme.value === 'dark')

  watch(theme, (t) => {
    isDark.value = t === 'dark'
  })

  function toggle() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
  }

  function setTheme(t: Theme) {
    theme.value = t
  }

  return { isDark, theme, toggle, setTheme }
}
