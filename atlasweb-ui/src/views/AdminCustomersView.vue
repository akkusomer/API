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
          <p class="admin-hero-card__eyebrow">Şirketler</p>
          <h1>Şirket yönetimi</h1>
          <p>Şirket kayıtlarını ve seçili şirkete bağlı kullanıcıları bu sayfadan ayrı ayrı yönet.</p>
        </div>

        <div class="admin-hero-card__actions">
          <button type="button" class="tool-button" @click="openCustomerModal()">Yeni Şirket</button>
          <button
            type="button"
            class="tool-button"
            :disabled="!selectedCustomerId"
            @click="openUserModal()"
          >
            Kullanıcı Ekle
          </button>
          <button type="button" class="tool-button" @click="loadCustomers">Yenile</button>
        </div>
      </section>

      <section class="admin-section">
        <header class="admin-section__header">
          <div>
            <p class="admin-section__eyebrow">Seçili Şirket</p>
            <h2>{{ selectedCustomer?.unvan || 'Şirket seçilmedi' }}</h2>
            <p>Liste solda, şirket kullanıcıları sağda durur. Her tablo kendi kolon düzenini ayrı saklar.</p>
          </div>
        </header>

        <div class="selection-summary">
          <div>
            <small>Şirket Kodu</small>
            <strong>{{ selectedCustomer?.musteriKodu || '-' }}</strong>
          </div>
          <div>
            <small>Paket</small>
            <strong>{{ formatPackage(selectedCustomer?.paketTipi) }}</strong>
          </div>
          <div>
            <small>Kullanıcı Sayısı</small>
            <strong>{{ customerUsers.length }}</strong>
          </div>
        </div>
      </section>

      <section class="admin-section">
        <header class="admin-section__header">
          <div>
            <p class="admin-section__eyebrow">Varsayılan Tanımlar</p>
            <h2>Birim ve Cari Tipleri</h2>
            <p>Seçili şirkete ait tanımları buradan görür, eksikse varsayılanları tek tuşla tekrar eklersin.</p>
          </div>
          <div class="admin-hero-card__actions">
            <button
              type="button"
              class="tool-button"
              :disabled="!selectedCustomerId || referenceLoading"
              @click="syncCustomerDefaults"
            >
              {{ referenceLoading ? 'Güncelleniyor...' : 'Varsayılanları Uygula' }}
            </button>
            <button
              type="button"
              class="tool-button"
              :disabled="!selectedCustomerId || citySyncLoading"
              @click="syncHksCitiesForAllCustomers"
            >
              {{ citySyncLoading ? 'İller çekiliyor...' : 'HKS İllerini Tüm Şirketlere Yaz' }}
            </button>
          </div>
        </header>

        <div class="selection-summary">
          <div>
            <small>Birim Sayısı</small>
            <strong>{{ customerUnits.length }}</strong>
          </div>
          <div>
            <small>Cari Tip Sayısı</small>
            <strong>{{ customerCariTypes.length }}</strong>
          </div>
          <div>
            <small>Durum</small>
            <strong>{{ selectedCustomerId ? (referenceLoading ? 'Yükleniyor' : 'Hazır') : '-' }}</strong>
          </div>
        </div>

        <p class="invoice-column-hint">
          İl listesi seçili şirketin HKS hesabıyla çekilir ve tüm şirketlerin kullandığı ortak HKS şehir listesine yazılır.
        </p>
        <p v-if="referenceError" class="form-error">{{ referenceError }}</p>
        <p v-if="citySyncMessage" class="form-success">{{ citySyncMessage }}</p>

        <div class="admin-reference-grid">
          <article class="admin-log-block">
            <header><span>Birimler</span></header>
            <div class="admin-chip-list">
              <span v-for="unit in customerUnits" :key="unit.id" class="type-chip">
                {{ unit.ad }} ({{ unit.sembol }})
              </span>
              <span v-if="customerUnits.length === 0" class="type-chip">Tanım yok</span>
            </div>
          </article>

          <article class="admin-log-block">
            <header><span>Cari Tipleri</span></header>
            <div class="admin-chip-list">
              <span v-for="type in customerCariTypes" :key="type.id" class="type-chip">
                {{ type.adi }}
              </span>
              <span v-if="customerCariTypes.length === 0" class="type-chip">Tanım yok</span>
            </div>
          </article>
        </div>
      </section>

      <div class="admin-group-grid">
        <WindowPanel
          title="Şirket Listesi"
          class="admin-col-7 admin-section--column"
          @mousedown="activeGrid = 'customers'"
          @focusin="activeGrid = 'customers'"
        >
          <template #toolbar>
          </template>

          <div class="window-toolbar">
            <label class="inline-field inline-field--grow">
              <span>Ara</span>
              <input v-model.trim="customerSearch" type="text" placeholder="Kod, ünvan veya e-posta ara" />
            </label>
          </div>

          <p class="invoice-column-hint">
            Kolon başlıklarını sürükleyerek şirket listesi düzenini kişiselleştirebilirsin.
          </p>
        <p class="invoice-column-hint">
          Yukarı ve aşağı oklarla satır seçebilir, Enter ile açabilir, F2 ile yeni şirket açabilir, F3 ile silebilirsin.
        </p>

          <div class="grid-shell">
            <table class="data-grid">
              <thead>
                <tr>
                  <th
                    v-for="column in visibleCustomerColumns"
                    :key="column.key"
                    draggable="true"
                    class="invoice-column-header"
                    :class="{
                      'is-dragging': draggedCustomerColumnKey === column.key,
                      'is-drop-target': dropCustomerTargetKey === column.key,
                    }"
                    @dragstart="handleCustomerDragStart(column.key)"
                    @dragenter.prevent="handleCustomerDragEnter(column.key)"
                    @dragover.prevent="handleCustomerDragOver(column.key)"
                    @drop.prevent="handleCustomerDrop(column.key)"
                    @dragend="handleCustomerDragEnd"
                  >
                    <span>{{ column.label }}</span>
                  </th>
                  <th class="align-right">İşlem</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="loading">
                  <td :colspan="visibleCustomerColumns.length + 1" class="empty-cell">Şirketler yükleniyor...</td>
                </tr>
                <tr v-else-if="filteredCustomers.length === 0">
                  <td :colspan="visibleCustomerColumns.length + 1" class="empty-cell">Filtreye uygun şirket bulunamadı.</td>
                </tr>
                <tr
                  v-for="customer in filteredCustomers"
                  :key="customer.id"
                  class="is-clickable"
                  :class="{ 'is-selected': selectedCustomerId === customer.id }"
                  @click="activeGrid = 'customers'; selectCustomerRow(customer)"
                >
                  <td v-for="column in visibleCustomerColumns" :key="column.key">
                    {{ formatCustomerCell(customer, column.key) }}
                  </td>
                  <td class="align-right">
                    <div class="table-actions">
                      <button type="button" class="table-action" @click.stop="openCustomerModal(customer)">Düzenle</button>
                      <button
                        type="button"
                        class="table-action table-action--danger"
                        @click.stop="removeCustomer(customer.id)"
                      >
                        Pasife Al
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </WindowPanel>

        <WindowPanel
          title="Şirket Kullanıcıları"
          class="admin-col-5 admin-section--column"
          @mousedown="activeGrid = 'users'"
          @focusin="activeGrid = 'users'"
        >
          <template #toolbar>
            <button
              type="button"
              class="tool-button"
              :disabled="!selectedCustomerId"
              @click="openUserModal()"
            >
              Yeni Kullanıcı
            </button>
          </template>

          <div class="window-toolbar">
            <label class="inline-field inline-field--grow">
              <span>Ara</span>
              <input
                v-model.trim="userSearch"
                type="text"
                :disabled="!selectedCustomerId"
                placeholder="Ad, e-posta veya telefon ara"
              />
            </label>
          </div>

          <p class="invoice-column-hint">
            Kolon başlıklarını sürükleyerek seçili şirket kullanıcı listesi düzenini kişiselleştirebilirsin.
          </p>
        <p class="invoice-column-hint">
          Yukarı ve aşağı oklarla satır seçebilir, F2 ile yeni kullanıcı açabilir, F3 ile silebilirsin.
        </p>

          <div class="grid-shell">
            <table class="data-grid">
              <thead>
                <tr>
                  <th
                    v-for="column in visibleUserColumns"
                    :key="column.key"
                    draggable="true"
                    class="invoice-column-header"
                    :class="{
                      'is-dragging': draggedUserColumnKey === column.key,
                      'is-drop-target': dropUserTargetKey === column.key,
                    }"
                    @dragstart="handleUserDragStart(column.key)"
                    @dragenter.prevent="handleUserDragEnter(column.key)"
                    @dragover.prevent="handleUserDragOver(column.key)"
                    @drop.prevent="handleUserDrop(column.key)"
                    @dragend="handleUserDragEnd"
                  >
                    <span>{{ column.label }}</span>
                  </th>
                  <th class="align-right">İşlem</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="usersLoading">
                  <td :colspan="visibleUserColumns.length + 1" class="empty-cell">Kullanıcılar yükleniyor...</td>
                </tr>
                <tr v-else-if="!selectedCustomerId">
                  <td :colspan="visibleUserColumns.length + 1" class="empty-cell">Önce bir şirket seçin.</td>
                </tr>
                <tr v-else-if="filteredCustomerUsers.length === 0">
                  <td :colspan="visibleUserColumns.length + 1" class="empty-cell">Kullanıcı bulunamadı.</td>
                </tr>
                <tr
                  v-for="user in filteredCustomerUsers"
                  :key="user.id"
                  class="is-clickable"
                  :class="{ 'is-selected': selectedCustomerUserId === user.id }"
                  @click="activeGrid = 'users'; selectCustomerUser(user)"
                >
                  <td v-for="column in visibleUserColumns" :key="column.key">
                    {{ formatUserCell(user, column.key) }}
                  </td>
                  <td class="align-right">
                    <div class="table-actions">
                      <button
                        type="button"
                        class="table-action table-action--danger"
                        @click="removeCustomerUser(user.id)"
                      >
                        Pasife Al
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </WindowPanel>
      </div>
    </section>

    <div v-if="customerModalOpen" class="modal-backdrop" @click.self="closeCustomerModal">
      <div class="modal-window modal-window--wide">
        <header class="modal-window__header">
          <h2>{{ editingCustomerId ? 'Şirket Düzenle' : 'Yeni Şirket' }}</h2>
          <button type="button" class="ghost-icon" @click="closeCustomerModal">×</button>
        </header>

        <form class="desktop-form" @submit.prevent="saveCustomer">
          <div class="desktop-form__grid desktop-form__grid--two">
            <label class="field">
              <span>Şirket Kodu</span>
              <input v-model.trim="customerForm.musteriKodu" type="text" required />
            </label>
            <label class="field">
              <span>Paket</span>
              <select v-model.number="customerForm.paketTipi" required>
                <option :value="0">Standart</option>
                <option :value="1">Premium</option>
                <option :value="2">Kurumsal</option>
              </select>
            </label>
            <label class="field field--full">
              <span>Ünvan</span>
              <input v-model.trim="customerForm.unvan" type="text" required />
            </label>
            <label class="field">
              <span>Kimlik Türü</span>
              <select v-model.number="customerForm.kimlikTuru" required>
                <option :value="0">VKN</option>
                <option :value="1">TCKN</option>
              </select>
            </label>
            <label class="field">
              <span>VKN / TCKN</span>
              <input v-model.trim="customerForm.vergiNo" type="text" />
            </label>
            <label class="field">
              <span>Vergi Dairesi</span>
              <input v-model.trim="customerForm.vergiDairesi" type="text" />
            </label>
            <label class="field">
              <span>GSM</span>
              <input v-model.trim="customerForm.gsmNo" type="text" />
            </label>
            <label class="field">
              <span>E-Posta</span>
              <input v-model.trim="customerForm.ePosta" type="email" />
            </label>
            <label class="field">
              <span>İl</span>
              <input v-model.trim="customerForm.il" type="text" />
            </label>
            <label class="field">
              <span>İlçe</span>
              <input v-model.trim="customerForm.ilce" type="text" />
            </label>
            <label class="field field--full">
              <span>Adres</span>
              <textarea v-model.trim="customerForm.adresDetay" rows="3" />
            </label>
            <label class="field">
              <span>Durum</span>
              <select v-model="customerForm.aktifMi">
                <option :value="true">Aktif</option>
                <option :value="false">Pasif</option>
              </select>
            </label>
          </div>

          <p v-if="customerFormError" class="form-error">{{ customerFormError }}</p>

          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeCustomerModal">İptal</button>
            <button type="submit" class="primary-button" :disabled="customerSaving">
              {{ customerSaving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
          </footer>
        </form>
      </div>
    </div>

    <div v-if="userModalOpen" class="modal-backdrop" @click.self="closeUserModal">
      <div class="modal-window modal-window--small">
        <header class="modal-window__header">
          <h2>Şirket Kullanıcısı Ekle</h2>
          <button type="button" class="ghost-icon" @click="closeUserModal">×</button>
        </header>

        <form class="desktop-form" @submit.prevent="saveCustomerUser">
          <div class="desktop-form__grid">
            <label class="field">
              <span>Ad</span>
              <input v-model.trim="userForm.ad" type="text" required />
            </label>
            <label class="field">
              <span>Soyad</span>
              <input v-model.trim="userForm.soyad" type="text" required />
            </label>
            <label class="field">
              <span>E-Posta</span>
              <input v-model.trim="userForm.ePosta" type="email" required />
            </label>
            <label class="field">
              <span>Telefon</span>
              <input v-model.trim="userForm.telefon" type="text" />
            </label>
            <label class="field field--full">
              <span>Şifre</span>
              <input v-model="userForm.sifre" type="password" minlength="8" required />
            </label>
          </div>

          <p v-if="userFormError" class="form-error">{{ userFormError }}</p>

          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeUserModal">İptal</button>
            <button type="submit" class="primary-button" :disabled="userSaving">
              {{ userSaving ? 'Kaydediliyor...' : 'Kaydet' }}
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

const CUSTOMER_COLUMNS = [
  { key: 'musteriKodu', label: 'Kod' },
  { key: 'unvan', label: 'Ünvan' },
  { key: 'ePosta', label: 'E-Posta' },
  { key: 'paketTipi', label: 'Paket' },
  { key: 'aktifMi', label: 'Durum' },
  { key: 'kayitTarihi', label: 'Kayıt' },
]

const USER_COLUMNS = [
  { key: 'adSoyad', label: 'Ad Soyad' },
  { key: 'ePosta', label: 'E-Posta' },
  { key: 'telefon', label: 'Telefon' },
  { key: 'rol', label: 'Rol' },
  { key: 'aktifMi', label: 'Durum' },
]

const loading = ref(true)
const usersLoading = ref(false)
const pageError = ref('')
const customerSearch = ref('')
const userSearch = ref('')
const customers = ref([])
const customerUsers = ref([])
const customerUnits = ref([])
const customerCariTypes = ref([])
const referenceLoading = ref(false)
const referenceError = ref('')
const citySyncLoading = ref(false)
const citySyncMessage = ref('')
const activeGrid = ref('customers')
const selectedCustomerId = ref('')
const selectedCustomerUserId = ref('')

const customerModalOpen = ref(false)
const customerSaving = ref(false)
const editingCustomerId = ref('')
const customerFormError = ref('')
const customerForm = reactive({
  musteriKodu: '',
  unvan: '',
  vergiNo: '',
  vergiDairesi: '',
  kimlikTuru: 0,
  gsmNo: '',
  ePosta: '',
  il: '',
  ilce: '',
  adresDetay: '',
  paketTipi: 0,
  aktifMi: true,
})

const userModalOpen = ref(false)
const userSaving = ref(false)
const userFormError = ref('')
const userForm = reactive({
  ad: '',
  soyad: '',
  ePosta: '',
  telefon: '',
  sifre: '',
})

const statusText = computed(() => `${customers.value.length} şirket, ${customerUsers.value.length} kullanıcı`)

const { route, displayName, navigation, topMenus, companyLabel, logout } = useAdminShell(statusText)

const {
  visibleColumns: visibleCustomerColumns,
  draggedColumnKey: draggedCustomerColumnKey,
  dropTargetKey: dropCustomerTargetKey,
  handleColumnDragStart: handleCustomerDragStart,
  handleColumnDragEnter: handleCustomerDragEnter,
  handleColumnDragOver: handleCustomerDragOver,
  handleColumnDrop: handleCustomerDrop,
  handleColumnDragEnd: handleCustomerDragEnd,
} = useColumnOrder('admin-customers', CUSTOMER_COLUMNS)

const {
  visibleColumns: visibleUserColumns,
  draggedColumnKey: draggedUserColumnKey,
  dropTargetKey: dropUserTargetKey,
  handleColumnDragStart: handleUserDragStart,
  handleColumnDragEnter: handleUserDragEnter,
  handleColumnDragOver: handleUserDragOver,
  handleColumnDrop: handleUserDrop,
  handleColumnDragEnd: handleUserDragEnd,
} = useColumnOrder('admin-customer-users', USER_COLUMNS)

const selectedCustomer = computed(() =>
  customers.value.find((customer) => customer.id === selectedCustomerId.value) || null
)

const filteredCustomers = computed(() => {
  const term = customerSearch.value.trim().toLowerCase()
  if (!term) {
    return customers.value
  }

  return customers.value.filter((customer) =>
    [customer.musteriKodu, customer.unvan, customer.ePosta]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(term))
  )
})

const filteredCustomerUsers = computed(() => {
  const term = userSearch.value.trim().toLowerCase()
  if (!term) {
    return customerUsers.value
  }

  return customerUsers.value.filter((user) =>
    [user.ad, user.soyad, user.ePosta, user.telefon]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(term))
  )
})

