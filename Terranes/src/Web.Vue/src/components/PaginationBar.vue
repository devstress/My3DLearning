<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  totalItems: number;
  pageSize?: number;
  currentPage?: number;
}>(), {
  pageSize: 12,
  currentPage: 1,
});

const emit = defineEmits<{ pageChange: [page: number] }>();

const totalPages = computed(() => Math.max(1, Math.ceil(props.totalItems / props.pageSize)));

const startItem = computed(() => props.totalItems === 0 ? 0 : (props.currentPage - 1) * props.pageSize + 1);
const endItem = computed(() => Math.min(props.currentPage * props.pageSize, props.totalItems));

const pageNumbers = computed(() => {
  const pages: number[] = [];
  const total = totalPages.value;
  const current = props.currentPage;
  const start = Math.max(1, current - 2);
  const end = Math.min(total, current + 2);
  for (let i = start; i <= end; i++) pages.push(i);
  return pages;
});
</script>

<template>
  <nav aria-label="Pagination" class="pagination-bar d-flex justify-content-between align-items-center mt-4">
    <small class="text-muted showing-text">
      Showing {{ startItem }}–{{ endItem }} of {{ totalItems }}
    </small>
    <ul class="pagination mb-0">
      <li class="page-item" :class="{ disabled: currentPage <= 1 }">
        <button class="page-link" :disabled="currentPage <= 1" @click="emit('pageChange', currentPage - 1)">
          &laquo; Prev
        </button>
      </li>
      <li
        v-for="p in pageNumbers"
        :key="p"
        class="page-item"
        :class="{ active: p === currentPage }"
      >
        <button class="page-link" @click="emit('pageChange', p)">{{ p }}</button>
      </li>
      <li class="page-item" :class="{ disabled: currentPage >= totalPages }">
        <button class="page-link" :disabled="currentPage >= totalPages" @click="emit('pageChange', currentPage + 1)">
          Next &raquo;
        </button>
      </li>
    </ul>
  </nav>
</template>
