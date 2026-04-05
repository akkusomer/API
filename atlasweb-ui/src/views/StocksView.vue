<template>
  <AppShell
    :active-section="'stok'"
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
      <WindowPanel title="Stok Kartları">
        <template #toolbar>
          <button type="button" class="tool-button" @click="openModal()">Yeni Kart</button>
          <button type="button" class="tool-button" @click="loadStocks">Yenile</button>
        </template>

        <div class="window-toolbar">
          <label class="inline-field inline-field--grow">
            <span>Ara</span>
            <input v-model.trim="search" type="text" placeholder="Stok adı veya kodu" />
          </label>
          <label class="inline-field">
            <span>Birim</span>
            <select v-model="selectedUnitId">
              <option value="">Tüm Birimler</option>
              <option v-for="unit in units" :key="unit.id" :value="unit.id">
                {{ unit.ad }} ({{ unit.sembol }})
              </option>
            </select>
          </label>
        </div>

        <p class="invoice-column-hint">
          Kolon başlıklarını sürükleyerek stok liste düzenini kişiselleştirebilirsin.
        </p>
        <p class="invoice-column-hint">
          Yukarı ve aşağı oklarla satır seçebilir, Enter ile açabilir, F2 ile yeni kayıt açabilir, F3 ile silebilirsin.
        </p>

        <div class="grid-shell">
          <table class="data-grid">
            <thead>
              <tr>
                <th
                  v-for="column in visibleColumns"
                  :key="column.key"
                  draggable="true"
                  class="invoice-column-header"
                  :class="{
                    'is-dragging': draggedColumnKey === column.key,
                    'is-drop-target': dropTargetKey === column.key,
                    'align-right': column.align === 'right',
                  }"
                  @dragstart="handleColumnDragStart(column.key)"
                  @dragenter.prevent="handleColumnDragEnter(column.key)"
                  @dragover.prevent="handleColumnDragOver(column.key)"
                  @drop.prevent="handleColumnDrop(column.key)"
                  @dragend="handleColumnDragEnd"
                >
                  <span>{{ column.label }}</span>
                </th>
                <th class="align-right">İşlem</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="loading">
                <td :colspan="visibleColumns.length + 1" class="empty-cell">Stok kayıtları yükleniyor...</td>
              </tr>
              <tr v-else-if="filteredStocks.length === 0">
                <td :colspan="visibleColumns.length + 1" class="empty-cell">Filtreye uygun stok kartı bulunamadı.</td>
              </tr>
              <tr
                v-for="stock in filteredStocks"
                :key="stock.id"
                class="is-clickable"
                :class="{ 'is-selected': selectedStockId === stock.id }"
                @click="selectStock(stock)"
              >
                <td
                  v-for="column in visibleColumns"
                  :key="column.key"
                  :class="{ 'align-right': column.align === 'right' }"
                >
                  {{ formatStockCell(stock, column.key) }}
                </td>
                <td class="align-right">
                  <div class="table-actions">
                    <button type="button" class="table-action" @click="openModal(stock)">Düzenle</button>
                    <button type="button" class="table-action table-action--danger" @click="removeStock(stock.id)">Sil</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </WindowPanel>
    </section>

    <div v-if="modalOpen" class="modal-backdrop" @click.self="closeModal">
      <div class="modal-window">
        <header class="modal-window__header">
          <h2>{{ editingId ? 'Stok Kartı Düzenle' : 'Yeni Stok Kartı' }}</h2>
          <button type="button" class="ghost-icon" @click="closeModal">x</button>
        </header>

        <form class="desktop-form" @submit.prevent="saveStock">
          <div class="desktop-form__grid">
            <label class="field field--full">
              <span>Stok Adı</span>
              <input v-model.trim="form.stokAdi" type="text" required />
            </label>
            <label class="field field--full">
              <span>Yedek Adı</span>
              <input v-model.trim="form.yedekAdi" type="text" />
            </label>
            <label class="field field--full">
              <span>Birim</span>
              <select v-model="form.birimId" required>
                <option value="">Birim seçiniz</option>
                <option v-for="unit in units" :key="unit.id" :value="unit.id">
                  {{ unit.ad }} ({{ unit.sembol }})
                </option>
              </select>
            </label>
          </div>

          <div class="desktop-form__grid desktop-form__grid--two">
            <label class="field">
              <span>Ürün Adı</span>
              <select v-model="form.hksUrunId">
                <option value="">Ürün seçiniz</option>
                <option v-for="product in hksProducts" :key="product.hksUrunId" :value="product.hksUrunId">
                  {{ product.ad }}
                </option>
              </select>
            </label>
            <label class="field">
              <span>Üretim Şekli</span>
              <select v-model="form.hksUretimSekliId">
                <option value="">Üretim şekli seçiniz</option>
                <option
                  v-for="productionShape in hksProductionShapes"
                  :key="productionShape.hksUretimSekliId"
                  :value="productionShape.hksUretimSekliId"
                >
                  {{ productionShape.ad }}
                </option>
              </select>
            </label>
            <label class="field">
              <span>Ürün Cinsi</span>
              <select v-model="form.hksUrunCinsiId" :disabled="!form.hksUrunId || loadingProductKinds">
                <option value="">
                  {{ loadingProductKinds ? 'Ürün cinsleri yükleniyor...' : 'Ürün cinsi seçiniz' }}
                </option>
                <option
                  v-for="productKind in filteredProductKinds"
                  :key="productKind.hksUrunCinsiId"
                  :value="productKind.hksUrunCinsiId"
                >
                  {{ productKind.ad }}
                </option>
              </select>
            </label>
            <label class="field">
              <span>Niteliği</span>
              <select v-model="form.hksNitelik" :disabled="availableNitelikOptions.length === 0">
                <option value="">Nitelik seçiniz</option>
                <option v-for="option in availableNitelikOptions" :key="option" :value="option">
                  {{ option }}
                </option>
              </select>
            </label>
          </div>

          <p v-if="formError" class="form-error">{{ formError }}</p>

          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeModal">İptal</button>
            <button type="submit" class="primary-button" :disabled="saving">
              {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </footer>
        </form>
      </div>
    </div>
  </AppShell>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useColumnOrder } from '../composables/useColumnOrder'
