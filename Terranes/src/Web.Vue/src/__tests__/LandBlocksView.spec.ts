import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { LandBlock, HomeModel, SitePlacement } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getLandBlocks: vi.fn(),
    getHomeModels: vi.fn(),
    createSitePlacement: vi.fn(),
  },
}));

import { api } from '../api/client';
import LandBlocksView from '../views/LandBlocksView.vue';

const mockBlocks: LandBlock[] = [
  {
    id: 'b1', address: '10 Main St', suburb: 'Surry Hills', state: 'NSW',
    areaSqm: 450, frontageMetre: 15.0, depthMetre: 30.0, zoning: 'R2',
  },
  {
    id: 'b2', address: '5 Park Ave', suburb: 'Bondi', state: 'NSW',
    areaSqm: 600, frontageMetre: 20.0, depthMetre: 30.0, zoning: 'R3',
  },
];

const mockModels: HomeModel[] = [
  {
    id: 'm1', name: 'Modern Villa', description: 'A modern villa',
    bedrooms: 4, bathrooms: 2, garageSpaces: 2, floorAreaSqm: 250,
    format: 'Gltf', fileSizeMb: 15.5, createdUtc: '2026-01-01T00:00:00Z',
  },
];

const mockPlacement: SitePlacement = {
  id: 'sp1', landBlockId: 'b1', homeModelId: 'm1',
  offsetX: 5.0, offsetY: 3.0, rotationDegrees: 0, scaleFactor: 1.0,
  placedUtc: '2026-03-01T00:00:00Z',
};

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('LandBlocksView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(api.getLandBlocks).mockReturnValue(new Promise(() => {}));
    const wrapper = mount(LandBlocksView, {
      global: { plugins: [createRouter({ history: createMemoryHistory(), routes: [{ path: '/', component: { template: '<div />' } }] })] },
    });
    expect(wrapper.text()).toContain('Loading land blocks...');
  });

  it('displays land blocks in a table after data loads', async () => {
    vi.mocked(api.getLandBlocks).mockResolvedValue(mockBlocks);
    const router = await createTestRouter();
    const wrapper = mount(LandBlocksView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('10 Main St');
    expect(wrapper.text()).toContain('Surry Hills');
    expect(wrapper.text()).toContain('5 Park Ave');
    expect(wrapper.text()).toContain('Bondi');
  });

  it('opens test-fit modal on "Test-Fit" click', async () => {
    vi.mocked(api.getLandBlocks).mockResolvedValue(mockBlocks);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(LandBlocksView, { global: { plugins: [router] } });
    await flushPromises();
    const testFitBtn = wrapper.findAll('button').find((b) => b.text() === 'Test-Fit');
    await testFitBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.find('.modal-title').text()).toContain('Test-Fit on 10 Main St');
  });

  it('shows placement result after test-fit succeeds', async () => {
    vi.mocked(api.getLandBlocks).mockResolvedValue(mockBlocks);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    vi.mocked(api.createSitePlacement).mockResolvedValue(mockPlacement);
    const router = await createTestRouter();
    const wrapper = mount(LandBlocksView, { global: { plugins: [router] } });
    await flushPromises();
    const testFitBtn = wrapper.findAll('button').find((b) => b.text() === 'Test-Fit');
    await testFitBtn!.trigger('click');
    await flushPromises();
    // Click the model to trigger test-fit
    const modelBtn = wrapper.findAll('.list-group-item-action').at(0);
    await modelBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Placement Created');
    expect(wrapper.text()).toContain('sp1');
  });

  it('shows error message when test-fit fails', async () => {
    vi.mocked(api.getLandBlocks).mockResolvedValue(mockBlocks);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    vi.mocked(api.createSitePlacement).mockRejectedValue(new Error('Design does not fit'));
    const router = await createTestRouter();
    const wrapper = mount(LandBlocksView, { global: { plugins: [router] } });
    await flushPromises();
    const testFitBtn = wrapper.findAll('button').find((b) => b.text() === 'Test-Fit');
    await testFitBtn!.trigger('click');
    await flushPromises();
    const modelBtn = wrapper.findAll('.list-group-item-action').at(0);
    await modelBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Design does not fit');
  });
});
