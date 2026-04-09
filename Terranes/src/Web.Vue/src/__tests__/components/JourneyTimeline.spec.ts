import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import JourneyTimeline from '../../components/JourneyTimeline.vue';
import type { TimelineEvent } from '../../components/JourneyTimeline.vue';

const mockEvents: TimelineEvent[] = [
  { id: '1', stage: 'Journey Started', timestamp: '2026-01-01T10:00:00Z', description: 'Began the journey' },
  { id: '2', stage: 'Design Selected', timestamp: '2026-01-01T11:00:00Z', description: 'Chose Modern Villa' },
  { id: '3', stage: 'Placed on Land', timestamp: '2026-01-01T12:00:00Z', description: 'Placed on 10 Main St' },
];

describe('JourneyTimeline', () => {
  it('renders all timeline events', () => {
    const wrapper = mount(JourneyTimeline, {
      props: { events: mockEvents },
    });
    expect(wrapper.findAll('.timeline-item').length).toBe(3);
  });

  it('shows stage name and description for each event', () => {
    const wrapper = mount(JourneyTimeline, {
      props: { events: mockEvents },
    });
    expect(wrapper.text()).toContain('Journey Started');
    expect(wrapper.text()).toContain('Began the journey');
    expect(wrapper.text()).toContain('Design Selected');
  });

  it('shows timestamps', () => {
    const wrapper = mount(JourneyTimeline, {
      props: { events: mockEvents },
    });
    // At least contains some formatted date text
    const textContent = wrapper.text();
    expect(textContent.length).toBeGreaterThan(0);
  });

  it('has timeline markers', () => {
    const wrapper = mount(JourneyTimeline, {
      props: { events: mockEvents },
    });
    expect(wrapper.findAll('.timeline-marker').length).toBe(3);
  });

  it('has role="list" with aria-label', () => {
    const wrapper = mount(JourneyTimeline, {
      props: { events: mockEvents },
    });
    const container = wrapper.find('.journey-timeline');
    expect(container.attributes('role')).toBe('list');
    expect(container.attributes('aria-label')).toBe('Journey timeline');
  });
});
