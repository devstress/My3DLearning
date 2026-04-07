import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { HomeModel } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getHomeModels: vi.fn(),
  },
}));

import { api } from '../api/client';
import HomeModelsView from '../views/HomeModelsView.vue';

const mockModels: HomeModel[] = [
  {
    id: '1', name: 'Modern Villa', description: 'A modern villa',
    bedrooms: 4, bathrooms: 2, garageSpaces: 2, floorAreaSqm: 250,
    format: 'Gltf', fileSizeMb: 15.5, createdUtc: '2026-01-01T00:00:00Z',
  },
  {
    id: '2', name: 'Compact Home', description: 'Small and efficient',
    bedrooms: 2, bathrooms: 1, garageSpaces: 1, floorAreaSqm: 120,
    format: 'Glb', fileSizeMb: 8.2, createdUtc: '2026-02-01T00:00:00Z',
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

describe('HomeModelsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(api.getHomeModels).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(HomeModelsView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.text()).toContain('Loading home designs...');
  });

  it('displays home model cards after data loads', async () => {
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(HomeModelsView, { global: { plugins: [router] } });
    await flushPromises();
    const cards = wrapper.findAll('.card');
    expect(cards.length).toBe(2);
    expect(wrapper.text()).toContain('Modern Villa');
    expect(wrapper.text()).toContain('Compact Home');
  });

  it('shows model details (bedrooms, bathrooms, format)', async () => {
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(HomeModelsView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('4');
    expect(wrapper.text()).toContain('2');
    expect(wrapper.text()).toContain('Gltf');
  });

  it('opens detail modal on "View Details" click', async () => {
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(HomeModelsView, { global: { plugins: [router] } });
    await flushPromises();
    const viewBtn = wrapper.findAll('button').find((b) => b.text().includes('View Details'));
    expect(viewBtn).toBeTruthy();
    await viewBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.find('.modal-title').text()).toBe('Modern Villa');
  });

  it('closes modal on close button click', async () => {
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(HomeModelsView, { global: { plugins: [router] } });
    await flushPromises();
    const viewBtn = wrapper.findAll('button').find((b) => b.text().includes('View Details'));
    await viewBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
    await wrapper.find('.btn-close').trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(false);
  });

  it('shows empty state when no models match', async () => {
    vi.mocked(api.getHomeModels).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(HomeModelsView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('No home designs found');
  });
});
