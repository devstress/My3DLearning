import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import SparklineChart from '../../components/SparklineChart.vue';

describe('SparklineChart', () => {
  it('renders an SVG with a polyline', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [5, 10, 3, 8] },
    });
    expect(wrapper.find('svg').exists()).toBe(true);
    expect(wrapper.find('polyline').exists()).toBe(true);
  });

  it('applies custom dimensions and color', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2, 3], color: '#ff0000', width: 200, height: 60 },
    });
    const svg = wrapper.find('svg');
    expect(svg.attributes('width')).toBe('200');
    expect(svg.attributes('height')).toBe('60');
    expect(wrapper.find('polyline').attributes('stroke')).toBe('#ff0000');
  });
});
