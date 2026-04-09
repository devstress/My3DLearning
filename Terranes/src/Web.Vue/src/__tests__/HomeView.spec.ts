import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';

async function createTestRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/villages', component: { template: '<div />' } },
      { path: '/home-models', component: { template: '<div />' } },
      { path: '/land', component: { template: '<div />' } },
      { path: '/marketplace', component: { template: '<div />' } },
      { path: '/journey', component: { template: '<div />' } },
      { path: '/dashboard', component: { template: '<div />' } },
    ],
  });
  await router.push('/');
  await router.isReady();
  return router;
}

describe('HomeView', () => {
  it('renders page title "Welcome to Terranes"', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain('Welcome to Terranes');
  });

  it('shows all 6 feature cards', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const cards = wrapper.findAll('.card');
    expect(cards.length).toBe(6);
  });

  it('has correct router-link to /villages', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const links = wrapper.findAll('a');
    const hrefs = links.map((l) => l.attributes('href'));
    expect(hrefs).toContain('/villages');
  });

  it('has correct router-link to /home-models', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const links = wrapper.findAll('a');
    const hrefs = links.map((l) => l.attributes('href'));
    expect(hrefs).toContain('/home-models');
  });

  it('has correct router-link paths for all 6 destinations', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const links = wrapper.findAll('a');
    const hrefs = links.map((l) => l.attributes('href'));
    expect(hrefs).toContain('/villages');
    expect(hrefs).toContain('/home-models');
    expect(hrefs).toContain('/land');
    expect(hrefs).toContain('/marketplace');
    expect(hrefs).toContain('/journey');
    expect(hrefs).toContain('/dashboard');
  });

  it('renders card titles', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const text = wrapper.text();
    expect(text).toContain('Virtual Villages');
    expect(text).toContain('Home Designs');
    expect(text).toContain('Find Land');
    expect(text).toContain('Marketplace');
    expect(text).toContain('Start Your Journey');
    expect(text).toContain('Dashboard');
  });

  it('renders hero section with animated gradient', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const hero = wrapper.find('#hero');
    expect(hero.exists()).toBe(true);
    expect(hero.classes()).toContain('hero');
    expect(hero.text()).toContain('Welcome to Terranes');
  });

  it('shows how-it-works section with 4 steps', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const section = wrapper.find('#how-it-works');
    expect(section.exists()).toBe(true);
    expect(section.text()).toContain('Browse');
    expect(section.text()).toContain('Select');
    expect(section.text()).toContain('Customise');
    expect(section.text()).toContain('Quote');
    const steps = section.findAll('.col-12.col-md-3');
    expect(steps.length).toBe(4);
  });

  it('has testimonial carousel with prev/next buttons', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const testimonials = wrapper.find('.testimonials');
    expect(testimonials.exists()).toBe(true);
    const buttons = testimonials.findAll('button');
    const prevBtn = buttons.find((b) => b.text().includes('Prev'));
    const nextBtn = buttons.find((b) => b.text().includes('Next'));
    expect(prevBtn).toBeTruthy();
    expect(nextBtn).toBeTruthy();
  });

  it('renders footer with copyright', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const footer = wrapper.find('.site-footer');
    expect(footer.exists()).toBe(true);
    expect(footer.text()).toContain('Terranes');
    expect(footer.text()).toContain('All rights reserved');
  });
});
