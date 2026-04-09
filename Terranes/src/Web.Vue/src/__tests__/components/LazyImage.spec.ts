import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import LazyImage from '../../components/LazyImage.vue';

let observerCallback: IntersectionObserverCallback;

class MockIntersectionObserver {
  constructor(callback: IntersectionObserverCallback) {
    observerCallback = callback;
  }
  observe = vi.fn();
  disconnect = vi.fn();
  unobserve = vi.fn();
}

beforeEach(() => {
  vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);
});

function triggerIntersect() {
  observerCallback(
    [{ isIntersecting: true } as IntersectionObserverEntry],
    {} as IntersectionObserver,
  );
}

describe('LazyImage', () => {
  it('renders placeholder before loading', () => {
    const wrapper = mount(LazyImage, {
      props: { src: '/test.jpg', alt: 'Test image' },
    });
    expect(wrapper.find('.card-img-placeholder').exists()).toBe(true);
  });

  it('shows img element after intersection', async () => {
    const wrapper = mount(LazyImage, {
      props: { src: '/test.jpg', alt: 'Test image' },
    });
    triggerIntersect();
    await wrapper.vm.$nextTick();
    expect(wrapper.find('img').exists()).toBe(true);
    expect(wrapper.find('img').attributes('src')).toBe('/test.jpg');
    expect(wrapper.find('img').attributes('alt')).toBe('Test image');
  });

  it('uses native loading="lazy" attribute', async () => {
    const wrapper = mount(LazyImage, {
      props: { src: '/test.jpg', alt: 'Test image' },
    });
    triggerIntersect();
    await wrapper.vm.$nextTick();
    expect(wrapper.find('img').attributes('loading')).toBe('lazy');
  });

  it('renders accessible placeholder with aria-label', () => {
    const wrapper = mount(LazyImage, {
      props: { src: '/test.jpg', alt: 'My photo' },
    });
    expect(wrapper.find('[role="img"]').attributes('aria-label')).toBe('My photo placeholder');
  });
});
