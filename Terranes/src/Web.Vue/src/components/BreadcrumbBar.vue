<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, type RouteLocationMatched } from 'vue-router';

interface Crumb {
  label: string;
  to?: string;
}

const route = useRoute();

const crumbs = computed<Crumb[]>(() => {
  const result: Crumb[] = [];
  const matched = route.matched as RouteLocationMatched[];

  // Always include Home as the first breadcrumb unless we're on home itself
  const isHome = route.path === '/';
  if (!isHome) {
    result.push({ label: 'Home', to: '/' });
  }

  for (const record of matched) {
    const meta = record.meta as Record<string, unknown>;
    const label = meta.breadcrumb as string | undefined;
    if (label && label !== 'Home') {
      result.push({ label, to: record.path || '/' });
    }
  }

  // Mark the last crumb as current (no link)
  if (result.length > 0) {
    result[result.length - 1].to = undefined;
  }

  return result;
});
</script>

<template>
  <nav v-if="crumbs.length > 1" aria-label="Breadcrumb" class="mb-3">
    <ol class="breadcrumb">
      <li
        v-for="(crumb, index) in crumbs"
        :key="index"
        class="breadcrumb-item"
        :class="{ active: index === crumbs.length - 1 }"
        :aria-current="index === crumbs.length - 1 ? 'page' : undefined"
      >
        <RouterLink v-if="crumb.to" :to="crumb.to">{{ crumb.label }}</RouterLink>
        <span v-else>{{ crumb.label }}</span>
      </li>
    </ol>
  </nav>
</template>
