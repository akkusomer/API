<template>
  <AppShell
    :active-section="'settings'"
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

      <div class="settings-grid">
        <WindowPanel title="Şirket Tanımları" class="settings-grid__summary">
          <div class="compact-list">
            <div>
              <small>şirket</small>
              <strong>{{ companyLabel }}</strong>
            </div>
            <div>
              <small>Birim Sayısı</small>
              <strong>{{ units.length }}</strong>
            </div>
            <div>
              <small>Cari Tipi Sayısı</small>
              <strong>{{ cariTypes.length }}</strong>
            </div>
            <div>
              <small>HKS Durumu</small>
              <strong>{{ hksStatusLabel }}</strong>
            </div>
          </div>
        </WindowPanel>

        <WindowPanel
          title="Birimler"
          class="settings-grid__panel"
          @mousedown="activeGrid = 'units'"
          @focusin="activeGrid = 'units'"
        >
          <template #toolbar>
            <button type="button" class="tool-button" @click="openUnitModal()">Yeni Birim</button>
          </template>

          <p class="invoice-column-hint">
            Kolon başlıklarını sürükleyerek birim listesi düzenini kişiselleştirebilirsin.
          </p>
          <p class="invoice-column-hint">
            Yukarı ve aşağı oklarla satır seçebilir, Enter ile açabilir, F2 ile yeni kayıt açabilir, F3 ile silebilirsin.
          </p>

          <div class="grid-shell">
            <table class="data-grid">
              <thead>
                <tr>
                  <th
                    v-for="column in visibleUnitColumns"
                    :key="column.key"
                    draggable="true"
                    class="invoice-column-header"
                    :class="{
                      'is-dragging': draggedUnitColumnKey === column.key,
                      'is-drop-target': dropUnitTargetKey === column.key,
                    }"
                    @dragstart="handleUnitDragStart(column.key)"
                    @dragenter.prevent="handleUnitDragEnter(column.key)"
                    @dragover.prevent="handleUnitDragOver(column.key)"
                    @drop.prevent="handleUnitDrop(column.key)"
                    @dragend="handleUnitDragEnd"
                  >
                    <span>{{ column.label }}</span>
                  </th>
                  <th class="align-right">İşlem</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="loading">
                  <td :colspan="visibleUnitColumns.length + 1" class="empty-cell">Birimler yükleniyor...</td>
                </tr>
                <tr v-else-if="units.length === 0">
                  <td :colspan="visibleUnitColumns.length + 1" class="empty-cell">Kayıt yok.</td>
                </tr>
                <tr
                  v-for="unit in units"
                  :key="unit.id"
                  class="is-clickable"
                  :class="{ 'is-selected': selectedUnitId === unit.id }"
                  @click="activeGrid = 'units'; selectUnit(unit)"
                >
                  <td v-for="column in visibleUnitColumns" :key="column.key">
                    {{ formatUnitCell(unit, column.key) }}
                  </td>
                  <td class="align-right">
                    <div class="table-actions">
                      <button type="button" class="table-action" @click="openUnitModal(unit)">Düzenle</button>
                      <button type="button" class="table-action table-action--danger" @click="removeUnit(unit.id)">Sil</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </WindowPanel>

        <WindowPanel
          title="Cari Tipleri"
          class="settings-grid__panel"
          @mousedown="activeGrid = 'cariTypes'"
          @focusin="activeGrid = 'cariTypes'"
        >
          <template #toolbar>
            <button type="button" class="tool-button" @click="openCariTypeModal()">Yeni Cari Tipi</button>
          </template>

          <p class="invoice-column-hint">
            Kolon basliklarini surukleyerek cari tipi listesi duzenini kisisellestirebilirsin.
          </p>
          <p class="invoice-column-hint">
            Yukarı ve aşağı oklarla satır seçebilir, Enter ile açabilir, F2 ile yeni kayıt açabilir, F3 ile silebilirsin.
          </p>

          <div class="grid-shell">
            <table class="data-grid">
              <thead>
                <tr>
                  <th
                    v-for="column in visibleCariTypeColumns"
                    :key="column.key"
                    draggable="true"
                    class="invoice-column-header"
                    :class="{
                      'is-dragging': draggedCariTypeColumnKey === column.key,
                      'is-drop-target': dropCariTypeTargetKey === column.key,
                    }"
                    @dragstart="handleCariTypeDragStart(column.key)"
                    @dragenter.prevent="handleCariTypeDragEnter(column.key)"
                    @dragover.prevent="handleCariTypeDragOver(column.key)"
                    @drop.prevent="handleCariTypeDrop(column.key)"
                    @dragend="handleCariTypeDragEnd"
                  >
                    <span>{{ column.label }}</span>
                  </th>
                  <th class="align-right">İşlem</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="loading">
                  <td :colspan="visibleCariTypeColumns.length + 1" class="empty-cell">Cari tipleri yukleniyor...</td>
                </tr>
                <tr v-else-if="cariTypes.length === 0">
                  <td :colspan="visibleCariTypeColumns.length + 1" class="empty-cell">Kayıt yok.</td>
                </tr>
                <tr
                  v-for="type in cariTypes"
                  :key="type.id"
                  class="is-clickable"
                  :class="{ 'is-selected': selectedCariTypeId === type.id }"
                  @click="activeGrid = 'cariTypes'; selectCariType(type)"
                >
                  <td v-for="column in visibleCariTypeColumns" :key="column.key">
                    {{ formatCariTypeCell(type, column.key) }}
                  </td>
                  <td class="align-right">
                    <div class="table-actions">
                      <button type="button" class="table-action" @click="openCariTypeModal(type)">Düzenle</button>
                      <button type="button" class="table-action table-action--danger" @click="removeCariType(type.id)">Sil</button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </WindowPanel>

        <WindowPanel title="HKS Ayarları" class="settings-grid__panel">
          <template #toolbar>
            <button type="button" class="tool-button" @click="saveHksSettings" :disabled="hksSaving">
              {{ hksSaving ? 'Kaydediliyor...' : 'HKS Bilgilerini Kaydet' }}
            </button>
          </template>

          <p class="invoice-column-hint">
            HKS kullanıcı adı, şifre ve servis şifresi her şirket için ayrı saklanır.
          </p>
          <p class="invoice-column-hint">
            Parola alanlarını boş bırakırsan mevcut kayıtlı şifreler korunur.
          </p>

          <form class="desktop-form" @submit.prevent="saveHksSettings">
            <div class="desktop-form__grid">
              <label class="field">
                <span>HKS Kullanıcı Adı</span>
                <input v-model.trim="hksForm.kullaniciAdi" type="text" autocomplete="username" required />
              </label>
              <label class="field">
                <span>HKS şifre</span>
                <input
                  v-model.trim="hksForm.password"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="hksState?.hasPassword ? 'Değiştirmek için yeniden girin' : 'İlk kayıt için zorunlu'"
                />
              </label>
              <label class="field">
                <span>HKS Servis şifresi</span>
                <input
                  v-model.trim="hksForm.servicePassword"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="hksState?.hasServicePassword ? 'Değiştirmek için yeniden girin' : 'İlk kayıt için zorunlu'"
                />
              </label>
            </div>

            <div class="compact-list">
              <div>
                <small>Kayitli HKS şifresi</small>
                <strong>{{ hksState?.hasPassword ? 'Var' : 'Yok' }}</strong>
              </div>
              <div>
                <small>Kayıtlı Servis şifresi</small>
                <strong>{{ hksState?.hasServicePassword ? 'Var' : 'Yok' }}</strong>
              </div>
              <div>
                <small>Son Güncelleme</small>
                <strong>{{ formatDateTime(hksState?.guncellemeTarihi) }}</strong>
              </div>
            </div>

            <p v-if="hksFormError" class="form-error">{{ hksFormError }}</p>
            <p v-if="hksFormSuccess" class="form-success">{{ hksFormSuccess }}</p>
          </form>
        </WindowPanel>

        <WindowPanel title="Bilgi" class="settings-grid__info">
          <div class="compact-list">
            <div>
              <small>Not</small>
              <strong>Birimler, cari tipleri ve HKS ayarları şirkete özeldir.</strong>
            </div>
            <div>
              <small>Kullanım</small>
              <strong>Stok, cari kart ve HKS sorgularında bu tanımlar kullanılır.</strong>
            </div>
          </div>
        </WindowPanel>
      </div>
    </section>

    <div v-if="unitModalOpen" class="modal-backdrop" @click.self="closeUnitModal">
      <div class="modal-window modal-window--small">
        <header class="modal-window__header">
          <h2>{{ editingUnitId ? 'Birim Düzenle' : 'Yeni Birim' }}</h2>
          <button type="button" class="ghost-icon" @click="closeUnitModal">x</button>
        </header>
        <form class="desktop-form" @submit.prevent="saveUnit">
          <div class="desktop-form__grid">
            <label class="field">
              <span>Birim Adı</span>
              <input v-model.trim="unitForm.ad" type="text" required />
            </label>
            <label class="field">
              <span>Sembol</span>
              <input v-model.trim="unitForm.sembol" type="text" required />
            </label>
          </div>
          <p v-if="unitFormError" class="form-error">{{ unitFormError }}</p>
          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeUnitModal">İptal</button>
            <button type="submit" class="primary-button" :disabled="unitSaving">
              {{ unitSaving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </footer>
        </form>
      </div>
    </div>

    <div v-if="cariTypeModalOpen" class="modal-backdrop" @click.self="closeCariTypeModal">
      <div class="modal-window modal-window--small">
        <header class="modal-window__header">
          <h2>{{ editingCariTypeId ? 'Cari Tipi Düzenle' : 'Yeni Cari Tipi' }}</h2>
          <button type="button" class="ghost-icon" @click="closeCariTypeModal">x</button>
        </header>
        <form class="desktop-form" @submit.prevent="saveCariType">
          <div class="desktop-form__grid">
            <label class="field">
              <span>Ad</span>
              <input v-model.trim="cariTypeForm.adi" type="text" required />
            </label>
            <label class="field">
              <span>Açıklama</span>
              <textarea v-model.trim="cariTypeForm.aciklama" rows="3" />
            </label>
          </div>
          <p v-if="cariTypeFormError" class="form-error">{{ cariTypeFormError }}</p>
          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeCariTypeModal">İptal</button>
            <button type="submit" class="primary-button" :disabled="cariTypeSaving">
              {{ cariTypeSaving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </footer>
        </form>
      </div>
    </div>
  </AppShell>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useColumnOrder } from '../composables/useColumnOrder'
import { useGridKeyboard } from '../composables/useGridKeyboard'
import { useSaveShortcut } from '../composables/useSaveShortcut'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const UNIT_COLUMNS = [
  { key: 'ad', label: 'Ad' },
  { key: 'sembol', label: 'Sembol' },
]

const CARI_TYPE_COLUMNS = [
  { key: 'adi', label: 'Ad' },
  { key: 'aciklama', label: 'Açıklama' },
]

const loading = ref(true)
const pageError = ref('')
const units = ref([])
const cariTypes = ref([])
const hksState = ref(null)
const activeGrid = ref('units')
const selectedUnitId = ref('')
const selectedCariTypeId = ref('')

const unitModalOpen = ref(false)
const unitSaving = ref(false)
const unitFormError = ref('')
const editingUnitId = ref('')
const unitForm = reactive({
  ad: '',
  sembol: '',
})

const cariTypeModalOpen = ref(false)
const cariTypeSaving = ref(false)
const cariTypeFormError = ref('')
const editingCariTypeId = ref('')
const cariTypeForm = reactive({
  adi: '',
  aciklama: '',
})

const hksSaving = ref(false)
const hksFormError = ref('')
const hksFormSuccess = ref('')
const hksForm = reactive({
  kullaniciAdi: '',
  password: '',
  servicePassword: '',
})

const hksStatusLabel = computed(() => {
  if (!hksState.value?.kullaniciAdi || !hksState.value?.hasPassword || !hksState.value?.hasServicePassword) {
    return 'Eksik'
  }

  return 'Hazır'
})

const statusText = computed(() => {
  return `${units.value.length} birim, ${cariTypes.value.length} cari tipi, HKS ${hksStatusLabel.value.toLowerCase()}`
})

const { route, displayName, companyLabel, navigation, topMenus, logout } = useWorkspaceShell('settings', statusText)

const {
  visibleColumns: visibleUnitColumns,
  draggedColumnKey: draggedUnitColumnKey,
  dropTargetKey: dropUnitTargetKey,
  handleColumnDragStart: handleUnitDragStart,
  handleColumnDragEnter: handleUnitDragEnter,
  handleColumnDragOver: handleUnitDragOver,
  handleColumnDrop: handleUnitDrop,
  handleColumnDragEnd: handleUnitDragEnd,
} = useColumnOrder('settings-units', UNIT_COLUMNS)

const {
  visibleColumns: visibleCariTypeColumns,
  draggedColumnKey: draggedCariTypeColumnKey,
  dropTargetKey: dropCariTypeTargetKey,
  handleColumnDragStart: handleCariTypeDragStart,
  handleColumnDragEnter: handleCariTypeDragEnter,
  handleColumnDragOver: handleCariTypeDragOver,
  handleColumnDrop: handleCariTypeDrop,
  handleColumnDragEnd: handleCariTypeDragEnd,
} = useColumnOrder('settings-cari-types', CARI_TYPE_COLUMNS)

const { selectItem: selectUnit } = useGridKeyboard({
  items: units,
  selectedKey: selectedUnitId,
  setSelectedKey: (value) => {
    selectedUnitId.value = value
  },
  enabled: computed(() => !unitModalOpen.value && !cariTypeModalOpen.value && activeGrid.value === 'units'),
  onCreate: () => openUnitModal(),
  onEnter: (unit) => openUnitModal(unit),
  onDelete: (unit) => removeUnit(unit.id),
})

const { selectItem: selectCariType } = useGridKeyboard({
  items: cariTypes,
  selectedKey: selectedCariTypeId,
  setSelectedKey: (value) => {
    selectedCariTypeId.value = value
  },
  enabled: computed(() => !unitModalOpen.value && !cariTypeModalOpen.value && activeGrid.value === 'cariTypes'),
  onCreate: () => openCariTypeModal(),
  onEnter: (type) => openCariTypeModal(type),
  onDelete: (type) => removeCariType(type.id),
})

useSaveShortcut({
  enabled: computed(() =>
    (unitModalOpen.value && !unitSaving.value)
    || (cariTypeModalOpen.value && !cariTypeSaving.value)
    || (!unitModalOpen.value && !cariTypeModalOpen.value && !hksSaving.value)
  ),
  onSave: async () => {
    if (unitModalOpen.value) {
      await saveUnit()
      return
    }

    if (cariTypeModalOpen.value) {
      await saveCariType()
      return
    }

    await saveHksSettings()
  },
})

function formatUnitCell(unit, key) {
  switch (key) {
    case 'ad':
      return unit.ad
    case 'sembol':
      return unit.sembol
    default:
      return '-'
  }
}

function formatCariTypeCell(type, key) {
  switch (key) {
    case 'adi':
      return type.adi
    case 'aciklama':
      return type.aciklama || '-'
    default:
      return '-'
  }
}

function applyHksState(value) {
  hksState.value = value || {
    kullaniciAdi: '',
    hasPassword: false,
    hasServicePassword: false,
    guncellemeTarihi: null,
  }

  hksForm.kullaniciAdi = hksState.value.kullaniciAdi || ''
  hksForm.password = ''
  hksForm.servicePassword = ''
}

async function loadSettings() {
  loading.value = true
  pageError.value = ''
  hksFormError.value = ''
  hksFormSuccess.value = ''

  try {
    const [unitsResponse, cariTypesResponse, hksResponse] = await Promise.all([
      apiClient.getUnits(),
      apiClient.getCariTypes(),
      apiClient.getHksSettings(),
    ])

    units.value = unitsResponse || []
    cariTypes.value = cariTypesResponse || []
    applyHksState(hksResponse)
  } catch (error) {
    pageError.value = error.message || 'Ayarlar yüklenemedi.'
  } finally {
    loading.value = false
  }
}

function openUnitModal(unit = null) {
  editingUnitId.value = unit?.id || ''
  unitForm.ad = unit?.ad || ''
  unitForm.sembol = unit?.sembol || ''
  unitFormError.value = ''
  unitModalOpen.value = true
}

function closeUnitModal() {
  unitModalOpen.value = false
}

async function saveUnit() {
  unitFormError.value = ''
  unitSaving.value = true
  try {
    const payload = {
      ad: unitForm.ad,
      sembol: unitForm.sembol,
    }

    if (editingUnitId.value) {
      await apiClient.updateUnit(editingUnitId.value, payload)
    } else {
      await apiClient.createUnit(payload)
    }

    closeUnitModal()
    await loadSettings()
  } catch (error) {
    unitFormError.value = error.message || 'Birim kaydedilemedi.'
  } finally {
    unitSaving.value = false
  }
}

async function removeUnit(id) {
  if (!window.confirm('Bu birimi silmek istiyor musunuz?')) return
  try {
    await apiClient.deleteUnit(id)
    await loadSettings()
  } catch (error) {
    pageError.value = error.message || 'Birim silinemedi.'
  }
}

function openCariTypeModal(type = null) {
  editingCariTypeId.value = type?.id || ''
  cariTypeForm.adi = type?.adi || ''
  cariTypeForm.aciklama = type?.aciklama || ''
  cariTypeFormError.value = ''
  cariTypeModalOpen.value = true
}

function closeCariTypeModal() {
  cariTypeModalOpen.value = false
}

async function saveCariType() {
  cariTypeFormError.value = ''
  cariTypeSaving.value = true
  try {
    const payload = {
      adi: cariTypeForm.adi,
      aciklama: cariTypeForm.aciklama || null,
    }

    if (editingCariTypeId.value) {
      await apiClient.updateCariType(editingCariTypeId.value, payload)
    } else {
      await apiClient.createCariType(payload)
    }

    closeCariTypeModal()
    await loadSettings()
  } catch (error) {
    cariTypeFormError.value = error.message || 'Cari tipi kaydedilemedi.'
  } finally {
    cariTypeSaving.value = false
  }
}

async function removeCariType(id) {
  if (!window.confirm('Bu cari tipini silmek istiyor musunuz?')) return
  try {
    await apiClient.deleteCariType(id)
    await loadSettings()
  } catch (error) {
    pageError.value = error.message || 'Cari tipi silinemedi.'
  }
}

async function saveHksSettings() {
  hksSaving.value = true
  hksFormError.value = ''
  hksFormSuccess.value = ''
  pageError.value = ''

  try {
    const response = await apiClient.saveHksSettings({
      kullaniciAdi: hksForm.kullaniciAdi,
      password: hksForm.password || null,
      servicePassword: hksForm.servicePassword || null,
    })

    applyHksState(response)
    hksFormSuccess.value = 'HKS bilgileri bu şirket için kaydedildi.'
  } catch (error) {
    hksFormError.value = error.message || 'HKS ayarları kaydedilemedi.'
  } finally {
    hksSaving.value = false
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

onMounted(loadSettings)
</script>
