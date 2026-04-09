<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink, RouterView } from 'vue-router';
import ToastContainer from './components/ToastContainer.vue';
import { useTheme } from './composables/useTheme';

const { theme, toggleTheme } = useTheme();

const sidebarOpen = ref(false);
function toggleSidebar() {
  sidebarOpen.value = !sidebarOpen.value;
}
</script>

<template>
  <div class="page">
    <a href="#main-content" class="skip-to-content">Skip to content</a>
    <div class="sidebar">
      <div class="top-row ps-3 navbar navbar-dark">
        <div class="container-fluid">
          <RouterLink class="navbar-brand" to="/">🏠 Terranes</RouterLink>
          <button class="navbar-toggler" type="button" aria-label="Toggle navigation" @click="toggleSidebar">
            <span class="navbar-toggler-icon"></span>
          </button>
        </div>
      </div>

      <div class="nav-scrollable" :class="{ open: sidebarOpen }" @click="sidebarOpen = false">
        <nav class="nav flex-column" aria-label="Main navigation">
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/" exact-active-class="active">
              <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </RouterLink>
          </div>
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/villages" active-class="active">
              <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Villages
            </RouterLink>
          </div>
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/home-models" active-class="active">
              <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Home Designs
            </RouterLink>
          </div>
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/land" active-class="active">
              <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Land Blocks
            </RouterLink>
          </div>
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/marketplace" active-class="active">
              <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Marketplace
            </RouterLink>
          </div>
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/journey" active-class="active">
              <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> My Journey
            </RouterLink>
          </div>
          <div class="nav-item px-3">
            <RouterLink class="nav-link" to="/dashboard" active-class="active">
              <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Dashboard
            </RouterLink>
          </div>
          <div class="nav-item px-3 mt-auto">
            <button
              class="nav-link theme-toggle-btn w-100 text-start"
              aria-label="Toggle dark mode"
              @click.stop="toggleTheme"
            >
              <span aria-hidden="true">{{ theme === 'dark' ? '☀️' : '🌙' }}</span>
              {{ theme === 'dark' ? 'Light Mode' : 'Dark Mode' }}
            </button>
          </div>
        </nav>
      </div>
    </div>

    <main id="main-content">
      <div class="top-row px-4">
        <a href="https://learn.microsoft.com/aspnet/core/" target="_blank">About</a>
      </div>
      <article class="content px-4">
        <RouterView v-slot="{ Component }">
          <Transition name="fade" mode="out-in">
            <component :is="Component" />
          </Transition>
        </RouterView>
      </article>
    </main>

    <ToastContainer />
  </div>
</template>
