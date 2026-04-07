<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import type { VirtualVillage, VillageLot } from '../types';
import LoadingSpinner from '../components/LoadingSpinner.vue';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';

const villages = ref<VirtualVillage[] | null>(null);
const searchName = ref('');
const selectedLayout = ref('');
const selectedVillage = ref<VirtualVillage | null>(null);
const villageLots = ref<VillageLot[] | null>(null);

const layouts = ['Grid', 'Radial', 'Linear', 'Cluster', 'Freeform'];

async function search() {
  villages.value = await api.getVillages({
    name: searchName.value || undefined,
    layout: selectedLayout.value || undefined,
  });
}

async function viewVillage(village: VirtualVillage) {
  selectedVillage.value = village;
  villageLots.value = await api.getVillageLots(village.id);
}

function closeModal() {
  selectedVillage.value = null;
  villageLots.value = null;
}

onMounted(search);
watch([searchName, selectedLayout], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏘️ Virtual Villages</h2>
    <p class="text-muted">Explore immersive 3D neighbourhoods of fully designed homes.</p>

    <div class="row mb-3">
      <div class="col-md-4">
        <input type="text" class="form-control" placeholder="Search by name..." v-model="searchName" />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="selectedLayout">
          <option value="">All Layouts</option>
          <option v-for="layout in layouts" :key="layout" :value="layout">{{ layout }}</option>
        </select>
      </div>
    </div>

    <LoadingSpinner v-if="villages === null" message="Loading villages..." />
    <div v-else-if="villages.length === 0" class="alert alert-info">
      No villages found. Create one to get started!
    </div>
    <div v-else class="row g-4">
      <div class="col-md-4" v-for="village in villages" :key="village.id">
        <div class="card h-100 shadow-sm">
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
            <button class="btn btn-sm btn-outline-primary" @click="viewVillage(village)">View Details</button>
          </div>
        </div>
      </div>
    </div>

    <DetailModal :show="!!selectedVillage" :title="selectedVillage?.name ?? ''" @close="closeModal">
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