import { useGridKeyboard } from '../composables/useGridKeyboard'
import { useSaveShortcut } from '../composables/useSaveShortcut'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const DEFAULT_COLUMNS = [
  { key: 'stokKodu', label: 'Kodu' },
  { key: 'stokAdi', label: 'Stok Adı' },
  { key: 'yedekAdi', label: 'Yedek Adı' },
  { key: 'birimAdi', label: 'Birimi' },
  { key: 'kayitTarihi', label: 'Kayıt' },
]

const HKS_NITELIK_OPTIONS = ['Yerli', 'İthal']

const loading = ref(true)
const saving = ref(false)
const stocks = ref([])
const units = ref([])
const hksProducts = ref([])
const hksProductionShapes = ref([])
const hksProductKinds = ref([])
const loadingProductKinds = ref(false)
const hydratingHksSelections = ref(false)
const search = ref('')
const selectedUnitId = ref('')
const modalOpen = ref(false)
const editingId = ref('')
const formError = ref('')
const pageError = ref('')
const selectedStockId = ref('')
const form = reactive({
  stokAdi: '',
  yedekAdi: '',
  birimId: '',
  hksUrunId: '',
  hksUretimSekliId: '',
  hksUrunCinsiId: '',
  hksNitelik: '',
})

const statusText = computed(() => `${filteredStocks.value.length} stok kaydı listeleniyor`)

const { route, displayName, companyLabel, navigation, topMenus, logout } = useWorkspaceShell('stok', statusText)

const {
  visibleColumns,
  draggedColumnKey,
  dropTargetKey,
  handleColumnDragStart,
  handleColumnDragEnter,
  handleColumnDragOver,
  handleColumnDrop,
  handleColumnDragEnd,
} = useColumnOrder('stock-list', DEFAULT_COLUMNS)

const filteredStocks = computed(() =>
  stocks.value.filter((stock) => {
    const term = search.value.trim().toLowerCase()
    const matchesSearch = !term
      || [stock.stokKodu, stock.stokAdi, stock.yedekAdi]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(term))
    const matchesUnit = !selectedUnitId.value || stock.birimId === selectedUnitId.value
    return matchesSearch && matchesUnit
  })
)

