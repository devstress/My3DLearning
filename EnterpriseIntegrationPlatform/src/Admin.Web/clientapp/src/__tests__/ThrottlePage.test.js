import { describe, it, expect } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ThrottlePage from '../components/ThrottlePage.vue'

describe('ThrottlePage', () => {
  it('renders the throttle page with table and add button', async () => {
    const wrapper = mount(ThrottlePage)
    await flushPromises()
    expect(wrapper.find('#page-throttle').exists()).toBe(true)
    expect(wrapper.find('#btn-add-throttle').exists()).toBe(true)
  })

  it('shows Add Policy button text by default', async () => {
    const wrapper = mount(ThrottlePage)
    await flushPromises()
    expect(wrapper.find('#btn-add-throttle').text()).toContain('Add Policy')
  })

  it('toggles throttle form when Add Policy button is clicked', async () => {
    const wrapper = mount(ThrottlePage)
    await flushPromises()
    expect(wrapper.find('#throttle-form').exists()).toBe(false)
    await wrapper.find('#btn-add-throttle').trigger('click')
    expect(wrapper.find('#throttle-form').exists()).toBe(true)
    expect(wrapper.find('#throttle-policyId').exists()).toBe(true)
    expect(wrapper.find('#throttle-name').exists()).toBe(true)
    expect(wrapper.find('#throttle-maxMps').exists()).toBe(true)
    expect(wrapper.find('#btn-save-throttle').exists()).toBe(true)
  })
})
