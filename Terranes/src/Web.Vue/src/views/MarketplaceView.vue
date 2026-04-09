<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { api } from '../api/client';
import type { PropertyListing } from '../types';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';
import SearchBar from '../components/SearchBar.vue';
import FilterChip from '../components/FilterChip.vue';
import EmptyState from '../components/EmptyState.vue';
import PaginationBar from '../components/PaginationBar.vue';
import { useDebounce } from '../composables/useDebounce';
import { usePagedList } from '../composables/usePagedList';

const route = useRoute();
const router = useRouter();

const listings = ref<PropertyListing[] | null>(null);
const searchSuburb = ref((route.query.suburb as string) ?? '');
const maxPrice = ref<number | undefined>(
  route.query.maxPrice ? Number(route.query.maxPrice) : undefined,
);
const selectedStatus = ref((route.query.status as string) ?? '');
const selectedListing = ref<PropertyListing | null>(null);

const debouncedSuburb = useDebounce(searchSuburb, 300);
const debouncedPrice = useDebounce(maxPrice, 300);

const statuses = ['Active', 'Draft', 'UnderOffer', 'Sold', 'Withdrawn'];
const sortBy = ref((route.query.sort as string) ?? '');
const sortOptions = [
  { value: '', label: 'Default' },
  { value: 'price-asc', label: 'Price: Low → High' },
  { value: 'price-desc', label: 'Price: High → Low' },
  { value: 'date-desc', label: 'Newest First' },
  { value: 'date-asc', label: 'Oldest First' },
];

function formatPrice(price?: number): string {
  if (price == null) return 'Price on Application';
  return `$${price.toLocaleString('en-AU', { maximumFractionDigits: 0 })}`;
}

const sortedListings = computed(() => {
  if (!listings.value) return null;
  const arr = [...listings.value];
  if (sortBy.value === 'price-asc') arr.sort((a, b) => (a.askingPriceAud ?? Infinity) - (b.askingPriceAud ?? Infinity));
  if (sortBy.value === 'price-desc') arr.sort((a, b) => (b.askingPriceAud ?? 0) - (a.askingPriceAud ?? 0));
  if (sortBy.value === 'date-desc') arr.sort((a, b) => new Date(b.listedUtc).getTime() - new Date(a.listedUtc).getTime());
  if (sortBy.value === 'date-asc') arr.sort((a, b) => new Date(a.listedUtc).getTime() - new Date(b.listedUtc).getTime());
  return arr;
});

const { currentPage, totalPages, pagedItems, goToPage, resetPage } = usePagedList(sortedListings, 12);

function syncQuery() {
  const query: Record<string, string> = {};
  if (debouncedSuburb.value) query.suburb = debouncedSuburb.value;
  if (debouncedPrice.value !== undefined && debouncedPrice.value !== null) query.maxPrice = String(debouncedPrice.value);
  if (selectedStatus.value) query.status = selectedStatus.value;
  if (sortBy.value) query.sort = sortBy.value;
  router.replace({ query });
}

async function search() {
  syncQuery();
  resetPage();
  listings.value = await api.getListings({
    suburb: debouncedSuburb.value || undefined,
    maxPriceAud: debouncedPrice.value,
    status: selectedStatus.value || undefined,
  });
}

function clearSuburb() { searchSuburb.value = ''; }
function clearPrice() { maxPrice.value = undefined; }
function clearStatus() { selectedStatus.value = ''; }

function viewListing(listing: PropertyListing) {
  selectedListing.value = listing;
}

function closeModal() {
  selectedListing.value = null;
}

onMounted(search);
watch([debouncedSuburb, debouncedPrice, selectedStatus], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏬 Property Marketplace</h2>
    <p class="text-muted">Browse property listings from agents, builders, and homeowners.</p>

    <div class="row mb-3">
      <div class="col-md-3">
        <SearchBar v-model="searchSuburb" placeholder="Suburb..." />
      </div>
      <div class="col-md-3">
        <input type="number" class="form-control" placeholder="Max price ($)" v-model.number="maxPrice" aria-label="Maximum price" />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="selectedStatus" aria-label="Filter by status">
          <option value="">All Statuses</option>
          <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
        </select>
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="sortBy" aria-label="Sort by">
          <option v-for="opt in sortOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
        </select>
      </div>
    </div>

    <div v-if="debouncedSuburb || debouncedPrice !== undefined || selectedStatus" class="d-flex flex-wrap gap-2 mb-3">
      <FilterChip v-if="debouncedSuburb" label="Suburb" :value="debouncedSuburb" @remove="clearSuburb" />
      <FilterChip v-if="debouncedPrice !== undefined" label="Max Price" :value="formatPrice(debouncedPrice)" @remove="clearPrice" />
      <FilterChip v-if="selectedStatus" label="Status" :value="selectedStatus" @remove="clearStatus" />
    </div>

    <SkeletonCard v-if="listings === null" :count="2" :columns="2" />
    <EmptyState v-else-if="listings.length === 0" title="No listings found" message="Try adjusting your suburb, price, or status filter." icon="listing" />
    <template v-else>
    <p class="text-muted small mb-2"><span class="badge bg-secondary result-count">{{ listings.length }}</span> result{{ listings.length !== 1 ? 's' : '' }}</p>
    <div class="row g-4">
      <div class="col-12 col-md-6" v-for="listing in pagedItems" :key="listing.id">
        <div class="card h-100 shadow-sm card-hover-lift">
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
            <button class="btn btn-sm btn-outline-primary" aria-label="View listing details" @click="viewListing(listing)">View Details</button>
          </div>
        </div>
      </div>
    </div>
    <PaginationBar :current-page="currentPage" :total-pages="totalPages" @page="goToPage" />
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
