import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
      meta: { title: 'Home', breadcrumb: 'Home' },
    },
    {
      path: '/villages',
      name: 'villages',
      component: () => import('../views/VillagesView.vue'),
      meta: { title: 'Villages', breadcrumb: 'Villages' },
    },
    {
      path: '/home-models',
      name: 'home-models',
      component: () => import('../views/HomeModelsView.vue'),
      meta: { title: 'Home Designs', breadcrumb: 'Home Designs' },
    },
    {
      path: '/land',
      name: 'land',
      component: () => import('../views/LandBlocksView.vue'),
      meta: { title: 'Land Blocks', breadcrumb: 'Land Blocks' },
    },
    {
      path: '/marketplace',
      name: 'marketplace',
      component: () => import('../views/MarketplaceView.vue'),
      meta: { title: 'Marketplace', breadcrumb: 'Marketplace' },
    },
    {
      path: '/journey',
      name: 'journey',
      component: () => import('../views/JourneyView.vue'),
      meta: { title: 'My Journey', breadcrumb: 'My Journey' },
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('../views/DashboardView.vue'),
      meta: { title: 'Dashboard', breadcrumb: 'Dashboard' },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('../views/NotFoundView.vue'),
      meta: { title: 'Page Not Found', breadcrumb: '404' },
    },
  ],
});

router.afterEach((to) => {
  const title = to.meta.title as string | undefined;
  document.title = title ? `${title} — Terranes` : 'Terranes';
});

export default router;
