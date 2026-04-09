<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { LandBlock, HomeModel, SitePlacement } from '../types';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import { useToast } from '../composables/useToast';
import { useDebounce } from '../composables/useDebounce';
import { useValidation, required } from '../composables/useValidation';
import { usePagedList } from '../composables/usePagedList';

const { showSuccess, showError } = useToast();
const route = useRoute();
const router = useRouter();

const blocks = ref<LandBlock[] | null>(null);
const searchSuburb = ref((route.query.suburb as string) || '');
const searchState = ref((route.query.state as string) || '');
const selectedBlock = ref<LandBlock | null>(null);
const availableModels = ref<HomeModel[] | null>(null);
const placementResult = ref<SitePlacement | null>(null);
const placementError = ref<string | null>(null);
const sortBy = ref('area');
const searchInput = ref<HTMLInputElement | null>(null);

const debouncedSuburb = useDebounce(searchSuburb);
const debouncedState = useDebounce(searchState);

const { validate: validateSuburb, clearErrors: clearSuburbErrors } = useValidation();
const { validate: validateState, clearErrors: clearStateErrors } = useValidation();

watch(searchSuburb, (v) => {
  if (v.length > 0) {
    validateSuburb(v, [required('Suburb cannot be empty')]);
  } else {
    clearSuburbErrors();
  }
});

watch(searchState, (v) => {
  if (v.length > 0) {
    validateState(v, [required('State cannot be empty')]);
  } else {
    clearStateErrors();
  }
});

const sortedBlocks = computed(() => {
  if (!blocks.value) return [];
  const sorted = [...blocks.value];
  if (sortBy.value === 'area') sorted.sort((a, b) => a.areaSqm - b.areaSqm);
  else if (sortBy.value === 'suburb') sorted.sort((a, b) => a.suburb.localeCompare(b.suburb));
  return sorted;
});

const { visibleItems: pagedBlocks, hasMore, showMore, resetVisible } = usePagedList(sortedBlocks, 20);

const hasActiveFilters = computed(() => !!debouncedSuburb.value || !!debouncedState.value);

const resultCount = computed(() => blocks.value?.length ?? 0);

async function search() {
  blocks.value = await api.getLandBlocks({
    suburb: debouncedSuburb.value || undefined,
    state: debouncedState.value || undefined,
  });
  resetVisible();
}

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedSuburb.value) query.suburb = debouncedSuburb.value;
  if (debouncedState.value) query.state = debouncedState.value;
  router.replace({ query });
}

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

function removeSuburbFilter() { searchSuburb.value = ''; }
function removeStateFilter() { searchState.value = ''; }
function clearAllFilters() {
  searchSuburb.value = '';
  searchState.value = '';
}

onMounted(() => {
  search();
  searchInput.value?.focus();
});
watch([debouncedSuburb, debouncedState], () => { search(); syncQuery(); });
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🗺️ Land Blocks</h2>
    <p class="text-muted">Search available land blocks and test-fit home designs.</p>

    <div class="row mb-3">
      <div class="col-12 col-md-4">
        <input type="text" class="form-control" placeholder="Search by suburb..." v-model="searchSuburb" ref="searchInput" />
      </div>
      <div class="col-12 col-md-3">
        <input type="text" class="form-control" placeholder="State (e.g. NSW)" v-model="searchState" />
      </div>
      <div class="col-12 col-md-3">
        <select class="form-select" v-model="sortBy">
          <option value="area">Sort by Area</option>
          <option value="suburb">Sort by Suburb</option>
        </select>
      </div>
    </div>

    <div class="mb-3 d-flex flex-wrap align-items-center">
      <FilterChip v-if="debouncedSuburb" :label="`Suburb: ${debouncedSuburb}`" @remove="removeSuburbFilter" />
      <FilterChip v-if="debouncedState" :label="`State: ${debouncedState}`" @remove="removeStateFilter" />
      <button v-if="hasActiveFilters" class="btn btn-sm btn-outline-danger ms-2 clear-all-filters" @click="clearAllFilters">Clear All Filters</button>
      <span v-if="blocks !== null" class="badge bg-secondary ms-auto result-count">Showing {{ resultCount }} results</span>
    </div>

    <SkeletonTable v-if="blocks === null" :rows="5" :cols="8" />
    <EmptyState v-else-if="blocks.length === 0" message="No land blocks found. Try a different search." />
    <div v-else class="table-responsive">
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
          <tr v-for="block in pagedBlocks" :key="block.id">
            <td>{{ block.address }}</td>
            <td>{{ block.suburb }}</td>
            <td>{{ block.state }}</td>
            <td>{{ block.areaSqm.toFixed(0) }}</td>
            <td>{{ block.frontageMetre.toFixed(1) }}m</td>
            <td>{{ block.depthMetre.toFixed(1) }}m</td>
            <td><span class="badge bg-secondary">{{ block.zoning }}</span></td>
            <td>
              <button class="btn btn-sm btn-outline-primary" aria-label="Test-fit a design on this land block" @click="selectBlock(block)">Test-Fit</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="hasMore" class="text-center mt-3">
        <button class="btn btn-outline-primary show-more-btn" @click="showMore">Show More</button>
      </div>
    </div>

    <DetailModal :show="!!selectedBlock" :title="selectedBlock ? 'Test-Fit on ' + selectedBlock.address : ''" @close="closeModal">
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