const { selectItem: selectCustomerRow } = useGridKeyboard({
  items: filteredCustomers,
  selectedKey: selectedCustomerId,
  setSelectedKey: (value) => {
    selectedCustomerId.value = value
  },
  enabled: computed(() => !customerModalOpen.value && !userModalOpen.value && activeGrid.value === 'customers'),
  onSelect: (customer) => loadCustomerContext(customer.id),
  onCreate: () => openCustomerModal(),
  onEnter: (customer) => openCustomerModal(customer),
  onDelete: (customer) => removeCustomer(customer.id),
})

const { selectItem: selectCustomerUser } = useGridKeyboard({
  items: filteredCustomerUsers,
  selectedKey: selectedCustomerUserId,
  setSelectedKey: (value) => {
    selectedCustomerUserId.value = value
  },
  enabled: computed(() => !customerModalOpen.value && !userModalOpen.value && activeGrid.value === 'users'),
  onCreate: () => openUserModal(),
  onDelete: (user) => removeCustomerUser(user.id),
})

useSaveShortcut({
  enabled: computed(() =>
    (customerModalOpen.value && !customerSaving.value)
    || (userModalOpen.value && !userSaving.value)
  ),
  onSave: async () => {
    if (customerModalOpen.value) {
      await saveCustomer()
      return
    }

    if (userModalOpen.value) {
      await saveCustomerUser()
    }
  },
})

