import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import EmptyState from '../../components/EmptyState.vue';

describe('EmptyState', () => {
  it('renders default message when no prop given', () => {
    const wrapper = mount(EmptyState);
    expect(wrapper.text()).toContain('No results found');
    expect(wrapper.find('svg').exists()).toBe(true);
  });

  it('renders custom message', () => {
    const wrapper = mount(EmptyState, { props: { message: 'Nothing here yet!' } });
    expect(wrapper.text()).toContain('Nothing here yet!');
  });
});
