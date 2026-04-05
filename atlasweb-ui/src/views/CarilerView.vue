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
      <WindowPanel title="Cari Kartlari">
        <template #toolbar>
          <button type="button" class="tool-button" @click="openModal()">Yeni Cari</button>
          <button type="button" class="tool-button" :disabled="!selectedCariId" @click="openCariEkstre()">Cari Ekstre (F10)</button>
          <button type="button" class="tool-button" @click="loadCariler">Yenile</button>
        </template>

        <div class="window-toolbar">
          <label class="inline-field inline-field--grow">
            <span>Ara</span>
            <input v-model.trim="search" type="text" placeholder="Unvan, ad veya VKN ara" />
          </label>
        </div>

        <div class="type-strip">
          <button
            v-for="type in cariTypes"
            :key="type.id"
            type="button"
            class="type-chip"
            :class="{ 'is-active': activeCariTypeId === type.id }"
            @click="activeCariTypeId = type.id"
          >
            <strong>{{ type.adi }}</strong>
            <span>{{ countByType(type.id) }} kayit</span>
          </button>
        </div>

        <p class="invoice-column-hint">
          Kolon basliklarini surukleyerek cari liste duzenini kisisellestirebilirsin.
        </p>
        <p class="invoice-column-hint">
          Yukari ve asagi oklarla satir secebilir, Enter ile acabilir, F2 ile yeni kayit acabilir, F3 ile silebilir, F10 ile ekstreye gecebilirsin.
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
                <th class="align-right">Islem</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="loading">
                <td :colspan="visibleColumns.length + 1" class="empty-cell">Cari kartlar yukleniyor...</td>
              </tr>
              <tr v-else-if="filteredCariler.length === 0">
                <td :colspan="visibleColumns.length + 1" class="empty-cell">Filtreye uygun cari kart bulunamadi.</td>
              </tr>
              <tr
                v-for="cari in filteredCariler"
                :key="cari.id"
                class="is-clickable"
                :class="{ 'is-selected': selectedCariId === cari.id }"
                @click="selectCari(cari)"
              >
                <td
                  v-for="column in visibleColumns"
                  :key="column.key"
                  :class="{ 'align-right': column.align === 'right' }"
                >
                  {{ formatCariCell(cari, column.key) }}
                </td>
                <td class="align-right">
                  <div class="table-actions">
                    <button type="button" class="table-action" @click.stop.prevent="openModal(cari)">Duzenle</button>
                    <button type="button" class="table-action table-action--danger" @click.stop.prevent="removeCari(cari.id)">Sil</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </WindowPanel>
    </section>

    <div v-if="modalOpen" class="modal-backdrop" @click.self="closeModal">
      <div class="modal-window modal-window--wide modal-window--cari">
        <header class="modal-window__header">
          <h2>{{ editingId ? 'Cari Kart Duzenle' : 'Yeni Cari Kart' }}</h2>
          <button type="button" class="ghost-icon" @click="closeModal">x</button>
        </header>

        <form class="desktop-form desktop-form--stack" @submit.prevent="saveCari">
          <div class="form-switcher">
            <button
              v-for="section in cariFormSections"
              :key="section.key"
              type="button"
              class="form-switcher__button"
              :class="{ 'is-active': activeCariFormSection === section.key }"
              @click="activeCariFormSection = section.key"
            >
              {{ section.label }}
            </button>
          </div>

          <section v-show="activeCariFormSection === 'kimlik'" class="form-panel form-panel--tab">
            <div class="form-panel__header">
              <p class="form-panel__eyebrow">Temel Bilgiler</p>
              <h3>Cari Kimligi</h3>
            </div>
            <div class="desktop-form__grid desktop-form__grid--two">
              <label class="field">
                <span>Cari Tipi</span>
                <select v-model="form.cariTipId" required>
                  <option value="">Cari tipi seciniz</option>
                  <option v-for="type in cariTypes" :key="type.id" :value="type.id">
                    {{ type.adi }}
                  </option>
                </select>
              </label>
              <label class="field">
                <span>Fatura Tipi</span>
                <select v-model.number="form.faturaTipi" required>
                  <option :value="1">Bireysel</option>
                  <option :value="2">Kurumsal</option>
                  <option :value="3">Ihracat</option>
                </select>
              </label>
              <label class="field">
                <span>Ticari Unvan</span>
                <input v-model.trim="form.unvan" type="text" />
              </label>
              <label class="field">
                <span>Adi Soyadi</span>
                <input v-model.trim="form.adiSoyadi" type="text" />
              </label>
              <label class="field field--full">
                <span>VKN / TCKN</span>
                <input v-model.trim="form.VTCK_No" type="text" maxlength="11" />
              </label>
              <label class="field field--full">
                <span>Vergi Dairesi</span>
                <input v-model.trim="form.vergiDairesi" type="text" />
              </label>
            </div>
          </section>

          <section v-show="activeCariFormSection === 'hks'" class="form-panel form-panel--tab">
            <div class="form-panel__header">
              <p class="form-panel__eyebrow">HKS Bilgileri</p>
              <h3>Eslesme ve Konum</h3>
            </div>
            <div class="desktop-form__grid desktop-form__grid--two">
              <label class="field field-with-action">
                <span>HKS Sifat</span>
                <div class="field-with-action__row">
                  <select v-model="form.hksSifatId" :disabled="hksSifatlar.length === 0">
                    <option value="">Sifat seciniz</option>
                    <option
                      v-for="sifat in hksSifatlar"
                      :key="getHksSifatOptionValue(sifat)"
                      :value="getHksSifatOptionValue(sifat)"
                    >
                      {{ sifat.ad }}
                    </option>
                  </select>
                  <button
                    type="button"
                    class="tool-button tool-button--neutral field-with-action__button"
                    :disabled="loadingSifatlar"
                    @click="loadHksSifatlarForCurrentIdentity"
                  >
                    {{ loadingSifatlar ? 'Getiriliyor...' : 'HKS Sifat Getir' }}
                  </button>
                </div>
              </label>
              <label class="field">
                <span>Isletme Turu</span>
                <select v-model="form.hksIsletmeTuruId" :disabled="hksIsletmeTurleri.length === 0">
                  <option value="">Isletme turu seciniz</option>
                  <option
                    v-for="isletmeTuru in hksIsletmeTurleri"
                    :key="isletmeTuru.id || isletmeTuru.hksIsletmeTuruId"
                    :value="String(isletmeTuru.hksIsletmeTuruId)"
                  >
                    {{ isletmeTuru.ad }}
                  </option>
                </select>
              </label>
              <label class="field field--full">
                <span>Hal Ici Isyeri</span>
                <select v-model="form.hksHalIciIsyeriId" :disabled="halIciIsyerleri.length === 0">
                  <option value="">Hal ici isyeri seciniz</option>
                  <option
                    v-for="isyeri in halIciIsyerleri"
                    :key="isyeri.id"
                    :value="String(isyeri.id)"
                  >
                    {{ isyeri.halAdi ? `${isyeri.ad} / ${isyeri.halAdi}` : isyeri.ad }}
                  </option>
                </select>
              </label>
              <div v-if="bulunanHksSifatlar.length" class="field field--full">
                <span>Bulunan HKS Sifatlari</span>
                <div class="hks-sifat-list">
                  <button
                    v-for="sifat in bulunanHksSifatlar"
                    :key="`sifat-${getHksSifatOptionValue(sifat)}`"
                    type="button"
                    class="hks-sifat-chip"
                    :class="{ 'is-selected': form.hksSifatId === getHksSifatOptionValue(sifat) }"
                    @click="form.hksSifatId = getHksSifatOptionValue(sifat)"
                  >
                    {{ sifat.ad }}
                  </button>
                </div>
              </div>
              <label class="field">
                <span>Dogum Tarihi</span>
                <input v-model="form.dogumTarihi" type="date" />
              </label>
              <div class="desktop-form__grid desktop-form__grid--three field--full">
                <label class="field">
                  <span>Il</span>
                  <select v-model="form.hksIlId">
                    <option value="">Il seciniz</option>
                    <option v-for="il in hksIller" :key="il.id || il.hksIlId" :value="String(il.hksIlId)">
                      {{ il.ad }}
                    </option>
                  </select>
                </label>
                <label class="field">
                  <span>Ilce</span>
                  <select v-model="form.hksIlceId" :disabled="!form.hksIlId">
                    <option value="">Ilce seciniz</option>
                    <option v-for="ilce in hksIlceler" :key="ilce.id || ilce.hksIlceId" :value="String(ilce.hksIlceId)">
                      {{ ilce.ad }}
                    </option>
                  </select>
                </label>
                <label class="field">
                  <span>Belde</span>
                  <select v-model="form.hksBeldeId" :disabled="!form.hksIlceId">
                    <option value="">Belde seciniz</option>
                    <option v-for="belde in hksBeldeler" :key="belde.id || belde.hksBeldeId" :value="String(belde.hksBeldeId)">
                      {{ belde.ad }}
                    </option>
                  </select>
                </label>
              </div>
            </div>
          </section>

          <section v-show="activeCariFormSection === 'konum'" class="form-panel form-panel--tab">
            <div class="form-panel__header">
              <p class="form-panel__eyebrow">Iletisim ve Diger</p>
              <h3>Telefon, Kod ve Adres</h3>
            </div>
            <div class="desktop-form__grid desktop-form__grid--two">
              <label class="field">
                <span>GSM</span>
                <input v-model.trim="form.gsm" type="text" />
              </label>
              <label class="field">
                <span>Telefon</span>
                <input v-model.trim="form.telefon" type="text" />
              </label>
              <label class="field">
                <span>Grup Kodu</span>
                <input v-model.trim="form.grupKodu" type="text" />
              </label>
              <label class="field">
                <span>Ozel Kod</span>
                <input v-model.trim="form.ozelKodu" type="text" />
              </label>
              <label class="field field--full">
                <span>Adres</span>
                <textarea v-model.trim="form.adres" rows="3" />
              </label>
            </div>
          </section>

          <p v-if="formSuccess" class="form-success">{{ formSuccess }}</p>
          <p v-if="formError" class="form-error">{{ formError }}</p>

          <footer class="modal-window__footer">
            <button type="button" class="secondary-button" @click="closeModal">Iptal</button>
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
  { key: 'tip', label: 'Kodu / Tip' },
  { key: 'unvan', label: 'Unvan / Kisi' },
  { key: 'faturaTipi', label: 'Fatura Tipi' },
  { key: 'iletisim', label: 'Iletisim' },
  { key: 'vtckNo', label: 'VKN/TCKN' },
]

