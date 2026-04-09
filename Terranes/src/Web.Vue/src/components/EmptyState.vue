<script setup lang="ts">
withDefaults(defineProps<{
  title?: string;
  message?: string;
  icon?: 'search' | 'village' | 'home' | 'land' | 'listing';
}>(), {
  title: 'No results found',
  message: 'Try adjusting your search or filters.',
  icon: 'search',
});

const svgPaths: Record<string, string> = {
  search: 'M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85zm-5.242.156a5 5 0 1 1 0-10 5 5 0 0 1 0 10z',
  village: 'M8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 .5-.5v-3.5a.5.5 0 0 1 .5-.5h2a.5.5 0 0 1 .5.5v3.5a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L13 5.793V2.5a.5.5 0 0 0-.5-.5h-1a.5.5 0 0 0-.5.5v1.293L8.354 1.146z',
  home: 'M2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2zm6.5 4.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3a.5.5 0 0 1 1 0z',
  land: 'M0 1a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H1a1 1 0 0 1-1-1V1zm4 0v6h8V1H4zm8 8H4v6h8V9zM1 1v2h2V1H1zm2 3H1v2h2V4zM1 7v2h2V7H1zm2 3H1v2h2v-2zm-2 3v2h2v-2H1zM15 1h-2v2h2V1zm-2 3v2h2V4h-2zm2 3h-2v2h2V7zm-2 3v2h2v-2h-2zm2 3h-2v2h2v-2z',
  listing: 'M2 1a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H2zm1 2h10v2H3V3zm0 4h10v2H3V7zm0 4h6v2H3v-2z',
};
</script>

<template>
  <div class="text-center py-5 empty-state">
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="64"
      height="64"
      fill="currentColor"
      viewBox="0 0 16 16"
      class="text-muted mb-3"
      aria-hidden="true"
    >
      <path :d="svgPaths[icon]" />
    </svg>
    <h5 class="text-muted">{{ title }}</h5>
    <p class="text-muted small">{{ message }}</p>
  </div>
</template>

<style scoped>
.empty-state svg {
  opacity: 0.5;
}
</style>
