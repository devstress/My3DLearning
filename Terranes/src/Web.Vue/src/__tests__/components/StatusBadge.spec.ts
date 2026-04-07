import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import StatusBadge from '../../components/StatusBadge.vue';

describe('StatusBadge', () => {
  it('renders status text', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'Active' } });
    expect(wrapper.text()).toBe('Active');
  });

  it('applies correct class for known status', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'Active' } });
    expect(wrapper.find('.badge').classes()).toContain('bg-success');
  });

  it('falls back to bg-secondary for unknown status', () => {
    const wrapper = mount(StatusBadge, { props: { status: 'Unknown' } });
    expect(wrapper.find('.badge').classes()).toContain('bg-secondary');
  });

  it('uses custom colorMap when provided', () => {
    const wrapper = mount(StatusBadge, {
      props: { status: 'Custom', colorMap: { Custom: 'bg-danger' } },
    });
    expect(wrapper.find('.badge').classes()).toContain('bg-danger');
  });
});