function formatDate(value) {
  if (!value) {
    return '-'
  }

  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(value))
}

function formatStatus(value) {
  return value ? 'Aktif' : 'Pasif'
}

function formatPackage(value) {
  switch (Number(value)) {
    case 1:
      return 'Premium'
    case 2:
      return 'Kurumsal'
    default:
      return 'Standart'
  }
}

function formatCustomerCell(customer, key) {
  switch (key) {
    case 'musteriKodu':
      return customer.musteriKodu || '-'
    case 'unvan':
      return customer.unvan || '-'
    case 'ePosta':
      return customer.ePosta || '-'
    case 'paketTipi':
      return formatPackage(customer.paketTipi)
    case 'aktifMi':
      return formatStatus(customer.aktifMi)
    case 'kayitTarihi':
      return formatDate(customer.kayitTarihi)
    default:
      return '-'
  }
}

function formatUserCell(user, key) {
  switch (key) {
    case 'adSoyad':
      return `${user.ad || ''} ${user.soyad || ''}`.trim() || '-'
    case 'ePosta':
      return user.ePosta || '-'
    case 'telefon':
      return user.telefon || '-'
    case 'rol':
      return user.rol === 'Admin' ? 'Yönetici' : 'Kullanıcı'
    case 'aktifMi':
      return formatStatus(user.aktifMi)
    default:
      return '-'
  }
}

