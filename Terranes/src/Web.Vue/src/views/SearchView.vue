<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { SearchResult } from '../types';
import SkeletonCard from '../components/SkeletonCard.vue';
import EmptyState from '../components/EmptyState.vue';
import StatusBadge from '../components/StatusBadge.vue';
import { useDebounce } from '../composables/useDebounce';

const route = useRoute();
const router = useRouter();

const searchQuery = ref((route.query.query as string) || '');
const entityType = ref((route.query.type as string) || '');
const results = ref<SearchResult[] | null>(null);
const loading = ref(false);

const debouncedQuery = useDebounce(searchQuery);

const entityTypes = ['HomeModel', 'LandBlock', 'Village', 'Listing', 'Journey'];

const resultCount = computed(() => results.value?.length ?? 0);

async function search() {
  if (!debouncedQuery.value) {
    results.value = [];
    return;
  }
  loading.value = true;
  results.value = null;
  try {
    if (entityType.value) {
      results.value = await api.searchByType(entityType.value, debouncedQuery.value);
    } else {
      results.value = await api.search(debouncedQuery.value);
    }
  } catch {
    results.value = [];
  } finally {
    loading.value = false;
  }
}

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedQuery.value) query.query = debouncedQuery.value;
  if (entityType.value) query.type = entityType.value;
  router.replace({ query });
}

onMounted(() => {
  if (debouncedQuery.value) search();
});

watch([debouncedQuery, entityType], () => { search(); syncQuery(); });
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🔍 Search</h2>
    <p class="text-muted">Search across all Terranes entities.</p>

    <div class="row mb-3">
      <div class="col-md-6">
        <input
          type="text"
          class="form-control"
          placeholder="Enter search query..."
          v-model="searchQuery"
        />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="entityType">
          <option value="">All Types</option>
          <option v-for="t in entityTypes" :key="t" :value="t">{{ t }}</option>
        </select>
      </div>
    </div>

    <div class="mb-3 d-flex flex-wrap align-items-center">
      <span v-if="results !== null" class="badge bg-secondary ms-auto result-count">Showing {{ resultCount }} results</span>
    </div>

    <SkeletonCard v-if="results === null && loading" :count="3" :columns="3" />
    <EmptyState v-else-if="results !== null && results.length === 0" message="No results found. Try a different search query." />
    <div v-else-if="results" class="row g-4">
      <div class="col-12 col-md-4" v-for="result in results" :key="result.entityId">
        <div class="card h-100 shadow-sm">
          <div class="card-body">
            <h5 class="card-title">{{ result.title }}</h5>
            <p class="card-text text-muted">{{ result.summary }}</p>
            <div class="d-flex justify-content-between mb-2">
              <StatusBadge :status="result.entityType" />
              <span class="badge bg-info">Score: {{ result.relevanceScore.toFixed(1) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
