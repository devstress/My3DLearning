import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import SparklineChart from '../../components/SparklineChart.vue';

describe('SparklineChart', () => {
  it('renders SVG element', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2, 3, 4, 5] },
    });
    expect(wrapper.find('svg').exists()).toBe(true);
  });

  it('renders polyline with data', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2, 3, 4, 5] },
    });
    expect(wrapper.find('polyline').exists()).toBe(true);
    expect(wrapper.find('polyline').attributes('points')).toBeTruthy();
  });

  it('renders fill polygon', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2, 3] },
    });
    expect(wrapper.find('polygon').exists()).toBe(true);
  });

  it('applies custom color', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2, 3], color: '#ff0000' },
    });
    expect(wrapper.find('polyline').attributes('stroke')).toBe('#ff0000');
  });

  it('applies custom dimensions', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2], width: 300, height: 100 },
    });
    const svg = wrapper.find('svg');
    expect(svg.attributes('width')).toBe('300');
    expect(svg.attributes('height')).toBe('100');
  });

  it('has accessible role and aria-label', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [1, 2, 3] },
    });
    const svg = wrapper.find('svg');
    expect(svg.attributes('role')).toBe('img');
    expect(svg.attributes('aria-label')).toBe('Sparkline chart');
  });

  it('handles single data point gracefully', () => {
    const wrapper = mount(SparklineChart, {
      props: { data: [5] },
    });
    expect(wrapper.find('polyline').exists()).toBe(false);
  });
});
