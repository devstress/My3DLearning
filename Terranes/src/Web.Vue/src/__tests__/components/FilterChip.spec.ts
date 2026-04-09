import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import FilterChip from '../../components/FilterChip.vue';

describe('FilterChip', () => {
  it('renders label and value', () => {
    const wrapper = mount(FilterChip, {
      props: { label: 'Status', value: 'Active' },
    });
    expect(wrapper.text()).toContain('Status');
    expect(wrapper.text()).toContain('Active');
  });

  it('emits remove on close button click', async () => {
    const wrapper = mount(FilterChip, {
      props: { label: 'Status', value: 'Active' },
    });
    await wrapper.find('.btn-close').trigger('click');
    expect(wrapper.emitted('remove')).toBeTruthy();
  });

  it('has correct aria-label on close button', () => {
    const wrapper = mount(FilterChip, {
      props: { label: 'Layout', value: 'Grid' },
    });
    expect(wrapper.find('.btn-close').attributes('aria-label')).toBe('Remove Layout filter');
  });

  it('uses badge styling', () => {
    const wrapper = mount(FilterChip, {
      props: { label: 'Name', value: 'Test' },
    });
    expect(wrapper.find('.badge').exists()).toBe(true);
    expect(wrapper.find('.filter-chip').exists()).toBe(true);
  });
});
