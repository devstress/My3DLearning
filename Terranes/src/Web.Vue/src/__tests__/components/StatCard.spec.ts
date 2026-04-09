import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick } from 'vue';
import StatCard from '../../components/StatCard.vue';

describe('StatCard', () => {
  it('renders label text', () => {
    const wrapper = mount(StatCard, {
      props: { value: 42, label: 'Active Journeys' },
    });
    expect(wrapper.text()).toContain('Active Journeys');
  });

  it('renders icon', () => {
    const wrapper = mount(StatCard, {
      props: { value: 10, label: 'Test', icon: '🚀' },
    });
    expect(wrapper.find('.stat-icon-emoji').text()).toBe('🚀');
  });

  it('applies color class', () => {
    const wrapper = mount(StatCard, {
      props: { value: 5, label: 'Test', color: 'success' },
    });
    expect(wrapper.find('.stat-value').classes()).toContain('text-success');
  });

  it('has card-hover-lift class', () => {
    const wrapper = mount(StatCard, {
      props: { value: 0, label: 'Test' },
    });
    expect(wrapper.find('.card-hover-lift').exists()).toBe(true);
  });

  it('displays value after mount', async () => {
    const wrapper = mount(StatCard, {
      props: { value: 100, label: 'Test', animate: false },
    });
    await nextTick();
    await nextTick();
    expect(wrapper.find('.stat-value').text()).toBe('100');
  });

  it('renders stat-value element', () => {
    const wrapper = mount(StatCard, {
      props: { value: 50, label: 'Test' },
    });
    expect(wrapper.find('.stat-value').exists()).toBe(true);
  });
});