const filteredProductKinds = computed(() => hksProductKinds.value.filter((productKind) => {
  if (!form.hksUrunId || productKind.hksUrunId !== Number(form.hksUrunId)) {
    return false
  }

  if (form.hksUretimSekliId && productKind.hksUretimSekliId !== Number(form.hksUretimSekliId)) {
    return false
  }

  if (form.hksNitelik) {
    return resolveNitelik(productKind.ithalMi) === form.hksNitelik
  }

  return true
}))

const availableNitelikOptions = computed(() => {
  const options = new Set(HKS_NITELIK_OPTIONS)

  for (const productKind of hksProductKinds.value) {
    if (!form.hksUrunId || productKind.hksUrunId !== Number(form.hksUrunId)) {
      continue
    }

    if (form.hksUretimSekliId && productKind.hksUretimSekliId !== Number(form.hksUretimSekliId)) {
      continue
    }

    const label = resolveNitelik(productKind.ithalMi)
    if (label) {
      options.add(label)
    }
  }

  return Array.from(options)
})

const { selectItem: selectStock } = useGridKeyboard({
  items: filteredStocks,
  selectedKey: selectedStockId,
  setSelectedKey: (value) => {
    selectedStockId.value = value
  },
  enabled: computed(() => !modalOpen.value),
  onCreate: () => openModal(),
  onEnter: (stock) => openModal(stock),
  onDelete: (stock) => removeStock(stock.id),
})

useSaveShortcut({
  enabled: computed(() => modalOpen.value && !saving.value),
  onSave: saveStock,
})

function formatDate(value) {
  if (!value) return '-'

  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(value))
}

function formatStockCell(stock, key) {
  switch (key) {
    case 'stokKodu':
      return stock.stokKodu
    case 'stokAdi':
      return stock.stokAdi
    case 'yedekAdi':
      return stock.yedekAdi || '-'
    case 'birimAdi':
      return stock.birimAdi || '-'
    case 'kayitTarihi':
      return formatDate(stock.kayitTarihi)
    default:
      return '-'
  }
}

