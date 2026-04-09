import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ConnectorsPage from '../components/ConnectorsPage.vue'

vi.stubGlobal('fetch', vi.fn())

describe('ConnectorsPage', () => {
  beforeEach(() => {
    fetch.mockReset()
  })

  it('renders the page', () => {
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve('[]'),
    })
    const wrapper = mount(ConnectorsPage)
    expect(wrapper.find('#page-connectors').exists()).toBe(true)
    expect(wrapper.find('#btn-refresh-connectors').exists()).toBe(true)
  })

  it('shows empty state when no connectors', async () => {
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve('[]'),
    })
    const wrapper = mount(ConnectorsPage)
    await flushPromises()
    expect(wrapper.text()).toContain('No connectors registered')
  })

  it('renders connector rows', async () => {
    const mockData = [
      { name: 'http-orders', connectorType: 'Http', healthStatus: 'Healthy', lastChecked: '2026-04-09T00:00:00Z' },
      { name: 'sftp-invoices', connectorType: 'Sftp', healthStatus: 'Degraded', lastChecked: '2026-04-09T00:00:00Z' },
    ]
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockData)),
    })
    const wrapper = mount(ConnectorsPage)
    await flushPromises()
    expect(wrapper.find('#connector-table').exists()).toBe(true)
    expect(wrapper.text()).toContain('http-orders')
    expect(wrapper.text()).toContain('sftp-invoices')
  })

  it('shows stat cards with correct counts', async () => {
    const mockData = [
      { name: 'c1', connectorType: 'Http', healthStatus: 'Healthy' },
      { name: 'c2', connectorType: 'Sftp', healthStatus: 'Unhealthy' },
      { name: 'c3', connectorType: 'Http', healthStatus: 'Healthy' },
    ]
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockData)),
    })
    const wrapper = mount(ConnectorsPage)
    await flushPromises()
    expect(wrapper.find('#connector-stats').exists()).toBe(true)
    // Total 3, Healthy 2, Unhealthy 1
    const stats = wrapper.find('#connector-stats').text()
    expect(stats).toContain('3')
    expect(stats).toContain('2')
    expect(stats).toContain('1')
  })

  it('filters by connector type', async () => {
    const mockData = [
      { name: 'c1', connectorType: 'Http', healthStatus: 'Healthy' },
      { name: 'c2', connectorType: 'Sftp', healthStatus: 'Healthy' },
    ]
    fetch.mockResolvedValueOnce({
      ok: true,
      text: () => Promise.resolve(JSON.stringify(mockData)),
    })
    const wrapper = mount(ConnectorsPage)
    await flushPromises()
    await wrapper.find('#conn-type-filter').setValue('Http')
    const tableText = wrapper.find('#connector-table').text()
    expect(tableText).toContain('c1')
    expect(tableText).not.toContain('c2')
  })
})
