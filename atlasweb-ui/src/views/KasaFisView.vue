<template>
  <AppShell
    :active-section="'kasa'"
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

      <WindowPanel title="Kasa Fisleri">
        <template #toolbar>
          <button type="button" class="tool-button" @click="openModal()">Yeni Fis</button>
          <button
            type="button"
            class="tool-button"
            :disabled="!selectedFisCariKartId"
            @click="openCariEkstre()"
          >
            Cari Ekstre (F10)
          </button>
          <button type="button" class="tool-button" @click="loadPageData">Yenile</button>
        </template>

        <div class="window-toolbar">
          <label class="inline-field inline-field--grow">
            <span>Ara</span>
            <input v-model.trim="search" type="text" placeholder="Belge no, cari veya aciklama" />
          </label>
        </div>

        <p class="invoice-column-hint">F2 yeni fis, Enter duzenle, F3 sil, F10 cari ekstre, F9 modalda kaydet.</p>

        <div class="grid-shell">
          <table class="data-grid">
            <thead>
              <tr>
                <th>Cari</th>
                <th>Tarih</th>
                <th>Aciklama</th>
                <th class="align-right">Tutar</th>
                <th class="align-right">Islem</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="loading">
                <td colspan="5" class="empty-cell">Kasa fisleri yukleniyor...</td>
              </tr>
              <tr v-else-if="filteredFisler.length === 0">
                <td colspan="5" class="empty-cell">Kasa fisi bulunamadi.</td>
              </tr>
              <tr
                v-for="fis in filteredFisler"
                :key="fis.id"
                class="is-clickable"
                :class="{ 'is-selected': selectedFisId === fis.id }"
                @click="selectFis(fis)"
                @dblclick="openModal(fis)"
              >
                <td>{{ fis.cariAdi || '-' }}</td>
                <td>{{ formatDate(fis.tarih) }}</td>
                <td>{{ fis.aciklama1 || '-' }}</td>
                <td class="align-right">{{ formatMoney(fis.tutar) }}</td>
                <td class="align-right">
                  <div class="table-actions">
                    <button type="button" class="table-action" @click.stop="openModal(fis)">Duzenle</button>
                    <button
                      type="button"
                      class="table-action"
                      :disabled="!fis.cariKartId"
                      @click.stop="openCariEkstre(fis)"
                    >
                      Ekstre
                    </button>
                    <button type="button" class="table-action table-action--danger" @click.stop="removeFis(fis.id)">Sil</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </WindowPanel>
    </section>

    <div v-if="modalOpen" class="modal-backdrop" @click.self="closeModal">
      <div class="modal-window modal-window--wide">
        <header class="modal-window__header">
          <h2>{{ editingId ? 'Kasa Fisi Duzenle' : 'Yeni Kasa Fisi' }}</h2>
          <button type="button" class="ghost-icon" @click="closeModal">x</button>
        </header>

        <form class="desktop-form" @submit.prevent="saveFis">
          <div class="desktop-form__grid">
            <label class="field">
              <span>Cari</span>
              <select v-model="form.cariKartId">
                <option value="">Cari secmeden devam et</option>
                <option v-for="cari in cariler" :key="cari.id" :value="cari.id">
                  {{ cari.unvan || cari.adiSoyadi || '-' }}
                </option>
              </select>
            </label>

            <label class="field">
              <span>Tarih</span>
              <input v-model="form.tarih" type="date" required />
            </label>

            <label class="field">
              <span>Aciklama-1</span>
              <input v-model.trim="form.aciklama1" type="text" />
            </label>

            <label class="field">
              <span>Tutar</span>
              <input v-model.number="form.tutar" type="number" min="0.01" step="0.01" required />
            </label>
          </div>

          <p v-if="formError" class="form-error">{{ formError }}</p>

          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeModal">Esc-Vazgec</button>
            <button type="submit" class="primary-button" :disabled="saving">
              {{ saving ? 'Kaydediliyor...' : 'F9-Kaydet' }}
            </button>
          </footer>
        </form>
      </div>
    </div>
  </AppShell>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useGridKeyboard } from '../composables/useGridKeyboard'
import { useSaveShortcut } from '../composables/useSaveShortcut'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const loading = ref(true)
const saving = ref(false)
const search = ref('')
const fisler = ref([])
const cariler = ref([])
const modalOpen = ref(false)
const editingId = ref('')
const selectedFisId = ref('')
const currentBelgeNo = ref(null)
const formError = ref('')
const pageError = ref('')

const form = reactive({
  cariKartId: '',
  tarih: toInputDate(),
  aciklama1: '',
  tutar: 0,
})

const filteredFisler = computed(() => {
  const term = search.value.trim().toLowerCase()
  if (!term) {
    return fisler.value
  }

  return fisler.value.filter((fis) =>
    [
      fis.belgeNo?.toString(),
      fis.kasaAdi,
      fis.cariAdi,
      fis.aciklama1,
      fis.aciklama2,
    ]
      .filter(Boolean)
      .some((value) => String(value).toLowerCase().includes(term))
  )
})

