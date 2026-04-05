<template>
  <AppShell
    :active-section="'cari'"
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

      <header class="invoice-workspace__header">
        <div class="invoice-workspace__copy">
          <h1>Cari Ekstre</h1>
          <p>{{ cariTitle }}</p>
        </div>

        <div class="invoice-workspace__actions">
          <button type="button" class="tool-button" @click="router.push('/cariler')">Cari Listeye Don</button>
          <button type="button" class="tool-button" @click="loadEkstre">Yenile</button>
        </div>
      </header>

      <div class="metric-grid">
        <div class="metric-card">
          <small>Fatura Sayisi</small>
          <strong>{{ summary.faturaSayisi }}</strong>
        </div>
        <div class="metric-card">
          <small>Toplam Borc</small>
          <strong>{{ formatMoney(summary.toplamBorc) }}</strong>
        </div>
        <div class="metric-card">
          <small>Toplam Tahsilat</small>
          <strong>{{ formatMoney(summary.toplamTahsilat) }}</strong>
        </div>
        <div class="metric-card metric-card--danger">
          <small>Kalan Borc</small>
          <strong>{{ formatMoney(summary.kalanBorc) }}</strong>
        </div>
      </div>

      <WindowPanel title="Cari Bilgileri">
        <div class="compact-list">
          <div><strong>Unvan / Kisi</strong><span>{{ cariTitle }}</span></div>
          <div><strong>Cari Tipi</strong><span>{{ cari.cariTipAdi || '-' }}</span></div>
          <div><strong>VKN / TCKN</strong><span>{{ cari.vtckNo || '-' }}</span></div>
          <div><strong>Iletisim</strong><span>{{ [cari.gsm, cari.telefon].filter(Boolean).join(' / ') || '-' }}</span></div>
        </div>
      </WindowPanel>

      <WindowPanel title="Faturalar">
        <div class="grid-shell">
          <table class="data-grid">
            <thead>
              <tr>
                <th>Fatura No</th>
                <th>Tarih</th>
                <th>Kalem</th>
                <th class="align-right">Tutar</th>
                <th class="align-right">Tahsilat</th>
                <th class="align-right">Kalan Borc</th>
                <th>Aciklama</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="loading">
                <td colspan="7" class="empty-cell">Cari ekstre yukleniyor...</td>
              </tr>
              <tr v-else-if="faturalar.length === 0">
                <td colspan="7" class="empty-cell">Bu cariye bagli fatura bulunamadi.</td>
              </tr>
              <tr v-for="fatura in faturalar" :key="fatura.id">
                <td>{{ fatura.faturaNo || '-' }}</td>
                <td>{{ formatDate(fatura.faturaTarihi) }}</td>
                <td>{{ fatura.kalemSayisi ?? 0 }}</td>
                <td class="align-right">{{ formatMoney(fatura.tutar) }}</td>
                <td class="align-right">{{ formatMoney(fatura.tahsilEdilenTutar) }}</td>
                <td class="align-right">{{ formatMoney(fatura.kalanBorc) }}</td>
                <td>{{ fatura.aciklama || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </WindowPanel>
    </section>
  </AppShell>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const loading = ref(true)
const pageError = ref('')
const faturalar = ref([])
const cari = reactive({
  unvan: '',
  adiSoyadi: '',
  cariTipAdi: '',
  telefon: '',
  gsm: '',
  vtckNo: '',
})
const summary = reactive({
  faturaSayisi: 0,
  kasaFisSayisi: 0,
  toplamTutar: 0,
  toplamBorc: 0,
  toplamKasaTahsilat: 0,
  toplamKasaOdeme: 0,
  toplamTahsilat: 0,
  kalanBorc: 0,
})

const statusText = computed(() => `${summary.faturaSayisi} fatura listeleniyor`)
const { route, router, displayName, companyLabel, navigation, topMenus, logout } = useWorkspaceShell('cari', statusText)

const cariId = computed(() => route.params.id)
const cariTitle = computed(() => cari.unvan || cari.adiSoyadi || 'Cari')

function formatMoney(value) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Number(value || 0))
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(value))
}

async function loadEkstre() {
  if (!cariId.value) {
    pageError.value = 'Cari secimi bulunamadi.'
    return
  }

  loading.value = true
  pageError.value = ''

  try {
    const response = await apiClient.getCariEkstre(cariId.value)
    Object.assign(cari, {
      unvan: response?.cari?.unvan || '',
      adiSoyadi: response?.cari?.adiSoyadi || '',
      cariTipAdi: response?.cari?.cariTipAdi || '',
      telefon: response?.cari?.telefon || '',
      gsm: response?.cari?.gsm || '',
      vtckNo: response?.cari?.vtckNo || response?.cari?.vtcK_No || response?.cari?.vtck_No || response?.cari?.VTCK_No || '',
    })
    Object.assign(summary, {
      faturaSayisi: response?.ozet?.faturaSayisi || 0,
      kasaFisSayisi: response?.ozet?.kasaFisSayisi || 0,
      toplamTutar: response?.ozet?.toplamTutar || 0,
      toplamBorc: response?.ozet?.toplamBorc || 0,
      toplamKasaTahsilat: response?.ozet?.toplamKasaTahsilat || 0,
      toplamKasaOdeme: response?.ozet?.toplamKasaOdeme || 0,
      toplamTahsilat: response?.ozet?.toplamTahsilat || 0,
      kalanBorc: response?.ozet?.kalanBorc || 0,
    })
    faturalar.value = response?.faturalar || []
  } catch (error) {
    pageError.value = error.message || 'Cari ekstresi yuklenemedi.'
  } finally {
    loading.value = false
  }
}

onMounted(loadEkstre)
watch(cariId, loadEkstre)
</script>
