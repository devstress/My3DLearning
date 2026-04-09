<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { api } from '../api/client';
import type { SearchResult } from '../types';
import SearchBar from '../components/SearchBar.vue';
import StatusBadge from '../components/StatusBadge.vue';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import EmptyState from '../components/EmptyState.vue';
import { useDebounce } from '../composables/useDebounce';

const router = useRouter();

const query = ref('');
const results = ref<SearchResult[] | null>(null);
const isSearching = ref(false);
const errorMessage = ref<string | null>(null);

const debouncedQuery = useDebounce(query, 400);

const entityTypes = [
  { value: '', label: 'All Types' },
  { value: 'HomeModel', label: 'Home Designs' },
  { value: 'LandBlock', label: 'Land Blocks' },
  { value: 'VirtualVillage', label: 'Villages' },
  { value: 'PropertyListing', label: 'Listings' },
];
const selectedType = ref('');

async function doSearch() {
  const q = debouncedQuery.value?.trim();
  if (!q) {
    results.value = null;
    return;
  }
  isSearching.value = true;
  errorMessage.value = null;
  try {
    results.value = selectedType.value
      ? await api.searchByType(selectedType.value, q)
      : await api.search(q);
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Search failed';
    results.value = [];
  } finally {
    isSearching.value = false;
  }
}

function navigateToResult(result: SearchResult) {
  const typeRoutes: Record<string, string> = {
    HomeModel: '/home-models',
    LandBlock: '/land',
    VirtualVillage: '/villages',
    PropertyListing: '/marketplace',
  };
  const path = typeRoutes[result.entityType] ?? '/';
  router.push(path);
}

watch([debouncedQuery, selectedType], doSearch);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🔍 Search</h2>
    <p class="text-muted">Search across all entities — homes, land, villages, and listings.</p>

    <div class="row mb-3">
      <div class="col-md-6">
        <SearchBar v-model="query" placeholder="Search everything..." />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="selectedType" aria-label="Filter by entity type">
          <option v-for="t in entityTypes" :key="t.value" :value="t.value">{{ t.label }}</option>
        </select>
      </div>
    </div>

    <LoadingSpinner v-if="isSearching" message="Searching..." />

    <div v-if="errorMessage" class="alert alert-danger" role="alert">{{ errorMessage }}</div>

    <EmptyState
      v-if="results !== null && results.length === 0 && !isSearching"
      title="No results found"
      message="Try a different search term or filter."
      icon="search"
    />

    <template v-if="results && results.length > 0">
      <p class="text-muted small mb-2">
        <span class="badge bg-secondary result-count">{{ results.length }}</span>
        result{{ results.length !== 1 ? 's' : '' }}
      </p>

      <div class="list-group">
        <button
          v-for="result in results"
          :key="result.entityId"
          class="list-group-item list-group-item-action d-flex justify-content-between align-items-start"
          @click="navigateToResult(result)"
        >
          <div>
            <h6 class="mb-1">{{ result.title }}</h6>
            <p class="mb-1 text-muted small">{{ result.summary }}</p>
          </div>
          <div class="text-end">
            <StatusBadge :status="result.entityType" />
            <br />
            <small class="text-muted">Score: {{ result.relevanceScore.toFixed(1) }}</small>
          </div>
        </button>
      </div>
    </template>
  </div>
</template>
