import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import StepIndicator from '../../components/StepIndicator.vue';

const steps = ['Browsing', 'DesignSelected', 'PlacedOnLand', 'Customising', 'Completed'];

describe('StepIndicator', () => {
  it('renders all steps', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'Browsing' },
    });
    expect(wrapper.findAll('.step-item').length).toBe(5);
  });

  it('marks current step as active', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'PlacedOnLand' },
    });
    const activeItems = wrapper.findAll('.step-active');
    expect(activeItems.length).toBe(1);
    expect(activeItems[0].find('.step-label').text()).toBe('PlacedOnLand');
  });

  it('marks previous steps as completed with checkmark', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'Customising' },
    });
    const completedItems = wrapper.findAll('.step-completed');
    expect(completedItems.length).toBe(3); // Browsing, DesignSelected, PlacedOnLand
    expect(completedItems[0].find('.step-circle').text()).toBe('✓');
  });

  it('marks future steps as pending', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'Browsing' },
    });
    const pendingItems = wrapper.findAll('.step-pending');
    expect(pendingItems.length).toBe(4);
  });

  it('renders connectors between steps', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'Browsing' },
    });
    expect(wrapper.findAll('.step-connector').length).toBe(4);
  });

  it('marks connector as active for completed transitions', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'PlacedOnLand' },
    });
    const activeConnectors = wrapper.findAll('.step-connector-active');
    expect(activeConnectors.length).toBe(2);
  });

  it('has role="group" and aria-label', () => {
    const wrapper = mount(StepIndicator, {
      props: { steps, currentStep: 'Browsing' },
    });
    const container = wrapper.find('.step-indicator');
    expect(container.attributes('role')).toBe('group');
    expect(container.attributes('aria-label')).toBe('Journey progress steps');
  });
});
