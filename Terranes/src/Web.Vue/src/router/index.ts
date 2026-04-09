import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
      meta: { title: 'Home | Terranes', breadcrumb: 'Home' },
    },
    {
      path: '/villages',
      name: 'villages',
      component: () => import('../views/VillagesView.vue'),
      meta: { title: 'Villages | Terranes', breadcrumb: 'Villages' },
    },
    {
      path: '/home-models',
      name: 'home-models',
      component: () => import('../views/HomeModelsView.vue'),
      meta: { title: 'Home Designs | Terranes', breadcrumb: 'Home Designs' },
    },
    {
      path: '/land',
      name: 'land',
      component: () => import('../views/LandBlocksView.vue'),
      meta: { title: 'Land Blocks | Terranes', breadcrumb: 'Land Blocks' },
    },
    {
      path: '/marketplace',
      name: 'marketplace',
      component: () => import('../views/MarketplaceView.vue'),
      meta: { title: 'Marketplace | Terranes', breadcrumb: 'Marketplace' },
    },
    {
      path: '/journey',
      name: 'journey',
      component: () => import('../views/JourneyView.vue'),
      meta: { title: 'My Journey | Terranes', breadcrumb: 'My Journey' },
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('../views/DashboardView.vue'),
      meta: { title: 'Dashboard | Terranes', breadcrumb: 'Dashboard' },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('../views/NotFoundView.vue'),
      meta: { title: 'Page Not Found | Terranes', breadcrumb: 'Not Found' },
    },
  ],
});

export default router;
