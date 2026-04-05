import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { apiClient } from '../services/api'
import { syncSessionState, useSession } from '../session'

export function useWorkspaceShell(activeSection, statusText) {
  const route = useRoute()
  const router = useRouter()
  const { displayName, companyName } = useSession()

  const companyLabel = computed(() => companyName.value)

  const navigation = computed(() => [
    {
      label: 'Çalışma Masası',
      section: 'workspace',
      route: '/dashboard',
      action: () => router.push('/dashboard'),
    },
    {
      label: 'Faturalar',
      section: 'fatura',
      route: '/faturalar',
      action: () => router.push('/faturalar'),
    },
    {
      label: 'Kasa Fisleri',
      section: 'kasa',
      route: '/kasa-fisleri',
      action: () => router.push('/kasa-fisleri'),
    },
    {
      label: 'Kartlar',
      section: 'cari',
      items: [
        { label: 'Cari Kartları', route: '/cariler', action: () => router.push('/cariler') },
        { label: 'Stok Kartları', route: '/stocks', action: () => router.push('/stocks') },
      ],
    },
    {
      label: 'HKS',
      section: 'hks',
      items: [
        { label: 'Referans Künyeler', route: '/hks', action: () => router.push('/hks') },
      ],
    },
    {
      label: 'Tanımlar',
      section: 'settings',
      items: [
        { label: 'Ayarlar', route: '/settings', action: () => router.push('/settings') },
      ],
    },
  ])

  const topMenus = computed(() => [
    { label: 'Çalışma Masası', route: '/dashboard', section: 'workspace', action: () => router.push('/dashboard') },
    { label: 'Cari', route: '/cariler', section: 'cari', action: () => router.push('/cariler') },
    { label: 'Stok', route: '/stocks', section: 'stok', action: () => router.push('/stocks') },
    { label: 'Faturalar', route: '/faturalar', section: 'fatura', action: () => router.push('/faturalar') },
    { label: 'Kasa', route: '/kasa-fisleri', section: 'kasa', action: () => router.push('/kasa-fisleri') },
    { label: 'HKS', route: '/hks', section: 'hks', action: () => router.push('/hks') },
    { label: 'Ayarlar', route: '/settings', section: 'settings', action: () => router.push('/settings') },
  ])

  async function logout() {
    await apiClient.logout()
    syncSessionState()
    await router.replace('/login')
  }

  return {
    route,
    router,
    displayName,
    companyLabel,
    navigation,
    topMenus,
    statusText,
    logout,
  }
}
