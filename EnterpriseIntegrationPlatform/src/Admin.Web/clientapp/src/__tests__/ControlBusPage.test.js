import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ControlBusPage from '../components/ControlBusPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn(),
}))

import { apiFetch } from '../api.js'

describe('ControlBusPage', () => {
  it('renders the control bus page with command form', () => {
    const wrapper = mount(ControlBusPage)
    expect(wrapper.find('#page-controlbus').exists()).toBe(true)
    expect(wrapper.find('#cb-command-type').exists()).toBe(true)
    expect(wrapper.find('#cb-payload').exists()).toBe(true)
    expect(wrapper.find('#btn-send-command').exists()).toBe(true)
  })

  it('shows Send Command button text by default', () => {
    const wrapper = mount(ControlBusPage)
    expect(wrapper.find('#btn-send-command').text()).toContain('Send Command')
  })

  it('shows error for empty custom command type', async () => {
    const wrapper = mount(ControlBusPage)
    await wrapper.find('#cb-command-type').setValue('custom')
    await wrapper.find('#btn-send-command').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('command type')
  })

  it('sends command and shows result', async () => {
    apiFetch.mockResolvedValueOnce({ success: true, commandId: '123' })
    const wrapper = mount(ControlBusPage)
    await wrapper.find('#cb-payload').setValue('{"key":"value"}')
    await wrapper.find('#btn-send-command').trigger('click')
    await flushPromises()
    expect(apiFetch).toHaveBeenCalledWith('/api/admin/controlbus/send', expect.objectContaining({ method: 'POST' }))
    expect(wrapper.find('#cb-result').exists()).toBe(true)
  })

  it('tracks command history in session', async () => {
    apiFetch.mockResolvedValueOnce({ success: true })
    const wrapper = mount(ControlBusPage)
    await wrapper.find('#btn-send-command').trigger('click')
    await flushPromises()
    expect(wrapper.find('#cb-history').exists()).toBe(true)
    expect(wrapper.findAll('#cb-history tbody tr').length).toBe(1)
  })

  it('shows error for invalid JSON payload', async () => {
    const wrapper = mount(ControlBusPage)
    await wrapper.find('#cb-payload').setValue('not valid json')
    await wrapper.find('#btn-send-command').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Invalid JSON')
  })
})
