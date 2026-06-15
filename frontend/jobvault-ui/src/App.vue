<script setup lang="ts">
import AppSidebar from '@/components/layout/AppSidebar.vue'
</script>

<template>
  <div class="flex h-screen overflow-hidden bg-surface">
    <AppSidebar />
    <main class="flex-1 overflow-hidden">
      <router-view />
    </main>
  </div>
</template>
