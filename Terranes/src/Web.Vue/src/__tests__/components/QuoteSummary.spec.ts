import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import QuoteSummary from '../../components/QuoteSummary.vue';

describe('QuoteSummary', () => {
  it('renders total journeys count', () => {
    const wrapper = mount(QuoteSummary, {
      props: { totalJourneys: 10, completedJourneys: 5, pendingQuotes: 2 },
    });
    expect(wrapper.text()).toContain('10');
    expect(wrapper.text()).toContain('Total Journeys');
  });

  it('renders completed and pending counts', () => {
    const wrapper = mount(QuoteSummary, {
      props: { totalJourneys: 10, completedJourneys: 5, pendingQuotes: 2 },
    });
    expect(wrapper.text()).toContain('5');
    expect(wrapper.text()).toContain('Completed');
    expect(wrapper.text()).toContain('2');
    expect(wrapper.text()).toContain('Pending Quotes');
  });

  it('shows completion rate percentage', () => {
    const wrapper = mount(QuoteSummary, {
      props: { totalJourneys: 10, completedJourneys: 5, pendingQuotes: 0 },
    });
    expect(wrapper.text()).toContain('50%');
  });

  it('shows 0% when no journeys', () => {
    const wrapper = mount(QuoteSummary, {
      props: { totalJourneys: 0, completedJourneys: 0, pendingQuotes: 0 },
    });
    expect(wrapper.text()).toContain('0%');
  });

  it('renders progress bar', () => {
    const wrapper = mount(QuoteSummary, {
      props: { totalJourneys: 10, completedJourneys: 7, pendingQuotes: 1 },
    });
    expect(wrapper.find('.progress-bar').exists()).toBe(true);
  });
});