const selectedFis = computed(() =>
  filteredFisler.value.find((item) => item.id === selectedFisId.value) || null
)
const selectedFisCariKartId = computed(() => selectedFis.value?.cariKartId || '')

const statusText = computed(() => `${filteredFisler.value.length} kasa fisi listeleniyor`)
const { route, router, displayName, companyLabel, navigation, topMenus, logout } = useWorkspaceShell('kasa', statusText)

const { selectItem: selectFis } = useGridKeyboard({
  items: filteredFisler,
  selectedKey: selectedFisId,
  setSelectedKey: (value) => {
    selectedFisId.value = value
  },
  enabled: computed(() => !modalOpen.value),
  onCreate: () => openModal(),
  onEnter: (fis) => openModal(fis),
  onDelete: (fis) => removeFis(fis.id),
  onF10: (fis) => openCariEkstre(fis),
})

useSaveShortcut({
  enabled: computed(() => modalOpen.value && !saving.value),
  onSave: saveFis,
})

function toInputDate(value) {
  const date = value ? new Date(value) : new Date()
  const year = date.getUTCFullYear()
  const month = `${date.getUTCMonth() + 1}`.padStart(2, '0')
  const day = `${date.getUTCDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
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

function formatMoney(value) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Number(value || 0))
}

async function loadPageData() {
  loading.value = true
  pageError.value = ''

  try {
    const [fisResponse, cariResponse] = await Promise.all([
      apiClient.getKasaFisleri(),
      apiClient.getCaris({ pageSize: 1000 }),
    ])

    fisler.value = fisResponse?.veriler || []
    cariler.value = cariResponse?.veriler || []
  } catch (error) {
    pageError.value = error.message || 'Kasa fisleri yuklenemedi.'
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.cariKartId = ''
  form.tarih = toInputDate()
  form.aciklama1 = ''
  form.tutar = 0
  editingId.value = ''
  currentBelgeNo.value = null
  formError.value = ''
}

async function openModal(record = null) {
  resetForm()
  modalOpen.value = true

  if (!record) {
    return
  }

  editingId.value = record.id
  currentBelgeNo.value = record.belgeNo || null

  try {
    const detail = await apiClient.getKasaFis(record.id)
    editingId.value = detail.id
    currentBelgeNo.value = detail.belgeNo || null
    form.cariKartId = detail.cariKartId || ''
    form.tarih = toInputDate(detail.tarih)
    form.aciklama1 = detail.aciklama1 || ''
    form.tutar = Number(detail.tutar || 0)
  } catch (error) {
    formError.value = error.message || 'Kasa fisi detaylari yuklenemedi.'
  }
}

function closeModal() {
  modalOpen.value = false
}

function openCariEkstre(record = null) {
  const targetCariId = record?.cariKartId || selectedFisCariKartId.value
  if (!targetCariId) {
    return
  }

  router.push(`/cariler/${targetCariId}/ekstre`)
}

function handleShortcut(event) {
  if (event.defaultPrevented || event.key !== 'F10') {
    return
  }

  if (modalOpen.value) {
    if (!form.cariKartId) {
      return
    }

    event.preventDefault()
    router.push(`/cariler/${form.cariKartId}/ekstre`)
    return
  }

  if (!selectedFisCariKartId.value) {
    return
  }

  event.preventDefault()
  openCariEkstre()
}

async function saveFis() {
  formError.value = ''
  saving.value = true

  try {
    const payload = {
      kasaAdi: 'MERKEZ TL KASA',
      belgeKodu: 'KF',
      islemTipi: 1,
      cariKartId: form.cariKartId || null,
      tarih: `${form.tarih}T00:00:00Z`,
      hareketTipi: 'GENEL',
      ozelKodu: null,
      aciklama1: form.aciklama1 || null,
      aciklama2: null,
      pos: null,
      tutar: Number(form.tutar || 0),
    }

    if (editingId.value) {
      await apiClient.updateKasaFis(editingId.value, payload)
    } else {
      const result = await apiClient.createKasaFis(payload)
      currentBelgeNo.value = result?.belgeNo || null
    }

    closeModal()
    await loadPageData()
  } catch (error) {
    formError.value = error.message || 'Kasa fisi kaydedilemedi.'
  } finally {
    saving.value = false
  }
}

async function removeFis(id) {
  if (!window.confirm('Bu kasa fisini silmek istiyor musunuz?')) {
    return
  }

  try {
    await apiClient.deleteKasaFis(id)
    await loadPageData()
  } catch (error) {
    pageError.value = error.message || 'Kasa fisi silinemedi.'
  }
}

onMounted(async () => {
  await loadPageData()
  window.addEventListener('keydown', handleShortcut, true)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleShortcut, true)
})
</script>
