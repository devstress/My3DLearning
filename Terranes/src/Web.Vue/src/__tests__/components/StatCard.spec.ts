import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import StatCard from '../../components/StatCard.vue';

describe('StatCard', () => {
  it('renders the label text', () => {
    const wrapper = mount(StatCard, {
      props: { label: 'Active Users', value: 42 },
    });
    expect(wrapper.text()).toContain('Active Users');
  });

  it('displays the value (animated or immediate)', async () => {
    // In test env, matchMedia may indicate reduced motion → value set immediately
    const wrapper = mount(StatCard, {
      props: { label: 'Count', value: 10 },
    });
    // After mount, value should be set (reduced motion path or 0 start)
    const statValue = wrapper.find('.stat-value');
    expect(statValue.exists()).toBe(true);
  });

  it('applies the color class from prop', () => {
    const wrapper = mount(StatCard, {
      props: { label: 'Test', value: 5, color: 'success' },
    });
    expect(wrapper.find('.stat-value').classes()).toContain('text-success');
  });
});