const HKS_ISLETME_TURU_ONERILERI = {
  pazarci: ['Perakende Satis Yeri'],
  manav: ['Perakende Satis Yeri'],
  market: ['Perakende Satis Yeri'],
  'e market': ['Perakende Satis Yeri'],
  komisyoncu: ['Hal Ici Isyeri'],
  'tuccar hal ici': ['Hal Ici Isyeri'],
  'tuccar hal disi': ['Hal Disi Isyeri'],
  'depo tasnif ve ambalaj': ['Tasnifleme ve Ambalajlama', 'Hal Disi Deposu'],
  sanayici: ['Sinai Isletme'],
  imalatci: ['Sinai Isletme'],
  ihracat: ['Yurt Disi'],
  ithalat: ['Yurt Disi'],
  hastane: ['Bireysel Tuketim'],
  lokanta: ['Bireysel Tuketim'],
  otel: ['Bireysel Tuketim'],
  yurt: ['Bireysel Tuketim'],
  'yemek fabrikasi': ['Bireysel Tuketim'],
}

const cariFormSections = [
  { key: 'kimlik', label: 'Temel' },
  { key: 'hks', label: 'HKS / Vergi' },
  { key: 'konum', label: 'Iletisim / Diger' },
]

const loading = ref(true)
const saving = ref(false)
const search = ref('')
const cariler = ref([])
const cariTypes = ref([])
const hksIller = ref([])
const hksIlceler = ref([])
const hksBeldeler = ref([])
const hksSifatlar = ref([])
const bulunanHksSifatlar = ref([])
const hksIsletmeTurleri = ref([])
const halIciIsyerleri = ref([])
const activeCariTypeId = ref('')
const modalOpen = ref(false)
const editingId = ref('')
const formError = ref('')
const formSuccess = ref('')
const pageError = ref('')
const selectedCariId = ref('')
const hydratingLocation = ref(false)
const loadingSifatlar = ref(false)
const lastSuggestedIsletmeTuruId = ref('')
const activeCariFormSection = ref('kimlik')
const form = reactive({
  cariTipId: '',
  faturaTipi: 1,
  unvan: '',
  adiSoyadi: '',
  VTCK_No: '',
  hksSifatId: '',
  hksIsletmeTuruId: '',
  hksHalIciIsyeriId: '',
  halIciIsyeriAdi: '',
  dogumTarihi: '',
  vergiDairesi: '',
  gsm: '',
  telefon: '',
  telefon2: '',
  hksIlId: '',
  hksIlceId: '',
  hksBeldeId: '',
  il: '',
  ilce: '',
  belde: '',
  grupKodu: '',
  ozelKodu: '',
  adres: '',
})

