import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import TenantsPage from '../components/TenantsPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn(),
}))

describe('TenantsPage', () => {
  it('renders the tenants page with onboard button and lookup form', () => {
    const wrapper = mount(TenantsPage)
    expect(wrapper.find('#page-tenants').exists()).toBe(true)
    expect(wrapper.find('#btn-onboard-tenant').exists()).toBe(true)
    expect(wrapper.find('#tenant-lookup-id').exists()).toBe(true)
    expect(wrapper.find('#btn-lookup-tenant').exists()).toBe(true)
    expect(wrapper.find('#btn-deprovision-tenant').exists()).toBe(true)
  })

  it('shows Onboard Tenant button text by default', () => {
    const wrapper = mount(TenantsPage)
    expect(wrapper.find('#btn-onboard-tenant').text()).toContain('Onboard Tenant')
  })

  it('toggles onboard form when button is clicked', async () => {
    const wrapper = mount(TenantsPage)
    expect(wrapper.find('#onboard-form').exists()).toBe(false)
    await wrapper.find('#btn-onboard-tenant').trigger('click')
    expect(wrapper.find('#onboard-form').exists()).toBe(true)
    expect(wrapper.find('#tenant-id').exists()).toBe(true)
    expect(wrapper.find('#tenant-name').exists()).toBe(true)
    expect(wrapper.find('#tenant-tier').exists()).toBe(true)
    expect(wrapper.find('#btn-submit-onboard').exists()).toBe(true)
  })

  it('shows error when onboarding without tenant ID', async () => {
    const wrapper = mount(TenantsPage)
    await wrapper.find('#btn-onboard-tenant').trigger('click')
    await wrapper.find('#btn-submit-onboard').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('required')
  })
})
