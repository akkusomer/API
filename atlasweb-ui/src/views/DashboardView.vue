<template>
  <AppShell
    :active-section="'workspace'"
    :current-route="route.path"
    :navigation="navigation"
    :top-menus="topMenus"
    :display-name="displayName"
    :company-label="companyLabel"
    :status-text="statusText"
    @logout="logout"
  >
    <section class="dashboard-screen">
      <p v-if="loadError" class="form-error">{{ loadError }}</p>

      <header class="dashboard-screen__hero">
        <div class="dashboard-screen__copy">
          <p class="dashboard-screen__eyebrow">operasyon merkezi</p>
          <h1>Çalışma Masası</h1>
          <p>{{ heroDescription }}</p>
        </div>

        <div class="dashboard-screen__hero-side">
          <span class="dashboard-screen__company">{{ companyLabel }}</span>
          <strong>{{ heroHeadline }}</strong>
          <small>{{ operationSignal }}</small>
        </div>
      </header>

      <section class="dashboard-overview">
        <article class="dashboard-kpi">
          <span class="dashboard-kpi__icon">FI</span>
          <div class="dashboard-kpi__body">
            <small>Bugünün Faturaları</small>
            <strong>{{ overview.todayInvoiceCount }}</strong>
            <span>{{ formatMoney(overview.todayInvoiceAmount) }} tutarında.</span>
          </div>
          <mark class="dashboard-kpi__chip">{{ dayDeltaLabel }}</mark>
        </article>

        <article class="dashboard-kpi">
          <span class="dashboard-kpi__icon dashboard-kpi__icon--soft">AY</span>
          <div class="dashboard-kpi__body">
            <small>Aylık Faturalar</small>
            <strong>{{ overview.monthInvoiceCount }}</strong>
            <span>{{ formatMoney(overview.monthInvoiceAmount) }} hacim.</span>
          </div>
          <div class="dashboard-kpi__progress">
            <span :style="{ width: monthlyProgressWidth }" />
          </div>
        </article>

        <article class="dashboard-kpi dashboard-kpi--accent">
          <div class="dashboard-kpi__body">
            <small>Toplam Fatura Hacmi</small>
            <strong>{{ formatMoney(overview.totalInvoiceAmount) }}</strong>
            <span>{{ overview.totalInvoiceCount }} kayıt içinde toplandı.</span>
          </div>
        </article>
      </section>

      <section class="dashboard-grid">
        <article class="dashboard-card dashboard-card--wide">
          <div class="dashboard-card__header">
            <div>
              <small>operasyon akisi</small>
              <h2>Son İşlemler</h2>
            </div>
            <RouterLink class="dashboard-card__link" to="/faturalar">Tümünü Gör</RouterLink>
          </div>

          <div v-if="loading" class="workspace-empty">Veriler hazırlanıyor...</div>
          <div v-else-if="overview.recentInvoices.length === 0" class="workspace-empty">
            Henüz fatura oluşmadı.
          </div>
          <div v-else class="dashboard-activity-table">
            <header class="dashboard-activity-table__head">
              <span>Müşteri / Cari</span>
              <span>Evrak</span>
              <span>Tarih</span>
              <span class="align-right">Tutar</span>
            </header>

            <article
              v-for="invoice in overview.recentInvoices"
              :key="invoice.id"
              class="dashboard-activity-row"
            >
              <div class="dashboard-activity-row__party">
                <span class="dashboard-activity-row__badge">{{ invoiceBadge(invoice.faturaNo) }}</span>
                <div>
                  <strong>{{ invoice.cariAdi || 'Cari seçilmemiş' }}</strong>
                  <small>{{ invoice.faturaNo }}</small>
                </div>
              </div>
              <span>Satış Faturası</span>
              <span>{{ formatDate(invoice.faturaTarihi) }}</span>
              <strong class="align-right">{{ formatMoney(invoice.tutar) }}</strong>
            </article>
          </div>
        </article>

        <aside class="dashboard-side">
          <article class="dashboard-card">
            <div class="dashboard-card__header">
              <div>
                <small>tanımlar</small>
                <h2>Hazır Alanlar</h2>
              </div>
              <RouterLink class="dashboard-card__link" to="/settings">Ayarlar</RouterLink>
            </div>

            <div class="dashboard-definition-list">
              <RouterLink class="dashboard-definition-card" to="/settings">
                <span class="dashboard-definition-card__icon">BR</span>
                <div>
                  <strong>Birimler</strong>
                  <small>{{ unitsPreview }}</small>
                </div>
                <em>{{ overview.unitCount }}</em>
              </RouterLink>

              <RouterLink class="dashboard-definition-card" to="/settings">
                <span class="dashboard-definition-card__icon dashboard-definition-card__icon--blue">CT</span>
                <div>
                  <strong>Cari Tipleri</strong>
                  <small>{{ cariTypesPreview }}</small>
                </div>
                <em>{{ overview.cariTypeCount }}</em>
              </RouterLink>
            </div>
          </article>

          <article class="dashboard-card">
            <div class="dashboard-card__header">
              <div>
                <small>kısayollar</small>
                <h2>Hızlı İşlemler</h2>
              </div>
            </div>

            <div class="dashboard-shortcuts">
              <RouterLink class="dashboard-shortcut dashboard-shortcut--primary" to="/faturalar">
                <strong>Yeni Fatura</strong>
                <span>Satır bazlı hızlı giriş ile evrak oluştur.</span>
              </RouterLink>
              <RouterLink class="dashboard-shortcut" to="/cariler">
                <strong>Cari Kartları</strong>
                <span>Müşteri ve tedarikçi kartlarını yönet.</span>
              </RouterLink>
              <RouterLink class="dashboard-shortcut" to="/stocks">
                <strong>Stok Kartları</strong>
                <span>Faturada kullanılacak stokları güncelle.</span>
              </RouterLink>
              <RouterLink class="dashboard-shortcut" to="/hks">
                <strong>HKS</strong>
                <span>Referans künye akışını tek yerden takip et.</span>
              </RouterLink>
            </div>
          </article>
        </aside>
      </section>
    </section>
  </AppShell>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AppShell from '../components/AppShell.vue'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const loading = ref(true)
