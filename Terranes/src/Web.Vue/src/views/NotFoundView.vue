<script setup lang="ts">
</script>

<template>
  <div class="container text-center py-5">
    <div class="mb-4">
      <svg xmlns="http://www.w3.org/2000/svg" width="120" height="120" fill="currentColor" class="text-muted opacity-50" viewBox="0 0 16 16" aria-hidden="true">
        <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
        <path d="M7.002 11a1 1 0 1 1 2 0 1 1 0 0 1-2 0zM7.1 4.995a.905.905 0 1 1 1.8 0l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 4.995z"/>
      </svg>
    </div>
    <h1 class="display-4 fw-bold text-muted">404</h1>
    <h2 class="mb-3">Page Not Found</h2>
    <p class="text-muted mb-4">
      The page you're looking for doesn't exist or has been moved.
    </p>
    <RouterLink to="/" class="btn btn-primary btn-lg">
      🏠 Go to Home
    </RouterLink>
  </div>
</template>
