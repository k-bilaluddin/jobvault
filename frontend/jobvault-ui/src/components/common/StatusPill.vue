<script setup lang="ts">
import type { ApplicationStatus } from '@/types'
import { STATUS_COLORS, STATUS_LABELS } from '@/utils/score'

const props = defineProps<{ status: ApplicationStatus }>()
const colors = STATUS_COLORS[props.status]
</script>

<template>
  <span
    :class="[
      'inline-flex items-center text-xs font-medium px-2 py-0.5 rounded-full',
      colors.bg,
      colors.text,
    ]"
  >
    {{ STATUS_LABELS[status] }}
  </span>
</template>
