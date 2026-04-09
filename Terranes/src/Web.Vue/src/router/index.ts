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
      path: '/search',
      name: 'search',
      component: () => import('../views/SearchView.vue'),
      meta: { title: 'Search | Terranes', breadcrumb: 'Search' },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: { title: 'Login | Terranes', breadcrumb: 'Login' },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('../views/RegisterView.vue'),
      meta: { title: 'Register | Terranes', breadcrumb: 'Register' },
    },
    {
      path: '/partners',
      name: 'partners',
      component: () => import('../views/PartnersView.vue'),
      meta: { title: 'Partners | Terranes', breadcrumb: 'Partners' },
    },
    {
      path: '/walkthroughs',
      name: 'walkthroughs',
      component: () => import('../views/WalkthroughsView.vue'),
      meta: { title: 'Walkthroughs | Terranes', breadcrumb: 'Walkthroughs' },
    },
    {
      path: '/design-editor',
      name: 'design-editor',
      component: () => import('../views/DesignEditorView.vue'),
      meta: { title: 'Design Editor | Terranes', breadcrumb: 'Design Editor' },
    },
    {
      path: '/reports',
      name: 'reports',
      component: () => import('../views/ReportsView.vue'),
      meta: { title: 'Reports | Terranes', breadcrumb: 'Reports' },
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
