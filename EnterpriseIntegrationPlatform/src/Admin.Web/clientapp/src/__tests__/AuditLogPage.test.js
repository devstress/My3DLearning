import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import AuditLogPage from '../components/AuditLogPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn().mockResolvedValue([]),
  formatDate: (d) => d || '—',
}))

import { apiFetch } from '../api.js'

describe('AuditLogPage', () => {
  it('renders the audit log page with filter form', async () => {
    const wrapper = mount(AuditLogPage)
    await flushPromises()
    expect(wrapper.find('#page-auditlog').exists()).toBe(true)
    expect(wrapper.find('#audit-action-filter').exists()).toBe(true)
    expect(wrapper.find('#audit-apikey-filter').exists()).toBe(true)
    expect(wrapper.find('#btn-search-audit').exists()).toBe(true)
    expect(wrapper.find('#btn-refresh-audit').exists()).toBe(true)
  })

  it('shows empty state when no audit entries', async () => {
    const wrapper = mount(AuditLogPage)
    await flushPromises()
    expect(wrapper.text()).toContain('No audit entries found')
  })

  it('renders audit entries from API', async () => {
    apiFetch.mockResolvedValueOnce([
      { timestamp: '2026-01-01T00:00:00Z', action: 'GetPlatformStatus', targetId: null, apiKey: 'admin-****' },
      { timestamp: '2026-01-01T00:00:01Z', action: 'DlqResubmit', targetId: '3fa85f64-5717-4562-b3fc-2c963f66afa6', apiKey: 'admin-****' },
    ])
    const wrapper = mount(AuditLogPage)
    await flushPromises()
    expect(wrapper.find('#audit-entries').exists()).toBe(true)
    expect(wrapper.text()).toContain('GetPlatformStatus')
    expect(wrapper.text()).toContain('DlqResubmit')
  })

  it('shows Load More button when results fill a page', async () => {
    const entries = Array.from({ length: 50 }, (_, i) => ({
      timestamp: '2026-01-01T00:00:00Z', action: `Action${i}`, targetId: null, apiKey: '****',
    }))
    apiFetch.mockResolvedValueOnce(entries)
    const wrapper = mount(AuditLogPage)
    await flushPromises()
    expect(wrapper.find('#btn-load-more-audit').exists()).toBe(true)
  })

  it('shows stat cards for entries shown and unique actions', async () => {
    apiFetch.mockResolvedValueOnce([
      { action: 'GetStatus' },
      { action: 'GetStatus' },
      { action: 'DlqResubmit' },
    ])
    const wrapper = mount(AuditLogPage)
    await flushPromises()
    // 3 entries shown, 2 unique actions
    expect(wrapper.text()).toContain('3')
    expect(wrapper.text()).toContain('2')
  })
})
