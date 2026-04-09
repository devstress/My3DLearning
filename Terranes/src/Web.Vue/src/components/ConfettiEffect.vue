<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

const visible = ref(true);
let timer: ReturnType<typeof setTimeout>;

onMounted(() => {
  timer = setTimeout(() => { visible.value = false; }, 3000);
});

onUnmounted(() => {
  clearTimeout(timer);
});

const particles = Array.from({ length: 32 }, (_, i) => ({
  id: i,
  color: ['#ff6b6b','#feca57','#48dbfb','#ff9ff3','#54a0ff','#5f27cd','#01a3a4','#f368e0'][i % 8],
  left: `${Math.random() * 100}%`,
  delay: `${Math.random() * 2}s`,
  duration: `${2 + Math.random() * 2}s`,
}));
</script>

<template>
  <div v-if="visible" class="confetti-container" aria-hidden="true">
    <div
      v-for="p in particles"
      :key="p.id"
      class="confetti-particle"
      :style="{
        backgroundColor: p.color,
        left: p.left,
        animationDelay: p.delay,
        animationDuration: p.duration,
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

.confetti-particle {
  position: absolute;
  top: -10px;
  width: 10px;
  height: 10px;
  border-radius: 2px;
  animation: confetti-fall linear forwards;
}

@keyframes confetti-fall {
  0% { transform: translateY(0) rotate(0deg); opacity: 1; }
  100% { transform: translateY(100vh) rotate(720deg); opacity: 0; }
}

@media (prefers-reduced-motion: reduce) {
  .confetti-particle {
    animation: none;
    display: none;
  }
}
</style>
