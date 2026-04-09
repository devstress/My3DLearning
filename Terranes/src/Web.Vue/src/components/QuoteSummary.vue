<script setup lang="ts">
import type { AggregatedQuote } from '../types';
import LoadingSpinner from './LoadingSpinner.vue';

defineProps<{
  quote: AggregatedQuote | null;
  loading: boolean;
}>();
</script>

<template>
  <div class="quote-summary">
    <LoadingSpinner v-if="loading" message="Loading quote..." />
    <template v-else-if="quote">
      <div class="mb-3">
        <span class="fs-3 fw-bold text-success">${{ quote.totalAmountAud.toLocaleString() }} AUD</span>
      </div>
      <table class="table table-sm table-striped">
        <thead>
          <tr>
            <th>Category</th>
            <th>Description</th>
            <th class="text-end">Amount</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in quote.lineItems" :key="item.id">
            <td>{{ item.category }}</td>
            <td>{{ item.description }}</td>
            <td class="text-end">${{ item.amountAud.toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
      <small class="text-muted">Generated: {{ new Date(quote.generatedUtc).toLocaleString() }}</small>
    </template>
    <p v-else class="text-muted">No quote available</p>
  </div>
</template>
