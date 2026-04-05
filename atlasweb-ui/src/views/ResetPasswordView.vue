<template>
  <div class="auth-page">
    <div class="auth-card auth-card--narrow">
      <div class="auth-layout auth-layout--compact">
        <section class="auth-hero auth-hero--compact">
          <AtlasBrand />
          <div class="auth-copy">
            <p class="auth-copy__eyebrow">Şifre Yenile</p>
            <h1>Yeni şifrenizi belirleyin ve hesabınıza güvenli şekilde geri dönün.</h1>
            <p>
              Daha temiz bir kimlik akışı için şifre kurallarını anlık izleyin, sonra tek adımla
              oturum ekranına geri dönün.
            </p>
          </div>
        </section>

        <section class="auth-panel">
          <form class="auth-form" @submit.prevent="handleReset">
            <label class="field">
              <span>Yeni Şifre</span>
              <input v-model="password" type="password" placeholder="En az 8 karakter" required />
            </label>

            <ul class="password-checks">
              <li :class="{ 'is-valid': checks.length }">En az 8 karakter</li>
              <li :class="{ 'is-valid': checks.special }">En az 1 özel karakter</li>
              <li :class="{ 'is-valid': checks.upper }">En az 1 büyük harf</li>
              <li :class="{ 'is-valid': checks.lower }">En az 1 küçük harf</li>
              <li :class="{ 'is-valid': checks.digit }">En az 1 rakam</li>
              <li :class="{ 'is-valid': checks.match }">Şifreler birbiriyle eşleşmeli</li>
            </ul>

            <label class="field">
              <span>Yeni Şifre Tekrar</span>
              <input v-model="confirmPassword" type="password" required />
            </label>

            <p v-if="errorMessage" class="form-error">{{ errorMessage }}</p>
            <p v-if="successMessage" class="form-success">{{ successMessage }}</p>

            <button type="submit" class="primary-button" :disabled="submitting || !canSubmit">
              {{ submitting ? 'Güncelleniyor...' : 'Şifreyi Güncelle' }}
            </button>

            <RouterLink class="linkish" to="/login">Giriş ekranına dön</RouterLink>
          </form>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AtlasBrand from '../components/AtlasBrand.vue'
import { apiClient } from '../services/api'

const route = useRoute()
const router = useRouter()
const password = ref('')
const confirmPassword = ref('')
const errorMessage = ref('')
const successMessage = ref('')
const submitting = ref(false)

const token = computed(() => route.query.token?.toString() || '')
const checks = computed(() => ({
  length: password.value.length >= 8,
  special: /[^a-zA-Z0-9]/.test(password.value),
  upper: /[A-Z]/.test(password.value),
  lower: /[a-z]/.test(password.value),
  digit: /[0-9]/.test(password.value),
  match: password.value.length > 0 && password.value === confirmPassword.value,
}))
const canSubmit = computed(() => token.value && Object.values(checks.value).every(Boolean))

async function handleReset() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!token.value) {
    errorMessage.value = 'Geçersiz şifre sıfırlama bağlantısı.'
    return
  }

  if (!canSubmit.value) {
    errorMessage.value = 'Şifre kurallarını tamamlamalısınız.'
    return
  }

  submitting.value = true
  try {
    const response = await apiClient.resetPassword(token.value, password.value, confirmPassword.value)
    successMessage.value = response?.mesaj || 'Şifre güncellendi.'
    window.setTimeout(() => {
      router.replace('/login?reset=success')
    }, 1200)
  } catch (error) {
    errorMessage.value = error.message || 'Şifre güncellenemedi.'
  } finally {
    submitting.value = false
  }
}
</script>
