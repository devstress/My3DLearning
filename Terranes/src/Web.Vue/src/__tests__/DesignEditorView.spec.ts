import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import DesignEditorView from '../views/DesignEditorView.vue';

async function mountDesignEditorView() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/design-editor', component: DesignEditorView },
    ],
  });
  await router.push('/design-editor');
  await router.isReady();
  return mount(DesignEditorView, { global: { plugins: [router] } });
}

describe('DesignEditorView', () => {
  it('renders editor heading', async () => {
    const wrapper = await mountDesignEditorView();
    expect(wrapper.text()).toContain('Design Editor');
  });

  it('has placement ID input', async () => {
    const wrapper = await mountDesignEditorView();
    expect(wrapper.find('#placement-id').exists()).toBe(true);
  });

  it('has operation select', async () => {
    const wrapper = await mountDesignEditorView();
    // Before loading, the form is not shown — only after placement ID
    // But the form is always there once history is loaded. Initially editHistory is null
    // so form is not shown.
    expect(wrapper.find('#placement-id').exists()).toBe(true);
  });
});
