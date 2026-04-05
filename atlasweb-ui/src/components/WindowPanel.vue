<template>
  <section class="window-panel" :class="{ 'window-panel--muted': muted }">
    <header class="window-panel__header">
      <div class="window-panel__titlebar">
        <span class="window-panel__dot" />
        <h2>{{ title }}</h2>
      </div>
      <div class="window-panel__actions">
        <slot name="toolbar" />
      </div>
    </header>
    <div class="window-panel__body">
      <slot />
    </div>
    <footer v-if="$slots.footer" class="window-panel__footer">
      <slot name="footer" />
    </footer>
  </section>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    required: true,
  },
  muted: {
    type: Boolean,
    default: false,
  },
})
</script>
