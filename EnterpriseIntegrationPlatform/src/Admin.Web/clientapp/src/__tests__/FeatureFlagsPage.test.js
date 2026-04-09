import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import FeatureFlagsPage from '../components/FeatureFlagsPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn().mockResolvedValue([]),
}))

import { apiFetch } from '../api.js'

describe('FeatureFlagsPage', () => {
  it('renders the feature flags page with table and add button', async () => {
    const wrapper = mount(FeatureFlagsPage)
    await flushPromises()
    expect(wrapper.find('#page-features').exists()).toBe(true)
    expect(wrapper.find('#btn-add-flag').exists()).toBe(true)
    expect(wrapper.find('#flags-table').exists()).toBe(true)
  })

  it('shows Add Flag button text by default', async () => {
    const wrapper = mount(FeatureFlagsPage)
    await flushPromises()
    expect(wrapper.find('#btn-add-flag').text()).toContain('Add Flag')
  })

  it('toggles flag form when Add Flag button is clicked', async () => {
    const wrapper = mount(FeatureFlagsPage)
    await flushPromises()
    expect(wrapper.find('#flag-form').exists()).toBe(false)
    await wrapper.find('#btn-add-flag').trigger('click')
    expect(wrapper.find('#flag-form').exists()).toBe(true)
    expect(wrapper.find('#flag-name').exists()).toBe(true)
    expect(wrapper.find('#flag-rollout').exists()).toBe(true)
    expect(wrapper.find('#btn-save-flag').exists()).toBe(true)
  })

  it('renders empty state for no flags', async () => {
    const wrapper = mount(FeatureFlagsPage)
    await flushPromises()
    expect(wrapper.text()).toContain('No feature flags')
  })
})
