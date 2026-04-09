import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import MessageFlowPage from '../components/MessageFlowPage.vue'

// Mock the api module
vi.mock('../api.js', () => ({
  apiFetch: vi.fn(),
  formatDate: (d) => d ? new Date(d).toLocaleString() : '—',
  formatDuration: (d) => d || '—',
}))

import { apiFetch } from '../api.js'

describe('MessageFlowPage', () => {
  it('renders search form with type selector and query input', () => {
    const wrapper = mount(MessageFlowPage)
    expect(wrapper.find('#page-message-flow').exists()).toBe(true)
    expect(wrapper.find('#flow-search-type').exists()).toBe(true)
    expect(wrapper.find('#flow-search-query').exists()).toBe(true)
    expect(wrapper.find('#btn-search-flow').exists()).toBe(true)
  })

  it('defaults to correlation ID search type', () => {
    const wrapper = mount(MessageFlowPage)
    expect(wrapper.find('#flow-search-type').element.value).toBe('correlation')
  })

  it('shows Track button text by default', () => {
    const wrapper = mount(MessageFlowPage)
    expect(wrapper.find('#btn-search-flow').text()).toContain('Track')
  })

  it('validates GUID format for correlation ID search', async () => {
    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-query').setValue('not-a-guid')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('valid GUID')
    expect(apiFetch).not.toHaveBeenCalled()
  })

  it('calls correlation API with valid GUID', async () => {
    const mockResult = {
      query: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      found: true,
      summary: 'Message delivered successfully',
      ollamaAvailable: true,
      latestStage: 'Delivery',
      latestStatus: 'Delivered',
      events: [
        { messageId: '123', correlationId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', stage: 'Ingestion', status: 'Pending', timestamp: '2026-01-01T00:00:00Z', details: 'Message received' },
        { messageId: '123', correlationId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', stage: 'Delivery', status: 'Delivered', timestamp: '2026-01-01T00:00:01Z', details: 'Delivered successfully' },
      ],
    }
    apiFetch.mockResolvedValueOnce(mockResult)

    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-query').setValue('3fa85f64-5717-4562-b3fc-2c963f66afa6')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()

    expect(apiFetch).toHaveBeenCalledWith('/api/admin/flow/correlation/3fa85f64-5717-4562-b3fc-2c963f66afa6')
    expect(wrapper.find('#flow-result').exists()).toBe(true)
    expect(wrapper.find('#flow-timeline').exists()).toBe(true)
    expect(wrapper.find('#flow-summary').exists()).toBe(true)
  })

  it('calls business key API for business search type', async () => {
    const mockResult = {
      query: 'ORDER-1234',
      found: true,
      summary: 'Tracked via business key',
      events: [{ messageId: '123', stage: 'Ingestion', status: 'Pending', timestamp: '2026-01-01T00:00:00Z' }],
      latestStage: 'Ingestion',
      latestStatus: 'Pending',
    }
    apiFetch.mockResolvedValueOnce(mockResult)

    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-type').setValue('business')
    await wrapper.find('#flow-search-query').setValue('ORDER-1234')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()

    expect(apiFetch).toHaveBeenCalledWith('/api/admin/flow/business/ORDER-1234')
    expect(wrapper.find('#flow-result').exists()).toBe(true)
  })

  it('renders timeline events with status badges', async () => {
    const mockResult = {
      query: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      found: true,
      summary: 'OK',
      latestStage: 'DeadLetter',
      latestStatus: 'DeadLettered',
      events: [
        { messageId: '123', stage: 'Ingestion', status: 'Pending', timestamp: '2026-01-01T00:00:00Z' },
        { messageId: '123', stage: 'Processing', status: 'InFlight', timestamp: '2026-01-01T00:00:01Z' },
        { messageId: '123', stage: 'DeadLetter', status: 'DeadLettered', timestamp: '2026-01-01T00:00:02Z', details: 'Max retries exceeded' },
      ],
    }
    apiFetch.mockResolvedValueOnce(mockResult)

    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-query').setValue('3fa85f64-5717-4562-b3fc-2c963f66afa6')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()

    const items = wrapper.findAll('.timeline-item')
    expect(items.length).toBe(3)
    expect(wrapper.findAll('.badge').length).toBeGreaterThanOrEqual(3)
  })

  it('expands event detail on click', async () => {
    const mockResult = {
      query: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      found: true,
      summary: 'OK',
      latestStage: 'Ingestion',
      latestStatus: 'Pending',
      events: [
        { messageId: '111', correlationId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', stage: 'Ingestion', status: 'Pending', timestamp: '2026-01-01T00:00:00Z', messageType: 'OrderCreated', source: 'orders-api', traceId: 'abc123' },
      ],
    }
    apiFetch.mockResolvedValueOnce(mockResult)

    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-query').setValue('3fa85f64-5717-4562-b3fc-2c963f66afa6')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()

    // Initially no expanded detail
    expect(wrapper.find('.timeline-expanded').exists()).toBe(false)

    // Click to expand
    await wrapper.find('.timeline-item').trigger('click')
    expect(wrapper.find('.timeline-expanded').exists()).toBe(true)
    expect(wrapper.text()).toContain('Message ID')
    expect(wrapper.text()).toContain('111')
    expect(wrapper.text()).toContain('OrderCreated')

    // Click again to collapse
    await wrapper.find('.timeline-item').trigger('click')
    expect(wrapper.find('.timeline-expanded').exists()).toBe(false)
  })

  it('shows not-found message when no events exist', async () => {
    const mockResult = {
      query: 'UNKNOWN-KEY',
      found: false,
      summary: 'No messages found',
      events: [],
    }
    apiFetch.mockResolvedValueOnce(mockResult)

    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-type').setValue('business')
    await wrapper.find('#flow-search-query').setValue('UNKNOWN-KEY')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('No events found')
  })

  it('shows error message on API failure', async () => {
    apiFetch.mockRejectedValueOnce(new Error('Network error'))

    const wrapper = mount(MessageFlowPage)
    await wrapper.find('#flow-search-query').setValue('3fa85f64-5717-4562-b3fc-2c963f66afa6')
    await wrapper.find('#btn-search-flow').trigger('click')
    await flushPromises()

    expect(wrapper.find('.alert-error').exists()).toBe(true)
    expect(wrapper.text()).toContain('Network error')
  })
})