const statusText = computed(() => `${filteredCariler.value.length} cari kaydi listeleniyor`)

const { route, router, displayName, companyLabel, navigation, topMenus, logout } = useWorkspaceShell('cari', statusText)

const {
  visibleColumns,
  draggedColumnKey,
  dropTargetKey,
  handleColumnDragStart,
  handleColumnDragEnter,
  handleColumnDragOver,
  handleColumnDrop,
  handleColumnDragEnd,
} = useColumnOrder('cari-list', DEFAULT_COLUMNS)

const filteredCariler = computed(() =>
  cariler.value.filter((cari) => {
    const term = search.value.trim().toLowerCase()
    const matchesSearch = !term
      || [
        cari.unvan,
        cari.adiSoyadi,
        cari.VTCK_No,
        cari.vtckNo,
        cari.telefon,
        cari.gsm,
        cari.il,
        cari.ilce,
        cari.belde,
        cari.halIciIsyeriAdi,
      ]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(term))

    const matchesType = !activeCariTypeId.value || cari.cariTipId === activeCariTypeId.value
    return matchesSearch && matchesType
  })
)

const { selectItem: selectCari } = useGridKeyboard({
  items: filteredCariler,
  selectedKey: selectedCariId,
  setSelectedKey: (value) => {
    selectedCariId.value = value
  },
  enabled: computed(() => !modalOpen.value),
  onCreate: () => openModal(),
  onEnter: (cari) => openModal(cari),
  onDelete: (cari) => removeCari(cari.id),
  onF10: (cari) => openCariEkstre(cari),
})

