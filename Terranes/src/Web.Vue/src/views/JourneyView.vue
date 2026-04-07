<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { BuyerJourney, HomeModel, LandBlock } from '../types';

const DEMO_BUYER_ID = '00000000-0000-0000-0000-000000000001';

const currentJourney = ref<BuyerJourney | null>(null);
const pastJourneys = ref<BuyerJourney[] | null>(null);
const availableModels = ref<HomeModel[] | null>(null);
const availableLand = ref<LandBlock[] | null>(null);
const errorMessage = ref<string | null>(null);

const journeyStages = [
  'Browsing', 'DesignSelected', 'PlacedOnLand',
  'Customising', 'QuoteRequested', 'QuoteReceived', 'Completed',
];

function getStageBadge(stage: string): string {
  switch (stage) {
    case 'Browsing': return 'bg-info';
    case 'DesignSelected':
    case 'PlacedOnLand': return 'bg-primary';
    case 'Customising':
    case 'QuoteRequested': return 'bg-warning text-dark';
    case 'QuoteReceived':
    case 'Referred':
    case 'Completed': return 'bg-success';
    case 'Abandoned': return 'bg-danger';
    default: return 'bg-secondary';
  }
}

function getProgressPercent(): number {
  if (!currentJourney.value) return 0;
  const idx = journeyStages.indexOf(currentJourney.value.currentStage);
  return idx < 0 ? 0 : Math.round(((idx + 1) / journeyStages.length) * 100);
}

async function loadStageData() {
  errorMessage.value = null;
  if (currentJourney.value?.currentStage === 'Browsing') {
    availableModels.value = await api.getHomeModels();
  } else if (currentJourney.value?.currentStage === 'DesignSelected') {
    availableLand.value = await api.getLandBlocks();
  }
}

async function startJourney() {
  currentJourney.value = await api.createJourney(DEMO_BUYER_ID);
  await loadStageData();
}

async function selectDesign(modelId: string) {
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'DesignSelected', modelId);
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
  }
}

async function selectLand(blockId: string) {
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'PlacedOnLand', blockId);
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
  }
}

async function moveToCustomising() {
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'Customising');
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
  }
}

async function requestQuote() {
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'QuoteRequested');
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
  }
}

async function checkQuoteReady() {
  try {
    currentJourney.value = await api.getJourney(currentJourney.value!.id);
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
  }
}

async function completeJourney() {
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'Completed');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
  }
}

async function startNewJourney() {
  currentJourney.value = null;
  await startJourney();
}

