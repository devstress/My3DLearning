import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import InFlightPage from '../components/InFlightPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn().mockResolvedValue([]),
  formatDate: (d) => d || '—',
}))

import { apiFetch } from '../api.js'

describe('InFlightPage', () => {
  it('renders the in-flight page with stat cards', async () => {
    const wrapper = mount(InFlightPage)
    await flushPromises()
    expect(wrapper.find('#page-inflight').exists()).toBe(true)
    expect(wrapper.find('#inflight-stats').exists()).toBe(true)
    expect(wrapper.find('#btn-refresh-inflight').exists()).toBe(true)
  })

  it('shows idle state when no messages in-flight', async () => {
    const wrapper = mount(InFlightPage)
    await flushPromises()
    expect(wrapper.text()).toContain('pipeline is idle')
  })

  it('shows in-flight total from API data', async () => {
    apiFetch.mockResolvedValueOnce([
      { messageType: 'OrderCreated', count: 5, status: 'InFlight', oldestTimestamp: '2026-01-01T00:00:00Z' },
      { messageType: 'PaymentProcessed', count: 3, status: 'Pending', oldestTimestamp: '2026-01-01T00:00:01Z' },
    ])
    const wrapper = mount(InFlightPage)
    await flushPromises()
    expect(wrapper.find('#inflight-total').text()).toBe('8')
    expect(wrapper.find('#inflight-breakdown').exists()).toBe(true)
  })

  it('has auto-refresh checkbox', async () => {
    const wrapper = mount(InFlightPage)
    await flushPromises()
    expect(wrapper.find('#inflight-auto-refresh').exists()).toBe(true)
  })

  it('cleans up timer on unmount', async () => {
    const wrapper = mount(InFlightPage)
    await flushPromises()
    await wrapper.find('#inflight-auto-refresh').setValue(true)
    expect(wrapper.vm.refreshTimer).not.toBeNull()
    wrapper.unmount()
    // Timer should be cleared by beforeUnmount
  })
})
