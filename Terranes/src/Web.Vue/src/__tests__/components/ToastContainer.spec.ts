import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ToastContainer from '../../components/ToastContainer.vue';
import { useToast } from '../../composables/useToast';

describe('ToastContainer', () => {
  function clearToasts() {
    const { toasts, removeToast } = useToast();
    toasts.value.forEach((t) => removeToast(t.id));
  }

  it('renders nothing when no toasts exist', () => {
    clearToasts();
    const wrapper = mount(ToastContainer);
    expect(wrapper.findAll('.toast')).toHaveLength(0);
  });

  it('renders a success toast', () => {
    clearToasts();
    const { showSuccess } = useToast();
    showSuccess('It worked!');
    const wrapper = mount(ToastContainer);
    expect(wrapper.findAll('.toast')).toHaveLength(1);
    expect(wrapper.text()).toContain('It worked!');
    expect(wrapper.find('.toast').classes()).toContain('bg-success');
  });

  it('renders an error toast', () => {
    clearToasts();
    const { showError } = useToast();
    showError('Something broke');
    const wrapper = mount(ToastContainer);
    expect(wrapper.find('.toast').classes()).toContain('bg-danger');
    expect(wrapper.text()).toContain('Something broke');
  });

  it('removes toast when close button clicked', async () => {
    clearToasts();
    const { showSuccess, toasts } = useToast();
    showSuccess('Dismissible');
    const wrapper = mount(ToastContainer);
    expect(toasts.value).toHaveLength(1);
    await wrapper.find('.btn-close').trigger('click');
    expect(toasts.value).toHaveLength(0);
  });

  it('renders multiple toasts stacked', () => {
    clearToasts();
    const { showSuccess, showError, showInfo } = useToast();
    showSuccess('Success');
    showError('Error');
    showInfo('Info');
    const wrapper = mount(ToastContainer);
    expect(wrapper.findAll('.toast')).toHaveLength(3);
  });
});