async function loadCustomers() {
  loading.value = true
  pageError.value = ''

  try {
    const response = await apiClient.getCustomers()
    customers.value = Array.isArray(response) ? response : []

    const resolvedCustomerId = customers.value.some((customer) => customer.id === selectedCustomerId.value)
      ? selectedCustomerId.value
      : (customers.value[0]?.id ?? '')

    if (resolvedCustomerId) {
      selectedCustomerId.value = resolvedCustomerId
      await loadCustomerContext(resolvedCustomerId)
    } else {
      selectedCustomerId.value = ''
      customerUsers.value = []
      customerUnits.value = []
      customerCariTypes.value = []
      referenceError.value = ''
    }
  } catch (error) {
    customerUsers.value = []
    customerUnits.value = []
    customerCariTypes.value = []
    referenceError.value = ''
    pageError.value = error.message || 'Şirket kayıtları yüklenemedi.'
  } finally {
    loading.value = false
  }
}

async function loadCustomerContext(customerId = selectedCustomerId.value) {
  citySyncMessage.value = ''
  await Promise.all([
    loadCustomerUsers(customerId),
    loadCustomerReferenceData(customerId),
  ])
}

async function loadCustomerUsers(customerId = selectedCustomerId.value) {
  if (!customerId) {
    customerUsers.value = []
    selectedCustomerUserId.value = ''
    return
  }

  usersLoading.value = true

  try {
    const response = await apiClient.getCustomerUsers(customerId)
    customerUsers.value = Array.isArray(response) ? response : []
  } catch (error) {
    customerUsers.value = []
    pageError.value = error.message || 'Şirket kullanıcıları yüklenemedi.'
  } finally {
    usersLoading.value = false
  }
}

