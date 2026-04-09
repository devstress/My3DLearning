<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { Walkthrough, HomeModel } from '../types';
import ActionButton from '../components/ActionButton.vue';
import StatusBadge from '../components/StatusBadge.vue';
import EmptyState from '../components/EmptyState.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import { useToast } from '../composables/useToast';

const DEMO_USER_ID = '00000000-0000-0000-0000-000000000001';

const { showSuccess, showError } = useToast();

const models = ref<HomeModel[] | null>(null);
const selectedModelId = ref<string | null>(null);
const walkthroughs = ref<Walkthrough[]>([]);
const isGenerating = ref(false);
const isLoading = ref(false);

async function loadModels() {
  isLoading.value = true;
  try {
    models.value = await api.getHomeModels();
  } catch {
    models.value = [];
  } finally {
    isLoading.value = false;
  }
}

async function generateWalkthrough() {
  if (!selectedModelId.value) return;
  isGenerating.value = true;
  try {
    const wt = await api.generateWalkthrough(selectedModelId.value, DEMO_USER_ID);
    walkthroughs.value.unshift(wt);
    showSuccess('Walkthrough generated successfully!');
  } catch (err: unknown) {
    showError(err instanceof Error ? err.message : 'Failed to generate walkthrough');
  } finally {
    isGenerating.value = false;
  }
}

async function loadWalkthroughs(modelId: string) {
  selectedModelId.value = modelId;
  try {
    walkthroughs.value = await api.getWalkthroughsByModel(modelId);
  } catch {
    walkthroughs.value = [];
  }
}

onMounted(loadModels);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🚶 Walkthroughs</h2>
    <p class="text-muted">Generate immersive 3D walkthroughs of home designs and explore them virtually.</p>

    <div class="row">
      <div class="col-12 col-md-5 mb-4">
        <h5>Select a Home Design</h5>
        <SkeletonCard v-if="models === null" :count="3" :columns="1" />
        <EmptyState v-else-if="models.length === 0" title="No models available" message="Add home designs first." icon="home" />
        <div v-else class="list-group">
          <button
            v-for="model in models"
            :key="model.id"
            class="list-group-item list-group-item-action"
            :class="{ active: selectedModelId === model.id }"
            @click="loadWalkthroughs(model.id)"
          >
            <div class="d-flex justify-content-between">
              <strong>{{ model.name }}</strong>
              <small>{{ model.bedrooms }} bed / {{ model.bathrooms }} bath</small>
            </div>
            <small class="text-muted">{{ model.floorAreaSqm.toFixed(0) }} m² — {{ model.format }}</small>
          </button>
        </div>
      </div>

      <div class="col-12 col-md-7">
        <template v-if="selectedModelId">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="mb-0">Walkthroughs</h5>
            <ActionButton
              :loading="isGenerating"
              variant="primary"
              loading-text="Generating..."
              @click="generateWalkthrough"
            >
              🎬 Generate Walkthrough
            </ActionButton>
          </div>

          <EmptyState
            v-if="walkthroughs.length === 0"
            title="No walkthroughs yet"
            message="Generate your first 3D walkthrough for this design."
            icon="walkthrough"
          />

          <div v-else class="list-group">
            <div
              v-for="wt in walkthroughs"
              :key="wt.id"
              class="list-group-item"
            >
              <div class="d-flex justify-content-between align-items-center">
                <div>
                  <strong>Walkthrough {{ wt.id.substring(0, 8) }}...</strong>
                  <br />
                  <small class="text-muted">Created {{ new Date(wt.createdUtc).toLocaleString() }}</small>
                </div>
                <StatusBadge :status="wt.status" />
              </div>
            </div>
          </div>
        </template>

        <template v-else>
          <div class="text-center text-muted py-5">
            <p>👈 Select a home design to view or generate walkthroughs.</p>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>
