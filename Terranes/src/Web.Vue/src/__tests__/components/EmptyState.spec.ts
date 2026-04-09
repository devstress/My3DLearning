import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import EmptyState from '../../components/EmptyState.vue';

describe('EmptyState', () => {
  it('renders default title and message', () => {
    const wrapper = mount(EmptyState);
    expect(wrapper.text()).toContain('No results found');
    expect(wrapper.text()).toContain('Try adjusting your search or filters.');
  });

  it('renders custom title and message', () => {
    const wrapper = mount(EmptyState, {
      props: { title: 'No villages', message: 'Create one!' },
    });
    expect(wrapper.text()).toContain('No villages');
    expect(wrapper.text()).toContain('Create one!');
  });

  it('renders SVG icon', () => {
    const wrapper = mount(EmptyState, { props: { icon: 'village' } });
    expect(wrapper.find('svg').exists()).toBe(true);
    expect(wrapper.find('svg').attributes('aria-hidden')).toBe('true');
  });

  it('has empty-state class for styling', () => {
    const wrapper = mount(EmptyState);
    expect(wrapper.find('.empty-state').exists()).toBe(true);
  });

  it('renders different icon paths for each type', () => {
    const searchWrapper = mount(EmptyState, { props: { icon: 'search' } });
    const landWrapper = mount(EmptyState, { props: { icon: 'land' } });
    const searchPath = searchWrapper.find('path').attributes('d');
    const landPath = landWrapper.find('path').attributes('d');
    expect(searchPath).not.toBe(landPath);
  });
});