useSaveShortcut({
  enabled: computed(() => modalOpen.value && !saving.value),
  onSave: saveCari,
})

function resolveTypeName(typeId) {
  return cariTypes.value.find((type) => type.id === typeId)?.adi || 'Tipsiz'
}

function countByType(typeId) {
  return cariler.value.filter((item) => item.cariTipId === typeId).length
}

function formatFaturaTipi(value) {
  switch (Number(value)) {
    case 1: return 'Bireysel'
    case 2: return 'Kurumsal'
    case 3: return 'Ihracat'
    default: return '-'
  }
}

function formatCariCell(cari, key) {
  switch (key) {
    case 'tip':
      return `${resolveTypeName(cari.cariTipId)}${cari.grupKodu ? ` / ${cari.grupKodu}` : ''}`
    case 'unvan':
      return cari.unvan || cari.adiSoyadi || '-'
    case 'faturaTipi':
      return formatFaturaTipi(cari.faturaTipi)
    case 'iletisim':
      return [cari.gsm, cari.telefon].filter(Boolean).join(' / ') || '-'
    case 'vtckNo':
      return cari.VTCK_No || cari.vtckNo || '-'
    default:
      return '-'
  }
}

function normalizeLookupLabel(value) {
  return String(value || '')
    .toLocaleLowerCase('tr-TR')
    .replaceAll('ç', 'c')
    .replaceAll('ğ', 'g')
    .replaceAll('ı', 'i')
    .replaceAll('ö', 'o')
    .replaceAll('ş', 's')
    .replaceAll('ü', 'u')
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .replace(/[^a-z0-9]+/g, ' ')
    .trim()
}

function getSelectedSifat() {
  if (!form.hksSifatId) {
    return null
  }

  const currentId = Number(form.hksSifatId)
  return hksSifatlar.value.find((item) => item.id === currentId || item.hksSifatId === currentId) || null
}

function getHksSifatOptionValue(sifat) {
  if (sifat?.hksSifatId != null) {
    return String(sifat.hksSifatId)
  }

  return sifat?.id != null ? String(sifat.id) : ''
}

function mergeHksSifatlar(items = []) {
  if (!Array.isArray(items) || items.length === 0) {
    return
  }

  const byKey = new Map(
    hksSifatlar.value
      .map((item) => [getHksSifatOptionValue(item), item])
      .filter(([key]) => Boolean(key)),
  )

  items.forEach((item) => {
    const key = getHksSifatOptionValue(item)
    if (key) {
      byKey.set(key, item)
    }
  })

  hksSifatlar.value = Array.from(byKey.values())
}

function suggestIsletmeTuruFromSifat() {
  const selectedSifat = getSelectedSifat()
  if (!selectedSifat) {
    return
  }

  const mappedNames = HKS_ISLETME_TURU_ONERILERI[normalizeLookupLabel(selectedSifat.ad)] || []
  if (mappedNames.length === 0) {
    return
  }

  const matchedType = hksIsletmeTurleri.value.find((item) =>
    mappedNames.some((name) => normalizeLookupLabel(item.ad) === normalizeLookupLabel(name)),
  )

  if (!matchedType?.hksIsletmeTuruId) {
    return
  }

  const matchedId = String(matchedType.hksIsletmeTuruId)
  const canOverwrite = !form.hksIsletmeTuruId || form.hksIsletmeTuruId === lastSuggestedIsletmeTuruId.value
  if (!canOverwrite) {
    return
  }

  form.hksIsletmeTuruId = matchedId
  lastSuggestedIsletmeTuruId.value = matchedId
}

