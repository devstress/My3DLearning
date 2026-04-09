<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const crumbs = computed(() => {
  const items: { label: string; to?: string }[] = [];

  if (route.name !== 'home') {
    items.push({ label: 'Home', to: '/' });
  }

  const breadcrumb = route.meta?.breadcrumb as string | undefined;
  if (breadcrumb) {
    items.push({ label: breadcrumb });
  }

  return items;
});
</script>

<template>
  <nav aria-label="breadcrumb" class="breadcrumb-bar">
    <ol class="breadcrumb mb-2">
      <li
        v-for="(crumb, index) in crumbs"
        :key="index"
        class="breadcrumb-item"
        :class="{ active: index === crumbs.length - 1 }"
      >
        <RouterLink v-if="crumb.to && index < crumbs.length - 1" :to="crumb.to">
          {{ crumb.label }}
        </RouterLink>
        <span v-else>{{ crumb.label }}</span>
      </li>
    </ol>
  </nav>
</template>
