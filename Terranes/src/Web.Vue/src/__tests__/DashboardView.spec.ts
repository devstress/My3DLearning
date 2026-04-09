import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { BuyerJourney, Notification as AppNotification, HomeModel, PropertyListing } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getBuyerJourneys: vi.fn(),
    getNotifications: vi.fn(),
    getHomeModels: vi.fn(),
    getListings: vi.fn(),
    getAnalyticsCount: vi.fn(),
  },
}));

import { api } from '../api/client';
import DashboardView from '../views/DashboardView.vue';

const mockJourneys: BuyerJourney[] = [
  {
    id: 'j1', buyerId: '00000000-0000-0000-0000-000000000001',
    currentStage: 'Browsing', startedUtc: '2026-01-01T00:00:00Z',
    lastUpdatedUtc: '2026-01-01T00:00:00Z',
  },
];

const mockNotifications: AppNotification[] = [
  {
    id: 'n1', recipientId: '00000000-0000-0000-0000-000000000001',
    title: 'Welcome', message: 'Welcome to Terranes', isRead: false,
    createdUtc: '2026-01-01T00:00:00Z',
  },
];

const mockModels: HomeModel[] = [
  {
    id: 'm1', name: 'Modern Villa', description: 'A modern villa',
    bedrooms: 4, bathrooms: 2, garageSpaces: 2, floorAreaSqm: 250,
    format: 'Gltf', fileSizeMb: 15.5, createdUtc: '2026-01-01T00:00:00Z',
  },
];

const mockListings: PropertyListing[] = [
  {
    id: 'p1', title: 'Beach House', description: 'Nice place',
    homeModelId: 'hm1', askingPriceAud: 500000, status: 'Active',
    listedUtc: '2026-01-01T00:00:00Z',
  },
];

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

function setupMocks() {
  vi.mocked(api.getBuyerJourneys).mockResolvedValue(mockJourneys);
  vi.mocked(api.getNotifications).mockResolvedValue(mockNotifications);
  vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
  vi.mocked(api.getListings).mockResolvedValue(mockListings);
  vi.mocked(api.getAnalyticsCount).mockResolvedValue(42);
}

describe('DashboardView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially for cards', () => {
    vi.mocked(api.getBuyerJourneys).mockReturnValue(new Promise(() => {}));
    vi.mocked(api.getNotifications).mockReturnValue(new Promise(() => {}));
    vi.mocked(api.getHomeModels).mockReturnValue(new Promise(() => {}));
    vi.mocked(api.getListings).mockReturnValue(new Promise(() => {}));
    vi.mocked(api.getAnalyticsCount).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(DashboardView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.text()).toContain('Loading...');
  });

  it('displays stat cards with correct counts after data loads', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('1');
    expect(wrapper.text()).toContain('Active Journeys');
    expect(wrapper.text()).toContain('Home Designs');
    expect(wrapper.text()).toContain('Marketplace Listings');
    expect(wrapper.text()).toContain('42');
  });

  it('lists active buyer journeys', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Active Buyer Journeys');
    expect(wrapper.text()).toContain('j1');
    expect(wrapper.text()).toContain('Browsing');
  });

  it('lists recent notifications', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Recent Notifications');
    expect(wrapper.text()).toContain('Welcome');
    expect(wrapper.text()).toContain('Unread');
  });

  it('shows recent home design cards', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Recent Home Designs');
    expect(wrapper.text()).toContain('Modern Villa');
    expect(wrapper.text()).toContain('4 bed');
  });

  it('renders StatCard components with values', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    const statCards = wrapper.findAll('.stat-card');
    expect(statCards.length).toBe(4);
    expect(wrapper.find('.stat-label').exists()).toBe(true);
  });

  it('shows quick action buttons', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    const quickActions = wrapper.findAll('.quick-action');
    expect(quickActions.length).toBe(2);
    expect(wrapper.text()).toContain('New Journey');
    expect(wrapper.text()).toContain('Browse Designs');
  });

  it('shows notification count badge for unread notifications', async () => {
    setupMocks();
    const router = await createTestRouter();
    const wrapper = mount(DashboardView, { global: { plugins: [router] } });
    await flushPromises();
    const badge = wrapper.find('.notification-count');
    expect(badge.exists()).toBe(true);
    expect(badge.text()).toBe('1');
  });
});
