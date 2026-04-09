<script setup lang="ts">
defineProps<{
  currentPage: number;
  totalPages: number;
}>();

defineEmits<{
  (e: 'page', page: number): void;
}>();
</script>

<template>
  <nav v-if="totalPages > 1" aria-label="Pagination">
    <ul class="pagination justify-content-center">
      <li class="page-item" :class="{ disabled: currentPage <= 1 }">
        <button
          class="page-link"
          aria-label="Previous page"
          :disabled="currentPage <= 1"
          @click="$emit('page', currentPage - 1)"
        >
          «
        </button>
      </li>
      <li
        v-for="page in totalPages"
        :key="page"
        class="page-item"
        :class="{ active: page === currentPage }"
      >
        <button
          class="page-link"
          :aria-label="`Page ${page}`"
          :aria-current="page === currentPage ? 'page' : undefined"
          @click="$emit('page', page)"
        >
          {{ page }}
        </button>
      </li>
      <li class="page-item" :class="{ disabled: currentPage >= totalPages }">
        <button
          class="page-link"
          aria-label="Next page"
          :disabled="currentPage >= totalPages"
          @click="$emit('page', currentPage + 1)"
        >
          »
        </button>
      </li>
    </ul>
  </nav>
</template>
