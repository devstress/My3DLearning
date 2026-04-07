<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import type { LandBlock, HomeModel, SitePlacement } from '../types';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import ErrorAlert from '../components/ErrorAlert.vue';

const blocks = ref<LandBlock[] | null>(null);
const searchSuburb = ref('');
const searchState = ref('');
const selectedBlock = ref<LandBlock | null>(null);
const availableModels = ref<HomeModel[] | null>(null);
const placementResult = ref<SitePlacement | null>(null);
const placementError = ref<string | null>(null);

async function search() {
  blocks.value = await api.getLandBlocks({
    suburb: searchSuburb.value || undefined,
    state: searchState.value || undefined,
  });
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
  } catch (err: unknown) {
    placementError.value = err instanceof Error ? err.message : 'Unknown error';
    placementResult.value = null;
  }
}

function closeModal() {
  selectedBlock.value = null;
  availableModels.value = null;
  placementResult.value = null;
  placementError.value = null;
}

onMounted(search);
watch([searchSuburb, searchState], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🗺️ Land Blocks</h2>
    <p class="text-muted">Search available land blocks and test-fit home designs.</p>

    <div class="row mb-3">
      <div class="col-md-4">
        <input type="text" class="form-control" placeholder="Search by suburb..." v-model="searchSuburb" />
      </div>
      <div class="col-md-3">
        <input type="text" class="form-control" placeholder="State (e.g. NSW)" v-model="searchState" />
      </div>
    </div>

    <LoadingSpinner v-if="blocks === null" message="Loading land blocks..." />
    <div v-else-if="blocks.length === 0" class="alert alert-info">
      No land blocks found. Try a different search.
    </div>
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
          <tr v-for="block in blocks" :key="block.id">
            <td>{{ block.address }}</td>
            <td>{{ block.suburb }}</td>
            <td>{{ block.state }}</td>
            <td>{{ block.areaSqm.toFixed(0) }}</td>
            <td>{{ block.frontageMetre.toFixed(1) }}m</td>
            <td>{{ block.depthMetre.toFixed(1) }}m</td>
            <td><span class="badge bg-secondary">{{ block.zoning }}</span></td>
            <td>
              <button class="btn btn-sm btn-outline-primary" @click="selectBlock(block)">Test-Fit</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <DetailModal :show="!!selectedBlock" :title="selectedBlock ? 'Test-Fit on ' + selectedBlock.address : ''" @close="closeModal">
      <template v-if="selectedBlock">
        <div class="row mb-3">
          <div class="col">
            <table class="table table-sm">
              <tr><th>Address</th><td>{{ selectedBlock.address }}, {{ selectedBlock.suburb }} {{ selectedBlock.state }}</td></tr>
              <tr><th>Area</th><td>{{ selectedBlock.areaSqm.toFixed(0) }} m²</td></tr>
              <tr><th>Frontage × Depth</th><td>{{ selectedBlock.frontageMetre.toFixed(1) }}m × {{ selectedBlock.depthMetre.toFixed(1) }}m</td></tr>
              <tr><th>Zoning</th><td>{{ selectedBlock.zoning }}</td></tr>
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
