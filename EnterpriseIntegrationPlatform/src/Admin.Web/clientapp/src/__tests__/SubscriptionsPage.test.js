import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import SubscriptionsPage from '../components/SubscriptionsPage.vue'

vi.mock('../api.js', () => ({
  apiFetch: vi.fn().mockResolvedValue([]),
  formatDate: (d) => d || '—',
}))

import { apiFetch } from '../api.js'

describe('SubscriptionsPage', () => {
  it('renders the subscriptions page with table and refresh button', async () => {
    const wrapper = mount(SubscriptionsPage)
    await flushPromises()
    expect(wrapper.find('#page-subscriptions').exists()).toBe(true)
    expect(wrapper.find('#btn-refresh-subs').exists()).toBe(true)
    expect(wrapper.find('#subs-table').exists()).toBe(true)
  })

  it('has broker filter dropdown', async () => {
    const wrapper = mount(SubscriptionsPage)
    await flushPromises()
    expect(wrapper.find('#sub-broker-filter').exists()).toBe(true)
  })

  it('shows empty state when no subscriptions', async () => {
    const wrapper = mount(SubscriptionsPage)
    await flushPromises()
    expect(wrapper.text()).toContain('No active subscriptions')
  })

  it('renders subscription rows from API', async () => {
    apiFetch.mockResolvedValueOnce([
      { topic: 'orders.created', consumerGroup: 'order-processor', brokerType: 'NatsJetStream', isActive: true },
      { topic: 'payments.completed', consumerGroup: 'payment-handler', brokerType: 'Kafka', isActive: true },
    ])
    const wrapper = mount(SubscriptionsPage)
    await flushPromises()
    const rows = wrapper.findAll('#subs-table tbody tr')
    expect(rows.length).toBe(2)
    expect(wrapper.text()).toContain('orders.created')
    expect(wrapper.text()).toContain('Kafka')
  })

  it('shows stat cards with correct counts', async () => {
    apiFetch.mockResolvedValueOnce([
      { topic: 'orders', consumerGroup: 'cg1', brokerType: 'NatsJetStream', isActive: true },
      { topic: 'payments', consumerGroup: 'cg2', brokerType: 'Kafka', isActive: false },
    ])
    const wrapper = mount(SubscriptionsPage)
    await flushPromises()
    expect(wrapper.text()).toContain('2') // total
    expect(wrapper.text()).toContain('1') // active
  })
})
