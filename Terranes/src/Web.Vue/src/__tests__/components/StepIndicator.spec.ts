import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import StepIndicator from '../../components/StepIndicator.vue';

const stages = ['Browsing', 'DesignSelected', 'PlacedOnLand', 'Completed'];

describe('StepIndicator', () => {
  it('renders all stages', () => {
    const wrapper = mount(StepIndicator, {
      props: { stages, currentStage: 'PlacedOnLand' },
    });
    expect(wrapper.findAll('.step-item').length).toBe(4);
  });

  it('marks completed stages with ✓', () => {
    const wrapper = mount(StepIndicator, {
      props: { stages, currentStage: 'PlacedOnLand' },
    });
    const completed = wrapper.findAll('.step-item.completed');
    expect(completed.length).toBe(2); // Browsing and DesignSelected
    expect(completed[0].text()).toContain('✓');
  });

  it('highlights current stage as active', () => {
    const wrapper = mount(StepIndicator, {
      props: { stages, currentStage: 'PlacedOnLand' },
    });
    const active = wrapper.findAll('.step-item.active');
    expect(active.length).toBe(1);
    expect(active[0].text()).toContain('3');
  });
});
