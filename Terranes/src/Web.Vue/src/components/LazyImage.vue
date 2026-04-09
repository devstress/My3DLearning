<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue';

const props = withDefaults(defineProps<{
  src: string;
  alt: string;
  width?: number;
  height?: number;
  placeholderClass?: string;
}>(), {
  placeholderClass: 'card-img-placeholder',
});

const loaded = ref(false);
const imgRef = ref<HTMLImageElement | null>(null);
let observer: IntersectionObserver | null = null;

onMounted(() => {
  if (!imgRef.value) return;

  if (typeof IntersectionObserver === 'undefined') {
    // Fallback: load immediately if IntersectionObserver is not available
    loaded.value = true;
    return;
  }

  observer = new IntersectionObserver(
    (entries) => {
      if (entries[0]?.isIntersecting) {
        loaded.value = true;
        observer?.disconnect();
      }
    },
    { rootMargin: '200px' },
  );
  observer.observe(imgRef.value);
});

onBeforeUnmount(() => {
  observer?.disconnect();
});
</script>

<template>
  <div ref="imgRef" class="lazy-image-container">
    <img
      v-if="loaded"
      :src="src"
      :alt="alt"
      :width="width"
      :height="height"
      class="lazy-image"
      loading="lazy"
    />
    <div v-else :class="placeholderClass" role="img" :aria-label="alt + ' placeholder'"></div>
  </div>
</template>

<style scoped>
.lazy-image-container {
  position: relative;
  overflow: hidden;
}

.lazy-image {
  width: 100%;
  height: auto;
  display: block;
}
</style>
