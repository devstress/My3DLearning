<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import type { HomeModel } from '../types';
import DetailModal from '../components/DetailModal.vue';
import SkeletonCard from '../components/SkeletonCard.vue';

const models = ref<HomeModel[] | null>(null);
const minBedrooms = ref<number | undefined>(undefined);
const selectedFormat = ref('');
const selectedModel = ref<HomeModel | null>(null);

const formats = ['Gltf', 'Glb', 'Obj', 'Fbx', 'Usd'];

async function search() {
  models.value = await api.getHomeModels({
    minBedrooms: minBedrooms.value,
    format: selectedFormat.value || undefined,
  });
}

function selectModel(model: HomeModel) {
  selectedModel.value = model;
}

function closeModal() {
  selectedModel.value = null;
}

onMounted(search);
watch([minBedrooms, selectedFormat], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏡 Home Designs</h2>
    <p class="text-muted">Browse our gallery of 3D home models.</p>

    <div class="row mb-3">
      <div class="col-md-3">
        <label class="form-label">Min Bedrooms</label>
        <input type="number" class="form-control" min="0" max="10" v-model.number="minBedrooms" />
      </div>
      <div class="col-md-3">
        <label class="form-label">Format</label>
        <select class="form-select" v-model="selectedFormat">
          <option value="">All Formats</option>
          <option v-for="fmt in formats" :key="fmt" :value="fmt">{{ fmt }}</option>
        </select>
      </div>
    </div>

    <SkeletonCard v-if="models === null" :count="3" :columns="3" />
    <div v-else-if="models.length === 0" class="alert alert-info">
      No home designs found matching your criteria.
    </div>
    <div v-else class="row g-4">
      <div class="col-md-4" v-for="model in models" :key="model.id">
        <div class="card h-100 shadow-sm">
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
            <button class="btn btn-sm btn-outline-primary" @click="selectModel(model)">View Details</button>
          </div>
        </div>
      </div>
    </div>

    <DetailModal :show="!!selectedModel" :title="selectedModel?.name ?? ''" @close="closeModal">
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
