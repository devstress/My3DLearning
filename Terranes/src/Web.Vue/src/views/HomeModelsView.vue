<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { HomeModel } from '../types';
import DetailModal from '../components/DetailModal.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import PaginationBar from '../components/PaginationBar.vue';
import { useDebounce } from '../composables/useDebounce';
import { usePagedList } from '../composables/usePagedList';

const route = useRoute();
const router = useRouter();

const models = ref<HomeModel[] | null>(null);
const minBedrooms = ref<number | undefined>(
  route.query.minBedrooms ? Number(route.query.minBedrooms) : undefined,
);
const selectedFormat = ref((route.query.format as string) ?? '');
const selectedModel = ref<HomeModel | null>(null);

const debouncedBedrooms = useDebounce(minBedrooms, 300);

const formats = ['Gltf', 'Glb', 'Obj', 'Fbx', 'Usd'];

const { currentPage, totalPages, pagedItems, goToPage, resetPage } = usePagedList(models, 12);

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedBedrooms.value !== undefined && debouncedBedrooms.value !== null) query.minBedrooms = String(debouncedBedrooms.value);
  if (selectedFormat.value) query.format = selectedFormat.value;
  router.replace({ query });
}

async function search() {
  syncQuery();
  resetPage();
  models.value = await api.getHomeModels({
    minBedrooms: debouncedBedrooms.value,
    format: selectedFormat.value || undefined,
  });
}

function clearBedrooms() { minBedrooms.value = undefined; }
function clearFormat() { selectedFormat.value = ''; }

function selectModel(model: HomeModel) {
  selectedModel.value = model;
}

function closeModal() {
  selectedModel.value = null;
}

onMounted(search);
watch([debouncedBedrooms, selectedFormat], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏡 Home Designs</h2>
    <p class="text-muted">Browse our gallery of 3D home models.</p>

    <div class="row mb-3">
      <div class="col-md-3">
        <label class="form-label">Min Bedrooms</label>
        <input type="number" class="form-control" min="0" max="10" v-model.number="minBedrooms" aria-label="Minimum bedrooms" />
      </div>
      <div class="col-md-3">
        <label class="form-label">Format</label>
        <select class="form-select" v-model="selectedFormat" aria-label="Filter by format">
          <option value="">All Formats</option>
          <option v-for="fmt in formats" :key="fmt" :value="fmt">{{ fmt }}</option>
        </select>
      </div>
    </div>

    <div v-if="debouncedBedrooms !== undefined || selectedFormat" class="d-flex flex-wrap gap-2 mb-3">
      <FilterChip v-if="debouncedBedrooms !== undefined" label="Min Beds" :value="String(debouncedBedrooms)" @remove="clearBedrooms" />
      <FilterChip v-if="selectedFormat" label="Format" :value="selectedFormat" @remove="clearFormat" />
    </div>

    <SkeletonCard v-if="models === null" :count="3" :columns="3" />
    <EmptyState v-else-if="models.length === 0" title="No home designs found" message="Try adjusting your filters or bedrooms count." icon="home" />
    <template v-else>
    <p class="text-muted small mb-2"><span class="badge bg-secondary result-count">{{ models.length }}</span> result{{ models.length !== 1 ? 's' : '' }}</p>
    <div class="row g-4">
      <div class="col-12 col-md-4" v-for="model in pagedItems" :key="model.id">
        <div class="card h-100 shadow-sm card-hover-lift">
          <div class="card-img-placeholder"></div>
          <div class="card-body">
            <h5 class="card-title">{{ model.name }}</h5>
            <p class="card-text text-muted small">{{ model.description }}</p>
            <div class="row text-center mb-2">
              <div class="col"><strong>{{ model.bedrooms }}</strong><br /><small>Beds</small></div>
              <div class="col"><strong>{{ model.bathrooms }}</strong><br /><small>Baths</small></div>
              <div class="col"><strong>{{ model.garageSpaces }}</strong><br /><small>Garage</small></div>
            </div>
            <div class="d-flex justify-content-between">
              <span class="badge bg-info">{{ model.format }}</span>
              <span>{{ model.floorAreaSqm.toFixed(0) }} m²</span>
            </div>
          </div>
          <div class="card-footer">
            <button class="btn btn-sm btn-outline-primary" aria-label="View model details" @click="selectModel(model)">View Details</button>
          </div>
        </div>
      </div>
    </div>
    <PaginationBar :current-page="currentPage" :total-pages="totalPages" @page="goToPage" />
    </template>

    <DetailModal :show="!!selectedModel" :title="selectedModel?.name ?? ''" back-label="Home Designs" @close="closeModal">
      <template v-if="selectedModel">
        <p>{{ selectedModel.description }}</p>
        <table class="table table-sm">
          <tbody>
            <tr><th>Bedrooms</th><td>{{ selectedModel.bedrooms }}</td></tr>
            <tr><th>Bathrooms</th><td>{{ selectedModel.bathrooms }}</td></tr>
            <tr><th>Garage Spaces</th><td>{{ selectedModel.garageSpaces }}</td></tr>
            <tr><th>Floor Area</th><td>{{ selectedModel.floorAreaSqm.toFixed(1) }} m²</td></tr>
            <tr><th>Format</th><td>{{ selectedModel.format }}</td></tr>
            <tr><th>File Size</th><td>{{ selectedModel.fileSizeMb.toFixed(1) }} MB</td></tr>
            <tr><th>Created</th><td>{{ new Date(selectedModel.createdUtc).toLocaleString() }}</td></tr>
          </tbody>
        </table>
      </template>
    </DetailModal>
  </div>
</template>
