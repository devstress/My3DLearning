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
      path: '/search',
      name: 'search',
      component: () => import('../views/SearchView.vue'),
      meta: { title: 'Search', breadcrumb: 'Search' },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: { title: 'Login', breadcrumb: 'Login' },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('../views/RegisterView.vue'),
      meta: { title: 'Register', breadcrumb: 'Register' },
    },
    {
      path: '/partners',
      name: 'partners',
      component: () => import('../views/PartnersView.vue'),
      meta: { title: 'Partners', breadcrumb: 'Partners' },
    },
    {
      path: '/walkthroughs',
      name: 'walkthroughs',
      component: () => import('../views/WalkthroughsView.vue'),
      meta: { title: 'Walkthroughs', breadcrumb: 'Walkthroughs' },
    },
    {
      path: '/design-editor',
      name: 'design-editor',
      component: () => import('../views/DesignEditorView.vue'),
      meta: { title: 'Design Editor', breadcrumb: 'Design Editor' },
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('../views/ReportsView.vue'),
      meta: { title: 'Reports', breadcrumb: 'Reports' },
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
