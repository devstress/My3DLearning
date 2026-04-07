import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import SkeletonCard from '../../components/SkeletonCard.vue';

describe('SkeletonCard', () => {
  it('renders default 3 skeleton cards', () => {
    const wrapper = mount(SkeletonCard);
    expect(wrapper.findAll('.card')).toHaveLength(3);
  });

  it('renders custom count of skeleton cards', () => {
    const wrapper = mount(SkeletonCard, { props: { count: 6, columns: 3 } });
    expect(wrapper.findAll('.card')).toHaveLength(6);
  });

  it('applies correct column class based on columns prop', () => {
    const wrapper = mount(SkeletonCard, { props: { count: 2, columns: 2 } });
    const cols = wrapper.findAll('[class*="col-md-"]');
    expect(cols[0].classes()).toContain('col-md-6');
  });

  it('has placeholder-glow class for animation', () => {
    const wrapper = mount(SkeletonCard);
    expect(wrapper.find('.placeholder-glow').exists()).toBe(true);
  });

  it('is hidden from screen readers with aria-hidden', () => {
    const wrapper = mount(SkeletonCard);
    expect(wrapper.find('.card').attributes('aria-hidden')).toBe('true');
  });
});
