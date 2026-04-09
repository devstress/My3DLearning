import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ConfigPage from '../components/ConfigPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn().mockResolvedValue([]),
}))

import { apiFetch } from '../api.js'

describe('ConfigPage', () => {
  it('renders the config page with table and add button', async () => {
    const wrapper = mount(ConfigPage)
    await flushPromises()
    expect(wrapper.find('#page-config').exists()).toBe(true)
    expect(wrapper.find('#btn-add-config').exists()).toBe(true)
    expect(wrapper.find('#config-table').exists()).toBe(true)
  })

  it('shows Add Entry button text by default', async () => {
    const wrapper = mount(ConfigPage)
    await flushPromises()
    expect(wrapper.find('#btn-add-config').text()).toContain('Add Entry')
  })

  it('toggles config form when Add Entry button is clicked', async () => {
    const wrapper = mount(ConfigPage)
    await flushPromises()
    expect(wrapper.find('#config-form').exists()).toBe(false)
    await wrapper.find('#btn-add-config').trigger('click')
    expect(wrapper.find('#config-form').exists()).toBe(true)
    expect(wrapper.find('#config-key').exists()).toBe(true)
    expect(wrapper.find('#config-value').exists()).toBe(true)
    expect(wrapper.find('#btn-save-config').exists()).toBe(true)
  })

  it('has environment filter dropdown', async () => {
    const wrapper = mount(ConfigPage)
    await flushPromises()
    expect(wrapper.find('#config-env-filter').exists()).toBe(true)
  })
})
