import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import EventStorePage from '../components/EventStorePage.vue'

vi.stubGlobal('fetch', vi.fn())

describe('EventStorePage', () => {
  beforeEach(() => {
    fetch.mockReset()
  })

  it('renders the page with search input', () => {
    const wrapper = mount(EventStorePage)
    expect(wrapper.find('#page-events').exists()).toBe(true)
    expect(wrapper.find('#event-stream-id').exists()).toBe(true)
    expect(wrapper.find('#btn-load-stream').exists()).toBe(true)
  })

  it('shows Load Stream button text', () => {
    const wrapper = mount(EventStorePage)
    expect(wrapper.find('#btn-load-stream').text()).toContain('Load Stream')
  })

  it('displays event timeline after loading', async () => {
    const mockEvents = [
      { eventType: 'OrderCreated', version: 1, timestamp: '2026-04-09T00:00:00Z', data: '{}', metadata: {} },
      { eventType: 'OrderShipped', version: 2, timestamp: '2026-04-09T00:01:00Z', data: '{}', metadata: {} },
    ]
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockEvents)),
    })
    const wrapper = mount(EventStorePage)
    await wrapper.find('#event-stream-id').setValue('order-123')
    await wrapper.find('#btn-load-stream').trigger('click')
    await flushPromises()
    expect(wrapper.find('#event-results').exists()).toBe(true)
    expect(wrapper.text()).toContain('OrderCreated')
    expect(wrapper.text()).toContain('OrderShipped')
  })

  it('shows stat cards with event count and version', async () => {
    const mockEvents = [
      { eventType: 'E1', version: 1, timestamp: '2026-04-09T00:00:00Z', data: '{}' },
      { eventType: 'E2', version: 2, timestamp: '2026-04-09T00:01:00Z', data: '{}' },
      { eventType: 'E3', version: 3, timestamp: '2026-04-09T00:02:00Z', data: '{}' },
    ]
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockEvents)),
    })
    const wrapper = mount(EventStorePage)
    await wrapper.find('#event-stream-id').setValue('agg-1')
    await wrapper.find('#btn-load-stream').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('3') // event count & latest version
  })

  it('shows empty state for unknown stream', async () => {
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve('[]'),
    })
    const wrapper = mount(EventStorePage)
    await wrapper.find('#event-stream-id').setValue('nonexistent')
    await wrapper.find('#btn-load-stream').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('No events found')
  })

  it('shows error on API failure', async () => {
    fetch.mockResolvedValueOnce({
      ok: false,
      text: () => Promise.resolve('Stream error'),
    })
    const wrapper = mount(EventStorePage)
    await wrapper.find('#event-stream-id').setValue('order-123')
    await wrapper.find('#btn-load-stream').trigger('click')
    await flushPromises()
    expect(wrapper.find('.alert-error').exists()).toBe(true)
  })
})
