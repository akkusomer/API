import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import { ensureSessionState } from './session'
import './style.css'

await ensureSessionState()

createApp(App)
  .use(router)
  .mount('#app')
