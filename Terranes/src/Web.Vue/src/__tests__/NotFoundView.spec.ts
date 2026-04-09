import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import NotFoundView from '../views/NotFoundView.vue';

async function mountNotFound() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/:pathMatch(.*)*', name: 'not-found', component: NotFoundView },
    ],
  });
  await router.push('/some-invalid-path');
  await router.isReady();
  return mount(NotFoundView, { global: { plugins: [router] } });
}

describe('NotFoundView', () => {
  it('displays 404 heading', async () => {
    const wrapper = await mountNotFound();
    expect(wrapper.text()).toContain('404');
  });

  it('displays "Page Not Found" message', async () => {
    const wrapper = await mountNotFound();
    expect(wrapper.text()).toContain('Page Not Found');
  });

  it('has a link back to home', async () => {
    const wrapper = await mountNotFound();
    const link = wrapper.find('a[href="/"]');
    expect(link.exists()).toBe(true);
    expect(link.text()).toContain('Home');
  });

  it('displays helpful description text', async () => {
    const wrapper = await mountNotFound();
    expect(wrapper.text()).toContain("doesn't exist");
  });
});
