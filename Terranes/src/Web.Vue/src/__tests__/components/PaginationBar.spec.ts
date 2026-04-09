import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import PaginationBar from '../../components/PaginationBar.vue';

describe('PaginationBar', () => {
  it('renders nothing when totalPages is 1', () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 1, totalPages: 1 },
    });
    expect(wrapper.find('nav').exists()).toBe(false);
  });

  it('renders pagination when totalPages > 1', () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 1, totalPages: 3 },
    });
    expect(wrapper.find('nav').exists()).toBe(true);
    expect(wrapper.findAll('.page-item').length).toBe(5); // prev + 3 pages + next
  });

  it('marks current page as active', () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 2, totalPages: 3 },
    });
    const activeItem = wrapper.find('.page-item.active');
    expect(activeItem.exists()).toBe(true);
    expect(activeItem.find('.page-link').text()).toBe('2');
  });

  it('disables previous button on first page', () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 1, totalPages: 3 },
    });
    const prevItem = wrapper.findAll('.page-item').at(0)!;
    expect(prevItem.classes()).toContain('disabled');
  });

  it('disables next button on last page', () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 3, totalPages: 3 },
    });
    const items = wrapper.findAll('.page-item');
    const nextItem = items.at(items.length - 1)!;
    expect(nextItem.classes()).toContain('disabled');
  });

  it('emits page event on page click', async () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 1, totalPages: 3 },
    });
    const page2Btn = wrapper.findAll('.page-link').find((b) => b.text() === '2');
    await page2Btn!.trigger('click');
    expect(wrapper.emitted('page')?.[0]).toEqual([2]);
  });

  it('has aria-label on navigation', () => {
    const wrapper = mount(PaginationBar, {
      props: { currentPage: 1, totalPages: 2 },
    });
    expect(wrapper.find('nav').attributes('aria-label')).toBe('Pagination');
  });
});