async function loadCariler() {
  loading.value = true
  pageError.value = ''

  try {
    const [cariResponse, typeResponse, savedIsletmeTurleri] = await Promise.all([
      apiClient.getCaris(),
      apiClient.getCariTypes(),
      apiClient.getSavedHksIsletmeTurleri().catch(() => []),
    ])

    cariler.value = cariResponse?.veriler || []
    cariTypes.value = typeResponse || []
    hksIsletmeTurleri.value = Array.isArray(savedIsletmeTurleri) ? savedIsletmeTurleri : []

    if (hksIsletmeTurleri.value.length === 0) {
      const syncedIsletmeTurleri = await apiClient.getHksIsletmeTurleri().catch(() => [])
      hksIsletmeTurleri.value = Array.isArray(syncedIsletmeTurleri) ? syncedIsletmeTurleri : []
    }

    const savedIller = await apiClient.getSavedHksIller().catch(() => [])
    hksIller.value = Array.isArray(savedIller) ? savedIller : []

    if (hksIller.value.length === 0) {
      const syncedIller = await apiClient.getHksIller().catch(() => [])
      hksIller.value = Array.isArray(syncedIller) ? syncedIller : []
    }

    if (!activeCariTypeId.value || !cariTypes.value.some((item) => item.id === activeCariTypeId.value)) {
      activeCariTypeId.value = cariTypes.value[0]?.id || ''
    }
  } catch (error) {
    pageError.value = error.message || 'Cari kartlar yuklenemedi.'
  } finally {
    loading.value = false
  }
}

async function loadIlcelerByIlId(ilId) {
  if (!ilId) {
    hksIlceler.value = []
    return
  }

  const savedIlceler = await apiClient.getSavedHksIlceler(ilId).catch(() => [])
  hksIlceler.value = Array.isArray(savedIlceler) ? savedIlceler : []

  if (hksIlceler.value.length === 0) {
    const syncedIlceler = await apiClient.getHksIlceler(ilId).catch(() => [])
    hksIlceler.value = Array.isArray(syncedIlceler) ? syncedIlceler : []
  }
}

async function loadBeldelerByIlceId(ilceId) {
  if (!ilceId) {
    hksBeldeler.value = []
    return
  }

  const savedBeldeler = await apiClient.getSavedHksBeldeler(ilceId).catch(() => [])
  hksBeldeler.value = Array.isArray(savedBeldeler) ? savedBeldeler : []

  if (hksBeldeler.value.length === 0) {
    const syncedBeldeler = await apiClient.getHksBeldeler(ilceId).catch(() => [])
    hksBeldeler.value = Array.isArray(syncedBeldeler) ? syncedBeldeler : []
  }
}

async function ensureHksSifatlarLoaded() {
  if (hksSifatlar.value.length > 0) {
    return
  }

  const savedSifatlar = await apiClient.getSavedHksSifatlar().catch(() => [])
  hksSifatlar.value = Array.isArray(savedSifatlar) ? savedSifatlar : []

  if (hksSifatlar.value.length === 0) {
    const syncedSifatlar = await apiClient.getHksSifatlar().catch(() => [])
    hksSifatlar.value = Array.isArray(syncedSifatlar) ? syncedSifatlar : []
  }
}

async function ensureCurrentSelectedSifatLoaded() {
  if (!form.hksSifatId) {
    return
  }

  await ensureHksSifatlarLoaded()

  const currentId = Number(form.hksSifatId)
  if (hksSifatlar.value.some((item) => item.hksSifatId === currentId || item.id === currentId)) {
    return
  }

  const savedSifatlar = await apiClient.getSavedHksSifatlar().catch(() => [])
  const matched = Array.isArray(savedSifatlar)
    ? savedSifatlar.find((item) => item.hksSifatId === currentId || item.id === currentId)
    : null

  if (matched) {
    mergeHksSifatlar([matched])
  }
}

async function ensureCurrentSelectedIsletmeTuruLoaded() {
  if (!form.hksIsletmeTuruId) {
    return
  }

  const currentId = Number(form.hksIsletmeTuruId)
  if (hksIsletmeTurleri.value.some((item) => item.hksIsletmeTuruId === currentId || item.id === currentId)) {
    return
  }

  const savedIsletmeTurleri = await apiClient.getSavedHksIsletmeTurleri().catch(() => [])
  const matched = Array.isArray(savedIsletmeTurleri)
    ? savedIsletmeTurleri.find((item) => item.hksIsletmeTuruId === currentId || item.id === currentId)
    : null

  hksIsletmeTurleri.value = matched ? [matched] : []
}

