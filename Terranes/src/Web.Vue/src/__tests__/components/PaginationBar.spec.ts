import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import PaginationBar from '../../components/PaginationBar.vue';

describe('PaginationBar', () => {
  it('shows "Showing X–Y of Z" text', () => {
    const wrapper = mount(PaginationBar, {
      props: { totalItems: 50, pageSize: 12, currentPage: 1 },
    });
    expect(wrapper.text()).toContain('Showing 1–12 of 50');
  });

  it('disables Prev button on first page', () => {
    const wrapper = mount(PaginationBar, {
      props: { totalItems: 50, pageSize: 12, currentPage: 1 },
    });
    const prevBtn = wrapper.findAll('button').find((b) => b.text().includes('Prev'));
    expect(prevBtn!.attributes('disabled')).toBeDefined();
  });

  it('disables Next button on last page', () => {
    const wrapper = mount(PaginationBar, {
      props: { totalItems: 10, pageSize: 12, currentPage: 1 },
    });
    const nextBtn = wrapper.findAll('button').find((b) => b.text().includes('Next'));
    expect(nextBtn!.attributes('disabled')).toBeDefined();
  });

  it('emits pageChange on next click', async () => {
    const wrapper = mount(PaginationBar, {
      props: { totalItems: 50, pageSize: 12, currentPage: 1 },
    });
    const nextBtn = wrapper.findAll('button').find((b) => b.text().includes('Next'));
    await nextBtn!.trigger('click');
    expect(wrapper.emitted('pageChange')).toBeTruthy();
    expect(wrapper.emitted('pageChange')![0]).toEqual([2]);
  });
});