async function loadCustomerReferenceData(customerId = selectedCustomerId.value) {
  if (!customerId) {
    customerUnits.value = []
    customerCariTypes.value = []
    referenceError.value = ''
    return
  }

  referenceLoading.value = true
  referenceError.value = ''

  try {
    const [unitsResponse, typesResponse] = await Promise.all([
      apiClient.getUnits(customerId),
      apiClient.getCariTypes(customerId),
    ])

    customerUnits.value = Array.isArray(unitsResponse) ? unitsResponse : []
    customerCariTypes.value = Array.isArray(typesResponse) ? typesResponse : []
  } catch (error) {
    customerUnits.value = []
    customerCariTypes.value = []
    referenceError.value = error.message || 'Varsayılan tanımlar yüklenemedi.'
  } finally {
    referenceLoading.value = false
  }
}

async function syncCustomerDefaults() {
  if (!selectedCustomerId.value) {
    referenceError.value = 'Önce bir şirket seçin.'
    citySyncMessage.value = ''
    return
  }

  referenceLoading.value = true
  referenceError.value = ''

  try {
    await apiClient.ensureCustomerDefaults(selectedCustomerId.value)
    await loadCustomerReferenceData(selectedCustomerId.value)
  } catch (error) {
    referenceError.value = error.message || 'Varsayılan tanımlar güncellenemedi.'
  } finally {
    referenceLoading.value = false
  }
}