onMounted(async () => {
  const journeys = await api.getBuyerJourneys(DEMO_BUYER_ID);
  const active = journeys.find(
    (j) => j.currentStage !== 'Completed' && j.currentStage !== 'Abandoned',
  );
  if (active) {
    currentJourney.value = active;
    await loadStageData();
  }
  pastJourneys.value = journeys;
});
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🚀 Your Buyer Journey</h2>
    <p class="text-muted">Experience the full buyer journey from browsing to quoting.</p>

    <template v-if="!currentJourney">
      <div class="card shadow-sm mb-4">
        <div class="card-body text-center py-5">
          <h4>Start Your Journey</h4>
          <p class="text-muted">Begin by browsing our virtual villages and finding your dream home.</p>
          <button class="btn btn-primary btn-lg" @click="startJourney">🚀 Begin Journey</button>
        </div>
      </div>

      <template v-if="pastJourneys && pastJourneys.length > 0">
        <h5>Previous Journeys</h5>
        <div class="list-group mb-4">
          <div
            v-for="j in pastJourneys"
            :key="j.id"
            class="list-group-item d-flex justify-content-between align-items-center"
          >
            <div>
              <strong>Journey {{ j.id.substring(0, 8) }}...</strong>
              <span class="badge ms-2" :class="getStageBadge(j.currentStage)">{{ j.currentStage }}</span>
            </div>
            <small class="text-muted">Started {{ new Date(j.startedUtc).toLocaleString() }}</small>
          </div>
        </div>
      </template>
    </template>

    <template v-else>
      <div class="card shadow-sm mb-4">
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="mb-0">Journey Progress</h5>
            <span class="badge fs-6" :class="getStageBadge(currentJourney.currentStage)">
              {{ currentJourney.currentStage }}
            </span>
          </div>

          <div class="progress mb-3" style="height: 25px;">
            <div class="progress-bar bg-success" :style="{ width: getProgressPercent() + '%' }">
              {{ currentJourney.currentStage }}
            </div>
          </div>

          <div class="row text-center">
            <div
              v-for="stage in journeyStages"
              :key="stage"
              class="col"
              :class="{
                'text-success': journeyStages.indexOf(currentJourney.currentStage) >= journeyStages.indexOf(stage),
                'text-muted': journeyStages.indexOf(currentJourney.currentStage) < journeyStages.indexOf(stage),
                'fw-bold': currentJourney.currentStage === stage,
              }"
            >
              {{ journeyStages.indexOf(currentJourney.currentStage) >= journeyStages.indexOf(stage) ? '✅' : '⬜' }}
              {{ stage }}
            </div>
          </div>
        </div>
      </div>

      <!-- Browsing -->
      <div v-if="currentJourney.currentStage === 'Browsing'" class="card shadow-sm mb-4">
        <div class="card-body">
          <h5>Step 1: Select a Home Design</h5>
          <p>Choose a home design from our gallery.</p>
          <div v-if="availableModels" class="row g-3">
            <div class="col-md-4" v-for="model in availableModels.slice(0, 6)" :key="model.id">
              <div class="card h-100">
                <div class="card-body">
                  <h6>{{ model.name }}</h6>
                  <small>{{ model.bedrooms }} bed, {{ model.bathrooms }} bath, {{ model.floorAreaSqm.toFixed(0) }} m²</small>
                </div>
                <div class="card-footer">
                  <button class="btn btn-sm btn-primary w-100" @click="selectDesign(model.id)">Select Design</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- DesignSelected -->
      <div v-if="currentJourney.currentStage === 'DesignSelected'" class="card shadow-sm mb-4">
        <div class="card-body">
          <h5>Step 2: Choose a Land Block</h5>
          <p>Select a land block to test-fit your chosen design.</p>
          <div v-if="availableLand" class="list-group">
            <button
              v-for="block in availableLand.slice(0, 5)"
              :key="block.id"
              class="list-group-item list-group-item-action"
              @click="selectLand(block.id)"
            >
              <strong>{{ block.address }}</strong>, {{ block.suburb }} {{ block.state }} —
              {{ block.areaSqm.toFixed(0) }} m², {{ block.zoning }}
            </button>
          </div>
        </div>
      </div>

      <!-- PlacedOnLand -->
      <div v-if="currentJourney.currentStage === 'PlacedOnLand'" class="card shadow-sm mb-4">
        <div class="card-body">
          <h5>Step 3: Customise Your Design</h5>
          <p>Your design has been placed on the land. Move to customisation.</p>
          <button class="btn btn-primary" @click="moveToCustomising">Start Customising →</button>
        </div>
      </div>

      <!-- Customising -->
      <div v-if="currentJourney.currentStage === 'Customising'" class="card shadow-sm mb-4">
        <div class="card-body">
          <h5>Step 4: Request a Quote</h5>
          <p>Your design is customised. Request an indicative quote from our partner network.</p>
          <button class="btn btn-primary" @click="requestQuote">💰 Request Indicative Quote</button>
        </div>
      </div>

      <!-- QuoteRequested -->
      <div v-if="currentJourney.currentStage === 'QuoteRequested'" class="card shadow-sm mb-4">
        <div class="card-body text-center">
          <h5>Quote Requested</h5>
          <p class="text-muted">Your quote is being prepared by our partner network...</p>
          <button class="btn btn-outline-primary" @click="checkQuoteReady">Refresh Status</button>
        </div>
      </div>

      <!-- QuoteReceived -->
      <div v-if="currentJourney.currentStage === 'QuoteReceived'" class="card shadow-sm mb-4">
        <div class="card-body">
          <h5>✅ Quote Received!</h5>
          <p>Your indicative quote is ready. You can proceed to partner referral.</p>
          <button class="btn btn-success" @click="completeJourney">🎉 Complete Journey</button>
        </div>
      </div>

      <!-- Completed / Referred -->
      <div v-if="currentJourney.currentStage === 'Completed' || currentJourney.currentStage === 'Referred'" class="alert alert-success text-center">
        <h4>🎉 Journey Complete!</h4>
        <p>Your journey is complete. Thank you for using Terranes!</p>
        <button class="btn btn-outline-primary" @click="startNewJourney">Start New Journey</button>
      </div>

      <div v-if="errorMessage" class="alert alert-danger">{{ errorMessage }}</div>
    </template>
  </div>
</template>
