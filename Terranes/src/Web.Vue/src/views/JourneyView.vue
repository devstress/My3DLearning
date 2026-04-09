<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { BuyerJourney, HomeModel, LandBlock, AggregatedQuote } from '../types';
import StatusBadge from '../components/StatusBadge.vue';
import ErrorAlert from '../components/ErrorAlert.vue';
import ActionButton from '../components/ActionButton.vue';
import StepIndicator from '../components/StepIndicator.vue';
import ConfirmDialog from '../components/ConfirmDialog.vue';
import ConfettiEffect from '../components/ConfettiEffect.vue';
import JourneyTimeline from '../components/JourneyTimeline.vue';
import QuoteSummary from '../components/QuoteSummary.vue';
import { useToast } from '../composables/useToast';

const { showSuccess, showError, showInfo } = useToast();

const DEMO_BUYER_ID = '00000000-0000-0000-0000-000000000001';

const currentJourney = ref<BuyerJourney | null>(null);
const pastJourneys = ref<BuyerJourney[] | null>(null);
const availableModels = ref<HomeModel[] | null>(null);
const availableLand = ref<LandBlock[] | null>(null);
const errorMessage = ref<string | null>(null);
const actionLoading = ref(false);
const showConfirmDialog = ref(false);
const showConfetti = ref(false);
const journeyQuotes = ref<AggregatedQuote | null>(null);
const quoteLoading = ref(false);

const journeyStages = [
  'Browsing', 'DesignSelected', 'PlacedOnLand',
  'Customising', 'QuoteRequested', 'QuoteReceived', 'Completed',
];

async function loadStageData() {
  errorMessage.value = null;
  if (currentJourney.value?.currentStage === 'Browsing') {
    availableModels.value = await api.getHomeModels();
  } else if (currentJourney.value?.currentStage === 'DesignSelected') {
    availableLand.value = await api.getLandBlocks();
  } else if (currentJourney.value?.currentStage === 'QuoteReceived') {
    quoteLoading.value = true;
    try {
      const quotes = await api.getJourneyQuotes(currentJourney.value.id);
      journeyQuotes.value = quotes.length > 0 ? quotes[0] : null;
    } finally {
      quoteLoading.value = false;
    }
  }
}

async function startJourney() {
  actionLoading.value = true;
  try {
    currentJourney.value = await api.createJourney(DEMO_BUYER_ID);
    showSuccess('Journey started! Browse our home designs to begin.');
    await loadStageData();
  } catch (err: unknown) {
    showError(err instanceof Error ? err.message : 'Failed to start journey');
  } finally {
    actionLoading.value = false;
  }
}

async function selectDesign(modelId: string) {
  actionLoading.value = true;
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'DesignSelected', modelId);
    showSuccess('Design selected! Now choose a land block.');
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    showError('Failed to select design. Please try again.');
  } finally {
    actionLoading.value = false;
  }
}

async function selectLand(blockId: string) {
  actionLoading.value = true;
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'PlacedOnLand', blockId);
    showSuccess('Land block selected! Your design has been placed.');
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    showError('Failed to place design on land.');
  } finally {
    actionLoading.value = false;
  }
}

async function moveToCustomising() {
  actionLoading.value = true;
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'Customising');
    showInfo('Customisation mode enabled.');
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    showError('Failed to start customisation.');
  } finally {
    actionLoading.value = false;
  }
}

async function requestQuote() {
  actionLoading.value = true;
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'QuoteRequested');
    showSuccess('Quote requested! Our partner network is preparing your quote.');
    await loadStageData();
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    showError('Failed to request quote.');
  } finally {
    actionLoading.value = false;
  }
}

async function checkQuoteReady() {
  actionLoading.value = true;
  try {
    currentJourney.value = await api.getJourney(currentJourney.value!.id);
    if (currentJourney.value.currentStage === 'QuoteReceived') {
      showSuccess('Your quote is ready!');
    } else {
      showInfo('Quote is still being prepared. Check back soon.');
    }
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    showError('Failed to check quote status.');
  } finally {
    actionLoading.value = false;
  }
}

function promptCompleteJourney() {
  showConfirmDialog.value = true;
}

async function completeJourney() {
  showConfirmDialog.value = false;
  actionLoading.value = true;
  try {
    currentJourney.value = await api.advanceJourney(currentJourney.value!.id, 'Completed');
    showConfetti.value = true;
    showSuccess('🎉 Journey complete! Thank you for using Terranes.');
  } catch (err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : 'Unknown error';
    showError('Failed to complete journey.');
  } finally {
    actionLoading.value = false;
  }
}