async function syncHksCitiesForAllCustomers() {
  if (!selectedCustomerId.value) {
    referenceError.value = 'Önce bir şirket seçin.'
    citySyncMessage.value = ''
    return
  }

  citySyncLoading.value = true
  citySyncMessage.value = ''
  referenceError.value = ''

  try {
    const response = await apiClient.syncHksIllerForAllCustomers(selectedCustomerId.value)
    const cityCount = Number(response?.ilSayisi ?? 0)
    const tenantCount = Number(response?.sirketSayisi ?? 0)
    const sourceName = selectedCustomer.value?.unvan || 'Seçili şirket'

    citySyncMessage.value = `${sourceName} HKS hesabıyla ${cityCount} il çekildi ve ${tenantCount} aktif şirket için ortak liste güncellendi.`
  } catch (error) {
    referenceError.value = error.message || 'HKS il listesi tüm şirketler için güncellenemedi.'
  } finally {
    citySyncLoading.value = false
  }
}

function resetCustomerForm() {
  Object.assign(customerForm, {
    musteriKodu: '',
    unvan: '',
    vergiNo: '',
    vergiDairesi: '',
    kimlikTuru: 0,
    gsmNo: '',
    ePosta: '',
    il: '',
    ilce: '',
    adresDetay: '',
    paketTipi: 0,
    aktifMi: true,
  })
  editingCustomerId.value = ''
  customerFormError.value = ''
}

