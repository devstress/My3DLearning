import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ConfirmDialog from '../../components/ConfirmDialog.vue';

describe('ConfirmDialog', () => {
  it('does not render when show is false', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: false },
    });
    expect(wrapper.find('.modal').exists()).toBe(false);
  });

  it('renders dialog when show is true', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true },
    });
    expect(wrapper.find('.modal').exists()).toBe(true);
  });

  it('shows custom title and message', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true, title: 'Delete?', message: 'Are you sure you want to delete?' },
    });
    expect(wrapper.find('.modal-title').text()).toBe('Delete?');
    expect(wrapper.text()).toContain('Are you sure you want to delete?');
  });

  it('emits confirm on confirm button click', async () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true, confirmText: 'Yes, do it' },
    });
    const confirmBtn = wrapper.findAll('button').find((b) => b.text() === 'Yes, do it');
    await confirmBtn!.trigger('click');
    expect(wrapper.emitted('confirm')).toBeTruthy();
  });

  it('emits cancel on cancel button click', async () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true },
    });
    const cancelBtn = wrapper.findAll('button').find((b) => b.text() === 'Cancel');
    await cancelBtn!.trigger('click');
    expect(wrapper.emitted('cancel')).toBeTruthy();
  });

  it('applies variant class to confirm button', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { show: true, variant: 'warning' },
    });
    const confirmBtn = wrapper.findAll('button').find((b) => b.text() === 'Confirm');
    expect(confirmBtn!.classes()).toContain('btn-warning');
  });
});
