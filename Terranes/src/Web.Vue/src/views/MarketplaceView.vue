<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { PropertyListing } from '../types';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import PaginationBar from '../components/PaginationBar.vue';
import { useDebounce } from '../composables/useDebounce';
import { useValidation, minValue } from '../composables/useValidation';

const route = useRoute();
const router = useRouter();

const listings = ref<PropertyListing[] | null>(null);
const searchSuburb = ref((route.query.suburb as string) || '');
const maxPrice = ref<number | undefined>(
  route.query.maxPrice ? Number(route.query.maxPrice) : undefined,
);
const selectedStatus = ref((route.query.status as string) || '');
const selectedListing = ref<PropertyListing | null>(null);
const sortBy = ref('price');
const currentPage = ref(1);
const pageSize = 12;
const searchInput = ref<HTMLInputElement | null>(null);

const debouncedSuburb = useDebounce(searchSuburb);
const debouncedPrice = useDebounce(maxPrice);

const statuses = ['Active', 'Draft', 'UnderOffer', 'Sold', 'Withdrawn'];

const resultCount = computed(() => listings.value?.length ?? 0);

const hasActiveFilters = computed(() => !!debouncedSuburb.value || debouncedPrice.value !== undefined || !!selectedStatus.value);

const { errors: priceErrors, validate: validatePrice, clearErrors: clearPriceErrors } = useValidation();

watch(maxPrice, (v) => {
  if (v !== undefined && v !== null && String(v) !== '') {
    validatePrice(v, [minValue(0)]);
  } else {
    clearPriceErrors();
  }
});

const sortedListings = computed(() => {
  if (!listings.value) return [];
  const sorted = [...listings.value];
  if (sortBy.value === 'price') sorted.sort((a, b) => (a.askingPriceAud ?? Infinity) - (b.askingPriceAud ?? Infinity));
  else if (sortBy.value === 'date') sorted.sort((a, b) => new Date(b.listedUtc).getTime() - new Date(a.listedUtc).getTime());
  return sorted;
});

const paginatedListings = computed(() => {
  const start = (currentPage.value - 1) * pageSize;
  return sortedListings.value.slice(start, start + pageSize);
});

function formatPrice(price?: number): string {
  if (price == null) return 'Price on Application';
  return `$${price.toLocaleString('en-AU', { maximumFractionDigits: 0 })}`;
}

async function search() {
  listings.value = await api.getListings({
    suburb: debouncedSuburb.value || undefined,
    maxPriceAud: debouncedPrice.value,
    status: selectedStatus.value || undefined,
  });
  currentPage.value = 1;
}

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedSuburb.value) query.suburb = debouncedSuburb.value;
  if (debouncedPrice.value !== undefined) query.maxPrice = String(debouncedPrice.value);
  if (selectedStatus.value) query.status = selectedStatus.value;
  router.replace({ query });
}

function viewListing(listing: PropertyListing) {
  selectedListing.value = listing;
}

function closeModal() {
  selectedListing.value = null;
}

function removeSuburbFilter() { searchSuburb.value = ''; }
function removePriceFilter() { maxPrice.value = undefined; }
function removeStatusFilter() { selectedStatus.value = ''; }
function clearAllFilters() {
  searchSuburb.value = '';
  maxPrice.value = undefined;
  selectedStatus.value = '';
}

