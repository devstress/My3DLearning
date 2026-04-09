import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ConfirmDialog from '../../components/ConfirmDialog.vue';

describe('ConfirmDialog', () => {
  it('does not render when show is false', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: false, title: 'Test', message: 'Are you sure?' },
    });
    expect(wrapper.find('.modal').exists()).toBe(false);
  });

  it('renders title and message when show is true', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true, title: 'Confirm Action', message: 'This is permanent.' },
    });
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.find('.modal-title').text()).toBe('Confirm Action');
    expect(wrapper.text()).toContain('This is permanent.');
  });

  it('emits confirm when confirm button clicked', async () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true, title: 'Test', message: 'Sure?', confirmText: 'Yes' },
    });
    const confirmBtn = wrapper.findAll('button').find((b) => b.text() === 'Yes');
    await confirmBtn!.trigger('click');
    expect(wrapper.emitted('confirm')).toHaveLength(1);
  });
});
