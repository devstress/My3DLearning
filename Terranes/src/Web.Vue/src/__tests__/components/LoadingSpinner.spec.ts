import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import LoadingSpinner from '../../components/LoadingSpinner.vue';

describe('LoadingSpinner', () => {
  it('renders default "Loading..." message', () => {
    const wrapper = mount(LoadingSpinner);
    expect(wrapper.text()).toBe('Loading...');
  });

  it('renders custom message when provided', () => {
    const wrapper = mount(LoadingSpinner, { props: { message: 'Fetching data...' } });
    expect(wrapper.text()).toBe('Fetching data...');
  });
});
