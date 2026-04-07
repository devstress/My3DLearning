import { createRouter, createWebHistory } from 'vue-router';
const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'home',
            component: () => import('../views/HomeView.vue'),
        },
        {
            path: '/villages',
            name: 'villages',
            component: () => import('../views/VillagesView.vue'),
        },
        {
            path: '/home-models',
            name: 'home-models',
            component: () => import('../views/HomeModelsView.vue'),
        },
        {
            path: '/land',
            name: 'land',
            component: () => import('../views/LandBlocksView.vue'),
        },
        {
            path: '/marketplace',
            name: 'marketplace',
            component: () => import('../views/MarketplaceView.vue'),
        },
        {
            path: '/journey',
            name: 'journey',
            component: () => import('../views/JourneyView.vue'),
        },
        {
            path: '/dashboard',
            name: 'dashboard',
            component: () => import('../views/DashboardView.vue'),
        },
    ],
});
export default router;
