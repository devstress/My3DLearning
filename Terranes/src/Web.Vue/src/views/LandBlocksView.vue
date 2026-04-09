<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { LandBlock, HomeModel, SitePlacement } from '../types';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import SearchBar from '../components/SearchBar.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import PaginationBar from '../components/PaginationBar.vue';
import { useToast } from '../composables/useToast';
import { useDebounce } from '../composables/useDebounce';
import { usePagedList } from '../composables/usePagedList';

const { showSuccess, showError } = useToast();
const route = useRoute();
const router = useRouter();

const blocks = ref<LandBlock[] | null>(null);
const searchSuburb = ref((route.query.suburb as string) ?? '');
const searchState = ref((route.query.state as string) ?? '');
const selectedBlock = ref<LandBlock | null>(null);
const availableModels = ref<HomeModel[] | null>(null);
const placementResult = ref<SitePlacement | null>(null);
const placementError = ref<string | null>(null);

const debouncedSuburb = useDebounce(searchSuburb, 300);
const debouncedState = useDebounce(searchState, 300);

const sortBy = ref((route.query.sort as string) ?? '');
const sortOptions = [
  { value: '', label: 'Default' },
  { value: 'area-asc', label: 'Area: Smallest' },
  { value: 'area-desc', label: 'Area: Largest' },
  { value: 'frontage-desc', label: 'Frontage: Widest' },
];

const sortedBlocks = computed(() => {
  if (!blocks.value) return null;
  const arr = [...blocks.value];
  if (sortBy.value === 'area-asc') arr.sort((a, b) => a.areaSqm - b.areaSqm);
  if (sortBy.value === 'area-desc') arr.sort((a, b) => b.areaSqm - a.areaSqm);
  if (sortBy.value === 'frontage-desc') arr.sort((a, b) => b.frontageMetre - a.frontageMetre);
  return arr;
});

const { currentPage, totalPages, pagedItems, goToPage, resetPage } = usePagedList(sortedBlocks, 12);

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedSuburb.value) query.suburb = debouncedSuburb.value;
  if (debouncedState.value) query.state = debouncedState.value;
  if (sortBy.value) query.sort = sortBy.value;
  router.replace({ query });
}

async function search() {
  syncQuery();
  resetPage();
  blocks.value = await api.getLandBlocks({
    suburb: debouncedSuburb.value || undefined,
    state: debouncedState.value || undefined,
  });
}

function clearSuburb() { searchSuburb.value = ''; }
function clearState() { searchState.value = ''; }
function clearAllFilters() { clearSuburb(); clearState(); sortBy.value = ''; }

async function selectBlock(block: LandBlock) {
  selectedBlock.value = block;
  placementResult.value = null;
  placementError.value = null;
  availableModels.value = await api.getHomeModels();
}

async function testFit(model: HomeModel) {
  try {
    placementError.value = null;
    placementResult.value = await api.createSitePlacement(selectedBlock.value!.id, model.id);
    showSuccess(`✅ ${model.name} placed successfully on ${selectedBlock.value!.address}`);
  } catch (err: unknown) {
    placementError.value = err instanceof Error ? err.message : 'Unknown error';
    placementResult.value = null;
    showError('Test-fit failed. The design may not fit this block.');
  }
}

function closeModal() {
  selectedBlock.value = null;
  availableModels.value = null;
  placementResult.value = null;
  placementError.value = null;
}

