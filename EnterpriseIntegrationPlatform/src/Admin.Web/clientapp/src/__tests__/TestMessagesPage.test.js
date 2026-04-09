import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import TestMessagesPage from '../components/TestMessagesPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn(),
}))

import { apiFetch } from '../api.js'

describe('TestMessagesPage', () => {
  it('renders the test message page with form fields', () => {
    const wrapper = mount(TestMessagesPage)
    expect(wrapper.find('#page-test-messages').exists()).toBe(true)
    expect(wrapper.find('#test-target-topic').exists()).toBe(true)
    expect(wrapper.find('#btn-generate-test').exists()).toBe(true)
  })

  it('shows Send Test Message button text by default', () => {
    const wrapper = mount(TestMessagesPage)
    expect(wrapper.find('#btn-generate-test').text()).toContain('Send Test Message')
  })

  it('shows error when no topic is provided', async () => {
    const wrapper = mount(TestMessagesPage)
    await wrapper.find('#btn-generate-test').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('target topic')
  })

  it('sends simple test message to API', async () => {
    apiFetch.mockResolvedValueOnce({ messageId: '123', success: true })
    const wrapper = mount(TestMessagesPage)
    await wrapper.find('#test-target-topic').setValue('orders.test')
    await wrapper.find('#btn-generate-test').trigger('click')
    await flushPromises()
    expect(apiFetch).toHaveBeenCalledWith('/api/admin/test-messages', expect.objectContaining({ method: 'POST' }))
    expect(wrapper.find('#test-result').exists()).toBe(true)
  })

  it('tracks test history in session', async () => {
    apiFetch.mockResolvedValueOnce({ messageId: '456', success: true })
    const wrapper = mount(TestMessagesPage)
    await wrapper.find('#test-target-topic').setValue('orders.test')
    await wrapper.find('#btn-generate-test').trigger('click')
    await flushPromises()
    expect(wrapper.find('#test-history').exists()).toBe(true)
    expect(wrapper.findAll('.test-history-item').length).toBe(1)
  })

  it('shows custom payload textarea when checkbox is enabled', async () => {
    const wrapper = mount(TestMessagesPage)
    expect(wrapper.find('#test-custom-payload').exists()).toBe(false)
    await wrapper.find('input[type="checkbox"]').setValue(true)
    expect(wrapper.find('#test-custom-payload').exists()).toBe(true)
  })
})
