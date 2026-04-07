import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import SkeletonTable from '../../components/SkeletonTable.vue';

describe('SkeletonTable', () => {
  it('renders default 5 rows and 4 columns', () => {
    const wrapper = mount(SkeletonTable);
    expect(wrapper.findAll('tbody tr')).toHaveLength(5);
    expect(wrapper.findAll('thead th')).toHaveLength(4);
  });

  it('renders custom rows and cols', () => {
    const wrapper = mount(SkeletonTable, { props: { rows: 3, cols: 6 } });
    expect(wrapper.findAll('tbody tr')).toHaveLength(3);
    expect(wrapper.findAll('thead th')).toHaveLength(6);
  });

  it('has placeholder-glow class for animation', () => {
    const wrapper = mount(SkeletonTable);
    expect(wrapper.find('.placeholder-glow').exists()).toBe(true);
  });
});