function ensureCurrentSelectedHalIciIsyeriLoaded() {
  if (!form.hksHalIciIsyeriId || !form.halIciIsyeriAdi) {
    return
  }

  const currentId = Number(form.hksHalIciIsyeriId)
  if (halIciIsyerleri.value.some((item) => item.id === currentId)) {
    return
  }

  halIciIsyerleri.value = [
    {
      id: currentId,
      ad: form.halIciIsyeriAdi,
      halAdi: '',
    },
  ]
}

async function loadHksSifatlarForCurrentIdentity() {
  formError.value = ''
  formSuccess.value = ''
  activeCariFormSection.value = 'hks'

  if (!form.VTCK_No?.trim()) {
    formError.value = 'HKS sifatlarini getirmek icin once VKN veya TCKN giriniz.'
    return
  }

  loadingSifatlar.value = true

  try {
    await ensureHksSifatlarLoaded()
    const result = await apiClient.getHksKayitliKisiSorgu(form.VTCK_No)
    const bulunanSifatlar = Array.isArray(result?.sifatlar) ? result.sifatlar : []
    const bulunanHalIciIsyerleri = Array.isArray(result?.halIciIsyerleri) ? result.halIciIsyerleri : []
    bulunanHksSifatlar.value = bulunanSifatlar
    mergeHksSifatlar(bulunanSifatlar)
    halIciIsyerleri.value = bulunanHalIciIsyerleri

    if (!result?.kayitliKisiMi) {
      form.hksHalIciIsyeriId = ''
      form.halIciIsyeriAdi = ''
      formError.value = 'Bu VKN / TCKN icin HKS kayitli kisi bulunamadi. HKS Sifat alanindan manuel secim yapabilirsiniz.'
      return
    }

    if (bulunanSifatlar.length === 1 && !form.hksSifatId) {
      form.hksSifatId = getHksSifatOptionValue(bulunanSifatlar[0])
    }

    if (form.hksHalIciIsyeriId && !bulunanHalIciIsyerleri.some((item) => item.id === Number(form.hksHalIciIsyeriId))) {
      form.hksHalIciIsyeriId = ''
      form.halIciIsyeriAdi = ''
    }

    if (bulunanHalIciIsyerleri.length === 1 && !form.hksHalIciIsyeriId) {
      form.hksHalIciIsyeriId = String(bulunanHalIciIsyerleri[0].id)
      form.halIciIsyeriAdi = bulunanHalIciIsyerleri[0].ad || ''
    }

    if (bulunanSifatlar.length === 0 && bulunanHalIciIsyerleri.length === 0) {
      formError.value = 'Kayitli kisi bulundu fakat HKS sifat veya hal ici isyeri donmedi. HKS Sifat alanindan manuel secim yapabilirsiniz.'
      return
    }

    if (bulunanSifatlar.length === 0) {
      formSuccess.value = `${bulunanHalIciIsyerleri.length} hal ici isyeri bulundu. HKS tarafinda sifat donmedi, HKS Sifat alanindan manuel secim yapabilirsiniz.`
      return
    }

    formSuccess.value = `${bulunanSifatlar.length} HKS sifati bulundu, ${bulunanHalIciIsyerleri.length} hal ici isyeri bulundu. Sadece bu kisiye ait kayitlar listelendi.`
  } catch (error) {
    formError.value = error.message || 'HKS sifat listesi alinamadi.'
  } finally {
    loadingSifatlar.value = false
  }
}

function resetForm() {
  activeCariFormSection.value = 'kimlik'
  Object.assign(form, {
    cariTipId: activeCariTypeId.value || '',
    faturaTipi: 1,
    unvan: '',
    adiSoyadi: '',
    VTCK_No: '',
    hksSifatId: '',
    hksIsletmeTuruId: '',
    hksHalIciIsyeriId: '',
    halIciIsyeriAdi: '',
    dogumTarihi: '',
    vergiDairesi: '',
    gsm: '',
    telefon: '',
    telefon2: '',
    hksIlId: '',
    hksIlceId: '',
    hksBeldeId: '',
    il: '',
    ilce: '',
    belde: '',
    grupKodu: '',
    ozelKodu: '',
    adres: '',
  })
  bulunanHksSifatlar.value = []
  hksSifatlar.value = []
  halIciIsyerleri.value = []
  hksIlceler.value = []
  hksBeldeler.value = []
  loadingSifatlar.value = false
  lastSuggestedIsletmeTuruId.value = ''
  editingId.value = ''
  formError.value = ''
  formSuccess.value = ''
}

