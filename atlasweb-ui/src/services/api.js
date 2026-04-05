const JWT_CLAIMS = {
  role: 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  email: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  name: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
  userId: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
}

function resolveApiBaseUrl() {
  const explicitBaseUrl = window.ATLAS_API_BASE_URL?.trim()
  if (explicitBaseUrl) {
    return explicitBaseUrl.replace(/\/+$/, '')
  }

  if (window.location.protocol === 'http:' || window.location.protocol === 'https:') {
    return window.location.origin.replace(/\/+$/, '')
  }

  return ''
}

async function readResponseBody(response) {
  if (response.status === 204) {
    return null
  }

  const contentType = response.headers.get('content-type') || ''
  if (contentType.includes('application/json')) {
    return response.json().catch(() => null)
  }

  const text = await response.text()
  return text.length > 0 ? text : null
}

function buildUrl(endpoint) {
  if (/^https?:\/\//i.test(endpoint)) {
    return endpoint
  }

  return `${API_CONFIG.BASE_URL}${endpoint}`
}

function formatValidationErrors(errors) {
  if (!errors || typeof errors !== 'object') {
    return ''
  }

  const messages = Object.entries(errors)
    .flatMap(([field, value]) => {
      const items = Array.isArray(value) ? value : [value]
      return items
        .filter(Boolean)
        .map((message) => {
          if (!field || field === '$' || field === 'dto') {
            return String(message)
          }

          return `${field}: ${message}`
        })
    })
    .filter(Boolean)

  return messages.join(' | ')
}

let refreshPromise = null
const SESSION_ACCESS_TOKEN_KEY = 'atlas_access_token'
let inMemoryAccessToken = readSessionAccessToken()

purgeLegacySessionStorage()

function notifySessionChanged() {
  window.dispatchEvent(new CustomEvent('atlas:session-changed'))
}

function purgeLegacySessionStorage() {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.removeItem('atlas_token')
    window.localStorage.removeItem('atlas_refresh')
  } catch {
    // Ignore storage access errors in restrictive browsers.
  }
}

function readSessionAccessToken() {
  if (typeof window === 'undefined') {
    return null
  }

  try {
    return window.sessionStorage.getItem(SESSION_ACCESS_TOKEN_KEY)
  } catch {
    return null
  }
}

function writeSessionAccessToken(token) {
  if (typeof window === 'undefined') {
    return
  }

  try {
    if (token) {
      window.sessionStorage.setItem(SESSION_ACCESS_TOKEN_KEY, token)
      return
    }

    window.sessionStorage.removeItem(SESSION_ACCESS_TOKEN_KEY)
  } catch {
    // Ignore storage access errors in restrictive browsers.
  }
}

