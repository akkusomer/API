import { computed, ref, watch } from 'vue'
import { useSession } from '../session'

export function useColumnOrder(storageKeySuffix, defaultColumns) {
  const { sessionState } = useSession()
  const draggedColumnKey = ref('')
  const dropTargetKey = ref('')
  const columnOrder = ref(defaultColumns.map((column) => column.key))

  const storageKey = computed(() =>
    `atlas.columns.${storageKeySuffix}.${sessionState.user?.userId || sessionState.user?.email || 'guest'}`
  )

  const visibleColumns = computed(() =>
    columnOrder.value
      .map((key) => defaultColumns.find((column) => column.key === key))
      .filter(Boolean)
  )

  function normalizeOrder(value) {
    const keys = Array.isArray(value)
      ? value.filter((key) => defaultColumns.some((column) => column.key === key))
      : []

    const missingKeys = defaultColumns
      .map((column) => column.key)
      .filter((key) => !keys.includes(key))

    return [...keys, ...missingKeys]
  }

  function loadOrder() {
    try {
      const raw = window.localStorage.getItem(storageKey.value)
      if (!raw) {
        columnOrder.value = defaultColumns.map((column) => column.key)
        return
      }

      columnOrder.value = normalizeOrder(JSON.parse(raw))
    } catch {
      columnOrder.value = defaultColumns.map((column) => column.key)
    }
  }

  function resetColumns() {
    columnOrder.value = defaultColumns.map((column) => column.key)
  }

  function moveColumn(columnKey, direction) {
    const currentIndex = columnOrder.value.indexOf(columnKey)
    if (currentIndex === -1) {
      return
    }

    const targetIndex = direction === 'left'
      ? currentIndex - 1
      : currentIndex + 1

    if (targetIndex < 0 || targetIndex >= columnOrder.value.length) {
      return
    }

    const nextOrder = [...columnOrder.value]
    const [movedKey] = nextOrder.splice(currentIndex, 1)
    nextOrder.splice(targetIndex, 0, movedKey)
    columnOrder.value = nextOrder
  }

  function handleColumnDragStart(columnKey) {
    draggedColumnKey.value = columnKey
    dropTargetKey.value = columnKey
  }

  function handleColumnDragEnter(columnKey) {
    if (!draggedColumnKey.value || draggedColumnKey.value === columnKey) {
      return
    }

    dropTargetKey.value = columnKey
  }

  function handleColumnDragOver(columnKey) {
    if (!draggedColumnKey.value || draggedColumnKey.value === columnKey) {
      return
    }

    dropTargetKey.value = columnKey
  }

  function handleColumnDrop(targetKey) {
    const sourceKey = draggedColumnKey.value
    if (!sourceKey || sourceKey === targetKey) {
      handleColumnDragEnd()
      return
    }

    const nextOrder = [...columnOrder.value]
    const sourceIndex = nextOrder.indexOf(sourceKey)
    const targetIndex = nextOrder.indexOf(targetKey)

    if (sourceIndex === -1 || targetIndex === -1) {
      handleColumnDragEnd()
      return
    }

    nextOrder.splice(sourceIndex, 1)
    nextOrder.splice(targetIndex, 0, sourceKey)
    columnOrder.value = nextOrder
    handleColumnDragEnd()
  }

  function handleColumnDragEnd() {
    draggedColumnKey.value = ''
    dropTargetKey.value = ''
  }

  watch(
    storageKey,
    () => {
      loadOrder()
    },
    { immediate: true }
  )

  watch(
    columnOrder,
    (value) => {
      window.localStorage.setItem(storageKey.value, JSON.stringify(value))
    },
    { deep: true }
  )

  return {
    visibleColumns,
    columnOrder,
    draggedColumnKey,
    dropTargetKey,
    moveColumn,
    resetColumns,
    handleColumnDragStart,
    handleColumnDragEnter,
    handleColumnDragOver,
    handleColumnDrop,
    handleColumnDragEnd,
  }
}
