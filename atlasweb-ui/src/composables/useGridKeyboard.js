import { computed, onBeforeUnmount, onMounted, unref, watch } from 'vue'

function isInteractiveTarget(target) {
  if (!(target instanceof HTMLElement)) {
    return false
  }

  return Boolean(
    target.closest('input, textarea, select, button, a[href], [contenteditable="true"], .modal-window')
  )
}

export function useGridKeyboard({
  items,
  selectedKey,
  setSelectedKey,
  getKey = (item) => item.id,
  enabled = true,
  onSelect,
  onCreate,
  onEnter,
  onDelete,
  onF10,
}) {
  const list = computed(() => unref(items) || [])
  const isEnabled = computed(() => unref(enabled) !== false)

  const selectedItem = computed(() =>
    list.value.find((item) => getKey(item) === unref(selectedKey)) || null
  )

  function applySelection(item) {
    if (!item) {
      setSelectedKey('')
      return
    }

    const key = getKey(item)
    if (unref(selectedKey) === key) {
      return
    }

    setSelectedKey(key)
    onSelect?.(item)
  }

  function moveSelection(step) {
    if (!list.value.length) {
      return
    }

    const currentIndex = selectedItem.value
      ? list.value.findIndex((item) => getKey(item) === getKey(selectedItem.value))
      : -1

    const nextIndex = Math.min(
      list.value.length - 1,
      Math.max(0, currentIndex === -1 ? 0 : currentIndex + step)
    )

    applySelection(list.value[nextIndex])
  }

  function ensureValidSelection() {
    if (!list.value.length) {
      if (unref(selectedKey)) {
        setSelectedKey('')
      }
      return
    }

    if (!selectedItem.value) {
      applySelection(list.value[0])
    }
  }

  function handleKeydown(event) {
    const isF10 = event.key === 'F10'

    if (event.defaultPrevented || !isEnabled.value || (isInteractiveTarget(event.target) && !isF10)) {
      return
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault()
        moveSelection(1)
        break
      case 'ArrowUp':
        event.preventDefault()
        moveSelection(-1)
        break
      case 'Home':
        event.preventDefault()
        if (list.value.length) {
          applySelection(list.value[0])
        }
        break
      case 'End':
        event.preventDefault()
        if (list.value.length) {
          applySelection(list.value[list.value.length - 1])
        }
        break
      case 'Enter':
        if (selectedItem.value && onEnter) {
          event.preventDefault()
          onEnter(selectedItem.value)
        }
        break
      case 'F2':
        if (onCreate) {
          event.preventDefault()
          onCreate(selectedItem.value)
        }
        break
      case 'F3':
        if (selectedItem.value && onDelete) {
          event.preventDefault()
          onDelete(selectedItem.value)
        }
        break
      case 'F10':
        if (selectedItem.value && onF10) {
          event.preventDefault()
          onF10(selectedItem.value)
        }
        break
      default:
        break
    }
  }

  onMounted(() => {
    window.addEventListener('keydown', handleKeydown, true)
  })

  onBeforeUnmount(() => {
    window.removeEventListener('keydown', handleKeydown, true)
  })

  watch(
    () => list.value.map((item) => getKey(item)),
    () => {
      ensureValidSelection()
    },
    { immediate: true }
  )

  return {
    selectedItem,
    selectItem: applySelection,
  }
}
