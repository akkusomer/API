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
          <p class="admin-hero-card__eyebrow">Sistem Yönetimi</p>
          <h1>Admin Özeti</h1>
          <p>
            Bu ekran sadece özet görünümü verir. Şirketler, yöneticiler, HKS ve loglar
            ayrı sayfalardan yönetilir.
          </p>
        </div>

        <div class="admin-hero-card__actions">
          <button type="button" class="tool-button" @click="router.push('/admin/sirketler')">
            Şirketler
          </button>
          <button type="button" class="tool-button" @click="router.push('/admin/yoneticiler')">
            Yöneticiler
          </button>
          <button type="button" class="tool-button" @click="router.push('/admin/loglar')">
            Loglar
          </button>
        </div>
      </section>

      <section class="admin-section">
        <header class="admin-section__header">
          <div>
            <p class="admin-section__eyebrow">Genel Durum</p>
            <h2>Platform Özeti</h2>
            <p>Sistem genelindeki temel sayıların hızlı görünümü.</p>
          </div>
        </header>

        <div class="admin-summary-strip">
          <article class="metric-card">
            <span>Şirket</span>
            <strong>{{ customers.length }}</strong>
          </article>
          <article class="metric-card">
            <span>Sistem Yöneticisi</span>
            <strong>{{ admins.length }}</strong>
          </article>
          <article class="metric-card">
            <span>Aktivite</span>
            <strong>{{ activities.length }}</strong>
          </article>
          <article class="metric-card">
            <span>Hata Kaydı</span>
            <strong>{{ errors.length }}</strong>
          </article>
        </div>
      </section>

      <div class="admin-group-grid">
        <WindowPanel title="Hızlı Geçişler" class="admin-col-7">
          <div class="launcher-grid">
            <button type="button" class="launcher-card admin-shortcut-card" @click="router.push('/admin/sirketler')">
              <strong>Şirketler</strong>
              <span>Müşteri listesi ve şirket kullanıcıları</span>
            </button>
            <button type="button" class="launcher-card admin-shortcut-card" @click="router.push('/admin/yoneticiler')">
              <strong>Yöneticiler</strong>
              <span>Sistem yöneticisi ekle, düzenle, pasife al</span>
            </button>
            <button type="button" class="launcher-card admin-shortcut-card" @click="router.push('/admin/loglar')">
              <strong>Loglar</strong>
              <span>Aktiviteler, sistem hataları ve dosya logları</span>
            </button>
          </div>
        </WindowPanel>

        <WindowPanel title="Son Aktiviteler" class="admin-col-5">
          <template #toolbar>
            <button type="button" class="tool-button" @click="loadOverview">
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
            <article v-for="activity in activities.slice(0, 6)" :key="activity.id || activity.timestamp">
              <strong>{{ activity.action || 'İşlem' }}</strong>
              <span>{{ activity.userId || 'Anonim kullanıcı' }}</span>
            </article>
          </div>
        </WindowPanel>

        <WindowPanel title="Son Sistem Hatalari" class="admin-col-12">
          <div class="timeline-list">
            <article v-if="loading">
              <strong>Yükleniyor...</strong>
              <span>Hata kayıtları alınıyor.</span>
            </article>
            <article v-else-if="errors.length === 0">
              <strong>Kayıt yok</strong>
              <span>Sistem hata kaydı bulunamadı.</span>
            </article>
            <article v-for="error in errors.slice(0, 4)" :key="error.id || error.timestamp">
              <strong>{{ error.hataMesaji || 'Hata' }}</strong>
              <span>{{ error.istekYolu || '-' }}</span>
            </article>
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
const customers = ref([])
const admins = ref([])
const activities = ref([])
const errors = ref([])

const statusText = computed(
  () => `${customers.value.length} şirket, ${admins.value.length} yönetici, ${errors.value.length} hata`
)

const {
  route,
  router,
  displayName,
  navigation,
  topMenus,
  companyLabel,
  logout,
} = useAdminShell(statusText)

async function loadOverview() {
  loading.value = true
  pageError.value = ''

  try {
    const [customerResponse, adminResponse, activityResponse, errorResponse] = await Promise.all([
      apiClient.getCustomers(),
      apiClient.getSystemAdmins(),
      apiClient.getAdminActivities(),
      apiClient.getAdminErrors(),
    ])

    customers.value = customerResponse || []
    admins.value = adminResponse || []
    activities.value = activityResponse || []
    errors.value = errorResponse || []
  } catch (error) {
    pageError.value = error.message || 'Admin özeti yüklenemedi.'
  } finally {
    loading.value = false
  }
}

onMounted(loadOverview)
</script>
