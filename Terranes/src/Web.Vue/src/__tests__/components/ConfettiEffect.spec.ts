import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ConfettiEffect from '../../components/ConfettiEffect.vue';

describe('ConfettiEffect', () => {
  it('does not render when inactive with no particles', () => {
    const wrapper = mount(ConfettiEffect, {
      props: { active: false },
    });
    expect(wrapper.find('.confetti-container').exists()).toBe(false);
  });

  it('renders container when active', () => {
    const wrapper = mount(ConfettiEffect, {
      props: { active: true },
    });
    expect(wrapper.find('.confetti-container').exists()).toBe(true);
  });

  it('container is aria-hidden', () => {
    const wrapper = mount(ConfettiEffect, {
      props: { active: true },
    });
    expect(wrapper.find('.confetti-container').attributes('aria-hidden')).toBe('true');
  });

  it('creates particles when start is called', async () => {
    const wrapper = mount(ConfettiEffect, {
      props: { active: true },
    });
    (wrapper.vm as unknown as { start: (count: number, duration: number) => void }).start(10, 1000);
    await wrapper.vm.$nextTick();
    expect(wrapper.findAll('.confetti-piece').length).toBe(10);
  });
});
