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
          <p class="admin-hero-card__eyebrow">Yöneticiler</p>
          <h1>Sistem yöneticileri</h1>
          <p>Sistem yöneticilerini ayrı bir sayfada yönet, aktifliği izle ve liste düzenini kişiselleştir.</p>
        </div>

        <div class="admin-hero-card__actions">
          <button type="button" class="tool-button" @click="openModal()">Yeni Yönetici</button>
          <button type="button" class="tool-button" @click="loadAdmins">Yenile</button>
        </div>
      </section>

      <section class="admin-section">
        <header class="admin-section__header">
          <div>
            <p class="admin-section__eyebrow">Özet</p>
            <h2>Yönetici görünümü</h2>
            <p>Bu ekran sadece sistem yöneticileri için ayrıldı. Kullanıcı tarafı ve şirket tarafı burada karışmaz.</p>
          </div>
        </header>

        <div class="selection-summary">
          <div>
            <small>Toplam Yönetici</small>
            <strong>{{ admins.length }}</strong>
          </div>
          <div>
            <small>Aktif</small>
            <strong>{{ activeAdminCount }}</strong>
          </div>
          <div>
            <small>Pasif</small>
            <strong>{{ passiveAdminCount }}</strong>
          </div>
        </div>
      </section>

      <WindowPanel title="Sistem Yöneticileri">
        <template #toolbar>
        </template>

        <div class="window-toolbar">
          <label class="inline-field inline-field--grow">
            <span>Ara</span>
            <input v-model.trim="search" type="text" placeholder="Ad, e-posta veya telefon ara" />
          </label>
        </div>

        <p class="invoice-column-hint">
          Kolon başlıklarını sürükleyerek sistem yöneticisi listesi düzenini kişiselleştirebilirsin.
        </p>
        <p class="invoice-column-hint">
          Yukarı ve aşağı oklarla satır seçebilir, Enter ile açabilir, F2 ile yeni yönetici açabilir, F3 ile silebilirsin.
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
                <td :colspan="visibleColumns.length + 1" class="empty-cell">Yöneticiler yükleniyor...</td>
              </tr>
              <tr v-else-if="filteredAdmins.length === 0">
                <td :colspan="visibleColumns.length + 1" class="empty-cell">Filtreye uygun yönetici bulunamadı.</td>
              </tr>
              <tr
                v-for="admin in filteredAdmins"
                :key="admin.id"
                class="is-clickable"
                :class="{ 'is-selected': selectedAdminId === admin.id }"
                @click="selectAdmin(admin)"
              >
                <td v-for="column in visibleColumns" :key="column.key">
                  {{ formatAdminCell(admin, column.key) }}
                </td>
                <td class="align-right">
                  <div class="table-actions">
                    <button type="button" class="table-action" @click="openModal(admin)">Düzenle</button>
                    <button type="button" class="table-action table-action--danger" @click="removeAdmin(admin.id)">
                      Pasife Al
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </WindowPanel>
    </section>

    <div v-if="modalOpen" class="modal-backdrop" @click.self="closeModal">
      <div class="modal-window modal-window--small">
        <header class="modal-window__header">
          <h2>{{ editingId ? 'Yönetici Düzenle' : 'Yeni Yönetici' }}</h2>
          <button type="button" class="ghost-icon" @click="closeModal">×</button>
        </header>

        <form class="desktop-form" @submit.prevent="saveAdmin">
          <div class="desktop-form__grid">
            <label class="field">
              <span>Ad</span>
              <input v-model.trim="form.ad" type="text" required />
            </label>
            <label class="field">
              <span>Soyad</span>
              <input v-model.trim="form.soyad" type="text" required />
            </label>
            <label class="field">
              <span>E-Posta</span>
              <input v-model.trim="form.ePosta" type="email" :disabled="Boolean(editingId)" :required="!editingId" />
            </label>
            <label class="field">
              <span>Telefon</span>
              <input v-model.trim="form.telefon" type="text" />
            </label>
            <label v-if="!editingId" class="field field--full">
              <span>Şifre</span>
              <input v-model="form.sifre" type="password" minlength="8" required />
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
import { computed, onMounted, reactive, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useAdminShell } from '../composables/useAdminShell'
import { useColumnOrder } from '../composables/useColumnOrder'
import { useGridKeyboard } from '../composables/useGridKeyboard'
import { useSaveShortcut } from '../composables/useSaveShortcut'
import { apiClient } from '../services/api'

const ADMIN_COLUMNS = [
  { key: 'adSoyad', label: 'Ad Soyad' },
  { key: 'ePosta', label: 'E-Posta' },
  { key: 'telefon', label: 'Telefon' },
  { key: 'rol', label: 'Rol' },
  { key: 'aktifMi', label: 'Durum' },
]