async function loadStocks() {
  loading.value = true
  pageError.value = ''

  try {
    const [stockResponse, unitsResponse, productsResponse, productionShapesResponse] = await Promise.all([
      apiClient.getStocks(),
      apiClient.getUnits(),
      loadSavedHksProducts(),
      loadSavedHksProductionShapes(),
    ])

    stocks.value = stockResponse?.veriler || []
    units.value = unitsResponse || []
    hksProducts.value = productsResponse
    hksProductionShapes.value = productionShapesResponse
  } catch (error) {
    pageError.value = error.message || 'Stok kayıtları yüklenemedi.'
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.stokAdi = ''
  form.yedekAdi = ''
  form.birimId = ''
  form.hksUrunId = ''
  form.hksUretimSekliId = ''
  form.hksUrunCinsiId = ''
  form.hksNitelik = ''
  hksProductKinds.value = []
  formError.value = ''
  editingId.value = ''
}

async function openModal(stock = null) {
  resetForm()

  if (stock) {
    hydratingHksSelections.value = true
    editingId.value = stock.id
    form.stokAdi = stock.stokAdi || ''
    form.yedekAdi = stock.yedekAdi || ''
    form.birimId = stock.birimId || ''
    form.hksUrunId = stock.hksUrunId ?? ''
    form.hksUretimSekliId = stock.hksUretimSekliId ?? ''
    form.hksUrunCinsiId = stock.hksUrunCinsiId ?? ''
    form.hksNitelik = stock.hksNitelik || ''
  }

  modalOpen.value = true

  if (form.hksUrunId) {
    await loadProductKinds(form.hksUrunId)
  }

  hydratingHksSelections.value = false
}

function closeModal() {
  modalOpen.value = false
}

async function saveStock() {
  formError.value = ''
  saving.value = true

  try {
    const payload = {
      stokAdi: form.stokAdi,
      yedekAdi: form.yedekAdi,
      hksUrunId: toNullableNumber(form.hksUrunId),
      hksUretimSekliId: toNullableNumber(form.hksUretimSekliId),
      hksUrunCinsiId: toNullableNumber(form.hksUrunCinsiId),
      hksNitelik: form.hksNitelik || null,
      birimId: form.birimId,
    }

    if (editingId.value) {
      await apiClient.updateStock(editingId.value, payload)
    } else {
      await apiClient.createStock(payload)
    }

    closeModal()
    await loadStocks()
  } catch (error) {
    formError.value = error.message || 'Stok kartı kaydedilemedi.'
  } finally {
    saving.value = false
  }
}

async function removeStock(id) {
  if (!window.confirm('Bu stok kartını silmek istiyor musunuz?')) return

  try {
    await apiClient.deleteStock(id)
    await loadStocks()
  } catch (error) {
    pageError.value = error.message || 'Stok kartı silinemedi.'
  }
}

async function loadSavedHksProducts() {
  try {
    const savedProducts = await apiClient.getSavedHksUrunler()
    if (Array.isArray(savedProducts) && savedProducts.length > 0) {
      return savedProducts
    }
  } catch {
    // Saved dictionary is optional, live sync fallback below.
  }

  const syncedProducts = await apiClient.getHksUrunler()
  return Array.isArray(syncedProducts) ? syncedProducts : []
}

async function loadSavedHksProductionShapes() {
  try {
    const savedProductionShapes = await apiClient.getSavedHksUretimSekilleri()
    if (Array.isArray(savedProductionShapes) && savedProductionShapes.length > 0) {
      return savedProductionShapes
    }
  } catch {
    // Saved dictionary is optional, live sync fallback below.
  }

  const syncedProductionShapes = await apiClient.getHksUretimSekilleri()
  return Array.isArray(syncedProductionShapes) ? syncedProductionShapes : []
}

async function loadProductKinds(productId) {
  if (!productId) {
    hksProductKinds.value = []
    return
  }

  loadingProductKinds.value = true

  try {
    const savedProductKinds = await apiClient.getSavedHksUrunCinsleri(productId)
    if (Array.isArray(savedProductKinds) && savedProductKinds.length > 0) {
      hksProductKinds.value = savedProductKinds
      return
    }

    const syncedProductKinds = await apiClient.getHksUrunCinsleri(productId)
    hksProductKinds.value = Array.isArray(syncedProductKinds) ? syncedProductKinds : []
  } catch (error) {
    hksProductKinds.value = []
    formError.value = error.message || 'HKS ürün cinsleri alınamadı.'
  } finally {
    loadingProductKinds.value = false
  }
}

function resolveNitelik(isImported) {
  if (isImported === true) {
    return 'İthal'
  }

  if (isImported === false) {
    return 'Yerli'
  }

  return null
}

function toNullableNumber(value) {
  if (value === '' || value === null || value === undefined) {
    return null
  }

  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

watch(() => form.hksUrunId, async (nextValue, previousValue) => {
  if (nextValue === previousValue) {
    return
  }

  if (hydratingHksSelections.value) {
    return
  }

  form.hksUrunCinsiId = ''
  form.hksNitelik = ''

  if (!nextValue) {
    hksProductKinds.value = []
    return
  }

  await loadProductKinds(nextValue)
})

watch(() => form.hksUretimSekliId, () => {
  if (form.hksUrunCinsiId && !filteredProductKinds.value.some((item) => item.hksUrunCinsiId === Number(form.hksUrunCinsiId))) {
    form.hksUrunCinsiId = ''
  }

  if (form.hksNitelik && !availableNitelikOptions.value.includes(form.hksNitelik)) {
    form.hksNitelik = ''
  }
})

watch(() => form.hksNitelik, () => {
  if (form.hksUrunCinsiId && !filteredProductKinds.value.some((item) => item.hksUrunCinsiId === Number(form.hksUrunCinsiId))) {
    form.hksUrunCinsiId = ''
  }
})

watch(() => form.hksUrunCinsiId, (nextValue) => {
  if (!nextValue) {
    return
  }

  const selectedProductKind = hksProductKinds.value.find((item) => item.hksUrunCinsiId === Number(nextValue))
  const selectedNitelik = resolveNitelik(selectedProductKind?.ithalMi)
  if (selectedNitelik) {
    form.hksNitelik = selectedNitelik
  }
})

onMounted(loadStocks)
</script>
