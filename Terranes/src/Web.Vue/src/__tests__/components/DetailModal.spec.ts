import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import DetailModal from '../../components/DetailModal.vue';

describe('DetailModal', () => {
  it('does not render when show is false', () => {
    const wrapper = mount(DetailModal, { props: { show: false, title: 'Test' } });
    expect(wrapper.find('.modal').exists()).toBe(false);
  });

  it('renders modal with title and slot content when show is true', () => {
    const wrapper = mount(DetailModal, {
      props: { show: true, title: 'My Title' },
      slots: { default: '<p>Slot content here</p>' },
    });
    expect(wrapper.find('.modal').exists()).toBe(true);
    expect(wrapper.find('.modal-title').text()).toBe('My Title');
    expect(wrapper.find('.modal-body').text()).toContain('Slot content here');
  });

  it('emits close event when close button is clicked', async () => {
    const wrapper = mount(DetailModal, {
      props: { show: true, title: 'Test' },
    });
    await wrapper.find('.btn-close').trigger('click');
    expect(wrapper.emitted('close')).toBeTruthy();
    expect(wrapper.emitted('close')!.length).toBe(1);
  });
});
