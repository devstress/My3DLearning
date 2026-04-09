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
    // 6 feature cards + 1 testimonial card = 7
    expect(cards.length).toBeGreaterThanOrEqual(6);
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
    const hero = wrapper.find('.hero-section');
    expect(hero.exists()).toBe(true);
    expect(hero.text()).toContain('Welcome to Terranes');
  });

  it('renders "How It Works" 4-step flow', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain('How It Works');
    const steps = wrapper.findAll('.how-step');
    expect(steps.length).toBe(4);
    expect(wrapper.text()).toContain('Explore Villages');
    expect(wrapper.text()).toContain('Choose a Design');
    expect(wrapper.text()).toContain('Test-Fit on Land');
    expect(wrapper.text()).toContain('Get a Quote');
  });

  it('renders testimonial carousel with navigation', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain('What Our Users Say');
    const carousel = wrapper.find('.testimonial-carousel');
    expect(carousel.exists()).toBe(true);
    // Check first testimonial visible
    expect(wrapper.text()).toContain('Sarah M.');

    // Click next and verify rotation
    const nextBtn = wrapper.find('[aria-label="Next testimonial"]');
    await nextBtn.trigger('click');
    expect(wrapper.text()).toContain('James T.');
  });

  it('renders footer with links', async () => {
    const router = await createTestRouter();
    const wrapper = mount(HomeView, { global: { plugins: [router] } });
    const footer = wrapper.find('footer');
    expect(footer.exists()).toBe(true);
    expect(footer.text()).toContain('Platform');
    expect(footer.text()).toContain('Services');
    expect(footer.text()).toContain('About');
    expect(footer.text()).toContain('© 2026 Terranes');
  });
});

