<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api } from '../api/client';
import type { PartnerProfile } from '../types';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import EmptyState from '../components/EmptyState.vue';

const partnerTypes = [
  { key: 'builder', label: '🔨 Builders', description: 'Licensed home builders and construction firms.' },
  { key: 'landscaper', label: '🌿 Landscapers', description: 'Garden and outdoor design specialists.' },
  { key: 'furniture', label: '🛋️ Furniture', description: 'Furniture suppliers and interior designers.' },
  { key: 'smart-home', label: '🏠 Smart Home', description: 'Home automation and IoT technology providers.' },
  { key: 'solicitor', label: '⚖️ Solicitors', description: 'Property conveyancing and legal services.' },
  { key: 'real-estate', label: '🏢 Real Estate', description: 'Licensed agents and property consultants.' },
];

const activeTab = ref('builder');
const builders = ref<PartnerProfile[] | null>(null);
const isLoading = ref(false);

async function loadBuilders() {
  isLoading.value = true;
  try {
    builders.value = await api.getBuilders();
  } catch {
    builders.value = [];
  } finally {
    isLoading.value = false;
  }
}

onMounted(loadBuilders);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🤝 Partners</h2>
    <p class="text-muted">Explore our trusted partner network across the home-building ecosystem.</p>

    <ul class="nav nav-pills mb-4 flex-wrap" role="tablist">
      <li v-for="pt in partnerTypes" :key="pt.key" class="nav-item" role="presentation">
        <button
          class="nav-link"
          :class="{ active: activeTab === pt.key }"
          :aria-selected="activeTab === pt.key"
          role="tab"
          @click="activeTab = pt.key"
        >
          {{ pt.label }}
        </button>
      </li>
    </ul>

    <div class="card shadow-sm mb-4">
      <div class="card-body">
        <h5>{{ partnerTypes.find(p => p.key === activeTab)?.label }}</h5>
        <p class="text-muted">{{ partnerTypes.find(p => p.key === activeTab)?.description }}</p>

        <template v-if="activeTab === 'builder'">
          <SkeletonCard v-if="builders === null" :count="3" :columns="3" />
          <EmptyState v-else-if="builders.length === 0" title="No builders found" message="Check back soon for partner listings." icon="partner" />
          <div v-else class="row g-3">
            <div class="col-12 col-md-4" v-for="builder in builders" :key="builder.partnerId">
              <div class="card h-100">
                <div class="card-body">
                  <h6>{{ builder.companyName }}</h6>
                  <p class="text-muted small mb-1">{{ builder.contactEmail }}</p>
                  <StatusBadge :status="builder.isActive ? 'Active' : 'Inactive'" />
                </div>
              </div>
            </div>
          </div>
        </template>

        <template v-else>
          <div class="text-center text-muted py-4">
            <p>📋 Partner listings for this category will be available soon.</p>
            <p class="small">Integration with <strong>{{ partnerTypes.find(p => p.key === activeTab)?.label }}</strong> APIs is ready on the backend.</p>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>
