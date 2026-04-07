import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ActionButton from '../../components/ActionButton.vue';

describe('ActionButton', () => {
  it('renders slot content when not loading', () => {
    const wrapper = mount(ActionButton, {
      slots: { default: 'Click Me' },
    });
    expect(wrapper.text()).toBe('Click Me');
    expect(wrapper.find('.spinner-border').exists()).toBe(false);
  });

  it('shows spinner and loading text when loading', () => {
    const wrapper = mount(ActionButton, {
      props: { loading: true, loadingText: 'Saving...' },
      slots: { default: 'Click Me' },
    });
    expect(wrapper.find('.spinner-border').exists()).toBe(true);
    expect(wrapper.text()).toContain('Saving...');
    expect(wrapper.text()).not.toContain('Click Me');
  });

  it('is disabled when loading', () => {
    const wrapper = mount(ActionButton, {
      props: { loading: true },
      slots: { default: 'Click Me' },
    });
    expect(wrapper.find('button').attributes('disabled')).toBeDefined();
  });

  it('is disabled when disabled prop is true', () => {
    const wrapper = mount(ActionButton, {
      props: { disabled: true },
      slots: { default: 'Click Me' },
    });
    expect(wrapper.find('button').attributes('disabled')).toBeDefined();
  });

  it('applies variant class', () => {
    const wrapper = mount(ActionButton, {
      props: { variant: 'success' },
      slots: { default: 'Done' },
    });
    expect(wrapper.find('button').classes()).toContain('btn-success');
  });

  it('applies size class', () => {
    const wrapper = mount(ActionButton, {
      props: { size: 'lg' },
      slots: { default: 'Big' },
    });
    expect(wrapper.find('button').classes()).toContain('btn-lg');
  });

  it('emits click event when clicked', async () => {
    const wrapper = mount(ActionButton, {
      slots: { default: 'Click Me' },
    });
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('click')).toHaveLength(1);
  });

  it('has aria-busy when loading', () => {
    const wrapper = mount(ActionButton, {
      props: { loading: true },
      slots: { default: 'Click Me' },
    });
    expect(wrapper.find('button').attributes('aria-busy')).toBe('true');
  });
});
