import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import FilterChip from '../../components/FilterChip.vue';

describe('FilterChip', () => {
  it('renders label text in a badge', () => {
    const wrapper = mount(FilterChip, { props: { label: 'Type: Grid' } });
    expect(wrapper.text()).toContain('Type: Grid');
    expect(wrapper.find('.badge').exists()).toBe(true);
  });

  it('emits remove event when close button clicked', async () => {
    const wrapper = mount(FilterChip, { props: { label: 'Status: Active' } });
    await wrapper.find('.btn-close').trigger('click');
    expect(wrapper.emitted('remove')).toHaveLength(1);
  });
});