function openCustomerModal(customer = null) {
  resetCustomerForm()

  if (customer) {
    editingCustomerId.value = customer.id
    Object.assign(customerForm, {
      musteriKodu: customer.musteriKodu || '',
      unvan: customer.unvan || '',
      vergiNo: customer.vergiNo || '',
      vergiDairesi: customer.vergiDairesi || '',
      kimlikTuru: Number(customer.kimlikTuru ?? 0),
      gsmNo: customer.gsmNo || '',
      ePosta: customer.ePosta || '',
      il: customer.il || '',
      ilce: customer.ilce || '',
      adresDetay: customer.adresDetay || '',
      paketTipi: Number(customer.paketTipi ?? 0),
      aktifMi: Boolean(customer.aktifMi),
    })
  }

  customerModalOpen.value = true
}

function closeCustomerModal() {
  customerModalOpen.value = false
}

async function saveCustomer() {
  customerFormError.value = ''
  customerSaving.value = true

  try {
    const payload = { ...customerForm }

    if (editingCustomerId.value) {
      await apiClient.updateCustomer(editingCustomerId.value, payload)
    } else {
      await apiClient.createCustomer(payload)
    }

    closeCustomerModal()
    await loadCustomers()
  } catch (error) {
    customerFormError.value = error.message || 'Şirket kaydedilemedi.'
  } finally {
    customerSaving.value = false
  }
}

async function removeCustomer(customerId) {
  if (!window.confirm('Bu şirketi pasife almak istiyor musunuz?')) {
    return
  }

  try {
    await apiClient.deactivateCustomer(customerId)
    await loadCustomers()
  } catch (error) {
    pageError.value = error.message || 'Şirket pasife alınamadı.'
  }
}

function resetUserForm() {
  Object.assign(userForm, {
    ad: '',
    soyad: '',
    ePosta: '',
    telefon: '',
    sifre: '',
  })
  userFormError.value = ''
}

function openUserModal() {
  if (!selectedCustomerId.value) {
    pageError.value = 'Önce bir şirket seçin.'
    return
  }

  resetUserForm()
  userModalOpen.value = true
}

function closeUserModal() {
  userModalOpen.value = false
}

async function saveCustomerUser() {
  if (!selectedCustomerId.value) {
    userFormError.value = 'Önce bir şirket seçin.'
    return
  }

  userFormError.value = ''
  userSaving.value = true

  try {
    await apiClient.createCustomerUser(selectedCustomerId.value, { ...userForm })
    closeUserModal()
    await loadCustomerUsers()
  } catch (error) {
    userFormError.value = error.message || 'Şirket kullanıcısı eklenemedi.'
  } finally {
    userSaving.value = false
  }
}

async function removeCustomerUser(userId) {
  if (!window.confirm('Bu kullanıcıyı pasife almak istiyor musunuz?')) {
    return
  }

  try {
    await apiClient.deactivateUser(userId)
    await loadCustomerUsers()
  } catch (error) {
    pageError.value = error.message || 'Kullanıcı pasife alınamadı.'
  }
}

onMounted(loadCustomers)
</script>
