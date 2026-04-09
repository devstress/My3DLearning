import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ReplayPage from '../components/ReplayPage.vue'

vi.stubGlobal('fetch', vi.fn())

describe('ReplayPage', () => {
  beforeEach(() => {
    fetch.mockReset()
  })

  it('renders the page with form fields', () => {
    const wrapper = mount(ReplayPage)
    expect(wrapper.find('#page-replay').exists()).toBe(true)
    expect(wrapper.find('#replay-correlationId').exists()).toBe(true)
    expect(wrapper.find('#replay-messageType').exists()).toBe(true)
    expect(wrapper.find('#replay-source').exists()).toBe(true)
    expect(wrapper.find('#replay-target').exists()).toBe(true)
    expect(wrapper.find('#btn-start-replay').exists()).toBe(true)
  })

  it('shows Start Replay button text', () => {
    const wrapper = mount(ReplayPage)
    expect(wrapper.find('#btn-start-replay').text()).toContain('Start Replay')
  })

  it('displays replay result after successful replay', async () => {
    const mockResult = {
      replayedCount: 5,
      skippedCount: 1,
      failedCount: 0,
      startedAt: '2026-04-09T00:00:00Z',
      completedAt: '2026-04-09T00:00:01Z',
    }
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockResult)),
    })
    const wrapper = mount(ReplayPage)
    await wrapper.find('#btn-start-replay').trigger('click')
    await flushPromises()
    expect(wrapper.find('#replay-result').exists()).toBe(true)
    expect(wrapper.text()).toContain('5')
    expect(wrapper.text()).toContain('1')
  })

  it('tracks replay in session history', async () => {
    const mockResult = {
      replayedCount: 3,
      skippedCount: 0,
      failedCount: 0,
      startedAt: '2026-04-09T00:00:00Z',
      completedAt: '2026-04-09T00:00:01Z',
    }
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockResult)),
    })
    const wrapper = mount(ReplayPage)
    await wrapper.find('#btn-start-replay').trigger('click')
    await flushPromises()
    expect(wrapper.find('#replay-history').exists()).toBe(true)
  })

  it('shows error on API failure', async () => {
    fetch.mockResolvedValueOnce({
      ok: false,
      text: () => Promise.resolve('Replay failed'),
    })
    const wrapper = mount(ReplayPage)
    await wrapper.find('#btn-start-replay').trigger('click')
    await flushPromises()
    expect(wrapper.find('.alert-error').exists()).toBe(true)
  })

  it('computes replay duration from result timestamps', async () => {
    const mockResult = {
      replayedCount: 2,
      skippedCount: 0,
      failedCount: 0,
      startedAt: '2026-04-09T00:00:00.000Z',
      completedAt: '2026-04-09T00:00:00.500Z',
    }
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockResult)),
    })
    const wrapper = mount(ReplayPage)
    await wrapper.find('#btn-start-replay').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('500ms')
  })
})