function applyFormData(source = {}) {
  Object.assign(form, {
    cariTipId: source.cariTipId || activeCariTypeId.value || '',
    faturaTipi: source.faturaTipi || 1,
    unvan: source.unvan || '',
    adiSoyadi: source.adiSoyadi || '',
    VTCK_No: source.VTCK_No || source.vtckNo || '',
    hksSifatId: source.hksSifatId ? String(source.hksSifatId) : '',
    hksIsletmeTuruId: source.hksIsletmeTuruId ? String(source.hksIsletmeTuruId) : '',
    hksHalIciIsyeriId: source.hksHalIciIsyeriId ? String(source.hksHalIciIsyeriId) : '',
    halIciIsyeriAdi: source.halIciIsyeriAdi || '',
    dogumTarihi: normalizeDateInput(source.dogumTarihi || source.DogumTarihi),
    vergiDairesi: source.vergiDairesi || '',
    gsm: source.gsm || '',
    telefon: source.telefon || '',
    telefon2: source.telefon2 || '',
    hksIlId: source.hksIlId ? String(source.hksIlId) : '',
    hksIlceId: source.hksIlceId ? String(source.hksIlceId) : '',
    hksBeldeId: source.hksBeldeId ? String(source.hksBeldeId) : '',
    il: source.il || source.Il || '',
    ilce: source.ilce || source.Ilce || '',
    belde: source.belde || source.Belde || '',
    grupKodu: source.grupKodu || '',
    ozelKodu: source.ozelKodu || '',
    adres: source.adres || '',
  })
}

async function syncLocationSelectionsFromNames() {
  if (!form.hksIlId && form.il) {
    const selectedIl = hksIller.value.find((item) => item.ad === form.il)
    if (selectedIl?.hksIlId) {
      form.hksIlId = String(selectedIl.hksIlId)
    }
  }

  if (form.hksIlId) {
    await loadIlcelerByIlId(Number(form.hksIlId))
  } else {
    hksIlceler.value = []
    hksBeldeler.value = []
    return
  }

  if (!form.hksIlceId && form.ilce) {
    const selectedIlce = hksIlceler.value.find((item) => item.ad === form.ilce)
    if (selectedIlce?.hksIlceId) {
      form.hksIlceId = String(selectedIlce.hksIlceId)
    }
  }

  if (form.hksIlceId) {
    await loadBeldelerByIlceId(Number(form.hksIlceId))
  } else {
    hksBeldeler.value = []
    return
  }

  if (!form.hksBeldeId && form.belde) {
    const selectedBelde = hksBeldeler.value.find((item) => item.ad === form.belde)
    if (selectedBelde?.hksBeldeId) {
      form.hksBeldeId = String(selectedBelde.hksBeldeId)
    }
  }
}

async function openModal(record = null) {
  resetForm()
  modalOpen.value = true

  if (!record) {
    return
  }

  hydratingLocation.value = true
  editingId.value = record.id
  applyFormData(record)
  if (form.hksSifatId) {
    await ensureCurrentSelectedSifatLoaded()
  }
  if (form.hksIsletmeTuruId) {
    await ensureCurrentSelectedIsletmeTuruLoaded()
    lastSuggestedIsletmeTuruId.value = form.hksIsletmeTuruId
  } else if (form.hksSifatId) {
    suggestIsletmeTuruFromSifat()
  }
  ensureCurrentSelectedHalIciIsyeriLoaded()
  await syncLocationSelectionsFromNames()
  hydratingLocation.value = false
}

function closeModal() {
  modalOpen.value = false
}

function getSelectedIlAdi() {
  if (!form.hksIlId) {
    return null
  }

  return hksIller.value.find((item) => item.hksIlId === Number(form.hksIlId))?.ad || null
}

function getSelectedIlceAdi() {
  if (!form.hksIlceId) {
    return null
  }

  return hksIlceler.value.find((item) => item.hksIlceId === Number(form.hksIlceId))?.ad || null
}

function getSelectedBeldeAdi() {
  if (!form.hksBeldeId) {
    return null
  }

  return hksBeldeler.value.find((item) => item.hksBeldeId === Number(form.hksBeldeId))?.ad || null
}

function getSelectedHalIciIsyeriAdi() {
  if (!form.hksHalIciIsyeriId) {
    return null
  }

  return halIciIsyerleri.value.find((item) => item.id === Number(form.hksHalIciIsyeriId))?.ad || form.halIciIsyeriAdi || null
}

