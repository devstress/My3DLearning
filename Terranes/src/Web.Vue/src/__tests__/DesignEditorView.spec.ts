import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import type { DesignEdit } from '../types';

vi.mock('../api/client', () => ({
  api: {
    applyEdit: vi.fn(),
    getEditHistory: vi.fn(),
    undoLastEdit: vi.fn(),
  },
}));

import { api } from '../api/client';
import DesignEditorView from '../views/DesignEditorView.vue';

const mockEdits: DesignEdit[] = [
  {
    id: 'e1', sitePlacementId: 'sp1', operation: 'ColorChange',
    targetElement: 'Wall-North', newValue: '#FF5733', appliedUtc: '2026-03-01T10:00:00Z',
  },
  {
    id: 'e2', sitePlacementId: 'sp1', operation: 'Move',
    targetElement: 'Door-Front', newValue: '1.5,0,0', appliedUtc: '2026-03-01T11:00:00Z',
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

describe('DesignEditorView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows form inputs', async () => {
    const router = await createTestRouter();
    const wrapper = mount(DesignEditorView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('Site Placement ID');
    expect(wrapper.text()).toContain('Operation');
    expect(wrapper.text()).toContain('Target Element');
    expect(wrapper.text()).toContain('New Value');
    expect(wrapper.text()).toContain('Apply Edit');
  });

  it('shows empty state for edit history', async () => {
    const router = await createTestRouter();
    const wrapper = mount(DesignEditorView, { global: { plugins: [router] } });
    await flushPromises();
    expect(wrapper.text()).toContain('No edits yet');
  });

  it('shows edit history when loaded', async () => {
    vi.mocked(api.getEditHistory).mockResolvedValue(mockEdits);
    const router = await createTestRouter();
    const wrapper = mount(DesignEditorView, { global: { plugins: [router] } });
    await flushPromises();
    // Set placement ID and click Load
    const inputs = wrapper.findAll('input[type="text"]');
    const historyInput = inputs.find((i) => i.attributes('placeholder')?.includes('Placement ID to load'));
    await historyInput!.setValue('sp1');
    const loadBtn = wrapper.findAll('button').find((b) => b.text() === 'Load');
    await loadBtn!.trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('ColorChange');
    expect(wrapper.text()).toContain('Wall-North');
    expect(wrapper.text()).toContain('#FF5733');
  });
});
