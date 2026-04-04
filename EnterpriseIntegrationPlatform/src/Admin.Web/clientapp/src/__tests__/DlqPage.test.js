import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import DlqPage from '../components/DlqPage.vue'

describe('DlqPage', () => {
  it('renders the DLQ page with form fields', () => {
    const wrapper = mount(DlqPage)
    expect(wrapper.find('#page-dlq').exists()).toBe(true)
    expect(wrapper.find('#dlq-correlationId').exists()).toBe(true)
    expect(wrapper.find('#dlq-messageType').exists()).toBe(true)
    expect(wrapper.find('#btn-resubmit-dlq').exists()).toBe(true)
  })

  it('shows Resubmit button text by default', () => {
    const wrapper = mount(DlqPage)
    expect(wrapper.find('#btn-resubmit-dlq').text()).toContain('Resubmit')
  })
})