onMounted(() => {
  search();
  searchInput.value?.focus();
});
watch([debouncedSuburb, debouncedPrice, selectedStatus], () => { search(); syncQuery(); });
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏬 Property Marketplace</h2>
    <p class="text-muted">Browse property listings from agents, builders, and homeowners.</p>

    <div class="row mb-3">
      <div class="col-12 col-md-3">
        <input type="text" class="form-control" placeholder="Suburb..." v-model="searchSuburb" ref="searchInput" />
      </div>
      <div class="col-12 col-md-3">
        <input type="number" class="form-control" :class="{ 'is-invalid': priceErrors.length > 0 }" placeholder="Max price ($)" v-model.number="maxPrice" />
        <div v-if="priceErrors.length > 0" class="invalid-feedback">
          {{ priceErrors[0] }}
        </div>
      </div>
      <div class="col-12 col-md-3">
        <select class="form-select" v-model="selectedStatus">
          <option value="">All Statuses</option>
          <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
        </select>
      </div>
      <div class="col-12 col-md-3">
        <select class="form-select" v-model="sortBy">
          <option value="price">Sort by Price</option>
          <option value="date">Sort by Date</option>
        </select>
      </div>
    </div>

    <div class="mb-3 d-flex flex-wrap align-items-center">
      <FilterChip v-if="debouncedSuburb" :label="`Suburb: ${debouncedSuburb}`" @remove="removeSuburbFilter" />
      <FilterChip v-if="debouncedPrice !== undefined" :label="`Max: ${formatPrice(debouncedPrice)}`" @remove="removePriceFilter" />
      <FilterChip v-if="selectedStatus" :label="`Status: ${selectedStatus}`" @remove="removeStatusFilter" />
      <button v-if="hasActiveFilters" class="btn btn-sm btn-outline-danger ms-2 clear-all-filters" @click="clearAllFilters">Clear All Filters</button>
      <span v-if="listings !== null" class="badge bg-secondary ms-auto result-count">Showing {{ resultCount }} results</span>
    </div>

    <SkeletonCard v-if="listings === null" :count="2" :columns="2" />
    <EmptyState v-else-if="listings.length === 0" message="No listings found matching your criteria." />
    <template v-else>
      <div class="row g-4">
        <div class="col-12 col-md-6" v-for="listing in paginatedListings" :key="listing.id">
          <div class="card h-100 shadow-sm">
            <div class="card-body">
              <div class="d-flex justify-content-between align-items-start">
                <h5 class="card-title">{{ listing.title }}</h5>
                <StatusBadge :status="listing.status" />
              </div>
              <p class="card-text text-muted">{{ listing.description }}</p>
              <div class="d-flex justify-content-between">
                <span v-if="listing.askingPriceAud != null" class="h5 text-success">
                  {{ formatPrice(listing.askingPriceAud) }}
                </span>
                <span v-else class="text-muted">Price on Application</span>
                <small class="text-muted">Listed {{ new Date(listing.listedUtc).toLocaleDateString() }}</small>
              </div>
            </div>
            <div class="card-footer">
              <button class="btn btn-sm btn-outline-primary" aria-label="View details for this listing" @click="viewListing(listing)">View Details</button>
            </div>
          </div>
        </div>
      </div>
      <PaginationBar
        :total-items="sortedListings.length"
        :page-size="pageSize"
        :current-page="currentPage"
        @page-change="currentPage = $event"
      />
    </template>

    <DetailModal :show="!!selectedListing" :title="selectedListing?.title ?? ''" @close="closeModal">
      <template v-if="selectedListing">
        <p>{{ selectedListing.description }}</p>
        <table class="table table-sm">
          <tbody>
            <tr>
              <th>Status</th>
              <td><StatusBadge :status="selectedListing.status" /></td>
            </tr>
            <tr>
              <th>Price</th>
              <td>{{ formatPrice(selectedListing.askingPriceAud) }}</td>
            </tr>
            <tr>
              <th>Home Model ID</th>
              <td><code>{{ selectedListing.homeModelId }}</code></td>
            </tr>
            <tr v-if="selectedListing.landBlockId">
              <th>Land Block ID</th>
              <td><code>{{ selectedListing.landBlockId }}</code></td>
            </tr>
            <tr>
              <th>Listed</th>
              <td>{{ new Date(selectedListing.listedUtc).toLocaleString() }}</td>
            </tr>
          </tbody>
        </table>
      </template>
    </DetailModal>
  </div>
</template>
