import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/dashboard' },
    { path: '/dashboard',        name: 'dashboard',    component: DashboardView },
    { path: '/applications',     name: 'applications', component: () => import('@/views/ApplicationsView.vue') },
    { path: '/company/:name',    name: 'company',      component: () => import('@/views/CompanyDetailView.vue') },
    { path: '/pipeline',         name: 'pipeline',     component: () => import('@/views/PipelineView.vue') },
    { path: '/interviews',       name: 'interviews',   component: () => import('@/views/InterviewsView.vue') },
    { path: '/:pathMatch(.*)*',  redirect: '/dashboard' },
  ],
})
export default router
