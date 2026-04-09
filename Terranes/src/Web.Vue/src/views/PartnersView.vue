<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { Partner } from '../types';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import { useDebounce } from '../composables/useDebounce';

const route = useRoute();
const router = useRouter();

const partners = ref<Partner[] | null>(null);
const searchName = ref((route.query.name as string) || '');
const selectedCategory = ref((route.query.category as string) || '');
const selectedPartner = ref<Partner | null>(null);

const debouncedName = useDebounce(searchName);

const categories = ['Builder', 'Landscaper', 'Furniture', 'SmartHome', 'Solicitor', 'RealEstateAgent'];

const staticPartners: Partner[] = [
  { id: 'sp1', name: 'GreenScape Gardens', category: 'Landscaper', description: 'Professional landscaping services for new builds.', contactEmail: 'info@greenscape.demo', isActive: true },
  { id: 'sp2', name: 'Modern Furnish Co', category: 'Furniture', description: 'Contemporary furniture packages for display homes.', contactEmail: 'sales@modernfurnish.demo', isActive: true },
  { id: 'sp3', name: 'SmartLiving Tech', category: 'SmartHome', description: 'Home automation and smart device installation.', contactEmail: 'hello@smartliving.demo', isActive: true },
  { id: 'sp4', name: 'Carter & Associates', category: 'Solicitor', description: 'Property conveyancing and legal services.', contactEmail: 'contact@carter.demo', isActive: false },
  { id: 'sp5', name: 'Prime Realty', category: 'RealEstateAgent', description: 'Specialist new home sales agents.', contactEmail: 'team@primerealty.demo', isActive: true },
];

const allPartners = computed<Partner[]>(() => {
  const builders = partners.value ?? [];
  return [...builders, ...staticPartners];
});

const filteredPartners = computed(() => {
  let result = allPartners.value;
  if (selectedCategory.value) {
    result = result.filter((p) => p.category === selectedCategory.value);
  }
  if (debouncedName.value) {
    const q = debouncedName.value.toLowerCase();
    result = result.filter((p) => p.name.toLowerCase().includes(q));
  }
  return result;
});

const resultCount = computed(() => filteredPartners.value.length);

async function loadBuilders() {
  try {
    partners.value = await api.getBuilders();
  } catch {
    partners.value = [];
  }
}

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedName.value) query.name = debouncedName.value;
  if (selectedCategory.value) query.category = selectedCategory.value;
  router.replace({ query });
}

function viewPartner(partner: Partner) {
  selectedPartner.value = partner;
}

function closeModal() {
  selectedPartner.value = null;
}

function removeNameFilter() { searchName.value = ''; }
function removeCategoryFilter() { selectedCategory.value = ''; }

onMounted(loadBuilders);
watch([debouncedName, selectedCategory], () => { syncQuery(); });
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🤝 Partner Directory</h2>
    <p class="text-muted">Browse our network of trusted building, design, and property partners.</p>

    <div class="row mb-3">
      <div class="col-md-4">
        <input type="text" class="form-control" placeholder="Search by name..." v-model="searchName" />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="selectedCategory">
          <option value="">All Categories</option>
          <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
        </select>
      </div>
    </div>

    <div class="mb-3 d-flex flex-wrap align-items-center">
      <FilterChip v-if="debouncedName" :label="`Name: ${debouncedName}`" @remove="removeNameFilter" />
      <FilterChip v-if="selectedCategory" :label="`Category: ${selectedCategory}`" @remove="removeCategoryFilter" />
      <span v-if="partners !== null" class="badge bg-secondary ms-auto result-count">Showing {{ resultCount }} results</span>
    </div>

    <SkeletonCard v-if="partners === null" :count="3" :columns="3" />
    <EmptyState v-else-if="filteredPartners.length === 0" message="No partners found matching your criteria." />
    <div v-else class="row g-4">
      <div class="col-12 col-md-4" v-for="partner in filteredPartners" :key="partner.id">
        <div class="card h-100 shadow-sm">
          <div class="card-body">
            <div class="d-flex justify-content-between align-items-start">
              <h5 class="card-title">{{ partner.name }}</h5>
              <StatusBadge :status="partner.isActive ? 'Active' : 'Inactive'" />
            </div>
            <span class="badge bg-info mb-2">{{ partner.category }}</span>
            <p class="card-text text-muted">{{ partner.description }}</p>
            <small class="text-muted">📧 {{ partner.contactEmail }}</small>
          </div>
          <div class="card-footer">
            <button class="btn btn-sm btn-outline-primary" aria-label="View details for this partner" @click="viewPartner(partner)">View Details</button>
          </div>
        </div>
      </div>
    </div>

    <DetailModal :show="!!selectedPartner" :title="selectedPartner?.name ?? ''" @close="closeModal">
      <template v-if="selectedPartner">
        <p>{{ selectedPartner.description }}</p>
        <table class="table table-sm">
          <tbody>
            <tr><th>Category</th><td>{{ selectedPartner.category }}</td></tr>
            <tr><th>Email</th><td>{{ selectedPartner.contactEmail }}</td></tr>
            <tr v-if="selectedPartner.phone"><th>Phone</th><td>{{ selectedPartner.phone }}</td></tr>
            <tr v-if="selectedPartner.website"><th>Website</th><td><a :href="selectedPartner.website" target="_blank">{{ selectedPartner.website }}</a></td></tr>
            <tr><th>Status</th><td><StatusBadge :status="selectedPartner.isActive ? 'Active' : 'Inactive'" /></td></tr>
          </tbody>
        </table>
      </template>
    </DetailModal>
  </div>
</template>
