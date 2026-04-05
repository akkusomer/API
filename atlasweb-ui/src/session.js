import { computed, reactive } from 'vue'
import { apiClient } from './services/api'

export const sessionState = reactive({
  user: apiClient.getCurrentUser(),
  accessToken: apiClient.getAccessToken(),
})

let ensureSessionPromise = null

export function syncSessionState() {
  sessionState.accessToken = apiClient.getAccessToken()
  sessionState.user = apiClient.getCurrentUser()
}

export async function ensureSessionState() {
  if (apiClient.isAccessTokenUsable()) {
    syncSessionState()
    return sessionState.user
  }

  if (ensureSessionPromise) {
    return ensureSessionPromise
  }

  ensureSessionPromise = (async () => {
    try {
      await apiClient.refreshAccessToken()
    } catch {
      apiClient.clearSession()
    } finally {
      syncSessionState()
      ensureSessionPromise = null
    }

    return sessionState.user
  })()

  return ensureSessionPromise
}

if (typeof window !== 'undefined') {
  window.addEventListener('atlas:session-changed', syncSessionState)
}

export function useSession() {
  const isAdmin = computed(() => sessionState.user?.role === 'Admin')
  const displayName = computed(() => {
    const value = sessionState.user?.displayName || sessionState.user?.email || 'Atlas'
    return value.includes('@') ? value.split('@')[0].toUpperCase() : value
  })
  const companyName = computed(() => {
    const raw = sessionState.user?.raw || {}
    const value = raw.companyName
      || raw.CompanyName
      || raw.firmaUnvan
      || raw.FirmaUnvan
      || raw.musteriUnvan
      || raw.MusteriUnvan
      || raw.unvan
      || raw.Unvan
      || sessionState.user?.email?.split('@')[0]
      || 'ATLAS MERKEZ'

    return value.toString().trim().toUpperCase()
  })

  return {
    sessionState,
    isAdmin,
    displayName,
    companyName,
    syncSessionState,
  }
}
