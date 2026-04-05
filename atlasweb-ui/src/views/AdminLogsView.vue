<template>
  <AppShell
    :active-section="'admin'"
    :current-route="route.path"
    :navigation="navigation"
    :top-menus="topMenus"
    :display-name="displayName"
    :company-label="companyLabel"
    :status-text="statusText"
    @logout="logout"
  >
    <section class="desktop-stage">
      <p v-if="pageError" class="form-error">{{ pageError }}</p>

      <section class="admin-hero-card">
        <div class="admin-hero-card__copy">
          <p class="admin-hero-card__eyebrow">Loglar</p>
          <h1>İzleme ve Kayıtlar</h1>
          <p>Aktiviteler, sistem hataları ve dosya logları bu sayfada ayrı izlenir.</p>
        </div>

        <div class="admin-hero-card__actions">
          <button type="button" class="tool-button" @click="loadLogs">
            Yenile
          </button>
        </div>
      </section>

      <section class="admin-section">
        <header class="admin-section__header">
          <div>
            <p class="admin-section__eyebrow">Özet</p>
            <h2>Log Özeti</h2>
            <p>Sistemdeki temel izleme kayıtlarının kısa görünümü.</p>
          </div>
        </header>

        <div class="admin-summary-strip">
          <article class="metric-card">
            <span>Aktivite</span>
            <strong>{{ activities.length }}</strong>
          </article>
          <article class="metric-card">
            <span>Hata</span>
            <strong>{{ errors.length }}</strong>
          </article>
          <article class="metric-card">
            <span>Dosya Logu</span>
            <strong>{{ fileLogs.length }}</strong>
          </article>
          <article class="metric-card">
            <span>İzleme</span>
            <strong>Canlı</strong>
          </article>
        </div>
      </section>

      <div class="admin-group-grid">
        <WindowPanel title="Sistem Aktiviteleri" class="admin-col-4">
          <template #toolbar>
            <button type="button" class="tool-button" @click="loadLogs">
              Yenile
            </button>
          </template>

          <div class="timeline-list">
            <article v-if="loading">
              <strong>Yükleniyor...</strong>
              <span>Aktivite kayıtları alınıyor.</span>
            </article>
            <article v-else-if="activities.length === 0">
              <strong>Kayıt yok</strong>
              <span>Aktivite verisi bulunamadı.</span>
            </article>
            <article v-for="activity in activities.slice(0, 8)" :key="activity.id || activity.timestamp">
              <strong>{{ activity.action || 'İşlem' }}</strong>
              <span>{{ activity.userId || 'Anonim kullanıcı' }}</span>
            </article>
          </div>
        </WindowPanel>

        <WindowPanel title="Sistem Hataları" class="admin-col-4">
          <div class="timeline-list">
            <article v-if="loading">
              <strong>Yükleniyor...</strong>
              <span>Hata kayıtları alınıyor.</span>
            </article>
            <article v-else-if="errors.length === 0">
              <strong>Kayıt yok</strong>
              <span>Sistem hata kaydı bulunamadı.</span>
            </article>
            <article v-for="error in errors.slice(0, 8)" :key="error.id || error.timestamp">
              <strong>{{ error.hataMesaji || 'Hata' }}</strong>
              <span>{{ error.istekYolu || '-' }}</span>
            </article>
          </div>
        </WindowPanel>

        <WindowPanel title="Dosya Log Özeti" class="admin-col-4">
          <div class="timeline-list">
            <article v-if="loading">
              <strong>Yükleniyor...</strong>
              <span>Dosya logları alınıyor.</span>
            </article>
            <article v-else-if="fileLogs.length === 0">
              <strong>Kayıt yok</strong>
              <span>Dosya log satırı bulunamadı.</span>
            </article>
            <article v-for="(line, index) in fileLogs.slice(0, 6)" :key="`${index}-${line}`">
              <strong>Satır {{ index + 1 }}</strong>
              <span>{{ line }}</span>
            </article>
          </div>
        </WindowPanel>

        <WindowPanel title="Dosya Logları" class="admin-col-12">
          <div class="admin-log-list">
            <p v-if="loading" class="empty-cell">Log satırları alınıyor...</p>
            <p v-else-if="fileLogs.length === 0" class="empty-cell">Log satırı bulunamadı.</p>
            <pre v-for="(line, index) in fileLogs.slice(0, 24)" :key="`${index}-full-${line}`">{{ line }}</pre>
          </div>
        </WindowPanel>
      </div>
    </section>
  </AppShell>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useAdminShell } from '../composables/useAdminShell'
import { apiClient } from '../services/api'

const loading = ref(true)
const pageError = ref('')
const activities = ref([])
const errors = ref([])
const fileLogs = ref([])

const statusText = computed(
  () => `${activities.value.length} aktivite, ${errors.value.length} hata, ${fileLogs.value.length} log`
)

const { route, displayName, navigation, topMenus, companyLabel, logout } = useAdminShell(statusText)

async function loadLogs() {
  loading.value = true
  pageError.value = ''

  try {
    const [activityResponse, errorResponse, fileLogResponse] = await Promise.all([
      apiClient.getAdminActivities(),
      apiClient.getAdminErrors(),
      apiClient.getFileLogs().catch(() => []),
    ])

    activities.value = activityResponse || []
    errors.value = errorResponse || []
    fileLogs.value = Array.isArray(fileLogResponse) ? fileLogResponse : []
  } catch (error) {
    pageError.value = error.message || 'Log kayıtları yüklenemedi.'
  } finally {
    loading.value = false
  }
}

onMounted(loadLogs)
</script>