async function startNewJourney() {
  showConfetti.value = false;
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
          <ActionButton :loading="actionLoading" variant="primary" size="lg" loading-text="Starting..." @click="startJourney">🚀 Begin Journey</ActionButton>
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
              <StatusBadge :status="j.currentStage" />
            </div>
            <small class="text-muted">Started {{ new Date(j.startedUtc).toLocaleString() }}</small>
          </div>
        </div>
      </template>
    </template>

    <template v-else>
      <div class="row">
        <div class="col-md-8">
          <div class="card shadow-sm mb-4">
            <div class="card-body">
              <div class="d-flex justify-content-between align-items-center mb-3">
                <h5 class="mb-0">Journey Progress</h5>
                <StatusBadge :status="currentJourney.currentStage" />
              </div>

              <StepIndicator :stages="journeyStages" :current-stage="currentJourney.currentStage" />
            </div>
          </div>

          <!-- Browsing -->
          <div v-if="currentJourney.currentStage === 'Browsing'" class="card shadow-sm mb-4">
            <div class="card-body">
              <h5>Step 1: Select a Home Design</h5>
              <p>Choose a home design from our gallery.</p>
              <div v-if="availableModels" class="row g-3">
                <div class="col-12 col-md-4" v-for="model in availableModels.slice(0, 6)" :key="model.id">
                  <div class="card h-100">
                    <div class="card-body">
                      <h6>{{ model.name }}</h6>
                      <small>{{ model.bedrooms }} bed, {{ model.bathrooms }} bath, {{ model.floorAreaSqm.toFixed(0) }} m²</small>
                    </div>
                    <div class="card-footer">
                      <button class="btn btn-sm btn-primary w-100" :disabled="actionLoading" @click="selectDesign(model.id)">
                        <span v-if="actionLoading" class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                        Select Design
                      </button>
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
              <ActionButton :loading="actionLoading" loading-text="Starting customisation..." @click="moveToCustomising">Start Customising →</ActionButton>
            </div>
          </div>

          <!-- Customising -->
          <div v-if="currentJourney.currentStage === 'Customising'" class="card shadow-sm mb-4">
            <div class="card-body">
              <h5>Step 4: Request a Quote</h5>
              <p>Your design is customised. Request an indicative quote from our partner network.</p>
              <ActionButton :loading="actionLoading" loading-text="Requesting quote..." @click="requestQuote">💰 Request Indicative Quote</ActionButton>
            </div>
          </div>

          <!-- QuoteRequested -->
          <div v-if="currentJourney.currentStage === 'QuoteRequested'" class="card shadow-sm mb-4">
            <div class="card-body text-center">
              <h5>Quote Requested</h5>
              <p class="text-muted">Your quote is being prepared by our partner network...</p>
              <ActionButton :loading="actionLoading" variant="outline-primary" loading-text="Checking..." @click="checkQuoteReady">Refresh Status</ActionButton>
            </div>
          </div>

          <!-- QuoteReceived -->
          <div v-if="currentJourney.currentStage === 'QuoteReceived'" class="card shadow-sm mb-4">
            <div class="card-body">
              <h5>✅ Quote Received!</h5>
              <p>Your indicative quote is ready. You can proceed to partner referral.</p>
              <QuoteSummary :quote="journeyQuotes" :loading="quoteLoading" />
              <ActionButton :loading="actionLoading" variant="success" loading-text="Completing..." @click="promptCompleteJourney">🎉 Complete Journey</ActionButton>
            </div>
          </div>

          <!-- Completed / Referred -->
          <div v-if="currentJourney.currentStage === 'Completed' || currentJourney.currentStage === 'Referred'" class="alert alert-success text-center">
            <h4>🎉 Journey Complete!</h4>
            <p>Your journey is complete. Thank you for using Terranes!</p>
            <ActionButton :loading="actionLoading" variant="outline-primary" loading-text="Starting..." @click="startNewJourney">Start New Journey</ActionButton>
          </div>

          <ErrorAlert :message="errorMessage" />
        </div>

        <div class="col-md-4">
          <JourneyTimeline
            :stages="journeyStages"
            :current-stage="currentJourney.currentStage"
            :started-utc="currentJourney.startedUtc"
          />
        </div>
      </div>
    </template>

    <ConfirmDialog
      :show="showConfirmDialog"
      title="Complete Journey"
      message="Are you sure you want to complete this journey? This action cannot be undone."
      confirm-text="Complete"
      confirm-variant="success"
      @confirm="completeJourney"
      @cancel="showConfirmDialog = false"
    />

    <ConfettiEffect v-if="showConfetti" />
  </div>
</template>
