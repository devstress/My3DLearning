<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { Walkthrough, WalkthroughPoi } from '../types';
import DetailModal from '../components/DetailModal.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import EmptyState from '../components/EmptyState.vue';
import ActionButton from '../components/ActionButton.vue';
import { useToast } from '../composables/useToast';

const { showSuccess, showError } = useToast();

const DEMO_MODEL_ID = '00000000-0000-0000-0000-000000000001';
const DEMO_USER_ID = '00000000-0000-0000-0000-000000000001';

const walkthroughs = ref<Walkthrough[] | null>(null);
const selectedWalkthrough = ref<Walkthrough | null>(null);
const pois = ref<WalkthroughPoi[]>([]);
const generating = ref(false);
const showGenerateForm = ref(false);
const generateModelId = ref('');

async function loadWalkthroughs() {
  try {
    walkthroughs.value = await api.getWalkthroughsByModel(DEMO_MODEL_ID);
  } catch {
    walkthroughs.value = [];
  }
}

async function viewWalkthrough(wt: Walkthrough) {
  selectedWalkthrough.value = wt;
  try {
    pois.value = await api.getWalkthroughPois(wt.id);
  } catch {
    pois.value = [];
  }
}

function closeModal() {
  selectedWalkthrough.value = null;
  pois.value = [];
}

async function generateWalkthrough() {
  const modelId = generateModelId.value.trim() || DEMO_MODEL_ID;
  generating.value = true;
  try {
    const wt = await api.generateWalkthrough(modelId, DEMO_USER_ID);
    showSuccess('Walkthrough generated successfully!');
    showGenerateForm.value = false;
    generateModelId.value = '';
    if (!walkthroughs.value) walkthroughs.value = [];
    walkthroughs.value.unshift(wt);
  } catch {
    showError('Failed to generate walkthrough.');
  } finally {
    generating.value = false;
  }
}

function truncateId(id: string): string {
  return id.length > 8 ? id.substring(0, 8) + '…' : id;
}

onMounted(loadWalkthroughs);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🚶 3D Walkthroughs</h2>
    <p class="text-muted">View and generate immersive 3D walkthrough sessions for home models.</p>

    <div class="mb-3">
      <button class="btn btn-primary" @click="showGenerateForm = !showGenerateForm">
        {{ showGenerateForm ? 'Cancel' : '+ Generate Walkthrough' }}
      </button>
    </div>

    <div v-if="showGenerateForm" class="card mb-4">
      <div class="card-body">
        <h5 class="card-title">Generate New Walkthrough</h5>
        <div class="row g-2 align-items-end">
          <div class="col-md-6">
            <label class="form-label">Home Model ID</label>
            <input type="text" class="form-control" v-model="generateModelId" placeholder="Leave blank for demo model" />
          </div>
          <div class="col-md-3">
            <ActionButton :loading="generating" variant="success" @click="generateWalkthrough">Generate</ActionButton>
          </div>
        </div>
      </div>
    </div>

    <SkeletonCard v-if="walkthroughs === null" :count="3" :columns="3" />
    <EmptyState v-else-if="walkthroughs.length === 0" message="No walkthroughs found. Generate one to get started!" />
    <div v-else class="row g-4">
      <div class="col-12 col-md-4" v-for="wt in walkthroughs" :key="wt.id">
        <div class="card h-100 shadow-sm">
          <div class="card-body">
            <h5 class="card-title">Walkthrough</h5>
            <p class="card-text">
              <span class="text-muted">Model:</span> <code>{{ truncateId(wt.homeModelId) }}</code>
            </p>
            <div class="d-flex justify-content-between mb-2">
              <span class="badge bg-primary">{{ wt.scenes.length }} scenes</span>
              <small class="text-muted">{{ new Date(wt.generatedUtc).toLocaleDateString() }}</small>
            </div>
          </div>
          <div class="card-footer">
            <button class="btn btn-sm btn-outline-primary" aria-label="View walkthrough details" @click="viewWalkthrough(wt)">View Details</button>
          </div>
        </div>
      </div>
    </div>

    <DetailModal :show="!!selectedWalkthrough" :title="'Walkthrough Details'" @close="closeModal">
      <template v-if="selectedWalkthrough">
        <table class="table table-sm">
          <tbody>
            <tr><th>ID</th><td><code>{{ selectedWalkthrough.id }}</code></td></tr>
            <tr><th>Home Model</th><td><code>{{ selectedWalkthrough.homeModelId }}</code></td></tr>
            <tr v-if="selectedWalkthrough.sitePlacementId"><th>Site Placement</th><td><code>{{ selectedWalkthrough.sitePlacementId }}</code></td></tr>
            <tr><th>Generated</th><td>{{ new Date(selectedWalkthrough.generatedUtc).toLocaleString() }}</td></tr>
          </tbody>
        </table>

        <h6>Scenes ({{ selectedWalkthrough.scenes.length }})</h6>
        <table v-if="selectedWalkthrough.scenes.length > 0" class="table table-sm table-striped">
          <thead><tr><th>#</th><th>Name</th><th>Duration</th></tr></thead>
          <tbody>
            <tr v-for="scene in selectedWalkthrough.scenes" :key="scene.id">
              <td>{{ scene.sceneOrder }}</td>
              <td>{{ scene.sceneName }}</td>
              <td>{{ scene.durationSeconds }}s</td>
            </tr>
          </tbody>
        </table>

        <h6>Points of Interest ({{ pois.length }})</h6>
        <table v-if="pois.length > 0" class="table table-sm table-striped">
          <thead><tr><th>Room</th><th>Label</th><th>Description</th></tr></thead>
          <tbody>
            <tr v-for="poi in pois" :key="poi.id">
              <td>{{ poi.room }}</td>
              <td>{{ poi.label }}</td>
              <td>{{ poi.description }}</td>
            </tr>
          </tbody>
        </table>
        <p v-else class="text-muted">No points of interest loaded.</p>
      </template>
    </DetailModal>
  </div>
</template>
