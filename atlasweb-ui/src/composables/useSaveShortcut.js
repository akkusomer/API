import { computed, onBeforeUnmount, onMounted, unref } from 'vue'

export function useSaveShortcut({
  enabled = true,
  onSave,
}) {
  const isEnabled = computed(() => unref(enabled) !== false)

  async function handleKeydown(event) {
    if (event.defaultPrevented || !isEnabled.value || event.key !== 'F9') {
      return
    }

    event.preventDefault()
    await onSave?.(event)
  }

  onMounted(() => {
    window.addEventListener('keydown', handleKeydown, true)
  })

  onBeforeUnmount(() => {
    window.removeEventListener('keydown', handleKeydown, true)
  })
}
