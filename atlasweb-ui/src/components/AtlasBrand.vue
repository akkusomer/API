<template>
  <div class="atlas-brand" :class="{ 'atlas-brand--compact': compact }">
    <span class="atlas-brand__copy">
      <span class="atlas-brand__title">ATLAS</span>
      <span class="atlas-brand__subtitle">HAL PROGRAMI</span>
    </span>
  </div>
</template>

<script setup>
defineProps({
  compact: {
    type: Boolean,
    default: false,
  },
})
</script>
