<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppSidebar from '@/components/layout/AppSidebar.vue'

const route = useRoute()
const isPublic = computed(() => route.meta.public === true)
</script>

<template>
  <!-- Public pages (login) — no sidebar -->
  <div v-if="isPublic" class="h-screen bg-surface">
    <router-view />
  </div>

  <!-- Authenticated pages — with sidebar -->
  <div v-else class="flex h-screen overflow-hidden bg-surface">
    <AppSidebar />
    <main class="flex-1 overflow-hidden">
      <router-view />
    </main>
  </div>
</template>
