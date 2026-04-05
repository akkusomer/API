<template>
  <div class="shell" :class="[`shell--${activeSection}`, { 'shell--no-topbar': !showTopbar }]">
    <aside class="shell__sidebar">
      <div class="shell__sidebar-head">
        <AtlasBrand compact />
        <div class="shell__sidebar-company">
          <span class="shell__sidebar-caption">Çalışan Alan</span>
          <strong>{{ companyLabel }}</strong>
        </div>
      </div>

      <div class="shell__sidebar-nav">
        <div
          v-for="entry in navigation"
          :key="entry.label"
          class="shell__tree-group"
        >
          <button
            v-if="isDirectEntry(entry)"
            type="button"
            class="shell__tree-title shell__tree-title--direct"
            :class="{ 'is-active': isEntryActive(entry) }"
            @click="entry.action"
          >
            <span>{{ entry.label }}</span>
          </button>

          <template v-else>
            <button
              type="button"
              class="shell__tree-title"
              :class="{ 'is-active': isEntryActive(entry) }"
              @click="toggleGroup(entry.label)"
            >
              <span>{{ entry.label }}</span>
              <span class="shell__tree-sign">{{ openedGroups.has(entry.label) ? '-' : '+' }}</span>
            </button>

            <div v-if="openedGroups.has(entry.label)" class="shell__tree-items">
              <button
                v-for="item in entry.items"
                :key="item.label"
                type="button"
                class="shell__tree-item"
                :class="{ 'is-current': item.route === currentRoute }"
                @click="item.action"
              >
                <span class="shell__tree-bullet" />
                <span>{{ item.label }}</span>
              </button>
            </div>
          </template>
        </div>
      </div>

      <div class="shell__sidebar-foot">
        <div class="shell__sidebar-status">
          <small>Durum</small>
          <strong>{{ statusText }}</strong>
        </div>

        <button type="button" class="shell__sidebar-logout" @click="$emit('logout')">
          Çıkış
        </button>
      </div>
    </aside>

    <div class="shell__main">
      <header v-if="showTopbar" class="shell__topbar">
        <div class="shell__workspace">
          <strong>İşlem Alanı</strong>
          <span>{{ companyLabel }}</span>
        </div>

        <nav class="shell__menu" aria-label="Üst gezinme">
          <button
            v-for="menu in topMenus"
            :key="menu.route"
            type="button"
            class="shell__menu-item"
            :class="{ 'is-active': isMenuActive(menu) }"
            @click="menu.action"
          >
            {{ menu.label }}
          </button>
        </nav>

        <div class="shell__account">
          <div class="shell__account-meta">
            <small>Oturum</small>
            <strong>{{ nowLabel }}</strong>
          </div>

          <div class="shell__account-user">
            <div class="shell__account-avatar">{{ initials }}</div>
            <div class="shell__account-copy">
              <strong>{{ displayName }}</strong>
              <span>{{ companyLabel }}</span>
            </div>
          </div>
        </div>
      </header>

      <main class="shell__stage">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import AtlasBrand from './AtlasBrand.vue'

const props = defineProps({
  activeSection: { type: String, default: 'workspace' },
  currentRoute: { type: String, required: true },
  navigation: { type: Array, required: true },
  topMenus: { type: Array, required: true },
  displayName: { type: String, required: true },
  companyLabel: { type: String, required: true },
  statusText: { type: String, required: true },
  showTopbar: { type: Boolean, default: true },
})

defineEmits(['logout'])

const openedGroups = ref(new Set())
const now = ref(new Date())
let timerId = null

const initials = computed(() =>
  (props.displayName || 'AT')
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() || '')
    .join('')
)

function isDirectEntry(entry) {
  return !Array.isArray(entry.items)
}

function syncOpenedGroups() {
  openedGroups.value = new Set()
}

function toggleGroup(label) {
  const next = new Set(openedGroups.value)
  if (next.has(label)) {
    next.delete(label)
  } else {
    next.add(label)
  }
  openedGroups.value = next
}

function isMenuActive(menu) {
  return menu.route === props.currentRoute || menu.section === props.activeSection
}

function isEntryActive(entry) {
  if (isDirectEntry(entry)) {
    return entry.route === props.currentRoute || entry.section === props.activeSection
  }

  return entry.section === props.activeSection
    || (entry.items || []).some((item) => item.route === props.currentRoute)
}

const nowLabel = computed(() =>
  new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Europe/Istanbul',
  }).format(now.value)
)

onMounted(() => {
  syncOpenedGroups()
  timerId = window.setInterval(() => {
    now.value = new Date()
  }, 30000)
})

watch(
  () => props.navigation,
  () => {
    syncOpenedGroups()
  },
  { deep: true }
)

onBeforeUnmount(() => {
  if (timerId) {
    window.clearInterval(timerId)
  }
})
</script>
