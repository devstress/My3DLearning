import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import type { AggregatedQuote } from '../../types';
import QuoteSummary from '../../components/QuoteSummary.vue';

const mockQuote: AggregatedQuote = {
  id: 'q1',
  journeyId: 'j1',
  totalAmountAud: 450000,
  lineItems: [
    { id: 'li1', quoteRequestId: 'qr1', category: 'Construction', description: 'Base build', amountAud: 350000 },
    { id: 'li2', quoteRequestId: 'qr1', category: 'Landscaping', description: 'Garden design', amountAud: 100000 },
  ],
  generatedUtc: '2026-03-15T10:00:00Z',
};

describe('QuoteSummary', () => {
  it('shows loading spinner when loading', () => {
    const wrapper = mount(QuoteSummary, {
      props: { quote: null, loading: true },
    });
    expect(wrapper.text()).toContain('Loading');
  });

  it('shows "No quote available" when null and not loading', () => {
    const wrapper = mount(QuoteSummary, {
      props: { quote: null, loading: false },
    });
    expect(wrapper.text()).toContain('No quote available');
  });

  it('shows total and line items when quote provided', () => {
    const wrapper = mount(QuoteSummary, {
      props: { quote: mockQuote, loading: false },
    });
    expect(wrapper.text()).toContain('450,000');
    expect(wrapper.text()).toContain('Construction');
    expect(wrapper.text()).toContain('Base build');
    expect(wrapper.text()).toContain('Landscaping');
    expect(wrapper.text()).toContain('Garden design');
  });
});
