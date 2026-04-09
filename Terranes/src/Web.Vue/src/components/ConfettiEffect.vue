<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

withDefaults(defineProps<{
  active?: boolean;
  particleCount?: number;
  duration?: number;
}>(), {
  active: false,
  particleCount: 50,
  duration: 3000,
});

const emit = defineEmits<{
  (e: 'complete'): void;
}>();

interface Particle {
  id: number;
  x: number;
  y: number;
  color: string;
  delay: number;
  size: number;
}

const particles = ref<Particle[]>([]);
let timer: ReturnType<typeof setTimeout> | null = null;

const colors = ['#ff6b6b', '#ffd93d', '#6bcb77', '#4d96ff', '#ff6fb7', '#c084fc'];

function start(count: number, dur: number) {
  particles.value = Array.from({ length: count }, (_, i) => ({
    id: i,
    x: Math.random() * 100,
    y: -10,
    color: colors[i % colors.length],
    delay: Math.random() * 0.5,
    size: Math.random() * 6 + 4,
  }));

  timer = setTimeout(() => {
    particles.value = [];
    emit('complete');
  }, dur);
}

onMounted(() => {
  // props not available directly, but parent controls via :active
});

onUnmounted(() => {
  if (timer) clearTimeout(timer);
});

defineExpose({ start });
</script>

<template>
  <div v-if="active || particles.length > 0" class="confetti-container" aria-hidden="true">
    <div
      v-for="p in particles"
      :key="p.id"
      class="confetti-piece"
      :style="{
        left: `${p.x}%`,
        animationDelay: `${p.delay}s`,
        backgroundColor: p.color,
        width: `${p.size}px`,
        height: `${p.size}px`,
      }"
    ></div>
  </div>
</template>

<style scoped>
.confetti-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 9999;
  overflow: hidden;
}

.confetti-piece {
  position: absolute;
  top: -10px;
  border-radius: 2px;
  animation: confetti-fall 3s ease-out forwards;
}

@keyframes confetti-fall {
  0% {
    transform: translateY(0) rotate(0deg);
    opacity: 1;
  }
  100% {
    transform: translateY(100vh) rotate(720deg);
    opacity: 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .confetti-piece {
    animation: none;
    opacity: 0;
  }
}
</style>
