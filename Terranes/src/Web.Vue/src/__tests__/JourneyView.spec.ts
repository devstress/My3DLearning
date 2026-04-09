import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { BuyerJourney, HomeModel } from '../types';

vi.mock('../api/client', () => ({
  api: {
    getBuyerJourneys: vi.fn(),
    createJourney: vi.fn(),
    getHomeModels: vi.fn(),
    getLandBlocks: vi.fn(),
    advanceJourney: vi.fn(),
    getJourney: vi.fn(),
  },
}));

import { api } from '../api/client';
import JourneyView from '../views/JourneyView.vue';

const mockModels: HomeModel[] = [
  {
    id: 'm1', name: 'Modern Villa', description: 'A modern villa',
    bedrooms: 4, bathrooms: 2, garageSpaces: 2, floorAreaSqm: 250,
    format: 'Gltf', fileSizeMb: 15.5, createdUtc: '2026-01-01T00:00:00Z',
  },
];

function makeMockJourney(stage: string): BuyerJourney {
  return {
    id: 'j1', buyerId: '00000000-0000-0000-0000-000000000001',
    currentStage: stage, startedUtc: '2026-01-01T00:00:00Z',
    lastUpdatedUtc: '2026-01-01T00:00:00Z',
  };
}

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('JourneyView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows "Begin Journey" button when no active journey', async () => {
    vi.mocked(api.getBuyerJourneys).mockResolvedValue([]);
    const router = await createTestRouter();
    const wrapper = mount(JourneyView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Begin Journey');
  });

  it('shows step indicator when journey is active', async () => {
    const browsingJourney = makeMockJourney('Browsing');
    vi.mocked(api.getBuyerJourneys).mockResolvedValue([browsingJourney]);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(JourneyView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.find('.step-indicator').exists()).toBe(true);
    expect(wrapper.findAll('.step-circle').length).toBeGreaterThan(0);
  });

  it('shows browsing stage with design cards', async () => {
    const browsingJourney = makeMockJourney('Browsing');
    vi.mocked(api.getBuyerJourneys).mockResolvedValue([browsingJourney]);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    const router = await createTestRouter();
    const wrapper = mount(JourneyView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Select a Home Design');
    expect(wrapper.text()).toContain('Modern Villa');
  });

  it('shows past journeys list', async () => {
    const completedJourney = makeMockJourney('Completed');
    vi.mocked(api.getBuyerJourneys).mockResolvedValue([completedJourney]);
    const router = await createTestRouter();
    const wrapper = mount(JourneyView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Previous Journeys');
    expect(wrapper.text()).toContain('Completed');
  });

  it('shows error message when API call fails', async () => {
    const browsingJourney = makeMockJourney('Browsing');
    vi.mocked(api.getBuyerJourneys).mockResolvedValue([browsingJourney]);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    vi.mocked(api.advanceJourney).mockRejectedValue(new Error('API failure'));
    const router = await createTestRouter();
    const wrapper = mount(JourneyView, { global: { plugins: [router] } });
    await flushPromises();
    // Click "Select Design" to trigger advanceJourney
    const selectBtn = wrapper.findAll('button').find((b) => b.text().includes('Select Design'));
    await selectBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('API failure');
  });

  it('shows completion message when journey is completed', async () => {
    const browsingJourney = makeMockJourney('Browsing');
    vi.mocked(api.getBuyerJourneys).mockResolvedValue([browsingJourney]);
    vi.mocked(api.getHomeModels).mockResolvedValue(mockModels);
    vi.mocked(api.advanceJourney).mockResolvedValue(makeMockJourney('Completed'));
    const router = await createTestRouter();
    const wrapper = mount(JourneyView, { global: { plugins: [router] } });
    await flushPromises();
    // Advance to completion by selecting a design (which triggers advanceJourney)
    const selectBtn = wrapper.findAll('button').find((b) => b.text().includes('Select Design'));
    await selectBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Journey Complete');
  });
});
