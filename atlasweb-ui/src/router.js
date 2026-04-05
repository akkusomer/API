import { createRouter, createWebHashHistory } from 'vue-router'
import { ensureSessionState, syncSessionState, sessionState } from './session'
import LoginView from './views/LoginView.vue'
import ResetPasswordView from './views/ResetPasswordView.vue'
import DashboardView from './views/DashboardView.vue'
import HksView from './views/HksView.vue'
import StocksView from './views/StocksView.vue'
import CarilerView from './views/CarilerView.vue'
import CariEkstreView from './views/CariEkstreView.vue'
import FaturaView from './views/FaturaView.vue'
import KasaFisView from './views/KasaFisView.vue'
import SettingsView from './views/SettingsView.vue'
import AdminView from './views/AdminView.vue'
import AdminCustomersView from './views/AdminCustomersView.vue'
import AdminManagersView from './views/AdminManagersView.vue'
import AdminLogsView from './views/AdminLogsView.vue'

const routes = [
  {
    path: '/',
    redirect: () => {
      if (!sessionState.user) {
        return '/login'
      }

      return sessionState.user.role === 'Admin' ? '/admin' : '/dashboard'
    },
  },
  {
    path: '/login',
    name: 'login',
    component: LoginView,
    meta: {
      guestOnly: true,
    },
  },
  {
    path: '/reset-password',
    name: 'reset-password',
    component: ResetPasswordView,
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: DashboardView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'workspace',
    },
  },
  {
    path: '/stocks',
    name: 'stocks',
    component: StocksView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'stok',
    },
  },
  {
    path: '/cariler',
    name: 'cariler',
    component: CarilerView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'cari',
    },
  },
  {
    path: '/cariler/:id/ekstre',
    name: 'cari-ekstre',
    component: CariEkstreView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'cari',
    },
  },
  {
    path: '/faturalar',
    name: 'faturalar',
    component: FaturaView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'fatura',
    },
  },
  {
    path: '/kasa-fisleri',
    name: 'kasa-fisleri',
    component: KasaFisView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'kasa',
    },
  },
  {
    path: '/hks',
    name: 'hks',
    component: HksView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'hks',
    },
  },
  {
    path: '/settings',
    name: 'settings',
    component: SettingsView,
    meta: {
      requiresAuth: true,
      userOnly: true,
      section: 'settings',
    },
  },
  {
    path: '/admin',
    name: 'admin',
    component: AdminView,
    meta: {
      requiresAuth: true,
      adminOnly: true,
      section: 'admin',
    },
  },
  {
    path: '/admin/sirketler',
    name: 'admin-customers',
    component: AdminCustomersView,
    meta: {
      requiresAuth: true,
      adminOnly: true,
      section: 'admin',
    },
  },
  {
    path: '/admin/yoneticiler',
    name: 'admin-managers',
    component: AdminManagersView,
    meta: {
      requiresAuth: true,
      adminOnly: true,
      section: 'admin',
    },
  },
  {
    path: '/admin/loglar',
    name: 'admin-logs',
    component: AdminLogsView,
    meta: {
      requiresAuth: true,
      adminOnly: true,
      section: 'admin',
    },
  },
]

const router = createRouter({
  history: createWebHashHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 }
  },
})

router.beforeEach(async (to) => {
  syncSessionState()

  if (to.meta.requiresAuth || to.meta.guestOnly) {
    await ensureSessionState()
  }

  if (to.meta.guestOnly && sessionState.user) {
    return sessionState.user.role === 'Admin' ? '/admin' : '/dashboard'
  }

  if (to.meta.requiresAuth && !sessionState.user) {
    return {
      path: '/login',
      query: {
        expired: 'true',
      },
    }
  }

  if (to.meta.userOnly && sessionState.user?.role === 'Admin') {
    return '/admin'
  }

  if (to.meta.adminOnly && sessionState.user?.role !== 'Admin') {
    return '/dashboard'
  }

  return true
})

export default router
