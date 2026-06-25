import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/',               redirect: '/dashboard' },
    { path: '/login',          name: 'login',        component: () => import('@/views/LoginView.vue'),      meta: { public: true } },
    { path: '/dashboard',      name: 'dashboard',    component: DashboardView },
    { path: '/applications',   name: 'applications', component: () => import('@/views/ApplicationsView.vue') },
    { path: '/company/:name',  name: 'company',      component: () => import('@/views/CompanyDetailView.vue') },
    { path: '/pipeline',       name: 'pipeline',     component: () => import('@/views/PipelineView.vue') },
    { path: '/interviews',     name: 'interviews',   component: () => import('@/views/InterviewsView.vue') },
    { path: '/skills-gap',     name: 'skills-gap',   component: () => import('@/views/SkillsGapView.vue') },
    { path: '/historical',     name: 'historical',   component: () => import('@/views/HistoricalView.vue') },
    { path: '/job-queue',      name: 'job-queue',    component: () => import('@/views/JobQueueView.vue') },
    { path: '/settings',       name: 'settings',     component: () => import('@/views/SettingsView.vue') },
    { path: '/:pathMatch(.*)*', redirect: '/dashboard' },
  ],
})

router.beforeEach((to, _from, next) => {
  const isAuth   = !!localStorage.getItem('jv_token')
  const isPublic = to.meta.public === true
  if (!isAuth && !isPublic) next('/login')
  else if (isAuth && to.path === '/login') next('/dashboard')
  else next()
})

export default router
