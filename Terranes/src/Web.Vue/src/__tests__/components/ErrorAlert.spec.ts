import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ErrorAlert from '../../components/ErrorAlert.vue';

describe('ErrorAlert', () => {
  it('does not render when message is null', () => {
    const wrapper = mount(ErrorAlert, { props: { message: null } });
    expect(wrapper.find('.alert').exists()).toBe(false);
  });

  it('shows error message when provided', () => {
    const wrapper = mount(ErrorAlert, { props: { message: 'Something went wrong' } });
    expect(wrapper.find('.alert-danger').exists()).toBe(true);
    expect(wrapper.text()).toBe('Something went wrong');
  });
});