async function saveCari() {
  formError.value = ''
  formSuccess.value = ''
  saving.value = true

  try {
    const payload = {
      cariTipId: form.cariTipId,
      faturaTipi: form.faturaTipi,
      unvan: form.unvan,
      adiSoyadi: form.adiSoyadi,
      VTCK_No: form.VTCK_No,
      hksSifatId: form.hksSifatId ? Number(form.hksSifatId) : null,
      hksIsletmeTuruId: form.hksIsletmeTuruId ? Number(form.hksIsletmeTuruId) : null,
      hksHalIciIsyeriId: form.hksHalIciIsyeriId ? Number(form.hksHalIciIsyeriId) : null,
      halIciIsyeriAdi: getSelectedHalIciIsyeriAdi(),
      dogumTarihi: form.dogumTarihi || null,
      vergiDairesi: form.vergiDairesi,
      gsm: form.gsm,
      telefon: form.telefon,
      telefon2: form.telefon2,
      hksIlId: form.hksIlId ? Number(form.hksIlId) : null,
      hksIlceId: form.hksIlceId ? Number(form.hksIlceId) : null,
      hksBeldeId: form.hksBeldeId ? Number(form.hksBeldeId) : null,
      il: getSelectedIlAdi(),
      ilce: getSelectedIlceAdi(),
      belde: getSelectedBeldeAdi(),
      grupKodu: form.grupKodu,
      ozelKodu: form.ozelKodu,
      adres: form.adres,
    }

    if (editingId.value) {
      await apiClient.updateCariCard(editingId.value, payload)
    } else {
      await apiClient.createCariCard(payload)
    }

    closeModal()
    await loadCariler()
  } catch (error) {
    formError.value = error.message || 'Cari kart kaydedilemedi.'
  } finally {
    saving.value = false
  }
}

async function removeCari(id) {
  if (!window.confirm('Bu cari karti silmek istiyor musunuz?')) return

  try {
    pageError.value = ''
    await apiClient.deleteCariCard(id)
    cariler.value = cariler.value.filter((item) => item.id !== id)
    if (selectedCariId.value === id) {
      selectedCariId.value = ''
    }
    await loadCariler()
  } catch (error) {
    await loadCariler()
    pageError.value = error.message || 'Cari kart silinemedi.'
  }
}

function openCariEkstre(record = null) {
  const targetId = record?.id || selectedCariId.value
  if (!targetId) {
    return
  }

  router.push(`/cariler/${targetId}/ekstre`)
}

onMounted(loadCariler)

function normalizeDateInput(value) {
  if (!value) {
    return ''
  }

  const normalized = String(value).trim()
  if (!normalized) {
    return ''
  }

  return normalized.includes('T')
    ? normalized.split('T')[0]
    : normalized.slice(0, 10)
}

watch(
  () => form.hksIlId,
  async (newValue, oldValue) => {
    if (!newValue) {
      hksIlceler.value = []
      hksBeldeler.value = []
      form.hksIlceId = ''
      form.hksBeldeId = ''
      return
    }

    await loadIlcelerByIlId(Number(newValue))

    if (!hydratingLocation.value && newValue !== oldValue && !hksIlceler.value.some((item) => item.hksIlceId === Number(form.hksIlceId))) {
      form.hksIlceId = ''
    }

    if (!form.hksIlceId) {
      hksBeldeler.value = []
      form.hksBeldeId = ''
    }
  }
)

watch(
  () => form.hksIlceId,
  async (newValue, oldValue) => {
    if (!newValue) {
      hksBeldeler.value = []
      form.hksBeldeId = ''
      return
    }

    await loadBeldelerByIlceId(Number(newValue))

    if (!hydratingLocation.value && newValue !== oldValue && !hksBeldeler.value.some((item) => item.hksBeldeId === Number(form.hksBeldeId))) {
      form.hksBeldeId = ''
    }
  }
)

watch(
  () => form.hksSifatId,
  (newValue, oldValue) => {
    if (!newValue) {
      if (form.hksIsletmeTuruId === lastSuggestedIsletmeTuruId.value) {
        form.hksIsletmeTuruId = ''
      }
      lastSuggestedIsletmeTuruId.value = ''
      return
    }

    if (newValue === oldValue) {
      return
    }

    suggestIsletmeTuruFromSifat()
  },
)

watch(
  () => form.hksHalIciIsyeriId,
  (newValue) => {
    if (!newValue) {
      form.halIciIsyeriAdi = ''
      return
    }

    form.halIciIsyeriAdi = halIciIsyerleri.value.find((item) => item.id === Number(newValue))?.ad || form.halIciIsyeriAdi
  },
)
</script>
