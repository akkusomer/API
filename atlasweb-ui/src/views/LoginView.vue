<template>
  <div class="auth-page">
    <div class="auth-card">
      <div class="auth-layout">
        <section class="auth-hero">
          <AtlasBrand />
          <div class="auth-copy">
            <p class="auth-copy__eyebrow">Web Operasyon Girişi</p>
            <h1>Atlas'ı daha hafif, daha temiz ve daha premium bir yüzeyle açın.</h1>
            <p>
              Sadece gerekli olan katmanları bıraktık. Daha sakin bir görünüm, daha net
              tipografi ve ekibin paylaşabileceği modern bir operasyon deneyimi.
            </p>
          </div>
          <div class="auth-badges">
            <article class="auth-badge">
              <strong>Çalışma Alanı</strong>
              <span>Sade beyaz katmanlar ve kontrollü yeşil vurgu</span>
            </article>
            <article class="auth-badge">
              <strong>Canlı Veri</strong>
              <span>Mevcut Atlas API akışı ile doğrudan uyumlu</span>
            </article>
            <article class="auth-badge">
              <strong>Hazır Arayüz</strong>
              <span>Frontend ekibiyle paylaşılabilir net bir temel</span>
            </article>
          </div>
        </section>

        <section class="auth-panel">
          <div v-if="route.query.expired" class="auth-notice auth-notice--error">
            Oturumunuz sona erdi. Lütfen tekrar giriş yapın.
          </div>

          <div v-if="route.query.reset === 'success'" class="auth-notice auth-notice--success">
            Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz.
          </div>

          <form class="auth-form" @submit.prevent="handleLogin">
            <label class="field">
              <span>E-Posta</span>
              <input v-model.trim="form.email" type="email" placeholder="mail@firma.com" required />
            </label>

            <label class="field">
              <span>Şifre</span>
              <input v-model="form.password" type="password" placeholder="********" required />
            </label>

            <div class="auth-form__row">
              <label class="field-check">
                <input type="checkbox" />
                <span>Beni hatırla</span>
              </label>
              <button type="button" class="linkish" @click="forgotOpen = true">
                Şifremi unuttum
              </button>
            </div>

            <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>

            <button type="submit" class="primary-button" :disabled="submitting">
              {{ submitting ? 'Giriş Yapılıyor...' : 'Giriş Yap' }}
            </button>
          </form>
        </section>
      </div>
    </div>

    <div v-if="forgotOpen" class="modal-backdrop" @click.self="forgotOpen = false">
      <div class="modal-window modal-window--small">
        <header class="modal-window__header">
          <h2>Şifremi Unuttum</h2>
          <button type="button" class="ghost-icon" @click="forgotOpen = false">x</button>
        </header>
        <p class="modal-window__copy">
          E-posta adresinizi girin, şifre yenileme bağlantısını hemen gönderelim.
        </p>
        <form class="auth-form" @submit.prevent="handleForgotPassword">
          <label class="field">
            <span>E-Posta</span>
            <input v-model.trim="forgotEmail" type="email" required />
          </label>
          <p v-if="forgotState.error" class="form-error">{{ forgotState.error }}</p>
          <p v-if="forgotState.success" class="form-success">{{ forgotState.success }}</p>
          <button type="submit" class="primary-button" :disabled="forgotState.loading">
            {{ forgotState.loading ? 'Gönderiliyor...' : 'Bağlantıyı Gönder' }}
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AtlasBrand from '../components/AtlasBrand.vue'
import { apiClient } from '../services/api'
import { syncSessionState } from '../session'

const route = useRoute()
const router = useRouter()
const form = reactive({
  email: '',
  password: '',
})
const submitting = ref(false)
const errorMessage = ref('')
const forgotOpen = ref(false)
const forgotEmail = ref('')
const forgotState = reactive({
  loading: false,
  error: '',
  success: '',
})

async function handleLogin() {
  errorMessage.value = ''
  submitting.value = true

  try {
    await apiClient.login(form.email, form.password)
    syncSessionState()
    const user = apiClient.getCurrentUser()
    await router.replace(user?.role === 'Admin' ? '/admin' : '/dashboard')
  } catch (error) {
    errorMessage.value = error.message || 'Giriş başarısız.'
  } finally {
    submitting.value = false
  }
}

async function handleForgotPassword() {
  forgotState.loading = true
  forgotState.error = ''
  forgotState.success = ''

  try {
    const response = await apiClient.forgotPassword(forgotEmail.value)
    forgotState.success = response?.mesaj || 'Bağlantı e-posta kutunuza gönderildi.'
  } catch (error) {
    forgotState.error = error.message || 'Bağlantı gönderilemedi.'
  } finally {
    forgotState.loading = false
  }
}
</script>
