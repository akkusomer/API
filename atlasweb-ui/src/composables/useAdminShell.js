import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { apiClient } from '../services/api'
import { syncSessionState, useSession } from '../session'

export function useAdminShell(statusText) {
  const route = useRoute()
  const router = useRouter()
  const { displayName } = useSession()

  const navigation = computed(() => [
    {
      label: 'Sistem Yönetimi',
      section: 'admin',
      items: [
        { label: 'Özet', route: '/admin', action: () => router.push('/admin') },
        { label: 'Şirketler', route: '/admin/sirketler', action: () => router.push('/admin/sirketler') },
        { label: 'Yöneticiler', route: '/admin/yoneticiler', action: () => router.push('/admin/yoneticiler') },
        { label: 'Loglar', route: '/admin/loglar', action: () => router.push('/admin/loglar') },
      ],
    },
  ])

  const topMenus = computed(() => [
    { label: 'Özet', route: '/admin', section: 'admin', action: () => router.push('/admin') },
    {
      label: 'Şirketler',
      route: '/admin/sirketler',
      section: 'admin',
      action: () => router.push('/admin/sirketler'),
    },
    {
      label: 'Yöneticiler',
      route: '/admin/yoneticiler',
      section: 'admin',
      action: () => router.push('/admin/yoneticiler'),
    },
    {
      label: 'Loglar',
      route: '/admin/loglar',
      section: 'admin',
      action: () => router.push('/admin/loglar'),
    },
  ])

  const companyLabel = computed(() => 'Sistem Yönetimi')

  async function logout() {
    await apiClient.logout()
    syncSessionState()
    await router.replace('/login')
  }

  return {
    route,
    router,
    displayName,
    navigation,
    topMenus,
    companyLabel,
    statusText,
    logout,
  }
}
