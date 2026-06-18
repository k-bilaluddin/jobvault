import { describe, it, expect, vi, beforeEach } from 'vitest'
import { createRouter, createWebHistory } from 'vue-router'

function makeRouter() {
  const router = createRouter({
    history: createWebHistory(),
    routes: [
      { path: '/', redirect: '/dashboard' },
      { path: '/login', name: 'login', component: { template: '<div>Login</div>' }, meta: { public: true } },
      { path: '/dashboard', name: 'dashboard', component: { template: '<div>Dashboard</div>' } },
      { path: '/applications', name: 'applications', component: { template: '<div>Apps</div>' } },
      { path: '/company/:name', name: 'company', component: { template: '<div>Company</div>' } },
      { path: '/pipeline', name: 'pipeline', component: { template: '<div>Pipeline</div>' } },
      { path: '/interviews', name: 'interviews', component: { template: '<div>Interviews</div>' } },
      { path: '/skills-gap', name: 'skills-gap', component: { template: '<div>Skills</div>' } },
      { path: '/historical', name: 'historical', component: { template: '<div>Historical</div>' } },
      { path: '/:pathMatch(.*)*', redirect: '/dashboard' },
    ],
  })

  router.beforeEach((to, _from, next) => {
    const isAuth = localStorage.getItem('jv_auth') === 'true'
    const isPublic = to.meta.public === true
    if (!isAuth && !isPublic) next('/login')
    else if (isAuth && to.path === '/login') next('/dashboard')
    else next()
  })

  return router
}

describe('router', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('has expected route names', () => {
    const router = makeRouter()
    const names = router.getRoutes().map(r => r.name).filter(Boolean)
    expect(names).toContain('dashboard')
    expect(names).toContain('login')
    expect(names).toContain('applications')
    expect(names).toContain('company')
    expect(names).toContain('pipeline')
    expect(names).toContain('interviews')
    expect(names).toContain('skills-gap')
    expect(names).toContain('historical')
  })

  it('login route is marked as public', () => {
    const router = makeRouter()
    const loginRoute = router.getRoutes().find(r => r.name === 'login')
    expect(loginRoute?.meta.public).toBe(true)
  })

  it('dashboard route is not public', () => {
    const router = makeRouter()
    const dashboardRoute = router.getRoutes().find(r => r.name === 'dashboard')
    expect(dashboardRoute?.meta.public).toBeFalsy()
  })

  it('company route has name param', () => {
    const router = makeRouter()
    const companyRoute = router.getRoutes().find(r => r.name === 'company')
    expect(companyRoute?.path).toContain(':name')
  })

  describe('auth guard', () => {
    it('redirects unauthenticated users to login', async () => {
      const router = makeRouter()
      localStorage.removeItem('jv_auth')
      await router.push('/dashboard')
      expect(router.currentRoute.value.path).toBe('/login')
    })

    it('allows authenticated users to access dashboard', async () => {
      const router = makeRouter()
      localStorage.setItem('jv_auth', 'true')
      await router.push('/dashboard')
      expect(router.currentRoute.value.path).toBe('/dashboard')
    })

    it('redirects authenticated users away from login', async () => {
      const router = makeRouter()
      localStorage.setItem('jv_auth', 'true')
      await router.push('/login')
      expect(router.currentRoute.value.path).toBe('/dashboard')
    })

    it('allows unauthenticated users to access login', async () => {
      const router = makeRouter()
      localStorage.removeItem('jv_auth')
      await router.push('/login')
      expect(router.currentRoute.value.path).toBe('/login')
    })

    it('redirects unknown paths to dashboard (if authed)', async () => {
      const router = makeRouter()
      localStorage.setItem('jv_auth', 'true')
      await router.push('/nonexistent-route')
      expect(router.currentRoute.value.path).toBe('/dashboard')
    })
  })
})
