import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import SearchBar from '../../components/SearchBar.vue';

describe('SearchBar', () => {
  it('renders search input with placeholder', () => {
    const wrapper = mount(SearchBar, {
      props: { modelValue: '', placeholder: 'Search villages...' },
    });
    const input = wrapper.find('input');
    expect(input.exists()).toBe(true);
    expect(input.attributes('placeholder')).toBe('Search villages...');
  });

  it('renders search icon', () => {
    const wrapper = mount(SearchBar, { props: { modelValue: '' } });
    expect(wrapper.find('.input-group-text').text()).toBe('🔍');
  });

  it('emits update:modelValue on input', async () => {
    const wrapper = mount(SearchBar, { props: { modelValue: '' } });
    await wrapper.find('input').setValue('test');
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['test']);
  });

  it('shows clear button when value is non-empty', () => {
    const wrapper = mount(SearchBar, { props: { modelValue: 'hello' } });
    const clearBtn = wrapper.find('button[aria-label="Clear search"]');
    expect(clearBtn.exists()).toBe(true);
  });

  it('hides clear button when value is empty', () => {
    const wrapper = mount(SearchBar, { props: { modelValue: '' } });
    const clearBtn = wrapper.find('button[aria-label="Clear search"]');
    expect(clearBtn.exists()).toBe(false);
  });

  it('emits empty string on clear button click', async () => {
    const wrapper = mount(SearchBar, { props: { modelValue: 'hello' } });
    await wrapper.find('button[aria-label="Clear search"]').trigger('click');
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['']);
  });

  it('has aria-label on input', () => {
    const wrapper = mount(SearchBar, { props: { modelValue: '' } });
    expect(wrapper.find('input').attributes('aria-label')).toBe('Search');
  });
});