export const API_CONFIG = {
  BASE_URL: resolveApiBaseUrl(),
  ENDPOINTS: {
    LOGIN: '/api/auth/login',
    FORGOT_PASSWORD: '/api/auth/forgot-password',
    RESET_PASSWORD: '/api/auth/reset-password',
    REFRESH: '/api/auth/refresh-token',
    LOGOUT: '/api/auth/logout',
    STOCKS: '/api/Stok',
    CUSTOMERS: '/api/Musteri',
    UNITS: '/api/Birim',
    CARI_TYPES: '/api/CariTip',
    CARIS: '/api/CariKart',
    INVOICES: '/api/Fatura',
    KASA_FIS: '/api/KasaFis',
    INVOICE_SUMMARY: '/api/Fatura/ozet',
    ADMIN_ACTIVITIES: '/api/Admin/aktiviteler',
    ADMIN_ERRORS: '/api/Admin/sistem-hatalari',
    ADMIN_FILE_LOGS: '/api/Admin/dosya-loglari',
    ADMIN_SYSTEM_ADMINS: '/api/Admin/yoneticiler',
    ADMIN_HKS_ILLER_TUM_SIRKETLER: '/api/Admin/hks/iller/tum-sirketler',
    HKS_SETTINGS: '/api/Hks/ayarlar',
    HKS_SIFATLAR: '/api/Hks/sifatlar',
    HKS_SIFATLAR_KAYITLI: '/api/Hks/sifatlar/kayitli',
    HKS_KAYITLI_KISI_SORGU: '/api/Hks/kayitli-kisi-sorgu',
    HKS_ILLER: '/api/Hks/iller',
    HKS_ILLER_KAYITLI: '/api/Hks/iller/kayitli',
    HKS_ILCELER: '/api/Hks/ilceler',
    HKS_ILCELER_KAYITLI: '/api/Hks/ilceler/kayitli',
    HKS_BELDELER: '/api/Hks/beldeler',
    HKS_BELDELER_KAYITLI: '/api/Hks/beldeler/kayitli',
    HKS_URUNLER: '/api/Hks/urunler',
    HKS_URUNLER_KAYITLI: '/api/Hks/urunler/kayitli',
    HKS_URUN_BIRIMLERI: '/api/Hks/urun-birimleri',
    HKS_URUN_BIRIMLERI_KAYITLI: '/api/Hks/urun-birimleri/kayitli',
    HKS_ISLETME_TURLERI: '/api/Hks/isletme-turleri',
    HKS_ISLETME_TURLERI_KAYITLI: '/api/Hks/isletme-turleri/kayitli',
    HKS_URETIM_SEKILLERI: '/api/Hks/uretim-sekilleri',
    HKS_URETIM_SEKILLERI_KAYITLI: '/api/Hks/uretim-sekilleri/kayitli',
    HKS_BILDIRIM_TURLERI: '/api/Hks/bildirim-turleri',
    HKS_BELGE_TIPLERI: '/api/Hks/belge-tipleri',
    HKS_URUN_CINSLERI: '/api/Hks/urun-cinsleri',
    HKS_URUN_CINSLERI_KAYITLI: '/api/Hks/urun-cinsleri/kayitli',
    HKS_REFERANS_KUNYELER: '/api/Hks/referans-kunyeler',
    HKS_REFERANS_KUNYELER_ANLIK: '/api/Hks/referans-kunyeler/anlik',
    HKS_REFERANS_KUNYELER_KAYITLI: '/api/Hks/referans-kunyeler/kayitli',
  },
}