const loading = ref(true)
const saving = ref(false)
const pageError = ref('')
const formError = ref('')
const search = ref('')
const admins = ref([])
const selectedAdminId = ref('')
const modalOpen = ref(false)
const editingId = ref('')

const form = reactive({
  ad: '',
  soyad: '',
  ePosta: '',
  telefon: '',
  sifre: '',
})

const statusText = computed(() => `${admins.value.length} sistem yöneticisi`)

const { route, displayName, navigation, topMenus, companyLabel, logout } = useAdminShell(statusText)

const {
  visibleColumns,
  draggedColumnKey,
  dropTargetKey,
  handleColumnDragStart,
  handleColumnDragEnter,
  handleColumnDragOver,
  handleColumnDrop,
  handleColumnDragEnd,
} = useColumnOrder('admin-managers', ADMIN_COLUMNS)

const filteredAdmins = computed(() => {
  const term = search.value.trim().toLowerCase()
  if (!term) {
    return admins.value
  }

  return admins.value.filter((admin) =>
    [admin.ad, admin.soyad, admin.ePosta, admin.telefon]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(term))
  )
})

const activeAdminCount = computed(() => admins.value.filter((admin) => admin.aktifMi).length)
const passiveAdminCount = computed(() => admins.value.filter((admin) => !admin.aktifMi).length)

const { selectItem: selectAdmin } = useGridKeyboard({
  items: filteredAdmins,
  selectedKey: selectedAdminId,
  setSelectedKey: (value) => {
    selectedAdminId.value = value
  },
  enabled: computed(() => !modalOpen.value),
  onCreate: () => openModal(),
  onEnter: (admin) => openModal(admin),
  onDelete: (admin) => removeAdmin(admin.id),
})

useSaveShortcut({
  enabled: computed(() => modalOpen.value && !saving.value),
  onSave: saveAdmin,
})

function formatStatus(value) {
  return value ? 'Aktif' : 'Pasif'
}

function formatAdminCell(admin, key) {
  switch (key) {
    case 'adSoyad':
      return `${admin.ad || ''} ${admin.soyad || ''}`.trim() || '-'
    case 'ePosta':
      return admin.ePosta || '-'
    case 'telefon':
      return admin.telefon || '-'
    case 'rol':
      return admin.rol === 'Admin' ? 'Yönetici' : admin.rol || '-'
    case 'aktifMi':
      return formatStatus(admin.aktifMi)
    default:
      return '-'
  }
}

async function loadAdmins() {
  loading.value = true
  pageError.value = ''

  try {
    const response = await apiClient.getSystemAdmins()
    admins.value = Array.isArray(response) ? response : []
  } catch (error) {
    pageError.value = error.message || 'Sistem yöneticileri yüklenemedi.'
  } finally {
    loading.value = false
  }
}

function resetForm() {
  Object.assign(form, {
    ad: '',
    soyad: '',
    ePosta: '',
    telefon: '',
    sifre: '',
  })
  editingId.value = ''
  formError.value = ''
}

function openModal(admin = null) {
  resetForm()

  if (admin) {
    editingId.value = admin.id
    form.ad = admin.ad || ''
    form.soyad = admin.soyad || ''
    form.ePosta = admin.ePosta || ''
    form.telefon = admin.telefon || ''
  }

  modalOpen.value = true
}

function closeModal() {
  modalOpen.value = false
}

async function saveAdmin() {
  formError.value = ''
  saving.value = true

  try {
    if (editingId.value) {
      await apiClient.updateSystemAdmin(editingId.value, {
        ad: form.ad,
        soyad: form.soyad,
        telefon: form.telefon || null,
      })
    } else {
      await apiClient.createSystemAdmin({
        ad: form.ad,
        soyad: form.soyad,
        ePosta: form.ePosta,
        telefon: form.telefon || null,
        sifre: form.sifre,
      })
    }

    closeModal()
    await loadAdmins()
  } catch (error) {
    formError.value = error.message || 'Sistem yöneticisi kaydedilemedi.'
  } finally {
    saving.value = false
  }
}

async function removeAdmin(id) {
  if (!window.confirm('Bu sistem yöneticisini pasife almak istiyor musunuz?')) {
    return
  }

  try {
    await apiClient.deactivateUser(id)
    await loadAdmins()
  } catch (error) {
    pageError.value = error.message || 'Sistem yöneticisi pasife alınamadı.'
  }
}

onMounted(loadAdmins)
</script>
