<template>
  <AppShell
    :active-section="'hks'"
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
      <p v-else-if="pageSuccess" class="form-success">{{ pageSuccess }}</p>

      <section v-if="searching" class="progress-card">
        <div class="progress-card__head">
          <strong>HKS sorgusu devam ediyor</strong>
          <span>%{{ progressPercent }}</span>
        </div>
        <div class="progress-card__track">
          <div class="progress-card__fill" :style="{ width: `${progressPercent}%` }"></div>
        </div>
        <p class="muted-line">{{ progressLabel }}</p>
      </section>

      <section class="admin-section">
        <header class="admin-section__header">
          <div>
            <p class="admin-section__eyebrow">Arama Filtreleri</p>
            <h2>HKS sorgusu</h2>
            <p>Başlangıç ve bitiş tarihine göre tüm ürünlerde referans künye ara.</p>
          </div>
          <div class="admin-section__actions">
            <button type="button" class="tool-button" @click="searchReferansKunyeler" :disabled="searching">
              {{ searching ? 'Sorgulanıyor...' : 'Liste Yenile' }}
            </button>
            <button type="button" class="tool-button" @click="resetFilters">
              Filtreyi Temizle
            </button>
          </div>
        </header>

        <form class="hks-filter-grid" @submit.prevent="searchReferansKunyeler">
          <label class="inline-field">
            <span>Başlangıç Tarihi</span>
            <input v-model="filters.baslangicTarihi" type="date" />
          </label>

          <label class="inline-field">
            <span>Bitiş Tarihi</span>
            <input v-model="filters.bitisTarihi" type="date" />
          </label>

          <div class="hks-filter-grid__actions">
            <button type="submit" class="primary-button" :disabled="searching">
              {{ searching ? 'Sorgulanıyor...' : 'Referans Künye Sorgula' }}
            </button>
          </div>
        </form>
      </section>

      <div class="selection-summary">
        <div>
          <small>Durum</small>
          <strong>{{ result?.durum || '-' }}</strong>
        </div>
        <div>
          <small>Son İşlem Kodu</small>
          <strong>{{ result?.islemKodu || '-' }}</strong>
        </div>
        <div>
          <small>Künye Kaydı</small>
          <strong>{{ referansKunyeler.length }}</strong>
        </div>
      </div>

      <WindowPanel title="Referans Künye Sonuçları">
        <template #toolbar>
          <button type="button" class="tool-button" @click="searchReferansKunyeler" :disabled="searching">
            Yenile
          </button>
        </template>

        <div class="grid-shell">
          <table class="data-grid">
            <thead>
              <tr>
                <th>Künye No</th>
                <th>Bildirim Tarihi</th>
                <th>Mal</th>
                <th>Cins / Tur</th>
                <th>Kalan Miktar</th>
                <th>Mal Sahibi</th>
                <th>UniqueId</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="searching">
                <td colspan="7" class="empty-cell">HKS referans künye listesi sorgulanıyor...</td>
              </tr>
              <tr v-else-if="referansKunyeler.length === 0">
                <td colspan="7" class="empty-cell">Henüz sonuç yok. Filtre verip sorgu çalıştır.</td>
              </tr>
              <tr v-for="item in referansKunyeler" :key="item.uniqueId || item.kunyeNo">
                <td>{{ item.kunyeNo }}</td>
                <td>{{ formatDateTime(item.bildirimTarihi) }}</td>
                <td>
                  <strong>{{ item.malinAdi || '-' }}</strong>
                  <div class="muted-line">Kod: {{ item.malinKodNo || '-' }}</div>
                </td>
                <td>{{ joinNonEmpty(item.malinCinsi, item.malinTuru) }}</td>
                <td>{{ formatQuantity(item.kalanMiktar, item.miktarBirimiAd || item.miktarBirimId) }}</td>
                <td>{{ item.malinSahibiTcKimlikVergiNo || '-' }}</td>
                <td>{{ item.uniqueId || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </WindowPanel>
    </section>
  </AppShell>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const searching = ref(false)
const pageError = ref('')
const pageSuccess = ref('')
const result = ref(null)
const referansKunyeler = ref([])
const hksSettings = ref(null)
const progressPercent = ref(0)
const progressLabel = ref('')
let pollTimer = null

const filters = reactive(createDefaultFilters())

const statusText = computed(() => {
  if (searching.value) {
    return `%${progressPercent.value} HKS sorgu`
  }

  return `${referansKunyeler.value.length} HKS referans künye`
})
const { route, displayName, companyLabel, navigation, topMenus, logout } = useWorkspaceShell('hks', statusText)

function createDefaultFilters() {
  const today = new Date()
  const start = new Date(today)
  start.setDate(today.getDate() - 29)

  return {
    baslangicTarihi: formatDateInput(start),
    bitisTarihi: formatDateInput(today),
  }
}

function formatDateInput(value) {
  const year = value.getFullYear()
  const month = String(value.getMonth() + 1).padStart(2, '0')
  const day = String(value.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function resetFilters() {
  Object.assign(filters, createDefaultFilters())
  pageError.value = ''
  pageSuccess.value = ''
}

function buildSearchPayload() {
  return {
    baslangicTarihi: filters.baslangicTarihi ? `${filters.baslangicTarihi}T00:00:00` : null,
    bitisTarihi: filters.bitisTarihi ? `${filters.bitisTarihi}T23:59:59` : null,
    kalanMiktariSifirdanBuyukOlanlar: true,
  }
}

function applySavedSnapshot(snapshot) {
  if (!snapshot) {
    return
  }

  result.value = snapshot
  referansKunyeler.value = (Array.isArray(snapshot.referansKunyeler) ? snapshot.referansKunyeler : []).sort((left, right) => {
    const leftDate = left.bildirimTarihi ? new Date(left.bildirimTarihi).getTime() : 0
    const rightDate = right.bildirimTarihi ? new Date(right.bildirimTarihi).getTime() : 0

    if (rightDate !== leftDate) {
      return rightDate - leftDate
    }

    return (right.kunyeNo || 0) - (left.kunyeNo || 0)
  })
  progressPercent.value = Number(snapshot.progressPercent || 0)
  progressLabel.value = snapshot.progressLabel || ''
  searching.value = snapshot.durum === 'Kuyrukta' || snapshot.durum === 'Isleniyor'

  if (snapshot.baslangicTarihi) {
    filters.baslangicTarihi = formatDateInput(new Date(snapshot.baslangicTarihi))
  }

  if (snapshot.bitisTarihi) {
    filters.bitisTarihi = formatDateInput(new Date(snapshot.bitisTarihi))
  }
}

function stopPolling() {
  if (pollTimer) {
    window.clearInterval(pollTimer)
    pollTimer = null
  }
}

async function pollSnapshot(showMessages = false) {
  const snapshot = await apiClient.getSavedHksReferansKunyeler()
  if (!snapshot) {
    return
  }

  applySavedSnapshot(snapshot)

  if (snapshot.durum === 'Tamamlandi') {
    stopPolling()
    pageError.value = ''
    if (showMessages) {
      pageSuccess.value = snapshot.kayitSayisi > 0
        ? `${snapshot.kayitSayisi} kayıt bulundu ve HKS sorgusu tamamlandı.`
        : 'HKS sorgusu tamamlandı, kayıt bulunamadı.'
    }
    return
  }

  if (snapshot.durum === 'Hatali') {
    stopPolling()
    pageSuccess.value = ''
    pageError.value = snapshot.hata || 'HKS sorgusu tamamlanamadı.'
    return
  }
}

function startPolling() {
  stopPolling()
  pollTimer = window.setInterval(() => {
    pollSnapshot(true).catch((error) => {
      stopPolling()
      searching.value = false
      pageError.value = error.message || 'HKS iş durumu alınamadı.'
    })
  }, 1500)
}

async function searchReferansKunyeler() {
  pageError.value = ''
  pageSuccess.value = ''

  try {
    if (!hksSettings.value?.kullanıcıAdi) {
      throw new Error('Önce Ayarlar ekranından HKS kullanıcı adını kaydetmelisiniz.')
    }

    if (!filters.baslangicTarihi || !filters.bitisTarihi) {
      throw new Error('Başlangıç ve bitiş tarihi zorunludur.')
    }

    const queued = await apiClient.searchHksReferansKunyeler(buildSearchPayload())
    applySavedSnapshot(queued)
    pageSuccess.value = 'HKS sorgusu kuyruğa alındı.'
    pageError.value = ''
    startPolling()
  } catch (error) {
    searching.value = false
    pageError.value = error.message || 'HKS referans künye listesi alınamadı.'
  }
}

function formatDateTime(value) {
  if (!value) {
    return '-'
  }

  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(value))
}

function formatQuantity(value, unit) {
  if (value === null || value === undefined) {
    return '-'
  }

  const formatted = new Intl.NumberFormat('tr-TR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 3,
  }).format(value)

  return unit ? `${formatted} ${unit}` : formatted
}

function joinNonEmpty(...values) {
  const normalized = values.filter(Boolean)
  return normalized.length > 0 ? normalized.join(' / ') : '-'
}

onMounted(async () => {
  try {
    hksSettings.value = await apiClient.getHksSettings()
  } catch (error) {
    pageError.value = error.message || 'HKS ayarları alınamadı.'
    return
  }

  try {
    const cachedProducts = await apiClient.getSavedHksUrunler()
    if (!Array.isArray(cachedProducts) || cachedProducts.length === 0) {
      await apiClient.getHksUrunler()
    }
  } catch {
    // Ürün cache'i kritik değil; sayfa açılışını bloklama.
  }

  try {
    const cachedCities = await apiClient.getSavedHksIller()
    if (!Array.isArray(cachedCities) || cachedCities.length === 0) {
      await apiClient.getHksIller()
    }
  } catch {
    // Il cache'i kritik degil; sayfa acilisini bloklama.
  }

  try {
    const savedSnapshot = await apiClient.getSavedHksReferansKunyeler()
    if (savedSnapshot) {
      applySavedSnapshot(savedSnapshot)
      pageSuccess.value = 'Son kayıtlı HKS listesi yüklendi.'
      if (savedSnapshot.durum === 'Kuyrukta' || savedSnapshot.durum === 'Isleniyor') {
        startPolling()
      }
    }
  } catch (error) {
    pageError.value = error.message || 'Kayıtlı HKS listesi alınamadı.'
  }
})

onUnmounted(() => {
  stopPolling()
})
</script>
