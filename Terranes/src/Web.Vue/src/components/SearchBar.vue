<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const query = ref('');

function performSearch() {
  if (query.value.trim()) {
    router.push({ path: '/search', query: { query: query.value.trim() } });
  }
}
</script>

<template>
  <div class="d-flex align-items-center search-bar">
    <div class="input-group input-group-sm">
      <span class="input-group-text">🔍</span>
      <input
        type="text"
        class="form-control"
        placeholder="Search..."
        v-model="query"
        @keydown.enter="performSearch"
      />
      <button class="btn btn-outline-secondary" type="button" @click="performSearch">Go</button>
    </div>
  </div>
</template>