const loadError = ref('')

const overview = reactive({
  totalInvoiceCount: 0,
  totalInvoiceAmount: 0,
  todayInvoiceCount: 0,
  todayInvoiceAmount: 0,
  monthInvoiceCount: 0,
  monthInvoiceAmount: 0,
  stockCount: 0,
  cariCount: 0,
  unitCount: 0,
  cariTypeCount: 0,
  recentInvoices: [],
})

const preview = reactive({
  units: [],
  cariTypes: [],
})

const unitsPreview = computed(() => (preview.units.length ? preview.units.join(', ') : 'Tanım yok'))
const cariTypesPreview = computed(() => (preview.cariTypes.length ? preview.cariTypes.join(', ') : 'Tanım yok'))

const heroHeadline = computed(() => {
  if (overview.todayInvoiceCount > 0) {
    return `${overview.todayInvoiceCount} fatura bugün aktif`
  }

  if (overview.totalInvoiceCount > 0) {
    return 'Gün operasyon için hazır'
  }

  return 'İlk evrak açılışı için hazır'
})

const heroDescription = computed(() => {
  if (overview.totalInvoiceCount === 0) {
    return 'Bu alan güne ait fatura hacmi, son hareketler ve tanım kısayollarını tek ekranda toplar.'
  }

  return `Bu ay ${overview.monthInvoiceCount} fatura oluştu ve ${formatMoney(overview.monthInvoiceAmount)} hacim kaydedildi.`
})

const operationSignal = computed(() => {
  if (overview.todayInvoiceCount > 0) {
    return 'Gün içinde aktif fatura hareketi var.'
  }

  if (overview.totalInvoiceCount > 0) {
    return 'Yeni hareket için sistem beklemede.'
  }

  return 'Kurulum tamam, ilk kayıt bekleniyor.'
})

const dayDeltaLabel = computed(() => {
  if (overview.todayInvoiceCount > 0) {
    return `+${Math.max(overview.todayInvoiceCount, 1)}`
  }

  return 'Hazır'
})

const monthlyProgressWidth = computed(() => {
  if (overview.totalInvoiceCount <= 0) {
    return '0%'
  }

  const ratio = Math.min((overview.monthInvoiceCount / Math.max(overview.totalInvoiceCount, 1)) * 100, 100)
  return `${Math.max(ratio, 8)}%`
})

const statusText = computed(() => {
  if (loading.value) {
    return 'Çalışma masası hazırlanıyor'
  }

  return `Bugün ${overview.todayInvoiceCount} fatura, toplam ${overview.totalInvoiceCount} kayıt`
})

const {
  route,
  displayName,
  companyLabel,
  navigation,
  topMenus,
  logout,
} = useWorkspaceShell('workspace', statusText)

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

function invoiceBadge(value) {
  const source = String(value || 'F')
  return source.slice(0, 2).toUpperCase()
}

async function loadDashboard() {
  loading.value = true
  loadError.value = ''

  try {
    const [invoiceSummary, stocks, caris, units, cariTypes] = await Promise.all([
      apiClient.getInvoiceSummary(),
      apiClient.getStocks(),
      apiClient.getCaris(),
      apiClient.getUnits(),
      apiClient.getCariTypes(),
    ])

    overview.totalInvoiceCount = invoiceSummary?.toplamFatura || 0
    overview.totalInvoiceAmount = Number(invoiceSummary?.toplamTutar || 0)
    overview.todayInvoiceCount = invoiceSummary?.bugunFatura || 0
    overview.todayInvoiceAmount = Number(invoiceSummary?.bugunTutar || 0)
    overview.monthInvoiceCount = invoiceSummary?.buAyFatura || 0
    overview.monthInvoiceAmount = Number(invoiceSummary?.buAyTutar || 0)
    overview.recentInvoices = invoiceSummary?.sonFaturalar || []

    overview.stockCount = stocks?.toplamKayit || stocks?.veriler?.length || 0
    overview.cariCount = caris?.toplamKayit || caris?.veriler?.length || 0
    overview.unitCount = units?.length || 0
    overview.cariTypeCount = cariTypes?.length || 0

    preview.units = (units || []).slice(0, 3).map((item) => item.ad)
    preview.cariTypes = (cariTypes || []).slice(0, 3).map((item) => item.adi)
  } catch (error) {
    loadError.value = error.message || 'Dashboard verileri yüklenemedi.'
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)
</script>
