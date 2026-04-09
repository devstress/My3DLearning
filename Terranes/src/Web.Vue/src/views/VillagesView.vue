<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { VirtualVillage, VillageLot } from '../types';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import SearchBar from '../components/SearchBar.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import PaginationBar from '../components/PaginationBar.vue';
import { useDebounce } from '../composables/useDebounce';
import { usePagedList } from '../composables/usePagedList';

const route = useRoute();
const router = useRouter();

const villages = ref<VirtualVillage[] | null>(null);
const searchName = ref((route.query.name as string) ?? '');
const selectedLayout = ref((route.query.layout as string) ?? '');
const selectedVillage = ref<VirtualVillage | null>(null);
const villageLots = ref<VillageLot[] | null>(null);

const debouncedName = useDebounce(searchName, 300);

const layouts = ['Grid', 'Radial', 'Linear', 'Cluster', 'Freeform'];

const { currentPage, totalPages, pagedItems, goToPage, resetPage } = usePagedList(villages, 12);

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedName.value) query.name = debouncedName.value;
  if (selectedLayout.value) query.layout = selectedLayout.value;
  router.replace({ query });
}

async function search() {
  syncQuery();
  resetPage();
  villages.value = await api.getVillages({
    name: debouncedName.value || undefined,
    layout: selectedLayout.value || undefined,
  });
}

function clearName() { searchName.value = ''; }
function clearLayout() { selectedLayout.value = ''; }
function clearAllFilters() { clearName(); clearLayout(); }

async function viewVillage(village: VirtualVillage) {
  selectedVillage.value = village;
  villageLots.value = await api.getVillageLots(village.id);
}

function closeModal() {
  selectedVillage.value = null;
  villageLots.value = null;
}

onMounted(search);
watch([debouncedName, selectedLayout], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏘️ Virtual Villages</h2>
    <p class="text-muted">Explore immersive 3D neighbourhoods of fully designed homes.</p>

    <div class="row mb-3">
      <div class="col-md-4">
        <SearchBar v-model="searchName" placeholder="Search by name..." />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="selectedLayout" aria-label="Filter by layout">
          <option value="">All Layouts</option>
          <option v-for="layout in layouts" :key="layout" :value="layout">{{ layout }}</option>
        </select>
      </div>
    </div>

    <div v-if="debouncedName || selectedLayout" class="d-flex flex-wrap gap-2 mb-3">
      <FilterChip v-if="debouncedName" label="Name" :value="debouncedName" @remove="clearName" />
      <FilterChip v-if="selectedLayout" label="Layout" :value="selectedLayout" @remove="clearLayout" />
      <button class="btn btn-sm btn-outline-secondary" aria-label="Clear all filters" @click="clearAllFilters">✕ Clear All</button>
    </div>

    <SkeletonCard v-if="villages === null" :count="3" :columns="3" />
    <EmptyState v-else-if="villages.length === 0" title="No villages found" message="Try adjusting your search or create a new village." icon="village" />
    <template v-else>
    <p class="text-muted small mb-2"><span class="badge bg-secondary result-count">{{ villages.length }}</span> result{{ villages.length !== 1 ? 's' : '' }}</p>
    <div class="row g-4">
      <div class="col-12 col-md-4" v-for="village in pagedItems" :key="village.id">
        <div class="card h-100 shadow-sm card-hover-lift">
          <div class="card-img-placeholder"></div>
          <div class="card-body">
            <h5 class="card-title">{{ village.name }}</h5>
            <p class="card-text text-muted">{{ village.description }}</p>
            <div class="d-flex justify-content-between mb-2">
              <span class="badge bg-primary">{{ village.layoutType }}</span>
              <span class="badge bg-secondary">Max {{ village.maxLots }} lots</span>
            </div>
            <small class="text-muted">
              📍 {{ village.centreLatitude.toFixed(4) }}, {{ village.centreLongitude.toFixed(4) }}
            </small>
          </div>
          <div class="card-footer">
            <button class="btn btn-sm btn-outline-primary" aria-label="View village details" @click="viewVillage(village)">View Details</button>
          </div>
        </div>
      </div>
    </div>
    <PaginationBar :current-page="currentPage" :total-pages="totalPages" @page="goToPage" />
    </template>

    <DetailModal :show="!!selectedVillage" :title="selectedVillage?.name ?? ''" back-label="Villages" @close="closeModal">
      <template v-if="selectedVillage">
        <p>{{ selectedVillage.description }}</p>
        <table class="table table-sm">
          <tbody>
            <tr><th>Layout</th><td>{{ selectedVillage.layoutType }}</td></tr>
            <tr><th>Max Lots</th><td>{{ selectedVillage.maxLots }}</td></tr>
            <tr><th>Location</th><td>{{ selectedVillage.centreLatitude.toFixed(4) }}, {{ selectedVillage.centreLongitude.toFixed(4) }}</td></tr>
            <tr><th>Created</th><td>{{ new Date(selectedVillage.createdUtc).toLocaleString() }}</td></tr>
          </tbody>
        </table>

        <template v-if="villageLots && villageLots.length > 0">
          <h6>Lots ({{ villageLots.length }})</h6>
          <table class="table table-sm table-striped">
            <thead><tr><th>#</th><th>Status</th><th>Position</th></tr></thead>
            <tbody>
              <tr v-for="lot in villageLots" :key="lot.id">
                <td>{{ lot.lotNumber }}</td>
                <td>
                  <StatusBadge :status="lot.status" />
                </td>
                <td>{{ lot.positionX.toFixed(1) }}, {{ lot.positionY.toFixed(1) }}</td>
              </tr>
            </tbody>
          </table>
        </template>
      </template>
    </DetailModal>
  </div>
</template>
