<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { api } from '../api/client';
import type { PropertyListing } from '../types';
import DetailModal from '../components/DetailModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonCard from '../components/SkeletonCard.vue';

const listings = ref<PropertyListing[] | null>(null);
const searchSuburb = ref('');
const maxPrice = ref<number | undefined>(undefined);
const selectedStatus = ref('');
const selectedListing = ref<PropertyListing | null>(null);

const statuses = ['Active', 'Draft', 'UnderOffer', 'Sold', 'Withdrawn'];

function formatPrice(price?: number): string {
  if (price == null) return 'Price on Application';
  return `$${price.toLocaleString('en-AU', { maximumFractionDigits: 0 })}`;
}

async function search() {
  listings.value = await api.getListings({
    suburb: searchSuburb.value || undefined,
    maxPriceAud: maxPrice.value,
    status: selectedStatus.value || undefined,
  });
}

function viewListing(listing: PropertyListing) {
  selectedListing.value = listing;
}

function closeModal() {
  selectedListing.value = null;
}

onMounted(search);
watch([searchSuburb, maxPrice, selectedStatus], search);
</script>

<template>
  <div class="container">
    <h2 class="mb-4">🏬 Property Marketplace</h2>
    <p class="text-muted">Browse property listings from agents, builders, and homeowners.</p>

    <div class="row mb-3">
      <div class="col-md-3">
        <input type="text" class="form-control" placeholder="Suburb..." v-model="searchSuburb" />
      </div>
      <div class="col-md-3">
        <input type="number" class="form-control" placeholder="Max price ($)" v-model.number="maxPrice" />
      </div>
      <div class="col-md-3">
        <select class="form-select" v-model="selectedStatus">
          <option value="">All Statuses</option>
          <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
        </select>
      </div>
    </div>

    <SkeletonCard v-if="listings === null" :count="2" :columns="2" />
    <div v-else-if="listings.length === 0" class="alert alert-info">
      No listings found matching your criteria.
    </div>
    <div v-else class="row g-4">
      <div class="col-12 col-md-6" v-for="listing in listings" :key="listing.id">
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
            <button class="btn btn-sm btn-outline-primary" @click="viewListing(listing)">View Details</button>
          </div>
        </div>
      </div>
    </div>

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