export const apiClient = {
  getAccessToken() {
    return inMemoryAccessToken || readSessionAccessToken()
  },

  getTokenPayload(token = null) {
    return this.parseJwt(token ?? this.getAccessToken())
  },

  isAccessTokenUsable(minValiditySeconds = 30) {
    const payload = this.getTokenPayload()
    if (!payload?.exp) {
      return false
    }

    const expirySeconds = Number(payload.exp)
    if (!Number.isFinite(expirySeconds)) {
      return false
    }

    const nowSeconds = Math.floor(Date.now() / 1000)
    return expirySeconds - nowSeconds > minValiditySeconds
  },

  setAccessToken(token) {
    if (token) {
      inMemoryAccessToken = token
      writeSessionAccessToken(token)
      notifySessionChanged()
      return
    }

    inMemoryAccessToken = null
    writeSessionAccessToken(null)
    notifySessionChanged()
  },

  clearSession() {
    inMemoryAccessToken = null
    writeSessionAccessToken(null)
    purgeLegacySessionStorage()
    notifySessionChanged()
  },

  parseJwt(token) {
    if (!token) {
      return null
    }

    try {
      const payload = token.split('.')[1]
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
      const decoded = decodeURIComponent(
        atob(normalized)
          .split('')
          .map((char) => `%${`00${char.charCodeAt(0).toString(16)}`.slice(-2)}`)
          .join('')
      )

      return JSON.parse(decoded)
    } catch {
      return null
    }
  },

  getCurrentUser() {
    const token = this.getAccessToken()
    const payload = this.parseJwt(token)
    if (!payload) {
      return null
    }

    return {
      role: payload[JWT_CLAIMS.role] || 'User',
      email: payload[JWT_CLAIMS.email] || '',
      displayName: payload[JWT_CLAIMS.name] || payload[JWT_CLAIMS.email] || 'Atlas',
      userId: payload[JWT_CLAIMS.userId] || '',
      raw: payload,
    }
  },

  isAuthEndpoint(endpoint) {
    const normalized = endpoint.toLowerCase()
    return normalized.includes('/api/auth/login')
      || normalized.includes('/api/auth/refresh-token')
      || normalized.includes('/api/auth/logout')
  },

  async request(endpoint, options = {}, retryOnUnauthorized = true) {
    if (!API_CONFIG.BASE_URL) {
      throw new Error('API base URL tanimli degil. HTTP/HTTPS ortaminda acin veya ATLAS_API_BASE_URL tanimlayin.')
    }

    if (retryOnUnauthorized && !this.isAuthEndpoint(endpoint) && !this.isAccessTokenUsable()) {
      try {
        await this.refreshAccessToken()
      } catch {
        this.clearSession()
      }
    }

    const headers = new Headers(options.headers || {})
    const hasBody = options.body !== undefined && options.body !== null

    if (hasBody && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    const accessToken = this.getAccessToken()
    if (accessToken && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${accessToken}`)
    }

    const response = await fetch(buildUrl(endpoint), {
      ...options,
      headers,
      credentials: 'include',
    })

    if (response.status === 401 && retryOnUnauthorized && !this.isAuthEndpoint(endpoint)) {
      try {
        await this.refreshAccessToken()
        return this.request(endpoint, options, false)
      } catch (refreshError) {
        this.clearSession()
        throw refreshError
      }
    }

    const payload = await readResponseBody(response)
    if (!response.ok) {
      const message = typeof payload === 'string'
        ? payload
        : payload?.hata || payload?.mesaj || formatValidationErrors(payload?.errors) || payload?.title || 'Bir hata oluştu.'

      const error = new Error(message)
      if (payload && typeof payload === 'object') {
        Object.assign(error, payload)
      } else {
        error.payload = payload
      }

      error.status = response.status
      throw error
    }

    return payload
  },

  async refreshAccessToken() {
    if (refreshPromise) {
      return refreshPromise
    }

    refreshPromise = (async () => {
      const response = await fetch(buildUrl(API_CONFIG.ENDPOINTS.REFRESH), {
        method: 'POST',
        credentials: 'include',
      })

      const payload = await readResponseBody(response)
      if (!response.ok || !payload?.accessToken) {
        throw new Error(
          typeof payload === 'string'
            ? payload
            : payload?.hata || 'Oturum yenilenemedi.'
        )
      }

      this.setAccessToken(payload.accessToken)
      return payload.accessToken
    })().finally(() => {
      refreshPromise = null
    })

    return refreshPromise
  },

  async login(email, password) {
    const data = await this.request(
      API_CONFIG.ENDPOINTS.LOGIN,
      {
        method: 'POST',
        body: JSON.stringify({ ePosta: email, sifre: password }),
      },
      false
    )

    if (data?.accessToken) {
      this.setAccessToken(data.accessToken)
    }

    return data
  },

  async forgotPassword(email) {
    return this.request(
      API_CONFIG.ENDPOINTS.FORGOT_PASSWORD,
      {
        method: 'POST',
        body: JSON.stringify({ ePosta: email }),
      },
      false
    )
  },

  async resetPassword(token, newPassword, confirmPassword) {
    return this.request(
      API_CONFIG.ENDPOINTS.RESET_PASSWORD,
      {
        method: 'POST',
        body: JSON.stringify({
          token,
          yeniSifre: newPassword,
          yeniSifreTekrar: confirmPassword,
        }),
      },
      false
    )
  },

  async logout() {
    try {
      await this.request(API_CONFIG.ENDPOINTS.LOGOUT, { method: 'POST' }, false)
    } catch {
      // Intentionally ignored.
    }

    this.clearSession()
  },

  async getStocks(page = 1, pageSize = 200, search = '') {
    const query = new URLSearchParams({ sayfa: page, sayfaBoyutu: pageSize })
    if (search.trim()) {
      query.set('arama', search.trim())
    }
    return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}?${query}`)
  },

  async getStock(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}/${id}`)
  },

  async createStock(payload) {
    return this.request(API_CONFIG.ENDPOINTS.STOCKS, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateStock(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async deleteStock(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}/${id}`, {
      method: 'DELETE',
    })
  },

  async getUnits(musteriId = null) {
    if (!musteriId) {
      return this.request(API_CONFIG.ENDPOINTS.UNITS)
    }

    const query = new URLSearchParams({ musteriId })
    return this.request(`${API_CONFIG.ENDPOINTS.UNITS}?${query}`)
  },

  async createUnit(payload) {
    return this.request(API_CONFIG.ENDPOINTS.UNITS, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateUnit(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.UNITS}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async deleteUnit(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.UNITS}/${id}`, {
      method: 'DELETE',
    })
  },

  async getCariTypes(musteriId = null) {
    if (!musteriId) {
      return this.request(API_CONFIG.ENDPOINTS.CARI_TYPES)
    }

    const query = new URLSearchParams({ musteriId })
    return this.request(`${API_CONFIG.ENDPOINTS.CARI_TYPES}?${query}`)
  },

  async createCariType(payload) {
    return this.request(API_CONFIG.ENDPOINTS.CARI_TYPES, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateCariType(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.CARI_TYPES}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async deleteCariType(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.CARI_TYPES}/${id}`, {
      method: 'DELETE',
    })
  },

  async getCaris({ page = 1, pageSize = 200, search = '' } = {}) {
    const query = new URLSearchParams({
      sayfa: page,
      sayfaBoyutu: pageSize,
    })

    if (search.trim()) {
      query.set('arama', search.trim())
    }

    return this.request(`${API_CONFIG.ENDPOINTS.CARIS}?${query}`)
  },

  async getCariCard(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.CARIS}/${id}`)
  },

  async getCariEkstre(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.CARIS}/${id}/ekstre`)
  },

  async createCariCard(payload) {
    return this.request(API_CONFIG.ENDPOINTS.CARIS, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateCariCard(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.CARIS}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async deleteCariCard(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.CARIS}/${id}`, {
      method: 'DELETE',
    })
  },

  async getInvoices({ page = 1, pageSize = 200, search = '' } = {}) {
    const query = new URLSearchParams({
      sayfa: page,
      sayfaBoyutu: pageSize,
    })

    if (search.trim()) {
      query.set('arama', search.trim())
    }

    return this.request(`${API_CONFIG.ENDPOINTS.INVOICES}?${query}`)
  },

  async getInvoiceSummary() {
    return this.request(API_CONFIG.ENDPOINTS.INVOICE_SUMMARY)
  },

  async getInvoice(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.INVOICES}/${id}`)
  },

  async createInvoice(payload) {
    return this.request(API_CONFIG.ENDPOINTS.INVOICES, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateInvoice(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.INVOICES}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async generateInvoiceSalesKunye(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.INVOICES}/${id}/satis-kunye`, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async deleteInvoice(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.INVOICES}/${id}`, {
      method: 'DELETE',
    })
  },

  async getKasaFisleri({ page = 1, pageSize = 200, search = '' } = {}) {
    const query = new URLSearchParams({
      sayfa: page,
      sayfaBoyutu: pageSize,
    })

    if (search.trim()) {
      query.set('arama', search.trim())
    }

    return this.request(`${API_CONFIG.ENDPOINTS.KASA_FIS}?${query}`)
  },

  async getKasaFis(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.KASA_FIS}/${id}`)
  },

  async createKasaFis(payload) {
    return this.request(API_CONFIG.ENDPOINTS.KASA_FIS, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateKasaFis(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.KASA_FIS}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async deleteKasaFis(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.KASA_FIS}/${id}`, {
      method: 'DELETE',
    })
  },

  async getCustomers() {
    return this.request(API_CONFIG.ENDPOINTS.CUSTOMERS)
  },

  async createCustomer(payload) {
    return this.request(API_CONFIG.ENDPOINTS.CUSTOMERS, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateCustomer(id, payload) {
    return this.request(`${API_CONFIG.ENDPOINTS.CUSTOMERS}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async deactivateCustomer(id) {
    return this.request(`${API_CONFIG.ENDPOINTS.CUSTOMERS}/${id}`, {
      method: 'DELETE',
    })
  },

  async ensureCustomerDefaults(customerId) {
    return this.request(`${API_CONFIG.ENDPOINTS.CUSTOMERS}/${customerId}/varsayilan-tanimlar`, {
      method: 'POST',
    })
  },

  async getSystemAdmins() {
    return this.request(API_CONFIG.ENDPOINTS.ADMIN_SYSTEM_ADMINS)
  },

  async getAdminActivities() {
    return this.request(API_CONFIG.ENDPOINTS.ADMIN_ACTIVITIES)
  },

  async getAdminErrors() {
    return this.request(API_CONFIG.ENDPOINTS.ADMIN_ERRORS)
  },

  async getFileLogs() {
    return this.request(API_CONFIG.ENDPOINTS.ADMIN_FILE_LOGS)
  },

  async getHksSifatlar() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_SIFATLAR)
  },

  async getSavedHksSifatlar() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_SIFATLAR_KAYITLI)
  },

  async getHksKayitliKisiSorgu(tcKimlikVergiNo) {
    const query = new URLSearchParams()
    if (tcKimlikVergiNo?.trim()) {
      query.set('tcKimlikVergiNo', tcKimlikVergiNo.trim())
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_KAYITLI_KISI_SORGU}${suffix}`)
  },

  async getHksIller() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_ILLER)
  },

  async getSavedHksIller() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_ILLER_KAYITLI)
  },

  async getHksIlceler(ilId) {
    const query = new URLSearchParams()
    if (ilId) {
      query.set('ilId', ilId)
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_ILCELER}${suffix}`)
  },

  async getSavedHksIlceler(ilId) {
    const query = new URLSearchParams()
    if (ilId) {
      query.set('ilId', ilId)
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_ILCELER_KAYITLI}${suffix}`)
  },

  async getHksBeldeler(ilceId) {
    const query = new URLSearchParams()
    if (ilceId) {
      query.set('ilceId', ilceId)
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_BELDELER}${suffix}`)
  },

  async getSavedHksBeldeler(ilceId) {
    const query = new URLSearchParams()
    if (ilceId) {
      query.set('ilceId', ilceId)
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_BELDELER_KAYITLI}${suffix}`)
  },

  async getHksSettings() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_SETTINGS)
  },

  async saveHksSettings(payload) {
    return this.request(API_CONFIG.ENDPOINTS.HKS_SETTINGS, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async getHksUrunler() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_URUNLER)
  },

  async getSavedHksUrunler() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_URUNLER_KAYITLI)
  },

  async getHksUrunBirimleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_URUN_BIRIMLERI)
  },

  async getSavedHksUrunBirimleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_URUN_BIRIMLERI_KAYITLI)
  },

  async getHksIsletmeTurleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_ISLETME_TURLERI)
  },

  async getSavedHksIsletmeTurleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_ISLETME_TURLERI_KAYITLI)
  },

  async getHksUretimSekilleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_URETIM_SEKILLERI)
  },

  async getSavedHksUretimSekilleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_URETIM_SEKILLERI_KAYITLI)
  },

  async getHksBildirimTurleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_BILDIRIM_TURLERI)
  },

  async getHksBelgeTipleri() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_BELGE_TIPLERI)
  },

  async getHksUrunCinsleri(urunId = null) {
    const query = new URLSearchParams()
    if (urunId) {
      query.set('urunId', urunId)
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_URUN_CINSLERI}${suffix}`)
  },

  async getSavedHksUrunCinsleri(urunId = null) {
    const query = new URLSearchParams()
    if (urunId) {
      query.set('urunId', urunId)
    }

    const suffix = query.size ? `?${query}` : ''
    return this.request(`${API_CONFIG.ENDPOINTS.HKS_URUN_CINSLERI_KAYITLI}${suffix}`)
  },

  async searchHksReferansKunyeler(payload) {
    return this.request(API_CONFIG.ENDPOINTS.HKS_REFERANS_KUNYELER, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async searchHksReferansKunyelerInstant(payload) {
    return this.request(API_CONFIG.ENDPOINTS.HKS_REFERANS_KUNYELER_ANLIK, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async getSavedHksReferansKunyeler() {
    return this.request(API_CONFIG.ENDPOINTS.HKS_REFERANS_KUNYELER_KAYITLI)
  },

  async saveHksReferansKunyelerSnapshot(payload) {
    return this.request(API_CONFIG.ENDPOINTS.HKS_REFERANS_KUNYELER_KAYITLI, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async getCustomerUsers(customerId) {
    return this.request(`/api/Admin/musteri/${customerId}/kullanicilar`)
  },

  async createCustomerUser(customerId, payload) {
    return this.request(`/api/Admin/musteri/${customerId}/kullanici`, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async deactivateUser(id) {
    return this.request(`/api/Admin/kullanici/${id}`, {
      method: 'DELETE',
    })
  },

  async createSystemAdmin(payload) {
    return this.request('/api/Admin/yonetici', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  async updateSystemAdmin(id, payload) {
    return this.request(`/api/Admin/yonetici/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },

  async syncHksIllerForAllCustomers(sourceCustomerId) {
    return this.request(`${API_CONFIG.ENDPOINTS.ADMIN_HKS_ILLER_TUM_SIRKETLER}/${sourceCustomerId}`, {
      method: 'POST',
    })
  },
}