onMounted(search);
watch([debouncedSuburb, debouncedState], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🗺️ Land Blocks</h2>
    <p class="text-muted">Search available land blocks and test-fit home designs.</p>

    <div class="row mb-3">
      <div class="col-md-4">
        <SearchBar v-model="searchSuburb" placeholder="Search by suburb..." />
      </div>
      <div class="col-md-3">
        <SearchBar v-model="searchState" placeholder="State (e.g. NSW)" />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="sortBy" aria-label="Sort by">
          <option v-for="opt in sortOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
        </select>
      </div>
    </div>

    <div v-if="debouncedSuburb || debouncedState" class="d-flex flex-wrap gap-2 mb-3">
      <FilterChip v-if="debouncedSuburb" label="Suburb" :value="debouncedSuburb" @remove="clearSuburb" />
      <FilterChip v-if="debouncedState" label="State" :value="debouncedState" @remove="clearState" />
      <button class="btn btn-sm btn-outline-secondary" aria-label="Clear all filters" @click="clearAllFilters">✕ Clear All</button>
    </div>

    <SkeletonTable v-if="blocks === null" :rows="5" :cols="8" />
    <EmptyState v-else-if="blocks.length === 0" title="No land blocks found" message="Try a different suburb or state." icon="land" />
    <template v-else>
    <p class="text-muted small mb-2"><span class="badge bg-secondary result-count">{{ blocks.length }}</span> result{{ blocks.length !== 1 ? 's' : '' }}</p>
    <div class="table-responsive">
      <table class="table table-hover">
        <thead>
          <tr>
            <th>Address</th>
            <th>Suburb</th>
            <th>State</th>
            <th>Area (m²)</th>
            <th>Frontage</th>
            <th>Depth</th>
            <th>Zoning</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="block in pagedItems" :key="block.id">
            <td>{{ block.address }}</td>
            <td>{{ block.suburb }}</td>
            <td>{{ block.state }}</td>
            <td>{{ block.areaSqm.toFixed(0) }}</td>
            <td>{{ block.frontageMetre.toFixed(1) }}m</td>
            <td>{{ block.depthMetre.toFixed(1) }}m</td>
            <td><span class="badge bg-secondary">{{ block.zoning }}</span></td>
            <td>
              <button class="btn btn-sm btn-outline-primary" aria-label="Test-fit design on block" @click="selectBlock(block)">Test-Fit</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <PaginationBar :current-page="currentPage" :total-pages="totalPages" @page="goToPage" />
    </template>

    <DetailModal :show="!!selectedBlock" :title="selectedBlock ? 'Test-Fit on ' + selectedBlock.address : ''" back-label="Land Blocks" @close="closeModal">
      <template v-if="selectedBlock">
        <div class="row mb-3">
          <div class="col">
            <table class="table table-sm">
              <tbody>
                <tr><th>Address</th><td>{{ selectedBlock.address }}, {{ selectedBlock.suburb }} {{ selectedBlock.state }}</td></tr>
                <tr><th>Area</th><td>{{ selectedBlock.areaSqm.toFixed(0) }} m²</td></tr>
                <tr><th>Frontage × Depth</th><td>{{ selectedBlock.frontageMetre.toFixed(1) }}m × {{ selectedBlock.depthMetre.toFixed(1) }}m</td></tr>
                <tr><th>Zoning</th><td>{{ selectedBlock.zoning }}</td></tr>
              </tbody>
            </table>
          </div>
        </div>

        <h6>Select a Home Design to Test-Fit</h6>
        <LoadingSpinner v-if="availableModels === null" message="Loading designs..." />
        <p v-else-if="availableModels.length === 0" class="text-muted">No home designs available. Create one first.</p>
        <div v-else class="list-group">
          <button
            v-for="model in availableModels"
            :key="model.id"
            class="list-group-item list-group-item-action d-flex justify-content-between align-items-center"
            @click="testFit(model)"
          >
            <div>
              <strong>{{ model.name }}</strong> —
              {{ model.bedrooms }} bed, {{ model.bathrooms }} bath, {{ model.floorAreaSqm.toFixed(0) }} m²
            </div>
            <span class="badge bg-primary">Test-Fit →</span>
          </button>
        </div>

        <div v-if="placementResult" class="alert alert-success mt-3">
          <strong>✅ Placement Created!</strong><br />
          Placement ID: <code>{{ placementResult.id }}</code><br />
          Offset: {{ placementResult.offsetX.toFixed(1) }}m × {{ placementResult.offsetY.toFixed(1) }}m |
          Rotation: {{ placementResult.rotationDegrees }}° | Scale: {{ placementResult.scaleFactor }}
        </div>

        <ErrorAlert :message="placementError" />
      </template>
    </DetailModal>
  </div>
</template>
